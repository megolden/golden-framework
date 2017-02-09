using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden
{
	public struct Range<T> where T : IComparable<T>
	{
        private readonly T _Min, _Max;

		public T MinValue
        {
            get { return _Min; }
        }
		public T MaxValue
        {
            get { return _Max; }
        }

        public Range(T minValue, T maxValue)
		{
			_Min = minValue;
			_Max = maxValue;
		}
		public bool Contains(T value)
		{
			return Utility.Utilities.IsBetween<T>(value, _Min, _Max);
		}
		public bool Contains(T value, bool includeMaxBound)
		{
			return Utility.Utilities.IsBetween<T>(value, this.MinValue, this.MaxValue, includeMaxBound);
		}
	}
}
