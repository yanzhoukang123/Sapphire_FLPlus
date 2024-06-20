using Azure.CameraLib;
using Azure.EthernetCommLib;
using Azure.Image.Processing;
using Azure.Ipp.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Azure.ImagingSystem
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

        public static double CalculateFlatImageAutoExposure(CameraController ActiveCamera, EthernetController CommController, ImageChannelSettings imageChannelSettings,)
        {
            double exposureTime = 1.0;          // Initial exposure
            double initialExposureTime = exposureTime;
            double maxPixelValue = 0;
            double upperCeiling = 55000;    // Default upper ceiling
            double chemiMaximumExposure = imageChannelSettings.MaxExposure;
            ImageArithmetic imgArith = new ImageArithmetic();
            double signalTooHighTestExposure = exposureTime / 10.0;
            double signalTooWeakTestExposure = exposureTime * 4.0;
            ImageStatistics imageStats = new ImageStatistics();

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
                if (signalTooHighTestExposure < 0.001)
                    signalTooHighTestExposure = 0.001;
                signalTooWeakTestExposure = Math.Round((exposureTime * 30), 3);
            }
            WriteableBitmap wbCapturedBitmap = null;
            if (ActiveCamera.SetExpoTime((uint)(exposureTime*1000000)))
            {
                ActiveCamera.CapturesImage(out wbCapturedBitmap);
            }
            WriteableBitmap biasImage = null;
            if (SettingsManager.ConfigSettings.CameraModeSettings.IsDynamicDarkCorrection)
            {
                // PvCam: using dynamic dark correction
            }
            else
            {
                biasImage = Workspace.This.MasterLibrary.GetBiasImage(activeCamera.VBin, activeCamera.ReadoutSpeed);

                if (biasImage != null)
                {
                    wbCapturedBitmap = imgArith.SubtractImage(wbCapturedBitmap, biasImage);
                }
                else
                {
                    string strMessage = "AE ERROR: Cannot find bias image, master library contains no bias image for binning mode '" + activeCamera.VBin + "x" + activeCamera.VBin + "'.";
                    throw new Exception(strMessage);
                }
            }

        }
    }
}
