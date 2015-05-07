﻿using System;
using System.Collections.Generic;

namespace PPMErrorCharter
{
    public class IdentData : IEquatable<IdentData>, IComparable<IdentData>
    {
        public readonly double IsotopeErrorFilterWindow = 0.2;
        public readonly double PpmErrorFilterWindow = 50.0;
        private const double IsotopeErrorTestWindow = 0.05;
        private const double IsotopeErrorFixWindow = 0.15;

        public string NativeId;
        public ulong ScanId;
        public double ScanTimeSeconds { get; set; } // Property for reflection by OxyPlot
        public string IdField;
        public string IdValue;
        public double ThresholdValue;
        public int _charge;
        private double _calcMz;
        private double _experMz;
        private double _experMzRefined;
        private double _experMzIsotoped;
        private double _experMzRefinedIsotoped;
        private bool _isSetCalcMz;
        private bool _isSetExperMz;
        private bool _isStoredExperMz;
        private bool _isSetExperMzRefined;
        private bool _isStoredExperMzRefined;
        private bool _fixIsoLocked;
        private bool _internalOp;

        private enum CheckedIsotopeError : byte
        {
            Yes,
            No,
            Unknown,
        }
        private CheckedIsotopeError _hasIsotopeError;
        private int _isotopeErrorCount;
        private double _isotopeErrorAdjustment;
        private CheckedIsotopeError _hasRefinedIsotopeError;
        private int _refinedIsotopeErrorCount;
        private double _refinedIsotopeErrorAdjustment;

        public int ScanIdInt
        {
            get { return Convert.ToInt32(ScanId); }
        }

        public int Charge
        {
            get { return _charge; }
            set
            {
                _charge = value;
                _hasIsotopeError = CheckedIsotopeError.Unknown;
                if (!_fixIsoLocked)
                {
                    _fixIsoLocked = true;
                    FindIsotopeError();
                    FixIsotopeError();
                    _fixIsoLocked = false;
                }
            }
        }

        public double CalcMz
        {
            get { return _calcMz; }
            set
            {
                _calcMz = value;
                _isSetCalcMz = true;
                bool runIsotopeCheck = false;
                bool locked = _fixIsoLocked;
                _fixIsoLocked = true; // Value only changed if was false
                if (_isSetExperMz && _calcMz != 0.0)
                {
                    runIsotopeCheck = true;
                    MassError = _experMz - _calcMz;
                    PpmError = (MassError / _calcMz) * 1.0e6;
                }
                if (_isSetExperMzRefined && _calcMz != 0.0)
                {
                    runIsotopeCheck = true;
                    MassErrorRefined = _experMzRefined - _calcMz;
                    PpmErrorRefined = (MassErrorRefined / _calcMz) * 1.0e6;
                }
                if (runIsotopeCheck && !locked) // Will only run if was not locked
                {
                    FindIsotopeError();
                    FixIsotopeError();
                }
                _fixIsoLocked = locked; // value restored to previous state
            }
        }

        public double ExperMz
        {
            get { return _experMz; }
            set
            {
                _experMz = value;
                _isSetExperMz = true;
                if (!_internalOp)
                {
                    _isStoredExperMz = false;
                }
                if (_isSetCalcMz && _calcMz != 0.0)
                {
                    MassError = _experMz - _calcMz;
                    PpmError = (MassError / _calcMz) * 1.0e6;
                    bool locked = _fixIsoLocked;
                    _fixIsoLocked = true; // Value only changed if was false
                    if (!locked) // Will only run if was not locked
                    {
                        FindIsotopeError();
                        FixIsotopeError();
                    }
                    _fixIsoLocked = locked; // value restored to previous state
                }
            }
        }

        public double ExperMzIsotoped
        {
            get { return _experMzIsotoped; }
            private set
            {
                _experMzIsotoped = value;
                _isStoredExperMz = true;
                if (_isSetCalcMz && _calcMz != 0.0)
                {
                    MassErrorIsotoped = _experMzIsotoped - _calcMz;
                    PpmErrorIsotoped = (MassErrorIsotoped / _calcMz) * 1.0e6;
                }
            }
        }

        public double ExperMzRefined
        {
            get { return _experMzRefined; }
            set
            {
                _experMzRefined = value;
                _isSetExperMzRefined = true;
                if (!_internalOp)
                {
                    _isStoredExperMzRefined = false;
                }
                if (_isSetCalcMz && _calcMz != 0.0)
                {
                    bool locked = _fixIsoLocked;
                    _fixIsoLocked = true; // Value only changed if was false

                    MassErrorRefined = _experMzRefined - _calcMz;
                    PpmErrorRefined = (MassErrorRefined / _calcMz) * 1.0e6;

                    if (!locked) // Will only run if was not locked
                    {
                        FindIsotopeError();
                        FixIsotopeError();
                    }
                    _fixIsoLocked = locked; // value restored to previous state
                }
            }
        }

        public double ExperMzRefinedIsotoped
        {
            get { return _experMzRefinedIsotoped; }
            private set
            {
                _experMzRefinedIsotoped = value;
                if (_isSetCalcMz && _calcMz != 0.0)
                {
                    MassErrorRefinedIsotoped = _experMzRefinedIsotoped - _calcMz;
                    PpmErrorRefinedIsotoped = (MassErrorRefinedIsotoped / _calcMz) * 1.0e6;
                }
            }
        }

        public double MassError { get; private set; }
        public double PpmError { get; private set; }
        public double MassErrorRefined { get; private set; }
        public double PpmErrorRefined { get; private set; }
        public double MassErrorIsotoped { get; private set; }
        public double PpmErrorIsotoped { get; private set; }
        public double MassErrorRefinedIsotoped { get; private set; }
        public double PpmErrorRefinedIsotoped { get; private set; }

        public IdentData()
        {
            NativeId = "";
            ScanId = 0;
            ScanTimeSeconds = -1;
            MassError = 0.0;
            PpmError = 0.0;
            Charge = 0;
            ThresholdValue = 0.0;
            _calcMz = 0.0;
            _experMz = 0.0;
            _experMzRefined = 0.0;
            _isSetCalcMz = false;
            _isSetExperMz = false;
            _isSetExperMzRefined = false;
            _hasIsotopeError = CheckedIsotopeError.Unknown;
            _isotopeErrorCount = 0;
            _isotopeErrorAdjustment = 0.0;
            _hasRefinedIsotopeError = CheckedIsotopeError.Unknown;
            _refinedIsotopeErrorCount = 0;
            _refinedIsotopeErrorAdjustment = 0.0;
            _experMzIsotoped = 0.0;
            _experMzRefinedIsotoped = 0.0;
            MassError = 0.0;
            PpmError = 0.0;
            MassErrorRefined = 0.0;
            PpmErrorRefined = 0.0;
            MassErrorIsotoped = 0.0;
            PpmErrorIsotoped = 0.0;
            MassErrorRefinedIsotoped = 0.0;
            PpmErrorRefinedIsotoped = 0.0;
            _fixIsoLocked = false;
            _internalOp = false;
            _isStoredExperMz = false;
            _isStoredExperMzRefined = false;
        }

        private void FindIsotopeError()
        {
            if (_hasIsotopeError != CheckedIsotopeError.Unknown)
            {
                return;
            }
            if (!(_isSetCalcMz && _isSetExperMz) || _charge == 0)
            {
                return;
            }
            // Assume that it doesn't, and only change it if it does.
            _hasIsotopeError = CheckedIsotopeError.No;
            if (_charge == 0 || (-IsotopeErrorFixWindow < MassError && MassError < IsotopeErrorFixWindow))
            {
                return;
            }
            double chargeWithSign = _charge;
            if (MassError < 0)
            {
                chargeWithSign = -chargeWithSign;
            }
            for (int i = 1; i <= 5; ++i)
            {
                double adjustment = (double)i / chargeWithSign;
                if ((adjustment - IsotopeErrorTestWindow) <= MassError && MassError <= (adjustment + IsotopeErrorTestWindow))
                {
                    _hasIsotopeError = CheckedIsotopeError.Yes;
                    _isotopeErrorCount = MassError < 0 ? -i : i;
                    _isotopeErrorAdjustment = adjustment;
                    break;
                }
            }
        }

        private void FindIsotopeErrorRefined()
        {
            if (_hasRefinedIsotopeError != CheckedIsotopeError.Unknown)
            {
                return;
            }
            if (!(_isSetCalcMz && _isSetExperMzRefined) || _charge == 0)
            {
                return;
            }
            // Assume that it doesn't, and only change it if it does.
            _hasRefinedIsotopeError = CheckedIsotopeError.No;
            if (_charge == 0 || (-IsotopeErrorFixWindow < MassErrorRefined && MassErrorRefined < IsotopeErrorFixWindow))
            {
                return;
            }
            double chargeWithSign = _charge;
            if (MassErrorRefined < 0)
            {
                chargeWithSign = -chargeWithSign;
            }
            for (int i = 1; i <= 5; ++i)
            {
                double adjustment = (double)i / chargeWithSign;
                if ((adjustment - IsotopeErrorTestWindow) <= MassErrorRefined && MassErrorRefined <= (adjustment + IsotopeErrorTestWindow))
                {
                    _hasRefinedIsotopeError = CheckedIsotopeError.Yes;
                    _refinedIsotopeErrorCount = MassError < 0 ? -i : i;
                    _refinedIsotopeErrorAdjustment = adjustment;
                    break;
                }
            }
        }

        private void FixIsotopeError()
        {
            _internalOp = true;
            if (_hasIsotopeError != CheckedIsotopeError.Unknown)
            {
                if (!_isStoredExperMz)
                {
                    ExperMzIsotoped = ExperMz;
                }
                if (_isSetExperMzRefined && !_isStoredExperMzRefined)
                {
                    ExperMzRefinedIsotoped = ExperMzRefined;
                }
            }
            if (_hasIsotopeError == CheckedIsotopeError.Yes)
            {
                ExperMz = ExperMzIsotoped - _isotopeErrorAdjustment;
                if (_isSetExperMzRefined && (PpmErrorRefinedIsotoped < -50 || 50 < PpmErrorRefinedIsotoped))
                {
                    FindIsotopeErrorRefined();
                    ExperMzRefined = ExperMzRefinedIsotoped - _refinedIsotopeErrorAdjustment;
                }
            }
            _internalOp = false;
        }

        public int CompareToByCalcMz(IdentData compareData)
        {
            return compareData == null ? 1 : this.CalcMz.CompareTo(compareData.CalcMz);
        }

        public int CompareTo(IdentData compareData)
        {
            return compareData == null ? 1 : this.ScanId.CompareTo(compareData.ScanId);
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(ScanId);
        }

        public bool Equals(IdentData other)
        {
            return other != null && this.ScanId.Equals(other.ScanId);
        }

        public string ToDebugString()
        {
            return NativeId + "\t" + CalcMz + "\t" + ExperMzIsotoped + "\t" + ExperMzRefinedIsotoped + 
                "\t" + MassErrorIsotoped + "\t" + PpmErrorIsotoped + "\t" + MassErrorRefinedIsotoped + 
                "\t" + PpmErrorRefinedIsotoped + "\t" + Charge;
        }

        public bool OutOfRange()
        {
            bool orig = (MassError < -IsotopeErrorFilterWindow || IsotopeErrorFilterWindow < MassError) ||
                        (PpmError < -PpmErrorFilterWindow || PpmErrorFilterWindow < PpmError);
            bool refined = (MassErrorRefined < -IsotopeErrorFilterWindow || IsotopeErrorFilterWindow < MassErrorRefined) ||
                        (PpmErrorRefined < -PpmErrorFilterWindow || PpmErrorFilterWindow < PpmErrorRefined);
            return orig || refined;
        }
    }

    public class IdentDataByCalcMz : IComparer<IdentData>
    {
        public int Compare(IdentData left, IdentData right)
        {
            if (left == null)
            {
                return right == null ? 0 : -1;
            }
            else
            {
                return right == null ? 1 : left.CalcMz.CompareTo(right.CalcMz);
            }
        }
    }

    public class IdentDataByScanTime : IComparer<IdentData>
    {
        public int Compare(IdentData left, IdentData right)
        {
            if (left == null)
            {
                return right == null ? 0 : -1;
            }
            else
            {
                return right == null ? 1 : left.ScanTimeSeconds.CompareTo(right.ScanTimeSeconds);
            }
        }
    }

    public class IdentDataByPpmError : IComparer<IdentData>
    {
        public int Compare(IdentData left, IdentData right)
        {
            if (left == null)
            {
                return right == null ? 0 : -1;
            }
            else
            {
                return right == null ? 1 : left.PpmError.CompareTo(right.PpmError);
            }
        }
    }

    public class IdentDataByPpmErrorRefined : IComparer<IdentData>
    {
        public int Compare(IdentData left, IdentData right)
        {
            if (left == null)
            {
                return right == null ? 0 : -1;
            }
            else
            {
                return right == null ? 1 : left.PpmErrorRefined.CompareTo(right.PpmErrorRefined);
            }
        }
    }
}
