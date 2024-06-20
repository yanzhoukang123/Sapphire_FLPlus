using Azure.Image.Processing;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Azure.ScannerEUI.SystemCommand
{
    public class MasterLibrary
    {
        private const string _MastersFolder = "Masters";
        private ObservableCollection<MasterFile> _AllMasterFiles = new ObservableCollection<MasterFile>();
        private Dictionary<string, List<MasterFile>> _DarkDictionary = new Dictionary<string, List<MasterFile>>();
        private Dictionary<string, List<MasterFile>> _BiasDictionary = new Dictionary<string, List<MasterFile>>();
        private Dictionary<string, MasterFile> _FlatDictionary = new Dictionary<string, MasterFile>();

        //private ObservableCollection<MasterFile>     _AllMasterFilesFast = new ObservableCollection<MasterFile>();
        //private Dictionary<string, List<MasterFile>> _DarkDictionaryFast = new Dictionary<string, List<MasterFile>>();
        //private Dictionary<string, MasterFile>       _BiasDictionaryFast = new Dictionary<string, MasterFile>();
        //private Dictionary<string, MasterFile>       _FlatDictionaryFast = new Dictionary<string, MasterFile>();

        private string _LibraryPath = "";

        private Dictionary<string, int> _SpeedDictionary = new Dictionary<string, int>();
        private Dictionary<string, int> _ExposureDictionary = new Dictionary<string, int>();

        #region public string LibraryPath
        /// <summary>
        /// Get/set the darkmaster library root path.
        /// </summary>
        public string LibraryPath
        {
            get { return _LibraryPath; }
            set
            {
                _LibraryPath = value;
                //OnPropertyChanged("LibraryPath");
            }
        }
        #endregion

        #region public ObservableCollection<MasterFileViewModel> AllMasterFiles
        /// <summary>
        /// Get the collection of all master files.
        /// </summary>
        public ObservableCollection<MasterFile> AllMasterFiles
        {
            get { return _AllMasterFiles; }
        }
        #endregion

        #region public Dictionary<string, List<MasterFile>> DarkMasterByBinningLookup
        /// <summary>
        /// Get a dictionary of dark master files by bin factor(e.g., "1x1", "3x3", etc).
        /// </summary>
        public Dictionary<string, List<MasterFile>> DarkMasterByBinningLookup
        {
            get { return _DarkDictionary; }
        }
        #endregion

        #region public Dictionary<string, List<MasterFile>> BiasByBinningLookup
        /// <summary>
        /// Get a dictionary of bias files by bin factor(e.g., "1x1", "3x3", etc).
        /// </summary>
        public Dictionary<string, List<MasterFile>> BiasByBinningLookup
        {
            get { return _BiasDictionary; }
        }
        #endregion

        #region public Dictionary<string, MasterFile> FlatDictionary
        /// <summary>
        /// Get a dictionary of flat-field files by setting indexes(e.g., "1x1x1x1x2", etc).
        /// </summary>
        public Dictionary<string, MasterFile> FlatDictionary
        {
            get { return _FlatDictionary; }
        }
        #endregion

        #region Constructors...
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="libraryPath"></param>
        public MasterLibrary(string libraryPath)
        {
            this.LibraryPath = libraryPath;

            _SpeedDictionary.Add("Fast", 1);
            _SpeedDictionary.Add("Normal", 0);

            _ExposureDictionary.Add("10s", 10);
            _ExposureDictionary.Add("1m", 60);
            _ExposureDictionary.Add("5m", 300);
            _ExposureDictionary.Add("10m", 600);
            _ExposureDictionary.Add("20m", 1200);
        }
        #endregion

        #region public void LoadLibraryInfo()
        /// <summary>
        /// Load the all master library information.
        /// </summary>
        public void LoadLibraryInfo()
        {
            _AllMasterFiles.Clear();
            _DarkDictionary.Clear();
            _BiasDictionary.Clear();
            _FlatDictionary.Clear();

            if (!Directory.Exists(_LibraryPath))
            {
                throw new ArgumentException("Master image directory does not exist: " + _LibraryPath);
            }

            DirectoryInfo dirInfo = new DirectoryInfo(System.IO.Path.Combine(_LibraryPath, _MastersFolder));
            FileInfo[] files = dirInfo.GetFiles();

            List<string> biasFiles = new List<string>();
            List<string> darkFiles = new List<string>();
            List<string> flatFiles = new List<string>();

            //
            // Loop through master files. Format:
            //   Bias_NxN_Speed.tif (exposure time has been removed from bias files)
            //   Dark_NxN_Spped_Exp_VAL.tif
            //
            foreach (FileInfo fileInfo in files)
            {
                MasterFile masterFile = new MasterFile(fileInfo);

                string fName = fileInfo.Name.Replace(fileInfo.Extension, "");
                string[] fileParts = fName.Split(new char[] { '_' });

                string binMode = string.Empty;
                //string strGain = string.Empty;
                //string strSpeed = string.Empty;
                string strFLAT = string.Empty;

                if (fileParts[0].StartsWith("bias", StringComparison.OrdinalIgnoreCase) ||
                    fileParts[0].StartsWith("dark", StringComparison.OrdinalIgnoreCase))
                {
                    if (fileParts == null || fileParts.Length < 2)
                    {
                        continue;
                    }

                    int intVal;
                    double expVal;
                    // darkmaster files - get exposure time
                    if (fileParts.Length > 2)
                    {
                        //masterFile.ExposureTime = _ExposureDictionary[fileParts[4]];
                        if (!double.TryParse(fileParts[2], out expVal))
                        {
                            continue;
                        }
                        masterFile.ExposureTime = expVal;
                    }

                    binMode = fileParts[1].Trim();
                    string[] binParts = binMode.Split(new char[] { 'x' });

                    if (!int.TryParse(binParts[0], out intVal))
                    {
                        continue;
                    }
                    masterFile.VerticalBins = intVal;

                    if (!int.TryParse(binParts[1], out intVal))
                    {
                        continue;
                    }
                    masterFile.HorizontalBins = intVal;

                    //// gain
                    //if (!int.TryParse(fileParts[2], out intVal))
                    //{
                    //    continue;
                    //}
                    //masterFile.Gain = intVal;

                    // speed
                    //masterFile.Speed = _SpeedDictionary[fileParts[2]];
                }
                else if (fileParts[0].StartsWith("u", StringComparison.OrdinalIgnoreCase))
                {
                    if (fileParts == null || fileParts.Length < 2)
                    {
                        continue;
                    }
                    strFLAT = fileParts[1].Trim();
                }

                _AllMasterFiles.Add(masterFile);

                if (fileParts[0].StartsWith("bias", StringComparison.OrdinalIgnoreCase))
                {
                    masterFile.MasterFileType = MasterFileType.Bias;
                    if (_BiasDictionary.ContainsKey(binMode))
                    {
                        List<MasterFile> list = _BiasDictionary[binMode];
                        list.Add(masterFile);
                    }
                    else
                    {
                        List<MasterFile> list = new List<MasterFile>();
                        list.Add(masterFile);
                        _BiasDictionary.Add(binMode, list);
                    }
                }
                else if (fileParts[0].StartsWith("dark", StringComparison.OrdinalIgnoreCase))
                {
                    masterFile.MasterFileType = MasterFileType.Dark;
                    if (_DarkDictionary.ContainsKey(binMode))
                    {
                        List<MasterFile> list = _DarkDictionary[binMode];
                        list.Add(masterFile);
                    }
                    else
                    {
                        List<MasterFile> list = new List<MasterFile>();
                        list.Add(masterFile);
                        _DarkDictionary.Add(binMode, list);
                    }
                }
                else if (fileParts[0].StartsWith("u", StringComparison.OrdinalIgnoreCase))
                {
                    masterFile.MasterFileType = MasterFileType.Flat;
                    _FlatDictionary.Add(strFLAT, masterFile);

                    //if (_FlatDictionary.ContainsKey(binMode))
                    //{
                    //    List<MasterFile> list = _FlatDictionary[binMode];
                    //    list.Add(masterFile);
                    //}
                    //else
                    //{
                    //    List<MasterFile> list = new List<MasterFile>();
                    //    list.Add(masterFile);
                    //    _FlatDictionary.Add(binMode, list);
                    //}
                }

            }

            //OnPropertyChanged("DarkMasterByBinningLookup");
            //OnPropertyChanged("BiasByBinningLookup");

        }
        #endregion

        /// <summary>
        /// Determines if master library contains a bias for the binning mode and speed argument.
        /// </summary>
        /// <param name="binningFactor"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public bool DoesLibraryContainBias(int binningFactor, int speed)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            if (_BiasDictionary.ContainsKey(binMode))
            {
                List<MasterFile> biasMasters = _BiasDictionary[binMode];
                foreach (var bias in biasMasters)
                {
                    if (speed == bias.Speed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool DoesLibraryContainBias(int binningFactor)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            if (_BiasDictionary.ContainsKey(binMode))
            {
                List<MasterFile> biasMasters = _BiasDictionary[binMode];
                if (biasMasters[0] != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if master library contains a dark master for the binning mode and speed argument.
        /// </summary>
        /// <param name="binningFactor"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public bool DoesLibraryContainDark(int binningFactor, int speed)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            if (_DarkDictionary.ContainsKey(binMode))
            {
                List<MasterFile> darkMastersAll = _DarkDictionary[binMode];
                List<MasterFile> darkMasters = new List<MasterFile>();

                foreach (var masterFile in darkMastersAll)
                {
                    if (masterFile.Speed == speed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool DoesLibraryContainDark(int binningFactor)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            if (_DarkDictionary.ContainsKey(binMode))
            {
                List<MasterFile> darkMastersAll = _DarkDictionary[binMode];

                if (darkMastersAll.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #region public WriteableBitmap GetBiasImage(int binningFactor, bool bSpeedFast)
        /// <summary>
        /// Get the bias image corresponding to the binning factor argument.
        /// </summary>
        /// <param name="binningFactor">The binning factor used for raw image channel.</param>
        /// <returns></returns>
        public WriteableBitmap GetBiasImage(int binningFactor, int iDigitizationSpeed)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> biasMasters = _BiasDictionary[binMode];
            MasterFile biasMaster = null;
            foreach (var bias in biasMasters)
            {
                if (iDigitizationSpeed == bias.Speed)
                {
                    biasMaster = bias;
                    break;
                }
            }

            if (biasMaster != null)
            {
                if (!File.Exists(biasMaster.FileInfo.FullName))
                {
                    //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                    return null;
                }
            }
            else
            {
                return null;
            }

            WriteableBitmap biasImage = ImageProcessing.LoadImageFile(biasMaster.FileInfo.FullName);

            return biasImage;
        }

        public WriteableBitmap GetBiasImage(int binningFactor)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> biasMasters = _BiasDictionary[binMode];
            if (biasMasters[0] != null)
            {
                if (!File.Exists(biasMasters[0].FileInfo.FullName))
                {
                    //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            MasterFile biasMaster = null;
            biasMaster = biasMasters[0];
    
            WriteableBitmap biasImage = ImageProcessing.LoadImageFile(biasMaster.FileInfo.FullName);

            return biasImage;
        }
        public Mat GetBiasImage()
        {
            string binMode = "1x1";

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> biasMasters = _BiasDictionary[binMode];
            if (biasMasters[0] != null)
            {
                if (!File.Exists(biasMasters[0].FileInfo.FullName))
                {
                    //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            MasterFile biasMaster = null;
            biasMaster = biasMasters[0];
            Mat biasImage = Cv2.ImRead(biasMaster.FileInfo.FullName);
            return biasImage;
        }
        public WriteableBitmap GetDarkMasterImage(int binningFactor)
        {
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> darkMasters = _DarkDictionary[binMode];
            if (darkMasters[0] != null)
            {
                if (!File.Exists(darkMasters[0].FileInfo.FullName))
                {
                    //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            MasterFile darkMaster = null;
            darkMaster = darkMasters[0];

            WriteableBitmap darkImage = ImageProcessing.LoadImageFile(darkMaster.FileInfo.FullName);

            return darkImage;
        }
        public Mat GetDarkMasterImage()
        {
            string binMode = "1x1";

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> darkMasters = _DarkDictionary[binMode];
            if (darkMasters[0] != null)
            {
                if (!File.Exists(darkMasters[0].FileInfo.FullName))
                {
                    //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            MasterFile darkMaster = null;
            darkMaster = darkMasters[0];
            Mat darkImage = Cv2.ImRead(darkMaster.FileInfo.FullName);
            return darkImage;
        }
        #endregion

        #region public byte[] GetBiasImageBuffer(int binningFactor, int iDigitizationSpeed)
        /// <summary>
        /// Get the bias image buffer corresponding to the binning factor argument.
        /// </summary>
        /// <param name="binningFactor">The binning factor used for raw image channel.</param>
        /// <returns></returns>
        public byte[] GetBiasImageBuffer(int binningFactor, int iDigitizationSpeed)
        {
            byte[] biasImageBuffer = null;
            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();

            if (!_BiasDictionary.ContainsKey(binMode))
            {
                //throw new Exception("Cannot get bias image, master library contains no bias image for binning mode '" + binMode + "'.");
                return null;
            }

            List<MasterFile> biasMasters = _BiasDictionary[binMode];
            MasterFile biasMaster = null;
            foreach (var bias in biasMasters)
            {
                if (iDigitizationSpeed == bias.Speed)
                {
                    biasMaster = bias;
                    break;
                }
            }

            if (biasMaster == null || !File.Exists(biasMaster.FileInfo.FullName))
            {
                //throw new Exception("Bias image file does not exist '" + biasMaster.FileInfo.FullName + "'.");
                return null;
            }

            WriteableBitmap biasImage = ImageProcessing.LoadImageFile(biasMaster.FileInfo.FullName);
            if (biasImage != null)
            {
                int iWidth = biasImage.PixelWidth;
                int iHeight = biasImage.PixelHeight;
                int bitsPerPixel = biasImage.Format.BitsPerPixel;
                int iStride = (iWidth * bitsPerPixel + 7) / 8;
                biasImageBuffer = new byte[iWidth * iHeight * 2];
                biasImage.CopyPixels(biasImageBuffer, iStride, 0);
            }

            return biasImageBuffer;
        }
        #endregion

        /*** #region public BitmapSource CalculateCorrectedImage(byte[] srcImageBuffer, int iWidth, int iHeight, double exposureTime, int binningFactor, int digitizationSpeed, out string correctionStr)
        /// <summary>
        /// Calculate corrected image from master files.
        /// </summary>
        public BitmapSource CalculateCorrectedImage(byte[] srcImageBuffer,
                                                    int iSrcWidth,
                                                    int iSrcHeight,
                                                    double exposureTime,
                                                    int binningFactor,
                                                    int digitizationSpeed,
                                                    out string correctionString)
        {
            BitmapSource srcBitmap = null;
            if (srcImageBuffer == null)
            {
                correctionString = "";
                srcBitmap = ImageBufferToBitmapSource(srcImageBuffer, iSrcWidth, iSrcHeight);
                return srcBitmap;
            }

            BitmapSource result = null;
            byte[] biasBuffer = null;
            byte[] darkScaledBuffer = null;
            int iDarkWidth = 0;
            int iDarkHeight = 0;
            int stride = 0;
            correctionString = "None";

            string binMode = binningFactor.ToString() + "x" + binningFactor;

            if (!DoesLibraryContainBias(binningFactor, digitizationSpeed))
            {
                //throw new Exception("Cannot scale image, master library contains no bias image for binning mode '" + binMode + "'.");
                correctionString = "";
                srcBitmap = ImageBufferToBitmapSource(srcImageBuffer, iSrcWidth, iSrcHeight);
                return srcBitmap;
            }

            if (!DoesLibraryContainDark(binningFactor, digitizationSpeed))
            {
                //throw new Exception("Cannot scale image, master library contains no bias image for binning mode '" + binMode + "'.");
                correctionString = "";
                srcBitmap = ImageBufferToBitmapSource(srcImageBuffer, iSrcWidth, iSrcHeight);
                return srcBitmap;
            }

            biasBuffer = GetBiasImageBuffer(binningFactor, digitizationSpeed);

            // source image - bias image
            if (biasBuffer != null)
            {
                int bitsPerPixel = 16;
                int iSrcStep = (iSrcWidth * bitsPerPixel + 7) / 8;
                Invoke.MVC_BUF_Sub16u(biasBuffer, srcImageBuffer, iSrcStep, iSrcStep, iSrcWidth, iSrcHeight);
            }
            else
            {
                correctionString = "";
                srcBitmap = ImageBufferToBitmapSource(srcImageBuffer, iSrcWidth, iSrcHeight);
                return srcBitmap;
            }

            //List<MasterFile> biasMasters = _BiasDictionary[binMode];
            List<MasterFile> darkMastersAll = _DarkDictionary[binMode];
            List<MasterFile> darkMasters = new List<MasterFile>();

            foreach (var masterFile in darkMastersAll)
            {
                if (masterFile.Speed == digitizationSpeed)
                {
                    darkMasters.Add(masterFile);
                }
            }

            //
            // Sort dark masters for this binning mode by exposure time:
            //
            darkMasters.Sort(delegate(MasterFile p1, MasterFile p2) { return p1.ExposureTime.CompareTo(p2.ExposureTime); });

            //
            // Find the master files that bound the current exposure time:
            //
            if (exposureTime <= darkMasters[0].ExposureTime)
            {
                MasterFile darkLow = darkMasters[0];
                BitmapSource darkLowImage = MVImage.Load(darkLow.FileInfo.FullName);
                if (darkLowImage != null)
                {
                    iDarkWidth = darkLowImage.PixelWidth;
                    iDarkHeight = darkLowImage.PixelHeight;
                    stride = (iDarkWidth * darkLowImage.Format.BitsPerPixel + 7) / 8;
                    darkScaledBuffer = new byte[iDarkWidth * iDarkHeight * 2];
                    darkLowImage.CopyPixels(darkScaledBuffer, stride, 0);
                }

                // dark scaled
                double lowScale = exposureTime / darkLow.ExposureTime;
                Invoke.MVC_BUF_ImageMulC(darkScaledBuffer, (int)lowScale, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);
            }
            else if (exposureTime >= darkMasters[darkMasters.Count - 1].ExposureTime)
            {
                MasterFile darkHigh = darkMasters[darkMasters.Count - 1];
                BitmapSource darkHighImage = MVImage.Load(darkHigh.FileInfo.FullName);

                if (darkHighImage != null)
                {
                    iDarkWidth = darkHighImage.PixelWidth;
                    iDarkHeight = darkHighImage.PixelHeight;
                    stride = (iDarkWidth * darkHighImage.Format.BitsPerPixel + 7) / 8;
                    darkScaledBuffer = new byte[iDarkWidth * iDarkHeight * 2];
                    darkHighImage.CopyPixels(darkScaledBuffer, stride, 0);
                }

                // dark scaled
                double highScale = exposureTime / darkHigh.ExposureTime;
                Invoke.MVC_BUF_ImageMulC(darkScaledBuffer, (int)highScale, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);
            }
            else
            {
                MasterFile darkLow = null;
                MasterFile darkHigh = null;

                foreach (MasterFile master in darkMasters)
                {
                    if (exposureTime >= master.ExposureTime)
                    {
                        darkLow = master;
                    }
                    if (exposureTime <= master.ExposureTime && darkHigh == null)
                    {
                        darkHigh = master;
                        break;
                    }
                }

                // 
                // Corrected_Image = Raw_Image - Bias - [(ExpHi-exp)*Darkmaster_Low + (exp-ExpLo)*Darkmaster_High]/(ExpHi-ExpLo)
                //
                double lowScale = (darkHigh.ExposureTime - exposureTime) / (darkHigh.ExposureTime - darkLow.ExposureTime);
                double highScale = (exposureTime - darkLow.ExposureTime) / (darkHigh.ExposureTime - darkLow.ExposureTime);

                BitmapSource darkLowImage = MVImage.Load(darkLow.FileInfo.FullName);
                byte[] darkLowImageBuffer = null;

                if (darkLowImage != null)
                {
                    iDarkWidth = darkLowImage.PixelWidth;
                    iDarkHeight = darkLowImage.PixelHeight;
                    stride = (iDarkWidth * darkLowImage.Format.BitsPerPixel + 7) / 8;
                    darkLowImageBuffer = new byte[iDarkWidth * iDarkHeight * 2];
                    darkLowImage.CopyPixels(darkLowImageBuffer, stride, 0);
                }

                BitmapSource darkHighImage = MVImage.Load(darkHigh.FileInfo.FullName);
                byte[] darkHighImageBuffer = null;

                if (darkHighImage != null)
                {
                    iDarkWidth = darkHighImage.PixelWidth;
                    iDarkHeight = darkHighImage.PixelHeight;
                    stride = (iDarkWidth * darkHighImage.Format.BitsPerPixel + 7) / 8;
                    darkHighImageBuffer = new byte[iDarkWidth * iDarkHeight * 2];
                    darkHighImage.CopyPixels(darkHighImageBuffer, stride, 0);
                }

                Invoke.MVC_BUF_ImageMulC(darkLowImageBuffer, (int)lowScale, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);
                Invoke.MVC_BUF_ImageMulC(darkHighImageBuffer, (int)highScale, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);

                Invoke.MVC_BUF_ImageAdd(darkLowImageBuffer, darkHighImageBuffer, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);

                // dark scaled
                darkScaledBuffer = darkLowImageBuffer;
            }

            if (iSrcWidth == iDarkWidth && iSrcHeight == iDarkHeight)
            {
                //((C-B) - ((D*C_exp)/D_exp)) - min;
                int iBitsPerPixel = 16;
                int iSrcStep = (iSrcWidth * iBitsPerPixel + 7) / 8;
                Invoke.MVC_BUF_Sub16u(darkScaledBuffer, srcImageBuffer, iSrcStep, iSrcStep, iSrcWidth, iSrcHeight);

                ushort iSrcMinValue = 0;
                Invoke.MVC_BUF_Min16u(srcImageBuffer, MVImage.P16uC1, iSrcStep, iSrcWidth, iSrcHeight, ref iSrcMinValue);
                Invoke.MVC_BUF_ImageSubC(srcImageBuffer, iSrcMinValue, MVImage.P16uC1, stride, iDarkWidth, iDarkHeight);

                result = BitmapSource.Create(iSrcWidth,
                                             iSrcHeight,
                                             96,
                                             96,
                                             PixelFormats.Gray16,
                                             null,
                                             srcImageBuffer,
                                             stride);

                correctionString = "Dark";
            }
            else
            {
                correctionString = "";
                srcBitmap = ImageBufferToBitmapSource(srcImageBuffer, iSrcWidth, iSrcHeight);
                return srcBitmap;
            }

            return result;
        }
        #endregion ***/

        #region public WriteableBitmap CalculateCorrectedImage(WriteableBitmap srcImage, int iSrcWidth, int iSrcHeight, double exposureTime, int binningFactor, int digitizationSpeed, out string correctionString)
        /// <summary>
        /// Calculate dark corrected image from master files.
        /// </summary>
        public WriteableBitmap CalculateCorrectedImage(WriteableBitmap srcImage,
                                                       double exposureTime,
                                                       int binningFactor,
                                                       int digitizationSpeed,
                                                       out string correctionString)
        {
            if (srcImage == null)
            {
                correctionString = "";
                return srcImage;
            }

            int iSrcWidth = srcImage.PixelWidth;
            int iSrcHeight = srcImage.PixelHeight;
            WriteableBitmap result = null;
            WriteableBitmap srcBiasCorrectedImage = null;
            ushort iSrcMinValue = 0;
            correctionString = "";

            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            //int iBitsPerPixel = srcImage.Format.BitsPerPixel;
            //int iSrcStep = (iSrcWidth * iBitsPerPixel + 7) / 8;
            ImageArithmetic imageArith = new ImageArithmetic();
            ImageStatistics imageStat = new ImageStatistics();
            WriteableBitmap darkScaledImage = null;

            if (!DoesLibraryContainBias(binningFactor, digitizationSpeed))
            {
                correctionString = "";
                return srcImage;
            }

            // source image - bias image
            WriteableBitmap biasImage = GetBiasImage(binningFactor, digitizationSpeed);
            if (biasImage != null)
            {
                if (biasImage.PixelWidth == srcImage.PixelWidth && biasImage.PixelHeight == srcImage.PixelHeight)
                {
                    srcBiasCorrectedImage = imageArith.SubtractImage(srcImage, biasImage);
                    correctionString = "Bias";
                    biasImage = null;
                }
                else
                {
                    correctionString = "";
                    return srcImage;
                }
            }
            else
            {
                correctionString = "";
                return srcImage;
            }

            //
            // don't do dark correction if the exposure time is less than 1 second
            //
            if (exposureTime < 1 || !DoesLibraryContainDark(binningFactor, digitizationSpeed))
            {
                correctionString = "Bias";
                return srcBiasCorrectedImage;
            }

            List<MasterFile> darkMastersAll = _DarkDictionary[binMode];
            List<MasterFile> darkMasters = new List<MasterFile>();

            foreach (var masterFile in darkMastersAll)
            {
                if (masterFile.Speed == digitizationSpeed)
                {
                    darkMasters.Add(masterFile);
                }
            }

            //
            // Sort dark masters for this binning mode by exposure time:
            //
            darkMasters.Sort(delegate (MasterFile p1, MasterFile p2) { return p1.ExposureTime.CompareTo(p2.ExposureTime); });

            //
            // Find the master files that bound the current exposure time:
            //
            if (exposureTime <= darkMasters[0].ExposureTime)
            {
                MasterFile darkLow = darkMasters[0];
                WriteableBitmap darkLowImage = ImageProcessing.LoadImageFile(darkLow.FileInfo.FullName);
                if (darkLowImage != null)
                {
                    // dark scaled
                    double lowScale = exposureTime / darkLow.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkLowImage, lowScale);
                    darkLowImage = null;
                }
            }
            else if (exposureTime >= darkMasters[darkMasters.Count - 1].ExposureTime)
            {
                MasterFile darkHigh = darkMasters[darkMasters.Count - 1];
                WriteableBitmap darkHighImage = ImageProcessing.LoadImageFile(darkHigh.FileInfo.FullName);
                if (darkHighImage != null)
                {
                    // dark scaled
                    double highScale = exposureTime / darkHigh.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkHighImage, highScale);
                    darkHighImage = null;
                }
            }
            else
            {
                MasterFile darkLow = null;
                MasterFile darkHigh = null;
                MasterFile darkSelected = null;

                foreach (MasterFile master in darkMasters)
                {
                    if (exposureTime >= master.ExposureTime)
                    {
                        darkLow = master;
                    }
                    if (exposureTime <= master.ExposureTime && darkHigh == null)
                    {
                        darkHigh = master;
                        break;
                    }
                }

                if ((darkHigh.ExposureTime - exposureTime) < (exposureTime - darkLow.ExposureTime))
                {
                    darkSelected = darkHigh;
                }
                else
                {
                    darkSelected = darkLow;
                }

                WriteableBitmap darkImage = ImageProcessing.LoadImageFile(darkSelected.FileInfo.FullName);
                if (darkImage != null)
                {
                    // dark scaled
                    double darkScale = exposureTime / darkSelected.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkImage, darkScale);
                    darkImage = null;
                }
            }

            if (iSrcWidth == darkScaledImage.PixelWidth && iSrcHeight == darkScaledImage.PixelHeight)
            {
                //((C-B) - ((D*C_exp)/D_exp)) - min;
                WriteableBitmap biasDarkCorrectedBitmap = imageArith.SubtractImage(srcBiasCorrectedImage, darkScaledImage);
                iSrcMinValue = (ushort)imageStat.GetPixelMin(biasDarkCorrectedBitmap, new System.Drawing.Rectangle(0, 0, iSrcWidth, iSrcHeight));
                result = imageArith.Subtract(biasDarkCorrectedBitmap, (iSrcMinValue - 5));
                correctionString = "Dark";
                biasDarkCorrectedBitmap = null;
            }
            else
            {
                correctionString = "Bias";
                return srcBiasCorrectedImage;
            }

            return result;
        }


        public WriteableBitmap CalculateCorrectedImage(WriteableBitmap srcImage,
                                               double exposureTime,
                                               int binningFactor,
                                               out string correctionString)
        {
            if (srcImage == null)
            {
                correctionString = "";
                return srcImage;
            }

            int iSrcWidth = srcImage.PixelWidth;
            int iSrcHeight = srcImage.PixelHeight;
            WriteableBitmap result = null;
            WriteableBitmap srcBiasCorrectedImage = null;
            ushort iSrcMinValue = 0;
            correctionString = "";

            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            //int iBitsPerPixel = srcImage.Format.BitsPerPixel;
            //int iSrcStep = (iSrcWidth * iBitsPerPixel + 7) / 8;
            ImageArithmetic imageArith = new ImageArithmetic();
            ImageStatistics imageStat = new ImageStatistics();
            WriteableBitmap darkScaledImage = null;

            if (!DoesLibraryContainBias(binningFactor))
            {
                correctionString = "";
                return srcImage;
            }

            // source image - bias image
            WriteableBitmap biasImage = GetBiasImage(binningFactor);
            if (biasImage != null)
            {
                if (biasImage.PixelWidth == srcImage.PixelWidth && biasImage.PixelHeight == srcImage.PixelHeight)
                {
                    srcBiasCorrectedImage = imageArith.SubtractImage(srcImage, biasImage);
                    correctionString = "Bias";
                    biasImage = null;
                }
                else
                {
                    correctionString = "";
                    return srcImage;
                }
            }
            else
            {
                correctionString = "";
                return srcImage;
            }

            //
            // don't do dark correction if the exposure time is less than 1 second
            //
            if (exposureTime < 1 || !DoesLibraryContainDark(binningFactor))
            {
                correctionString = "Bias";
                return srcBiasCorrectedImage;
            }

            List<MasterFile> darkMastersAll = _DarkDictionary[binMode];
            List<MasterFile> darkMasters = new List<MasterFile>();


            darkMasters.Add(darkMastersAll[0]);
  

            //
            // Sort dark masters for this binning mode by exposure time:
            //
            darkMasters.Sort(delegate (MasterFile p1, MasterFile p2) { return p1.ExposureTime.CompareTo(p2.ExposureTime); });

            //
            // Find the master files that bound the current exposure time:
            //
            if (exposureTime <= darkMasters[0].ExposureTime)
            {
                MasterFile darkLow = darkMasters[0];
                WriteableBitmap darkLowImage = ImageProcessing.LoadImageFile(darkLow.FileInfo.FullName);
                if (darkLowImage != null)
                {
                    // dark scaled
                    double lowScale = exposureTime / darkLow.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkLowImage, lowScale);
                    darkLowImage = null;
                }
            }
            else if (exposureTime >= darkMasters[darkMasters.Count - 1].ExposureTime)
            {
                MasterFile darkHigh = darkMasters[darkMasters.Count - 1];
                WriteableBitmap darkHighImage = ImageProcessing.LoadImageFile(darkHigh.FileInfo.FullName);
                if (darkHighImage != null)
                {
                    // dark scaled
                    double highScale = exposureTime / darkHigh.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkHighImage, highScale);
                    darkHighImage = null;
                }
            }
            else
            {
                MasterFile darkLow = null;
                MasterFile darkHigh = null;
                MasterFile darkSelected = null;

                foreach (MasterFile master in darkMasters)
                {
                    if (exposureTime >= master.ExposureTime)
                    {
                        darkLow = master;
                    }
                    if (exposureTime <= master.ExposureTime && darkHigh == null)
                    {
                        darkHigh = master;
                        break;
                    }
                }

                if ((darkHigh.ExposureTime - exposureTime) < (exposureTime - darkLow.ExposureTime))
                {
                    darkSelected = darkHigh;
                }
                else
                {
                    darkSelected = darkLow;
                }

                WriteableBitmap darkImage = ImageProcessing.LoadImageFile(darkSelected.FileInfo.FullName);
                if (darkImage != null)
                {
                    // dark scaled
                    double darkScale = exposureTime / darkSelected.ExposureTime;
                    darkScaledImage = imageArith.Multiply(darkImage, darkScale);
                    darkImage = null;
                }
            }

            if (iSrcWidth == darkScaledImage.PixelWidth && iSrcHeight == darkScaledImage.PixelHeight)
            {
                //((C-B) - ((D*C_exp)/D_exp)) - min;
                WriteableBitmap biasDarkCorrectedBitmap = imageArith.SubtractImage(srcBiasCorrectedImage, darkScaledImage);
                iSrcMinValue = (ushort)imageStat.GetPixelMin(biasDarkCorrectedBitmap, new System.Drawing.Rectangle(0, 0, iSrcWidth, iSrcHeight));
                result = imageArith.Subtract(biasDarkCorrectedBitmap, (iSrcMinValue - 5));
                correctionString = "Dark";
                biasDarkCorrectedBitmap = null;
            }
            else
            {
                correctionString = "Bias";
                return srcBiasCorrectedImage;
            }

            return result;
        }

        public WriteableBitmap ChemiSOLO_CalculateCorrectedImage(WriteableBitmap srcImage,
                                       double exposureTime,
                                       int binningFactor,
                                       out string correctionString)
        {
            if (srcImage == null)
            {
                correctionString = "";
                return srcImage;
            }

            int iSrcWidth = srcImage.PixelWidth;
            int iSrcHeight = srcImage.PixelHeight;
            WriteableBitmap result = null;
            WriteableBitmap srcBiasCorrectedImage = null;
            ushort iSrcMinValue = 0;
            correctionString = "";

            string binMode = binningFactor.ToString() + "x" + binningFactor.ToString();
            //int iBitsPerPixel = srcImage.Format.BitsPerPixel;
            //int iSrcStep = (iSrcWidth * iBitsPerPixel + 7) / 8;
            ImageArithmetic imageArith = new ImageArithmetic();
            ImageStatistics imageStat = new ImageStatistics();
            WriteableBitmap darkScaledImage = null;

            if (!DoesLibraryContainBias(binningFactor))
            {
                correctionString = "";
                return srcImage;
            }

            // source image - bias image
            WriteableBitmap biasImage = GetBiasImage(binningFactor);
            if (biasImage != null)
            {
                if (biasImage.PixelWidth == srcImage.PixelWidth && biasImage.PixelHeight == srcImage.PixelHeight)
                {
                    srcBiasCorrectedImage = imageArith.SubtractImage(srcImage, biasImage);
                    correctionString = "Bias";
                    biasImage = null;
                    return srcBiasCorrectedImage;
                }
                else
                {
                    correctionString = "";
                    return srcImage;
                }
            }
            else
            {
                correctionString = "";
                return srcImage;
            }
        }

        public Mat ChemiSOLO_ApplyDark_GlowFun(Mat DarkMasterImage, Mat srcImage, Mat BiasImage)
        {
            // 1.使用与样本图像l2相同的箱子加载暗主图像l1 。
            // 设定参数
            Mat l1 = new Mat();
            Mat l2 = new Mat();
            Cv2.Subtract(DarkMasterImage, BiasImage, l1);
            int interval = 5;
            // 2.在l1的4个角测量300x300像素盒的平均强度。
            int m = l1.Height;
            int n = l1.Width;
            int box = 300;
            int box_m = m - box;
            int box_n = n - box;
            float d1 = 0, d2 = 0, d3 = 0, d4 = 0;
            //
            l1.ConvertTo(l1, 5);
            for (int x = 0; x < box; x++)
            {
                for (int y = 0; y < box; y++)
                {
                    d1 += l1.At<float>(x, y);
                }
            }
            d1 /= 900;
            //
            for (int x = box_m; x < m; x++)
            {
                for (int y = 0; y < box; y++)
                {
                    d2 += l1.At<float>(x, y);
                }
            }
            d2 /= 900;
            //
            for (int x = 0; x < box; x++)
            {
                for (int y = box_n; y < n; y++)
                {
                    d3 += l1.At<float>(x, y);
                }
            }
            d3 /= 900;
            //
            for (int x = box_m; x < m; x++)
            {
                for (int y = box_n; y < n; y++)
                {
                    d4 += l1.At<float>(x, y);
                }
            }
            d4 /= 900;

            // 3.在强度最高的角落，定义一个W/5 * H/5区域（W是图像宽度和H是图像高度）。
            int h_5 = m / 5;
            int w_5 = n / 5;
            //cout << h_5 << "," << w_5 << endl;
            //system("pause");
            int interval_h = (h_5 / interval);
            int interval_w = (w_5 / interval);
            l2.ConvertTo(l2, 5);
            float s_1, s_2;
            int array_index = 0;
            int x_sta = 0, x_end = 0, y_sta = 0, y_end = 0;
            // 最强角落的判断和位置确定
            if (d1 > d2 && d1 > d3 && d1 > d4)
            {
                x_sta = 0;
                x_end = h_5;
                y_sta = 0;
                y_end = w_5;
            }
            else if (d2 > d1 && d2 > d3 && d2 > d4)
            {
                x_sta = m - h_5;
                x_end = m;
                y_sta = 0;
                y_end = w_5;

            }
            else if (d3 > d1 && d3 > d2 && d3 > d4)
            {
                x_sta = 0;
                x_end = h_5;
                y_sta = n - w_5;
                y_end = n;
            }
            else if (d4 > d1 && d4 > d2 && d4 > d3)
            {
                x_sta = m - h_5;
                x_end = m;
                y_sta = n - w_5;
                y_end = n;
            }
            // 4.在W/5 * H/5区域均匀定义 5*5 采样网格 。
            // 5.在25个网格点的每一个上，测量一个W/100x H/100盒子的平均强度，并将这25个测量值存储在阵列D1中。
            // 6.在l2的同一个角落，以相同的采样顺序执行相同的 25 次测量，并将25次测量存储在阵列D2中。
            // 第 4,5,6 步一起实现。
            // 采样网格 。
            int h_100 = m / 100;
            int w_100 = n / 100;

            x_end = x_sta + interval_h * 5;
            y_end = y_sta + interval_w * 5;

            int center_dis_x = (interval_h - h_100) / 2;
            int center_dis_y = (interval_w - w_100) / 2;
            float[,] D_array = new float[25, 2];
            for (int x = x_sta; x < x_end; x = x + interval_h)
            {
                for (int y = y_sta; y < y_end; y = y + interval_w)
                {
                    s_1 = 0;
                    s_2 = 0;
                    for (int x_1 = x + center_dis_x; x_1 < x + h_100 + center_dis_x; x_1++)
                    {
                        for (int y_1 = y + center_dis_y; y_1 < y + w_100 + center_dis_y; y_1++)
                        {
                            s_1 += l1.At<float>(x_1, y_1);
                            s_2 += l2.At<float>(x_1, y_1);
                        }
                    }
                    s_1 /= ((float)w_100 * (float)h_100);
                    s_2 /= ((float)w_100 * (float)h_100);
                    // 存到数组
                    D_array[array_index,0] = s_1;
                    D_array[array_index,1] = s_2;
                    array_index++;
                }
            }

            //7.进行线性拟合，得到拟合函数Y = aX + b，其中X为D1，Y为D2。
            
            List<OpenCvSharp.Point2f> points=new List<OpenCvSharp.Point2f>();
            for (int i = 0; i < 24; ++i)
            {
                points.Add(new OpenCvSharp.Point2f(D_array[i,0], D_array[i,1]));
            }
            var line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
            double k = line.Vy / line.Vx;//直线斜率
            double b = line.Y1 - k * line.X1;

            // 8.执行 l1 * a + b，为样本图像L2生成模拟暗图像l3。
            Mat l3, l4;
            l1.ConvertTo(l1, 2);
            //imwrite("l1l.tif", l1);
            l1.ConvertTo(l1, 5);
            l3 = l1 * k + b;

            // 9.执行图像减法l2 - l3，返回同时进行暗校正和辉光校正的结果图像。
            l4 = l2 - l3;
            int rows = l4.Rows;
            int cols = l4.Cols;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int l4Pixel = l4.At<int>(i, j);
                    if (l4Pixel < 0)
                    {
                        l4.At<int>(i, j) = 0;
                    }

                }
            }
            double minVal = 0.0;
            double maxVal = 0.0;
            Cv2.MinMaxLoc(l4, out minVal, out maxVal);
            l4 -= minVal;

            // 10. 保存 l3 和 l4。
            l3.ConvertTo(l3, 2);
            //imwrite("l3.tif", l3);
            l4.ConvertTo(l4, 2);
            return l4;

        }

        public Mat ChemiSOLO_LineCalibration(Mat l1, Mat M1, Mat srcImage)
        {
            Mat l2 = new Mat();
            Mat l3 = new Mat();
            // 2. 在图像l1上使用自动阈值，为背景生成掩膜图像M1(255为背景，0为印迹膜); 在收集化学图像
            // 后，应用暗色主色生成图像12（尺寸为mxn）。
            // otsu
            srcImage.CopyTo(l2);
            double minVal = 0.0;
            double maxVal = 0.0;
            M1.ConvertTo(M1, 2);
            M1 = 255 - M1;
            l2.ConvertTo(l2, 5);
            Cv2.MinMaxLoc(l2, out minVal, out maxVal);
            // 3. 执行M1 * 11 / 255，得到背景图像l3（斑点膜的像素强度为0)。
            l1.ConvertTo(l1, 5);
            M1.ConvertTo(M1, 5);
            M1 = M1 / 255;
            l3 = M1.Mul(l1);
            l3 = l3 / 255;
            l3.ConvertTo(l3, 2);
            l3.ConvertTo(l3, 5);
            l2.ConvertTo(l2, 5);
            int rows = l3.Rows;
            int cols = l3.Cols;
            // 4. 计算图像 l3 中每一行像素的平均强度（计算中不包括印迹膜像素）, 并保存为一个列向量 C （ 大小为 m x 1 ）。
            // 5. 从图像l2中减去向量C，以去除线条，并将任何负值强制为0。
            // 第 4，5 步同时实现。
            float s;
            for (int row = 0; row < rows; row++)
            {
                s = 0;
                for (int col = 0; col < cols; col++)
                {
                    s += l3.At<float>(row, col);
                }
                s = s / cols;
                for (int col = 0; col < cols; col++)
                {
                    l2.At<float>(row, col) = l2.At<float>(row, col) - s;
                    if (l2.At<float>(row, col) < 0)
                    {
                        l2.At<float>(row, col) = 0;
                    }
                }
            }
            // 6. 返回结果图像。
            l2.ConvertTo(l2, 2);
            return l2;
        }

        public WriteableBitmap ChemiSOLO_CalculateFlatCorrectedImage(WriteableBitmap srcImage, int lightType, out string strCorrection, System.Drawing.Rectangle scalingRect,double average)
        {
            string strLookup = string.Empty;
            ImageArithmetic imageArith = new ImageArithmetic();
            ImageStatistics imageStat = new ImageStatistics();
            double avg2 = 0.0;
            strCorrection = string.Empty;
            strLookup = lightType.ToString();

            if (!_FlatDictionary.ContainsKey(strLookup))
            {
                strCorrection = string.Empty;
                return srcImage;
            }
            MasterFile flatFieldMaster = _FlatDictionary[strLookup];
            WriteableBitmap flatImage = ImageProcessing.LoadImageFile(flatFieldMaster.FileInfo.FullName);

            if (flatImage == null)
            {
                strCorrection = string.Empty;
                return srcImage;
            }
            float[] flatImageResurtMatrix = null;
            //Load flat image F (same channel as source image S) and source image S and ptMax and meanI;
            // Convert flat image F to 2-D floating point matrix f;
            //Convert source image S to 2-D floating point matrix s;
            //Create corrected matrix s’ by dividing matrix s by matrix f;

            ImagingSystemHelper.ChemiSOLO_ImageDivAndScale(srcImage, flatImage, out flatImageResurtMatrix);
            //Get the average intensity meanI’ of the 5x5 region around ptMax  in the corrected matrix s’; (alternative is using the mean of the flat image F)
            avg2 = imageStat.ChemiSOLO_GetS_FImageMatrixAverage(srcImage, flatImageResurtMatrix, scalingRect);
            //Calculate the scaling factor scaledFactor meanI/meanI’;
            //Multiply the corrected matrix s’ by scaledFactor;
            //Convert the corrected matrix s’ to the corrected image S’ (16 bit);
            if (average != 0 && avg2 != 0)
            {
                double scaleFactor = average / avg2;
                srcImage=imageArith.ChemiSOLO_MatrixMultiplyScaledFactor(srcImage, flatImageResurtMatrix, scaleFactor);
            }
            if (srcImage != null)
            {
                strCorrection = "Flat";
            }

            flatImage = null;

            return srcImage;
        }


        #endregion





        /***#region public BitmapSource CalculateFlatCorrectedImage(BitmapSource srcImage, int filterType, int apertureType, out string strCorrection)
        /// <summary>
        /// Calculate flat-field corrected image from master file.
        /// </summary>
        /// <remarks>
        /// Corrected_Image = NormFactor * srcImage / FlatImage
        /// NormFactor is the average pixel value from the flat-field image.
        /// </remarks>
        /// <param name="srcImage"></param>
        /// <param name="filterType"></param>
        /// <param name="apertureType"></param>
        /// <param name="strCorrection"></param>
        /// <returns></returns>
        public BitmapSource CalculateFlatCorrectedImage(BitmapSource srcImage, int filterType, int apertureType, out string strCorrection)
        {
            string strLookup = string.Empty;

            strCorrection = string.Empty;

            strLookup = string.Format("{0}x{1}", filterType,  apertureType);

            if (!_FlatDictionary.ContainsKey(strLookup))
            {
                strCorrection = string.Empty;
                return srcImage;
            }

            MasterFile flatFieldMaster = _FlatDictionary[strLookup];
            BitmapSource flatImage = MVImage.Load(flatFieldMaster.FileInfo.FullName);

            if (flatImage == null)
            {
                strCorrection = string.Empty;
                return srcImage;
            }

            if (srcImage.PixelHeight != flatImage.PixelHeight && srcImage.PixelWidth != flatImage.PixelWidth)
            {
                int iWidth = srcImage.PixelWidth;
                int iHeight = srcImage.PixelHeight;

                System.Drawing.Size newImageSize = new System.Drawing.Size(iWidth, iHeight);

                flatImage = MVImage.ImageResize(flatImage, newImageSize);
            }

            bool bResult = FlatFieldCalibrate.Apply(ref srcImage, ref flatImage, false);

            if (bResult)
            {
                strCorrection = "Flat";
            }

            return srcImage;

        }
        #endregion***/

        #region public WriteableBitmap CalculateFlatCorrectedImage(WriteableBitmap srcImage, int lightType, int apertureType, bool bIsTopShelf, int chemiRank, out string strCorrection)
        /// <summary>
        /// Calculate flat-field corrected image from master file.
        /// </summary>
        /// <remarks>
        /// Corrected_Image = srcImage / flatImage * 6500/max
        /// </remarks>
        /// <param name="srcImage"></param>
        /// <param name="lightType"></param>
        /// <param name="apertureType"></param>
        /// <param name="strCorrection"></param>
        /// <returns></returns>
        //TODO: FIX ME (make changes for scanner only)
        public WriteableBitmap CalculateFlatCorrectedImage(WriteableBitmap srcImage, int lightType, out string strCorrection)
        {
            string strLookup = string.Empty;    // new flat-field image naming convention
            //string strLookupOld = string.Empty; // old flat-field image naming convention
            ImageArithmetic imageArith = new ImageArithmetic();

            strCorrection = string.Empty;

            // Light source (None = ZERO = Chemi)
            /*if ((ScannerDataStruct.APDLaserLedType)lightType == ScannerDataStruct.APDLaserLedType.ZERO)
            {
                string strShelf = (bIsTopShelf) ? "2" : "1"; // 1 = bottom shelf (or no shelf); 2 = top shelf
                strLookup = string.Format("{0}x{1}x{2}x{3}", filterType, lightType, apertureType, strShelf);
                strLookupOld = string.Format("{0}x{1}x{2}", lightType, apertureType, strShelf);
            }
            else
            {
                //special case: c280: RGB marker and RGB coomassie
                string strShelf = (bIsTopShelf) ? "x2" : ""; // 2 = top shelf

                strLookup = string.Format("{0}x{1}x{2}{3}", filterType, lightType, apertureType, strShelf);
                strLookupOld = string.Format("{0}x{1}{2}", lightType, apertureType, strShelf);
            }*/

            strLookup = lightType.ToString();

            if (!_FlatDictionary.ContainsKey(strLookup))
            {
                strCorrection = string.Empty;
                return srcImage;
            }
            /*else if (_FlatDictionary.ContainsKey(strLookupOld) &&
                    !_FlatDictionary.ContainsKey(strLookup))
            {
                // Use flat-field image with the old naming convention
                // ApetureType for Gel for non-c200 is: 4
                if ((filterType == 2 && (strLookupOld == "1x4" || strLookupOld == "2x4" || strLookupOld == "8x4" || strLookupOld == "11x4")) ||
                    (filterType == 5 && strLookupOld == "3x2") ||
                    (filterType == 4 && strLookupOld == "4x2") ||
                    (filterType == 3 && strLookupOld == "5x2") ||
                    (filterType == 6 && strLookupOld == "6x2") ||
                    (filterType == 7 && strLookupOld == "7x2") ||
                    (filterType == 1 && strLookupOld == "10x1x1") ||
                    (filterType == 1 && strLookupOld == "10x1x2"))
                {
                    strLookup = strLookupOld;
                }
            }*/

            MasterFile flatFieldMaster = _FlatDictionary[strLookup];
            WriteableBitmap flatImage = ImageProcessing.LoadImageFile(flatFieldMaster.FileInfo.FullName);

            if (flatImage == null)
            {
                strCorrection = string.Empty;
                return srcImage;
            }

            if (srcImage.PixelHeight != flatImage.PixelHeight && srcImage.PixelWidth != flatImage.PixelWidth)
            {
                int iWidth = srcImage.PixelWidth;
                int iHeight = srcImage.PixelHeight;

                System.Windows.Size newImageSize = new System.Windows.Size(iWidth, iHeight);
                flatImage = ImageProcessing.Resize(flatImage, newImageSize);
            }

            srcImage = ImagingSystemHelper.ImageDivAndScale(srcImage, flatImage, 65000);
            srcImage = imageArith.Subtract(srcImage, 5);

            if (srcImage != null)
            {
                strCorrection = "Flat";
            }

            flatImage = null;

            return srcImage;

        }
        #endregion

        /*#region public WriteableBitmap CalculateFlatCorrectedImageWithROI(WriteableBitmap srcImage, int lightType, int apertureType, out string strCorrection)
        /// <summary>
        /// Calculate flat-field corrected image from master file.
        /// </summary>
        /// <remarks>
        /// Corrected_Image = srcImage / flatImage * 6500/max
        /// </remarks>
        /// <param name="srcImage"></param>
        /// <param name="lightType"></param>
        /// <param name="apertureType"></param>
        /// <param name="strCorrection"></param>
        /// <returns></returns>
        public WriteableBitmap CalculateFlatCorrectedImageWithROI(WriteableBitmap srcImage, int filterType, int lightType, int apertureType, out string strCorrection)
        {
            string strLookup = string.Empty;    // new flat-field image naming convention
            string strLookupOld = string.Empty; // old flat-field image naming convention
            int iOffset = 100;   // number of pixels from the edges

            strCorrection = string.Empty;

            strLookup = string.Format("{0}x{1}x{2}", filterType, lightType, apertureType);
            strLookupOld = string.Format("{0}x{1}", lightType, apertureType);

            if (!_FlatDictionary.ContainsKey(strLookup) &&
                !_FlatDictionary.ContainsKey(strLookupOld))
            {
                strCorrection = string.Empty;
                return srcImage;
            }
            else if (_FlatDictionary.ContainsKey(strLookupOld) &&
                    !_FlatDictionary.ContainsKey(strLookup))
            {
                // Use flat-field image with the old naming convention
                // ApetureType for Gel for c200 is: 2
                if ((filterType == 2 && (strLookupOld == "1x2" || strLookupOld == "2x2" || strLookupOld == "8x2" || strLookupOld == "11x2")) ||
                    (filterType == 5 && strLookupOld == "3x2") ||
                    (filterType == 4 && strLookupOld == "4x2") ||
                    (filterType == 3 && strLookupOld == "5x2") ||
                    (filterType == 6 && strLookupOld == "6x2") ||
                    (filterType == 7 && strLookupOld == "7x2") ||
                    (filterType == 1 && strLookupOld == "10x1x1") ||
                    (filterType == 1 && strLookupOld == "10x1x2"))
                {
                    strLookup = strLookupOld;
                }
            }

            MasterFile flatFieldMaster = _FlatDictionary[strLookup];
            WriteableBitmap flatImage = ImageProcessing.Load(flatFieldMaster.FileInfo.FullName);

            if (flatImage == null)
            {
                strCorrection = string.Empty;
                return srcImage;
            }

            if (srcImage.PixelHeight != flatImage.PixelHeight && srcImage.PixelWidth != flatImage.PixelWidth)
            {
                int iWidth = srcImage.PixelWidth;
                int iHeight = srcImage.PixelHeight;

                Size newImageSize = new Size(iWidth, iHeight);

                flatImage = ImageProcessing.Resize(flatImage, newImageSize);
            }

            srcImage = ImageProcessing.ImageDivAndScaleWithROI(srcImage, flatImage, 65000, iOffset);

            if (srcImage != null)
            {
                strCorrection = "Flat";
            }

            flatImage = null;

            return srcImage;

        }
        #endregion*/

        #region public WriteableBitmap CalculateFlatCorrectedImage(WriteableBitmap srcImage, WriteableBitmap flatImage, out string strCorrection)
        /// <summary>
        /// VISIBLE SOFTWARE WORK-AROUND: apply flat using dynamic flat
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="flatImage"></param>
        /// <param name="strCorrection"></param>
        /// <returns></returns>
        public WriteableBitmap CalculateFlatCorrectedImage(WriteableBitmap srcImage, WriteableBitmap flatImage, out string strCorrection)
        {
            ImageArithmetic imageArith = new ImageArithmetic();

            strCorrection = string.Empty;

            if (flatImage == null)
            {
                strCorrection = string.Empty;
                return srcImage;
            }

            if (srcImage.PixelHeight != flatImage.PixelHeight && srcImage.PixelWidth != flatImage.PixelWidth)
            {
                int iWidth = srcImage.PixelWidth;
                int iHeight = srcImage.PixelHeight;

                System.Windows.Size newImageSize = new System.Windows.Size(iWidth, iHeight);

                flatImage = ImageProcessing.Resize(flatImage, newImageSize);
            }

            srcImage = ImagingSystemHelper.ImageDivAndScale(srcImage, flatImage, 65000);
            //srcImage = imageArith.Subtract(srcImage, 5);

            if (srcImage != null)
            {
                strCorrection = "Flat";
            }

            return srcImage;

        }
        #endregion

        #region private BitmapSource ImageBufferToBitmapSource(byte[] srcImageBuffer, int iWidth, int iHeight)
        /// <summary>
        /// Image buffer to 16-bit BitmapSource.
        /// </summary>
        /// <param name="srcImageBuffer"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="iBitsPerPixel"></param>
        /// <returns></returns>
        private BitmapSource ImageBufferToBitmapSource(byte[] srcImageBuffer, int iWidth, int iHeight)
        {
            int bitsPerPixel = 16;
            int iStride = (iWidth * bitsPerPixel + 7) / 8;
            BitmapSource bitmap = BitmapSource.Create(iWidth, iHeight, 96, 96, PixelFormats.Gray16, null, srcImageBuffer, iStride);
            return bitmap;
        }
        #endregion

    }


    #region public enum MasterFileType
    /// <summary>
    /// Master file enumerated type.
    /// </summary>
    public enum MasterFileType
    {
        Bias,
        Dark,
        Flat
    }
    #endregion

    #region public class MasterFile
    /// <summary>
    /// Master file (darkmaster and bias) information class.
    /// </summary>
    public class MasterFile
    {
        private FileInfo _FileInfo;
        private MasterFileType _MasterFileType = MasterFileType.Bias;
        private double _ExposureTime;
        private int _VerticalBins = 1;
        private int _HorizontalBins = 1;
        //private int            _Gain            = 0;
        private int _Speed = 0;

        #region public MasterFileType  MasterFileType
        /// <summary>
        /// Get/set the MasterFileType (bias, dark or flat).
        /// </summary>
        public MasterFileType MasterFileType
        {
            get { return _MasterFileType; }
            set
            {
                _MasterFileType = value;
                //OnPropertyChanged("MasterFileType");
            }
        }
        #endregion

        #region public double ExposureTime
        /// <summary>
        /// Get/set the exposure time in seconds.
        /// </summary>
        public double ExposureTime
        {
            get { return _ExposureTime; }
            set
            {
                _ExposureTime = value;
                //OnPropertyChanged("ExposureTime");
            }
        }
        #endregion

        #region  public int    VerticalBins
        /// <summary>
        /// Get/set the vertical binning factor.
        /// </summary>
        public int VerticalBins
        {
            get { return _VerticalBins; }
            set
            {
                _VerticalBins = value;
                //OnPropertyChanged("VerticalBins");
            }
        }
        #endregion

        #region  public int    HorizontalBins
        /// <summary>
        /// Get/set the horizontal binning factor.
        /// </summary>
        public int HorizontalBins
        {
            get { return _HorizontalBins; }
            set
            {
                _HorizontalBins = value;
                //OnPropertyChanged("HorizontalBins");
            }
        }
        #endregion

        //public int Gain
        //{
        //    get { return _Gain; }
        //    set
        //    {
        //        _Gain = value;
        //        //OnPropertyChanged("Gain");
        //    }
        //}

        public int Speed
        {
            get { return _Speed; }
            set
            {
                _Speed = value;
                //OnPropertyChanged("Speed");
            }
        }

        #region public FileInfo FileInfo
        /// <summary>
        /// Get the master file, file info.
        /// </summary>
        public FileInfo FileInfo
        {
            get { return _FileInfo; }
        }
        #endregion


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath"></param>
        public MasterFile(FileInfo fileInfo)
        {
            _FileInfo = fileInfo;
        }

    }
    #endregion
}
