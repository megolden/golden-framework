namespace Golden
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Linq;
	using System.ComponentModel;
	using System.Text.RegularExpressions;
	using System.Globalization;

    #region Types

    /// <summary>
    /// The seasons of year.
    /// </summary>
    public enum YearSeason : byte
    {
        /// <summary>
        /// The spring season.
        /// </summary>
        Spring = 0,
        /// <summary>
        /// The summer season.
        /// </summary>
        Summer = 1,
        /// <summary>
        /// The autumn season.
        /// </summary>
        Autumn = 2,
        /// <summary>
        /// The winter season.
        /// </summary>
        Winter = 3
    }

    #endregion

    [Serializable]
	[TypeConverter(typeof(PersianDateTimeConverter))]
	public struct PersianDateTime : IFormattable, IComparable<PersianDateTime>, IEquatable<PersianDateTime>, IComparable, IConvertible, IEqualityComparer<PersianDateTime>
	{
		#region Constants

		/// <summary>
		/// 365 Days
		/// </summary>
		private const long TICKS_YEAR = 315360000000000L;
		/// <summary>
		/// 0622/03/21 00:00:00.000
		/// </summary>
		private const long TICKS_BASE_UTC_OFFSET = 196036416000000000L;
		/// <summary>
		/// 9999/12/29 23:59:59.999
		/// </summary>
		private const long TICKS_MAX = 3155378975999990000L;
		/// <summary>
		/// Max supported year
		/// </summary>
		private const int MAX_YEAR = 9999;

		#endregion
		#region Fields

		private readonly long _Ticks;
		private readonly int _Year, _Month, _Day, _Hour, _Hour12, _Minute, _Second, _Millisecond, _DayOfYear;
		private readonly DayOfWeek _DayOfWeek;
		private readonly string _MonthName, _DayName;
		private readonly TimeSpan _TimeOfDay;
		private readonly bool _IsAfternoon;
		private readonly Lazy<PersianDateTime> _Date;
		/// <summary>
		/// 03:30:00
		/// </summary>
		private static readonly TimeSpan BaseUtcTimeOffset = new TimeSpan(126000000000L);
		private static readonly CultureInfo DefaultCultureInfo;
		public static readonly string[] DayNames = new[] { "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه" };
		public static readonly string[] MonthNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
		public static readonly string[] NativeDigits = new[] { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
		public static readonly PersianDateTime MinValue = new PersianDateTime(0L);
		public static readonly PersianDateTime MaxValue = new PersianDateTime(TICKS_MAX);

		#endregion
		#region Properties

		public static PersianDateTime Today
		{
			get { return new PersianDateTime(DateTime.Today); }
		}
		public static PersianDateTime Now
		{
			get { return new PersianDateTime(DateTime.Now); }
		}
		public static PersianDateTime UtcNow
		{
			get { return new PersianDateTime(DateTime.UtcNow); }
		}
		public string DayName
		{
			get { return _DayName; }
		}
		public string MonthName
		{
			get { return _MonthName; }
		}
		public long Ticks
		{
			get { return _Ticks; }
		}
		public int Year
		{
			get { return _Year; }
		}
		public int Month
		{
			get { return _Month; }
		}
		public int Day
		{
			get { return _Day; }
		}
		public int Hour
		{
			get { return _Hour; }
		}
		public int Hour12
		{
			get { return _Hour12; }
		}
		public bool IsAfternoon
		{
			get { return _IsAfternoon; }
		}
		public int Minute
		{
			get { return _Minute; }
		}
		public int Second
		{
			get { return _Second; }
		}
		public int Millisecond
		{
			get { return _Millisecond; }
		}
		public DayOfWeek DayOfWeek
		{
			get { return _DayOfWeek; }
		}
		public int DayOfYear
		{
			get { return _DayOfYear; }
		}
		public TimeSpan TimeOfDay
		{
			get { return _TimeOfDay; }
		}
		public PersianDateTime Date
		{
			get { return _Date.Value; }
		}

		#endregion
		#region Methods

		static PersianDateTime()
		{
			DefaultCultureInfo = CultureInfo.CreateSpecificCulture("fa-IR");
			#region NumberFormatInfo
			var nf = new NumberFormatInfo();
			nf.NativeDigits = PersianDateTime.NativeDigits;
			nf.CurrencyDecimalSeparator = "/";
			nf.NumberDecimalSeparator = ".";
			nf.CurrencySymbol = "ریال";
			DefaultCultureInfo.NumberFormat = nf;
			#endregion
			#region DateTimeFormatInfo
			var dtf = new DateTimeFormatInfo();
			dtf.DayNames = new[] { DayNames[1], DayNames[2], DayNames[3], DayNames[4], DayNames[5], DayNames[6], DayNames[0] };
			dtf.AbbreviatedDayNames = dtf.DayNames.Select(n => n.Substring(0, 1)).ToArray();
			dtf.ShortestDayNames = (string[])dtf.AbbreviatedDayNames.Clone();
			dtf.MonthNames = new[] { MonthNames[0], MonthNames[1], MonthNames[2], MonthNames[3], MonthNames[4], MonthNames[5], MonthNames[6], MonthNames[7], MonthNames[8], MonthNames[9], MonthNames[10], MonthNames[11], "" };
			dtf.AbbreviatedMonthNames = (string[])dtf.MonthNames.Clone();
			dtf.AbbreviatedMonthGenitiveNames = (string[])dtf.MonthNames.Clone();
			dtf.AMDesignator = "ق.ظ";
			dtf.PMDesignator = "ب.ظ";
			dtf.FirstDayOfWeek = DayOfWeek.Saturday;
			dtf.FullDateTimePattern = "yyyy MMMM dddd, dd HH:mm:ss";
			dtf.LongDatePattern = "yyyy MMMM dddd, dd";
			dtf.ShortDatePattern = "yyyy/MM/dd";
			DefaultCultureInfo.DateTimeFormat = dtf;
			#endregion
		}
		public PersianDateTime(DateTime time)
		{
			PersianDateTime dateTime;
			if (!IsValid(time, out dateTime)) throw new ArgumentOutOfRangeException("time");
			this = dateTime;
			_Date = new Lazy<PersianDateTime>(() => new PersianDateTime(dateTime._Year, dateTime._Month, dateTime._Day));
		}
		public PersianDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
		{
			this = new PersianDateTime();
			long ticks;
			if (!IsValid(year, month, day, hour, minute, second, millisecond, out ticks)) throw new ArgumentException("Invalid date value.");
			_Ticks = ticks;
			_Year = year;
			_Month = month;
			_Day = day;
			_Hour = hour;
			_Minute = minute;
			_Second = second;
			_Millisecond = millisecond;
			_DayOfWeek = GetDayOfWeek(GetWeekDay(_Ticks));
			_MonthName = GetMonthName(_Month);
			_DayName = GetDayName(_DayOfWeek);
			_DayOfYear = GetDayOfYear(_Month, _Day);
			_TimeOfDay = new TimeSpan(0, _Hour, _Minute, _Second, _Millisecond);
			_Hour12 = GetHour12(_Hour);
			_IsAfternoon = (_Hour >= 12);
		}
		public PersianDateTime(long ticks)
		{
			this = new PersianDateTime();

			if (!IsValidTicks(ticks)) throw new ArgumentException("Invalid date value.", nameof(ticks));
			int year, month, day, hour, minute, second, millisecond;
			GetDateParts(ticks, out year, out month, out day, out hour, out minute, out second, out millisecond);
			if (!IsValid(year, month, day, hour, minute, second, millisecond)) throw new ArgumentException("Invalid date value.");
			_Ticks = ticks;
			_Year = year;
			_Month = month;
			_Day = day;
			_Hour = hour;
			_Minute = minute;
			_Second = second;
			_Millisecond = millisecond;
			_DayOfWeek = GetDayOfWeek(GetWeekDay(_Ticks));
			_MonthName = GetMonthName(_Month);
			_DayName = GetDayName(_DayOfWeek);
			_DayOfYear = GetDayOfYear(_Month, _Day);
			_TimeOfDay = new TimeSpan(0, _Hour, _Minute, _Second, _Millisecond);
			_Hour12 = GetHour12(_Hour);
			_IsAfternoon = (_Hour >= 12);
		}
		public PersianDateTime(int year, int month, int day, int hour, int minute, int second) : this(year, month, day, hour, minute, second, 0) { }
		public PersianDateTime(int year, int month, int day) : this(year, month, day, 0, 0, 0) { }
		private static int GetWeekDay(long ticks)
		{
			//5 number in below formula is: week day index (begins from Saturday) of first date (0001/01/01). 
			var days = ticks / TimeSpan.TicksPerDay;
			return (int)((days + 5L) % 7L);
		}
		private static DayOfWeek GetDayOfWeek(int weekDay)
		{
			return (DayOfWeek)((weekDay + 6) % 7);
		}
		private static int GetDayOfYear(int month, int day)
		{
			while (--month > 0)
			{
				day += DaysInMonth(0, month);
			}
			return day;
		}
		private static int GetHour12(int hour)
		{
			int result = hour % 12;
			return (result == 0 ? 12 : result);
		}
		public PersianDateTime AddTicks(long value)
		{
			return new PersianDateTime(_Ticks + value);
		}
		public PersianDateTime Add(TimeSpan value)
		{
			return AddTicks(value.Ticks);
		}
		public PersianDateTime AddDays(long value)
		{
			return AddTicks(TimeSpan.TicksPerDay * value);
		}
		public PersianDateTime AddHours(long value)
		{
			return AddTicks(TimeSpan.TicksPerHour * value);
		}
		public PersianDateTime AddMilliseconds(long value)
		{
			return AddTicks(TimeSpan.TicksPerMillisecond * value);
		}
		public PersianDateTime AddMinutes(long value)
		{
			return AddTicks(TimeSpan.TicksPerMinute * value);
		}
		public PersianDateTime AddMonths(int value)
		{
			int year = _Year;
			int month = _Month;
            bool sign = (value < 0);
            if (sign)
            {
                while (value != 0)
                {
                    month--;
                    if (month < 1)
                    {
                        year--;
                        month = 12;
                    }
                    value++;
                }
            }
            else
            {
                while (value != 0)
                {
                    month++;
                    if (month > 12)
                    {
                        year++;
                        month = 1;
                    }
                    value--;
                }
            }
			int day = DaysInMonth(year, month);
			if (_Day <= day) day = _Day;
            return new PersianDateTime(year, month, day, _Hour, _Minute, _Second, _Millisecond);
		}
		public PersianDateTime AddSeconds(long value)
		{
			return AddTicks(TimeSpan.TicksPerSecond * value);
		}
		public PersianDateTime AddWeeks(long value)
		{
			return AddDays(7L * value);
		}
		public PersianDateTime AddYears(int value)
		{
			return AddMonths(value * 12);
		}
		public TimeSpan Subtract(PersianDateTime value)
		{
			return new TimeSpan(_Ticks - value._Ticks);
		}
		public PersianDateTime Subtract(TimeSpan value)
		{
			return AddTicks(-value.Ticks);
		}
		public DateTime ToDateTime()
		{
			return new DateTime(TICKS_BASE_UTC_OFFSET + _Ticks, DateTimeKind.Unspecified);
		}
		//private static bool IsDaylightSavingTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
		//{
		//	return false;
		//}
		//public DateTime ToLocalTime()
		//{
		//	return this.ToUniversalTime().ToLocalTime();
		//}
		//public DateTime ToUniversalTime()
		//{
		//	var ticks = TICKS_BASE_UTC_OFFSET + _Ticks - BaseUtcTimeOffset.Ticks;
		//	if (IsDaylightSavingTime(_Year, _Month, _Day, _Hour, _Minute, _Second, _Millisecond)) ticks -= TimeSpan.TicksPerHour;
		//	return new DateTime(ticks, DateTimeKind.Utc);
		//}
		//public double ToOADate()
		//{
		//	return this.ToLocalTime().ToOADate();
		//}
		//public long ToBinary()
		//{
		//	return this.ToLocalTime().ToBinary();
		//}
		//public long ToFileTime()
		//{
		//	return this.ToLocalTime().ToFileTime();
		//}
		//public long ToFileTimeUtc()
		//{
		//	return this.ToUniversalTime().ToFileTimeUtc();
		//}
		//public static PersianDateTime FromBinary(long dateData)
		//{
		//	return new PersianDateTime(DateTime.FromBinary(dateData));
		//}
		//public static PersianDateTime FromFileTime(long fileTime)
		//{
		//	return new PersianDateTime(DateTime.FromFileTime(fileTime));
		//}
		//public static PersianDateTime FromFileTimeUtc(long fileTime)
		//{
		//	return new PersianDateTime(DateTime.FromFileTimeUtc(fileTime));
		//}
		//public static PersianDateTime FromOADate(double d)
		//{
		//	return new PersianDateTime(DateTime.FromOADate(d));
		//}
		private static int GetWeekOfYear(int year, int month, int day, bool fullWeeks)
		{
			var firstDayOfYearTicks = GetTicks(year, month, 1, 0, 0, 0, 0);
			var firstDayOfYearWeekDay = GetWeekDay(firstDayOfYearTicks);
			day = GetDayOfYear(month, day);

			if (firstDayOfYearWeekDay > 0) day -= (6 - firstDayOfYearWeekDay + 1);
			int rem = 0;
			int div = Math.DivRem(day, 7, out rem);

			if (fullWeeks == false)
			{
				if (firstDayOfYearWeekDay > 0) div++;
				if (rem > 0) div++;
			}
			return div;
		}
		public int GetWeekOfYear(bool fullWeeksOnly = false)
		{
			return GetWeekOfYear(_Year, _Month, _Day, fullWeeksOnly);
		}
		public int CompareTo(object obj)
		{
			if (obj == null) return 1;
			if (obj is PersianDateTime == false) throw new InvalidOperationException();
			return CompareTo((PersianDateTime)obj);
		}
		public int CompareTo(PersianDateTime other)
		{
			return _Ticks.CompareTo(other._Ticks);
		}
		public bool Equals(PersianDateTime other)
		{
			return (_Ticks == other._Ticks);
		}
		public override bool Equals(object obj)
		{
			return (obj is PersianDateTime && this.Equals((PersianDateTime)obj));
		}
		public static bool Equals(PersianDateTime time1, PersianDateTime time2)
		{
			return time1.Equals(time2);
		}
		public string ToLongDateString()
		{
			return ToString("D");
		}
		public string ToLongTimeString()
		{
			return ToString("T");
		}
		public string ToShortDateString()
		{
			return ToString("d");
		}
		public string ToShortTimeString()
		{
			return ToString("t");
		}
		public override string ToString()
		{
			return ToString((string)null);
		}
		/// <summary>
		/// ToString("yyyy'/'MM'/'dd HH':'mm':'ss")
		/// </summary>
		public string ToDateTimeString()
		{
			return ToString("yyyy'/'MM'/'dd HH':'mm':'ss");
		}
		/// <summary>
		/// ToString("yyyy'/'MM'/'dd")
		/// </summary>
		public string ToDateString()
		{
			return ToString("yyyy'/'MM'/'dd");
		}
		/// <summary>
		/// ToString("HH':'mm':'ss")
		/// </summary>
		public string ToTimeString()
		{
			return this.ToString("HH':'mm':'ss");
		}
		public string ToString(string format)
		{
			return ToString(format, (IFormatProvider)null);
		}
		public string ToString(string format, IFormatProvider formatProvider)
		{
			return FormatDateTime(this, format, formatProvider);
		}
		public string ToString(IFormatProvider formatProvider)
		{
			return ToString((string)null, formatProvider);
		}
		private static int FindDifferent(string s, int index)
		{
			if (index < 0 || string.IsNullOrEmpty(s) || index >= (s.Length - 1)) return -1;
			var ch = s[index];
			while (++index < s.Length)
			{
				if (s[index] != ch) return index;
			}
			return -1;
		}
		private static string GetCustomizedFormat(ref PersianDateTime time, string format, CultureInfo culture)
		{
			if (string.IsNullOrEmpty(format)) return "";

			var result = new StringBuilder();

			#region fnFormat
			Action<string, PersianDateTime> fnFormat = (s, t) =>
			{
				#region d
				if ("d".EqualsOrdinal(s))
					result.Append(t._Day.ToString(culture));
				else if ("dd".EqualsOrdinal(s))
					result.Append(t._Day.ToString("00", culture));
				else if ("ddd".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.AbbreviatedDayNames[(int)t._DayOfWeek]);
				else if ("dddd".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.DayNames[(int)t._DayOfWeek]);
				#endregion
				#region f
				else if ("f".EqualsOrdinal(s))
					result.Append((t._Millisecond / 100).ToString(culture));
				else if ("ff".EqualsOrdinal(s))
					result.Append((t._Millisecond / 10).ToString(culture));
				else if ("fff".EqualsOrdinal(s))
					result.Append(t._Millisecond.ToString(culture));
				//else if (Utility.Utils.StringEquals(s, "ffff"))
				//	result.Append(t._Millisecond.ToString(culture));
				//else if (Utility.Utils.StringEquals(s, "fffff"))
				//	result.Append(t._Millisecond.ToString(culture));
				//else if (Utility.Utils.StringEquals(s, "ffffff"))
				//	result.Append(t._Millisecond.ToString(culture));
				//else if (Utility.Utils.StringEquals(s, "fffffff"))
				//	result.Append(t._Millisecond.ToString(culture)); 
				#endregion
				#region F
				else if ("F".EqualsOrdinal(s))
				{
					int temp = (t._Millisecond / 100);
					if (temp != 0) result.Append(temp.ToString(culture));
				}
				else if ("FF".EqualsOrdinal(s))
				{
					int temp = (t._Millisecond / 10);
					if (temp != 0) result.Append(temp.ToString(culture));
				}
				else if ("FFF".EqualsOrdinal(s))
				{
					int temp = (t._Millisecond / 100);
					if (temp != 0) result.Append(temp.ToString(culture));
				}
				//else if (Utility.Utils.StringEquals(s, "FFFF"))
				//{
				//	if (t._Millisecond != 0) result.Append(t._Millisecond.ToString(culture));
				//}
				//else if (Utility.Utils.StringEquals(s, "FFFFF"))
				//{
				//	if (t._Millisecond != 0) result.Append(t._Millisecond.ToString(culture));
				//}
				//else if (Utility.Utils.StringEquals(s, "FFFFFF"))
				//{
				//	if (t._Millisecond != 0) result.Append(t._Millisecond.ToString(culture));
				//}
				//else if (Utility.Utils.StringEquals(s, "FFFFFFF"))
				//{
				//	if (t._Millisecond != 0) result.Append(t._Millisecond.ToString(culture));
				//} 
				#endregion
				#region g
				else if ("g".EqualsOrdinal(s))
				{
					//result.Append("");
				}
				else if ("gg".EqualsOrdinal(s))
				{
					//result.Append("");
				}
				#endregion
				#region h
				else if ("h".EqualsOrdinal(s))
					result.Append(t._Hour12.ToString(culture));
				else if ("hh".EqualsOrdinal(s))
					result.Append(t._Hour12.ToString("00", culture));
				#endregion
				#region H
				else if ("H".EqualsOrdinal(s))
					result.Append(t._Hour.ToString(culture));
				else if ("HH".EqualsOrdinal(s))
					result.Append(t._Hour.ToString("00", culture));
				#endregion
				#region K
				else if ("K".EqualsOrdinal(s))
					result.Append(string.Concat(BaseUtcTimeOffset.Hours.ToString("+00;-00;00", culture), culture.DateTimeFormat.TimeSeparator, BaseUtcTimeOffset.Minutes.ToString("00", culture)));
				#endregion
				#region m
				else if ("m".EqualsOrdinal(s))
					result.Append(t._Minute.ToString(culture));
				else if ("mm".EqualsOrdinal(s))
					result.Append(t._Minute.ToString("00", culture));
				#endregion
				#region M
				else if ("M".EqualsOrdinal(s))
					result.Append(t._Month.ToString(culture));
				else if ("MM".EqualsOrdinal(s))
					result.Append(t._Month.ToString("00", culture));
				else if ("MMM".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.AbbreviatedMonthNames[t._Month - 1]);
				else if ("MMMM".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.MonthNames[t._Month - 1]);
				#endregion
				#region s
				else if ("s".EqualsOrdinal(s))
					result.Append(t._Second.ToString(culture));
				else if ("ss".EqualsOrdinal(s))
					result.Append(t._Second.ToString("00", culture));
				#endregion
				#region t
				else if ("t".EqualsOrdinal(s))
					result.Append((t._IsAfternoon ? culture.DateTimeFormat.PMDesignator.Remove(1) : culture.DateTimeFormat.AMDesignator.Remove(1)));
				else if ("tt".EqualsOrdinal(s))
					result.Append((t._IsAfternoon ? culture.DateTimeFormat.PMDesignator : culture.DateTimeFormat.AMDesignator));
				#endregion
				#region y
				else if ("y".EqualsOrdinal(s))
					result.Append((t._Year % 100).ToString(culture));
				else if ("yy".EqualsOrdinal(s))
					result.Append((t._Year % 100).ToString("00", culture));
				else if ("yyy".EqualsOrdinal(s))
					result.Append((t._Year < 1000 ? t._Year.ToString("000", culture) : t._Year.ToString(culture)));
				else if ("yyyy".EqualsOrdinal(s))
					result.Append(t._Year.ToString("0000", culture));
				else if ("yyyyy".EqualsOrdinal(s))
					result.Append(t._Year.ToString("00000", culture));
				#endregion
				#region z
				else if ("z".EqualsOrdinal(s))
					result.Append(BaseUtcTimeOffset.Hours.ToString(culture));
				else if ("zz".EqualsOrdinal(s))
					result.Append(BaseUtcTimeOffset.Hours.ToString("00", culture));
				else if ("zzz".EqualsOrdinal(s))
					result.Append(string.Concat(BaseUtcTimeOffset.Hours.ToString("00", culture), culture.DateTimeFormat.TimeSeparator, BaseUtcTimeOffset.Minutes.ToString("00", culture)));
				#endregion
				#region Separators
				else if (":".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.TimeSeparator);
				else if ("/".EqualsOrdinal(s))
					result.Append(culture.DateTimeFormat.DateSeparator);
				#endregion
				else
					result.Append(s);
			}; 
			#endregion

			int i = 0, di = -1;
			while (i < format.Length)
			{
				di = FindDifferent(format, i);
				if (di == -1)
				{
					fnFormat(format.Substring(i), time);
					break;
				}
				else
				{
					fnFormat(format.Substring(i, di-i), time);
					i = di;
				}
			}

			return result.ToString();
		}
		private static string FormatDateTime(PersianDateTime time, string format, IFormatProvider formatProvider)
		{
			var culture = GetCulture(formatProvider);

			if (string.IsNullOrEmpty(format)) format = "G";

			#region StandardFormats
			if (format != null && format.Length == 1)
			{
				switch (format[0])
				{
					case 'd':
					case 'D':
					case 'f':
					case 'F':
					case 'g':
					case 'G':
					case 'm':
					case 'M':
					case 'o':
					case 'O':
					case 'r':
					case 'R':
					case 's':
					case 't':
					case 'T':
					case 'u':
					case 'U':
					case 'y':
					case 'Y':
						format = culture.DateTimeFormat.GetAllDateTimePatterns(format[0]).FirstOrDefault();
						break;
				}
			}
			#endregion
			#region CustomFormats

			if (format == null) format = "";

			var result = new StringBuilder();
			var fmtBuffer = new StringBuilder();
			//The DFA-Machine with five states ;-)
			//	0: Processing a character, 
			//	1: Literal processing(single qute), 
			//	2: Literal processing(double qute),
			//	3: Processing escape character
			//	4: Processing character as custom datetime format
			byte state = 0;
			int iCh = 0;
			char ch;
			#region fnProcessBuffer
			Action fnProcessBuffer = () =>
			{
				if (fmtBuffer.Length > 0)
				{
					result.Append(GetCustomizedFormat(ref time, fmtBuffer.ToString(), culture));
					fmtBuffer.Clear();
				}
			};
			#endregion
			while (iCh < format.Length)
			{
				ch = format[iCh];
				switch (state)
				{
					case 0:
						#region State 0
						if (ch == '\'')
						{
							fnProcessBuffer();
							state = 1;
						}
						else if (ch == '"')
						{
							fnProcessBuffer();
							state = 2;
						}
						else if (ch == '\\')
						{
							fnProcessBuffer();
							state = 3;
						}
						else if (ch == '%')
						{
							fnProcessBuffer();
							state = 4;
						}
						else
						{
							fmtBuffer.Append(ch);
						}
						#endregion
						break;
					case 1:
						#region State 1
						if (ch == '\'')
						{
							state = 0;
						}
						else
						{
							result.Append(ch);
						}
						#endregion
						break;
					case 2:
						#region State 2
						if (ch == '"')
						{
							state = 0;
						}
						else
						{
							result.Append(ch);
						}
						#endregion
						break;
					case 3:
						#region State 3
						result.Append(ch);
						state = 0;
						#endregion
						break;
					case 4:
						#region State 4
						result.Append(GetCustomizedFormat(ref time, ch.ToString(), culture));
						state = 0;
						#endregion
						break;
				}
				iCh++;
			}
			if (state != 0)
				throw new FormatException();
			else
				fnProcessBuffer();
			#endregion
			return result.ToString();
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		public static string GetMonthName(int month)
		{
			return MonthNames[(month - 1)];
		}
		public static string GetDayName(DayOfWeek weekDay)
		{
			return DayNames[(((int)weekDay + 8) % 7)];
		}
		public static int Compare(PersianDateTime time1, PersianDateTime time2)
		{
			return time1.CompareTo(time2);
		}
		public static PersianDateTime Parse(string s)
		{
			return Parse(s, (IFormatProvider)null);
		}
		public static bool TryParse(string s, out PersianDateTime time)
		{
			return TryParse(s, (IFormatProvider)null, out time);
		}
		public static PersianDateTime Parse(string s, IFormatProvider formatProvider)
		{
			PersianDateTime dt;
			if (TryParse(s, formatProvider, out dt) == false) throw new ArgumentException("Invalid date value.", nameof(s));
			return dt;
		}
		public static bool TryParse(string s, IFormatProvider formatProvider, out PersianDateTime time)
		{
			return IsValid(s, formatProvider, out time);
		}
		public static int DaysInMonth(int year, int month)
		{
			if (month <= 6) return 31;
			if (month <= 11) return 30;
			return (IsLeapYear(year) ? 30 : 29);
		}
		public static bool IsLeapYear(int year)
		{
			switch ((year % 33))
			{
				case 1:
				case 5:
				case 9:
				case 13:
				case 17:
				case 22:
				case 26:
				case 30:
					return true;
			}
			return false;
		}
		public static bool IsLeapMonth(int year, int month)
		{
			return (month == 12 && IsLeapYear(year));
		}
		public static bool IsLeapDay(int year, int month, int day)
		{
			return (day == 30 && IsLeapMonth(year, month));
		}
		public static int DaysInYear(int year)
		{
			return (IsLeapYear(year) ? 366 : 365);
		}
		private static long GetTicks(int year, int month, int day, int hour, int minute, int second, int millisecond)
		{
			long ticks = 0L;
			int temp = year - 1;
			while (temp > 0)
			{
				ticks += TICKS_YEAR;
				if (IsLeapYear(temp)) ticks += TimeSpan.TicksPerDay;
				temp--;
			}
			while (--month > 0)
			{
				ticks += (TimeSpan.TicksPerDay * (long)DaysInMonth(year, month));
			}
			if (day > 1) ticks += (((long)day - 1L) * TimeSpan.TicksPerDay);
			if (hour > 0) ticks += ((long)hour * TimeSpan.TicksPerHour);
			if (minute > 0) ticks += ((long)minute * TimeSpan.TicksPerMinute);
			if (second > 0) ticks += ((long)second * TimeSpan.TicksPerSecond);
			if (millisecond > 0) ticks += ((long)millisecond * TimeSpan.TicksPerMillisecond);
			return ticks;
		}
		private static void GetDateParts(long ticks, out int year, out int month, out int day, out int hour, out int minute, out int second, out int millisecond)
		{
			year = 1;
			month = 1;
			day = 1;
			hour = 0;
			minute = 0;
			second = 0;
			millisecond = 0;

			if (ticks == 0L) return;
			long temp;

			//Year
			temp = (IsLeapYear(year) ? TICKS_YEAR + TimeSpan.TicksPerDay : TICKS_YEAR);
			while (ticks >= temp)
			{
				ticks -= temp;
				year++;
				temp = (IsLeapYear(year) ? TICKS_YEAR + TimeSpan.TicksPerDay : TICKS_YEAR);
			}
			if (ticks == 0L) return;

			//Month
			temp = (long)DaysInMonth(year, month) * TimeSpan.TicksPerDay;
			while (ticks >= temp)
			{
				ticks -= temp;
				month++;
				temp = (long)DaysInMonth(year, month) * TimeSpan.TicksPerDay;
			}
			if (ticks == 0L) return;

			//Day
			long rem;
			day += (int)Math.DivRem(ticks, TimeSpan.TicksPerDay, out rem);
			if (rem == 0) return;

			//Hour
			temp = Math.DivRem(rem, TimeSpan.TicksPerHour, out rem);
			hour += (int)temp;
			if (rem == 0) return;

			//Minute
			minute += (int)Math.DivRem(rem, TimeSpan.TicksPerMinute, out rem);
			if (rem == 0) return;

			//Second
			second += (int)Math.DivRem(rem, TimeSpan.TicksPerSecond, out rem);
			if (rem == 0) return;

			//Millisecond
			millisecond += (int)(rem / TimeSpan.TicksPerMillisecond);
		}
		private static CultureInfo GetCulture(IFormatProvider formatProvider)
		{
			//var culture = (formatProvider ?? PersianDateTime.DefaultCultureInfo) as CultureInfo;
			var culture = PersianDateTime.DefaultCultureInfo;
			if (culture == null) culture = CultureInfo.CurrentCulture;
			return culture;
		}
		private static bool IsValid(string s, IFormatProvider formatProvider, out PersianDateTime time)
		{
			bool isValid = false;
			time = default(PersianDateTime);

			var culture = GetCulture(formatProvider);

			try
			{
				string
					dateSepPatt = @"(/|-|\.|,|\s|" + Regex.Escape(culture.DateTimeFormat.DateSeparator) + ")", // Standard date separator characters.
					timeSepPatt = @"(:|\.|,|\s|" + Regex.Escape(culture.DateTimeFormat.TimeSeparator) + ")", // Standard time separator characters.
					dateTimeSepPatt = @"(\s+|T)"; // Standard date and time separators.
				string datePatt =
					 @"( (?<year>\d+) {DSEP} (?<month>\d\d?) {DSEP} (?<day>\d\d?) )"
					.Replace("{DSEP}", dateSepPatt);
				string timePatt =
					@"( (?<hour>\d\d?) ({TSEP} (?<min>\d\d?) ({TSEP} (?<sec>\d\d?) ({TSEP} (?<mill>\d{1,3}))? )? )? (\s+ (?<des>\S+))? )"
					.Replace("{TSEP}", timeSepPatt);

				string dateTimePatt =
					((@"^ \s* ( ({DATEPATT} ( {DTSEP} {TIMEPATT})? ) | ({TIMEPATT}) ) \s* $"
					.Replace("{DATEPATT}", datePatt))
					.Replace("{TIMEPATT}", timePatt))
					.Replace("{DTSEP}", dateTimeSepPatt);

				var match = Regex.Match(s, dateTimePatt, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.IgnoreCase);
				if (match.Success)
				{
					int year = 0, month = 0, day = 0;
					int hour = 0, minute = 0, second = 0, millisecond = 0;

					var g = match.Groups["year"];
					if (g.Success == false) // 's' contains only time;
					{
						var today = PersianDateTime.Today;
						year = today._Year;
						month = today._Month;
						day = today._Day;
					}
					else
					{
						year = int.Parse(g.Value, culture);
						month = int.Parse(match.Groups["month"].Value, culture);
						day = int.Parse(match.Groups["day"].Value, culture);
					}
					g = match.Groups["hour"];
					if (g.Success) hour = int.Parse(g.Value, culture);
					g = match.Groups["des"];
					if (g.Success)
					{
						if (g.Value.StartsWith(culture.DateTimeFormat.PMDesignator.Substring(0, 1), StringComparison.OrdinalIgnoreCase))
						{
							if (hour > 0 && hour < 12) hour += 12;
						}
					}
					g = match.Groups["min"];
					if (g.Success) minute = int.Parse(g.Value, culture);
					g = match.Groups["sec"];
					if (g.Success) second = int.Parse(g.Value, culture);
					g = match.Groups["mill"];
					if (g.Success) millisecond = int.Parse(g.Value, culture);

					isValid = IsValid(year, month, day, hour, minute, second, millisecond);
					if (isValid) time = new PersianDateTime(year, month, day, hour, minute, second, millisecond);
				}
			}
			catch
			{
				isValid = false;
			}

			return isValid;
		}
		public static bool IsValid(string s)
		{
			PersianDateTime time;
			return IsValid(s, (IFormatProvider)null, out time);
		}
		public static bool IsValid(int year, int month, int day)
		{
			return IsValid(year, month, day, 0, 0, 0, 0);
		}
		public static bool IsValid(int year, int month, int day, int hour, int minute, int second, int millisecond)
		{
			long ticks;
			return IsValid(year, month, day, hour, minute, second, millisecond, out ticks);
		}
		public static bool IsValid(int year, int month, int day, int hour, int minute, int second)
		{
			return IsValid(year, month, day, hour, minute, second, 0);
		}
		private static bool IsValid(int year, int month, int day, int hour, int minute, int second, int millisecond, out long ticks)
		{
			ticks = 0L;
			if (year < 1 || year > MAX_YEAR || month < 1 || month > 12 || day < 1 || day > 31) return false;
			if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59 || millisecond < 0 || millisecond > 999) return false;
			if (day > DaysInMonth(year, month)) return false;
			ticks = GetTicks(year, month, day, hour, minute, second, millisecond);
			if (IsValidTicks(ticks) == false)
			{
				ticks = 0L;
				return false;
			}
			return true;
		}
		private static bool IsValidTicks(long ticks)
		{
			return (ticks >= 0L && ticks <= TICKS_MAX);
		}
		public static bool IsValid(DateTime time)
		{
			PersianDateTime temp;
			return IsValid(time, out temp);
		}
		private static bool IsValid(DateTime time, out PersianDateTime dateTime)
		{
			dateTime = default(PersianDateTime);
			var ticks = time.ToUniversalTime().Ticks - TICKS_BASE_UTC_OFFSET;
			var utcOffset = (time.Kind != DateTimeKind.Utc ? TimeZoneInfo.Local.GetUtcOffset(time).Ticks : 0L);
			ticks += utcOffset;
			if (IsValidTicks(ticks))
			{
				dateTime = new PersianDateTime(ticks);
				return true;
			}
			return false;
		}
		#region IConvertible
		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Object;
		}
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			return this.ToDateTime();
		}
		string IConvertible.ToString(IFormatProvider provider)
		{
			return this.ToString(null, provider);
		}
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		char IConvertible.ToChar(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			throw new InvalidCastException();
		}
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			return Utility.Utilities.ConvertibleToType(this, conversionType, provider);
		}
		#endregion
		#region IEqualityComparer
		int IEqualityComparer<PersianDateTime>.GetHashCode(PersianDateTime obj)
		{
			return obj.GetHashCode();
		}
		bool IEqualityComparer<PersianDateTime>.Equals(PersianDateTime x, PersianDateTime y)
		{
			return Equals(x, y);
		}
		#endregion
		#endregion
		#region Operators
		public static TimeSpan operator -(PersianDateTime pd1, PersianDateTime pd2)
		{
			return pd1.Subtract(pd2);
		}
		public static PersianDateTime operator -(PersianDateTime pd, TimeSpan ts)
		{
			return pd.Subtract(ts);
		}
		public static PersianDateTime operator +(PersianDateTime pd, TimeSpan ts)
		{
			return pd.Add(ts);
		}
		public static bool operator <(PersianDateTime pd1, PersianDateTime pd2)
		{
			return (pd1.CompareTo(pd2) < 0);
		}
		public static bool operator <=(PersianDateTime pd1, PersianDateTime pd2)
		{
			return (pd1.CompareTo(pd2) <= 0);
		}
		public static bool operator >(PersianDateTime pd1, PersianDateTime pd2)
		{
			return (pd1.CompareTo(pd2) > 0);
		}
		public static bool operator >=(PersianDateTime pd1, PersianDateTime pd2)
		{
			return (pd1.CompareTo(pd2) >= 0);
		}
		public static bool operator ==(PersianDateTime pd1, PersianDateTime pd2)
		{
			return pd1.Equals(pd2);
		}
		public static bool operator !=(PersianDateTime pd1, PersianDateTime pd2)
		{
			return (!pd1.Equals(pd2));
		}
		#endregion
	}
	public class PersianDateTimeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(PersianDateTime) || sourceType == typeof(DateTime)) return true;
			if (sourceType == typeof(string)) return true;
			return false;
		}
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(PersianDateTime) || destinationType == typeof(DateTime)) return true;
			if (destinationType == typeof(string)) return true;
			return false;
		}
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is PersianDateTime)
				return value;
			else if (value is DateTime)
				return new PersianDateTime((DateTime)value);
			else if (value is string)
				return PersianDateTime.Parse((string)value);
			return base.ConvertFrom(context, culture, value);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value == null) return base.ConvertTo(context, culture, value, destinationType);
			var ic = value as IConvertible;
			return ic.ToType(destinationType, culture);
		}
		public override bool IsValid(ITypeDescriptorContext context, object value)
		{
			if (value is PersianDateTime)
				return true;
			else if (value is DateTime)
				return PersianDateTime.IsValid((DateTime)value);
			else if (value is string)
				return PersianDateTime.IsValid((string)value);
			return false;
		}
	}
}
