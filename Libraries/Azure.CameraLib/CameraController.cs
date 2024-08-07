﻿using Azure.Image.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Azure.CameraLib
{
    public class CameraController
    {
        #region Fields
        public Toupcam cam_ = null;
        private Thread timer_ = null;
        private double _CcdTemp = 0;
        private double _USConvertMS = 1000;
        private bool _IsCameraConnected = false;
        private decimal _ExposureTime_MIN;
        private decimal _ExposureTime_Max;
        private int _CaptureImage_Width = 0;
        private int __CaptureImage_Height = 0;
        private int _Width = 0;
        private int _Height = 0;
        private int _Left = 0;
        private int _Top = 0;
        private bool SingeCapture = false;
        public bool LiveCapture = false;
        WriteableBitmap temp = null;
        Toupcam.FrameInfoV3 info;
        ushort[] pixelData = null;
        public delegate void ImageReceivedHandler(WriteableBitmap dis);
        public event ImageReceivedHandler LiveImageReceived;
        int _currentMode = 0;
        #endregion

        #region Properties
        public enum Binning
        {
            Not_Binning = 0x01,

            Fusion_Binning2x2 = 0x02,
            Fusion_Binning3x3 = 0x03,
            Fusion_Binning4x4 = 0x04,

            Average_Binning2x2 = 0x82,
            Average_Binning3x3 = 0x83,
            Average_Binning4x4 = 0x84,



        }
        public double CcdTemp
        {
            get { return _CcdTemp; }
            set
            {
                if (_CcdTemp != value)
                {
                    _CcdTemp = value;
                }
            }

        }
        public bool IsCameraConnected
        {
            get { return _IsCameraConnected; }
            set
            {
                if (_IsCameraConnected != value)
                {
                    _IsCameraConnected = value;
                }
            }
        }
        public double USConvertMS { get => _USConvertMS; set => _USConvertMS = value; }
        public decimal ExposureTime_MIN { get => _ExposureTime_MIN; set => _ExposureTime_MIN = value; }
        public decimal ExposureTime_Max { get => _ExposureTime_Max; set => _ExposureTime_Max = value; }
        public int Width { get => _Width; set => _Width = value; }
        public int Height { get => _Height; set => _Height = value; }
        public int Left { get => _Left; set => _Left = value; }
        public int Top { get => _Top; set => _Top = value; }
        public int CaptureImage_Width { get => _CaptureImage_Width; set => _CaptureImage_Width = value; }
        public int CaptureImage_Height { get => __CaptureImage_Height; set => __CaptureImage_Height = value; }
        #endregion
        public CameraController()
        {
            Toupcam.GigeEnable(null);
            timer_ = new Thread(TECtempeatureMethod);
            timer_.IsBackground = true;
            timer_.Start();
        }
        void TECtempeatureMethod()
        {
            while (true)
            {
                if (cam_ != null)
                {
                    //Retrieve the current temperature of TEC once per second
                    short nTemperature;
                    if (cam_.get_Temperature(out nTemperature))
                        CcdTemp = nTemperature / 10.0;
                    else
                        CcdTemp = 0;
                }
                Thread.Sleep(1000);
            }
        }
        ~CameraController()
        {
            CloseCamera();
        }
        public void CloseCamera()
        {
            cam_?.Close();
            cam_ = null;
            IsCameraConnected = false;
        }
        /// <summary>
        /// 初始化相机 Initialize camera
        /// </summary>
        public bool Initialize()
        {
            IsCameraConnected = false;
            if (cam_ != null)
            {
                CloseCamera();
                return false;
            }
            Toupcam.DeviceV2[] arr = Toupcam.EnumV2();
            if (arr.Length <= 0)
            {
                //MessageBox.Show("No camera found.");
                return false;
            }
            else if (1 == arr.Length)
                return startDevice(arr[0].id);
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camId"></param>
        private bool startDevice(string camId)
        {
            cam_ = Toupcam.Open(camId);
            if (cam_ != null)
            {
                //Turn off the automatic exposure function inside the camera, 
                cam_?.put_AutoExpoEnable(false);

                //The exposure time range supported by the camera, in microseconds.
                decimal min = 0, max = 0;
                InitExpoTime(out min, out max);
                ExposureTime_MIN = min;
                ExposureTime_Max = max;

                //ROI
                uint xOffset = 0, yOffset = 0, width = 0, height = 0;
                if (cam_.get_Roi(out xOffset, out yOffset, out width, out height))
                {
                    Left = (int)xOffset;
                    Top = (int)yOffset;
                    CaptureImage_Width = Width = (int)width;
                    CaptureImage_Height = Height = (int)height;

                }
                info = new Toupcam.FrameInfoV3();
                //// Live Mode,
                //cam_.put_Option(Toupcam.eOPTION.OPTION_TRIGGER, 0);
                //TEC 1=on,0=off。 
                cam_.put_Option(Toupcam.eOPTION.OPTION_TEC, 1);
                IsCameraConnected = true;
                SetCCDTemp(-10);//-10
                ChangeCaptureMode(1);
                //if (!cam_.StartPullModeWithCallback(new Toupcam.DelegateEventCallback(DelegateOnEventCallback))) 
                //    return false;
                return true;
            }
            return false;
        }
        private void DelegateOnEventCallback(Toupcam.eEVENT evt)
        {
            if (cam_ != null)
            {
                switch (evt)
                {
                    case Toupcam.eEVENT.EVENT_ERROR:
                        OnEventError();
                        break;
                    case Toupcam.eEVENT.EVENT_DISCONNECTED:
                        OnEventDisconnected();
                        break;
                    case Toupcam.eEVENT.EVENT_EXPOSURE:
                        OnEventExposure();
                        break;
                    case Toupcam.eEVENT.EVENT_IMAGE:
                        if (_currentMode == 1)
                            OnEventImage();
                        else
                            LiveEventimage();
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// 出现异常错误
        /// </summary>
        private void OnEventError()
        {
            cam_.Close();
            cam_ = null;
            IsCameraConnected = false;
            MessageBox.Show("Generic error.");
        }

        /// <summary>
        /// 相机断开连接
        /// </summary>
        private void OnEventDisconnected()
        {
            cam_.Close();
            cam_ = null;
            IsCameraConnected = false;
            //MessageBox.Show("Camera disconnect.");
        }

        /// <summary>
        /// 获取相机设置的曝光时间（微秒） Obtain the exposure time set by the current camera (Microsecond)
        /// </summary>
        private void OnEventExposure()
        {
            uint nTime = 0;
            if (cam_.get_ExpoTime(out nTime))
            {
                //ExposureTime = nTime / MSConvertS;
            }
        }
        /// <summary>
        /// Triggered when the camera exposure completes an image
        /// </summary>
        private unsafe void OnEventImage()
        {
            temp = new WriteableBitmap(CaptureImage_Width, CaptureImage_Height, 0, 0, PixelFormats.Gray16, null);
            bool bOK = false;
            try
            {
                temp.Lock();
                bOK = cam_.PullImageV3(temp.BackBuffer, 0, 16, temp.BackBufferStride, out info); // check the return value
                temp.AddDirtyRect(new Int32Rect(0, 0, temp.PixelWidth, temp.PixelHeight));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                temp.Unlock();
                if (SingeCapture)
                {
                    int stride = (temp.PixelWidth * temp.Format.BitsPerPixel + 7) / 8;
                    pixelData = new ushort[temp.PixelHeight * stride];
                    temp.CopyPixels(pixelData, stride, 0);
                    SingeCapture = false;
                }
            }
        }

        private void LiveEventimage()
        {
            temp = new WriteableBitmap(CaptureImage_Width, CaptureImage_Height, 0, 0, PixelFormats.Bgr32, null);
            bool bOK = false;
            try
            {
                temp.Lock();
                bOK = cam_.PullImageV3(temp.BackBuffer, 0, 32, temp.BackBufferStride, out info); // check the return value
                temp.AddDirtyRect(new Int32Rect(0, 0, temp.PixelWidth, temp.PixelHeight));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            finally
            {
                temp.Unlock();
                if (LiveCapture && bOK)
                {
                    if (temp != null)
                    {
                        if (temp.CanFreeze)
                        {
                            temp.Freeze();
                        }
                    }
                    LiveImageReceived?.Invoke(temp);
                }
            }
        }
        /// <summary>
        /// 相机支持的曝光时间范围(微秒） The exposure time range supported by the camera (Microsecond)
        /// </summary>
        public bool InitExpoTime(out decimal ExposureTime_MIN, out decimal ExposureTime_Max)
        {
            ExposureTime_MIN = 0;
            ExposureTime_Max = 0;
            if (cam_ == null)
                return false;

            uint nMin = 0, nMax = 0, nDef = 0;
            if (cam_.get_ExpTimeRange(out nMin, out nMax, out nDef))
            {
                ExposureTime_MIN = (decimal)(nMin / USConvertMS);
                ExposureTime_Max = (decimal)(nMax / USConvertMS);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Gain 100=1x.200=2x ...
        /// </summary>
        /// <param name="gain">100-5000</param>
        /// <returns></returns>
        public bool SetGain(int gain)
        {
            if (cam_ != null)
            {
                return cam_.put_ExpoAGain((ushort)gain);
            }
            return false;
        }

        public bool SetBinning(Binning binning)
        {
            if (cam_ != null)
            {
                int _bin = (int)binning;
                bool result = cam_.put_Option(Toupcam.eOPTION.OPTION_BINNING, _bin);
                return result;
            }
            return false;
        }

        public bool GetRoi()
        {
            uint xOffset = 0, yOffset = 0, width = 0, height = 0;
            if (cam_.get_Roi(out xOffset, out yOffset, out width, out height))
            {
                Left = (int)xOffset;
                Top = (int)yOffset;
                Width = (int)width;
                Height = (int)height;
                return true;
            }
            return false;
        }

        public bool SetRoi(uint Left, uint Top, uint Width, uint Height)
        {
            if (cam_.put_Roi((uint)Left, (uint)Top, (uint)Width, (uint)Height))
            {
                return true;
            }
            return false;
        }
        public bool SetCCDTemp(double temp)
        {
            if (cam_.put_Option(Toupcam.eOPTION.OPTION_TECTARGET, Convert.ToInt32(temp) * 10))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// ExposureTime
        /// </summary>
        /// <param name="time">um</param>
        /// <returns></returns>
        public bool SetExpoTime(uint time)
        {
            if (cam_.put_ExpoTime(time))
            {
                return true;
            }
            return false;
        }
        //1=Capture one image, 0xffff=Loop capture, 0=Stop capture, triggering mode is only effective
        public bool ChangeTriggerMode(ushort trigger) 
        {
            if (cam_.Trigger(trigger))
            {
                return true;
            }
            return false;
        }
        public unsafe bool CapturesImage(ref WriteableBitmap capturedImage)
        {
            //Change the camera to trigger mode, where it can capture an image by sending a signal.
            if (!ChangeCaptureMode(1))
                return false;
            int currentMode = 0;
            cam_.get_Option(Toupcam.eOPTION.OPTION_TRIGGER, out currentMode);
            //Capture an image.
            if (ChangeTriggerMode(1))
            {
                SingeCapture = true;
                while (SingeCapture == true)//wait
                {
                    Thread.Sleep(1);
                }
                //14bit to 16bit
                fixed (ushort* ptr = pixelData)
                {
                    for (int i = 0; i < pixelData.Length; i++)
                    {
                        *(ptr + i) = (ushort)((*(ptr + i)) << 2);
                    }
                }
                WriteableBitmap temp;
                ImageProcessing.FrameToBitmap(out temp, pixelData, CaptureImage_Width, CaptureImage_Height);
                capturedImage = temp;
                return true;
            }
            return false;
        }
        public unsafe bool ChangeCaptureMode(int mode)
        {
            //0=Live mode,1=trigger mode
            int currentMode = 0;
            _currentMode = mode;
            cam_.get_Option(Toupcam.eOPTION.OPTION_TRIGGER, out currentMode);
            if (mode != currentMode)
            {
                if(!cam_.put_Option(Toupcam.eOPTION.OPTION_TRIGGER, mode))
                    return false;
            }
            if (mode == 0 && currentMode != 0) //Live
            {
                if (!cam_.Stop())
                    return false;
                ///*.
                //  0 = 8bit.
                //  1 = 14bit
                if (!cam_.put_Option(Toupcam.eOPTION.OPTION_BITDEPTH, 0))
                    return false;
                //(bitdepth)
                /* 0 = RGB24
                   1 = RGB48
                   2 = RGB32
                   3 = 8bit Gray
                   4 = 16bit Gray
                   5 = RGB64 */
                if (!cam_.put_Option(Toupcam.eOPTION.OPTION_RGB, 2))
                    return false;
                if (!cam_.StartPullModeWithCallback(new Toupcam.DelegateEventCallback(DelegateOnEventCallback)))
                    return false;
            }
            else if (mode == 1 && currentMode != 1)//trigger
            {
                if (!cam_.Stop())
                    return false;
                ///*.
                //  0 = 8bit.
                //  1 = 14bit
                if (!cam_.put_Option(Toupcam.eOPTION.OPTION_BITDEPTH, 1))
                    return false;
                //(bitdepth)
                /* 0 = RGB24
                   1 = RGB48
                   2 = RGB32
                   3 = 8bit Gray
                   4 = 16bit Gray
                   5 = RGB64 */
                if (!cam_.put_Option(Toupcam.eOPTION.OPTION_RGB, 4))
                    return false;
                if (!cam_.StartPullModeWithCallback(new Toupcam.DelegateEventCallback(DelegateOnEventCallback))) 
                    return false;
            }
            return true;
        }
    }
}
