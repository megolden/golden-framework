using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Golden.Win.Converters
{
    [ValueConversion(typeof(object), typeof(object))]
    public class UnaryConverter : IValueConverter
    {
        private static readonly Lazy<UnaryConverter> _Default = new Lazy<UnaryConverter>(() => new UnaryConverter());

        public static UnaryConverter Default
        {
            get { return _Default.Value; }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null) return value;

            var strOp = parameter.ToString();
            if ("Not".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (!object.ReferenceEquals(value, null) ? (object)(!(bool)value) : null);
            }
            else if ("IsNull".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return object.ReferenceEquals(value, null);
            }
            else if ("IsNotNull".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (!object.ReferenceEquals(value, null));
            }
            else if ("IsNullOrEmpty".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (object.ReferenceEquals(value, null) || string.Empty.Equals(value));
            }
            else if ("IsNotNullOrEmpty".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (!object.ReferenceEquals(value, null) && !string.Empty.Equals(value));
            }
            else if ("ToString".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (object.ReferenceEquals(value, null) ? null : Golden.Utility.Utilities.Convert<string>(value));
            }
			else if ("IsEqual".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
				if (value == null) return (Golden.Utility.TypeHelper.CanBeNull(targetType) ? null : (object)false);
				return object.Equals(value, Golden.Utility.Utilities.Convert(value.GetType(), parameter));
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var strOp = parameter?.ToString();
			if ("IsEqual".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
				if (value == null || !((bool)value))
					return DependencyProperty.UnsetValue;
				return Golden.Utility.Utilities.Convert(targetType, parameter);
            }

            throw new NotSupportedException();
        }
    }
}
