using Azure.Configuration.Settings;
using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Azure.ScannerEUI.ViewModel
{
    class MultipleExposureViewModel : ViewModelBase
    {

        #region Private data...
        private int _FrameCount = 1;
        private int _SelectedFrame = 1;
        private double _ExposureTime = 0.0;   // exposure time in seconds
        private int _ExposureMin = 0;
        private int _ExposureSec = 0;
        private int _ExposureMsec = 0;
        private List<double> _MultipleExposureList = null;
        #endregion
        public MultipleExposureViewModel()
        {
            _MultipleExposureList = new List<double>();
            _MultipleExposureList.Add(0.0);
        }

        #region Public data
        public double ExposureTime
        {
            get { return _ExposureTime; }
            set
            {
                _ExposureTime = value;
                TimeSpan timeSpan = TimeSpan.FromSeconds(_ExposureTime);
                ExposureMin = timeSpan.Minutes;
                ExposureSec = timeSpan.Seconds;
                ExposureMsec = timeSpan.Milliseconds;
            }
        }

        public int ExposureMin
        {
            get { return _ExposureMin; }
            set
            {
                _ExposureMin = value;
                RaisePropertyChanged("ExposureMin");
                _ExposureTime = (double)_ExposureMsec / 1000.0 + (double)_ExposureSec + (double)_ExposureMin * 60.0;
                _MultipleExposureList[_SelectedFrame - 1] = _ExposureTime;
            }
        }

        public int ExposureSec
        {
            get { return _ExposureSec; }
            set
            {
                _ExposureSec = value;
                RaisePropertyChanged("ExposureSec");
                _ExposureTime = (double)_ExposureMsec / 1000.0 + (double)_ExposureSec + (double)_ExposureMin * 60.0;
                _MultipleExposureList[_SelectedFrame - 1] = _ExposureTime;
            }
        }

        public int ExposureMsec
        {
            get { return _ExposureMsec; }
            set
            {
                _ExposureMsec = value;
                RaisePropertyChanged("ExposureMsec");
                _ExposureTime = (double)_ExposureMsec / 1000.0 + (double)_ExposureSec + (double)_ExposureMin * 60.0;
                _MultipleExposureList[_SelectedFrame - 1] = _ExposureTime;
            }
        }

        public int FrameCount
        {
            get { return _FrameCount; }
            set
            {
                if (_FrameCount != value)
                {
                    _FrameCount = value;
                    RaisePropertyChanged("FrameCount");

                    if (_FrameCount == 1)
                    {
                        SelectedFrame = 1;
                    }
                }
            }
        }

        public int SelectedFrame
        {
            get { return _SelectedFrame; }
            set
            {
                if (_SelectedFrame != value)
                {
                    _SelectedFrame = value;
                    RaisePropertyChanged("SelectedFrame");
                }
            }
        }

        public List<double> MultipleExposureList
        {
            get { return _MultipleExposureList; }
        }

        #endregion

        #region FrameIncrCommand
        private RelayCommand _FrameIncrCommand = null;
        /// <summary>
        /// Get the frame increment command.
        /// </summary>
        public ICommand FrameIncrCommand
        {
            get
            {
                if (_FrameIncrCommand == null)
                {
                    _FrameIncrCommand = new RelayCommand(ExecuteFrameIncrCommand, CanExecuteFrameIncrCommand);
                }

                return _FrameIncrCommand;
            }
        }

        /// <summary>
        /// Increment frame count command implementation.
        /// </summary>
        /// <param name="parameter"></param>
        protected void ExecuteFrameIncrCommand(object parameter)
        {
            if (_FrameCount < SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.NumFrames)
            {
                _MultipleExposureList.Add(0.0);
                FrameCount++;
            }
        }
        protected bool CanExecuteFrameIncrCommand(object parameter)
        {
            return (_FrameCount < SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.NumFrames);
        }
        #endregion

        #region FrameDecrCommand
        private RelayCommand _FrameDecrCommand = null;
        /// <summary>
        /// Get the frame decrement command.
        /// </summary>
        public ICommand FrameDecrCommand
        {
            get
            {
                if (_FrameDecrCommand == null)
                {
                    _FrameDecrCommand = new RelayCommand(ExecuteFrameDecrCommand, CanExecuteFrameDecrCommand);
                }

                return _FrameDecrCommand;
            }
        }
        /// <summary>
        /// Decrement frame count command implementation.
        /// </summary>
        /// <param name="parameter"></param>
        protected void ExecuteFrameDecrCommand(object parameter)
        {
            if (_FrameCount > 1)
            {
                FrameCount--;

                if (SelectedFrame > FrameCount)
                {
                    SelectedFrame = FrameCount;
                    ExposureTime = _MultipleExposureList[_SelectedFrame - 1];  // FrameCount is 1 index
                }

                _MultipleExposureList.RemoveAt(FrameCount);    // FrameCount is 1 index
            }
        }
        protected bool CanExecuteFrameDecrCommand(object parameter)
        {
            return (_FrameCount > 1);
        }
        #endregion

        #region FrameLeftCommand
        private RelayCommand _FrameLeftCommand = null;
        /// <summary>
        /// Get the frame selection command.
        /// </summary>
        public ICommand FrameLeftCommand
        {
            get
            {
                if (_FrameLeftCommand == null)
                {
                    _FrameLeftCommand = new RelayCommand(ExecuteFrameLeftCommand, CanExecuteFrameLeftCommand);
                }
                return _FrameLeftCommand;
            }
        }
        /// <summary>
        /// Frame selection command implementation.
        /// </summary>
        /// <param name="parameter"></param>
        protected void ExecuteFrameLeftCommand(object parameter)
        {
            if (_SelectedFrame > 1)
            {
                /*if (_MultipleExposureList[_SelectedFrame - 1] == 0.0)
                {
                    double minExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MinExposure;
                    double maxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
                    string strCaption = "Invalid entry....";
                    string strMessage = string.Format("Invalid exposure time.\nPlease enter the value between {0} sec and {1} min.",
                         minExposure, (maxExposure / 60.0));
                    Xceed.Wpf.Toolkit.MessageBox.Show(strMessage, strCaption, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }*/
                SelectedFrame--;
                ExposureTime = _MultipleExposureList[_SelectedFrame - 1];

                //SelectedFrame--;
                //_NumKeypad.KeypadSetting = _MultipleExposureList[_SelectedFrame - 1];
                //RaisePropertyChanged("ExposureTime");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected bool CanExecuteFrameLeftCommand(object parameter)
        {
            return (_SelectedFrame > 1);
        }
        #endregion

        #region FrameRightCommand
        private RelayCommand _FrameRightCommand = null;
        /// <summary>
        /// Get the frame selection command.
        /// </summary>
        public ICommand FrameRightCommand
        {
            get
            {
                if (_FrameRightCommand == null)
                {
                    _FrameRightCommand = new RelayCommand(ExecuteFrameRightCommand, CanExecuteFrameRightCommand);
                }
                return _FrameRightCommand;
            }
        }
        /// <summary>
        /// Frame selection command implementation.
        /// </summary>
        /// <param name="parameter"></param>
        protected void ExecuteFrameRightCommand(object parameter)
        {
            if (_SelectedFrame < _FrameCount)
            {
                /*if (_MultipleExposureList[_SelectedFrame - 1] == 0.0)
                {
                    double minExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MinExposure;
                    double maxExposure = SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure;
                    string strCaption = "Invalid entry....";
                    string strMessage = string.Format("Invalid exposure time.\nPlease enter the value between {0} sec and {1} min.", minExposure, (maxExposure / 60.0));
                    Xceed.Wpf.Toolkit.MessageBox.Show(strMessage, strCaption, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }*/
                SelectedFrame++;
                ExposureTime = _MultipleExposureList[_SelectedFrame - 1];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected bool CanExecuteFrameRightCommand(object parameter)
        {
            return (_SelectedFrame < _FrameCount);
        }
        #endregion
    }
}
