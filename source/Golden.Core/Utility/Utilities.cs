using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using Golden.Annotations;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace Golden.Utility
{
    public static class Utilities
    {
        public static void SetSystemTime(DateTime newTime)
        {
            var sysTime = new Utility.PInvokeHelper.SYSTEMTIME();
            sysTime.wYear = (ushort)newTime.Year;
            sysTime.wMonth = (ushort)newTime.Month;
            sysTime.wDay = (ushort)newTime.Day;
            sysTime.wHour = (ushort)newTime.Hour;
            sysTime.wMinute = (ushort)newTime.Minute;
            sysTime.wSecond = (ushort)newTime.Second;
            sysTime.wMilliseconds = (ushort)newTime.Millisecond;
            Utility.PInvokeHelper.SetSystemTime(ref sysTime);
        }
        private static readonly Lazy<MethodInfo> mHasFlag = new Lazy<MethodInfo>(()=>
        {
            return typeof(Utilities).GetMethod(nameof(HasFlag), BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        });
        public static bool HasFlag<T>(T value, T flag) where T : struct
        {
            var type = typeof(T);
            #region Enum
            if (type.IsEnum)
            {
                var enumType = System.Enum.GetUnderlyingType(type);
                object pValue = System.Convert.ChangeType(value, enumType);
                object pFlag = System.Convert.ChangeType(flag, enumType);
                return (bool)mHasFlag.Value.MakeGenericMethod(enumType).Invoke(null, new object[] { pValue, pFlag });
            }
            #endregion
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Byte:
                    var bFlag = (byte)((object)flag);
                    if (bFlag == 0) return true;
                    return (((byte)((object)value) & bFlag) == bFlag);
                case TypeCode.Int16:
                    var sFlag = (short)((object)flag);
                    if (sFlag == 0) return true;
                    return (((short)((object)value) & sFlag) == sFlag);
                case TypeCode.Int32:
                    var iFlag = (int)((object)flag);
                    if (iFlag == 0) return true;
                    return (((int)((object)value) & iFlag) == iFlag);
                case TypeCode.Int64:
                    var lFlag = (long)((object)flag);
                    if (lFlag == 0) return true;
                    return (((long)((object)value) & lFlag) == lFlag);
                case TypeCode.SByte:
                    var sbFlag = (sbyte)((object)flag);
                    if (sbFlag == 0) return true;
                    return (((sbyte)((object)value) & sbFlag) == sbFlag);
                case TypeCode.UInt16:
                    var usFlag = (ushort)((object)flag);
                    if (usFlag == 0) return true;
                    return (((ushort)((object)value) & usFlag) == usFlag);
                case TypeCode.UInt32:
                    var uiFlag = (uint)((object)flag);
                    if (uiFlag == 0) return true;
                    return (((uint)((object)value) & uiFlag) == uiFlag);
                case TypeCode.UInt64:
                    var ulFlag = (ulong)((object)flag);
                    if (ulFlag == 0) return true;
                    return (((ulong)((object)value) & ulFlag) == ulFlag);
            }
            throw new NotSupportedException();
        }
        #region NumberName
        private static readonly Lazy<string[]> _PN20 = new Lazy<string[]>(() => new[] { "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه", "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" });
        private static readonly Lazy<string[]> _PN10 = new Lazy<string[]>(() => new[] { "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" });
        private static readonly Lazy<string[]> _PN100 = new Lazy<string[]>(() => new[] { "صد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" });
        public static string GetPersianNumberName(decimal number)
        {
            if (number < 0M) throw new NotSupportedException();
            if ((number % 1M) != 0M) throw new NotSupportedException();

            if (number < 20M) return _PN20.Value[(int)number];
            const string _NSep = " و ";
            var result = new StringBuilder();
            int d, r;
            #region ده
            if (number < 100M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 10, out r);
                result.Append(_PN10.Value[d - 2]);
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(r)));
            }
            #endregion
            #region صد
            else if (number < 1000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 100, out r);
                result.Append(_PN100.Value[d - 1]);
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(r)));
            }
            #endregion
            #region هزار
            else if (number < 1000000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 1000, out r);
                if (d > 1) result.AppendFormat("{0} ", GetPersianNumberName(new decimal(d)));
                result.Append("هزار");
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(r)));
            }
            #endregion
            #region میلیون
            else if (number < 1000000000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 1000000, out r);
                result.AppendFormat("{0} {1}", GetPersianNumberName(new decimal(d)), "میلیون");
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(r)));
            }
            #endregion
            #region میلیارد
            else if (number < 1000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000L, out lr);
                result.AppendFormat("{0} {1}", GetPersianNumberName(new decimal(ld)), "میلیارد");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(lr)));
            }
            #endregion
            #region بیلیون
            else if (number < 1000000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000000L, out lr);
                result.AppendFormat("{0} {1}", GetPersianNumberName(new decimal(ld)), "بیلیون");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(lr)));
            }
            #endregion
            #region تریلیون
            else if (number < 1000000000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000000000L, out lr);
                result.AppendFormat("{0} {1}", GetPersianNumberName(new decimal(ld)), "تریلیون");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(new decimal(lr)));
            }
            #endregion
            #region کوآدریلیون
            else if (number < 1000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000M);
                result.AppendFormat("{0} {1}", GetPersianNumberName(dd), "کوآدریلیون");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(dr));
            }
            #endregion
            #region کوینتیلیون
            else if (number < 1000000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000000M);
                result.AppendFormat("{0} {1}", GetPersianNumberName(dd), "کوینتیلیون");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(dr));
            }
            #endregion
            #region سکستیلیون
            else if (number < 1000000000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000000000M);
                result.AppendFormat("{0} {1}", GetPersianNumberName(dd), "سکستیلیون");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetPersianNumberName(dr));
            }
            #endregion
            return result.ToString();
        }
        private static readonly Lazy<string[]> _EN20 = new Lazy<string[]>(() => new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" });
        private static readonly Lazy<string[]> _EN10 = new Lazy<string[]>(() => new[] { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninty" });
        public static string GetEnglishNumberName(decimal number)
        {
            if (number < 0M) throw new NotSupportedException();
            if ((number % 1M) != 0M) throw new NotSupportedException();

            if (number < 20M) return _EN20.Value[(int)number];
            const string _NSep = " ";
            var result = new StringBuilder();
            int d, r;
            #region Ten
            if (number < 100M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 10, out r);
                result.Append(_EN10.Value[d - 2]);
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(r)));
            }
            #endregion
            #region Hundred
            else if (number < 1000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 100, out r);
                result.AppendFormat("{0}{1}Hundred", _EN20.Value[d], _NSep);
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(r)));
            }
            #endregion
            #region Thousand
            else if (number < 1000000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 1000, out r);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(new decimal(d)), _NSep, "Thousand");
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(r)));
            }
            #endregion
            #region Million
            else if (number < 1000000000M)
            {
                d = Math.DivRem(decimal.ToInt32(number), 1000000, out r);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(new decimal(d)), _NSep, "Million");
                if (r != 0) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(r)));
            }
            #endregion
            #region Milliard
            else if (number < 1000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000L, out lr);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(new decimal(ld)), _NSep, "Milliard");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(lr)));
            }
            #endregion
            #region Billion
            else if (number < 1000000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000000L, out lr);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(new decimal(ld)), _NSep, "Billion");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(lr)));
            }
            #endregion
            #region Trillion
            else if (number < 1000000000000000000M)
            {
                long lr;
                long ld = Math.DivRem(decimal.ToInt64(number), 1000000000000000L, out lr);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(new decimal(ld)), _NSep, "Trillion");
                if (lr != 0L) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(new decimal(lr)));
            }
            #endregion
            #region Quadrillion
            else if (number < 1000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000M);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(dd), _NSep, "Quadrillion");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(dr));
            }
            #endregion
            #region Quintillion
            else if (number < 1000000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000000M);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(dd), _NSep, "Quintillion");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(dr));
            }
            #endregion
            #region Sextillion
            else if (number < 1000000000000000000000000000M)
            {
                decimal dd = decimal.Divide(number, 1000000000000000000000000M);
                decimal dr = decimal.Remainder(number, 1000000000000000000000000M);
                result.AppendFormat("{0}{1}{2}", GetEnglishNumberName(dd), _NSep, "Sextillion");
                if (dr != 0M) result.AppendFormat("{0}{1}", _NSep, GetEnglishNumberName(dr));
            }
            #endregion
            return result.ToString();
        }
        #endregion
        public static string ConvertToPersian(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var result = new StringBuilder();
            int cc;
            foreach (char ch in value)
            {
                cc = (short)ch;
                if (cc >= 48 && cc <= 57) cc += 1728;
                result.Append((char)cc);
            }
            return result.ToString();
        }
        public static string ConvertToEnglish(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var result = new StringBuilder();
            int cc;
            foreach (char ch in value)
            {
                cc = (short)ch;
                if (cc >= 1776 && cc <= 1785) cc -= 1728;
                else if (cc >= 1632 && cc <= 1641) cc -= 1584;
                result.Append((char)cc);
            }
            return result.ToString();
        }
        #region Disposing
        public static void Dispose(object obj)
        {
            if (obj != null && typeof(IDisposable).IsAssignableFrom(obj.GetType())) { ((IDisposable)obj).Dispose(); }
        }
        public static void Dispose(object obj1, object obj2)
        {
            Dispose(obj1);
            Dispose(obj2);
        }
        public static void Dispose(object obj1, object obj2, object obj3)
        {
            Dispose(obj1);
            Dispose(obj2);
            Dispose(obj3);
        }
        public static void Dispose(object obj1, object obj2, object obj3, object obj4)
        {
            Dispose(obj1);
            Dispose(obj2);
            Dispose(obj3);
            Dispose(obj4);
        }
        public static void DisposeAndNull<T>(ref T obj)
        {
            if (obj != null)
            {
                Dispose(obj);
                obj = default(T);
            }
        }
        public static void DisposeAndNull<T1, T2>(ref T1 obj1, ref T2 obj2)
        {
            DisposeAndNull(ref obj1);
            DisposeAndNull(ref obj2);
        }
        public static void DisposeAndNull<T1, T2, T3>(ref T1 obj1, ref T2 obj2, ref T3 obj3)
        {
            DisposeAndNull(ref obj1);
            DisposeAndNull(ref obj2);
            DisposeAndNull(ref obj3);
        }
        public static void DisposeAndNull<T1, T2, T3, T4>(ref T1 obj1, ref T2 obj2, ref T3 obj3, ref T4 obj4)
        {
            DisposeAndNull(ref obj1);
            DisposeAndNull(ref obj2);
            DisposeAndNull(ref obj3);
            DisposeAndNull(ref obj4);
        }
        #endregion
        #region Conversion
        public static bool CanConvert(object value, Type conversionType)
        {
            return CanConvert(value, conversionType, false);
        }
        public static bool CanConvert(object value, Type conversionType, bool dbNullAsNull)
        {
            return ConvertT.CanConvert(value, conversionType, dbNullAsNull);
        }
        public static T Convert<T>(object value)
        {
            return Convert<T>(value, false);
        }
        public static T Convert<T>(object value, bool dbNullAsNull)
        {
            return ConvertT.Convert<T>(value, dbNullAsNull);
        }
        public static object Convert(Type conversionType, object value)
        {
            return Convert(conversionType, value, false);
        }
        public static object Convert(Type conversionType, object value, bool dbNullAsNull)
        {
            return ConvertT.Convert(conversionType, value, dbNullAsNull);
        }
        internal static object ConvertibleToType(IConvertible value, Type type, IFormatProvider provider)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (value.GetType() == type) return value;
            if (type == typeof(object)) return value;
            if (type.IsEnum) return (Enum)value;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return value.ToBoolean(provider);
                case TypeCode.Byte:
                    return value.ToByte(provider);
                case TypeCode.Char:
                    return value.ToChar(provider);
                case TypeCode.DateTime:
                    return value.ToDateTime(provider);
                case TypeCode.Decimal:
                    return value.ToDecimal(provider);
                case TypeCode.Double:
                    return value.ToDouble(provider);
                case TypeCode.Int16:
                    return value.ToInt16(provider);
                case TypeCode.Int32:
                    return value.ToInt32(provider);
                case TypeCode.Int64:
                    return value.ToInt64(provider);
                case TypeCode.SByte:
                    return value.ToSByte(provider);
                case TypeCode.Single:
                    return value.ToSingle(provider);
                case TypeCode.String:
                    return value.ToString(provider);
                case TypeCode.UInt16:
                    return value.ToUInt16(provider);
                case TypeCode.UInt32:
                    return value.ToUInt32(provider);
                case TypeCode.UInt64:
                    return value.ToUInt64(provider);
            }
            throw new InvalidCastException();
        }
        #endregion
        public static bool CheckInternetConnection()
        {
            int desc;
            return Utility.PInvokeHelper.InternetGetConnectedState(out desc, 0);
        }
        public static T Clone<T>(T obj)
        {
            var newObj = System.Activator.CreateInstance<T>();
            Clone<T>(obj, ref newObj);
            return newObj;
        }
        public static void Clone<T>(T obj, T newObj) where T : class
        {
            Clone<T>(obj, ref newObj);
        }
        public static void Clone<T>(T obj, ref T newObj)
        {
            var type = typeof(T);
            if ((type.IsValueType && type.IsPrimitive) || type == typeof(string))
            {
                newObj = obj;
                return;
            }
            if (type.IsClass || TypeHelper.IsStructure(type))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                object value;
                var refObj = __makeref(obj);
                var refNewObj = __makeref(newObj);
                foreach (var field in fields)
                {
                    value = field.GetValueDirect(refObj);
                    field.SetValueDirect(refNewObj, value);
                }
                return;
            }
            throw new System.InvalidOperationException(string.Format("Can't clone object type '{0}'.", type.FullName));
        }
        public static bool IsBetween(object value, object minValue, object maxValue)
        {
            return IsBetween(value, minValue, maxValue, true);
        }
        public static bool IsBetween<T>(T value, T minValue, T maxValue) where T : IComparable<T>
        {
            return IsBetween<T>(value, minValue, maxValue, true);
        }
        public static bool IsBetween(object value, object minValue, object maxValue, bool includeMaxBound)
        {
            if (value == null) return false;
            var valueType = value.GetType();
            if (typeof(IComparable<>).MakeGenericType(valueType).IsAssignableFrom(valueType))
            {
                MethodInfo genMethod = null;
                foreach (MethodInfo m in typeof(Utilities).GetMember(nameof(Utilities.IsBetween), MemberTypes.Method, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.GetGenericArguments().Length == 1)
                    {
                        genMethod = m.MakeGenericMethod(valueType);
                        break;
                    }
                }
                return (bool)genMethod.Invoke(null, new object[] { value, minValue, maxValue });
            }
            if (typeof(IComparable).IsAssignableFrom(valueType))
            {
                var valueCmp = value as IComparable;
                var cmp = valueCmp.CompareTo(minValue);
                if (includeMaxBound)
                {
                    if (cmp < 0) return false;
                }
                else
                {
                    if (cmp <= 0) return false;
                }
                cmp = valueCmp.CompareTo(maxValue);
                if (includeMaxBound)
                {
                    return (cmp <= 0);
                }
                else
                {
                    return (cmp < 0);
                }
            }
            throw new InvalidOperationException("Value must implement IComparable.");
        }
        public static bool IsBetween<T>(T value, T minValue, T maxValue, bool includeMaxBound) where T : IComparable<T>
        {
            var cmp = value.CompareTo(minValue);
            if (includeMaxBound)
            {
                if (cmp < 0) return false;
            }
            else
            {
                if (cmp <= 0) return false;
            }
            cmp = value.CompareTo(maxValue);
            if (includeMaxBound)
            {
                return (cmp <= 0);
            }
            else
            {
                return (cmp < 0);
            }
        }
        public static bool IsIn<T>(T value, params T[] items)
        {
            if (items == null || items.Length == 0) return false;
            return (Array.IndexOf<T>(items, value) != -1);
        }
        public static bool TryDeleteFile(string fileName)
        {
            if (System.IO.File.Exists(fileName) == false) return false;
            try
            {
                System.IO.File.Delete(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string GetMemberName<T>(Expression<Func<T>> member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var memberExp = member.Body as MemberExpression;
            if (memberExp == null)
            {
                var unaryExp = member.Body as UnaryExpression;
                if (unaryExp == null) throw new ArgumentOutOfRangeException(nameof(member));
                memberExp = unaryExp.Operand as MemberExpression;
                if (memberExp == null) throw new ArgumentOutOfRangeException(nameof(member));
            }
            return memberExp.Member.Name;
        }
        public static string GetMemberName<TSource, T>(Expression<Func<TSource, T>> member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var memberExp = member.Body as MemberExpression;
            if (memberExp == null)
            {
                var unaryExp = member.Body as UnaryExpression;
                if (unaryExp == null) throw new ArgumentOutOfRangeException(nameof(member));
                memberExp = unaryExp.Operand as MemberExpression;
                if (memberExp == null) throw new ArgumentOutOfRangeException(nameof(member));
            }
            return memberExp.Member.Name;
        }
        public static MemberInfo GetMember<T>(Expression<Func<T>> member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var memberExp = member.Body as MemberExpression;
            if (memberExp == null)
            {
                var unaryExp = member.Body as UnaryExpression;
                if (unaryExp == null) throw new ArgumentOutOfRangeException(nameof(member));
                memberExp = unaryExp.Operand as MemberExpression;
                if (memberExp == null) throw new ArgumentOutOfRangeException(nameof(member));
            }
            return memberExp.Member;
        }
        public static MemberInfo GetMember<TSource, T>(Expression<Func<TSource, T>> member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var memberExp = member.Body as MemberExpression;
            if (memberExp == null)
            {
                var unaryExp = member.Body as UnaryExpression;
                if (unaryExp == null) throw new ArgumentOutOfRangeException(nameof(member));
                memberExp = unaryExp.Operand as MemberExpression;
                if (memberExp == null) throw new ArgumentOutOfRangeException(nameof(member));
            }
            return memberExp.Member;
        }
        public static MethodInfo GetMethodMember(LambdaExpression method)
        {
            var memberExp = method.Body as MemberExpression;
            if (memberExp == null)
            {
                var exp = method.Body;
                if (method.Body is UnaryExpression) exp = ((UnaryExpression)method.Body).Operand;
#if NET45
                var methodCallObject = (ConstantExpression)((MethodCallExpression)exp).Object;
                return ((MethodInfo)methodCallObject.Value);
#else
                var methodInfoExpression = (ConstantExpression)((MethodCallExpression)exp).Arguments.Last();
                return ((MethodInfo)methodInfoExpression.Value);
#endif
            }
            return (MethodInfo)memberExp.Member;
        }
        public static int GetWindowThreadProcessId(int windowHandle)
        {
            int procId;
            Utility.PInvokeHelper.GetWindowThreadProcessId(windowHandle, out procId);
            return procId;
        }
        /// <summary>
        /// Gets current NIST Internet Time and convert it to local time.
        /// </summary>
        /// <returns>The NIST Internet Time</returns>
        public static DateTime GetNistInternetTime()
        {
            string htmlBody = "";
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
            request.Method = "GET";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (var stream = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        htmlBody = stream.ReadToEnd(); //<timestamp time="13454974899444" delay="13454974899444"/>
                        stream.Close();
                    }
                }
                response.Close();
            }
            string time = Regex.Match(htmlBody, @"(?<=\btime="")[^""]*").Value;
            double milliseconds = System.Convert.ToInt64(time) / 1000D;
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds).ToLocalTime();
        }
        public static T ThrowNotSupported<T>(string message = null)
        {
            throw new NotSupportedException(message);
        }
        public static T ThrowNotImplemented<T>(string message = null)
        {
            throw new NotImplementedException(message);
        }
        public static T ConvertBits<T>(params bool[] bits)
        {
            var type = typeof(T);
            if (!TypeHelper.IsInteger(type)) throw new InvalidOperationException($"Invalid type '{type.FullName}', type must be one of integer data types.");

            if (bits == null) throw new ArgumentNullException(nameof(bits));

            if (bits.Length == 0) return Convert<T>(0);

            double result = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result += Math.Pow(2d, i);
                }
            }
            return Convert<T>(result);
        }
    }
}