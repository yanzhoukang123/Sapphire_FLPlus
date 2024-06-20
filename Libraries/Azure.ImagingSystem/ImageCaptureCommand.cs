using Azure.CameraLib;
using Azure.CommandLib;
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
    public class ImageCaptureCommand : ThreadBase
    {
        // Image capture status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image capture status event
        public event CommandStatusHandler CommandStatus;
        // Image capture completion time estimate delegate
        public delegate void CommandCompletionEstHandler(ThreadBase sender, DateTime dateTime, double estTime);
        // Image capture completion time estimate event
        public event CommandCompletionEstHandler CompletionEstimate;
        private Dispatcher _CallingDispatcher = null;
        private ImageChannelSettings _ImageChannel = null;
        private ImageInfo _ImageInfo = null;
        private bool _IsCommandAborted = false;
        private CameraController _ActiveCamera = null;
        private WriteableBitmap captureimage = null;
        private CameraParameterStruct _CameraParameter = null;
        private int Width;
        private int Height;
        public ImageCaptureCommand(Dispatcher callingDispatcher,
                                  CameraController camera,
                                  ImageChannelSettings imageChannel,
                                  CameraParameterStruct cameraParameter)
        {
            _CallingDispatcher = callingDispatcher;
            _ActiveCamera = camera;
            _ImageChannel = imageChannel;
            _CameraParameter = cameraParameter;
        }
        public ImageInfo ImageInfo
        {
            get { return _ImageInfo; }
        }

        public WriteableBitmap CaptureImage { get => captureimage; set => captureimage = value; }

        public override void ThreadFunction()
        {
            if (CommandStatus != null)
            {
                CommandStatus(this, "Preparing to capture....");
            }
            uint ExposureTime=_CameraParameter.ExposureTime;
            int Gain = _CameraParameter.Gain;
            int Bin = _CameraParameter.Bin;
            Width = _CameraParameter.Width;
            Height = _CameraParameter.Height;



            // Setup estimate time remaining countdown
            int iBinningFactor = _ImageChannel.BinningMode;
            double estCaptureTime = _ImageChannel.Exposure;
            DateTime dateTime = DateTime.Now;
            _ImageInfo = new Azure.Image.Processing.ImageInfo();
            _ImageInfo.DateTime = System.String.Format("{0:G}", dateTime.ToString());
            _ImageInfo.CaptureType = "Chemi";
            _ImageInfo.IsScannedImage = false;
            _ImageInfo.BinFactor = _ImageChannel.BinningMode;
            _ImageInfo.GrayChannel.Exposure = _ImageChannel.Exposure;
            _ImageInfo.GainValue = _ImageChannel.AdGain;

            if (CompletionEstimate != null)
            {
                CompletionEstimate(this, dateTime, estCaptureTime); //
            }

            if (CommandStatus != null)
            {
                CommandStatus(this, "Capturing image....");
            }

            try
            {
                if (_ActiveCamera.SetExpoTime(ExposureTime))
                {
                    _ActiveCamera.CapturesImage(ref captureimage);
                    if (captureimage != null)
                    {
                        if (captureimage.CanFreeze) { captureimage.Freeze(); }
                    }
                }
                if (CommandStatus != null)
                {
                    CommandStatus(this, string.Empty);
                }
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
                    throw new Exception("Image capture error.", cex);
                }
            }
            catch (Exception ex)
            {
                if (!_IsCommandAborted)
                {
                    _ActiveCamera.ChangeTriggerMode(0);
                }
                throw new Exception("Image capture error.", ex);
            }
            finally
            {

            }
        }
        public override void Finish()
        {
            if (CommandStatus != null)
            {
                CommandStatus(this, string.Empty);
            }
        }
        public override void AbortWork()
        {
            _IsCommandAborted = true;
            try
            {
                //停止捕获
                _ActiveCamera.ChangeTriggerMode(0);
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public class CameraParameterStruct
        {
            public uint ExposureTime;  //um
            public int Bin;
            public int Gain;
            public int Width;
            public int Height;
        }
    }
}
