using Azure.CameraLib;
using Azure.CommandLib;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.Ipp.Imaging;
using Azure.ScannerEUI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Azure.ScannerEUI.SystemCommand
{
    public class DarkmasterCaptureCommand : ThreadBase
    {
        // Progress update delegate
        public delegate void ProgressChangedHandler(object sender, int maxFrames, int currentFrame);
        // image capture completed event
        public event ProgressChangedHandler ProgressChanged;


        #region Private fields/data

        private Dispatcher _CallingDispatcher = null;
        private CameraController _ActiveCamera = null;
        private EthernetController _CommController;
        private int Width;
        private int Height;
        private bool SingeCapture = false;

        private  int _DarkFrameCount = 10;
        private  int _BiasFrameCount = 10;
        private List<double> _ExposureTimes = null;
        private List<BinningFactorType> _BinModes = null;
        private string _TargetDirectory = string.Empty;
        private bool _IsCaptureBiasImages = false;
        private bool _IsCommandAborted = false;

        #endregion


        public DarkmasterCaptureCommand(Dispatcher callingDispatcher,
                                        CameraController camera,
                                        EthernetController ethernet,
                                        List<BinningFactorType> binModes,
                                        List<double> exposureTimes,
                                        string targetDirectory,
                                        int framecount)
        {
            _CommController = ethernet;
            this._CallingDispatcher = callingDispatcher;
            this._ActiveCamera = camera;
            this._BinModes = binModes;
            this._ExposureTimes = exposureTimes;
            this._TargetDirectory = targetDirectory;
            if (framecount != 0)
            {
                _DarkFrameCount = framecount;
            }
            if (_ExposureTimes.Contains(0.0))
            {
                _IsCaptureBiasImages = true;
            }

            if (!Directory.Exists(_TargetDirectory))
            {
                Directory.CreateDirectory(_TargetDirectory);
            }
        }
        public override void Finish()
        {
            if (_IsCommandAborted)
            {
                if (_ActiveCamera != null)
                {
                    _ActiveCamera.ChangeTriggerMode(0);
                }
            }
        }
        public override void AbortWork()
        {
            _IsCommandAborted = true;
        }

        public override void ThreadFunction()
        {
            ImageArithmetic imageArith = new ImageArithmetic();
            string fileName = string.Empty;
            WriteableBitmap biasImage = null;
            WriteableBitmap darkmasterImage = null;
            WriteableBitmap biasCorrectedImage = null;
            Width = _ActiveCamera.CaptureImage_Width;
            Height = _ActiveCamera.CaptureImage_Height;
            try
            {
                foreach (var binMode in _BinModes)
                {
                    fileName = "Bias_" + binMode.DisplayName + ".tif";
                    string biasFilePath = Path.Combine(_TargetDirectory, fileName);
                    if (_IsCaptureBiasImages || !File.Exists(biasFilePath))
                    {
                        biasImage = CaptureBiasImage(biasFilePath);
                    }
                    else
                    {
                        biasImage = ImageProcessing.LoadImageFile(biasFilePath);
                    }

                    if (biasImage == null && !_IsCommandAborted)
                    {
                        throw new Exception("Error retrieving bias image file.");
                    }
                    #region === Create darkmasters ===
                    foreach (double exposureTime in _ExposureTimes)
                    {
                        if (exposureTime == 0.0)
                        {
                            continue;
                        }
                        darkmasterImage = CaptureDarkmaster(exposureTime);
                        if (darkmasterImage != null)
                        {
                            //darkmasterImage = CaptureDarkmaster(binMode.VerticalBins, exposureTime);
                            biasCorrectedImage = imageArith.SubtractImage(darkmasterImage, biasImage);

                            //
                            // Save bias corrected darkmaster image:
                            //
                            fileName = "Dark_" + binMode.DisplayName + "_" + exposureTime.ToString() + ".tif";
                            string darkmasterFilePath = Path.Combine(_TargetDirectory, fileName);

                            if (File.Exists(darkmasterFilePath))
                            {
                                try
                                {
                                    File.Delete(darkmasterFilePath);
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(e.Message);
                                }
                            }

                            using (System.IO.FileStream fileStream = new System.IO.FileStream(darkmasterFilePath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                            {
                                ImageProcessing.Save(fileStream, ImageProcessing.TIFF_FILE, biasCorrectedImage);
                            }
                        }
                    }
                    #endregion
                    biasImage = null;
                    darkmasterImage = null;
                    biasCorrectedImage = null;
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                //Ignore
            }
            catch (Exception ex)
            {
                if (!_IsCommandAborted)
                {
                    _CallingDispatcher.Invoke((Action)delegate
                    {
                        string caption = "Generate darkmaster error....";
                        MessageBox.Show(Application.Current.MainWindow, ex.Message, caption, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    });
                    throw;
                }
            }

        }

        /// <summary>
        /// Create bias image and save it to a file.
        /// </summary>
        /// <returns></returns>
        private unsafe WriteableBitmap CaptureBiasImage(string biasFilePath)
        {
            double dExposureTime = 0.5;  //ms
            WriteableBitmap biasImageBuffer = null;
            WriteableBitmap biasImage = null;
            float[] floatImageBuffer = new float[Width * Height];

            try
            {
                // Update progress
                if (ProgressChanged != null)
                {
                    ProgressChanged(this, _BiasFrameCount, 0);
                }

                for (int i = 0; i < _BiasFrameCount; ++i)
                {

                    //_ActiveCamera.GrabImage(dExposureTime, CaptureFrameType.Dark, ref biasImageBuffer);
                    // Grab an image from the camera
                    if (_ActiveCamera.SetExpoTime((uint)(dExposureTime * _ActiveCamera.USConvertMS)))
                    {
                        _ActiveCamera.CapturesImage(ref biasImageBuffer);
                    }

                    if (biasImageBuffer != null)
                    {
                        byte* pBiasImageBuffer = (byte*)biasImageBuffer.BackBuffer.ToPointer();
                        fixed (float* pImageBuffer = &floatImageBuffer[0])
                        {
                            if (i == 0)
                            {
                                ImagingSystemHelper.AddDivided16uTo32f(pBiasImageBuffer, null, pImageBuffer, Width, Height, _BiasFrameCount);
                            }
                            else
                            {
                                ImagingSystemHelper.AddDivided16uTo32f(pBiasImageBuffer, pImageBuffer, pImageBuffer, Width, Height, _BiasFrameCount);
                            }
                        }
                        biasImageBuffer = null;
                    }

                    // Update progress
                    if (ProgressChanged != null)
                    {
                        ProgressChanged(this, _BiasFrameCount, i);
                    }
                }

                biasImage = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Gray16, null);
                byte* pBiasImage = (byte*)biasImage.BackBuffer.ToPointer();
                int iSrcStride = Width * sizeof(float);
                int iDstStride = (Width * 16 + 7) / 8;
                fixed (float* pImageBuffer = &floatImageBuffer[0])
                {
                    IppImaging.Convert_32f16u_C1R(pImageBuffer, iSrcStride, pBiasImage, iDstStride, Width, Height);
                }
                using (System.IO.FileStream fileStream = new System.IO.FileStream(biasFilePath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                {
                    ImageProcessing.Save(fileStream, ImageProcessing.TIFF_FILE, biasImage);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error generating darkmaster.\n{0}", ex.Message));
            }
            finally
            {
                floatImageBuffer = null;
            }

            return biasImage;
        }

        private unsafe WriteableBitmap CaptureDarkmaster(double exposureTime)
        {
            WriteableBitmap darkImageBuffer = null;
            WriteableBitmap darkImage = null;
            float[] floatImageBuffer = new float[Width * Height];

            try
            {
                // Update progress
                if (ProgressChanged != null)
                {
                    ProgressChanged(this, _DarkFrameCount, 0);
                }

                for (int i = 0; i < _DarkFrameCount; ++i)
                {
                    if (_IsCommandAborted) { return null; }

                    // Grab an image from the camera
                    //将秒转为微秒
                    double us = _ActiveCamera.USConvertMS * _ActiveCamera.USConvertMS;
                    if (_ActiveCamera.SetExpoTime((uint)(exposureTime * us)))
                    {
                        _ActiveCamera.CapturesImage(ref darkImageBuffer);
                    }
                    if (darkImageBuffer != null)
                    {
                        byte* pDarkImageBuffer = (byte*)darkImageBuffer.BackBuffer.ToPointer();
                        fixed (float* pImageBuffer = floatImageBuffer)
                        {
                            if (i == 0)
                            {
                                ImagingSystemHelper.AddDivided16uTo32f(pDarkImageBuffer, null, pImageBuffer, Width, Height, _DarkFrameCount);
                            }
                            else
                            {
                                ImagingSystemHelper.AddDivided16uTo32f(pDarkImageBuffer, pImageBuffer, pImageBuffer, Width, Height, _DarkFrameCount);
                            }
                        }
                        darkImageBuffer = null;

                        // Update progress
                        if (ProgressChanged != null)
                        {
                            ProgressChanged(this, _DarkFrameCount, i);
                        }
                    }
                }

                if (_IsCommandAborted) { return null; }

                darkImage = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Gray16, null);
                byte* pDarkImage = (byte*)darkImage.BackBuffer.ToPointer();
                int iSrcStride = Width * sizeof(float);
                int iDstStride = (Width * 16 + 7) / 8;
                fixed (float* pImageBuffer = &floatImageBuffer[0])
                {
                    IppImaging.Convert_32f16u_C1R(pImageBuffer, iSrcStride, pDarkImage, iDstStride, Width, Height);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error generating darkmaster.\n{0}", ex.Message));
            }
            finally
            {
                floatImageBuffer = null;
            }

            return darkImage;
        }





















    }
}
