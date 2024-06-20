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
        private double _USConvertMS = 1000; //微秒转毫秒
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
        WriteableBitmap temp = null;
        ushort[] pixelData = null;
        private double _Scalefactor = 0.875;//这个相机是14位的，因此我们在使用16位的算法时需要用到这个系数
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
        public double Scalefactor { get => _Scalefactor; set => _Scalefactor = value; }
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
                    //1秒获取一次TEC当前的温度
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
                return false;
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
        /// 打开相机
        /// </summary>
        /// <param name="camId">实例地址</param>
        private bool startDevice(string camId)
        {
            cam_ = Toupcam.Open(camId);//相机实例句柄
            if (cam_ != null)
            {
                //关闭相机内部的自动曝光功能
                cam_?.put_AutoExpoEnable(false);

                //获取相机的曝光时间范围,如果不在这个范围内，将无法捕获到图像，
                decimal min = 0, max = 0;
                InitExpoTime(out min, out max); 
                ExposureTime_MIN = min;
                ExposureTime_Max = max;

                //获取相机的ROI
                uint xOffset = 0, yOffset = 0, width = 0, height = 0;
                if (cam_.get_Roi(out xOffset, out yOffset, out width, out height))
                {
                    Left = (int)xOffset;
                    Top = (int)yOffset;
                    CaptureImage_Width = Width = (int)width;
                    CaptureImage_Height = Height = (int)height;

                }

                /*.
                0 = 表示使用8Bits位深度.
                1 = 表示使用本相机支持的最高位深度*/
                cam_.put_Option(Toupcam.eOPTION.OPTION_BITDEPTH, 1);

                //相机支持的最大位深度(bitdepth)

                /* 0 = 使用RGB24
                   1 = 在位深度 > 8时, 启用RGB48格式
                   2 = 使用RGB32
                   3 = 8位灰度(只对黑白相机有效)
                   4 = 16位灰度(只对黑白相机并且位深度 > 8时有效)
                   5 = 在位深度 > 8时, 启用RGB64格式 */
                cam_.put_Option(Toupcam.eOPTION.OPTION_RGB, 4);

                //触发模式，这个模式下可以控制相机拍摄的图片张数，比如1张，或者一直循环。
                cam_.put_Option(Toupcam.eOPTION.OPTION_TRIGGER, 1);
                //TEC 1=启动TEC,0=关闭TEC。 
                cam_.put_Option(Toupcam.eOPTION.OPTION_TEC, 1);
                IsCameraConnected = true;
                SetCCDTemp(-10);//设置-10
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
                        OnEventImage();
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
            MessageBox.Show("Camera disconnect.");
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
        /// 捕获完成图像后
        /// </summary>
        private unsafe void OnEventImage()
        {
            temp = new WriteableBitmap(CaptureImage_Width, CaptureImage_Height, 0, 0, PixelFormats.Gray16, null);
            Toupcam.FrameInfoV3 info = new Toupcam.FrameInfoV3();
            bool bOK = false;
            int bits = 16;
            try
            {
                temp.Lock();
                try
                {
                    bOK = cam_.PullImageV3(temp.BackBuffer, 0, bits, temp.BackBufferStride, out info); // check the return value
                    temp.AddDirtyRect(new Int32Rect(0, 0, temp.PixelWidth, temp.PixelHeight));
                }
                finally
                {
                    temp.Unlock();
                    if (SingeCapture)//完成了图像捕获
                    {
                        int stride = (temp.PixelWidth * temp.Format.BitsPerPixel + 7) / 8;
                        pixelData = new ushort[temp.PixelHeight * stride];
                        temp.CopyPixels(pixelData, stride, 0);
                        SingeCapture = false;//停止捕获
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            // temp = null;
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
        /// 增益
        /// </summary>
        /// <param name="gain">100，200，300，400，500</param>
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
                bool result= cam_.put_Option(Toupcam.eOPTION.OPTION_BINNING, _bin);
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
        public bool ChangeTriggerMode(ushort trigger)  //1=捕获一张,0xffff=循环捕获，0=停止捕获, 必须在触发模式这三个参数才有效
        {
            if (cam_.Trigger(trigger))
            {
                return true;
            }
            return false;
        }
        public unsafe bool CapturesImage(ref WriteableBitmap capturedImage)
        {
            cam_.Stop();//Stop后可以重新定义DelegateOnEventCallback代理函数
            if (!cam_.StartPullModeWithCallback(new Toupcam.DelegateEventCallback(DelegateOnEventCallback)))  //代理函数
                return false;
            if (ChangeTriggerMode(1))//捕获一张
            {
                SingeCapture = true;
                while (SingeCapture == true)//等待图像捕获完成
                {
                    Thread.Sleep(1);
                }
                PixelFormat format = PixelFormats.Gray16;
                int stride = (CaptureImage_Width * format.BitsPerPixel + 7) / 8;
                BitmapSource bitmapSource = BitmapSource.Create(CaptureImage_Width, CaptureImage_Height, 96, 96, format, null, pixelData, stride);
                capturedImage = new WriteableBitmap(bitmapSource);

                //ushort* Caputrebuff = (ushort*)capturedImage.BackBuffer.ToPointer();
                //ushort* tempbuff = (ushort*)temp.BackBuffer.ToPointer();
                //int index = 0;
                //for (int x = 0; x < Width; x++)
                //{
                //    for (int y = 0; y < Height; y++)
                //    {
                //        index = x + y * temp.BackBufferStride / 2;
                //        Caputrebuff[index] = tempbuff[index];
                //    }

                //}
                return true;
            }
            return false;
        }

    }

}
