namespace Golden
{
	using System;
	using System.ComponentModel;
	using System.Reflection;
	using System.Linq;

	internal static class ConvertT
	{
		#region Fields
		private static readonly Lazy<MethodInfo> mEnumTryParse = new Lazy<MethodInfo>(() =>
			typeof(System.Enum)
			.GetMember(nameof(System.Enum.TryParse), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static)
			.OfType<MethodInfo>()
			.FirstOrDefault(m =>
			{
				if (m.ReturnType != typeof(bool)) return false;
				if (m.GetGenericArguments().Length != 1) return false;
				var parameters = m.GetParameters();
				return (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].IsOut);
			}));
        private static readonly Lazy<MethodInfo> mConvertT = new Lazy<MethodInfo>(() =>
            typeof(ConvertT)
            .GetMember(nameof(Convert), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static)
            .OfType<MethodInfo>()
            .FirstOrDefault(m => m.IsGenericMethod));
        #endregion

        public static bool CanConvert(object value, Type conversionType, bool dbNullAsNull)
		{
			if (conversionType == null) return false;
			if (conversionType == typeof(object)) return true;
			if (dbNullAsNull && System.Convert.IsDBNull(value)) value = null;
			if (value == null) return Utility.TypeHelper.CanBeNull(conversionType);

			var valueType = value.GetType();
			var nnTargetType = Utility.TypeHelper.GetNonNullableType(conversionType);
			var nnTargetTypeCode = Type.GetTypeCode(nnTargetType);

			if (conversionType.IsAssignableFrom(valueType)) return true;

			#region fnIsString
			Func<Type, bool> fnIsString = t => (t == typeof(char) || t == typeof(string));
			#endregion
			#region fnToString
			Func<object, string> fnToString = v => (v is bool ? ((bool)v ? "1" : "0") : v.ToString());
			#endregion

			#region Enum
			if (nnTargetType.IsEnum)
			{
				if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType))
				{
					if (mEnumTryParse.Value != null)
					{
						object en = null;
						return (bool)mEnumTryParse.Value.MakeGenericMethod(nnTargetType).Invoke(null, new object[] { value.ToString(), en });
					}
				}
			}
			#endregion
			#region Guid
			else if (nnTargetType == typeof(Guid) && fnIsString(valueType))
			{
				Guid gi;
				return Guid.TryParse(value.ToString(), out gi);
			}
			#endregion
			#region TimeSpan
			else if (nnTargetType == typeof(TimeSpan))
			{
				TimeSpan ts;
				return TimeSpan.TryParse(fnToString(value), out ts);
			}
			#endregion
			#region Version
			else if (nnTargetType == typeof(Version))
			{
				Version v;
				return Version.TryParse(value.ToString(), out v);
			}
			#endregion
			#region Uri
			else if (nnTargetType == typeof(Uri))
			{
				Uri uri;
				return Uri.TryCreate(value.ToString(), UriKind.RelativeOrAbsolute, out uri);
			}
			#endregion
			#region DateTimeOffset
			else if (nnTargetType == typeof(DateTimeOffset))
			{
				DateTimeOffset dto;
				return DateTimeOffset.TryParse(value.ToString(), out dto);
			}
			#endregion

			//Numeric, String, Char, Boolean
			switch (nnTargetTypeCode)
			{
				case TypeCode.Boolean:
					if (Utility.TypeHelper.IsNumeric(valueType)) return true;
					bool b;
					if (fnIsString(valueType)) return bool.TryParse(fnToString(value), out b);
					break;
				case TypeCode.Byte:
					byte bt;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return byte.TryParse(fnToString(value), out bt);
					break;
				case TypeCode.Char:
					char c;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType)) return char.TryParse(fnToString(value), out c);
					break;
				case TypeCode.DateTime:
					DateTime dt;
					if (fnIsString(valueType)) return DateTime.TryParse(value.ToString(), out dt);
					break;
				case TypeCode.Decimal:
					decimal dc;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return decimal.TryParse(fnToString(value), out dc);
					break;
				case TypeCode.Double:
					double db;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return double.TryParse(fnToString(value), out db);
					break;
				case TypeCode.Int16:
					short sh;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return short.TryParse(fnToString(value), out sh);
					break;
				case TypeCode.Int32:
					int i;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return int.TryParse(fnToString(value), out i);
					break;
				case TypeCode.Int64:
					long l;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return long.TryParse(fnToString(value), out l);
					break;
				case TypeCode.SByte:
					sbyte sb;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return sbyte.TryParse(fnToString(value), out sb);
					break;
				case TypeCode.Single:
					float f;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return float.TryParse(fnToString(value), out f);
					break;
				case TypeCode.String:
					if (valueType == typeof(char) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsDateTime(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return true;
					break;
				case TypeCode.UInt16:
					ushort us;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return ushort.TryParse(fnToString(value), out us);
					break;
				case TypeCode.UInt32:
					uint ui;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return uint.TryParse(fnToString(value), out ui);
					break;
				case TypeCode.UInt64:
					ulong ul;
					if (fnIsString(valueType) || Utility.TypeHelper.IsNumeric(valueType) || Utility.TypeHelper.IsBoolean(valueType)) return ulong.TryParse(fnToString(value), out ul);
					break;
				case TypeCode.DBNull:
					return System.Convert.IsDBNull(value);
			}

			var tConverter = TypeDescriptor.GetConverter(conversionType);
			if (tConverter != null && tConverter.IsValid(value)) return true;

			return false;
		}
		public static T Convert<T>(object value, bool dbNullAsNull)
		{
			Type conversionType = typeof(T);
            if (dbNullAsNull && System.Convert.IsDBNull(value)) value = null;
            if (!conversionType.IsValueType)
            {
                if (value == null) return default(T);
                return ToReferenceType<T>(value);
            }
            if (Utility.TypeHelper.IsNullableValueType(conversionType))
            {
                if (value == null) return default(T);
                var underlyingType = System.Nullable.GetUnderlyingType(conversionType);
                value = mConvertT.Value.MakeGenericMethod(underlyingType).Invoke(null, new object[] { value, false });
                return (T)Activator.CreateInstance(typeof(System.Nullable<>).MakeGenericType(underlyingType), new object[] { value });
            }
            return ConvertT.ToValueType<T>(value);
		}
		public static object Convert(Type conversionType, object value, bool dbNullAsNull)
		{
            return mConvertT.Value.MakeGenericMethod(conversionType).Invoke(null, new object[] { value, dbNullAsNull });
		}
		private static T ToReferenceType<T>(object value)
		{
            if (value == null) return default(T);
            if (typeof(T) == typeof(string))
            {
                return ((T)System.Convert.ChangeType(value, typeof(string)));
            }

			if (value is T) return ((T)value);
			return ((T)System.Convert.ChangeType(value, typeof(T)));
		}
		private static T ToValueType<T>(object value)
		{
            var type = typeof(T);
			if (value == null) throw new InvalidCastException(string.Format("Unable to cast null object to type '{0}'.", type.FullName));
			if (value is T) return ((T)value);
			if (type.IsEnum) return ((T)Enum.Parse(type, value.ToString()));
			return ((T)System.Convert.ChangeType(value, type));
		}
	}
}