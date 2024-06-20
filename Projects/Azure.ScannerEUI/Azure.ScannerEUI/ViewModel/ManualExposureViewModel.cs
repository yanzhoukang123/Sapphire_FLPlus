using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ScannerEUI.ViewModel
{
    class ManualExposureViewModel : ViewModelBase
    {
        #region Private data...
        private double _ExposureTime = 0.0;   // exposure time in seconds
        private int _ExposureMin = 0;
        private int _ExposureSec = 0;
        private int _ExposureMsec = 0;
        #endregion

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
            }
        }
        #endregion
    }
}
