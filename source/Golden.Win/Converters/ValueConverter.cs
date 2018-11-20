namespace Golden.Win.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public class ValueConverter : IValueConverter
	{
		private readonly Func<object, Type, object, CultureInfo, object> _Converter, _BackConverter;

		public ValueConverter(Func<object, object> converter, Func<object, object> backConverter = null)
		{
			_Converter = (value, targetType, param, culture) => converter(value);
			if (backConverter != null) _BackConverter = (value, targetType, param, culture) => backConverter(value);
		}
		public ValueConverter(Func<object, object, object> converter, Func<object, object, object> backConverter = null)
		{
			_Converter = (value, targetType, param, culture) => converter(value, param);
			if (backConverter != null) _BackConverter = (value, targetType, param, culture) => backConverter(value, param);
		}
		public ValueConverter(Func<object, Type, object> converter, Func<object, Type, object> backConverter = null)
		{
			_Converter = (value, targetType, param, culture) => converter(value, targetType);
			if (backConverter != null) _BackConverter = (value, targetType, param, culture) => backConverter(value, targetType);
		}
		public ValueConverter(Func<object, Type, object, object> converter, Func<object, Type, object, object> backConverter = null)
		{
			_Converter = (value, targetType, param, culture) => converter(value, targetType, param);
			if (backConverter != null) _BackConverter = (value, targetType, param, culture) => backConverter(value, targetType, param);
		}
		public ValueConverter(Func<object, Type, object, CultureInfo, object> converter, Func<object, Type, object, CultureInfo, object> backConverter = null)
		{
			_Converter = converter;
			if (backConverter != null) _BackConverter = backConverter;
		}
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return _Converter(value, targetType, parameter, culture);
		}
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (_BackConverter == null) throw new NotImplementedException();
			return _BackConverter(value, targetType, parameter, culture);
		}
	}
}
