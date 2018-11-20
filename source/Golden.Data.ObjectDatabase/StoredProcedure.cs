using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Golden.Data.ObjectDatabase
{
    public class StoredProcedure : DbSchemaObject
    {
        private readonly Lazy<ReadOnlyCollection<View>> _OutputResults;
        private readonly Lazy<ReadOnlyCollection<FunctionParameter>> _Parameters;

        public new Database Parent
        {
            get { return base.Parent as Database; }
            internal set { base.Parent = value; }
        }
        /// <summary>
        /// Stored procedures always returns 'int' value.
        /// </summary>
        public DataType DataType { get; private set; }
        public ReadOnlyCollection<View> OutputResults { get { return _OutputResults.Value; } }
        public ReadOnlyCollection<FunctionParameter> Parameters { get { return _Parameters.Value; } }

        public StoredProcedure()
        {
        }
        public StoredProcedure(string name) : base(name)
        {
        }
        public StoredProcedure(string name, string schema) : base(name, schema)
        {
            this.DataType = new ObjectDatabase.DataType(SqlDbType.Int);
            _OutputResults = new Lazy<ReadOnlyCollection<View>>(LoadOutputResults);
            _Parameters = new Lazy<ReadOnlyCollection<FunctionParameter>>(LoadParameters);
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
        private ReadOnlyCollection<View> LoadOutputResults()
        {
            var parameters = _Parameters.Value;

            var result = new List<IEnumerable<Column>>();

            #region SchemaTables
            var schemaTables = new List<DataTable>();
            var paramsReadOnly = parameters.Select(p => p.IsReadOnly).ToArray();
            var cmdStr = string.Concat(
                "EXECUTE ",
                this.QuoteFullName,
                " ",
                string.Join(",", paramsReadOnly.Select(ro => (ro ? "DEFAULT" : "NULL"))),
                ";");
            try
            {
                this.Parent.ExecuteReader(cmdStr, reader =>
                {
                    DataTable tblSchema = null;
                    do
                    {
                        tblSchema = reader.GetSchemaTable();
                        if (tblSchema != null) schemaTables.Add(tblSchema);

                    } while (reader.NextResult());

                }, behavior: CommandBehavior.SchemaOnly);

                foreach (DataTable tblSchema in schemaTables)
                {
                    var columns = new List<Column>();
                    foreach (DataRow colInfo in tblSchema.Rows)
                    {
                        var dataTypeName = colInfo.Field<string>("DataTypeName");
                        var columnType = new DataType(SqlDbType.Variant);
                        if (!string.IsNullOrEmpty(dataTypeName))
                        {
                            var maxLen = colInfo.Field<int?>("ColumnSize").GetValueOrDefault(0);
                            if (maxLen == int.MaxValue) maxLen = -1;

                            var precision = colInfo.Field<short?>("NumericPrecision").GetValueOrDefault(0);
                            if (precision == 255) precision = 0;

                            var scale = colInfo.Field<short?>("NumericScale").GetValueOrDefault(0);
                            if (scale == 255) scale = 0;

                            columnType = new DataType(dataTypeName, maxLen, precision, scale);
                            var temp = colInfo.Field<string>("BaseSchemaName");
                            if (!string.IsNullOrEmpty(temp)) columnType.Schema = temp;
                        }
                        var column = new Column(colInfo.Field<string>("ColumnName"), columnType);
                        column.IsComputed = (colInfo.Field<bool?>("IsExpression") == false && colInfo.Field<bool?>("IsReadOnly") == true);
                        column.IsIdentity = colInfo.Field<bool?>("IsIdentity").GetValueOrDefault(false);
                        column.IsNullable = colInfo.Field<bool?>("AllowDBNull").GetValueOrDefault(true);

                        columns.Add(column);
                    }
                    result.Add(columns);
                }
            }
            catch { }
            #endregion

            return new ReadOnlyCollection<View>(result.Select((c, i) => new View(this.Name.Append("Result", (i > 0 ? (i + 1).ToString() : "")), c)).ToList());
        }
    }
}
