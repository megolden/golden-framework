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
        public static string ReplaceNewLines(this string str, string newValue)
        {
            if (str.IsNullOrWhiteSpace()) return str;

            var buffer = new StringBuilder(str);
            buffer.Replace("\r\n", newValue);
            buffer.Replace("\r", newValue);
            buffer.Replace("\n", newValue);

            return buffer.ToString();
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
        public static string ToBase64(this string str)
        {
            return str.ToBase64(Encoding.UTF8);
        }
        public static string ToBase64(this string str, Encoding encoding)
        {
            return Convert.ToBase64String(encoding.GetBytes(str));
        }
        public static string FromBase64(this string str)
        {
            return str.FromBase64(Encoding.UTF8);
        }
        public static string FromBase64(this string str, Encoding encoding)
        {
            return Convert.FromBase64String(str).GetString(encoding);
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
    public static class DateTimeExtensions
    {
        public static Golden.PersianDateTime ToPersianDateTime(this DateTime date)
        {
            return new Golden.PersianDateTime(date);
        }
    }
    public static class TypeExtensions
    {
        public static string NonGenericName(this Type type, bool fullName)
        {
            var name = (fullName ? type.FullName : type.Name);
            var i = name.IndexOf('`');
            if (i >= 0) return name.Remove(i);
            return name;
        }
        public static string NonGenericName(this Type type)
        {
            return type.NonGenericName(false);
        }
        public static string FriendlyName(this Type type, bool fullName)
        {
            if (type == typeof(bool))
                return "bool";
            else if (type == typeof(char))
                return "char";
            else if (type == typeof(sbyte))
                return "sbyte";
            else if (type == typeof(byte))
                return "byte";
            else if (type == typeof(short))
                return "short";
            else if (type == typeof(ushort))
                return "ushort";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(uint))
                return "uint";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(ulong))
                return "ulong";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(decimal))
                return "decimal";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(object))
                return "object";
            else if (type == typeof(void))
                return "void";

            if (type.IsArray)
            {
                return string.Format(
                    "{0}[{1}]",
                    type.GetElementType().FriendlyName(fullName),
                    string.Join(" ", Enumerable.Repeat(",", type.GetArrayRank() - 1)));
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                return string.Format("{0}?", nullableType.FriendlyName(fullName));
            }

            var genArgs = type.GetGenericArguments();
            if (genArgs.Length > 0)
            {
                return string.Format(
                    "{0}<{1}>",
                    type.NonGenericName(fullName),
                    string.Join(", ", genArgs.Select(t => t.FriendlyName(fullName))));
            }

            return (fullName ? type.FullName : type.Name);
        }
        public static string FriendlyName(this Type type)
        {
            return type.FriendlyName(false);
        }
    }
}
namespace System.IO
{
    using System.Collections.Generic;
    using System.Linq;

    public static class IOExtensions
    {
        public static byte[] ToBytes(this Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }

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
        public static void SaveToFile(this Stream stream, string filePath)
        {
            File.WriteAllBytes(filePath, stream.ToBytes());
        }
        public static void WriteToFile(this IEnumerable<byte> bytes, string filePath)
        {
            File.WriteAllBytes(filePath, bytes.ToArray());
        }
        public static MemoryStream ToStream(this IEnumerable<byte> bytes)
        {
            return bytes.ToStream(false);
        }
        public static MemoryStream ToStream(this IEnumerable<byte> bytes, bool writable)
        {
            return new MemoryStream(bytes.ToArray(), writable);
        }
    }
}
namespace System.Text
{
    using System.IO;

    public static class TextExtensions
    {
        public static void SaveToFile(this string str, string filePath)
        {
            str.SaveToFile(filePath, Encoding.UTF8);
        }
        public static void SaveToFile(this string str, string filePath, Encoding encoding)
        {
            File.WriteAllText(filePath, str, encoding);
        }
        public static void SaveToFile(this StringBuilder builder, string filePath)
        {
            builder.SaveToFile(filePath, Encoding.UTF8);
        }
        public static void SaveToFile(this StringBuilder builder, string filePath, Encoding encoding)
        {
            builder.ToString().SaveToFile(filePath, encoding);
        }
        public static void AppendFile(this StringBuilder builder, string filePath)
        {
            builder.AppendFile(filePath, Encoding.UTF8);
        }
        public static void AppendFile(this StringBuilder builder, string filePath, Encoding encoding)
        {
            builder.Append(File.ReadAllText(filePath, encoding));
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
    using Reflection;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Text;
    using Golden;
    using Text.RegularExpressions;
    using ComponentModel;

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
            var comparer = Golden.EqualityComparer<T>.ByProperty(property);
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
            return collection.Distinct(Golden.EqualityComparer<T>.ByProperty(property));
        }
        public static IEnumerable<T> Intersect<T, TProperty>(this IEnumerable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
        {
            return collection.Intersect(second, Golden.EqualityComparer<T>.ByProperty(property));
        }
        public static IEnumerable<T> Except<T, TProperty>(this IEnumerable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
        {
            return collection.Except(second, Golden.EqualityComparer<T>.ByProperty(property));
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
        public static IEnumerable<T> ExcludeNull<T>(this IEnumerable<T> source)
        {
            return source.Where(item => item != null);
        }
        public static bool Any<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T result)
        {
            foreach (var item in source)
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
        public static IEnumerable<T> TakePage<T>(this IEnumerable<T> source, int page, int count)
        {
            return source.Skip((page - 1) * count).Take(count);
        }
    }
    public static class QueryableExtensions
    {
        #region Fields

        private static Lazy<MethodInfo> mOrderBy = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.OrderBy), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2);
        });
        private static Lazy<MethodInfo> mOrderByComparer = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.OrderBy), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 3);
        });
        private static Lazy<MethodInfo> mThenBy = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.ThenBy), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2);
        });
        private static Lazy<MethodInfo> mThenByComparer = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.ThenBy), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 3);
        });
        private static Lazy<MethodInfo> mOrderByDescending = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.OrderByDescending), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2);
        });
        private static Lazy<MethodInfo> mOrderByDescendingComparer = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.OrderByDescending), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 3);
        });
        private static Lazy<MethodInfo> mThenByDescending = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.ThenByDescending), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 2);
        });
        private static Lazy<MethodInfo> mThenByDescendingComparer = new Lazy<MethodInfo>(() =>
        {
            return typeof(Queryable).GetMember(nameof(Queryable.ThenByDescending), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 3);
        });

        #endregion
        public static IQueryable<T> Distinct<T>(this IQueryable<T> collection, Func<T, T, bool> comparer)
        {
            return collection.Distinct(new Golden.EqualityComparer<T>(comparer));
        }
        public static IQueryable<T> Distinct<T, TProperty>(this IQueryable<T> collection, Expression<Func<T, TProperty>> property)
        {
            return collection.Distinct(Golden.EqualityComparer<T>.ByProperty(property));
        }
        public static IQueryable<T> Intersect<T, TProperty>(this IQueryable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
        {
            return collection.Intersect(second, Golden.EqualityComparer<T>.ByProperty(property));
        }
        public static IQueryable<T> Except<T, TProperty>(this IQueryable<T> collection, IEnumerable<T> second, Expression<Func<T, TProperty>> property)
        {
            return collection.Except(second, Golden.EqualityComparer<T>.ByProperty(property));
        }
        public static IQueryable<T> ExcludeNull<T>(this IQueryable<T> collection)
        {
            return collection.Where(item => item != null);
        }
        public static IQueryable<T> TakePage<T>(this IQueryable<T> source, int page, int count)
        {
            return source.Skip((page - 1) * count).Take(count);
        }
        public static IQueryable<T> Where<T, TProperty>(this IQueryable<T> source, string propertyName, Expression<Func<TProperty, bool>> predicate)
        {
            var exp = Expression.Lambda<Func<T, TProperty, bool>>(predicate.Body, Expression.Parameter(typeof(T), "obj"), predicate.Parameters[0]);
            return source.Where(propertyName, exp);
        }
        public static IQueryable<T> Where<T, TProperty>(this IQueryable<T> source, string propertyName, Expression<Func<T, TProperty, bool>> predicate)
        {
            var property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase);
            var propertyExp = Expression.MakeMemberAccess(predicate.Parameters[0], property);
            var condExp = ExpressionReplacer.Replace(predicate.Body, predicate.Parameters[1], propertyExp);
            var lCondExp = Expression.Lambda<Func<T, bool>>(condExp, predicate.Parameters[0]);
            return source.Where(lCondExp);
        }
        public static IQueryable<T> Sort<T>(this IQueryable<T> source, string sortDescription)
        {
            return source.Sort(sortDescription, (IComparer<object>)null);
        }
        public static IQueryable<T> Sort<T, TProperty>(this IQueryable<T> source, string sortDescription, IComparer<TProperty> comparer)
        {
            var sourceType = typeof(T);
            var propType = typeof(TProperty);
            var properties = sortDescription.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(sortDesc =>
                {
                    string pName = sortDesc.Trim();
                    char sortType = 'a';
                    int i = pName.IndexOf(' ');
                    if (i >= 0)
                    {
                        sortType = pName.Substring(i + 1).TrimStart().Left(1).ToLower()[0];
                        pName = pName.Remove(i);
                    }

                    var prop = sourceType.GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase);
                    return new KeyValuePair<PropertyInfo, ListSortDirection>(prop, (sortType == 'a' ? ListSortDirection.Ascending : ListSortDirection.Descending));
                })
                .ToList();

            if (properties.Count == 0) return source;

            var param = Expression.Parameter(sourceType, "source");
            if (properties.Count > 0)
            {
                var propertyExp = Expression.MakeMemberAccess(param, properties[0].Key);
                var lCondExp = Expression.Lambda(propertyExp, param);

                if (comparer != null)
                {
                    source = (IQueryable<T>)
                        (properties[0].Value == ListSortDirection.Ascending ? mOrderByComparer.Value : mOrderByDescendingComparer.Value)
                            .MakeGenericMethod(sourceType, properties[0].Key.PropertyType).Invoke(null, new object[] { source, lCondExp, comparer });
                }
                else
                {
                    source = (IQueryable<T>)
                        (properties[0].Value == ListSortDirection.Ascending ? mOrderBy.Value : mOrderByDescending.Value)
                            .MakeGenericMethod(sourceType, properties[0].Key.PropertyType).Invoke(null, new object[] { source, lCondExp });
                }
            }
            if (properties.Count > 1)
            {
                for (int i = 1; i < properties.Count; i++)
                {
                    var propertyExp = Expression.MakeMemberAccess(param, properties[i].Key);
                    var lCondExp = Expression.Lambda(propertyExp, param);

                    if (comparer != null)
                    {
                        source = (IQueryable<T>)
                            (properties[i].Value == ListSortDirection.Ascending ? mThenByComparer.Value : mThenByDescendingComparer.Value)
                                .MakeGenericMethod(sourceType, properties[i].Key.PropertyType).Invoke(null, new object[] { source, lCondExp, comparer });
                    }
                    else
                    {
                        source = (IQueryable<T>)
                            (properties[i].Value == ListSortDirection.Ascending ? mThenBy.Value : mThenByDescending.Value)
                                .MakeGenericMethod(sourceType, properties[i].Key.PropertyType).Invoke(null, new object[] { source, lCondExp });
                    }
                }
            }
            return source;
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
namespace Golden
{
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using System.Linq;

    public static class PersianDateTimeExtensions
    {
        public static PersianDateTime GetFirstDayOfYear(this PersianDateTime date)
        {
            return new PersianDateTime(date.Year, 1, 1);
        }
        public static PersianDateTime GetLastDayOfYear(this PersianDateTime date)
        {
            return new PersianDateTime(date.Year, 12, PersianDateTime.DaysInMonth(date.Year, 12), 23, 59, 59, 999);
        }
        public static YearSeason GetSeason(this PersianDateTime date)
        {
            return (YearSeason)((date.Month - 1) / 3);
        }
    }
    public static class ObjectQueryableExtensions
    {
        private class ObjectQueryableVisitor : ExpressionVisitor
        {
            private readonly Expression source;

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var valueType = node.Value?.GetType();
                if (valueType != null)
                {
                    if (valueType.IsGenericType && valueType == typeof(ObjectQueryable<>).MakeGenericType(valueType.GetGenericArguments()[0]))
                    {
                        return source;
                    }
                }
                return base.VisitConstant(node);
            }
            public ObjectQueryableVisitor(Expression source)
            {
                this.source = source;
            }
        }

        /// <summary>
        /// Applying an <see cref="ObjectQueryable{TResult}" /> object on input data source.
        /// </summary>
        /// <typeparam name="TSource">The type of the data in the input data source</typeparam>
        /// <typeparam name="TResult">The type of the value returned by query object</typeparam>
        /// <param name="source">The input data source</param>
        /// <param name="query">The queryable object for applying to data source</param>
        /// <returns>An <see cref="IEnumerable{TResult}"/> whose elements are the result of applying object query on input data source.</returns>
        public static IQueryable<TResult> ApplyQuery<TSource, TResult>(this IEnumerable<TSource> source, IQueryable<TResult> query)
        {
            if (object.ReferenceEquals(source, null))
                throw new ArgumentNullException(nameof(source));

            if (object.ReferenceEquals(query, null))
                return ((IEnumerable<TResult>)source).AsQueryable();

            var pSource = Expression.Parameter(typeof(IQueryable<TSource>), "source");
            var newExp = (new ObjectQueryableVisitor(pSource)).Visit(query.Expression);
            var iqExp = Expression.Lambda<Func<IQueryable<TSource>, IQueryable<TResult>>>(newExp, pSource);

            return iqExp.Compile().Invoke(source.AsQueryable());
        }
    }
}