using Azure.CameraLib;
using Azure.CommandLib;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Azure.ImagingSystem
{
    public class FlatfieldCaptureCommand : ThreadBase
    {
        #region Fields/data...

        private Dispatcher _CallingDispatcher = null;
        private CameraController _ActiveCamera = null;
        private EthernetController _CommController = null;
        private ImageChannelSettings _ImageChannel = null;
        private string _TargetPath = string.Empty;
        private bool _IsCommandAborted = false;

        #endregion

        #region Constructors...

        public FlatfieldCaptureCommand(Dispatcher callingDispatcher,
                                       CameraController camera,
                                       EthernetController ethernet,
                                       ImageChannelSettings imageChannel,
                                       string targetPath)
        {
            this._CallingDispatcher = callingDispatcher;
            this._ActiveCamera = camera;
            this._CommController = ethernet;
            this._ImageChannel = imageChannel;
            this._TargetPath = targetPath;
        }

        public WriteableBitmap FlatImage { get; set; }

        public override void Initialize()
        {
        }
        public override void Finish()
        {
            if (_ActiveCamera != null)
            {
                _ActiveCamera.ChangeTriggerMode(0);
            }
        }
        public override void AbortWork()
        {
            _IsCommandAborted = true;
        }

        #endregion

        public override void ThreadFunction()
        {
            string darkCorrection = string.Empty;
            ImageArithmetic imageArith = new ImageArithmetic();
            WriteableBitmap wbCapturedBitmap = null;

            try
            {
                if (_ImageChannel.IsAutoExposure)
                {
                    _ImageChannel.Exposure = ImagingSystemHelper.CalculateAutoExposure(_ActiveCamera,_CommController, _ImageChannel);
                }
            }
            catch (System.Threading.ThreadAbortException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
