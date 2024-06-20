using Azure.CameraLib;
using Azure.CommandLib;
using Azure.Configuration.Settings;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.ScannerEUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Azure.ScannerEUI.SystemCommand
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
                double us = 0;
                if (_ImageChannel.IsAutoExposure)
                {
                    _ImageChannel.Exposure = ImagingSystemHelper.ChemiSOLO_CalculateFlatImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel);
                    us = _ActiveCamera.USConvertMS;  //ms 
                }
                else
                {
                    us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS; //sec
                }
                if (_ActiveCamera.SetExpoTime((uint)(_ImageChannel.Exposure * us)))
                {
                    _ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                }
                if (_IsCommandAborted || wbCapturedBitmap == null)
                {
                    if (_IsCommandAborted)
                    {
                        this.ExitStat = ThreadExitStat.Abort;
                    }
                    return;
                }
                WriteableBitmap darkCorrectedImage = null;
                if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                {
                    darkCorrectedImage = wbCapturedBitmap;  // PvCam currently using the camera's dynamic dark correction.
                }
                else
                {
                    // Apply bias and darkmaster correction (darkmaster is not applied if exposure time is less than 1 second)
                    darkCorrectedImage = Workspace.This.MasterLibrary.ChemiSOLO_CalculateCorrectedImage(
                                                wbCapturedBitmap,
                                                _ImageChannel.Exposure,
                                                _ImageChannel.BinningMode,
                                                out darkCorrection);
                }
                FlatImage = darkCorrectedImage;
                if (FlatImage.CanFreeze) { FlatImage.Freeze(); }

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
