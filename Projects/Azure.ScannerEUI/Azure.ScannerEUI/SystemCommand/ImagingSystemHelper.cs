using Azure.CameraLib;
using Azure.Configuration.Settings;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Azure.Ipp.Imaging;
using Azure.ScannerEUI.ViewModel;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Azure.ScannerEUI.SystemCommand
{
    public class ImagingSystemHelper
    {
        public static unsafe IppStatus AddDivided16uTo32f(byte* pSrc1Data, float* pSrc2Data, float* pDstData, int width, int height, int divisorConstant)
        {
            IppStatus ippStat = IppStatus.ippStsNoErr;
            IppiSize roiSize = new IppiSize(width, height);
            float[] tempSrc1Data = null;
            int dstStep = width * sizeof(float);
            int srcStep = width * sizeof(byte) * 2;

            try
            {
                tempSrc1Data = new float[width * height];

                fixed (float* pTempSrc1Data = tempSrc1Data)
                {
                    if (pSrc1Data != null)
                    {
                        // pSrc1Data: byte to float (result in: pTempSrc1Data)
                        ippStat = IppImaging.Convert_16u32f_C1R(pSrc1Data, srcStep, pTempSrc1Data, dstStep, roiSize);
                        if (ippStat != IppStatus.ippStsNoErr)
                        {
                            return ippStat;
                        }
                    }

                    if (pSrc2Data == null)
                    {
                        ippStat = IppImaging.DivC_32f_C1R(pTempSrc1Data, dstStep, divisorConstant, pDstData, dstStep, roiSize);
                        if (ippStat != IppStatus.ippStsNoErr)
                        {
                            return ippStat;
                        }
                    }
                    else
                    {
                        ippStat = IppImaging.DivC_32f_C1R(pTempSrc1Data, dstStep, divisorConstant, pTempSrc1Data, dstStep, roiSize);
                        if (ippStat != IppStatus.ippStsNoErr)
                        {
                            return ippStat;
                        }

                        //add lSrcData2 and lDstData to lDstData
                        if (pSrc2Data != null)
                        {
                            //ippStat = IppImaging.Add_32f_C1R(pSrc2Data, dstStep, pDstData, dstStep, pDstData, dstStep, roiSize);
                            ippStat = IppImaging.Add_32f_C1R(pTempSrc1Data, dstStep, pSrc2Data, dstStep, pDstData, dstStep, roiSize);
                            if (ippStat != IppStatus.ippStsNoErr)
                            {
                                return ippStat;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error: AddDivided16uTo32f", ex);
            }
            finally
            {
                tempSrc1Data = null;

                // Forces an immediate garbage collection.
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.WaitForPendingFinalizers();
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }

            return ippStat;
        }

        public static unsafe WriteableBitmap ImageDivAndScale(WriteableBitmap srcImage, WriteableBitmap flatImage, int upperLimit, int offsetX = 0, int offsetY = 0)
        {
            if (srcImage == null || flatImage == null)
                return null;

            if (IppImaging.GetPixelFormatType(srcImage.Format) != PixelFormatType.P16u_C1)
            {
                throw new Exception("Source image format not supported");
            }

            float[] srcCvtData = null;
            float[] flatCvtData = null;
            float[] dst32fData = null;

            try
            {
                // Reserve the back buffer for updates.
                if (!srcImage.IsFrozen)
                    srcImage.Lock();
                if (!flatImage.IsFrozen)
                    flatImage.Lock();

                int srcWidth = srcImage.PixelWidth;
                int srcHeight = srcImage.PixelHeight;
                int bitsPerPixel = srcImage.Format.BitsPerPixel;

                byte* pSrc = (byte*)srcImage.BackBuffer.ToPointer();
                byte* pFlat = (byte*)flatImage.BackBuffer.ToPointer();
                int srcStep = srcImage.BackBufferStride;
                int flatStep = flatImage.BackBufferStride;

                //float* pSrcCvt = null;
                //float* pFlatCvt = null;
                //float* pDst32f = null;
                int srcCvtStep = srcWidth * sizeof(float);
                int flatCvtStep = srcCvtStep;
                int dst32fStep = srcCvtStep;
                IppiSize roiSize = new IppiSize(srcWidth, srcHeight);

                srcCvtData = new float[srcWidth * srcHeight];
                flatCvtData = new float[srcWidth * srcHeight];
                dst32fData = new float[srcWidth * srcHeight];

                //fixed (float* p = srcCvtData) pSrcCvt = p;
                //fixed (float* p = flatCvtData) pFlatCvt = p;
                //fixed (float* p = dst32fData) pDst32f = p;
                fixed (float* pSrcCvt = srcCvtData, pFlatCvt = flatCvtData, pDst32f = dst32fData)
                {
                    IppImaging.Convert_16u32f_C1R(pSrc, srcStep, pSrcCvt, srcCvtStep, new IppiSize(srcWidth, srcHeight));
                    IppImaging.Convert_16u32f_C1R(pFlat, flatStep, pFlatCvt, flatCvtStep, new IppiSize(srcWidth, srcHeight));

                    IppImaging.Div_32f_C1R(pFlatCvt, flatCvtStep, pSrcCvt, srcCvtStep, pDst32f, dst32fStep, roiSize);

                    float pixelMax = 0;

                    IppiSize offsettedRoiSize = new IppiSize(srcWidth - (2 * offsetX), srcHeight - (2 * offsetY));
                    IppImaging.Max(pDst32f + ((srcWidth * offsetY) + offsetX), dst32fStep, offsettedRoiSize, ref pixelMax);
                    float scaledFactor = (float)upperLimit / pixelMax;

                    IppImaging.MulC(pDst32f, dst32fStep, scaledFactor, pDst32f, dst32fStep, roiSize);

                    IppImaging.Convert_32f16u_C1R(pDst32f, dst32fStep, pSrc, srcStep, srcWidth, srcHeight);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // Release the back buffer and make it available for display.
                if (!srcImage.IsFrozen)
                    srcImage.Unlock();
                if (!flatImage.IsFrozen)
                    flatImage.Unlock();

                srcCvtData = null;
                flatCvtData = null;
                dst32fData = null;

                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.WaitForPendingFinalizers();
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.Collect();
            }

            return srcImage;
        }

        public static unsafe void ChemiSOLO_ImageDivAndScale(WriteableBitmap srcImage, WriteableBitmap  flatImage, out float[] flatImageMatrix)
        {
            srcImage.Lock();
            flatImage.Lock();
            int imgHeight = srcImage.PixelHeight;
            int imgWidth = srcImage.PixelWidth;
            ushort* srcimgData = (ushort*)srcImage.BackBuffer.ToPointer();
            ushort* flatimgData = (ushort*)flatImage.BackBuffer.ToPointer();
            flatImageMatrix = new float[imgHeight * imgWidth];
            fixed (float* ptr = flatImageMatrix)
            {
                for (int i = 0; i < imgHeight; i++)
                {
                    for (int j = 0; j < imgWidth; j++)
                    {
                        int address = (i * (imgWidth)) + j;
                        float srciResult = *(srcimgData + address);
                        float flatiResult = *(flatimgData + address);
                        float offsettedRoiSize = srciResult / flatiResult;
                        *(ptr + address) = offsettedRoiSize; 
                    }
                }
            }
            if (!srcImage.IsFrozen)
                srcImage.Unlock();
            if (!flatImage.IsFrozen)
                flatImage.Unlock();
        }

        public static double CalculateFlatImageAutoExposure(CameraController ActiveCamera, EthernetController CommController, ImageChannelSettings imageChannelSettings)
        {
            double exposureTime = 1.0;          // Initial exposure
            double initialExposureTime = exposureTime;
            double maxPixelValue = 0;
            double upperCeiling = 55000;    // Default upper ceiling   55000/65535*16383=13749
            double chemiMaximumExposure = imageChannelSettings.MaxExposure;
            ImageArithmetic imgArith = new ImageArithmetic();
            double signalTooHighTestExposure = exposureTime / 10.0;
            double signalTooWeakTestExposure = exposureTime * 4.0;
            ImageStatistics imageStats = new ImageStatistics();
            double minexposuretime = (double)ActiveCamera.ExposureTime_MIN;
            if (imageChannelSettings.AutoExposureUpperCeiling > 0 &&
                imageChannelSettings.AutoExposureUpperCeiling != upperCeiling)
            {
                upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
            }
            // Chemi: Light source = None
            if (imageChannelSettings.LightType == 0)
            {
                //if (imageChannelSettings.AutoExposureUpperCeiling > 0)
                //{
                //    upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
                //}
            }
            else
            {
                if (imageChannelSettings.LightType == 1 ||
                    imageChannelSettings.LightType == 2 ||
                    imageChannelSettings.LightType == 3 ||
                    imageChannelSettings.LightType == 4)
                {

                    exposureTime = initialExposureTime;    // Initial exposure for R/G/B light source
                }

                signalTooHighTestExposure = Math.Round((exposureTime / 10.0), 5);
                if (signalTooHighTestExposure < minexposuretime) //0.05ms
                    signalTooHighTestExposure = minexposuretime;
                signalTooWeakTestExposure = Math.Round((exposureTime * 30), 3);
            }
            WriteableBitmap wbCapturedBitmap = null;
            try
            {
                while (true)
                {
                    if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                    {
                        ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                    }
                    WriteableBitmap biasImage = null;
                    if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                    {
                        // PvCam: using dynamic dark correction
                    }
                    else
                    {
                        biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                        if (biasImage != null)
                        {
                            wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                        }
                        else
                        {
                            string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                            throw new Exception(strMessage);
                        }
                    }
                    int Biasmean = (int)imageStats.GetMean(wbCapturedBitmap);
                    wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                    maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                    if (maxPixelValue > upperCeiling)
                    {
                        if (exposureTime != signalTooHighTestExposure)
                        {
                            // Workspace.This.LogMessage("  AutoExposure: Signal too high");
                            exposureTime = signalTooHighTestExposure;
                            continue;
                        }
                        else
                        {
                            // Signal too strong, default to 0.05 msec for Photometrics camera.
                            exposureTime = minexposuretime;
                            break;
                        }
                    }
                    else if (maxPixelValue <= upperCeiling && maxPixelValue > 200) // 200/65535*16383=49.9977111467155,
                    {
                        exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                        exposureTime = (upperCeiling / maxPixelValue) * exposureTime;
                        if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                        {
                            exposureTime = chemiMaximumExposure;
                        }
                        break;
                    }
                    else if (maxPixelValue <= 200)
                    {
                        if (exposureTime == initialExposureTime)
                        {
                            exposureTime = signalTooWeakTestExposure;
                        }
                        wbCapturedBitmap = null;
                        if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                        {
                            ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                        }
                        if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                        {
                            // PvCam: using dynamic dark correction
                        }
                        else
                        {
                            biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                            if (biasImage != null)
                            {
                                wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                            }
                            else
                            {
                                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                                throw new Exception(strMessage);
                            }
                        }
                        wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                        maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                        if (maxPixelValue > 200)
                        {
                            int binningMode = imageChannelSettings.BinningMode;
                            exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                            if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                            {
                                exposureTime = chemiMaximumExposure;
                            }
                            break;
                        }
                        else if (maxPixelValue <= 200)
                        {
                            throw new Exception("AE ERROR: Signal too weak!");
                        }
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                exposureTime = minexposuretime; //ms
                throw;
            }
            catch (Exception ex)
            {
                exposureTime = minexposuretime; //ms
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {

            }
            wbCapturedBitmap = null;
            return exposureTime;


        }

        public static double ChemiSOLO_CalculateFlatImageAutoExposure(CameraController ActiveCamera, EthernetController CommController, ImageChannelSettings imageChannelSettings)
        {
            double exposureTime = 0.05;          // Initial exposure  ms
            double initialExposureTime = exposureTime;
            double maxPixelValue = 0;
            double upperCeiling = 55000;    // Default upper ceiling  
            double chemiMaximumExposure = imageChannelSettings.MaxExposure;
            ImageArithmetic imgArith = new ImageArithmetic();
            double signalTooHighTestExposure = exposureTime / 10.0;
            double signalTooWeakTestExposure = exposureTime * 4.0;
            ImageStatistics imageStats = new ImageStatistics();
            double minexposuretime = (double)ActiveCamera.ExposureTime_MIN;
            if (imageChannelSettings.AutoExposureUpperCeiling > 0 &&
                imageChannelSettings.AutoExposureUpperCeiling != upperCeiling)
            {
                upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
            }
            // Chemi: Light source = None
            if (imageChannelSettings.LightType == 0)
            {
                //if (imageChannelSettings.AutoExposureUpperCeiling > 0)
                //{
                //    upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
                //}
            }
            else
            {
                if (imageChannelSettings.LightType == 1 ||
                    imageChannelSettings.LightType == 2 ||
                    imageChannelSettings.LightType == 3 ||
                    imageChannelSettings.LightType == 4)
                {

                    exposureTime = imageChannelSettings.InitialExposureTime;    // Initial exposure for R/G/B light source
                }

                signalTooHighTestExposure = Math.Round((exposureTime / 10.0), 5);
                if (signalTooHighTestExposure < minexposuretime) //0.05ms
                    signalTooHighTestExposure = minexposuretime;
                signalTooWeakTestExposure = Math.Round((exposureTime * 30), 3);
            }
            WriteableBitmap wbCapturedBitmap = null;
            try
            {
                while (true)
                {
                    if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                    {
                        ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                    }
                    WriteableBitmap biasImage = null;
                    if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                    {
                        // PvCam: using dynamic dark correction
                    }
                    else
                    {
                        biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                        if (biasImage != null)
                        {
                            wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                        }
                        else
                        {
                            string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                            throw new Exception(strMessage);
                        }
                    }
                    int Biasmean = (int)imageStats.GetMean(biasImage);
                    wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                    maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                    if (maxPixelValue > upperCeiling)
                    {
                        if (exposureTime != signalTooHighTestExposure)
                        {
                            // Workspace.This.LogMessage("  AutoExposure: Signal too high");
                            exposureTime = signalTooHighTestExposure;
                            continue;
                        }
                        else
                        {
                            // Signal too strong, default to 0.05 msec for Photometrics camera.
                            exposureTime = minexposuretime;
                            break;
                        }
                    }
                    else if (maxPixelValue <= upperCeiling && maxPixelValue > 200) 
                    {
                        exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                        //exposureTime = (upperCeiling / maxPixelValue) * exposureTime;
                        if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                        {
                            exposureTime = chemiMaximumExposure;
                        }
                        break;
                    }
                    else if (maxPixelValue <= 200)
                    {
                        if (exposureTime == initialExposureTime)
                        {
                            exposureTime = signalTooWeakTestExposure;

                        }

                        wbCapturedBitmap = null;
                        if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                        {
                            ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                        }
                        if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                        {
                            // PvCam: using dynamic dark correction
                        }
                        else
                        {
                            biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                            if (biasImage != null)
                            {
                                wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                            }
                            else
                            {
                                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                                throw new Exception(strMessage);
                            }
                        }
                        wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                        maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                        if (maxPixelValue > 200)
                        {
                            int binningMode = imageChannelSettings.BinningMode;
                            exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                            if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                            {
                                exposureTime = chemiMaximumExposure;
                            }
                            break;
                        }
                        else if (maxPixelValue <= 200)
                        {
                            throw new Exception("AE ERROR: Signal too weak!");
                        }

                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                exposureTime = minexposuretime; //ms
                throw;
            }
            catch (Exception ex)
            {
                exposureTime = minexposuretime; //ms
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {

            }
            wbCapturedBitmap = null;
            return exposureTime;


        }

        public static double ChemiSOLO_CalculateImageAutoExposure(CameraController ActiveCamera, EthernetController CommController, ImageChannelSettings imageChannelSettings)
        {
            double exposureTime = 1.0;          // Initial exposure  ms
            double initialExposureTime = exposureTime;
            double maxPixelValue = 0;
            double upperCeiling = 55000;    
            double chemiMaximumExposure = imageChannelSettings.MaxExposure;
            ImageArithmetic imgArith = new ImageArithmetic();
            double signalTooHighTestExposure = exposureTime / 10.0;
            double signalTooWeakTestExposure = exposureTime * 4.0;
            ImageStatistics imageStats = new ImageStatistics();
            double minexposuretime = (double)ActiveCamera.ExposureTime_MIN;
            if (imageChannelSettings.AutoExposureUpperCeiling > 0 &&
                imageChannelSettings.AutoExposureUpperCeiling != upperCeiling)
            {
                upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
            }
            // Chemi: Light source = None
            if (imageChannelSettings.LightType == 0)
            {
                //if (imageChannelSettings.AutoExposureUpperCeiling > 0)
                //{
                //    upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
                //}
                exposureTime = imageChannelSettings.ChemiInitialExposureTime;
            }
            else
            {
                if (imageChannelSettings.LightType == 1 ||
                    imageChannelSettings.LightType == 2 ||
                    imageChannelSettings.LightType == 3 ||
                    imageChannelSettings.LightType == 4)
                {

                    exposureTime = imageChannelSettings.InitialExposureTime;    // Initial exposure for R/G/B light source
                }

                signalTooHighTestExposure = Math.Round((exposureTime / 10.0), 5);
                if (signalTooHighTestExposure < minexposuretime) //0.05ms
                    signalTooHighTestExposure = minexposuretime;
                signalTooWeakTestExposure = Math.Round((exposureTime * 30), 3);
            }
            WriteableBitmap wbCapturedBitmap = null;
            try
            {
                while (true)
                {
                    if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                    {
                        ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                    }
                    WriteableBitmap biasImage = null;
                    if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                    {
                        // PvCam: using dynamic dark correction
                    }
                    else
                    {
                        biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                        if (biasImage != null)
                        {
                            wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                        }
                        else
                        {
                            string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                            throw new Exception(strMessage);
                        }
                    }
                    int Biasmean = (int)imageStats.GetMean(biasImage);
                    wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                    maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                    if (maxPixelValue > upperCeiling)
                    {
                        if (exposureTime != signalTooHighTestExposure)
                        {
                            // Workspace.This.LogMessage("  AutoExposure: Signal too high");
                            exposureTime = signalTooHighTestExposure;
                            continue;
                        }
                        else
                        {
                            // Signal too strong, default to 0.05 msec for Photometrics camera.
                            exposureTime = minexposuretime;
                            break;
                        }
                    }
                    else if (maxPixelValue <= upperCeiling && maxPixelValue > 200) 
                    {
                        exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                        //exposureTime = (upperCeiling / maxPixelValue) * exposureTime;
                        if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                        {
                            exposureTime = chemiMaximumExposure;
                        }
                        if (imageChannelSettings.LightType == 0)
                        {
                            if (imageChannelSettings.BinningMode == 2)
                                exposureTime = exposureTime / 4;
                            else if (imageChannelSettings.BinningMode == 3)
                                exposureTime = exposureTime / 9;
                            else if (imageChannelSettings.BinningMode == 4)
                                exposureTime = exposureTime / 16;
                            exposureTime = Math.Round(exposureTime, 3);
                        }
                        break;
                    }
                    else if (maxPixelValue <= 200)
                    {
                        if (exposureTime == initialExposureTime)
                        {
                            exposureTime = signalTooWeakTestExposure;

                        }

                        wbCapturedBitmap = null;
                        if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                        {
                            ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                        }
                        if (SettingsManager.ConfigSettings.CameraModeSettings.ChemiSettings.IsDynamicDarkCorrection)
                        {
                            // PvCam: using dynamic dark correction
                        }
                        else
                        {
                            biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                            if (biasImage != null)
                            {
                                wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                            }
                            else
                            {
                                string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                                throw new Exception(strMessage);
                            }
                        }
                        wbCapturedBitmap = ImageProcessing.MedianFilter(wbCapturedBitmap);
                        maxPixelValue = imageStats.GetPixelMax(wbCapturedBitmap);
                        if (maxPixelValue > 200)
                        {
                            int binningMode = imageChannelSettings.BinningMode;
                            exposureTime = Math.Round(((upperCeiling - Biasmean) / (maxPixelValue)) * exposureTime, 3);
                            if (imageChannelSettings.LightType == 0 && exposureTime > chemiMaximumExposure)
                            {
                                exposureTime = chemiMaximumExposure;
                            }
                            if (imageChannelSettings.LightType == 0)
                            {
                                if (imageChannelSettings.BinningMode == 2)
                                    exposureTime = exposureTime / 4;
                                else if (imageChannelSettings.BinningMode == 3)
                                    exposureTime = exposureTime / 9;
                                else if (imageChannelSettings.BinningMode == 4)
                                    exposureTime = exposureTime / 16;
                                exposureTime = Math.Round(exposureTime, 3);
                            }
                            break;
                        }
                        else if (maxPixelValue <= 200)
                        {
                            throw new Exception("AE ERROR: Signal too weak!");
                        }

                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                exposureTime = minexposuretime; //ms
                throw;
            }
            catch (Exception ex)
            {
                exposureTime = minexposuretime; //ms
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {

            }
            wbCapturedBitmap = null;
            return exposureTime;


        }
        /// <summary>
        /// ChemiSOLO_Auto Exposure for Chemi Imaging Mode
        /// </summary>
        /// <param name="ActiveCamera"></param>
        /// <param name="CommController"></param>
        /// <param name="imageChannelSettings"></param>
        /// <param name="Chemi_T1"></param>
        /// <param name="Str_Chemi_binning_Kxk"></param>
        /// <param name="Chemi_Intensity"></param>
        /// <returns></returns>
        public unsafe static double ChemiSOLO_Calculate_New_Algo_ImageAutoExposure(CameraController ActiveCamera, EthernetController CommController, ImageChannelSettings imageChannelSettings, double Chemi_T1, string Str_Chemi_binning_Kxk, int Chemi_Intensity)
        {
            double exposureTime = Chemi_T1;          // exposure  ms
            double minexposuretime = (double)ActiveCamera.ExposureTime_MIN;
            WriteableBitmap wbCapturedBitmap = null;
            WriteableBitmap biasImage = null;
            ImageArithmetic imgArith = new ImageArithmetic();
            try
            {
                double upperCeiling = imageChannelSettings.AutoExposureUpperCeiling;
                bool apFlag = true;
                bool saturationFlag = true;
                double l1subl2 = 0;
                int i = 0;
                string[] listbin = Str_Chemi_binning_Kxk.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//10,3,1
                int Chemi_binning_Kxk = Convert.ToInt32(listbin[i]);
                while (apFlag || saturationFlag)
                {
                    if (apFlag)
                    {
                        if (ActiveCamera.SetExpoTime((uint)(exposureTime * ActiveCamera.USConvertMS)))
                        {
                            ActiveCamera.CapturesImage(ref wbCapturedBitmap);
                        }
                        biasImage = Workspace.This.MasterLibrary.GetBiasImage(imageChannelSettings.BinningMode);

                        if (biasImage != null)
                        {
                            wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                        }
                        else
                        {
                            string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '1x1";
                            throw new Exception(strMessage);
                        }
                        WriteableBitmap Binning = imgArith.ConvertBinKxk(wbCapturedBitmap, Chemi_binning_Kxk);
                        Binning = ImageProcessing.MedianFilter(Binning);
                        Mat _mat = imgArith.ConvertWriteableBitmapToMat(Binning);
                        double l1, l2;
                        imgArith.CalculateIntensityValues(_mat, out l1, out l2);
                        l1subl2 = l1 - l2;
                        if (l1 > 63000)
                        {
                            i = i + 1;
                            Chemi_binning_Kxk = Convert.ToInt32(listbin[i]);
                        }
                        else
                        {
                            saturationFlag = false;
                        }
                        if (l1subl2 > Chemi_Intensity)
                        {

                            apFlag = false;
                        }
                        else
                        {
                            exposureTime *= 10;
                        }

                    }
                    double SecExposure = exposureTime / 1000;
                    exposureTime = upperCeiling / l1subl2 * Chemi_binning_Kxk * Chemi_binning_Kxk * SecExposure;
                    return exposureTime;
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                exposureTime = minexposuretime; //ms
                throw;
            }
            catch (Exception ex)
            {
                exposureTime = minexposuretime; //ms
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {

            }
            wbCapturedBitmap = null;
            return exposureTime;


        }

        /// <summary>
        ///  Lens curvature/distortion correction
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="paramA"></param>
        /// <param name="paramB"></param>
        /// <param name="paramC"></param>
        /// <returns></returns>
        public static unsafe WriteableBitmap ChemiSOLO_DistortionCorrection(WriteableBitmap srcImage, double paramA, double paramB, double paramC)
        {
            if (srcImage == null || (paramA == 0.0 && paramB == 0.0 && paramC == 0.0))
            {
                return srcImage;
            }

            int width = srcImage.PixelWidth;
            int height = srcImage.PixelHeight;
            int bufferWidth = srcImage.BackBufferStride;
            WriteableBitmap cpyImage = (WriteableBitmap)srcImage.Clone();

            // parameters for correction
            //double paramA = 0.0; // affects only the outermost pixels of the image
            //double paramB = 0.013; // most cases only require b optimization
            //double paramC = 0.0; // most uniform correction
            double paramD = 1.0 - paramA - paramB - paramC; // describes the linear scaling of the image

            // Reserve the back buffer for updates.
            if (!srcImage.IsFrozen)
                srcImage.Lock();
            if (!cpyImage.IsFrozen)
                cpyImage.Lock();

            byte* pSrcBuffer = (byte*)srcImage.BackBuffer.ToPointer();
            byte* pCpyBuffer = (byte*)cpyImage.BackBuffer.ToPointer();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int d = Math.Min(width, height) / 2;    // radius of the circle

                    // center of dst image
                    double centerX = (width - 1) / 2.0;
                    double centerY = (height - 1) / 2.0;

                    // cartesian coordinates of the destination point (relative to the centre of the image)
                    double deltaX = (x - centerX) / d;
                    double deltaY = (y - centerY) / d;

                    // distance or radius of dst image
                    double dstR = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                    // distance or radius of src image (with formula)
                    double srcR = (paramA * dstR * dstR * dstR + paramB * dstR * dstR + paramC * dstR + paramD) * dstR;

                    // comparing old and new distance to get factor
                    double factor = Math.Abs(dstR / srcR);

                    // coordinates in source image
                    double srcXd = centerX + (deltaX * factor * d);
                    double srcYd = centerY + (deltaY * factor * d);

                    // With interpolation (causes 'crop circles' pattern)
                    /*int srcX = (int)srcXd;
                    int srcY = (int)srcYd;
                    double rX = srcXd % srcX;
                    double rY = srcYd % srcY;

                    if (srcX >= 0 && srcY >= 0 && srcX < width && srcY < height)
                    {
                        ushort* pSrc1 = (ushort*)(pSrcBuffer + (srcY * bufferWidth));
                        ushort* pSrc2 = (ushort*)(pSrcBuffer + ((srcY + 1) * bufferWidth));
                        ushort* pCpy = (ushort*)(pCpyBuffer + (y * bufferWidth));

                        double value = (1 - rX) * (1 - rY) * (*(pSrc1 + srcX)) +
                                       rX * (1 - rY) * (*(pSrc1 + srcX + 1)) +
                                       (1 - rX) * rY * (*(pSrc2 + srcX)) +
                                       rX * rY * (*(pSrc2 + srcX + 1));
                        *(pCpy + x) = (ushort)value;
                    }*/

                    // No interpolation (just nearest point)
                    /*int srcX = (int)srcXd;
                    int srcY = (int)srcYd;

                    if (srcX >= 0 && srcY >= 0 && srcX < width && srcY < height)
                    {
                        ushort* pSrc1 = (ushort*)(pSrcBuffer + (srcY * bufferWidth));
                        ushort* pCpy = (ushort*)(pCpyBuffer + (y * bufferWidth));
                        *(pCpy + x) = *(pSrc1 + srcX);
                    }*/

                    // Linear interpolation
                    /*int srcX = (int)srcXd;
                    int srcX1 = srcX + 1;
                    int srcY = (int)srcYd;
                    int srcY1 = srcY + 1;

                    if (srcX >= 0 && srcY >= 0 && srcX < width && srcY < height)
                    {
                        ushort* pSrc1 = (ushort*)(pSrcBuffer + (srcY * bufferWidth));
                        ushort* pSrc2 = (ushort*)(pSrcBuffer + ((srcY1) * bufferWidth));

                        var temp1 = (*(pSrc1 + srcX1) * (srcXd - srcX)) + (*(pSrc1 + srcX) * (srcX1 - srcXd));
                        var temp2 = (*(pSrc2 + srcX1) * (srcXd - srcX)) + (*(pSrc2 + srcX) * (srcX1 - srcXd));

                        ushort* pCpy = (ushort*)(pCpyBuffer + (y * bufferWidth));
                        *(pCpy + x) = (ushort)((temp2 * (srcYd - srcY)) + (temp1 * (srcY1 - srcYd)));
                    }*/

                    var p1 = Interpolation(pSrcBuffer, srcXd - 0.5, srcYd - 0.5, width, height, bufferWidth);
                    var p2 = Interpolation(pSrcBuffer, srcXd - 0.5, srcYd + 0.5, width, height, bufferWidth);
                    var p3 = Interpolation(pSrcBuffer, srcXd + 0.5, srcYd + 0.5, width, height, bufferWidth);
                    var p4 = Interpolation(pSrcBuffer, srcXd + 0.5, srcYd - 0.5, width, height, bufferWidth);
                    ushort* pCpy = (ushort*)(pCpyBuffer + (y * bufferWidth));
                    *(pCpy + x) = (ushort)(p1 / 4 + p2 / 4 + p3 / 4 + p4 / 4);
                }
            }

            // Release the back buffer and make it available for display.
            if (!srcImage.IsFrozen)
                srcImage.Unlock();
            if (!cpyImage.IsFrozen)
                cpyImage.Unlock();

            return cpyImage;
        }
        private static unsafe double Interpolation(byte* srcData, double Xd, double Yd, int width, int height, int bufferWidth)
        {
            double result = 0;

            int X = (int)Xd;
            int Y = (int)Yd;
            int X1 = X + 1;
            int Y1 = Y + 1;

            if (X >= 0 && Y >= 0 && X1 < width && Y1 < height)
            {
                ushort* pSrc1 = (ushort*)(srcData + (Y * bufferWidth));
                ushort* pSrc2 = (ushort*)(srcData + (Y1 * bufferWidth));
                var pixelY = (Xd - X) * (*(pSrc1 + X1)) + (X1 - Xd) * (*(pSrc1 + X));
                var pixelY1 = (Xd - X) * (*(pSrc2 + X1)) + (X1 - Xd) * (*(pSrc2 + X));
                result = (Yd - Y) * pixelY1 + (Y1 - Yd) * pixelY;
            }
            else
            {
                ushort* pSrc1 = (ushort*)(srcData + (Y * bufferWidth));
                result = *(pSrc1 + X);
            }

            return result;
        }
    }
}
