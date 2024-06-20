using Azure.Configuration.Settings;
using Azure.ImagingSystem;
using Azure.ScannerEUI.SystemCommand;
using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Azure.ScannerEUI.ViewModel
{
    class CreateDarkmastersViewModel : ViewModelBase
    {
        #region Fields...
        private ObservableCollection<BinningFactorType> _BinningModeOptions = null;
        private BinningFactorType _SelectedBinningMode = null;
        private string _ExposureTimeInput = string.Empty;
        private double _ExposureTimeInSec = 0.0;
        private string _EstTotalTime = string.Empty;
        private string _EstCompletionTime = string.Empty;
        private string _CurrentStatus = string.Empty;
        private ObservableCollection<DataKey> _ExposureTimeOptions = null;
        private DataKey _SelectedExposureTime = null;
        private ObservableCollection<DataKey> _FrameCountOptions = null;
        private DataKey _SelectedFrameCount = null;
        private int _FrameCount = 5;
        private int _CurrentFrame = 0;
        private bool _IsGeneratingDarkmaster = false;
        private bool _IsCreatingSingleDarkmaster = false;
        private bool _IsCreatingDarkMasterPanel = true;

        private ICommand _GenerateDarkmastersCommand = null; // Generate a set of darkmasters files.
        private ICommand _CreateDarkmasterCommand = null;    // Create a single dark/bias file.
        private ICommand _StopDarkmasterCommand = null;

        private DarkmasterCaptureCommand _CreateDarkmasterCaptureCommand = null;
        private DarkmasterCaptureCommand _GenerateDarkmasterCaptureCommand = null;
        private uint _PreviousExecutionState;
        #endregion

        #region Constructors...

        public CreateDarkmastersViewModel()
        {
            _BinningModeOptions = new ObservableCollection<BinningFactorType>();
            BinningFactorType bf = new BinningFactorType();
            bf.DisplayName = "1x1";
            bf.Position = 1;
            bf.VerticalBins = 1;
            bf.HorizontalBins = 1;
            _BinningModeOptions.Add(bf);
            _SelectedBinningMode = _BinningModeOptions[0];

            // Exposure time dropdown
            DataKey myDataKey = null;
            _ExposureTimeOptions = new ObservableCollection<DataKey>();
            myDataKey = new DataKey(0, "0");
            _ExposureTimeOptions.Add(myDataKey);
            myDataKey = new DataKey(10, "10sec");
            _ExposureTimeOptions.Add(myDataKey);
            myDataKey = new DataKey(60, "1min");
            _ExposureTimeOptions.Add(myDataKey);
            myDataKey = new DataKey(300, "5min");
            _ExposureTimeOptions.Add(myDataKey);
            myDataKey = new DataKey(600, "10min");
            _ExposureTimeOptions.Add(myDataKey);
            myDataKey = new DataKey(1200, "20min");
            _ExposureTimeOptions.Add(myDataKey);
            _SelectedExposureTime = _ExposureTimeOptions[0];

            // Frame count dropdown
            _FrameCountOptions = new ObservableCollection<DataKey>();
            myDataKey = new DataKey(5, "5");
            _FrameCountOptions.Add(myDataKey);
            myDataKey = new DataKey(10, "10");
            _FrameCountOptions.Add(myDataKey);
            myDataKey = new DataKey(20, "20");
            _FrameCountOptions.Add(myDataKey);
            myDataKey = new DataKey(50, "50");
            _FrameCountOptions.Add(myDataKey);
            myDataKey = new DataKey(100, "100");
            _FrameCountOptions.Add(myDataKey);
            _SelectedFrameCount = _FrameCountOptions[0];
        }

        #endregion
        public ObservableCollection<BinningFactorType> BinningModeOptions
        {
            get { return _BinningModeOptions; }
            set
            {
                _BinningModeOptions = value;
                RaisePropertyChanged("BinningModeOptions");
            }
        }
        public BinningFactorType SelectedBinningMode
        {
            get { return _SelectedBinningMode; }
            set
            {
                _SelectedBinningMode = value;
                RaisePropertyChanged("SelectedBinningMode");
            }
        }
        public string ExposureTimeInput
        {
            get { return _ExposureTimeInput; }
            set
            {
                if (_ExposureTimeInput != value)
                {
                    _ExposureTimeInput = value;
                    RaisePropertyChanged("ExposureTimeInput");
                }
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

        public string EstTotalTime
        {
            get { return _EstTotalTime; }
            set
            {
                _EstTotalTime = value;
                RaisePropertyChanged("EstTotalTime");
            }
        }

        public string EstCompletionTime
        {
            get { return _EstCompletionTime; }
            set
            {
                _EstCompletionTime = value;
                RaisePropertyChanged("EstCompletionTime");
            }
        }

        public string CurrentStatus
        {
            get { return _CurrentStatus; }
            set
            {
                _CurrentStatus = value;
                RaisePropertyChanged("CurrentStatus");
            }
        }
        public ObservableCollection<DataKey> ExposureTimeOptions
        {
            get { return _ExposureTimeOptions; }
        }
        public DataKey SelectedExposureTime
        {
            get { return _SelectedExposureTime; }
            set
            {
                _SelectedExposureTime = value;
                RaisePropertyChanged("SelectedExposureTime");
            }
        }
        public ObservableCollection<DataKey> FrameCountOptions
        {
            get { return _FrameCountOptions; }
        }

        public DataKey SelectedFrameCount
        {
            get { return _SelectedFrameCount; }
            set
            {
                _SelectedFrameCount = value;
                RaisePropertyChanged("SelectedFrameCount");
                if (_SelectedFrameCount != null)
                {
                    FrameCount = _SelectedFrameCount.Value;
                }
            }
        }
        public int FrameCount
        {
            get { return _FrameCount; }
            set
            {
                _FrameCount = value;
                RaisePropertyChanged("FrameCount");
            }
        }
        public int CurrentFrame
        {
            get { return _CurrentFrame; }
            set
            {
                _CurrentFrame = value;
                RaisePropertyChanged("CurrentFrame");
            }
        }

        public bool IsGeneratingDarkmaster
        {
            get { return _IsGeneratingDarkmaster; }
            set
            {
                _IsGeneratingDarkmaster = value;
                RaisePropertyChanged("IsGeneratingDarkmaster");
                Workspace.This.IsCapturing = value;
            }
        }

        public bool IsCreatingSingleDarkmaster
        {
            get { return _IsCreatingSingleDarkmaster; }
            set
            {
                _IsCreatingSingleDarkmaster = value;
                RaisePropertyChanged("IsCreatingSingleDarkmaster");
                Workspace.This.IsCapturing = value;
            }
        }

        public bool IsCreatingDarkMasterPanel
        {
            get { return _IsCreatingDarkMasterPanel; }
            set
            {
                if (_IsCreatingDarkMasterPanel != value)
                {
                    _IsCreatingDarkMasterPanel = value;
                    RaisePropertyChanged("IsCreatingDarkMasterPanel");
                }
            }
        }

        #region GenerateDarkmastersCommand
        public ICommand GenerateDarkmastersCommand
        {
            get
            {
                if (_GenerateDarkmastersCommand == null)
                {
                    _GenerateDarkmastersCommand = new RelayCommand(ExecuteGenerateDarkmastersCommand, CanExecuteGenerateDarkmastersCommand);
                }

                return _GenerateDarkmastersCommand;
            }
        }
        public void ExecuteGenerateDarkmastersCommand(object parameter)
        {
            string inputExposures = _ExposureTimeInput.Trim();
            if (string.IsNullOrEmpty(inputExposures))
            {
                string strMessage = "No exposure values were entered.";
                string strCaption = "Darkmaster Generation Error...";
                Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, strMessage, strCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SettingsManager.ConfigSettings.BinningFactorOptions.Count == 0)
            {
                string strMessage = "No master generation binning modes were found in the 'config.xml' file";
                string strCaption = "Darkmaster Generation Error...";
                Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, strMessage, strCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            List<double> exposureTimes = new List<double>();
            string[] expValues = inputExposures.Split(new char[] { ',' });
            if (expValues == null || expValues.Length == 0)
            {
                string strMessage = "No exposure values were entered.";
                string strCaption = "Darkmaster Generation Error...";
                Xceed.Wpf.Toolkit.MessageBox.Show(Workspace.This.Owner, strMessage, strCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Exposure times in seconds
            double result;
            foreach (string exp in expValues)
            {
                result = 0;
                bool success = double.TryParse(exp, out result);

                if (!success || result < 0 || result > 60)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Invalid exposure value: " + exp, "Generation Error...");
                    return;
                }

                exposureTimes.Add(60 * result); //Command exposure arguments are in seconds.
            }
            TimeSpan totalTime = new TimeSpan();
            DateTime dateTime = new DateTime();
            List<BinningFactorType> binModes = BinningModeOptions.ToList<BinningFactorType>();
            DarkmasterGeneratorHelper.CalculateCompletionTime(binModes, exposureTimes, ref dateTime, ref totalTime);
            string targetDirectory = System.IO.Path.Combine(SettingsManager.ApplicationDataPath, "Masters\\");
            EstTotalTime = totalTime.ToString("c");
            EstCompletionTime = dateTime.ToString();
            int gain = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Gain;
            if (Workspace.This.CameraController.cam_!= null)
            {
                if (!Workspace.This.CameraController.SetGain(gain))
                {
                    MessageBox.Show("Gain fail");
                }
            }
            Workspace.This.CameraModeViewModel.IsWhiteLEDOn = false;  //Turn off all the LED's.
            _GenerateDarkmasterCaptureCommand = new DarkmasterCaptureCommand(Workspace.This.Owner.Dispatcher,
                                                  Workspace.This.CameraController,
                                                  Workspace.This.EthernetController,
                                                  binModes,
                                                  exposureTimes,
                                                  targetDirectory,
                                                  0);

            _GenerateDarkmasterCaptureCommand.Completed += new CommandLib.ThreadBase.CommandCompletedHandler(_GenerateDarkmasterCaptureCommand_Completed);
            _GenerateDarkmasterCaptureCommand.Start();
            Workspace.This.CameraModeViewModel.IsCameraPanel = false;
            Workspace.This.CameraModeViewModel.IsCameraEnabled = false;
            Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = false;

            IsGeneratingDarkmaster = true;
            CurrentStatus = "Bias/Darkmaster generation in progress....";
            #region Set execution state to prevent system sleep
            // Enable away mode and prevent the sleep idle time-out.
            // For more information read here. http://msdn.microsoft.com/en-us/library/aa373208%28v=vs.85%29.aspx
            _PreviousExecutionState = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_DISPLAY_REQUIRED | NativeMethods.ES_SYSTEM_REQUIRED);
            if (_PreviousExecutionState == 0)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("SetThreadExecutionState failed: prevent system sleep",
                                "Prevent system sleep",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            #endregion

        }
        private void _GenerateDarkmasterCaptureCommand_Completed(CommandLib.ThreadBase sender, CommandLib.ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.Invoke((Action)delegate
            {
                IsGeneratingDarkmaster = false;
                Workspace.This.CameraModeViewModel.IsCameraPanel = true;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                DarkmasterCaptureCommand darkmasterCapture = (sender as DarkmasterCaptureCommand);
                if (darkmasterCapture != null)
                {
                    if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.None)
                    {
                        CurrentStatus = "Darkmaster generation completed.";
                        // reload dark master library
                        Workspace.This.MasterLibrary.LoadLibraryInfo();
                    }
                    else if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.Abort)
                    {
                        CurrentStatus = "Darkmaster generation cancelled.";
                    }
                    else if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.Error)
                    {
                        CurrentStatus = "Error generating darkmaster image file.";
                    }

                    _GenerateDarkmasterCaptureCommand.Completed -= new CommandLib.ThreadBase.CommandCompletedHandler(_GenerateDarkmasterCaptureCommand_Completed);
                    //_GenerateDarkmasterCaptureCommand.ProgressChanged -= new DarkmasterCaptureCommand.ProgressChangedHandler(_DarkmasterCaptureCommand_ProgressChanged);
                    _GenerateDarkmasterCaptureCommand = null;

                    // Restore previous state
                    if (NativeMethods.SetThreadExecutionState(_PreviousExecutionState) == 0)
                    {
                        // Display an message error?
                    }
                }
            });
        }
        public bool CanExecuteGenerateDarkmastersCommand(object parameter)
        {
            return true;
        }
        #endregion

        #region CreateDarkmasterCommand
        public ICommand CreateDarkmasterCommand
        {
            get
            {
                if (_CreateDarkmasterCommand == null)
                {
                    _CreateDarkmasterCommand = new RelayCommand(ExecuteCreateDarkmasterCommand, CanExecuteCreateDarkmasterCommand);
                }

                return _CreateDarkmasterCommand;
            }
        }
        public void ExecuteCreateDarkmasterCommand(object parameter)
        {
            if (!IsGeneratingDarkmaster)
            {
                string caption = "Warning!";
                string message = "Are you sure you want to re-create darkmaster files?";
                if (MessageBoxResult.No == Xceed.Wpf.Toolkit.MessageBox.Show(message, caption, MessageBoxButton.YesNo))
                {
                    return;
                }
                message = "Make sure the camera is not exposed to any light.\nWould you like to proceed?";
                if (MessageBoxResult.No == Xceed.Wpf.Toolkit.MessageBox.Show(message, caption, MessageBoxButton.YesNo))
                {
                    return;
                }
                List<BinningFactorType> binModes = BinningModeOptions.ToList<BinningFactorType>();

                List<double> exposureTimes = new List<double>();
                exposureTimes.Add(_SelectedExposureTime.Value);

                int gain = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.Gain;
                if (Workspace.This.CameraController.cam_ != null)
                {
                    if (!Workspace.This.CameraController.SetGain(gain))
                    {
                        MessageBox.Show("Gain fail");
                    }

                }
                Workspace.This.CameraModeViewModel.IsWhiteLEDOn = false;  //Turn off all the LED's.
                string targetDirectory = System.IO.Path.Combine(SettingsManager.ApplicationDataPath, "Masters\\");
                _CreateDarkmasterCaptureCommand = new DarkmasterCaptureCommand(Workspace.This.Owner.Dispatcher,
                                                      Workspace.This.CameraController,
                                                      Workspace.This.EthernetController,
                                                      binModes,
                                                      exposureTimes,
                                                      targetDirectory,
                                                      SelectedFrameCount.Value);

                _CreateDarkmasterCaptureCommand.Completed += new CommandLib.ThreadBase.CommandCompletedHandler(_CreateDarkmasterCaptureCommand_Completed);
                _CreateDarkmasterCaptureCommand.ProgressChanged += new DarkmasterCaptureCommand.ProgressChangedHandler(_CreateDarkmasterCaptureCommand_ProgressChanged);
                _CreateDarkmasterCaptureCommand.Start();
                IsCreatingSingleDarkmaster = true;
                Workspace.This.CameraModeViewModel.IsCameraPanel = false;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = false;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = false;
                CurrentStatus = "Bias/Darkmaster generation in progress....";
            }
        }
        public bool CanExecuteCreateDarkmasterCommand(object parameter)
        {
            return true;
        }

        private void _CreateDarkmasterCaptureCommand_Completed(CommandLib.ThreadBase sender, CommandLib.ThreadBase.ThreadExitStat exitState)
        {
            Workspace.This.Owner.Dispatcher.Invoke((Action)delegate
            {
                IsCreatingSingleDarkmaster = false;
                Workspace.This.CameraModeViewModel.IsCameraPanel = true;
                Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
                Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
                DarkmasterCaptureCommand darkmasterCapture = (sender as DarkmasterCaptureCommand);
                if (darkmasterCapture != null)
                {
                    if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.None)
                    {
                        CurrentStatus = "Darkmaster generation completed.";

                        // reload dark master library
                        Workspace.This.MasterLibrary.LoadLibraryInfo();
                    }
                    else if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.Abort)
                    {
                        CurrentStatus = "Darkmaster generation cancelled.";
                    }
                    else if (darkmasterCapture.ExitStat == CommandLib.ThreadBase.ThreadExitStat.Error)
                    {
                        CurrentStatus = "Error generating darkmaster image file.";
                    }
                    _CreateDarkmasterCaptureCommand.Completed -= new CommandLib.ThreadBase.CommandCompletedHandler(_CreateDarkmasterCaptureCommand_Completed);
                    _CreateDarkmasterCaptureCommand.ProgressChanged -= new DarkmasterCaptureCommand.ProgressChangedHandler(_CreateDarkmasterCaptureCommand_ProgressChanged);
                    _CreateDarkmasterCaptureCommand = null;

                    // Restore previous state
                    if (NativeMethods.SetThreadExecutionState(_PreviousExecutionState) == 0)
                    {
                        // Display an message error?
                    }
                }
            });
        }

        private void _CreateDarkmasterCaptureCommand_ProgressChanged(object sender, int maxFrames, int currentFrame)
        {
            Workspace.This.Owner.Dispatcher.BeginInvoke((Action)delegate
            {
                FrameCount = maxFrames;
                CurrentFrame = currentFrame;
            });
        }
        #endregion

        #region StopDarkmasterCommand
        public ICommand StopDarkmasterCommand
        {
            get
            {
                if (_StopDarkmasterCommand == null)
                {
                    _StopDarkmasterCommand = new RelayCommand(ExecuteStopDarkmasterCommand, CanExecuteStopDarkmasterCommand);
                }

                return _StopDarkmasterCommand;
            }
        }
        public void ExecuteStopDarkmasterCommand(object parameter)
        {
            Workspace.This.CameraModeViewModel.IsCameraPanel = true;
            Workspace.This.CameraModeViewModel.IsCameraEnabled = true;
            Workspace.This.CreateFlatsViewModel.IsCreatingFlatsPanel = true;
            if (_CreateDarkmasterCaptureCommand != null)
            {
                CurrentStatus = "Cancelling darkmasters generator....please wait....";
                _CreateDarkmasterCaptureCommand.Abort();
            }
            else if (_GenerateDarkmasterCaptureCommand != null)
            {
                CurrentStatus = "Cancelling darkmaster creator....please wait....";
                _GenerateDarkmasterCaptureCommand.Abort();
            }
        }
        public bool CanExecuteStopDarkmasterCommand(object parameter)
        {
            return true;
        }
        #endregion



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

        #region public class DarkmasterGeneratorHelper
        /// <summary>
        /// Calculate the estimated completion date and time
        /// </summary>
        public class DarkmasterGeneratorHelper
        {
            #region public static void CalculateCompletionTime(List<string> binModes, List<double> exposureTimes, ref DateTime dateTime, ref TimeSpan totalTime)
            /// <summary>
            /// Calculate the estimated completion date and time
            /// </summary>
            /// <param name="binModes"></param>
            /// <param name="exposureTimes"></param>
            /// <param name="dateTime"></param>
            /// <param name="totalTime"></param>
            public static void CalculateCompletionTime(List<BinningFactorType> binModes, List<double> exposureTimes, ref DateTime dateTime, ref TimeSpan totalTime)
            {
                double estTotalTime = 0;
                int binCount = binModes.Count;
                int frameCount = 10;    // number of dark frames

                // calculate total time to complete the task
                if (exposureTimes.Count == 1 && exposureTimes.Contains(0.0))
                {
                    foreach (var binning in binModes)
                    {
                        //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * frameCount;
                        //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                        estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 10;//Bias uses 10 frames (06/04/2024)
                    }
                }
                else
                {
                    for (int i = 0; i < exposureTimes.Count; i++)
                    {
                        if (exposureTimes[i] == 0.0)
                        {
                            foreach (var binning in binModes)
                            {
                                //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * frameCount;
                                //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                                estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 10;//Bias uses 10 frames (06/04/2024)
                            }
                        }
                        else
                        {
                            foreach (var binning in binModes)
                            {
                                //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                                estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 10;//Bias uses 10 frames (06/04/2024)
                                estTotalTime += ((10 / (binning.VerticalBins * binning.VerticalBins)) + exposureTimes[i]) * frameCount;
                            }
                        }
                    }
                }

                // Estimated total time to complete the task
                totalTime = TimeSpan.FromSeconds(estTotalTime);

                // Get completion date and time
                int[] daysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                DateTime currentTime = DateTime.Now;
                int month = currentTime.Month;
                int day = currentTime.Day;
                int year = currentTime.Year;
                TimeSpan timeOfDay = currentTime.TimeOfDay;
                TimeSpan estCompletionTime = timeOfDay.Add(totalTime);

                if (estCompletionTime.Days > 0)
                {
                    day += estCompletionTime.Days;

                    if (day > daysInMonth[month])
                    {
                        if (IsLeapYear(year) && month == 2)
                        {
                            if (day > daysInMonth[month] + 1)
                            {
                                day -= (daysInMonth[month] + 1);
                                month++;
                            }
                        }
                        else
                        {
                            day -= daysInMonth[month];
                            month++;
                            if (month > 12)
                            {
                                month = 1;
                                year++;
                            }
                        }
                    }
                }

                dateTime = new DateTime(year, month, day, estCompletionTime.Hours, estCompletionTime.Minutes, estCompletionTime.Seconds);

            }
            #endregion

            public static void CalculateCompletionTime(List<BinningFactorType> binModes, List<double> exposureTimes, ref DateTime dateTime, ref TimeSpan totalTime, int frameCount, bool bIsFastReadout)
            {
                double estTotalTime = 0;
                int binCount = binModes.Count;
                double cameraDownloadTime = 10.0;

                if (bIsFastReadout)
                {
                    cameraDownloadTime = 10.0;
                }
                else
                {
                    cameraDownloadTime = 1.5;
                }

                // calculate total time to complete the task
                if (exposureTimes.Count == 1 && exposureTimes.Contains(0.0))
                {
                    foreach (var binning in binModes)
                    {
                        estTotalTime += (cameraDownloadTime / (binning.VerticalBins * binning.VerticalBins)) * frameCount;
                        //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                    }
                }
                else
                {
                    for (int i = 0; i < exposureTimes.Count; i++)
                    {
                        if (exposureTimes[i] == 0.0)
                        {
                            foreach (var binning in binModes)
                            {
                                estTotalTime += (cameraDownloadTime / (binning.VerticalBins * binning.VerticalBins)) * frameCount;
                                //estTotalTime += (10 / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                            }
                        }
                        else
                        {
                            foreach (var binning in binModes)
                            {
                                estTotalTime += (cameraDownloadTime / (binning.VerticalBins * binning.VerticalBins)) * 100; // Bias uses 100 frames (02/16/2016)
                                estTotalTime += ((cameraDownloadTime / (binning.VerticalBins * binning.VerticalBins)) + exposureTimes[i]) * frameCount;
                            }
                        }
                    }
                }

                // Estimated total time to complete the task
                totalTime = TimeSpan.FromSeconds(estTotalTime);

                // Get completion date and time
                int[] daysInMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                DateTime currentTime = DateTime.Now;
                int month = currentTime.Month;
                int day = currentTime.Day;
                int year = currentTime.Year;
                TimeSpan timeOfDay = currentTime.TimeOfDay;
                TimeSpan estCompletionTime = timeOfDay.Add(totalTime);

                if (estCompletionTime.Days > 0)
                {
                    day += estCompletionTime.Days;

                    if (day > daysInMonth[month])
                    {
                        if (IsLeapYear(year) && month == 2)
                        {
                            if (day > daysInMonth[month] + 1)
                            {
                                day -= (daysInMonth[month] + 1);
                                month++;
                            }
                        }
                        else
                        {
                            day -= daysInMonth[month];
                            month++;
                            if (month > 12)
                            {
                                month = 1;
                                year++;
                            }
                        }
                    }
                }

                dateTime = new DateTime(year, month, day, estCompletionTime.Hours, estCompletionTime.Minutes, estCompletionTime.Seconds);

            }

            #region private static bool IsLeapYear(int year)
            /// <summary>
            /// Check if the year is a leap year
            /// </summary>
            /// <param name="year"></param>
            /// <returns></returns>
            private static bool IsLeapYear(int year)
            {
                bool leap = (((year % 4 == 0) && ((year % 100 != 0)
                    || (year % 400 == 0))) ? true : false);

                return (leap);
            }
            #endregion
        }
        #endregion
    }
}
