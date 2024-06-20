using Azure.CommandLib;
using Azure.Configuration.Settings;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Azure.CameraLib.CameraController;

namespace Azure.ScannerEUI.ViewModel
{
    class CameraModeViewModel : ViewModelBase
    {
        #region Private data...
        private decimal _ExposureTime = 1M;   // exposure time in seconds
        private ObservableCollection<BinningFactorType> _BinningOptions = new ObservableCollection<BinningFactorType>(); //Bin
        private ObservableCollection<GainType> _GainOptions = null; //Gain
        private Dictionary<bool, string> _DarkFrameCorrOptions = new Dictionary<bool, string>();
        private BinningFactorType _SelectedBinning = null;
        private GainType _SelectedGain = null;

        private bool _IsDarkFrameCorrEnabled = false; //是否启用Dark校准
        private int _Left = 0;
        private int _Top = 0;
        private int _Width = 0;
        private int _Height = 0;


        private double _CcdTempSetPoint = -10;
        private double _CcdTemp = 0;

        private int _LedRedIntensity = 0;
        private int _LedGreenIntensity = 0;
        private int _LedBlueIntensity = 0;

        private bool _IsWhiteLEDOn = false;
        private bool _IsCameraConnected = false;
        private bool _IsLedRedSelected = false;
        private bool _IsLedGreenSelected = false;
        private bool _IsLedBlueSelected = false;

        private string _ExposureTimeRange = "0";

        private RelayCommand _ResetRoiCommand = null;
        private RelayCommand _SetCcdTempCommand = null;
        private RelayCommand _ReadCcdTempCommand = null;
        private RelayCommand _StartCaptureCommand = null;
        private RelayCommand _StopCaptureCommand = null;
        private RelayCommand _StartContinuousCommand = null;
        private RelayCommand _StopContinuousCommand = null;
        private RelayCommand _CameraModeCommand = null;
        private string _CameraMode = "CameraMode";
        private bool _IsCameraEnabled = true;
        private bool _IsCapturing = false;
        private bool _IsContinuous = false;
        private bool _IsCameraPanel = true;
        private Thread _ModeSwitch = null;

        private DispatcherTimer timer_ = null;
        private ImageCaptureCommand _ImageCaptureCommand = null;
        private ImagingLiveCommand _LiveModeCommand = null;

        #endregion

        #region Public data
        public CameraModeViewModel()
        {
            _BinningOptions = SettingsManager.ConfigSettings.BinningFactorOptions;
            _GainOptions = SettingsManager.ConfigSettings.GainOptions;
            //Select binning 1x1
            if (_BinningOptions != null && _BinningOptions.Count > 0)
            {
                SelectedBinning = _BinningOptions[0];
            }
            //Select gain: 1
            if (_GainOptions != null && GainOptions.Count > 0)
            {
                SelectedGain = _GainOptions[0];
            }
            _DarkFrameCorrOptions.Add(true, "Enable");
            _DarkFrameCorrOptions.Add(false, "Disabled");
            timer_ = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
            timer_.Tick += (sender, e) =>
            {
               CcdTemp = Workspace.This.CameraController.CcdTemp;
               IsCameraConnected = Workspace.This.CameraController.IsCameraConnected;
            };
            timer_.Start();
        }
        public decimal ExposureTime
        {
            get { return _ExposureTime; }
            set
            {
                if (_ExposureTime != value)
                {
                    _ExposureTime = value;
                    RaisePropertyChanged("ExposureTime");
                }
            }
        }
        public ObservableCollection<BinningFactorType> BinningOptions
        {
            get
            {
                return _BinningOptions;
            }
        }

        public ObservableCollection<GainType> GainOptions
        {
            get { return _GainOptions; }
        }
        public BinningFactorType SelectedBinning
        {
            get { return _SelectedBinning; }
            set
            {
                if (_SelectedBinning != value)
                {
                    _SelectedBinning = value;
                    if (IsCameraConnected)
                    {
                        if(value.HorizontalBins==1)
                          Workspace.This.CameraController.SetBinning(Binning.Not_Binning);
                        if (value.HorizontalBins == 2)
                            Workspace.This.CameraController.SetBinning(Binning.Fusion_Binning2x2);//
                        if (value.HorizontalBins == 3)
                            Workspace.This.CameraController.SetBinning(Binning.Fusion_Binning3x3);//
                        if (value.HorizontalBins == 4)
                            Workspace.This.CameraController.SetBinning(Binning.Fusion_Binning4x4);//
                        int  Bin_Width = Workspace.This.CameraController.Width / value.HorizontalBins;
                        int  Bin_Height = Workspace.This.CameraController.Height / value.HorizontalBins;
                        Workspace.This.CameraController.CaptureImage_Width = Bin_Width;
                        Workspace.This.CameraController.CaptureImage_Height = Bin_Height;

                    }
                    RaisePropertyChanged("SelectedBinning");
                }
            }
        }
        public GainType SelectedGain
        {
            get { return _SelectedGain; }
            set
            {
                if (_SelectedGain != value)
                {
                    _SelectedGain = value;
                    if(IsCameraConnected)
                       Workspace.This.CameraController.SetGain(value.Value);
                    RaisePropertyChanged("SelectedGain");
                }
            }
        }

        public bool IsDarkFrameCorrEnabled
        {
            get { return _IsDarkFrameCorrEnabled; }
            set
            {
                if (_IsDarkFrameCorrEnabled != value)
                {
                    _IsDarkFrameCorrEnabled = value;
                    RaisePropertyChanged("IsDarkFrameCorrEnabled");
                }
            }
        }

        /// <summary>
        /// The x-coordinate of the left edge of the rectangle
        /// </summary>
        public int Left
        {
            get { return _Left; }
            set
            {
                if (_Left != value)
                {
                    _Left = value;
                    RaisePropertyChanged("Left");
                }
            }
        }
        /// <summary>
        /// The y-coordinate of the top edge of the rectangle
        /// </summary>
        public int Top
        {
            get { return _Top; }
            set
            {
                if (_Top != value)
                {
                    _Top = value;
                    RaisePropertyChanged("Top");
                }
            }
        }
        public int Width
        {
            get { return _Width; }
            set
            {
                if (_Width != value)
                {
                    _Width = value;
                    RaisePropertyChanged("Width");
                }
            }
        }
        public int Height
        {
            get { return _Height; }
            set
            {
                if (_Height != value)
                {
                    _Height = value;
                    RaisePropertyChanged("Height");
                }
            }
        }

        public double CcdTempSetPoint
        {
            get { return _CcdTempSetPoint; }
            set
            {
                if (_CcdTempSetPoint != value)
                {
                    _CcdTempSetPoint = value;
                    RaisePropertyChanged("CcdTempSetPoint");
                }
            }
        }
        public double CcdTemp
        {
            get { return _CcdTemp; }
            set
            {
                if (_CcdTemp != value)
                {
                    _CcdTemp = value;
                    RaisePropertyChanged("CcdTemp");
                }
            }
        }

        public bool IsWhiteLEDOn
        {
            get { return _IsWhiteLEDOn; }
            set
            {
                if (_IsWhiteLEDOn != value)
                {
                    _IsWhiteLEDOn = value;
                    if (value == true)
                    {
                        Workspace.This.EthernetController.SetRGBLightRegisterControl(1);
                    }
                    else
                    {
                        Workspace.This.EthernetController.SetRGBLightRegisterControl(0);
                    }
                    RaisePropertyChanged("IsWhiteLEDOn");
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
                    RaisePropertyChanged("IsCameraConnected");
                    RaisePropertyChanged("IsEnabledControl");
                }
            }
        }

        public bool IsEnabledControl
        {
            get
            {
                if (IsCameraConnected && !IsCapturing && !IsContinuous)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public string CameraMode
        {
            get
            {
                return _CameraMode;
            }
            set
            {
                _CameraMode = value;
                RaisePropertyChanged("CameraMode");
            }
        }
        public bool IsCameraEnabled
        {
            get
            {
                return _IsCameraEnabled;
            }
            set
            {
                if (_IsCameraEnabled != value)
                {
                    _IsCameraEnabled = value;
                    RaisePropertyChanged("IsCameraEnabled");
                }
            }
        }

        public string ExposureTimeRange
        {
            get
            {
                return _ExposureTimeRange;
            }
            set
            {
                if (_ExposureTimeRange != value)
                {
                    _ExposureTimeRange = value;
                    RaisePropertyChanged("ExposureTimeRange");
                }
            }
        }

        public bool IsCapturing
        {
            get { return _IsCapturing; }
            set
            {
                if (_IsCapturing != value)
                {
                    Workspace.This.IsCapturing = value;
                    _IsCapturing = value;
                    RaisePropertyChanged("IsCapturing");
                }
            }
        }

        public bool IsContinuous
        {
            get { return _IsContinuous; }
            set
            {
                if (_IsContinuous != value)
                {
                    _IsContinuous = value;
                    Workspace.This.IsContinuous = value;
                    RaisePropertyChanged("IsContinuous");
                }
            }
        }

        public bool IsCameraPanel
        {
            get { return _IsCameraPanel; }
            set
            {
                if (_IsCameraPanel != value)
                {
                    _IsCameraPanel = value;
                    RaisePropertyChanged("IsCameraPanel");
                }
            }
        }

        public int LedRedIntensity
        {
            get { return _LedRedIntensity; }
            set
            {
                if (_LedRedIntensity != value)
                {
                    _LedRedIntensity = value;
                    RaisePropertyChanged("LedRedIntensity");
                }
            }
        }

        public int LedGreenIntensity
        {
            get { return _LedGreenIntensity; }
            set
            {
                if (_LedGreenIntensity != value)
                {
                    _LedGreenIntensity = value;
                    RaisePropertyChanged("LedGreenIntensity");
                }
            }
        }

        public int LedBlueIntensity
        {
            get { return _LedBlueIntensity; }
            set
            {
                if (_LedBlueIntensity != value)
                {
                    _LedBlueIntensity = value;
                    RaisePropertyChanged("LedBlueIntensity");
                }
            }
        }

        public bool IsLedRedSelected
        {
            get { return _IsLedRedSelected; }
            set
            {
                if (_IsLedRedSelected != value)
                {
                    _IsLedRedSelected = value;
                    RaisePropertyChanged("IsLedRedSelected");
                    if (_IsLedRedSelected == true)
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedRed(LedRedIntensity);
                        //}
                    }
                    else
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedRed(0);
                        //}
                    }
                }
            }
        }

        public bool IsLedGreenSelected
        {
            get { return _IsLedGreenSelected; }
            set
            {
                if (_IsLedGreenSelected != value)
                {
                    _IsLedGreenSelected = value;
                    RaisePropertyChanged("IsLedGreenSelected");
                    if (_IsLedGreenSelected == true)
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedGreen(LedGreenIntensity);
                        //}
                    }
                    else
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedGreen(0);
                        //}
                    }
                }
            }
        }

        public bool IsLedBlueSelected
        {
            get { return _IsLedBlueSelected; }
            set
            {
                if (_IsLedBlueSelected != value)
                {
                    _IsLedBlueSelected = value;
                    RaisePropertyChanged("IsLedBlueSelected");
                    if (_IsLedBlueSelected == true)
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedBlue(LedBlueIntensity);
                        //}
                    }
                    else
                    {
                        //if (Workspace.This.ApdVM.APDTransfer.APDTransferIsAlive)
                        //{
                        //    Workspace.This.ApdVM.APDTransfer.APDLaserLedBlue(0);
                        //}
                    }
                }
            }
        }

        #endregion


        #region ResetRoiCommand

        public ICommand ResetRoiCommand
        {
            get
            {
                if (_ResetRoiCommand == null)
                {
                    _ResetRoiCommand = new RelayCommand(ExecuteResetRoiCommand, CanExecuteResetRoiCommand);
                }

                return _ResetRoiCommand;
            }
        }
        public void ExecuteResetRoiCommand(object parameter)
        {
            if (!IsCameraConnected)
            {
                MessageBox.Show("相机没有连接");
                return;
            }
            //unsigned xOffset: x偏移, 必须是偶数, 否则返回E_INVALIDARG
            //unsigned yOffset: y偏移, 必须是偶数, 否则返回E_INVALIDARG
            //unsigned xWidth: 宽度.最小值16, 必须是偶数, 否则返回E_INVALIDARG
            //unsigned yHeight: 高度.最小值16, 必须是偶数, 否则返回E_INVALIDARG
            if (!Workspace.This.CameraController.SetRoi((uint)Left, (uint)Top, (uint)Width, (uint)Height))
            {
                MessageBox.Show("ROI设置失败");
                return;
            }
            if (Workspace.This.CameraController.GetRoi())
            {
                Workspace.This.CameraController.Left = Left;
                Workspace.This.CameraController.Top = Top;
                Workspace.This.CameraController.Width = Width;
                Workspace.This.CameraController.Height = Height;
                int Bin_Width = Workspace.This.CameraController.Width / SelectedBinning.HorizontalBins;
                int Bin_Height = Workspace.This.CameraController.Height / SelectedBinning.HorizontalBins;
                Workspace.This.CameraController.CaptureImage_Width = Bin_Width;
                Workspace.This.CameraController.CaptureImage_Height = Bin_Height;
            }
            MessageBox.Show("ROI设置成功");
            return;
        }

        public bool CanExecuteResetRoiCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region SetCcdTempCommand

        public ICommand SetCcdTempCommand
        {
            get
            {
                if (_SetCcdTempCommand == null)
                {
                    _SetCcdTempCommand = new RelayCommand(ExecuteSetCcdTempCommand, CanExecuteSetCcdTempCommand);
                }

                return _SetCcdTempCommand;
            }
        }
        public void ExecuteSetCcdTempCommand(object parameter)
        {
            Workspace.This.CameraController.SetCCDTemp(CcdTempSetPoint);
        }

        public bool CanExecuteSetCcdTempCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region ReadCcdTempCommand

        public ICommand ReadCcdTempCommand
        {
            get
            {
                if (_ReadCcdTempCommand == null)
                {
                    _ReadCcdTempCommand = new RelayCommand(ExecuteReadCcdTempCommand, CanExecuteReadCcdTempCommand);
                }

                return _ReadCcdTempCommand;
            }
        }
        public void ExecuteReadCcdTempCommand(object parameter)
        {

        }

        public bool CanExecuteReadCcdTempCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region StartCaptureCommand

        public ICommand StartCaptureCommand
        {
            get
            {
                if (_StartCaptureCommand == null)
                {
                    _StartCaptureCommand = new RelayCommand(ExecuteStartCaptureCommand, CanExecuteStartCaptureCommand);
                }

                return _StartCaptureCommand;
            }
        }
        public void ExecuteStartCaptureCommand(object parameter)
        {
            if (!IsCameraConnected)
            {
                return;
            }
            try
            {
                if (ExposureTime <Workspace.This.CameraController.ExposureTime_MIN || ExposureTime > Workspace.This.CameraController.ExposureTime_Max)
                {
                    MessageBox.Show("Exposure time out range.");
                    return;
                }
                //将毫秒转为微秒
                uint us_exposuretime = (uint)((double)ExposureTime * Workspace.This.CameraController.USConvertMS);
                ImageChannelSettings imagingChannel = new ImageChannelSettings();
                imagingChannel.Exposure = ((double)ExposureTime / Workspace.This.CameraController.USConvertMS); // exposure time in seconds
                imagingChannel.BinningMode = SelectedBinning.VerticalBins;
                imagingChannel.AdGain = SelectedGain.Value;

                ImageCaptureCommand.CameraParameterStruct Parameter = new ImageCaptureCommand.CameraParameterStruct();  //Set parameter
                Parameter.ExposureTime = us_exposuretime;
                Parameter.Bin = SelectedBinning.VerticalBins;
                Parameter.Gain = SelectedGain.Value;
                Parameter.Width = Workspace.This.CameraController.CaptureImage_Width;
                Parameter.Height = Workspace.This.CameraController.CaptureImage_Height;

                _ImageCaptureCommand = new ImageCaptureCommand(Workspace.This.Owner.Dispatcher,
                                                         Workspace.This.CameraController,
                                                         imagingChannel,
                                                         Parameter);

                _ImageCaptureCommand.Completed += new CommandLib.ThreadBase.CommandCompletedHandler(_ImageCaptureCommand_Completed);
                _ImageCaptureCommand.CommandStatus += new ImageCaptureCommand.CommandStatusHandler(_ImageCaptureCommand_CommandStatus);
                _ImageCaptureCommand.CompletionEstimate += new ImageCaptureCommand.CommandCompletionEstHandler(_ImageCaptureCommand_CompletionEstimate);
                _ImageCaptureCommand.Start();
                IsCapturing = true;
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = false;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = false;
                IsCameraEnabled = false;
                RaisePropertyChanged("IsEnabledControl");
            }
            catch (Exception ex)
            {
                throw new Exception("Capture mode error.", ex);
            }
        }

        private void _ImageCaptureCommand_CompletionEstimate(ThreadBase sender, DateTime dateTime, double estTime)
        {
            Workspace.This.Owner.Dispatcher.Invoke((Action)delegate
            {
                Workspace.This.CaptureCountdownTimer.Start();
            });

            Workspace.This.CaptureStartTime = dateTime;
            Workspace.This.EstimatedCaptureTime = estTime;
        }

        private void _ImageCaptureCommand_CommandStatus(object sender, string status)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.CapturingTopStatusText = status;
            });
        }

        private void _ImageCaptureCommand_Completed(CommandLib.ThreadBase sender, CommandLib.ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.CaptureCountdownTimer.Stop();
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                IsCameraEnabled = true;
                IsCapturing = false;
                RaisePropertyChanged("IsEnabledControl");

                ImageCaptureCommand imageCaptureThread = (sender as ImageCaptureCommand);

                if (exitState == ThreadBase.ThreadExitStat.None)
                {
                    // Capture successful
                    WriteableBitmap capturedImage = imageCaptureThread.CaptureImage;
                    ImageInfo imageInfo = imageCaptureThread.ImageInfo;
                    if (imageInfo != null)
                    {
                        imageInfo.SoftwareVersion = Workspace.This.ProductVersion;
                    }

                    if (capturedImage != null)
                    {
                        string newTitle = String.Format("Image{0}", ++Workspace.This.FileNameCount);
                        Workspace.This.NewDocument(capturedImage, imageInfo, newTitle, false);
                        Workspace.This.SelectedTabIndex = (int)ApplicationTabType.Gallery;   // Switch to gallery tab
                    }
                }
                else if (exitState == ThreadBase.ThreadExitStat.Error)
                {
                    // Oh oh something went wrong - handle the error

                    if (imageCaptureThread != null && imageCaptureThread.Error != null)
                    {
                        string strCaption = "Image acquisition error...";
                        string strMessage = string.Empty;

                        if (imageCaptureThread.IsOutOfMemory)
                        {
                            strMessage = "System low on memory.\n" +
                                         "Please close some images before acquiring another image.\n" +
                                         "If this error persists, please restart the application.";
                            MessageBox.Show(strMessage, strCaption);
                        }
                        else
                        {
                            strMessage = "Image acquisition error: \n" + imageCaptureThread.Error.Message;
                            MessageBox.Show(strMessage, strCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }

                }

                _ImageCaptureCommand.Completed -= new CommandLib.ThreadBase.CommandCompletedHandler(_ImageCaptureCommand_Completed);
                _ImageCaptureCommand.CommandStatus -= new ImageCaptureCommand.CommandStatusHandler(_ImageCaptureCommand_CommandStatus);
                _ImageCaptureCommand = null;

            });
        }

        public bool CanExecuteStartCaptureCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region StopCaptureCommand

        public ICommand StopCaptureCommand
        {
            get
            {
                if (_StopCaptureCommand == null)
                {
                    _StopCaptureCommand = new RelayCommand(ExecuteStopCaptureCommand, CanExecuteStopCaptureCommand);
                }

                return _StopCaptureCommand;
            }
        }
        public void ExecuteStopCaptureCommand(object parameter)
        {
            // Abort image capture thread
            if (_ImageCaptureCommand != null)
            {
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                IsCameraEnabled = true;
                _ImageCaptureCommand.Abort();
            }
        }

        public bool CanExecuteStopCaptureCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region StartContinuousCommand

        public ICommand StartContinuousCommand
        {
            get
            {
                if (_StartContinuousCommand == null)
                {
                    _StartContinuousCommand = new RelayCommand(ExecuteStartContinuousCommand, CanExecuteStartContinuousCommand);
                }
                return _StartContinuousCommand;
            }
        }

        public void ExecuteStartContinuousCommand(object parameter)
        {
            if (!IsCameraConnected)
            {
                return;
            }
            try
            {
                if (ExposureTime < Workspace.This.CameraController.ExposureTime_MIN || ExposureTime > Workspace.This.CameraController.ExposureTime_Max)
                {
                    MessageBox.Show("Exposure time out range.");
                    return;
                }
                //设置曝光时间，单位是微妙
                uint ustime = (uint)((double)ExposureTime * Workspace.This.CameraController.USConvertMS);
                ImagingLiveCommand.CameraParameterStruct Parameter = new ImagingLiveCommand.CameraParameterStruct();  //Set parameter
                Parameter.ExposureTime = ustime;
                Parameter.Bin = SelectedBinning.VerticalBins;
                Parameter.Gain = SelectedGain.Value;
                Parameter.Width = Workspace.This.CameraController.CaptureImage_Width;
                Parameter.Height = Workspace.This.CameraController.CaptureImage_Height;
                _LiveModeCommand = new ImagingLiveCommand(Workspace.This.CameraController, Workspace.This.EthernetController, Parameter);
                _LiveModeCommand.CommandStatus += new ImagingLiveCommand.CommandStatusHandler(_LiveModeCommand_CommandStatus);
                _LiveModeCommand.LiveImageReceived += new ImagingLiveCommand.ImageReceivedHandler(_LiveModeCommand_LiveImageReceived);
                _LiveModeCommand.Completed += new ThreadBase.CommandCompletedHandler(_LiveModeCommand_Completed);
                _LiveModeCommand.Start();
                IsContinuous = true;
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = false;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = false;
                IsCameraEnabled = false;
                RaisePropertyChanged("IsEnabledControl");
            }
            catch (Exception ex)
            {
                throw new Exception("Live mode error.", ex);
            }
        }
        private void _LiveModeCommand_Completed(ThreadBase sender, ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.DisplayImage = null;
            });
            Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
            Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
            IsCameraEnabled = true;
            IsContinuous = false;
            RaisePropertyChanged("IsEnabledControl");

            _LiveModeCommand.CommandStatus -= new ImagingLiveCommand.CommandStatusHandler(_LiveModeCommand_CommandStatus);
            _LiveModeCommand.LiveImageReceived -= new ImagingLiveCommand.ImageReceivedHandler(_LiveModeCommand_LiveImageReceived);
            _LiveModeCommand.Completed -= new ThreadBase.CommandCompletedHandler(_LiveModeCommand_Completed);
            _LiveModeCommand = null;
        }
        private void _LiveModeCommand_LiveImageReceived(BitmapSource displayBitmap)
        {
            try
            {
                Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
                {
                    Workspace.This.DisplayImage = displayBitmap;
                });
            }
            catch (Exception ex)
            {
                ExecuteStopContinuousCommand(null);
                throw new Exception("Live mode error.", ex);
            }
        }
        private void _LiveModeCommand_CommandStatus(object sender, string status)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.CapturingTopStatusText = status;
            });
        }

        public bool CanExecuteStartContinuousCommand(object parameter)
        {
            return true;
        }


        #endregion

        #region StopContinuousCommand

        public ICommand StopContinuousCommand
        {
            get
            {
                if (_StopContinuousCommand == null)
                {
                    _StopContinuousCommand = new RelayCommand(ExecuteStopContinuousCommand, CanExecuteStopContinuousCommand);
                }

                return _StopContinuousCommand;
            }
        }
        public void ExecuteStopContinuousCommand(object parameter)
        {
            if (_LiveModeCommand != null)
            {
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                IsCameraEnabled = true;
                _LiveModeCommand.Abort();
            }
        }

        public bool CanExecuteStopContinuousCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region CameraModeCommand
        public ICommand CameraModeCommand
        {
            get
            {
                if (_CameraModeCommand == null)
                {
                    _CameraModeCommand = new RelayCommand(ExecuteCameraModeCommand, CanExecuteCameraModeCommand);
                }

                return _CameraModeCommand;
            }
        }
        public void ExecuteCameraModeCommand(object parameter)
        {
            _ModeSwitch = new Thread(BatchProcessMethod);
            _ModeSwitch.IsBackground = true;
            _ModeSwitch.Start();
        }

        private void BatchProcessMethod()
        {
            if (CameraMode == "CameraMode")
            {                
                if (!Workspace.This.CameraController.Initialize())
                {
                    MessageBox.Show("No camera found.");
                    return;
                }
                double AbsXPos = SettingsManager.ConfigSettings.CameraModeSettings.CameraSettings.AbsXPos;
                double AbsYPos = SettingsManager.ConfigSettings.CameraModeSettings.CameraSettings.AbsYPos;
                ExposureTimeRange = "(" + Workspace.This.CameraController.ExposureTime_MIN + "/" + Workspace.This.CameraController.ExposureTime_Max + ")";
                ExposureTime = Workspace.This.CameraController.ExposureTime_MIN;
                string caption = "Switch to camera mode...";
                string message = "Switch to the camera mode requires the Y stage to move.\n" +
                                 "It will first home the Y motor then move it "+ AbsXPos + "mm.\n" +
                                 "Press \"OK\" to proceed";
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Window window = new Window();
                    window.Topmost = true;
                    System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(window, message, caption, System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Information);
                    if (result == System.Windows.MessageBoxResult.Cancel) { Workspace.This.CameraController.CloseCamera(); return; }
                    IsCameraConnected = Workspace.This.CameraController.IsCameraConnected;
                    if (IsCameraConnected)
                    {
                        Workspace.This.MotorVM.ExecuteHomeCommand(MotorType.X);
                        Workspace.This.MotorVM.ExecuteHomeCommand(MotorType.Y);
                        while (!Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.X].AtHome ||
                               !Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.Y].AtHome)
                        {
                            Thread.Sleep(500);
                        }
                        Workspace.This.MotorVM.AbsXPos = AbsXPos;
                        Workspace.This.MotorVM.ExecuteGoAbsPosCommand(MotorType.X);
                        Workspace.This.MotorVM.AbsYPos = AbsYPos;
                        Workspace.This.MotorVM.ExecuteGoAbsPosCommand(MotorType.Y);
                        while (Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.X].IsBusy ||
                               Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.Y].IsBusy)
                        {
                            Thread.Sleep(500);

                        }
                        CameraMode = "ScannerMode";
                        Workspace.This.IsScanner_Mode = Visibility.Hidden;
                        Workspace.This.IsCamera_Mode = Visibility.Visible;
                        Workspace.This.ScannerModelWindowWidth = 400;
                        Workspace.This.IsMotorEnabled = false;
                        if (Workspace.This.CameraController.GetRoi())
                        {
                            Left = Workspace.This.CameraController.Left;
                            Top = Workspace.This.CameraController.Top;
                            Width = Workspace.This.CameraController.Width;
                            Height = Workspace.This.CameraController.Height;
                        }
                    }
                });
            }
            else
            {
                Workspace.This.CameraController.CloseCamera();
                Workspace.This.MotorVM.ExecuteHomeCommand(MotorType.X);
                Workspace.This.MotorVM.ExecuteHomeCommand(MotorType.Y);
                while (!Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.X].AtHome ||
                       !Workspace.This.MotorVM.MotionController.CrntState[EthernetCommLib.MotorTypes.Y].AtHome)
                {
                    Thread.Sleep(500);
                }
                CameraMode = "CameraMode";
                Workspace.This.IsCamera_Mode = Visibility.Hidden;
                Workspace.This.IsScanner_Mode = Visibility.Visible;
                Workspace.This.ScannerModelWindowWidth = 630;
                Workspace.This.IsMotorEnabled = true;

            }
        }

        public bool CanExecuteCameraModeCommand(object parameter)
        {
            return true;
        }
        #endregion

        public class DarkFrameCorrType
        {
            #region Public properties...

            public int Position { get; set; }

            public int Value { get; set; }

            public string DisplayName { get; set; }

            #endregion

            #region Constructors...

            public DarkFrameCorrType()
            {
            }

            public DarkFrameCorrType(int position, int value, string displayName)
            {
                this.Position = position;
                this.Value = value;
                this.DisplayName = displayName;
            }

            #endregion
        }
    }
}
