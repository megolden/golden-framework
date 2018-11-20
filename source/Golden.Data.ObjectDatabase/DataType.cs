using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Golden.Data.ObjectDatabase
{
	public class DataType : DbSchemaObject
	{
        public static readonly DataType SqlVariant = new DataType(SqlDbType.Variant);

		public SqlDbType SqlDbType { get; internal set; }
		public int MaximumLength { get; internal set; }
		public short NumericPrecision { get; internal set; }
		public short NumericScale { get; internal set; }
		public bool IsUserDefined { get; private set; }
        public bool IsTableType { get; private set; }
        public ReadOnlyCollection<Column> TableTypeColumns { get; internal set; }

        public DataType(string providerName, int maxLength = 0, short precision = 0, short scale = 0, 
            bool isUserDefined = false, bool isTableType = false, 
            string schema = null) : base(providerName, schema)
		{
            var sqlDbType = SqlDbType.Variant;
            var match = Regex.Match(providerName, @"^ (?<name>\w+) \s* (\( \s* (?<lenprec>\w+) \s* (, \s* (?<scale>\d+))? \s* \))? $", RegexOptions.IgnorePatternWhitespace);
            if (match.Success)
            {
                providerName = match.Groups["name"].Value;
                var lenprec = (match.Groups["lenprec"].Success ? match.Groups["lenprec"].Value : null);
                var nscale = (match.Groups["scale"].Success ? match.Groups["scale"].Value : null);
                if (!string.IsNullOrEmpty(lenprec))
                {
                    if ("max".Equals(lenprec, StringComparison.OrdinalIgnoreCase))
                    {
                        maxLength = -1;
                    }
                    else if (nscale == null)
                    {
                        maxLength = int.Parse(lenprec);
                    }
                    else
                    {
                        precision = short.Parse(lenprec);
                    }
                }
                if (!string.IsNullOrEmpty(nscale)) scale = short.Parse(nscale);
                switch (providerName.ToLower())
                {
                    case "bigint":
                    case "bit":
                    case "date":
                    case "datetime":
                    case "datetime2":
                    case "datetimeoffset":
                    case "image":
                    case "float":
                    case "int":
                    case "real":
                    case "smalldatetime":
                    case "smallint":
                    case "smallmoney":
                    case "xml":
                    case "uniqueidentifier":
                    case "tinyint":
                    case "timestamp":
                    case "money":
                    case "ntext":
                    case "text":
						sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), providerName, true);
                        maxLength = 0;
                        precision = 0;
                        scale = 0;
                        break;
                    case "rowversion":
                        sqlDbType = SqlDbType.Timestamp;
                        maxLength = 0;
                        precision = 0;
                        scale = 0;
                        break;
                    case "sql_variant":
                        sqlDbType = SqlDbType.Variant;
                        maxLength = 0;
                        precision = 0;
                        scale = 0;
                        break;

                    case "decimal":
                    case "numeric":
						sqlDbType = SqlDbType.Decimal;
                        maxLength = 0;
                        break;

                    case "binary":
                    case "char":
                    case "nchar":
                        sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), providerName, true);
                        precision = 0;
                        scale = 0;
                        break;
                    case "varbinary":
                    case "nvarchar":
                    case "varchar":
                        sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), providerName, true);
                        precision = 0;
                        scale = 0;
                        break;
                    case "filestream":
                        sqlDbType = SqlDbType.VarBinary;
                        maxLength = -1;
                        precision = 0;
                        scale = 0;
                        break;

                    case "time":
                        sqlDbType = SqlDbType.Time;
                        precision = 0;
                        break;
                }
                if (isTableType)
                {
                    sqlDbType = SqlDbType.Structured;
                }
                else if (isUserDefined)
                {
                    sqlDbType = SqlDbType.Udt;
                }
            }
            this.IsUserDefined = isUserDefined;
            this.IsTableType = isTableType;
            Initialize(sqlDbType, maxLength, precision, scale);
		}
		public DataType(SqlDbType sqlDbType, int maxLength = 0, short precision = 0, short scale = 0)
		{
            Initialize(sqlDbType, maxLength, precision, scale);
		}
		public DataType() : this(SqlDbType.Variant)
		{
		}
        private void Initialize(SqlDbType sqlDbType, int maxLength = 0, short precision = 0, short scale = 0)
        {
            this.SqlDbType = sqlDbType;
            this.MaximumLength = maxLength;
            this.NumericPrecision = precision;
            this.NumericScale = scale;
        }
        public Type GetClrType(bool nullable = true)
		{
			#region fnGetNullable
			Func<Type, Type> fnGetNullable = t =>
			{
				if (!nullable) return t;
				if (Golden.Utility.TypeHelper.CanBeNull(t)) return t;
				return typeof(Nullable<>).MakeGenericType(t);
			};
            #endregion
            switch (this.SqlDbType)
			{
				case SqlDbType.BigInt:
					return fnGetNullable(typeof(long));
				case SqlDbType.Binary:
				case SqlDbType.VarBinary:
					return fnGetNullable((this.MaximumLength == 1 ? typeof(byte) : typeof(byte[])));
				case SqlDbType.Bit:
					return fnGetNullable(typeof(bool));
				case SqlDbType.Char:
				case SqlDbType.VarChar:
					return fnGetNullable((this.MaximumLength == 1 ? typeof(char) : typeof(string)));
				case SqlDbType.NChar:
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
				case SqlDbType.Text:
					return fnGetNullable(typeof(string));
				case SqlDbType.DateTime:
				case SqlDbType.Date:
				case SqlDbType.DateTime2:
				case SqlDbType.SmallDateTime:
					return fnGetNullable(typeof(DateTime));
				case SqlDbType.Decimal:
				case SqlDbType.Money:
				case SqlDbType.SmallMoney:
					return fnGetNullable(typeof(decimal));
				case SqlDbType.Float:
					return fnGetNullable(typeof(double));
				case SqlDbType.Int:
					return fnGetNullable(typeof(int));
				case SqlDbType.Real:
					return fnGetNullable(typeof(float));
				case SqlDbType.SmallInt:
					return fnGetNullable(typeof(short));
				case SqlDbType.TinyInt:
					return fnGetNullable(typeof(byte));
				case SqlDbType.UniqueIdentifier:
					return fnGetNullable(typeof(Guid));
				case SqlDbType.Variant:
					return fnGetNullable(typeof(object));
				case SqlDbType.Time:
					return fnGetNullable(typeof(TimeSpan));
				case SqlDbType.DateTimeOffset:
					return fnGetNullable(typeof(DateTimeOffset));

				case SqlDbType.Xml:
					return fnGetNullable(typeof(string));
				case SqlDbType.Image:
					return fnGetNullable(typeof(byte[]));
				case SqlDbType.Timestamp:
					return fnGetNullable(typeof(byte[]));

                case SqlDbType.Udt:
                case SqlDbType.Structured:
                    return null; // return fnGetNullable(typeof(object));
            }
			return null;
		}
        public override string ToString()
        {
            if (IsTableType || IsUserDefined)
                return base.ToString();

            var name = this.SqlDbType.ToString();
            if (this.MaximumLength == -1)
            {
                name = name.Append("Max");
            }
            else if (this.NumericScale > 0)
            {
                name = $"{name}({this.NumericPrecision}, {this.NumericScale})";
            }
            else if (this.NumericPrecision > 0)
            {
                name = $"{name}({this.NumericPrecision})";
            }
            else if (this.MaximumLength > 0)
            {
                name = $"{name}({this.MaximumLength})";
            }
            return name;
        }
    }
}
