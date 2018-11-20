using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Golden.Win.Converters
{
    [ValueConversion(typeof(object), typeof(object))]
    public class BinaryConverter : IMultiValueConverter
    {
        private static readonly Lazy<BinaryConverter> _Default = new Lazy<BinaryConverter>(() => new BinaryConverter());

        public static BinaryConverter Default
        {
            get { return _Default.Value; }
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null) return values;

            var strOp = parameter.ToString();
            if ("Is".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                var type = (values[1] is Type ? (Type)values[1] : values[1].GetType());
                return type.IsAssignableFrom(values[0].GetType());
            }
            else if ("IsEqual".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return object.Equals(values[0], values[1]);
            }
            else if ("IsNotEqual".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (!object.Equals(values[0], values[1]));
            }
            else if ("Or".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (values.Length > 0 && values.Any(i => true.Equals(i)));
            }
            else if ("And".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return (values.Length > 0 && !values.Any(i => false.Equals(i)));
            }
            else if ("IIf".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                if (true.Equals(values[0])) return values[1];
                return values[2];
            }
            else if ("Concat".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat(values);
            }
            else if ("IsIn".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                return Golden.Utility.Utilities.IsIn(values[0], values.Skip(1).Select(i => Golden.Utility.Utilities.Convert(values[0].GetType(), i)).ToArray());
            }
            else if ("ToCollection".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
                Type elementType = null;
                var firstElementType = false;
                if (values.Length > 0 && values[0] is Type)
                {
                    elementType = (Type)values[0];
                    firstElementType = true;
                }
                else
                {
                    elementType = Golden.Utility.TypeHelper.GetElementType(values.GetType());
                }
                if (elementType == null) elementType = typeof(object);
                var colItems =
                    typeof(Enumerable).GetMethod(nameof(Enumerable.OfType), BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(elementType)
                    .Invoke(null, new object[] { (firstElementType ? values.Skip(1) : values) });
                return Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(elementType), new object[] { colItems });
            }
			else if ("ToString".Equals(strOp, StringComparison.OrdinalIgnoreCase))
            {
				if (values.Length == 1)
					return Golden.Utility.Utilities.Convert<string>(values[0]);
				else if (values.Length == 2)
					return string.Format(values[1].ToString(), values[0]);
            }
            return values;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
