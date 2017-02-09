namespace System
{
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Text;

	public static class StringExtensions
	{
		/// <summary>
		/// Splits string by "\r", "\n" and "\r\n" delimiters.
		/// </summary>
		public static string[] SplitLines(this string str)
		{
			return SplitLines(str, StringSplitOptions.None);
		}
		/// <summary>
		/// Splits string by "\r", "\n" and "\r\n" delimiters.
		/// </summary>
		public static string[] SplitLines(this string str, StringSplitOptions options)
		{
			if (str == null) return null;
			return str.Split(new string[] { "\r", "\r\n", "\n" }, options);
		}
		public static string Right(this string str, int length)
		{
			if (string.IsNullOrEmpty(str)) return str;
			if (str.Length <= length) return str;
			return str.Substring(str.Length - length);
		}
		public static string Left(this string str, int length)
		{
			if (string.IsNullOrEmpty(str)) return str;
			if (str.Length <= length) return str;
			return str.Substring(0, length);
		}
		public static bool Contains(this string str, string value, StringComparison comparisonType)
		{
			return (str.IndexOf(value, comparisonType) >= 0);
		}
		public static string Repeat(this string str, int count)
		{
			if (string.IsNullOrEmpty(str)) return str;
			return string.Concat(Enumerable.Repeat(str, count));
		}
		public static string Replace(this string str, int startIndex, int length, string newValue)
		{
			var result = new StringBuilder(str);
			if (length > 0) result.Remove(startIndex, length);
			if (!newValue.IsNullOrEmpty()) result.Insert(startIndex, newValue);
			return result.ToString();
		}
		public static string Reverse(this string str)
		{
			if (str == null || str.Length < 2) return str;
			var chars = new char[str.Length];
			for (int i = 0; i < str.Length; i++)
			{
				chars[i] = str[str.Length - i - 1];
			}
			return new string(chars);
		}
		public static string Append(this string str, params string[] args)
		{
			if (args == null || args.Length == 0) return str;
			return string.Concat(new[] { str }.Concat(args));
		}
		public static string EmptyAsNull(this string str)
		{
			if (string.IsNullOrEmpty(str)) return null;
			return str;
		}
		public static string EmptyAsNullWhiteSpace(this string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return null;
			return str;
		}
		public static string NullAsEmpty(this string str)
		{
			return (str ?? "");
		}
		public static bool IsNull(this string str)
		{
			return (str == null);
		}
		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}
		public static bool IsNullOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}
		public static bool EqualsOrdinal(this string str, string str2, bool ignoreCase = false)
		{
			return string.Equals(str, str2, (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
		}
		public static int CompareOrdinal(this string str, string str2, bool ignoreCase = false)
		{
			return string.Compare(str, str2, (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
		}
		public static KeyValuePair<int, int> IndexOfAny(this string str, string[] values)
		{
			return str.IndexOfAny(values, 0);
		}
		public static KeyValuePair<int, int> IndexOfAny(this string str, string[] values, StringComparison comparisonType)
		{
			return str.IndexOfAny(values, 0, comparisonType);
		}
		public static KeyValuePair<int, int> IndexOfAny(this string str, string[] values, int startIndex)
		{
			return str.IndexOfAny(values, startIndex, StringComparison.CurrentCulture);
		}
		public static KeyValuePair<int, int> IndexOfAny(this string str, string[] values, int startIndex, StringComparison comparisonType)
		{
			if (values.Length == 0) return new KeyValuePair<int, int>(-1, -1);
			if (values.Length == 1) return new KeyValuePair<int, int>(str.IndexOf(values[0], startIndex, comparisonType), 0);
			int index = -1;
			for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
			{
				index = str.IndexOf(values[valueIndex], startIndex, comparisonType);
				if (index >= 0) return new KeyValuePair<int, int>(index, valueIndex);
			}
			return new KeyValuePair<int, int>(-1, -1);
		}
		public static byte[] GetBytes(this string str, Encoding encoding)
		{
			return encoding.GetBytes(str);
		}
		public static byte[] GetBytes(this string str)
		{
			return str.GetBytes(Encoding.UTF8);
		}
		/// <summary>
		/// Formats string with case-sensitive named place holders (e.g. Hello {Name}).
		/// </summary>
		/// <param name="str">The input format string</param>
		/// <param name="parameters">The parameters object (e.g. new { Name = "..." }).</param>
		public static string Format(this string str, object parameters)
		{
			if (str.IsNullOrEmpty()) return str;
			
			var props = parameters.GetType().GetProperties();
			var buffer = new StringBuilder(str);
			var fParams = new object[props.Length];
			for (int i = 0; i < props.Length; i++)
			{
				buffer.Replace(string.Concat("{", props[i].Name, "}"), string.Concat("{", i.ToString(), "}"));
				fParams[i] = props[i].GetValue(parameters, null);
			}
			return string.Format(buffer.ToString(), fParams);
		}
	}

	public static class ReflectionExtensions
	{
#if !NET45
		public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = false) where T : System.Attribute
		{
			return (T)Attribute.GetCustomAttribute(member, typeof(T), inherit);
		}
		public static T[] GetCustomAttributes<T>(this MemberInfo member, bool inherit = false) where T : System.Attribute
		{
			return Attribute.GetCustomAttributes(member, typeof(T), inherit).OfType<T>().ToArray();
		}
		public static T GetCustomAttribute<T>(this ParameterInfo parameter, bool inherit = false) where T : System.Attribute
		{
			return (T)Attribute.GetCustomAttribute(parameter, typeof(T), inherit);
		}
		public static T[] GetCustomAttributes<T>(this ParameterInfo parameter, bool inherit = false) where T : System.Attribute
		{
			return Attribute.GetCustomAttributes(parameter, typeof(T), inherit).OfType<T>().ToArray();
		}
#endif
		public static bool IsDefined<T>(this MemberInfo member, bool inherit = false) where T : System.Attribute
		{
			return member.IsDefined(typeof(T), inherit);
		}
		public static bool IsDefined<T>(this ParameterInfo parameter, bool inherit = false) where T : System.Attribute
		{
			return parameter.IsDefined(typeof(T), inherit);
		}
	}
}
namespace System.IO
{
	public static class IOExtensions
	{
		public static byte[] ToBytes(this Stream stream)
		{
			var buffer = new byte[stream.Length];
			long prevPos = -1;
			if (stream.CanSeek && stream.Position != 0)
			{
				prevPos = stream.Position;
				stream.Seek(0, SeekOrigin.Begin);
			}
			stream.Read(buffer, 0, buffer.Length);
			if (prevPos != -1) stream.Seek(prevPos, SeekOrigin.Begin);
			return buffer;
		}
	}
}
namespace System.Collections.Generic
{
	using System.Linq;

	public static class ListExtensions
	{
		public static void Assign<T>(this ICollection<T> collection, IEnumerable<T> newItems)
		{
			collection.Clear();
			collection.AddRange(newItems);
		}
		public static void Swap<T>(this IList<T> source, int oldIndex, int newIndex)
		{
			if (oldIndex < 0 || oldIndex >= source.Count) throw new ArgumentOutOfRangeException(nameof(oldIndex));
			if (newIndex < 0 || newIndex >= source.Count) throw new ArgumentOutOfRangeException(nameof(newIndex));

			if (oldIndex == newIndex) return;

			var temp = source[oldIndex];
			source[oldIndex] = source[newIndex];
			source[newIndex] = temp;
		}
	}
}
namespace System.Linq
{
	using IO;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using Text;

	public static class EnumerableExtensions
	{
		public static MemoryStream ToStream(this IEnumerable<byte> bytes, bool writable = true)
		{
			return new MemoryStream(bytes.ToArray(), writable);
		}
		public static string GetString(this IEnumerable<byte> bytes)
		{
			return bytes.GetString(Encoding.UTF8);
		}
		public static string GetString(this IEnumerable<byte> bytes, Encoding encoding)
		{
			return encoding.GetString(bytes.ToArray());
		}
		public static string ToHexString(this IEnumerable<byte> bytes)
		{
			var result = new StringBuilder();
			foreach (byte b in bytes)
			{
				result.Append(b.ToString("x2"));
			}
			return result.ToString();
		}
		public static IEnumerable<Tuple<T, T>> Map<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer)
		{
			foreach (var item in first)
			{
				foreach (var item2 in second)
				{
					if (comparer.Equals(item, item2))
						yield return new Tuple<T, T>(item, item2);
				}
			}
			yield break;
		}
		public static IEnumerable<Tuple<T, T>> Map<T, TProperty>(this IEnumerable<T> first, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
		{
			var comparer = Golden.EqualityComparer.ByProperty(property);
			foreach (var item in first)
			{
				foreach (var item2 in second)
				{
					if (comparer.Equals(item, item2))
						yield return new Tuple<T, T>(item, item2);
				}
			}
			yield break;
		}
		public static IEnumerable<Tuple<TFirst, TSecond>> Map<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> mapper)
		{
			foreach (var item in first)
			{
				foreach (var item2 in second)
				{
					if (mapper.Invoke(item, item2))
						yield return new Tuple<TFirst, TSecond>(item, item2);
				}
			}
			yield break;
		}
		public static IEnumerable<T> Distinct<T>(this IEnumerable<T> collection, Func<T, T, bool> comparer)
		{
			return collection.Distinct(new Golden.EqualityComparer<T>(comparer));
		}
		public static IEnumerable<T> Distinct<T, TProperty>(this IEnumerable<T> collection, Expression<Func<T, TProperty>> property)
		{
			return collection.Distinct(Golden.EqualityComparer.ByProperty(property));
		}
		public static IEnumerable<T> Intersect<T, TProperty>(this IEnumerable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
		{
			return collection.Intersect(second, Golden.EqualityComparer.ByProperty(property));
		}
		public static IEnumerable<T> Except<T, TProperty>(this IEnumerable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
		{
			return collection.Except(second, Golden.EqualityComparer.ByProperty(property));
		}
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action.Invoke(item);
			}
		}
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T, int> action)
		{
			int i = 0;
			foreach (var item in collection)
			{
				action.Invoke(item, i);
				i++;
			}
		}
		public static IEnumerable<T> ExcludeNull<T>(this IEnumerable<T> collection)
		{
			foreach (var item in collection)
			{
				if (item != null) yield return item;
			}
			yield break;
		}
		public static bool TryFirst<T>(this IEnumerable<T> collection, Func<T, bool> predicate, out T result)
		{
			foreach (var item in collection)
			{
				if (predicate.Invoke(item))
				{
					result = item;
					return true;
				}
			}
			result = default(T);
			return false;
		}
		public static void AddRange<T>(this ICollection<T> source, params T[] items)
		{
			if (items == null || items.Length == 0) return;
			foreach (var item in items)
			{
				source.Add(item);
			}
		}
		public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> collection)
		{
			foreach (var item in collection)
			{
				source.Add(item);
			}
		}
	}

	public static class QueryableExtensions
	{
		public static IQueryable<T> Distinct<T>(this IQueryable<T> collection, Func<T, T, bool> comparer)
		{
			return collection.Distinct(new Golden.EqualityComparer<T>(comparer));
		}
		public static IQueryable<T> Distinct<T, TProperty>(this IQueryable<T> collection, Expression<Func<T, TProperty>> property)
		{
			return collection.Distinct(Golden.EqualityComparer.ByProperty(property));
		}
		public static IQueryable<T> Intersect<T, TProperty>(this IQueryable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
		{
			return collection.Intersect(second, Golden.EqualityComparer.ByProperty(property));
		}
		public static IQueryable<T> Except<T, TProperty>(this IQueryable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
		{
			return collection.Except(second, Golden.EqualityComparer.ByProperty(property));
		}
		public static IQueryable<T> ExcludeNull<T>(this IQueryable<T> collection)
		{
			return collection.Where(item => item != null);
		}
	}
}
namespace System.Linq.Expressions
{
	public static class ExpressionExtensions
	{
		public static Expression GetQuoted(this Expression expression)
		{
			return expression.GetQuoted<Expression>();
		}
		public static T GetQuoted<T>(this Expression expression) where T : Expression
		{
			if (expression == null || expression.NodeType != ExpressionType.Quote) return (expression as T);
			return ((UnaryExpression)expression).Operand?.GetQuoted<T>();
		}
	}
}
namespace System.Xml.Linq
{
	public static class XmlLinqExtensions
	{
		public static string GetAttributeValue(this XElement element, XName name)
		{
			var attrib = element.Attribute(name);
			if (attrib != null) return attrib.Value;
			return null;
		}
		//public static void SetOnlyAttributeValue(this XElement element, XName name, object value, bool emptyAsNull)
		//{
		//	if (emptyAsNull && value is string && "".Equals((string)value)) value = null;
		//	if (value == null) return;
		//	element.SetAttributeValue(name, value);
		//}
	}
}
