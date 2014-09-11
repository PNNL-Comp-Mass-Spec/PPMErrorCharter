using System;
using System.Collections.Generic;

namespace PPMErrorCharter
{
	public class IdentData : IEquatable<IdentData>, IComparable<IdentData>
	{
		public string NativeId;
		public ulong ScanId;
		public int Charge;
		public double SpecEValue;
		public double QValue;
		private double _calcMz;
		private double _experMz;
		private double _experMzFixed;
		private bool _isSetCalcMz;
		private bool _isSetExperMz;
		private bool _isSetExperMzFixed;

		public int ScanIdInt
		{
			get { return Convert.ToInt32(ScanId); }
		}

		public double CalcMz
		{
			get { return _calcMz; }
			set
			{
				_calcMz = value;
				if (_isSetCalcMz && _isSetExperMz && _calcMz != 0.0)
				{
					MassError = _experMz - _calcMz;
					PpmError = (MassError / _calcMz) * 1.0e6;
				}
				if (_isSetCalcMz && _isSetExperMzFixed && _calcMz != 0.0)
				{
					MassErrorFixed = _experMzFixed - _calcMz;
					PpmErrorFixed = (MassErrorFixed / _calcMz) * 1.0e6;
				}
				_isSetCalcMz = true;
			}
		}

		public double ExperMz
		{
			get { return _experMzFixed; }
			set
			{
				_experMz = value;
				if (_isSetCalcMz && _calcMz != 0.0)
				{
					MassError = _experMz - _calcMz;
					PpmError = (MassError / _calcMz) * 1.0e6;
				}
				_isSetExperMz = true;
			}
		}

		public double ExperMzFixed
		{
			get { return _experMzFixed; }
			set
			{
				_experMzFixed = value;
				if (_isSetCalcMz && _calcMz != 0.0)
				{
					MassErrorFixed = _experMzFixed - _calcMz;
					PpmErrorFixed = (MassErrorFixed / _calcMz) * 1.0e6;
				}
				_isSetExperMzFixed = true;
			}
		}

		public double MassError { get; private set; }
		public double PpmError { get; private set; }
		public double MassErrorFixed { get; private set; }
		public double PpmErrorFixed { get; private set; }

		public IdentData()
		{
			NativeId = "";
			ScanId = 0;
			MassError = 0.0;
			PpmError = 0.0;
			Charge = 0;
			SpecEValue = 0.0;
			QValue = 0.0;
			_calcMz = 0.0;
			_experMz = 0.0;
			_experMzFixed = 0.0;
			_isSetCalcMz = false;
			_isSetExperMz = false;
			_isSetExperMzFixed = false;
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
}
