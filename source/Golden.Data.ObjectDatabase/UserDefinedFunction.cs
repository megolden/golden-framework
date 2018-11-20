using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;

namespace Golden.Data.ObjectDatabase
{
    public class UserDefinedFunction : DbSchemaObject
    {
        private readonly Lazy<ReadOnlyCollection<FunctionParameter>> _Parameters;
        private readonly Lazy<ReadOnlyCollection<Column>> _Columns;

        public new Database Parent
        {
            get { return base.Parent as Database; }
            internal set { base.Parent = value; }
        }
        public DataType DataType { get; internal set; }
        public bool IsTableValued
        {
            get { return (this.DataType == null); }
        }
        public ReadOnlyCollection<Column> Columns
        {
            get { return _Columns.Value; }
        }
        public ReadOnlyCollection<FunctionParameter> Parameters { get { return _Parameters.Value; } }

        public UserDefinedFunction()
        {
        }
        public UserDefinedFunction(string name) : base(name)
        {
        }
        public UserDefinedFunction(string name, string schema) : base(name, schema)
        {
            _Parameters = new Lazy<ReadOnlyCollection<FunctionParameter>>(LoadParameters);
            _Columns = new Lazy<ReadOnlyCollection<Column>>(LoadColumns);
        }
        private ReadOnlyCollection<FunctionParameter> LoadParameters()
        {
            this.Parent.UserDefinedTypes.AsEnumerable(); //Loads database user defined types.

            var result = new List<FunctionParameter>();

            var cmdStr = Resources.ResourceManager.Default.GetDbScript("GetParameters");
            var parameters = new[]
            {
                new SqlParameter("@FullObjectName", (object)this.FullName),
            };
            this.Parent.ExecuteReader(cmdStr, reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var isUserDefinedType = Convert.ToBoolean(reader["IsUserDefinedType"]);
                        var schemaName = Convert.ToString(reader["SchemaName"]);
                        var name = Convert.ToString(reader["TypeName"]);
                        DataType type = null;
                        if (isUserDefinedType)
                        {
                            type = this.Parent.UserDefinedType(name, schemaName);
                        }
                        else
                        {
                            type = new DataType(
                                Convert.ToString(reader["TypeName"]),
                                Convert.ToInt32(reader["MaxLength"]),
                                Convert.ToInt16(reader["NumericPrecision"]),
                                Convert.ToInt16(reader["NumericScale"]),
                                schema: Utility.Utilities.Convert<string>(reader["SchemaName"], true))
                            { Parent = this.Parent };
                        }

                        var parameter = new FunctionParameter(
                            Convert.ToString(reader["Name"]),
                            type,
                            Convert.ToBoolean(reader["IsReadOnly"]),
                            Convert.ToBoolean(reader["IsOutput"]))
                        { Parent = this };
                        result.Add(parameter);
                    }
                }
            }, parameters);

            return result.AsReadOnly();
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
