using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Data;

namespace Golden.Data.ObjectDatabase
{
    public class Table : DbSchemaObject
    {
        private readonly Lazy<ReadOnlyCollection<Column>> _Columns;
        private readonly Lazy<ReadOnlyCollection<ForeignKey>> _ForeignKeys;
        private readonly Lazy<ReadOnlyCollection<TableConstraint>> _Constraints;

        public ReadOnlyCollection<Column> Columns
        {
            get { return _Columns.Value; }
        }
        public ReadOnlyCollection<ForeignKey> ForeignKeys
        {
            get { return _ForeignKeys.Value; }
        }
        public new Database Parent
        {
            get { return base.Parent as Database; }
            internal set { base.Parent = value; }
        }

        public Table() : this(null)
        {
        }
        public Table(string name) : this(name, (string)null)
        {
        }
        public Table(string name, string schema) : this(name, null, schema)
        {
        }
        internal Table(string name, IEnumerable<Column> columns = null, string schema = null) : base(name, schema)
        {
            if (columns == null)
                _Columns = new Lazy<ReadOnlyCollection<Column>>(LoadColumns);
            else
            {
                columns.ForEach(c=>c.Parent = this);
                _Columns = new Lazy<ReadOnlyCollection<Column>>(() => new ReadOnlyCollection<Column>(columns.ToArray()));
            }
            _ForeignKeys = new Lazy<ReadOnlyCollection<ForeignKey>>(LoadForeignKeys);
            _Constraints = new Lazy<ReadOnlyCollection<TableConstraint>>(LoadConstraints);
        }
        public Column Column(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _Columns.Value.First(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        private ReadOnlyCollection<TableConstraint> LoadConstraints()
        {
            var result = new List<TableConstraint>();

            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetConstraints");
            var parameters = new[]
            {
                new SqlParameter("@Schema", (object)this.Schema),
                new SqlParameter("@TableName", (object)this.Name),
            };
            this.Parent.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var constraint = new TableConstraint(
                            this,
                            Convert.ToString(reader["Name"]),
                            TableConstraint.ParseConstraintType(Convert.ToString(reader["TypeName"])));
                        constraint.UserData = Convert.ToString(reader["ColumnName"]);
                        result.Add(constraint);
                    }
                }
            }, parameters);

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<Column> LoadColumns()
        {
            var constraints = _Constraints.Value;

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
                            InPrimaryKey = constraints.Any(c => c.Type == ConstraintType.PrimaryKey && c.UserData != null && colName.Equals((string)c.UserData, StringComparison.OrdinalIgnoreCase)),
                            IsForeignKey = constraints.Any(c => c.Type == ConstraintType.ForeignKey && c.UserData != null && colName.Equals((string)c.UserData, StringComparison.OrdinalIgnoreCase)),
                            IsUnique = constraints.Any(c => c.Type == ConstraintType.Unique && c.UserData != null && colName.Equals((string)c.UserData, StringComparison.OrdinalIgnoreCase)),
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
        private ReadOnlyCollection<ForeignKey> LoadForeignKeys()
        {
            var columns = _Columns.Value;

            var result = new List<ForeignKey>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetForeignKeys");
            var parameters = new[]
            {
                new SqlParameter("@Schema", (object)this.Schema),
                new SqlParameter("@Name", (object)this.Name),
            };
            var resultTable = this.Parent.ExecuteWithResult(cmdStr, parameters);
            if (resultTable.Rows.Count > 0)
            {
                var fkColumns = resultTable.AsEnumerable().GroupBy(dr => dr.Field<string>("Name"), StringComparer.OrdinalIgnoreCase);
                foreach (var fk in fkColumns)
                {
                    var firstItem = fk.First();
                    var fCols = new List<ForeignKeyColumn>();
                    fk.ForEach(dr =>
                    {
                        fCols.Add(new ForeignKeyColumn
                        {
                            Column = this.Column(dr.Field<string>("FKColumn")),
                            ReferencedColumn = this.Parent.Table(dr.Field<string>("PKTable"), dr.Field<string>("PKSchema")).Column(dr.Field<string>("PKColumn")),
                        });
                    });
                    var foreignKey = new ForeignKey(fCols, fk.Key) { Parent = this, ReferencedTable = fCols[0].ReferencedColumn.Parent as Table };
                    fCols.ForEach(fc => fc.Parent = foreignKey);
                    foreignKey.Name = fk.Key;
                    foreignKey.UpdateAction = ParseForeignKeyAction(firstItem.Field<string>("UpdateRule"));
                    foreignKey.DeleteAction = ParseForeignKeyAction(firstItem.Field<string>("DeleteRule"));
                    result.Add(foreignKey);
                }
            }

            return result.AsReadOnly();
        }
        private static ForeignKeyAction ParseForeignKeyAction(string name)
        {
            if (string.IsNullOrEmpty(name)) return default(ForeignKeyAction);
            name = name.Trim().Append(",");
            if ("cascade,".IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return ForeignKeyAction.Cascade;
            else if ("setdefault,default,set default,set_default,".IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return ForeignKeyAction.SetDefault;
            else if ("setnull,null,set null,set_null,".IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return ForeignKeyAction.SetNull;
            return default(ForeignKeyAction);
        }
    }
    public class TableConstraint : DbObjectBase
    {
        public new Table Parent
        {
            get { return base.Parent as Table; }
            internal set { base.Parent = value; }
        }
        public ConstraintType Type { get; private set; }

        public TableConstraint(Table parent, string name, ConstraintType type) : base(name)
        {
            this.Parent = parent;
            this.Type = type;
        }
        internal static ConstraintType ParseConstraintType(string typeName)
        {
            typeName = typeName.Replace(" ", "");
            return (ConstraintType)Enum.Parse(typeof(ConstraintType), typeName, true);
        }
    }
}
