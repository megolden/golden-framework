using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;

namespace Golden.Data.ObjectDatabase
{
    public class View : DbSchemaObject
    {
        private readonly Lazy<ReadOnlyCollection<Column>> _Columns;

        public ReadOnlyCollection<Column> Columns
        {
            get { return _Columns.Value; }
        }
        public new Database Parent
        {
            get { return base.Parent as Database; }
            internal set { base.Parent = value; }
        }

        public View() : this(null)
        {
        }
        public View(string name) : this(name, (string)null)
        {
        }
        public View(string name, string schema) : this(name, null, schema)
        {
        }
        internal View(string name, IEnumerable<Column> columns = null, string schema = null) : base(name, schema)
        {
            if (columns == null)
                _Columns = new Lazy<ReadOnlyCollection<Column>>(LoadColumns);
            else
            {
                columns.ForEach(c => c.Parent = this);
                _Columns = new Lazy<ReadOnlyCollection<Column>>(() => new ReadOnlyCollection<Column>(columns.ToArray()));
            }
        }
		public Column Column(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			return _Columns.Value.First(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
		private ReadOnlyCollection<Column> LoadColumns()
        {
            var result = new List<Column>();

            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetColumns");
            var parameters = new[]
            {
                new SqlParameter("@FullTableName", (object)this.FullName),
            };
            this.Parent.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var colName = Convert.ToString(reader["Name"]);
                        var type = new DataType(
                            Convert.ToString(reader["TypeName"]),
                            Convert.ToInt32(reader["MaxLength"]),
                            Convert.ToInt16(reader["NumericPrecision"]),
                            Convert.ToInt16(reader["NumericScale"]));
                        var col = new Column(this, colName, type)
                        {
                            IsComputed = Convert.ToBoolean(reader["IsComputed"]),
                            IsIdentity = Convert.ToBoolean(reader["IsIdentity"]),
                            IsNullable = Convert.ToBoolean(reader["IsNullable"]),
                        };
                        result.Add(col);
                    }
                }
            }, parameters);

            return result.AsReadOnly();
        }
    }
}
