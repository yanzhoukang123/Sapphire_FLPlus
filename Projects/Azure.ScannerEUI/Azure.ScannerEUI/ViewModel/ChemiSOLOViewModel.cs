using Azure.CommandLib;
using Azure.Configuration.Settings;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.ScannerEUI.SystemCommand;
using Azure.WPF.Framework;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Azure.ScannerEUI.ViewModel
{
    class ChemiSOLOViewModel : ViewModelBase
    {
        #region Private data...
        private ObservableCollection<ApplicationType> _ApplicationOptions = null;
        private ApplicationType _SelectedApplication = null;

        private ObservableCollection<SampleType> _SampleOptions = null;
        private SampleType _SelectedSample = null;

        private ObservableCollection<BinningFactorType> _BinningOptions = new ObservableCollection<BinningFactorType>(); //Bin
        private BinningFactorType _SelectedBinning = null;

        private ObservableCollection<MarkerType> _MarkerOptions = null;
        private MarkerType _SelectedMarker = null;

        private ObservableCollection<ExposureType> _ExposureTypeOptions = null;
        private ExposureType _SelectedExposureType = null;

        private ObservableCollection<ModeType> _ImagingModeOptions = null;
        private ModeType _SelectedImagingMode = null;

        private ObservableCollection<DataKey> _bitOptions = null;
        private DataKey _Selectedbit = null;

        private Visibility _IsMultipleExposure = Visibility.Hidden;
        private Visibility _IsExposure = Visibility.Hidden;
        private Visibility _IsEDR = Visibility.Hidden;

        private bool _IsEnabledControl = true;

        private RelayCommand _StartCaptureCommand = null;
        private RelayCommand _StopCaptureCommand = null;

        private ChemiSOLOCaptureCommand _ImageCaptureCommand = null;

        #endregion

        #region Public data
        public ChemiSOLOViewModel()
        {

            DataKey myDataKey = null;
            _bitOptions = new ObservableCollection<DataKey>();
            myDataKey = new DataKey(18, "18");
            _bitOptions.Add(myDataKey);
            myDataKey = new DataKey(20, "20");
            _bitOptions.Add(myDataKey);
            myDataKey = new DataKey(22, "22");
            _bitOptions.Add(myDataKey);
            myDataKey = new DataKey(24, "24");
            _bitOptions.Add(myDataKey);
            _Selectedbit = _bitOptions[0];


            //Application
            _ApplicationOptions = new ObservableCollection<ApplicationType>();
            ApplicationType application_Chemi_Imaging = new ApplicationType();
            application_Chemi_Imaging.DisplayName = "Chemi Imaging";
            application_Chemi_Imaging.Position = 0;
            application_Chemi_Imaging.Applicationn = ChemiApplicationType.Chemi_Imaging;
            ApplicationType application_TrueColor_Imaging = new ApplicationType();
            application_TrueColor_Imaging.DisplayName = "True Color Imaging";
            application_TrueColor_Imaging.Position = 1;
            application_TrueColor_Imaging.Applicationn = ChemiApplicationType.TrueColor_Imaging;
            ApplicationType application_Grayscale_Imaging = new ApplicationType();
            application_Grayscale_Imaging.DisplayName = "Grayscale Imaging";
            application_Grayscale_Imaging.Position = 2;
            application_Grayscale_Imaging.Applicationn = ChemiApplicationType.Grayscale_Imaging;
            _ApplicationOptions.Add(application_Chemi_Imaging);
            _ApplicationOptions.Add(application_TrueColor_Imaging);
            _ApplicationOptions.Add(application_Grayscale_Imaging);
            _SelectedApplication = _ApplicationOptions[0];

            //SampleType
            _SampleOptions = new ObservableCollection<SampleType>();
            SampleType Sample_Auto_detect = new SampleType();
            Sample_Auto_detect.DisplayName = "Auto-detect";
            Sample_Auto_detect.Position = 0;
            Sample_Auto_detect.Type = ChemiSampleType.Auto_detect;
            SampleType Sample_Qpaque = new SampleType();
            Sample_Qpaque.DisplayName = "Qpaque";
            Sample_Qpaque.Position = 1;
            Sample_Qpaque.Type = ChemiSampleType.Qpaque;
            SampleType Sample_Translucent = new SampleType();
            Sample_Translucent.DisplayName = "Translucent";
            Sample_Translucent.Position = 2;
            Sample_Translucent.Type = ChemiSampleType.Translucent;
            _SampleOptions.Add(Sample_Auto_detect);
            _SampleOptions.Add(Sample_Qpaque);
            _SampleOptions.Add(Sample_Translucent);
            _SelectedSample = _SampleOptions[0];


            //Marker
            _MarkerOptions = new ObservableCollection<MarkerType>();
            MarkerType Marker_None = new MarkerType();
            Marker_None.DisplayName = "None";
            Marker_None.Position = 0;
            Marker_None.Type = ChemiMarkerType.None;
            MarkerType Marker_TureColor = new MarkerType();
            Marker_TureColor.DisplayName = "TureColor";
            Marker_TureColor.Position = 1;
            Marker_TureColor.Type = ChemiMarkerType.TureColor;
            MarkerType Marker_Grayscale = new MarkerType();
            Marker_Grayscale.DisplayName = "Grayscale";
            Marker_Grayscale.Position = 2;
            Marker_Grayscale.Type = ChemiMarkerType.Grayscale;
            _MarkerOptions.Add(Marker_None);
            _MarkerOptions.Add(Marker_TureColor);
            _MarkerOptions.Add(Marker_Grayscale);
            _SelectedMarker = _MarkerOptions[0];


            //ExposureType
            _ExposureTypeOptions = new ObservableCollection<ExposureType>();
            ExposureType ExposureType_RapidCapture = new ExposureType();
            ExposureType_RapidCapture.DisplayName = "RapidCapture";
            ExposureType_RapidCapture.Position = 0;
            ExposureType_RapidCapture.Type = ChemiExposureType.RapidCapture;

            ExposureType ExposureType_Extended_Dynamic_Range = new ExposureType();
            ExposureType_Extended_Dynamic_Range.DisplayName = "Extended Dynamic Range";
            ExposureType_Extended_Dynamic_Range.Position = 1;
            ExposureType_Extended_Dynamic_Range.Type = ChemiExposureType.Extended_Dynamic_Range;

            ExposureType ExposureType_Overexposure = new ExposureType();
            ExposureType_Overexposure.DisplayName = "Overexposure";
            ExposureType_Overexposure.Position = 2;
            ExposureType_Overexposure.Type = ChemiExposureType.Overexposure;

            ExposureType ExposureType_Manual = new ExposureType();
            ExposureType_Manual.DisplayName = "Manual";
            ExposureType_Manual.Position = 3;
            ExposureType_Manual.Type = ChemiExposureType.Manual;
            _ExposureTypeOptions.Add(ExposureType_RapidCapture);
            _ExposureTypeOptions.Add(ExposureType_Overexposure);
            _ExposureTypeOptions.Add(ExposureType_Manual);
            _ExposureTypeOptions.Add(ExposureType_Extended_Dynamic_Range);
            _SelectedExposureType = _ExposureTypeOptions[0];


            //mode
            _ImagingModeOptions = new ObservableCollection<ModeType>();
            ModeType Mode_Single = new ModeType();
            Mode_Single.DisplayName = "Single";
            Mode_Single.Position = 0;
            Mode_Single.Type = ChemiModeType.Single;
            ModeType Mode_Cumulative = new ModeType();
            Mode_Cumulative.DisplayName = "Cumulative";
            Mode_Cumulative.Position = 1;
            Mode_Cumulative.Type = ChemiModeType.Cumulative;
            ModeType Mode_Multiple = new ModeType();
            Mode_Multiple.DisplayName = "Multiple";
            Mode_Multiple.Position = 2;
            Mode_Multiple.Type = ChemiModeType.Multiple;
            _ImagingModeOptions.Add(Mode_Single);
            _ImagingModeOptions.Add(Mode_Cumulative);
            _ImagingModeOptions.Add(Mode_Multiple);
            _SelectedImagingMode = _ImagingModeOptions[0];


            //Binning
            _BinningOptions = SettingsManager.ConfigSettings.BinningFactorOptions;
            if (_BinningOptions != null && _BinningOptions.Count > 0)
            {
                SelectedBinning = _BinningOptions[0];
            }
        }
        public Visibility IsMultipleExposure
        {
            get { return _IsMultipleExposure; }
            set
            {
                if (_IsMultipleExposure != value)
                {
                    _IsMultipleExposure = value;
                    RaisePropertyChanged("IsMultipleExposure");
                }
            }
        }
        public Visibility IsExposure
        {
            get { return _IsExposure; }
            set
            {
                if (_IsExposure != value)
                {
                    _IsExposure = value;
                    RaisePropertyChanged("IsExposure");
                }
            }
        }
        public Visibility IsEDR
        {
            get { return _IsEDR; }
            set
            {
                if (_IsEDR != value)
                {
                    _IsEDR = value;
                    RaisePropertyChanged("IsEDR");
                }
            }
        }

        public ObservableCollection<DataKey> bitOptions
        {
            get
            {
                return _bitOptions;
            }
            set
            {
                if (_bitOptions != value)
                {
                    _bitOptions = value;
                    RaisePropertyChanged("bitOptions");
                }
            }
        }

        public DataKey Selectedbit
        {
            get
            {
                return _Selectedbit;
            }
            set
            {
                if (value != _Selectedbit)
                {
                    _Selectedbit = value;
                    RaisePropertyChanged("Selectedbit");
                }
            }
        }

        public ObservableCollection<ApplicationType> ApplicationOptions
        {
            get
            {
                return _ApplicationOptions;
            }
            set
            {
                if (_ApplicationOptions != value)
                {
                    _ApplicationOptions = value;
                    RaisePropertyChanged("ApplicationOptions");
                }
            }
        }

        public ApplicationType SelectedApplication
        {
            get
            {
                return _SelectedApplication;
            }
            set
            {
                if (value != _SelectedApplication)
                {
                    _SelectedApplication = value;
                    RaisePropertyChanged("SelectedApplication");
                    Protocol();
                }
            }
        }

        public ObservableCollection<SampleType> SampleOptions
        {
            get
            {
                return _SampleOptions;
            }
            set
            {
                if (_SampleOptions != value)
                {
                    _SampleOptions = value;
                    RaisePropertyChanged("SampleOptions");
                }
            }
        }

        public SampleType SelectedSample
        {
            get
            {
                return _SelectedSample;
            }
            set
            {
                if (value != _SelectedSample)
                {
                    _SelectedSample = value;
                    RaisePropertyChanged("SelectedSample");
                }
            }
        }

        public ObservableCollection<MarkerType> MakerOptions
        {
            get
            {
                return _MarkerOptions;
            }
            set
            {
                if (_MarkerOptions != value)
                {
                    _MarkerOptions = value;
                    RaisePropertyChanged("MakerOptions");
                }
            }
        }

        public MarkerType SelectedMaker
        {
            get
            {
                return _SelectedMarker;
            }
            set
            {
                if (value != _SelectedMarker)
                {
                    _SelectedMarker = value;
                    RaisePropertyChanged("SelectedMaker");
                    Protocol();
                }
            }
        }

        public ObservableCollection<ExposureType> ExposureTypeOptions
        {
            get
            {
                return _ExposureTypeOptions;
            }
            set
            {
                if (_ExposureTypeOptions != value)
                {
                    _ExposureTypeOptions = value;
                    RaisePropertyChanged("ExposureTypeOptions");
                }
            }
        }

        public ExposureType SelectedExposureType
        {
            get
            {
                return _SelectedExposureType;
            }
            set
            {
                if (value != _SelectedExposureType)
                {
                    _SelectedExposureType = value;
                    Protocol();
                    RaisePropertyChanged("SelectedExposureType");
                    //if (value.Type == ChemiExposureType.Manual && SelectedImagingMode.Type == ChemiModeType.Multiple)
                    //{
                    //    SelectedImagingMode = _ImagingModeOptions[0];
                    //}
                }

            }
        }

        public ObservableCollection<ModeType> ImagingModeOptions
        {
            get
            {
                return _ImagingModeOptions;
            }
            set
            {
                if (_ImagingModeOptions != value)
                {
                    _ImagingModeOptions = value;
                    RaisePropertyChanged("ImagingModeOptions");
                }
            }
        }

        public ModeType SelectedImagingMode
        {
            get
            {
                return _SelectedImagingMode;
            }
            set
            {
                if (value != _SelectedImagingMode)
                {
                    _SelectedImagingMode = value;
                    RaisePropertyChanged("SelectedImagingMode");
                    Protocol();
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
        public BinningFactorType SelectedBinning
        {
            get { return _SelectedBinning; }
            set
            {
                if (_SelectedBinning != value)
                {
                    _SelectedBinning = value;
                    RaisePropertyChanged("SelectedBinning");
                    Protocol();
                }
            }
        }

        public bool IsEnabledControl
        {
            get
            {
                return _IsEnabledControl;
            }
            set
            {
                if (value != _IsEnabledControl)
                {
                    _IsEnabledControl = value;
                    RaisePropertyChanged("IsEnabledControl");
                }
            }
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

        public unsafe void ExecuteStartCaptureCommand(object parameter)
        {
            if (!Workspace.This.CameraModeViewModel.IsCameraConnected)
            {
                return;
            }
            try
            {
                ChemiSOLOCaptureCommand.ChemiSOLOParameterStruct Parameter = new ChemiSOLOCaptureCommand.ChemiSOLOParameterStruct();  //Set parameter
                Parameter.chemiApplicationType = SelectedApplication.Applicationn;
                Parameter.chemiSampleType = SelectedSample.Type;
                Parameter.pixelbin = SelectedBinning.HorizontalBins;
                Parameter.chemiMarkerType = SelectedMaker.Type;
                Parameter.chemiModeType = SelectedImagingMode.Type;
                Parameter.chemiExposureType = SelectedExposureType.Type;
                Parameter.bit = 16;
                Parameter.isAutoexposure = true;
                Parameter.numFrames = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.NumFrames;
                Parameter.rgbimagegain = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.RGBImageGain;
                Parameter.chemiimagegain = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.ChemiImageGain;
                Parameter.Chemi_NewAlgo_Enable = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Chemi_NewAlgo_Enable;
                Parameter.Chemi_T1 = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Chemi_T1;
                Parameter.Chemi_binning_Kxk = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Chemi_binning_Kxk;
                Parameter.Chemi_Intensity = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Chemi_Intensity;
                Parameter.Dark_GlowCorrection = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Dark_GlowCorrection;
                Parameter.LineCorrection = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.LineCorrection;
                Parameter.DespecklerCorrection = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.DespecklerCorrection;
                Parameter.FlatfieldCorrection = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.FlatfieldCorrection;
                Parameter.LensDistortionCorrection = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.LensDistortionCorrection;
                Parameter.paramA = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.paramA;
                Parameter.paramB = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.paramB;
                Parameter.paramC = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.paramC;
                Parameter.BlotFindExposureTime = (uint)(SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.BlotFindExposureTime * Workspace.This.CameraController.USConvertMS);
                Parameter.SampleType_threshold = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.SampleType_threshold;
                Parameter.BlotPvCamScalingThreshold = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.BlotPvCamScalingThreshold;
                Parameter.GelPvCamScalingThreshold = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.GelPvCamScalingThreshold;
                if (SelectedExposureType.Type == ChemiExposureType.RapidCapture)
                {
                    Parameter.upperCeiling = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.RapidCapture;
                }
                else if (SelectedExposureType.Type == ChemiExposureType.Overexposure)
                {
                    Parameter.upperCeiling = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.OverExposeure;
                }
                else if (SelectedExposureType.Type == ChemiExposureType.Extended_Dynamic_Range)
                {
                    Parameter.upperCeiling = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.DynamicRange;
                    Parameter.bit = Selectedbit.Value;
                }
                else if (SelectedExposureType.Type == ChemiExposureType.Manual)
                {
                    Parameter.isAutoexposure = false;
                    uint us_exposuretime = (uint)(Workspace.This.ManualExposureViewModel.ExposureTime * Workspace.This.CameraController.USConvertMS);
                    Parameter.exposureTime = us_exposuretime;
                    Parameter.upperCeiling = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.RapidCapture;
                }
                Parameter.width = Workspace.This.CameraController.CaptureImage_Width;
                Parameter.height = Workspace.This.CameraController.CaptureImage_Height;
                Parameter.name= Workspace.This.GenerateChemiSOLOFileName(string.Empty, string.Empty);
                int exposurtlistcount = Workspace.This.MultipleExposureViewModel.MultipleExposureList.Count;
                for (int i = 0; i < exposurtlistcount; i++)
                {
                    Parameter.multipleExposureList.Add((uint)(Workspace.This.MultipleExposureViewModel.MultipleExposureList[i] * Workspace.This.CameraController.USConvertMS));
                }

                _ImageCaptureCommand = new ChemiSOLOCaptureCommand(Workspace.This.Owner.Dispatcher,
                                           Workspace.This.EthernetController,
                                           Workspace.This.CameraController,
                                           Parameter);

                _ImageCaptureCommand.Completed += new CommandLib.ThreadBase.CommandCompletedHandler(_ImageCaptureCommand_Completed);
                _ImageCaptureCommand.CommandStatus += new ChemiSOLOCaptureCommand.CommandStatusHandler(_ImageCaptureCommand_CommandStatus);
                _ImageCaptureCommand.CompletionEstimate += new ChemiSOLOCaptureCommand.CommandCompletionEstHandler(_ImageCaptureCommand_CompletionEstimate);
                _ImageCaptureCommand.ImageReceived += new ChemiSOLOCaptureCommand.ImageReceivedHandler(_ImageCaptureCommand_ImageReceived);
                _ImageCaptureCommand.Start();
                Workspace.This.CameraModeViewModel.IsCapturing = true;
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = false;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = false;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = false;
                RaisePropertyChanged("IsEnabledControl");
            }
            catch { }
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
        private void _ImageCaptureCommand_ImageReceived(WriteableBitmap Image,ImageInfo imageInfo,string ImageName)
        {
            try
            {
                Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
                {
                    // Capture successful
                    WriteableBitmap capturedImage = Image;
                    if (imageInfo != null)
                    {
                        imageInfo.SoftwareVersion = Workspace.This.ProductVersion;
                    }

                    if (capturedImage != null)
                    {
                        Workspace.This.NewDocument(capturedImage, imageInfo, ImageName, false);
                        Workspace.This.SelectedTabIndex = (int)ApplicationTabType.Gallery;   // Switch to gallery tab
                    }
                });
            }
            catch (Exception ex)
            {
                ExecuteStopCaptureCommand(null);
                throw new Exception(" error.", ex);
            }
        }
        private void _ImageCaptureCommand_Completed(CommandLib.ThreadBase sender, CommandLib.ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.CaptureCountdownTimer.Stop();
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                Workspace.This.CameraModeViewModel.IsCapturing = false;
                RaisePropertyChanged("IsEnabledControl");

                ChemiSOLOCaptureCommand imageCaptureThread = (sender as ChemiSOLOCaptureCommand);

                if (exitState == ThreadBase.ThreadExitStat.None)
                {
                    // Capture successful
                    //WriteableBitmap capturedImage = imageCaptureThread.CaptureImage;
                    //ImageInfo imageInfo = imageCaptureThread.ImageInfo;
                    //if (imageInfo != null)
                    //{
                    //    imageInfo.SoftwareVersion = Workspace.This.ProductVersion;
                    //}

                    //if (capturedImage != null)
                    //{
                    //    string newTitle = imageCaptureThread.ImageName;
                    //    Workspace.This.NewDocument(capturedImage, imageInfo, newTitle, false);
                    //    Workspace.This.SelectedTabIndex = (int)ApplicationTabType.Gallery;   // Switch to gallery tab
                    //}
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
                _ImageCaptureCommand.CommandStatus -= new ChemiSOLOCaptureCommand.CommandStatusHandler(_ImageCaptureCommand_CommandStatus);
                _ImageCaptureCommand.ImageReceived -= new ChemiSOLOCaptureCommand.ImageReceivedHandler(_ImageCaptureCommand_ImageReceived);
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
                Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                _ImageCaptureCommand.Abort();
            }
        }

        public bool CanExecuteStopCaptureCommand(object parameter)
        {
            return true;
        }

        #endregion


        private void Protocol()
        {
            //if (_SelectedExposureType == null)
            //{
            //    _SelectedExposureType = _ExposureTypeOptions[0];
            //    RaisePropertyChanged("SelectedExposureType");
            //}
            //
            if (_SelectedApplication.Applicationn == ChemiApplicationType.Chemi_Imaging &&
                _SelectedBinning.HorizontalBins == 1 &&
                _SelectedMarker.Type == ChemiMarkerType.None &&
                _SelectedImagingMode.Type == ChemiModeType.Single)
            {
                if (_ExposureTypeOptions.Count == 3)
                {
                    _SelectedExposureType = _ExposureTypeOptions[0];
                    RaisePropertyChanged("SelectedExposureType");
                    ExposureType ExposureType_Extended_Dynamic_Range = new ExposureType();
                    ExposureType_Extended_Dynamic_Range.DisplayName = "Extended Dynamic Range";
                    ExposureType_Extended_Dynamic_Range.Position = 3;
                    ExposureType_Extended_Dynamic_Range.Type = ChemiExposureType.Extended_Dynamic_Range;
                    _ExposureTypeOptions.Add(ExposureType_Extended_Dynamic_Range);
                }
            }
            else
            {
                if (_SelectedExposureType.Type==ChemiExposureType.Extended_Dynamic_Range)
                {
                    if (_ExposureTypeOptions.Count == 4)
                    {
                        _SelectedExposureType = _ExposureTypeOptions[0];
                        RaisePropertyChanged("SelectedExposureType");
                        _ExposureTypeOptions.RemoveAt(3);
                    }
                }
            }
            RaisePropertyChanged("SelectedExposureType");
            //
            if (_SelectedApplication.Applicationn == ChemiApplicationType.Chemi_Imaging)
            {
                IsEnabledControl = true;
            }
            else
            {
                IsEnabledControl = false;
            }
            //
            if (_SelectedImagingMode.Type == ChemiModeType.Multiple)
            {
                IsMultipleExposure = Visibility.Visible;
                IsEDR = Visibility.Hidden;
                IsExposure = Visibility.Hidden;
            }
            else if (_SelectedImagingMode.Type == ChemiModeType.Single && _SelectedExposureType.Type == ChemiExposureType.Extended_Dynamic_Range)
            {
                IsMultipleExposure = Visibility.Hidden;
                IsEDR = Visibility.Visible;
                IsExposure = Visibility.Hidden;
            }
            else if (_SelectedImagingMode.Type == ChemiModeType.Single && _SelectedExposureType.Type == ChemiExposureType.Manual)
            {
                IsMultipleExposure = Visibility.Hidden;
                IsEDR = Visibility.Hidden;
                IsExposure = Visibility.Visible;
            }
            else if ( _SelectedImagingMode.Type == ChemiModeType.Cumulative && _SelectedExposureType.Type == ChemiExposureType.Manual)
            {
                IsMultipleExposure = Visibility.Hidden;
                IsEDR = Visibility.Hidden;
                IsExposure = Visibility.Visible;
            }
            else
            {
                IsMultipleExposure = Visibility.Hidden;
                IsEDR = Visibility.Hidden;
                IsExposure = Visibility.Hidden;
            }
        }
        public class DataKey
        {
            public int Value { get; set; }
            public string DisplayName { get; set; }

            public DataKey(int value, string displayName)
            {
                this.Value = value;
                this.DisplayName = displayName;
            }
        }
        public class ApplicationType
        {
            public int Position
            {
                get;
                set;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public ChemiApplicationType Applicationn
            {
                get;
                set;
            }

            public ApplicationType()
            {
            }

            public ApplicationType(int position, string displayName, ChemiApplicationType applicationn)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.Applicationn = applicationn;
            }
        }

        public class SampleType
        {
            public int Position
            {
                get;
                set;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public ChemiSampleType Type
            {
                get;
                set;
            }

            public SampleType()
            {
            }

            public SampleType(int position, string displayName, ChemiSampleType type)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.Type = type;
            }
        }

        public class MarkerType
        {
            public int Position
            {
                get;
                set;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public ChemiMarkerType Type
            {
                get;
                set;
            }

            public MarkerType()
            {
            }

            public MarkerType(int position, string displayName, ChemiMarkerType type)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.Type = type;
            }
        }

        public class ExposureType
        {
            public int Position
            {
                get;
                set;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public ChemiExposureType Type
            {
                get;
                set;
            }

            public ExposureType()
            {
            }

            public ExposureType(int position, string displayName, ChemiExposureType type)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.Type = type;
            }
        }

        public class ModeType
        {
            public int Position
            {
                get;
                set;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public ChemiModeType Type
            {
                get;
                set;
            }

            public ModeType()
            {
            }

            public ModeType(int position, string displayName, ChemiModeType type)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.Type = type;
            }
        }
    }
}
