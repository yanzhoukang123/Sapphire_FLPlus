using Azure.CameraLib;
using Azure.CommandLib;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Azure.ImagingSystem
{
    public class ImagingLiveCommand : ThreadBase
    {
        // Image capture status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image capture status event
        public event CommandStatusHandler CommandStatus;

        // Image received delegate
        public delegate void ImageReceivedHandler(BitmapSource displayBitmap);
        /// <summary>
        /// Live image received event handler. Triggered everytime the live image is received.
        /// </summary>
        public event ImageReceivedHandler LiveImageReceived;

        private CameraController _ActiveCamera = null;
        private EthernetController _CommController;
        private string _StatusText = string.Empty;
        private BitmapSource _DisplayBitmap;
        private CameraParameterStruct _CameraParameter = null;
        private AutoResetEvent _LiveAutoResetEvent = new AutoResetEvent(false);
        private int Width;
        private int Height;
        ushort[] pixelData = null;
        public ImagingLiveCommand(CameraController camera,
                                  EthernetController ethernet,
                                  CameraParameterStruct cameraParameter)
        {
            _CommController = ethernet;
            _ActiveCamera = camera;
            _CameraParameter = cameraParameter;
            _ActiveCamera.LiveImageReceived += _ActiveCamera_LiveImageReceived;
        }
        public override void Finish()
        {

            _CommController.SetRGBLightRegisterControl(0);//关闭R/G/B灯光
            _StatusText = string.Empty;
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }

            if (_ActiveCamera != null)
            {
                //_ActiveCamera.ChangeTriggerMode(0);
                _ActiveCamera.LiveCapture = false;
                _ActiveCamera.LiveImageReceived -= _ActiveCamera_LiveImageReceived;
            }
            _DisplayBitmap = null;
        }
        private unsafe void OnEventImage()
        {
            WriteableBitmap temp = new WriteableBitmap(Width, Height, 0, 0, PixelFormats.Gray16, null);
            Toupcam.FrameInfoV3 info = new Toupcam.FrameInfoV3();
            bool bOK = false;
            int bits = 16;
            try
            {
                temp.Lock();
                try
                {
                    bOK = _ActiveCamera.cam_.PullImageV3(temp.BackBuffer, 0, bits, temp.BackBufferStride, out info); // check the return value
                    //Console.WriteLine(bOK);
                    temp.AddDirtyRect(new Int32Rect(0, 0, temp.PixelWidth, temp.PixelHeight));
                }
                finally
                {
                    temp.Unlock();
                    PixelFormat format = PixelFormats.Gray16;
                    int stride = (temp.PixelWidth * temp.Format.BitsPerPixel + 7) / 8;
                    pixelData = new ushort[temp.PixelHeight * stride];
                    temp.CopyPixels(pixelData, stride, 0);
                    //14bit to 16bit
                    fixed (ushort* ptr = pixelData)
                    {
                        for (int i = 0; i < pixelData.Length; i++)
                        {
                            *(ptr + i) = (ushort)((*(ptr + i)) << 2);
                        }
                    }
                    _DisplayBitmap = BitmapSource.Create(temp.PixelWidth, temp.PixelHeight, 96, 96, format, null, pixelData, stride);
                    //if (LiveImageReceived != null)
                    //{
                    //    if (_DisplayBitmap != null)
                    //    {
                    //        // Rotate the image 180 degree
                    //        TransformedBitmap tb = new TransformedBitmap();
                    //        tb.BeginInit();
                    //        tb.Source = _DisplayBitmap;
                    //        System.Windows.Media.RotateTransform transform = new System.Windows.Media.RotateTransform(180);
                    //        tb.Transform = transform;
                    //        tb.EndInit();
                    //        _DisplayBitmap = new WriteableBitmap((BitmapSource)tb);
                    //    }
                    //}
                    if (_DisplayBitmap != null)
                    {
                        if (_DisplayBitmap.CanFreeze)
                        {
                            _DisplayBitmap.Freeze();
                        }

                        if (LiveImageReceived != null)
                        {
                            LiveImageReceived(_DisplayBitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            // temp = null;
        }
        private void DelegateOnEventCallback(Toupcam.eEVENT evt)
        {
            if (_ActiveCamera.cam_ != null)
            {
                switch (evt)
                {
                    case Toupcam.eEVENT.EVENT_IMAGE:
                        OnEventImage();
                        break;
                    default:
                        break;
                }
            }
        }
        public override void ThreadFunction()
        {
            uint ExposureTime = _CameraParameter.ExposureTime;
            int Gain = _CameraParameter.Gain;
            int Bin = _CameraParameter.Bin;
            Width = _CameraParameter.Width;
            Height = _CameraParameter.Height;

            _StatusText = "Preparing...";
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }
            _CommController.SetRGBLightRegisterControl(1);//打开R/G/B灯光
            System.Threading.Thread.Sleep(1000);    // NOTE: wait for camera cover to be opened

            _StatusText = "LIVE MODE";
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }
            try
            {
                //_ActiveCamera.cam_.Stop();//为了重新定义DelegateOnEventCallback代理方法，需要先执行Stop()
                //_ActiveCamera.cam_.StartPullModeWithCallback(new Toupcam.DelegateEventCallback(DelegateOnEventCallback));
                //if (_ActiveCamera.SetExpoTime(ExposureTime))
                //{
                //    _ActiveCamera.ChangeCaptureMode(0xffff); //循环模式
                //}

                if (_ActiveCamera.SetExpoTime(ExposureTime))
                {
                    _ActiveCamera.ChangeCaptureMode(0); //视频模式
                    _ActiveCamera.LiveCapture = true;
                }
                _LiveAutoResetEvent.WaitOne();
            }
            catch (System.Threading.ThreadAbortException)
            {
                // don't throw exception if the user abort the process.
            }
            catch (System.Runtime.InteropServices.SEHException)
            {
                // The SEHException class handles SEH errors that are thrown from unmanaged code,
                // but have not been mapped to another .NET Framework exception.
                throw new OutOfMemoryException();
            }
            catch (System.Runtime.InteropServices.COMException cex)
            {
                if (cex.ErrorCode == unchecked((int)0x88980003))
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    throw cex;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Live mode error.", ex);
            }
            finally
            {
                _ActiveCamera.LiveCapture = false;
                _CommController.SetRGBLightRegisterControl(0);
                _LiveAutoResetEvent.Set();
                _ActiveCamera.LiveImageReceived -= _ActiveCamera_LiveImageReceived;
            }

        }

        private void _ActiveCamera_LiveImageReceived(WriteableBitmap dis)
        {
            //WriteableBitmap temp;
            //ImageProcessing.FrameToBitmap(out temp, pixelData, _ActiveCamera.Width, _ActiveCamera.Height);
            //if (LiveImageReceived != null)
            //{
            //    if (_DisplayBitmap != null)
            //    {
            //        // Rotate the image 180 degree
            //        TransformedBitmap tb = new TransformedBitmap();
            //        tb.BeginInit();
            //        tb.Source = _DisplayBitmap;
            //        System.Windows.Media.RotateTransform transform = new System.Windows.Media.RotateTransform(180);
            //        tb.Transform = transform;
            //        tb.EndInit();
            //        _DisplayBitmap = new WriteableBitmap((BitmapSource)tb);
            //    }
            //}
           // _DisplayBitmap = ImageProcessing.ConvertCameraWriteableBitmapToBitmapImage(dis);
            //_DisplayBitmap = dis;
            //if (_DisplayBitmap != null)
            //{
            //    if (_DisplayBitmap.CanFreeze)
            //    {
            //        _DisplayBitmap.Freeze();
            //    }
                LiveImageReceived?.Invoke(dis);
           // }
        }

        public class CameraParameterStruct
        {
            public uint ExposureTime;  //um
            public int Bin;
            public int Gain;
            public int Width;
            public int Height;
        }
        public override void AbortWork()
        {
            _ActiveCamera.LiveCapture = false;
            _ActiveCamera.LiveImageReceived -= _ActiveCamera_LiveImageReceived;
            //_ActiveCamera.ChangeTriggerMode(0);
        }
    }
}
