using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Azure.ImagingSystem
{

    public enum ChemiSampleType
    {
        Auto_detect,
        Qpaque,
        Translucent,
    }
    public enum ChemiMarkerType
    {
        None,
        TureColor,
        Grayscale,
    }
    public enum ChemiApplicationType
    {
        Chemi_Imaging,
        TrueColor_Imaging,
        Grayscale_Imaging,
    }
    public enum ChemiModeType
    {
        Single,
        Cumulative,
        Multiple,
    }

    public enum ChemiExposureType
    {
        RapidCapture,
        Extended_Dynamic_Range,
        Overexposure,
        Manual,
    }
    public class RgbLedIntensity
    {
        #region Public properties...

        public int Position { get; set; }

        public int Intensity { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public RgbLedIntensity()
        {
        }

        public RgbLedIntensity(int position, int intensity, string displayName)
        {
            this.Position = position;
            this.Intensity = intensity;
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class GainType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public GainType()
        {
        }

        public GainType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class CameraModeSetting
    {
        private ChemiSetting _ChemiSettings = new ChemiSetting();
        private CameraSettings _CameraSettings = new CameraSettings();
        public ChemiSetting ChemiSettings
        {
            get { return _ChemiSettings; }
            set { _ChemiSettings = value; }
        }

        public CameraSettings CameraSettings
        {
            get { return _CameraSettings; }
            set { _CameraSettings = value; }
        }
    }

    public class ChemiSetting
    {
        public int NumFrames { get; set; }
        public int Gain { get; set; } = 300;
        public int FlatsAutoExposureOptimal { get; set; } = 40000;

        public bool IsDynamicDarkCorrection { get; set; }
        public double MaxExposure { get; set; }
        public int RGBImageGain { get; set; } = 1;
        public int ChemiImageGain { get; set; } = 100;
        public int RapidCapture { get; set; }
        public int DynamicRange { get; set; }
        public int OverExposeure { get; set; }

        public double InitialExposureTime { get; set; }
        public double ChemiInitialExposureTime { get; set; }
        public bool Chemi_NewAlgo_Enable { get; set; }
        public double Chemi_T1 { get; set; }
        public string Chemi_binning_Kxk { get; set; }
        public int Chemi_Intensity { get; set; }

        public bool Dark_GlowCorrection { get; set; }
        public bool LineCorrection { get; set; }
        public bool DespecklerCorrection { get; set; }
        public bool FlatfieldCorrection { get; set; }
        public bool LensDistortionCorrection { get; set; }

        public double paramA { get; set; }
        public double paramB { get; set; }
        public double paramC { get; set; }
        public double BlotFindExposureTime { get; set; }
        public double SampleType_threshold { get; set; }
        public int BlotPvCamScalingThreshold { get; set; }
        public int GelPvCamScalingThreshold { get; set; }
        public ChemiSetting()
        {
        }
    }

    public class CameraSettings
    {
        public double AbsXPos { get; set; }
        public double AbsYPos { get; set; }
        public CameraSettings()
        {
        }
    }


}
