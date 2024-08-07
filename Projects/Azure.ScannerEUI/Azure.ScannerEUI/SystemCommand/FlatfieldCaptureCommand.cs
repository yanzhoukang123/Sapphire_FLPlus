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
using System.Windows;
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
                double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;
                if (_ImageChannel.IsAutoExposure)
                {
                    _ImageChannel.Exposure = ImagingSystemHelper.ChemiSOLO_CalculateFlatImageAutoExposure(_ActiveCamera, _CommController, _ImageChannel);
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
                darkCorrectedImage = wbCapturedBitmap;
                // 这个方法中减去bias图像和应用了DarkCorrection
                //darkCorrectedImage = Workspace.This.MasterLibrary.ChemiSOLO_CalculateCorrectedImage(
                //                            wbCapturedBitmap,
                //                            _ImageChannel.Exposure,
                //                            _ImageChannel.BinningMode,
                //                            out darkCorrection);

                //该方法中只减去了bias图像
                darkCorrectedImage = Workspace.This.MasterLibrary.ChemiSOLO_CalculateCorrectedImage_SubtractBias(
                                            wbCapturedBitmap,
                                            _ImageChannel.Exposure,
                                            _ImageChannel.BinningMode,
                                            out darkCorrection);

                //应用了过滤方法
                // Resize the original image before apply the filter
                //int iOrigWidth = darkCorrectedImage.PixelWidth;
                //int iOrigHeight = darkCorrectedImage.PixelHeight;
                //int iWidth = iOrigWidth / 2;
                //int iHeight = iOrigHeight / 2;

                //Size newImageSize = new Size(iWidth, iHeight);
                //WriteableBitmap flatImage = ImageProcessing.Resize(darkCorrectedImage, newImageSize);

                //// Apply gaussian filtering on the image with 40x40 kernel
                //System.Drawing.Size kernelSize = new System.Drawing.Size(40, 40);
                //FastGaussianFilter gaussianFilter = new FastGaussianFilter(kernelSize);
                //gaussianFilter.Apply(ref flatImage);

                //// Resize to the original size
                //newImageSize = new Size(iOrigWidth, iOrigHeight);
                //FlatImage = ImageProcessing.Resize(flatImage, newImageSize);

                //直接返回减去bias图像的结果图像
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
