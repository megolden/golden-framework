namespace Golden.Win.Converters
{
	using System;
	using System.Windows;
	using System.Windows.Data;

	[ValueConversion(typeof(bool), typeof(FlowDirection))]
	public class RightToLeftToFlowDirectionConverter : IValueConverter
	{
		private static readonly Lazy<RightToLeftToFlowDirectionConverter> _Default = new Lazy<RightToLeftToFlowDirectionConverter>(()=> new RightToLeftToFlowDirectionConverter());

		public static RightToLeftToFlowDirectionConverter Default
        {
            get { return _Default.Value; }
        }

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var result = false;
			if (value is bool)
				result = (bool)value;
			else if (value is bool?)
				result = ((bool?)value).GetValueOrDefault(false);
			return (result ? FlowDirection.RightToLeft : FlowDirection.LeftToRight);
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Visibility) return ((FlowDirection)value == FlowDirection.RightToLeft);
			return false;
		}
	}
}
