using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
	public class Database : DbObjectBase
    {
        private readonly Lazy<ReadOnlyCollection<Table>> _Tables;
        private readonly Lazy<ReadOnlyCollection<View>> _Views;
        private readonly Lazy<ReadOnlyCollection<Schema>> _Schemas;
        private readonly Lazy<ReadOnlyCollection<StoredProcedure>> _StoredProcedures;
        private readonly Lazy<ReadOnlyCollection<UserDefinedFunction>> _UserDefinedFunctions;
        private readonly Lazy<ReadOnlyCollection<DataType>> _UserDefinedTypes;
        private readonly Lazy<string> _DefaultSchema;

        public ReadOnlyCollection<Table> Tables
        {
            get { return _Tables.Value; }
        }
        public ReadOnlyCollection<View> Views
        {
            get { return _Views.Value; }
        }
        public ReadOnlyCollection<StoredProcedure> StoredProcedures
        {
            get { return _StoredProcedures.Value; }
        }
        public ReadOnlyCollection<UserDefinedFunction> UserDefinedFunctions
        {
            get { return _UserDefinedFunctions.Value; }
        }
        public ReadOnlyCollection<DataType> UserDefinedTypes
        {
            get { return _UserDefinedTypes.Value; }
        }
        public ReadOnlyCollection<Schema> Schemas
        {
            get { return _Schemas.Value; }
        }
        public new Server Parent
		{
			get { return base.Parent as Server; }
			internal set { base.Parent = value; }
		}
        public string DefaultSchema
        {
            get { return _DefaultSchema.Value; }
        }

        public Database(string name) : base(name)
        {
            _Tables = new Lazy<ReadOnlyCollection<Table>>(LoadTables);
            _Views = new Lazy<ReadOnlyCollection<View>>(LoadViews);
            _StoredProcedures = new Lazy<ReadOnlyCollection<StoredProcedure>>(LoadStoredProcedures);
            _UserDefinedFunctions = new Lazy<ReadOnlyCollection<UserDefinedFunction>>(LoadUserDefinedFunctions);
            _UserDefinedTypes = new Lazy<ReadOnlyCollection<DataType>>(LoadUserDefinedTypes);
            _Schemas = new Lazy<ReadOnlyCollection<Schema>>(LoadSchemas);
            _DefaultSchema = new Lazy<string>(LoadDefaultSchema);
        }
        private string LoadDefaultSchema()
        {
			return (string)this.ExecuteScalar("SELECT SCHEMA_NAME()");
        }
        public Table Table(string name)
        {
            string schema = null;
            var i = name.LastIndexOf('.');
            if (i >= 0)
            {
                schema = name.Remove(i);
                name = name.Remove(0, i + 1);
            }
            return Table(name, (schema ?? DefaultSchema));
        }
        public Table Table(string name, string schema)
        {
            return _Tables.Value.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase));
        }
        public View View(string name)
        {
            string schema = null;
            var i = name.LastIndexOf('.');
            if (i >= 0)
            {
                schema = name.Remove(i);
                name = name.Remove(0, i + 1);
            }
            return View(name, (schema ?? DefaultSchema));
        }
        public View View(string name, string schema)
        {
			return _Views.Value.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase));
        }
        public StoredProcedure StoredProcedure(string name)
        {
            string schema = null;
            var i = name.LastIndexOf('.');
            if (i >= 0)
            {
                schema = name.Remove(i);
                name = name.Remove(0, i + 1);
            }
            return StoredProcedure(name, (schema ?? DefaultSchema));
        }
        public StoredProcedure StoredProcedure(string name, string schema)
        {
			return _StoredProcedures.Value.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase));
        }
        public UserDefinedFunction UserDefinedFunction(string name)
        {
            string schema = null;
            var i = name.LastIndexOf('.');
            if (i >= 0)
            {
                schema = name.Remove(i);
                name = name.Remove(0, i + 1);
            }
            return UserDefinedFunction(name, (schema ?? DefaultSchema));
        }
        public UserDefinedFunction UserDefinedFunction(string name, string schema)
        {
			return _UserDefinedFunctions.Value.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase));
        }
        public DataType UserDefinedType(string name)
        {
            string schema = null;
            var i = name.LastIndexOf('.');
            if (i >= 0)
            {
                schema = name.Remove(i);
                name = name.Remove(0, i + 1);
            }
            return UserDefinedType(name, (schema ?? DefaultSchema));
        }
        public DataType UserDefinedType(string name, string schema)
        {
            return _UserDefinedTypes.Value.First(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase));
        }
        private ReadOnlyCollection<Table> LoadTables()
        {
            var result = new List<Table>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetTables");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var tbl = new Table(Convert.ToString(reader["Name"]), Convert.ToString(reader["Schema"]));
                        tbl.Parent = this;
                        result.Add(tbl);
                    }
                }
            });

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<View> LoadViews()
        {
            var result = new List<View>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetViews");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var vw = new View(Convert.ToString(reader["Name"]), Convert.ToString(reader["Schema"]));
                        vw.Parent = this;
                        result.Add(vw);
                    }
                }
            });

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<Schema> LoadSchemas()
        {
            var result = new List<Schema>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetSchemas");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var sch = new Schema(Convert.ToInt32(reader["Id"]), Convert.ToString(reader["Name"]), Utility.Utilities.Convert<int?>(reader["OwnerId"], true));
                        sch.Parent = this;
                        result.Add(sch);
                    }
                }
            });

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<StoredProcedure> LoadStoredProcedures()
        {
            var result = new List<StoredProcedure>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetStoredProcedures");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var proc = new StoredProcedure(Convert.ToString(reader["Name"]), Convert.ToString(reader["Schema"]));
                        proc.Parent = this;
                        result.Add(proc);
                    }
                }
            });

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<UserDefinedFunction> LoadUserDefinedFunctions()
        {
            var result = new List<UserDefinedFunction>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetUDFunctions");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var udfunc = new UserDefinedFunction(Convert.ToString(reader["Name"]), Convert.ToString(reader["Schema"]));
                        if (!Convert.ToBoolean(reader["IsTableValued"]))
                        {
                            var typeName = Utility.Utilities.Convert<string>(reader["ReturnTypeName"], true);
                            var maxLen = Utility.Utilities.Convert<int?>(reader["ReturnMaxLength"], true).GetValueOrDefault(0);
                            var precision = Utility.Utilities.Convert<short?>(reader["ReturnPrecision"], true).GetValueOrDefault(0);
                            var scale = Utility.Utilities.Convert<short?>(reader["ReturnScale"], true).GetValueOrDefault(0);

                            udfunc.DataType = new DataType(typeName, maxLen, precision, scale);
                        }
                        udfunc.Parent = this;
                        result.Add(udfunc);
                    }
                }
            });

            return result.AsReadOnly();
        }
        private ReadOnlyCollection<DataType> LoadUserDefinedTypes()
        {
            var result = new List<DataType>();
            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetUDTypes");
            this.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var userTypeId = Utility.Utilities.Convert<int>(reader["UserTypeId"], true);
                        var schemaName = Utility.Utilities.Convert<string>(reader["SchemaName"], true);
                        var name = Utility.Utilities.Convert<string>(reader["Name"], true);
                        var maxLen = Utility.Utilities.Convert<short?>(reader["MaxLength"], true).GetValueOrDefault(0);
                        var precision = Utility.Utilities.Convert<byte?>(reader["NumericPrecision"], true).GetValueOrDefault(0);
                        var scale = Utility.Utilities.Convert<byte?>(reader["NumericScale"], true).GetValueOrDefault(0);
                        var isNullable = Utility.Utilities.Convert<bool?>(reader["IsNullable"], true).GetValueOrDefault(false);
                        var isTableType = Utility.Utilities.Convert<bool?>(reader["IsTableType"], true).GetValueOrDefault(false);
                        var udType = new DataType(name, maxLen, precision, scale, true, isTableType, schemaName) { Id = userTypeId };
                        udType.Parent = this;
                        result.Add(udType);
                    }

                    if (reader.NextResult() && reader.HasRows)
                    {
                        var typeCols = new List<KeyValuePair<int, Column>>();
                        while (reader.Read())
                        {
                            var userTypeId = Utility.Utilities.Convert<int>(reader["UserTypeId"], true);
                            var udtType = result.First(t => t.Id == userTypeId);

                            var colName = Convert.ToString(reader["Name"]);
                            var type = new DataType(
                                Utility.Utilities.Convert<string>(reader["TypeName"], true),
                                Convert.ToInt32(reader["MaxLength"]),
                                Convert.ToInt16(reader["NumericPrecision"]),
                                Convert.ToInt16(reader["NumericScale"]),
                                schema: Utility.Utilities.Convert<string>(reader["TypeSchemaName"], true));
                            var col = new Column(udtType, colName, type)
                            {
                                IsNullable = Utility.Utilities.Convert<bool?>(reader["IsNullable"], true).GetValueOrDefault(false),
                                IsIdentity = Utility.Utilities.Convert<bool?>(reader["IsIdentity"], true).GetValueOrDefault(false),
                                IsComputed = Utility.Utilities.Convert<bool?>(reader["IsComputed"], true).GetValueOrDefault(false)
                            };
                            typeCols.Add(new KeyValuePair<int, Column>(userTypeId, col));
                        }
                        typeCols.GroupBy(tc => tc.Key).ForEach(x => result.First(t => t.Id == x.Key).TableTypeColumns = x.Select(i => i.Value).ToList().AsReadOnly());
                    }
                }
            });

            return result.AsReadOnly();
        }
        public int ExecuteNonQuery(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.Parent.InternalExecuteNonQuery(this.Name, sqlCommand, parameters);
		}
		public object ExecuteScalar(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.Parent.InternalExecuteScalar(this.Name, sqlCommand, parameters);
		}
		public void ExecuteReader(string sqlCommand, Action<SqlDataReader> action, SqlParameter[] parameters = null, CommandBehavior behavior = CommandBehavior.Default)
		{
			this.Parent.InternalExecuteReader(this.Name, sqlCommand, action, parameters, behavior);
		}
		public DataSet ExecuteWithResults(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.Parent.InternalExecuteWithResults(this.Name, sqlCommand, parameters);
		}
		public DataTable ExecuteWithResult(string sqlCommand, SqlParameter[] parameters = null)
		{
			return this.Parent.InternalExecuteWithResult(this.Name, sqlCommand, parameters);
		}
		public static Database FromConnectionString(string connectionString)
		{
			var csb = new SqlConnectionStringBuilder(connectionString);
			var server = new Server();
			server.Initialize(connectionString);
			return new Database(csb.InitialCatalog) { Parent = server };
		}
	}
}
