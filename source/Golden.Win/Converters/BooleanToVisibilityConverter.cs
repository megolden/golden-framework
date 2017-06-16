using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Golden.Win.Converters
{
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public class BooleanToVisibilityConverter : IValueConverter
	{
		private static readonly Lazy<BooleanToVisibilityConverter> _Default = new Lazy<BooleanToVisibilityConverter>(()=> new BooleanToVisibilityConverter());

		public static BooleanToVisibilityConverter Default { get { return _Default.Value; } }

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool bValue = (value != null ? (bool)value : false);
			if (parameter != null && "Not".Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase)) bValue = !bValue;
			return (bValue ? Visibility.Visible : Visibility.Collapsed);
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Visibility vis = (value != null ? (Visibility)value : Visibility.Collapsed);
			if (parameter != null && "Not".Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase)) vis = (vis == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible);
			return (vis == Visibility.Visible ? true : false);
		}
	}
}
