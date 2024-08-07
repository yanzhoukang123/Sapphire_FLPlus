using Azure.CameraLib;
using Azure.CommandLib;
using Azure.Configuration.Settings;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.ScannerEUI.ViewModel;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Azure.CameraLib.CameraController;

namespace Azure.ScannerEUI.SystemCommand
{
    class ChemiSOLOCaptureCommand : ThreadBase
    {
        // Image capture status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image capture status event
        public event CommandStatusHandler CommandStatus;
        // Image capture completion time estimate delegate
        public delegate void CommandCompletionEstHandler(ThreadBase sender, DateTime dateTime, double estTime);
        // Image capture completion time estimate event
        public event CommandCompletionEstHandler CompletionEstimate;
        private ImageInfo _ImageInfo = null;
        private string imagename = "";
        private bool _IsCommandAborted = false;
        private Dispatcher _CallingDispatcher = null;
        private ImageChannelSettings _ImageChannel = null;
        private CameraController _ActiveCamera = null;
        private WriteableBitmap captureimage = null;
        private ChemiSOLOParameterStruct _Parameter = null;
        private string _FlatCorrection = string.Empty;
        // Image received delegate
        public delegate void ImageReceivedHandler(WriteableBitmap displayBitmap, ImageInfo imageInfo, string ImageName);
        public event ImageReceivedHandler ImageReceived;
        private EthernetController _CommController = null;
        private string SampleType = "";
        double RedChannelExposure = 0;
        double GreenChannelExposure = 0;
        double BlueChannelExposure = 0;
        double GrayChannelExposure = 0;
        WriteableBitmap MaskImage = null;
        private DateTime StartTime;
        private DateTime CompletedTime;
        ImageStatistics imageStat = new ImageStatistics();
        ImageArithmetic imageArith = new ImageArithmetic();
        public ChemiSOLOCaptureCommand(Dispatcher callingDispatcher,
                          EthernetController ethernet,
                          CameraController camera,
                          ChemiSOLOParameterStruct Parameter)
        {
            _CallingDispatcher = callingDispatcher;
            this._CommController = ethernet;
            _ActiveCamera = camera;
            _Parameter = Parameter;
        }
        public ImageInfo ImageInfo
        {
            get { return _ImageInfo; }
        }
        public WriteableBitmap CaptureImage
        {
            get => captureimage;
            set => captureimage = value;
        }
        public string ImageName
        {
            get => imagename;
            set => imagename = value;
        }


        public unsafe override void ThreadFunction()
        {
            try
            {
                WriteableBitmap GrayChannel = null;
                WriteableBitmap RedChannel = null;
                WriteableBitmap GreenChannel = null;
                WriteableBitmap BlueChannel = null;
                WriteableBitmap WhiteChannel = null;
                CommandStatus?.Invoke(this, "Preparing to capture....");
                _ActiveCamera.SetBinning(Binning.Not_Binning); //每次拍摄图像时都设置Bin是1x1
                if (_Parameter.chemiApplicationType == ChemiApplicationType.Chemi_Imaging)
                {
                    StartTime = DateTime.Now;
                    if (_Parameter.chemiMarkerType == ChemiMarkerType.None)
                    {
                        Workspace.This.LogMessage("*****************Executing command ChemiBlot_NoMarker **********************");
                        ChemiImaging(out GrayChannel, 0);//None
                    }
                    else if (GrayChannel != null && _Parameter.chemiMarkerType == ChemiMarkerType.TureColor)
                    {
                        Workspace.This.LogMessage("*****************Executing command ChemiBlot_TrueColorMarker **********************");
                        string captrue_type = "Chemi_Imaging";
                        int bit = _Parameter.bit;
                        ImageInfo _imageInfo = new ImageInfo();
                        _imageInfo.GainValue = _Parameter.rgbimagegain;
                        _imageInfo.BinFactor = _Parameter.pixelbin;
                        DateTime dateTime = DateTime.Now;
                        _imageInfo.DateTime = dateTime.ToString();
                        _imageInfo.Calibration = "Bias/Flat";
                        _imageInfo.CaptureType = captrue_type;
                        _imageInfo.EdrBitDepth = bit;
                        _imageInfo.DynamicBit = bit;

                        CommandStatus?.Invoke(this, "Capture BlotFinding Image ....");
                        Workspace.This.LogMessage("=============Perform blot finding================");
                        BlotFinding();
                        CommandStatus?.Invoke(this, "Capture Red Channel Image ....");
                        RedChannel = TrueColorImaging(1); //R
                        CommandStatus?.Invoke(this, "Capture Green Channel Image ....");
                        GreenChannel = TrueColorImaging(2); //G
                        CommandStatus?.Invoke(this, "Capture Blue Channel Image ....");
                        BlueChannel = TrueColorImaging(3); //B
                        _imageInfo.RedChannel.Exposure = RedChannelExposure;
                        _imageInfo.GreenChannel.Exposure = GreenChannelExposure;
                        _imageInfo.BlueChannel.Exposure = BlueChannelExposure;
                        _imageInfo.GrayChannel.Exposure = GrayChannelExposure;
                        WriteableBitmap _image = ImageProcessing.SetChannel(RedChannel, GreenChannel, BlueChannel, GrayChannel);
                        if (_image.CanFreeze)
                        {
                            _image.Freeze();
                        }
                        Workspace.This.LogMessage("Write ImageInfo");
                        ImageReceived(_image, _imageInfo, _Parameter.name + "_Marker.tif");
                    }
                    else if (GrayChannel != null && _Parameter.chemiMarkerType == ChemiMarkerType.Grayscale)
                    {
                        Workspace.This.LogMessage("*****************Executing command ChemiBlot_GrayscaleMarker **********************");
                        string captrue_type = "Chemi_Imaging";
                        int bit = _Parameter.bit;
                        ImageInfo _imageInfo = new ImageInfo();
                        _imageInfo.GainValue = _Parameter.rgbimagegain;
                        _imageInfo.BinFactor = _Parameter.pixelbin;
                        _imageInfo.IsRedChannelAvail = true;
                        _imageInfo.IsGreenChannelAvail = true;
                        _imageInfo.IsMultipleGrayscaleChannels = true;
                        _imageInfo.GreenChannel.ColorChannel = ImageChannelType.Gray;
                        _imageInfo.RedChannel.ColorChannel = ImageChannelType.Gray;
                        DateTime dateTime = DateTime.Now;
                        _imageInfo.DateTime = dateTime.ToString();
                        _imageInfo.Calibration = "Bias/Flat";
                        _imageInfo.CaptureType = captrue_type;
                        _imageInfo.EdrBitDepth = bit;
                        _imageInfo.DynamicBit = bit;
                        CommandStatus?.Invoke(this, "Capture Gray Image....");
                        WhiteChannel = GrayscaleImaging(4);//White
                        _imageInfo.RedChannel.Exposure = GrayChannelExposure;
                        _imageInfo.GreenChannel.Exposure = RedChannelExposure;
                        WriteableBitmap _BChannel = new WriteableBitmap(WhiteChannel.PixelWidth, WhiteChannel.PixelHeight, 0, 0, PixelFormats.Gray16, null);
                        WriteableBitmap _image = ImageProcessing.SetChannel(GrayChannel, WhiteChannel, _BChannel);
                        if (_image.CanFreeze)
                        {
                            _image.Freeze();
                        }
                        Workspace.This.LogMessage("Write ImageInfo");
                        ImageReceived(_image, _imageInfo, _Parameter.name + "_Marker.tif");
                    }
                    CompletedTime = DateTime.Now;

                    // 计算时间差
                    TimeSpan timeDifference = CompletedTime - StartTime;
                    string formattedTimeDifference = $"{(int)timeDifference.TotalHours}:{timeDifference.Minutes:D2}:{timeDifference.Seconds:D2}.{timeDifference.Milliseconds:D3}{timeDifference.Ticks % TimeSpan.TicksPerMillisecond / 10:D3}";
                    Workspace.This.LogMessage("Start:      " + StartTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Completed:  " + CompletedTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Elapsed:    " + formattedTimeDifference);
                }
                else if (_Parameter.chemiApplicationType == ChemiApplicationType.TrueColor_Imaging)
                {
                    StartTime = DateTime.Now;
                    Workspace.This.LogMessage("*****************Executing command TrueColor **********************");
                    string captrue_type = "TrueColor_Imaging";
                    int bit = _Parameter.bit;
                    ImageInfo _imageInfo = new ImageInfo();
                    _imageInfo.GainValue = _Parameter.rgbimagegain;
                    _imageInfo.BinFactor = _Parameter.pixelbin;
                    DateTime dateTime = DateTime.Now;
                    _imageInfo.DateTime = dateTime.ToString();
                    _imageInfo.Calibration = "Bias/Flat";
                    _imageInfo.CaptureType = captrue_type;
                    _imageInfo.EdrBitDepth = bit;
                    _imageInfo.DynamicBit = bit;

                    CommandStatus?.Invoke(this, "Capture BlotFinding Image ....");
                    Workspace.This.LogMessage("=============Perform blot finding================");
                    BlotFinding();

                    CommandStatus?.Invoke(this, "Capture Red Channel Image ....");
                    RedChannel = TrueColorImaging(1); //R
                    CommandStatus?.Invoke(this, "Capture Green Channel Image ....");
                    GreenChannel = TrueColorImaging(2); //G
                    CommandStatus?.Invoke(this, "Capture Blue Channel Image ....");
                    BlueChannel = TrueColorImaging(3); //B
                    _imageInfo.RedChannel.Exposure = RedChannelExposure;
                    _imageInfo.GreenChannel.Exposure = GreenChannelExposure;
                    _imageInfo.BlueChannel.Exposure = BlueChannelExposure;
                    WriteableBitmap _image = ImageProcessing.SetChannel(RedChannel, GreenChannel, BlueChannel);
                    if (_image.CanFreeze)
                    {
                        _image.Freeze();
                    }
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(_image, _imageInfo, _Parameter.name + "_RGB.tif");
                    CompletedTime = DateTime.Now;

                    // 计算时间差
                    TimeSpan timeDifference = CompletedTime - StartTime;
                    string formattedTimeDifference = $"{(int)timeDifference.TotalHours}:{timeDifference.Minutes:D2}:{timeDifference.Seconds:D2}.{timeDifference.Milliseconds:D3}{timeDifference.Ticks % TimeSpan.TicksPerMillisecond / 10:D3}";
                    Workspace.This.LogMessage("Start:      " + StartTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Completed:  " + CompletedTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Elapsed:    " + formattedTimeDifference);
                }
                else if (_Parameter.chemiApplicationType == ChemiApplicationType.Grayscale_Imaging)
                {
                    StartTime = DateTime.Now;
                    Workspace.This.LogMessage("*****************Executing command Grayscale **********************");
                    string captrue_type = "Grayscale_Imaging";
                    int bit = _Parameter.bit;
                    ImageInfo _imageInfo = new ImageInfo();
                    _imageInfo.GainValue = _Parameter.rgbimagegain;
                    _imageInfo.BinFactor = _Parameter.pixelbin;
                    DateTime dateTime = DateTime.Now;
                    _imageInfo.DateTime = dateTime.ToString();
                    _imageInfo.Calibration = "Bias/Flat";
                    _imageInfo.CaptureType = captrue_type;
                    _imageInfo.EdrBitDepth = bit;
                    _imageInfo.DynamicBit = bit;
                    CommandStatus?.Invoke(this, "Capture Gray Image ....");
                    WhiteChannel = GrayscaleImaging(4);//White
                    _imageInfo.RedChannel.Exposure = RedChannelExposure;
                    if (WhiteChannel.CanFreeze)
                    {
                        WhiteChannel.Freeze();
                    }
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(WhiteChannel, _imageInfo, _Parameter.name + "_Gray.tif");
                    CompletedTime = DateTime.Now;

                    // 计算时间差
                    TimeSpan timeDifference = CompletedTime - StartTime;
                    string formattedTimeDifference = $"{(int)timeDifference.TotalHours}:{timeDifference.Minutes:D2}:{timeDifference.Seconds:D2}.{timeDifference.Milliseconds:D3}{timeDifference.Ticks % TimeSpan.TicksPerMillisecond / 10:D3}";
                    Workspace.This.LogMessage("Start:      " + StartTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Completed:  " + CompletedTime.ToString("HH:mm:ss.fff"));
                    Workspace.This.LogMessage("Elapsed:    " + formattedTimeDifference);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                CommandStatus?.Invoke(this, string.Empty);
                Workspace.This.LogMessage("\n\n\n\n\n\n");
                // don't throw exception if the user abort the process.
            }
            catch (System.Runtime.InteropServices.SEHException)
            {
                CommandStatus?.Invoke(this, string.Empty);
                Workspace.This.LogMessage("\n\n\n\n\n\n");
                // The SEHException class handles SEH errors that are thrown from unmanaged code,
                // but have not been mapped to another .NET Framework exception.

                throw new OutOfMemoryException();
            }
            catch (System.Runtime.InteropServices.COMException cex)
            {
                CommandStatus?.Invoke(this, string.Empty);
                Workspace.This.LogMessage("\n\n\n\n\n\n");
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
                Workspace.This.LogMessage("\n\n\n\n\n\n");
                CommandStatus?.Invoke(this, string.Empty);
                if (!_IsCommandAborted)
                {
                    _ActiveCamera.ChangeTriggerMode(0);
                }
                if (ex.Message == "AE ERROR: Signal too weak!")
                {
                    throw new Exception("AE ERROR: Signal too weak!", ex);
                }
                else
                {
                    throw new Exception("Image capture error.", ex);
                }

            }
            finally
            {
                Workspace.This.LogMessage("\n\n\n\n\n\n");
                CommandStatus?.Invoke(this, string.Empty);
            }
        }

        #region Chemi
        private unsafe void ChemiImaging(out WriteableBitmap writeableBitmap,int LightCode)
        {
            Workspace.This.LogMessage("=================Image Acquisition===================");
            writeableBitmap = null;
            //LED None
            Workspace.This.EthernetController.SetRGBLightRegisterControl(LightCode);
            //Set Gain
            _ActiveCamera.SetGain(_Parameter.chemiimagegain);

            #region Parameter
            _ImageChannel = new ImageChannelSettings();
            _ImageChannel.AutoExposureUpperCeiling = _Parameter.upperCeiling;
            _ImageChannel.MaxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
            _ImageChannel.LightType = LightCode;  //None
            _ImageChannel.BinningMode = _Parameter.pixelbin;
            _ImageChannel.InitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.InitialExposureTime;//RGB光源下，获取自动曝光时间里的初始值
            _ImageChannel.ChemiInitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.ChemiInitialExposureTime;//RGB光源下，获取自动曝光时间里的初始值
            bool Chemi_NewAlgo_Enable = _Parameter.Chemi_NewAlgo_Enable;
            double Chemi_T1 = _Parameter.Chemi_T1;
            string Chemi_binning_Kxk = _Parameter.Chemi_binning_Kxk;
            int Chemi_Intensity = _Parameter.Chemi_Intensity;
            int bit = _Parameter.bit;
            int loopNumber = 0;
            uint exposuretime = _Parameter.exposureTime;
            #endregion
            Workspace.This.LogMessage("Binning Mode            =:" + _Parameter.pixelbin);
            Workspace.This.LogMessage("Gain                    =:" + _Parameter.chemiimagegain);
            #region Get AutoExposureTime
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS; //秒转微秒
            double _exposuretime = 0;
            Workspace.This.LogMessage("***********Image capture frame settings**************");
            Workspace.This.LogMessage("LightSource             =:" + LightCode);
            Workspace.This.LogMessage("Calculating autoexposure");
            Workspace.This.LogMessage("CalcAuto: GrabImage - Exposure time =" + (_ImageChannel.ChemiInitialExposureTime * us) + " s");
            if (_Parameter.isAutoexposure)
            {
                //get autoExposureTime
                if (!Chemi_NewAlgo_Enable)
                    _exposuretime = ImagingSystemHelper.ChemiSOLO_CalculateImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel);
                else
                    _exposuretime = ImagingSystemHelper.ChemiSOLO_Calculate_New_Algo_ImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel, Chemi_T1, Chemi_binning_Kxk, Chemi_Intensity);
                exposuretime = (uint)(_exposuretime * us);
            }
            #endregion

            string captrue_type = "Chemi_Imaging";
            #region CaptureImage
            //Singe
            if (_Parameter.chemiModeType == ChemiModeType.Single)
            {
                if (bit == 16)
                {
                    ImageInfo _imageInfo = new ImageInfo();
                    _imageInfo.GainValue = _Parameter.chemiimagegain;
                    _imageInfo.BinFactor = _Parameter.pixelbin;
                    DateTime dateTime = DateTime.Now;
                    _imageInfo.DateTime = dateTime.ToString();
                    _imageInfo.Calibration = "Bias/Flat";
                    _imageInfo.CaptureType = captrue_type;
                    _imageInfo.EdrBitDepth = bit;
                    _imageInfo.DynamicBit = bit;
                    double Sec = exposuretime / us;
                    Workspace.This.LogMessage("Exposure time set to    =:" + Sec+"s");
                    Workspace.This.LogMessage("EdrBitDepth             =:" + bit);
                    Workspace.This.LogMessage("DynamicBit              =:" + 16);
                    GrayChannelExposure = Sec;
                    _imageInfo.RedChannel.Exposure = Sec;
                    CompletionEstimate?.Invoke(this, dateTime, Sec);
                    CommandStatus?.Invoke(this, "Capture Chemi Image....");
                    writeableBitmap = CaptureChemiImage(exposuretime, LightCode);
                    if (writeableBitmap.CanFreeze)
                    {
                        writeableBitmap.Freeze();
                    }
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(writeableBitmap, _imageInfo, _Parameter.name + ".tif");
                }
                else //EDR
                {
                    ImageInfo _imageInfo = new ImageInfo();
                    _imageInfo.GainValue = _Parameter.chemiimagegain;
                    _imageInfo.BinFactor = _Parameter.pixelbin;
                    _imageInfo.EdrBitDepth = bit;
                    _imageInfo.DynamicBit = 24;
                    _imageInfo.Calibration = "Bias/Flat";
                    _imageInfo.CaptureType = captrue_type;
                    DateTime dateTime = DateTime.Now;
                    _imageInfo.DateTime = dateTime.ToString();
                    int DynamicBit = 24;
                    WriteableBitmap l1;
                    List<WriteableBitmap> lImg = new List<WriteableBitmap>();
                    int N = 0;
                    if (bit == 18)
                    {
                        N = 4;
                    }
                    else if (bit == 20)
                    {
                        N = 16;
                    }
                    else if (bit == 22)
                    {
                        N = 64;
                    }
                    else if (bit == 24)
                    {
                        N = 256;
                    }
                    double Sec = exposuretime / us * N;
                    Workspace.This.LogMessage("Exposure time set to    =:" + Sec + "s");
                    Workspace.This.LogMessage("EdrBitDepth             =:" + bit);
                    Workspace.This.LogMessage("DynamicBit              =:" + 24);
                    Workspace.This.LogMessage("N                       =:" + N);
                    _imageInfo.RedChannel.Exposure = Sec;
                    CompletionEstimate?.Invoke(this, dateTime, Sec);
                    CommandStatus?.Invoke(this, "Capture EDR Chemi Image....");
                    double scale = (double)16 / (double)DynamicBit;
                    int roiWidth = _Parameter.width;
                    int roiHeight = _Parameter.height;
                    for (int i = 0; i < N; i++)
                    {
                        Workspace.This.LogMessage("=================Capture " + (i + 1) + "st image===================");
                        l1 = CaptureChemiImage(exposuretime, LightCode);
                        lImg.Add(l1);
                    }
                    Workspace.This.LogMessage("Compress the n bit I3 to a 16bit image I4 by Value_16bit=Value_nbit^(16/n)");
                    EDRImageCumulative(ref writeableBitmap, lImg, scale, _ActiveCamera.Width, _ActiveCamera.Height);
                    if (writeableBitmap.CanFreeze)
                    {
                        writeableBitmap.Freeze();
                    }
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(writeableBitmap, _imageInfo, _Parameter.name + ".tif");
                }
            }
            //Cumulative
            else if (_Parameter.chemiModeType == ChemiModeType.Cumulative)
            {
                WriteableBitmap fristImage = null;
                WriteableBitmap _image = null;
                ImageInfo _imageInfo = new ImageInfo();
                _imageInfo.GainValue = _Parameter.chemiimagegain;
                _imageInfo.BinFactor = _Parameter.pixelbin;
                _imageInfo.Calibration = "Bias/Flat";
                _imageInfo.CaptureType = captrue_type;
                _imageInfo.EdrBitDepth = bit;
                _imageInfo.DynamicBit = bit;
                double Sec = exposuretime / us;
                loopNumber = _Parameter.numFrames;
                for (int i = 0; i < loopNumber; i++)
                {
                    DateTime dateTime = DateTime.Now;
                    CompletionEstimate?.Invoke(this, dateTime, Sec);
                    CommandStatus?.Invoke(this, "Capture Chemi Image....");
                    double cumulativeSec = Sec * (i + 1);
                    Workspace.This.LogMessage("Exposure time set to    =:" + cumulativeSec + "s");
                    Workspace.This.LogMessage("EdrBitDepth             =:" + bit);
                    Workspace.This.LogMessage("DynamicBit              =:" + 16);
                    Workspace.This.LogMessage("=================Capture " + (i + 1) + "st image===================");
                    if (i == 0)
                    {
                        fristImage = CaptureChemiImage(exposuretime, LightCode);
                    }
                    else
                    {
                        _image = CaptureChemiImage(exposuretime, LightCode);
                        WriteableBitmap mutableBitmap = null;
                        mutableBitmap = fristImage.Clone();
                        fristImage = imageArith.AddImage(ref mutableBitmap, ref _image);
                    }
                    if (fristImage.CanFreeze)
                    {
                        fristImage.Freeze();
                    }
                    _imageInfo.RedChannel.Exposure = cumulativeSec;
                    _imageInfo.DateTime = dateTime.ToString();
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(fristImage, _imageInfo, _Parameter.name + "_F" + (i + 1) + ".tif");
                }
            }
            //Multiple
            else if (_Parameter.chemiModeType == ChemiModeType.Multiple)
            {
                ImageInfo _imageInfo = new ImageInfo();
                _imageInfo.GainValue = _Parameter.chemiimagegain;
                _imageInfo.BinFactor = _Parameter.pixelbin;
                _imageInfo.Calibration = "Bias/Flat";
                _imageInfo.CaptureType = captrue_type;
                _imageInfo.EdrBitDepth = bit;
                _imageInfo.DynamicBit = bit;
                List<uint> multipleExposureList = _Parameter.multipleExposureList;
                loopNumber = multipleExposureList.Count;
                for (int i = 0; i < loopNumber; i++)
                {
                    DateTime dateTime = DateTime.Now;
                    exposuretime = multipleExposureList[i];
                    double Sec = exposuretime / us;
                    CompletionEstimate?.Invoke(this, dateTime, Sec);
                    CommandStatus?.Invoke(this, "Capture Chemi Image....");
                    Workspace.This.LogMessage("Exposure time set to    =:" + Sec + "s");
                    Workspace.This.LogMessage("EdrBitDepth             =:" + bit);
                    Workspace.This.LogMessage("DynamicBit              =:" + 16);
                    Workspace.This.LogMessage("=================Capture " + (i + 1) + "st image===================");
                    WriteableBitmap _image = CaptureChemiImage(exposuretime,LightCode);
                    if (_image.CanFreeze)
                    {
                        _image.Freeze();
                    }
                    _imageInfo.RedChannel.Exposure = Sec;
                    _imageInfo.DateTime = dateTime.ToString();
                    Workspace.This.LogMessage("Write ImageInfo");
                    ImageReceived(_image, _imageInfo, _Parameter.name + "_F" + (i + 1) + ".tif");
                }
            }
            #endregion
        }

        private WriteableBitmap CaptureChemiImage(uint ExposureTime,int LightCode)
        {
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS; //秒转微秒
            Workspace.This.LogMessage("GrabImage Started  ");
            Workspace.This.LogMessage("Exposure time set to    =:" + ExposureTime* us + "s");
            WriteableBitmap biasImage = null;
            WriteableBitmap image = null;
            Mat _captureimage = null;
            Mat _dark = null;
            Mat _bias = null;
            double avg1 = 0.0, avg2 = 0.0, average = 0.0;
            System.Drawing.Point ptMax = new System.Drawing.Point();
            ImageStatistics imageStat = new ImageStatistics();
            //Acquire chemi image
            if (_ActiveCamera.SetExpoTime(ExposureTime))
            {
                _ActiveCamera.CapturesImage(ref image);
            }
            Workspace.This.LogMessage("GrabImage  cmpleted");
            Workspace.This.LogMessage("================Image correction applied=============");
            //Subtract bias master B1
            Workspace.This.LogMessage("Applying Subtract_bias_master");
            biasImage = Workspace.This.MasterLibrary.GetBiasImage(1);
            if (biasImage != null)
            {
                image = imageArith.SubtractImage(image, biasImage);
            }
            else
            {
                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                throw new Exception(strMessage);
            }
            _captureimage = imageArith.ConvertWriteableBitmapToMat(image);
            //Apply dark/glow correction if it is enabled and if the exposure is longer than 1s
            if (_Parameter.Dark_GlowCorrection && ExposureTime > 1000000)
            {
                Workspace.This.LogMessage("Applying Dark_DlowCorrection");
                _dark = Workspace.This.MasterLibrary.GetDarkMasterImage();
                _bias = Workspace.This.MasterLibrary.GetBiasImage();
                _captureimage = Workspace.This.MasterLibrary.ChemiSOLO_ApplyDark_GlowFun(_dark, _captureimage, _bias);
            }
            //Apply line correction if it is enabled
            if (_Parameter.LineCorrection)
            {
                //This calibration is not required
            }
            image = imageArith.ConvertMatToWriteableBitmap(_captureimage);
            //Apply despeckler if it is enabled
            if (_Parameter.DespecklerCorrection)
            {
                Workspace.This.LogMessage("Applying DespecklerCorrection");
                image = ImageProcessing.MedianFilter(image);
            }
            //Apply lens distortion correction if it is enabled
            if (_Parameter.LensDistortionCorrection)
            {
                if (_Parameter.paramB != 0.0)
                {
                    Workspace.This.LogMessage("Applying LensDistortionCorrection");
                    image = ImagingSystemHelper.ChemiSOLO_DistortionCorrection(image, _Parameter.paramA, _Parameter.paramB, _Parameter.paramC);
                }
            }
            //Apply flat field correction with chemi-flats U0 if it is enabled(need to use ptMax and meanI as input)

            if (_Parameter.FlatfieldCorrection)
            {
                Workspace.This.LogMessage("Applying FlatfieldCorrection");
                imageStat.GetImagePixelAverage5x5(image, ref avg1, ref ptMax); //avg1
                //Get the average intensity meanI of the 5x5 region around the max pixel location ptMax
                int roiWidth = 5;
                int roiHeight = 5;
                System.Drawing.Rectangle scalingRect = new System.Drawing.Rectangle((int)(ptMax.X - 2), (int)(ptMax.Y - 2), roiWidth, roiHeight);
                average = imageStat.GetAverage(image, scalingRect); //avg2


                image = Workspace.This.MasterLibrary.ChemiSOLO_CalculateFlatCorrectedImage(image, LightCode, out _FlatCorrection, scalingRect, average);
            }
            //Apply kxk binning for chemi mode if kxk (k=2,3,4) binning is selected
            if (_Parameter.pixelbin > 1) //k=2,3,4
            {
                Workspace.This.LogMessage("Apply kxk binning for chemi mode if kxk (k=2,3,4) binning is selected");
                image = imageArith.ConvertBinKxk(image, _Parameter.pixelbin);
            }
            return image;
        }
        #endregion

        #region TrueColor
        private unsafe WriteableBitmap TrueColorImaging(int LightCode)
        {
            if(LightCode==1)
                Workspace.This.LogMessage("===============================CalcAuto Red=================================");
            else if(LightCode == 2)
                Workspace.This.LogMessage("===============================CalcAuto Green===============================");
            else if (LightCode == 3)
                Workspace.This.LogMessage("===============================CalcAuto Blue================================");
            //LED
            Workspace.This.EthernetController.SetRGBLightRegisterControl(LightCode);
            //Set Gain
            _ActiveCamera.SetGain(_Parameter.rgbimagegain);
            Workspace.This.LogMessage("*****************CCD settings************************");
            Workspace.This.LogMessage("Binning Mode            =:" + _Parameter.pixelbin);
            Workspace.This.LogMessage("Gain                    =:" + _Parameter.rgbimagegain);
            Workspace.This.LogMessage("*****************************************************");
            #region Parameter
            _ImageChannel = new ImageChannelSettings();
            _ImageChannel.AutoExposureUpperCeiling = _Parameter.upperCeiling;
            _ImageChannel.MaxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
            _ImageChannel.LightType = LightCode;  //None
            _ImageChannel.BinningMode = _Parameter.pixelbin;
            _ImageChannel.InitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.InitialExposureTime;
            _ImageChannel.ChemiInitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.ChemiInitialExposureTime;
            //Settings
            uint exposuretime = _Parameter.exposureTime;
            #endregion
            Workspace.This.LogMessage("***********Image capture frame settings**************");
            Workspace.This.LogMessage("LightSource             =:"+ LightCode);
            Workspace.This.LogMessage("Calculating autoexposure");
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;  //s to us
            Workspace.This.LogMessage("CalcAuto: GrabImage - Exposure time =" + (_ImageChannel.InitialExposureTime * us) + " s");
            #region Get AutoExposureTime
            double _exposuretime = 0;
            //get autoExposureTime
            _exposuretime = ImagingSystemHelper.ChemiSOLO_CalculateImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel);
            exposuretime = (uint)(_exposuretime * us);
            #endregion

            #region CaptureImage
            double Sec = exposuretime / us;
            Workspace.This.LogMessage("Exposure time set to    =:" + Sec + "s");
            if (LightCode == 1)//R
            {
                RedChannelExposure = Sec;
                Workspace.This.LogMessage("================Red Channel Image Acquisition========");
            }
            else if (LightCode == 2)
            {
                GreenChannelExposure = Sec;
                Workspace.This.LogMessage("================Green Channel Image Acquisition========");
            }
            else if (LightCode == 3)
            {
                BlueChannelExposure = Sec;
                Workspace.This.LogMessage("================Blue Channel Image Acquisition========");
            }
            WriteableBitmap _image = CaptureTrueColorImage(exposuretime, LightCode);
            #endregion
            return _image;
        }

        private WriteableBitmap CaptureTrueColorImage(uint ExposureTime, int LightCode)
        {
            Workspace.This.LogMessage("GrabImage Started  ");
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;  //s to us
            Workspace.This.LogMessage("Exposure time set to    =:" + ExposureTime * us + "s");
            double avg1 = 0.0, avg2 = 0.0, average = 0.0;
            System.Drawing.Point ptMax = new System.Drawing.Point();
            WriteableBitmap biasImage = null;
            WriteableBitmap image = null;
            //Acquire chemi image
            if (_ActiveCamera.SetExpoTime(ExposureTime))
            {
                _ActiveCamera.CapturesImage(ref image);
            }
            Workspace.This.LogMessage("GrabImage  cmpleted");
            Workspace.This.LogMessage("================Image correction applied=============");
            //Subtract bias master B1
            Workspace.This.LogMessage("Applying Subtract_bias_master");
            biasImage = Workspace.This.MasterLibrary.GetBiasImage(1);
            if (biasImage != null)
            {
                image = imageArith.SubtractImage(image, biasImage);
            }
            else
            {
                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                throw new Exception(strMessage);
            }
            //Apply despeckler if it is enabled
            if (_Parameter.DespecklerCorrection)
            {
                Workspace.This.LogMessage("Applying DespecklerCorrection");
                image = ImageProcessing.MedianFilter(image);
            }
            //Apply lens distortion correction if it is enabled
            if (_Parameter.LensDistortionCorrection)
            {
                if (_Parameter.paramB != 0.0)
                {
                    Workspace.This.LogMessage("Applying LensDistortionCorrection");
                    image = ImagingSystemHelper.ChemiSOLO_DistortionCorrection(image, _Parameter.paramA, _Parameter.paramB, _Parameter.paramC);
                }
            }
            //Apply flatfield correction (Red U1, Green U2, Blue U3, White U4) if it is enabled (need to use ptMax and meanI as input here)
            if (_Parameter.FlatfieldCorrection)
            {
                Workspace.This.LogMessage("Applying FlatfieldCorrection");
                imageStat.GetImagePixelAverage5x5(image, ref avg1, ref ptMax); //avg1
                //Get the average intensity meanI of the 5x5 region around the max pixel location ptMax
                int roiWidth = 5;
                int roiHeight = 5;
                System.Drawing.Rectangle scalingRect = new System.Drawing.Rectangle((int)(ptMax.X - 2), (int)(ptMax.Y - 2), roiWidth, roiHeight);
                average = imageStat.GetAverage(image, scalingRect);
                image = Workspace.This.MasterLibrary.ChemiSOLO_CalculateFlatCorrectedImage(image, LightCode, out _FlatCorrection, scalingRect, average);
            }
            WriteableBitmap back = image.Clone();
            WriteableBitmap fach = image.Clone();
            imageArith.M1_Split(ref back, ref fach, MaskImage);
            //Apply white balance correction
            Workspace.This.LogMessage("Applying White balance correction");
            if (SampleType != "BLOT")
            {
                int Peak = ImageProcessing.GetHistogramPeak(fach);
                double scaleVal = (double)_Parameter.GelPvCamScalingThreshold / (double)Peak;
                image = imageArith.Multiply(image, scaleVal);
            }
            else
            {
                int BackPeak = ImageProcessing.GetHistogramPeak(back);
                double back_scaleVal = (double)_Parameter.BlotPvCamScalingThreshold / (double)BackPeak;
                back = imageArith.Multiply(back, back_scaleVal);

                int fach_Peak = ImageProcessing.GetHistogramPeak(fach);
                double fach_scaleVal = (double)_Parameter.GelPvCamScalingThreshold / (double)fach_Peak;
                fach = imageArith.Multiply(fach, fach_scaleVal);

                image = imageArith.M1_Merge(back, fach);
            }

            //Apply kxk binning for rgb mode if kxk (k=2,3,4) binning is selected
            if (_Parameter.pixelbin > 1) //k=2,3,4
            {
                Workspace.This.LogMessage("Apply kxk binning for chemi mode if kxk (k=2,3,4) binning is selected");
                image = imageArith.ConvertBinKxk(image, _Parameter.pixelbin);
            }
            return image;
        }
        #endregion

        #region Grayscale
        private unsafe WriteableBitmap GrayscaleImaging(int LightCode)
        {
            Workspace.This.LogMessage("=================Image Acquisition===================");
            //LED
            Workspace.This.EthernetController.SetRGBLightRegisterControl(LightCode);
            //Set Gain
            _ActiveCamera.SetGain(_Parameter.rgbimagegain);

            #region Parameter
            _ImageChannel = new ImageChannelSettings();
            _ImageChannel.AutoExposureUpperCeiling = _Parameter.upperCeiling;
            _ImageChannel.MaxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
            _ImageChannel.LightType = LightCode;  //None
            _ImageChannel.BinningMode = _Parameter.pixelbin;
            _ImageChannel.InitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.InitialExposureTime;
            _ImageChannel.ChemiInitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.ChemiInitialExposureTime;
            //Settings
            uint exposuretime = _Parameter.exposureTime;
            #endregion
            Workspace.This.LogMessage("Binning Mode            =:" + _Parameter.pixelbin);
            Workspace.This.LogMessage("Gain                    =:" + _Parameter.rgbimagegain);
            #region Get AutoExposureTime
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;
            double _exposuretime = 0;
            //get autoExposureTime
            _exposuretime = ImagingSystemHelper.ChemiSOLO_CalculateImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel);
            exposuretime = (uint)(_exposuretime * us);
            #endregion
            Workspace.This.LogMessage("***********Image capture frame settings**************");
            Workspace.This.LogMessage("LightSource             =:" + LightCode);
            Workspace.This.LogMessage("Calculating autoexposure");
            Workspace.This.LogMessage("CalcAuto: GrabImage - Exposure time =" + (_ImageChannel.InitialExposureTime * us) + " s");
            #region CaptureImage
            double Sec = exposuretime / us;
            RedChannelExposure = Sec;
            Workspace.This.LogMessage("Exposure time set to    =:" + Sec + "s");
            WriteableBitmap _image = CaptureGrayscaleImage(exposuretime, LightCode);
            #endregion
            return _image;

        }
        private WriteableBitmap CaptureGrayscaleImage(uint ExposureTime, int LightCode)
        {
            Workspace.This.LogMessage("GrabImage Started  ");
            double avg1 = 0.0, avg2 = 0.0, average = 0.0;
            System.Drawing.Point ptMax = new System.Drawing.Point();
            WriteableBitmap biasImage = null;
            WriteableBitmap image = null;
            //Acquire chemi image
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;  //s to us
            Workspace.This.LogMessage("Exposure time set to    =:" + ExposureTime * us + "s");
            if (_ActiveCamera.SetExpoTime(ExposureTime))
            {
                _ActiveCamera.CapturesImage(ref image);
            }
            Workspace.This.LogMessage("GrabImage  cmpleted");
            Workspace.This.LogMessage("================Image correction applied=============");
            //Subtract bias master B1
            Workspace.This.LogMessage("Applying Subtract_bias_master");
            biasImage = Workspace.This.MasterLibrary.GetBiasImage(1);
            if (biasImage != null)
            {
                image = imageArith.SubtractImage(image, biasImage);
            }
            else
            {
                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                throw new Exception(strMessage);
            }
            //Apply despeckler if it is enabled
            if (_Parameter.DespecklerCorrection)
            {
                Workspace.This.LogMessage("Applying DespecklerCorrection");
                image = ImageProcessing.MedianFilter(image);
            }
            //Apply lens distortion correction if it is enabled
            if (_Parameter.LensDistortionCorrection)
            {
                if (_Parameter.paramB != 0.0)
                {
                    Workspace.This.LogMessage("Applying LensDistortionCorrection");
                    image = ImagingSystemHelper.ChemiSOLO_DistortionCorrection(image, _Parameter.paramA, _Parameter.paramB, _Parameter.paramC);
                }
            }
            //Apply flatfield correction (Red U1, Green U2, Blue U3, White U4) if it is enabled (need to use ptMax and meanI as input here)
            if (_Parameter.FlatfieldCorrection)
            {
                Workspace.This.LogMessage("Applying FlatfieldCorrection");
                imageStat.GetImagePixelAverage5x5(image, ref avg1, ref ptMax); //avg1
                //Get the average intensity meanI of the 5x5 region around the max pixel location ptMax
                int roiWidth = 5;
                int roiHeight = 5;
                System.Drawing.Rectangle scalingRect = new System.Drawing.Rectangle((int)(ptMax.X - 2), (int)(ptMax.Y - 2), roiWidth, roiHeight);
                average = imageStat.GetAverage(image, scalingRect);
                image = Workspace.This.MasterLibrary.ChemiSOLO_CalculateFlatCorrectedImage(image, LightCode, out _FlatCorrection, scalingRect, average);
            }
            //Apply kxk binning for grayscale mode if kxk (k=2,3,4) binning is selected
            if (_Parameter.pixelbin > 1) //k=2,3,4
            {
                Workspace.This.LogMessage("Apply kxk binning for chemi mode if kxk (k=2,3,4) binning is selected");
                image = imageArith.ConvertBinKxk(image, _Parameter.pixelbin);
            }
            return image;
        }
        #endregion

        #region BlotFinding
        private unsafe void BlotFinding()
        {
            //off LED
            Workspace.This.EthernetController.SetRGBLightRegisterControl(4);
            //Set Gain
            _ActiveCamera.SetGain(_Parameter.rgbimagegain);


            double avg1 = 0.0, avg2 = 0.0, average = 0.0;
            System.Drawing.Point ptMax = new System.Drawing.Point();
            WriteableBitmap image = null;
            Mat mat = null;
            double sampleType_t = _Parameter.SampleType_threshold;
            uint ExposureTime = _Parameter.BlotFindExposureTime;
            double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;
            double Sec = ExposureTime / us;
            Workspace.This.LogMessage("Binning Mode            =:" + 1);
            Workspace.This.LogMessage("Gain                    =:" + _Parameter.rgbimagegain);
            Workspace.This.LogMessage("Light                   = 4");
            Workspace.This.LogMessage("Exposure time set to    =:" + Sec + "s");
            Workspace.This.LogMessage("GrabImage Started  ");
            DateTime dateTime = DateTime.Now;
            //Acquire chemi image
            if (_ActiveCamera.SetExpoTime(ExposureTime))
            {
                _ActiveCamera.CapturesImage(ref image);
            }
            Workspace.This.LogMessage("GrabImage  cmpleted  ");
            imageStat.GetImagePixelAverage5x5(image, ref avg1, ref ptMax); //avg1
            //Get the average intensity meanI of the 5x5 region around the max pixel location ptMax
            int roiWidth = 5;
            int roiHeight = 5;
            System.Drawing.Rectangle scalingRect = new System.Drawing.Rectangle((int)(ptMax.X - 2), (int)(ptMax.Y - 2), roiWidth, roiHeight);
            average = imageStat.GetAverage(image, scalingRect);
            image = Workspace.This.MasterLibrary.ChemiSOLO_CalculateFlatCorrectedImage(image, 4, out _FlatCorrection, scalingRect, average);
            mat = imageArith.ConvertWriteableBitmapToMat(image);
            mat = imageArith.Mat16bitConvertTo8bit(mat);
            Cv2.Threshold(mat, mat, 0, 255, ThresholdTypes.Otsu);
            MaskImage = imageArith.ConvertMatToWriteableBitmap(mat);
            int sum_ = imageArith.GetImageSum(mat, 255);
            if (_Parameter.chemiSampleType == ChemiSampleType.Auto_detect)
            {
                SampleType = imageArith.GetSampleType(mat, sum_, sampleType_t);
            }
            else if (_Parameter.chemiSampleType == ChemiSampleType.Qpaque)
            {
                SampleType = "BLOT";
            }
            else if (_Parameter.chemiSampleType == ChemiSampleType.Translucent)
            {
                SampleType = "GEL";
            }
            Workspace.This.LogMessage("SampleType                = "+ SampleType);
            Workspace.This.LogMessage("========BlotImage GrabImage completed ========");
        }
        #endregion

        #region EDRImageCumulative()
        public unsafe WriteableBitmap EDRImageCumulative(ref WriteableBitmap image, List<WriteableBitmap> lImg, double scale,int roiWidth, int roiHeight)
        {
            image =  new WriteableBitmap(roiWidth, roiHeight, 0, 0, PixelFormats.Gray16, null);
            ushort l4Pixel = 0;
            int index = 0;
            image.Lock();
            ushort* l4pbuff = (ushort*)image.BackBuffer.ToPointer();
            for (int x = 0; x < roiHeight; x++)
            {
                for (int y = 0; y < roiWidth; y++)
                {
                    l4Pixel = 0;
                    index = x + y * image.BackBufferStride / 2;
                    for (int a = 0; a < lImg.Count; a++)
                    {
                        ushort* tempbuff = (ushort*)lImg[a].BackBuffer.ToPointer();
                        l4Pixel += tempbuff[index];
                    }
                    ushort _l4Pixel = (ushort)Math.Pow(l4Pixel, scale);
                    l4pbuff[index] = _l4Pixel;
                }
            }
            image.Unlock();
            return image;
        }

        #endregion

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
        public class ChemiSOLOParameterStruct
        {
            public int width;
            public int height;
            public string name;
            public ChemiApplicationType chemiApplicationType;
            public ChemiSampleType chemiSampleType;
            public int pixelbin;
            public ChemiMarkerType chemiMarkerType;
            public ChemiModeType chemiModeType;
            public ChemiExposureType chemiExposureType;
            public bool isAutoexposure = false;
            public int bit = 16;
            public int numFrames = 1;
            public uint exposureTime;  //um
            public List<uint> multipleExposureList = new List<uint>();
            public int rgbimagegain;
            public int chemiimagegain;
            public bool Chemi_NewAlgo_Enable;
            public double Chemi_T1;
            public string Chemi_binning_Kxk;
            public int Chemi_Intensity;
            public int upperCeiling = 55000;
            public bool Dark_GlowCorrection;
            public bool LineCorrection;
            public bool DespecklerCorrection;
            public bool FlatfieldCorrection;
            public bool LensDistortionCorrection;
            public double paramA;
            public double paramB;
            public double paramC;
            public uint BlotFindExposureTime;//um
            public double SampleType_threshold;
            public int BlotPvCamScalingThreshold;
            public int GelPvCamScalingThreshold;
        }
    }
}
