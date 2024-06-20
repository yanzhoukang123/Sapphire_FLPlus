using Azure.CommandLib;
using Azure.Configuration.Settings;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.ScannerEUI.SystemCommand;
using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Azure.ScannerEUI.ViewModel
{
    class CreateFlatsViewModel : ViewModelBase
    {
        #region Fields...

        private double _ExposureTimeInSec = 0.0;
        private bool _IsAutoExposure = true;
        private ObservableCollection<LightingType> _LightingTypes = null;
        private LightingType _SelectedLightingType = null;
        private bool _IsCreatingFlats = false;
        private bool _IsSendImageToGallery = true;
        private bool _IsCreatingFlatsPanel = true;
        private ICommand _CreateFlatsCommand = null;
        private ICommand _CancelFlatsCommand = null;
        private FlatfieldCaptureCommand _FlatFieldCaptureCommand = null;
        private string _TargetPath = string.Empty;

        #endregion

        public CreateFlatsViewModel()
        {
            _LightingTypes = new ObservableCollection<LightingType>();
            LightingType lightingTypeNone = new LightingType();
            lightingTypeNone.DisplayName = "None";
            lightingTypeNone.Position = 0;
            lightingTypeNone.LightCode = 0;

            LightingType lightingTypeR = new LightingType();
            lightingTypeR.DisplayName = "RED";
            lightingTypeR.Position = 2;
            lightingTypeR.LightCode = 2;

            LightingType lightingTypeG = new LightingType();
            lightingTypeG.DisplayName = "GREEN";
            lightingTypeG.Position = 3;
            lightingTypeG.LightCode = 3;

            LightingType lightingTypeB = new LightingType();
            lightingTypeB.DisplayName = "BLUE";
            lightingTypeB.Position = 4;
            lightingTypeB.LightCode = 4;


            LightingType lightingTypeW = new LightingType();
            lightingTypeW.DisplayName = "WHITE";
            lightingTypeW.Position = 1;
            lightingTypeW.LightCode = 1;

            _LightingTypes.Add(lightingTypeNone);
            _LightingTypes.Add(lightingTypeR);
            _LightingTypes.Add(lightingTypeG);
            _LightingTypes.Add(lightingTypeB);
            _LightingTypes.Add(lightingTypeW);

            _SelectedLightingType = LightingTypes[0];

        }

        #region Properties/Commands...

        public string Name
        {
            get { return "CREATE FLATS"; }
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                _IsSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        public double ExposureTimeInSec
        {
            get { return _ExposureTimeInSec; }
            set
            {
                if (_ExposureTimeInSec != value)
                {
                    _ExposureTimeInSec = value;
                    RaisePropertyChanged("ExposureTimeInSec");
                }
            }
        }

        public bool IsAutoExposure
        {
            get { return _IsAutoExposure; }
            set
            {
                if (_IsAutoExposure != value)
                {
                    _IsAutoExposure = value;
                    RaisePropertyChanged("IsAutoExposure");
                }
            }
        }

        public ObservableCollection<LightingType> LightingTypes
        {
            get
            {
                return _LightingTypes;
            }
            set
            {
                if (_LightingTypes != value)
                {
                    _LightingTypes = value;
                    RaisePropertyChanged("LightingTypes");
                }
            }
        }

        public LightingType SelectedLightingType
        {
            get
            {
                return _SelectedLightingType;
            }
            set
            {
                if (value != _SelectedLightingType)
                {
                    _SelectedLightingType = value;
                    RaisePropertyChanged("SelectedLightingType");
                }
            }
        }

        public bool IsCreatingFlats
        {
            get { return _IsCreatingFlats; }
            set
            {
                if (_IsCreatingFlats != value)
                {
                    _IsCreatingFlats = value;
                    RaisePropertyChanged("IsCreatingFlats");
                    Workspace.This.IsCapturing = _IsCreatingFlats;
                }
            }
        }

        public bool IsSendImageToGallery
        {
            get { return _IsSendImageToGallery; }
            set
            {
                if (_IsSendImageToGallery != value)
                {
                    _IsSendImageToGallery = value;
                    RaisePropertyChanged("IsSendImageToGallery");
                }
            }
        }

        public bool IsCreatingFlatsPanel
        {
            get { return _IsCreatingFlatsPanel; }
            set
            {
                if (_IsCreatingFlatsPanel != value)
                {
                    _IsCreatingFlatsPanel = value;
                    RaisePropertyChanged("IsCreatingFlatsPanel");
                }
            }
        }

        

        public ICommand CreateFlatsCommand
        {
            get
            {
                if (_CreateFlatsCommand == null)
                {
                    _CreateFlatsCommand = new RelayCommand(
                        param => CreateFlats(),
                        param => (_IsCreatingFlats != true)
                    );
                }
                return _CreateFlatsCommand;
            }
        }

        public ICommand CancelFlatsCommand
        {
            get
            {
                if (_CancelFlatsCommand == null)
                {
                    _CancelFlatsCommand = new RelayCommand(
                        param => CancelCreateFlats(),
                        param => (_IsCreatingFlats == true)
                    );
                }
                return _CancelFlatsCommand;
            }
        }

        #endregion



        #region Methods

        private void CreateFlats()
        {
            if (!IsCreatingFlats)
            {
                if (_SelectedLightingType == null)
                {
                    return;
                }
                if (!IsAutoExposure)
                {
                    //Validate exposure time.
                    if (ExposureTimeInSec <= 0.0)
                    {
                        string caption = "Invalid entry...";
                        string message = "Please enter a valid exposure time.";
                        Xceed.Wpf.Toolkit.MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }
                }
                ImageChannelSettings imageChannel = new ImageChannelSettings();
                imageChannel.Exposure = ExposureTimeInSec;
                imageChannel.IsAutoExposure = IsAutoExposure;
                imageChannel.AutoExposureUpperCeiling = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.FlatsAutoExposureOptimal;
                imageChannel.MaxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
                imageChannel.LightType = _SelectedLightingType.LightCode;
                imageChannel.BinningMode = 1;
                imageChannel.InitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.InitialExposureTime;
                imageChannel.ChemiInitialExposureTime = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.ChemiInitialExposureTime;

                Workspace.This.CameraModeViewModel.IsWhiteLEDOn = true;  //Turn on LED (red/green/blue/white);

                string targetDirectory = System.IO.Path.Combine(SettingsManager.ApplicationDataPath, "Masters\\");
                string fileName = string.Format("U_{0}.dat", _SelectedLightingType.LightCode);

                _TargetPath = System.IO.Path.Combine(targetDirectory, fileName);
                if (System.IO.File.Exists(_TargetPath))
                {
                    string strMessage = "The flat field image already exists for the selected light source.\n" +
                                        "Would you like to replace the existing flat field image?";
                    string strCaption = "Create flat field correction image....";
                    if (Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, strMessage, strCaption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                _FlatFieldCaptureCommand = new FlatfieldCaptureCommand(Workspace.This.Owner.Dispatcher,
                                                   Workspace.This.CameraController,
                                                   Workspace.This.EthernetController,
                                                   imageChannel,
                                                   _TargetPath);
                _FlatFieldCaptureCommand.Completed += new CommandLib.ThreadBase.CommandCompletedHandler(_FlatFieldCaptureCommand_Completed);
                _FlatFieldCaptureCommand.Start();
                IsCreatingFlats = true;
                Workspace.This.CameraModeViewModel.IsCameraPanel = false;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = false;
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = false;


            }
        }

        void _FlatFieldCaptureCommand_Completed(CommandLib.ThreadBase sender, CommandLib.ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                Workspace.This.CameraModeViewModel.IsCameraPanel = true;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                Workspace.This.CameraModeViewModel.IsWhiteLEDOn = false;  //Turn off LED; (red/green/blue/white);
                string caption = string.Empty;
                string message = string.Empty;

                if (exitState == ThreadBase.ThreadExitStat.Error)
                {
                    caption = "Create flat-field...";
                    message = string.Format("Error creating flat-field image file.\n{0}", sender.Error.Message);
                    Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (exitState == ThreadBase.ThreadExitStat.None)
                {
                    caption = "Create flat-field...";
                    message = "Creating flat-field image file successfully completed!";
                    Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                    ThreadBase flatCaptureCommand = null;
                    flatCaptureCommand = (sender as FlatfieldCaptureCommand);
                    if (flatCaptureCommand != null)
                    {
                        WriteableBitmap flatImage = ((FlatfieldCaptureCommand)flatCaptureCommand).FlatImage;
                        if (flatImage != null)
                        {
                            try
                            {
                                if (File.Exists(_TargetPath))
                                {
                                    File.Delete(_TargetPath);
                                }

                                using (System.IO.FileStream fileStream = new System.IO.FileStream(_TargetPath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                                {
                                    ImageProcessing.Save(fileStream, ImageProcessing.TIFF_FILE, flatImage);
                                }
                            }
                            catch (Exception ex)
                            {
                                caption = "Create flat-field image...";
                                message = "ERROR: " + ex.Message;
                                Xceed.Wpf.Toolkit.MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Stop);
                            }
                        }
                    }
                }

                IsCreatingFlats = false;
                _FlatFieldCaptureCommand.Completed -= new CommandLib.ThreadBase.CommandCompletedHandler(_FlatFieldCaptureCommand_Completed);
                _FlatFieldCaptureCommand = null;

                if (exitState == ThreadBase.ThreadExitStat.None && IsSendImageToGallery)
                {
                    // Open the created flat image in the Gallery tab
                    var fileViewModel = Workspace.This.Open(_TargetPath);
                    if (fileViewModel != null)
                    {
                        Workspace.This.ActiveDocument = fileViewModel;      //Make the current image the active image
                        //Workspace.This.ActiveDocument.IsFileLoaded = true;
                        Workspace.This.SelectedTabIndex = (int)ApplicationTabType.Gallery;  // Switch to the Gallery tab
                    }
                }
            });
        }

        private void CancelCreateFlats()
        {
            if (IsCreatingFlats)
            {
                if (_FlatFieldCaptureCommand != null)
                {
                    Workspace.This.CameraModeViewModel.IsCameraPanel = true;
                    Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                    Workspace.This.CreateDarkmastersViewModel.IsCreatingDarkMasterPanel = true;
                    _FlatFieldCaptureCommand.Abort();
                }
            }
        }

        #endregion




        public class LightingType
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

            public int LightCode
            {
                get;
                set;
            }

            public LightingType()
            {
            }

            public LightingType(int position, string displayName, int lightCode)
            {
                this.Position = position;
                this.DisplayName = displayName;
                this.LightCode = lightCode;
            }
        }
    }
}
