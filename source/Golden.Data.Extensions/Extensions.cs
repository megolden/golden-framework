namespace System
{
    public static class DataStringExtensions
    {
        public static bool Like(this string str, string pattern)
        {
            throw new NotSupportedException();
        }
    }
}
namespace System.Data
{
    using Golden.Annotations;
    using Golden.Data.Extensions;
    using Linq.Expressions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class DataExtensions
    {
        internal static Delegate CreateSchema(Type type, DataTable table, IEnumerable<PropertyInfo> properties)
        {
            var objExp = Expression.Parameter(type, "obj");
            var propsExps = new List<Expression>();
            foreach (var prop in properties)
            {
                var column = table.Columns.Add(prop.Name, (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
                column.AllowDBNull = Golden.Utility.TypeHelper.CanBeNull(prop.PropertyType);
                column.ReadOnly = ((!prop.CanWrite) || (prop.GetSetMethod() != null));
                Expression propExp =
                    (prop.PropertyType == typeof(object) ?
                    (Expression)Expression.MakeMemberAccess(objExp, prop) :
                    Expression.Convert(Expression.MakeMemberAccess(objExp, prop), typeof(object)));
                if (column.AllowDBNull)
                {
                    propsExps.Add(Expression.Coalesce(propExp, Expression.Convert(Expression.Constant(DBNull.Value, typeof(DBNull)), typeof(object))));
                }
                else
                {
                    propsExps.Add(propExp);
                }
            }
            return Expression.Lambda(Expression.NewArrayInit(typeof(object), propsExps), objExp).Compile();
        }
        private static Func<T, object[]> CreateSchema<T>(DataTable table)
        {
            var type = typeof(T);
            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => (p.GetCustomAttribute<ColumnAttribute>()?.Order).GetValueOrDefault(int.MaxValue))
                .Where(p => !p.IsDefined<IgnoreAttribute>(true));
            return (Func<T, object[]>)CreateSchema(type, table, props);
        }
        internal static Delegate InstanceCreator(Type type, DataTable table, IEnumerable<PropertyInfo> properties)
        {
            var itemArrayExp = Expression.Parameter(typeof(object[]), "itemArray");
            var propsBindExps = new List<MemberBinding>();
            Expression exp = null;
            var mIsDBNull = new Lazy<MethodInfo>(() => typeof(Convert).GetMethod(nameof(Convert.IsDBNull), BindingFlags.Public | BindingFlags.Static));
            foreach (var prop in properties)
            {
                var column = table.Columns[prop.Name];

                if (column == null) continue;

                if (Golden.Utility.TypeHelper.CanBeNull(prop.PropertyType))
                {
                    //<Field> = (Convert.IsDBNull(itemArray[1]) ? null : (string)itemArray[1])
                    exp = Expression.ArrayIndex(itemArrayExp, Expression.Constant(column.Ordinal, typeof(int)));
                    exp = Expression.Condition(
                        Expression.Call(mIsDBNull.Value, exp),
                        Expression.Default(prop.PropertyType),
                        Expression.Convert(exp, prop.PropertyType));
                }
                else
                {
                    //Id = (int)itemArray[0]
                    exp = Expression.ArrayIndex(itemArrayExp, Expression.Constant(column.Ordinal, typeof(int)));
                    exp = Expression.Convert(exp, prop.PropertyType);
                }
                propsBindExps.Add(Expression.Bind(prop, exp));
            }
            return Expression.Lambda(Expression.MemberInit(Expression.New(type), propsBindExps), itemArrayExp).Compile();
        }
        private static Func<object[], T> InstanceCreator<T>(DataTable table)
        {
            var type = typeof(T);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => ((!p.CanWrite) || (p.GetSetMethod() == null)) || p.GetIndexParameters().Length > 0);
            return (Func<object[], T>)InstanceCreator(type, table, props);
        }
        internal static DataTable ToDataTable(this Collections.IEnumerable collection, Type elementType = null, Delegate instanceCreator = null, DataTable table = null)
        {
            if (elementType == null) elementType = Golden.Utility.TypeHelper.GetElementType(collection.GetType());
            if (table == null) table = new DataTable(elementType.NonGenericName());
            if (instanceCreator == null)
            {
                var mCreateSchema =
                    typeof(DataExtensions).GetMember(nameof(DataExtensions.CreateSchema), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .OfType<MethodInfo>().FirstOrDefault(m => m.GetParameters().Length == 1)
                    .MakeGenericMethod(elementType);
                instanceCreator = (Delegate)mCreateSchema.Invoke(null, new object[] { table });
            }
            table.BeginLoadData();
            foreach (var item in collection)
            {
                var row = table.NewRow();
                row.ItemArray = (object[])instanceCreator.DynamicInvoke(item);
                table.Rows.Add(row);
            }
            table.EndLoadData();

            return table;
        }
        public static DataTable ToDataTable(this Collections.IEnumerable collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            return ToDataTable(collection, (Type)null, (Delegate)null, (DataTable)null);
        }
        public static DataTable ToDataTable<T>(this IEnumerable<T> collection)
        {
            return ((Collections.IEnumerable)collection).ToDataTable(elementType: typeof(T));
        }
        public static IEnumerable<T> ToEnumerable<T>(this DataTable table) where T : new()
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (table.Rows.Count == 0)
                yield break;

            var creator = InstanceCreator<T>(table);
            foreach (var item in table.AsEnumerable())
            {
                yield return creator.Invoke(item.ItemArray);
            }
            yield break;
        }
    }
}
namespace System.Data.Entity
{
    using System;
    using Golden.Data.Extensions;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Diagnostics;
    using System.Text;
    using System.ComponentModel.DataAnnotations.Schema;
    using Linq.Expressions;
    using Golden.Utility;
    using Common;
    using SqlClient;
    using Golden.GoldenExtensions;

    public static class DbModelExtensions
    {
        public static void AddFunctions<T>(this DbModelBuilder modelBuilder, string defaultSchema = null)
        {
            AddFunctions(modelBuilder, typeof(T), defaultSchema);
        }
        public static void AddFunctions(this DbModelBuilder modelBuilder, Type methodClassType, string defaultSchema = null)
        {
            modelBuilder.Conventions.Add(new FunctionConvention(defaultSchema, methodClassType));
        }
        public static void AddComplexTypes(this DbModelBuilder modelBuilder, Assembly assembly)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var mComplexType = typeof(DbModelBuilder).GetMethod(nameof(DbModelBuilder.ComplexType), BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var type in assembly.GetExportedTypes().Where(type => type.IsDefined(typeof(ComplexTypeAttribute), false)))
            {
                mComplexType.MakeGenericMethod(type).Invoke(modelBuilder, null);
            }
        }
        public static void AddComplexTypes(this DbModelBuilder modelBuilder, params Type[] types)
        {
            if (types == null || types.Length == 0) return;
            var mComplexType = typeof(DbModelBuilder).GetMethod(nameof(DbModelBuilder.ComplexType), BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var type in types)
            {
                mComplexType.MakeGenericMethod(type).Invoke(modelBuilder, null);
            }
        }
        public static void SetEntitySchema(this DbModelBuilder modelBuilder, string schema, params Type[] entityTypes)
        {
            if (entityTypes == null || entityTypes.Length == 0) return;
            modelBuilder.Types().Where(type => entityTypes.Contains(type)).Configure(config =>
            {
                var tableAttrib = config.ClrType.GetCustomAttribute<TableAttribute>();
                var tableName = (tableAttrib != null && tableAttrib.Name != null ? tableAttrib.Name : config.ClrType.Name);
                config.ToTable(tableName, schema);
            });
        }
    }
    public static class DbContextExtensions
    {
        #region Fields
        private static readonly Lazy<MethodInfo> mTranslate = new Lazy<MethodInfo>(() =>
        {
            return typeof(ObjectContext)
                .GetMember(nameof(Core.Objects.ObjectContext.Translate), BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.IsGenericMethod && mi.GetParameters().Length == 1);
        });
        private static readonly Lazy<MethodInfo> mTranslateEntity = new Lazy<MethodInfo>(() =>
        {
            return typeof(ObjectContext)
                .GetMember(nameof(Core.Objects.ObjectContext.Translate), BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.IsGenericMethod && mi.GetParameters().Length == 3);
        });
        private static readonly Lazy<MethodInfo> mEnumerableToList = new Lazy<MethodInfo>(() =>
        {
            return typeof(Enumerable)
                .GetMember(nameof(Enumerable.ToList), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi => mi.GetParameters().Length == 1);
        });
        #endregion

        public static DbContext DbContext(this Database db)
        {
            var iContext = TypeHelper.GetMemberValue("_internalContext", db);
            return (TypeHelper.GetMemberValue("Owner", iContext) as DbContext);
        }
        public static ObjectContext ObjectContext(this Database db)
        {
            var iContext = TypeHelper.GetMemberValue("_internalContext", db);
            return (TypeHelper.GetMemberValue("ObjectContext", iContext) as ObjectContext);
        }
        public static DataTable ExecuteToDataTable(this Database db, string cmdText, params object[] parameters)
        {
            var cmd = db.Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            if (db.CurrentTransaction != null)
                cmd.Transaction = db.CurrentTransaction.UnderlyingTransaction;
            var cmdParams = parameters.Select((pi, i) =>
            {
                if (pi is DbParameter)
                    return (DbParameter)pi;
                else
                    return new SqlClient.SqlParameter("@_".Append(i.ToString()), (parameters[i] ?? DBNull.Value));
            }).ToList();
            if (parameters.Length > 0 && !(parameters[0] is DbParameter))
            {
                cmdText = string.Format(cmdText, cmdParams.Select(dbp => (object)dbp.ParameterName).ToArray());
            }
            cmd.CommandText = cmdText;
            cmdParams.ForEach(p => cmd.Parameters.Add(p));

            var table = new DataTable();
            using (var adapter = new SqlClient.SqlDataAdapter((SqlClient.SqlCommand)cmd))
            {
                adapter.Fill(table);
            }
            return table;
        }
        public static DataSet ExecuteToDataSet(this Database db, string cmdText, params object[] parameters)
        {
            var cmd = db.Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            if (db.CurrentTransaction != null)
                cmd.Transaction = db.CurrentTransaction.UnderlyingTransaction;
            var cmdParams = parameters.Select((pi, i) =>
            {
                if (pi is DbParameter)
                    return (DbParameter)pi;
                else
                    return new SqlClient.SqlParameter("@_".Append(i.ToString()), (parameters[i] ?? DBNull.Value));
            }).ToList();
            if (parameters.Length > 0 && !(parameters[0] is DbParameter))
            {
                cmdText = string.Format(cmdText, cmdParams.Select(dbp => (object)dbp.ParameterName).ToArray());
            }
            cmd.CommandText = cmdText;
            cmdParams.ForEach(p => cmd.Parameters.Add(p));

            var dataSet = new DataSet();
            using (var adapter = new SqlClient.SqlDataAdapter((SqlClient.SqlCommand)cmd))
            {
                adapter.Fill(dataSet);
            }
            return dataSet;
        }
        public static ObjectContext ObjectContext(this DbContext context)
        {
            return (context as IObjectContextAdapter).ObjectContext;
        }
        public static int SaveDbChanges(this DbContext context)
        {
            return context.ObjectContext().SaveChanges(SaveOptions.None);
        }
        public static void AcceptAllChanges(this DbContext context)
        {
            context.ObjectContext().AcceptAllChanges();
        }
        public static TEntity Delete<TEntity>(this DbSet<TEntity> set, params object[] keyValues) where TEntity : class
        {
            if (keyValues == null) throw new ArgumentNullException(nameof(keyValues));
            if (keyValues.Length == 0) throw new InvalidOperationException($"Entity key(s) is not specified.");

            var entityType = typeof(TEntity);
            var context = set.ToObjectQuery().Context;
            var entity = set.Create();
            var entityMap = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context);
            entityMap.PropertyMaps.Where(p => p.IsKey).ToList().ForEach((pm, i) =>
            {
                TypeHelper.SetMemberValue(pm.PropertyName, keyValues[i], entity);
            });
            set.Attach(entity);
            context.DeleteObject(entity);

            return entity;
        }
        public static TEntity Update<TEntity, TUpdateExpression>(this DbSet<TEntity> set, Expression<Func<TUpdateExpression>> updateExpression) where TEntity : class
        {
            var entityType = typeof(TEntity);
            var objectQuery = set.ToObjectQuery();
            var entityMap = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, objectQuery.Context);
            var updateParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var exp = updateExpression.Body.GetQuoted();
            if (exp is NewExpression)
            {
                var nexp = exp as NewExpression;
                nexp.Arguments.ForEach((arg, i) =>
                {
                    var value = (
                        arg.NodeType == ExpressionType.Constant ?
                        (arg as ConstantExpression).Value :
                        Expression.Lambda(arg).Compile().DynamicInvoke());
                    updateParams.Add(nexp.Members[i].Name, value);
                });
            }
            else if (exp is MemberInitExpression)
            {
                var miexp = exp as MemberInitExpression;

                miexp.Bindings.OfType<MemberAssignment>().ForEach(assign =>
                {
                    var value = (
                        assign.Expression.NodeType == ExpressionType.Constant ?
                        (assign.Expression as ConstantExpression).Value :
                        Expression.Lambda(assign.Expression).Compile().DynamicInvoke());
                    updateParams.Add(assign.Member.Name, value);
                });
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(updateExpression), $"Invalid update expression type, valid types is {typeof(NewExpression).Name} and {typeof(MemberInitExpression).Name}.");
            }

            if (updateParams.Count == 0) return null;

            var entity = set.Create();
            entityMap.PropertyMaps.Where(p => p.IsKey).ToList().ForEach(pm =>
            {
                TypeHelper.SetMemberValue(pm.PropertyName, updateParams[pm.PropertyName], entity);
            });
            set.Attach(entity);
            updateParams.ForEach(up =>
            {
                TypeHelper.SetMemberValue(up.Key, up.Value, entity);
            });

            return entity;
        }
        public static void LockTable<TEntity>(this DbContext context, bool exclusiveLock = true, bool holdLock = true) where TEntity : class
        {
            context.LockTable(typeof(TEntity), exclusiveLock, holdLock);
        }
        public static int LockTable(this DbContext context, Type entityType, bool exclusiveLock = true, bool holdLock = true)
        {
            var entityMap = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context.ObjectContext());
            return context.LockTable(MetadataMappingProvider.QuoteIdentifier(entityMap.TableName), exclusiveLock, holdLock);
        }
        public static int LockTable(this DbContext context, string tableName, bool exclusiveLock = true, bool holdLock = true)
        {
            var lockExpr = (exclusiveLock ? "TABLOCKX" : "TABLOCK");
            if (holdLock) lockExpr = lockExpr.Append(", HOLDLOCK");
            var cmdStr = string.Format(
                "SELECT TOP(0) NULL FROM {0} WITH ({1});",
                tableName,
                lockExpr);

            return context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, cmdStr);
        }
        public static string GetTableName<TEntity>(this DbContext context) where TEntity : class
        {
            return context.GetTableName(typeof(TEntity));
        }
        public static string GetTableName(this DbContext context, Type entityType)
        {
            return MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context.ObjectContext()).TableName;
        }
        private static Type GetRealParameterType(ParameterInfo parameter, bool nonNullable = true)
        {
            var type = parameter.ParameterType;
            if (type.IsByRef) type = type.GetElementType();
            if (nonNullable) type = TypeHelper.GetNonNullableType(type);
            return type;
        }
        private static SqlDbType GetSqlDbType(Type clrType)
        {
            if (clrType == typeof(long))
                return SqlDbType.BigInt;
            else if (clrType == typeof(byte))
                return SqlDbType.TinyInt;
            else if (clrType == typeof(byte[]))
                return SqlDbType.VarBinary;
            else if (clrType == typeof(bool))
                return SqlDbType.Bit;
            else if (clrType == typeof(char))
                return SqlDbType.NVarChar;
            else if (clrType == typeof(string))
                return SqlDbType.NVarChar;
            else if (clrType == typeof(DateTime))
                return SqlDbType.DateTime;
            else if (clrType == typeof(decimal))
                return SqlDbType.Decimal;
            else if (clrType == typeof(double))
                return SqlDbType.Float;
            else if (clrType == typeof(int))
                return SqlDbType.Int;
            else if (clrType == typeof(float))
                return SqlDbType.Real;
            else if (clrType == typeof(short))
                return SqlDbType.SmallInt;
            else if (clrType == typeof(Guid))
                return SqlDbType.UniqueIdentifier;
            else if (clrType == typeof(TimeSpan))
                return SqlDbType.Time;
            else if (clrType == typeof(DateTimeOffset))
                return SqlDbType.DateTimeOffset;
            return SqlDbType.Variant;
        }
        private static ObjectParameter GetObjParameter(ParameterInfo parameter, object value)
        {
            var pType = GetRealParameterType(parameter);
            if (value == null)
                return new ObjectParameter(parameter.Name, pType);
            if (Type.GetTypeCode(pType) == TypeCode.Object && pType.IsArray)
                value = ((Collections.IEnumerable)value).ToDataTable();
            return new ObjectParameter(parameter.Name, value);
        }
        private static DbParameter GetDbParameter(ParameterInfo parameter, object value)
        {
            if (value is DbParameter)
                return ((DbParameter)value);

            var pType = GetRealParameterType(parameter);

            var name = "@".Append((parameter.GetCustomAttribute<ParameterAttribute>()?.Name ?? parameter.Name));
            var dir = (parameter.ParameterType.IsByRef ? ParameterDirection.InputOutput : (parameter.IsOut ? ParameterDirection.Output : ParameterDirection.Input));
            SqlClient.SqlParameter param = null;
            if (pType.IsArray && pType != typeof(string) && pType != typeof(byte[]))
            {
                var elemType = pType.GetElementType();
                param = new SqlClient.SqlParameter(name, ((Collections.IEnumerable)value).ToDataTable());
                param.SqlDbType = SqlDbType.Structured;
                var udtAttrib = elemType.GetCustomAttribute<UserDefinedTypeAttribute>(true);
                if (udtAttrib != null)
                {
                    param.TypeName = (udtAttrib.Schema.IsNullOrEmpty() ? (udtAttrib.Name ?? elemType.Name) : udtAttrib.Schema.Append(".", udtAttrib.Name));
                }
                else
                {
                    param.TypeName = elemType.Name;
                }
            }
            else
            {
                if (value != null)
                    param = new SqlClient.SqlParameter(name, value);
                else
                    param = new SqlClient.SqlParameter(name, GetSqlDbType(pType)) { Value = DBNull.Value };
                var udtAttrib = pType.GetCustomAttribute<UserDefinedTypeAttribute>(true);
                if (udtAttrib != null)
                {
                    param.TypeName = (udtAttrib.Schema.IsNullOrEmpty() ? (udtAttrib.Name ?? pType.Name) : udtAttrib.Schema.Append(".", udtAttrib.Name));
                }
            }
            param.Direction = dir;

            //Can be more advanced...!
            if (dir != ParameterDirection.Input && pType == typeof(string)) param.Size = -1;

            return param;
        }
        public static IMultipleResult ExecuteMultipleResult(this Database db, string cmdText, Type[] resultTypes, params object[] parameters)
        {
            var results = new List<Collections.IList>();

            var objContext = db.ObjectContext();
            // If using Code First we need to make sure the model is built before we open the connection 
            // This isn't required for models created with the EF Designer 
            db.Initialize(false);

            var cmd = db.Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            if (db.CurrentTransaction != null)
                cmd.Transaction = db.CurrentTransaction.UnderlyingTransaction;
            var cmdParams = parameters.Select((pi, i) =>
            {
                if (pi is DbParameter)
                    return (DbParameter)pi;
                else
                    return new SqlClient.SqlParameter("@_".Append(i.ToString()), (parameters[i] ?? DBNull.Value));
            }).ToList();
            if (parameters.Length > 0 && !(parameters[0] is DbParameter))
            {
                cmdText = string.Format(cmdText, cmdParams.Select(dbp => (object)dbp.ParameterName).ToArray());
            }
            cmd.CommandText = cmdText;
            cmdParams.ForEach(p => cmd.Parameters.Add(p));

            bool prevConn = (db.Connection.State != ConnectionState.Closed);
            try
            {
                if (!prevConn) db.Connection.Open();

                if (resultTypes.Length > 0)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        int resultTypeIndex = 0;
                        Collections.IList currResult = null;

                        do
                        {
                            if (resultTypeIndex >= resultTypes.Length) break;

                            var resultType = resultTypes[resultTypeIndex];
                            if (reader.HasRows)
                            {
                                var objRet = mTranslate.Value.MakeGenericMethod(resultType).Invoke(objContext, new object[] { reader });
                                currResult = (Collections.IList)mEnumerableToList.Value.MakeGenericMethod(resultType).Invoke(null, new object[] { objRet });
                            }
                            else
                            {
                                currResult = (Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType));
                            }
                            results.Add(currResult);

                            resultTypeIndex++;

                        } while (reader.NextResult());

                        if (!reader.IsClosed) reader.Close();
                    }
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (!prevConn) db.Connection.Close();
            }

            return new MultipleResult(results);
        }
        public static T ExecuteNiladicFunction<T>(this DbContext context)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            var functionAttrib = callingMethod.GetCustomAttribute<FunctionAttribute>();

            var strCmd = string.Format("SELECT {0}", functionAttrib.FunctionName);

            return context.Database.SqlQuery<T>(strCmd).SingleOrDefault();
        }
        public static T ExecuteBuiltInFunction<T>(this DbContext context, params object[] parameters)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            var functionAttrib = callingMethod.GetCustomAttribute<FunctionAttribute>();

            var dbParams = callingMethod.GetParameters().Select((pi, i) => GetDbParameter(pi, parameters[i])).ToArray();
            var strCmd = string.Format(
                "SELECT {0}({1})",
                functionAttrib.FunctionName,
                string.Join(", ", dbParams.Select(p => p.ParameterName)));

            return context.Database.SqlQuery<T>(strCmd, dbParams.Cast<object>().ToArray()).SingleOrDefault();
        }
        public static T ExecuteScalarFunction<T>(this DbContext context, params object[] parameters)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            var functionAttrib = callingMethod.GetCustomAttribute<FunctionAttribute>();

            var strCmd = new StringBuilder("SELECT ");
            if (functionAttrib.Schema != null) strCmd.AppendFormat("{0}.", MetadataMappingProvider.QuoteIdentifier(functionAttrib.Schema));
            var dbParams = callingMethod.GetParameters().Select((pi, i) => GetDbParameter(pi, parameters[i])).ToArray();
            strCmd.AppendFormat(
                "{0}({1})",
                MetadataMappingProvider.QuoteIdentifier(functionAttrib.FunctionName),
                string.Join(", ", dbParams.Select(p => p.ParameterName)));

            return context.Database.SqlQuery<T>(strCmd.ToString(), dbParams.Cast<object>().ToArray()).SingleOrDefault();
        }
        public static IQueryable<T> ExecuteTableValuedFunction<T>(this DbContext context, params object[] parameters)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            var methodParams = callingMethod.GetParameters();
            var functionAttrib = callingMethod.GetCustomAttribute<FunctionAttribute>();

            var objParams = new List<ObjectParameter>();
            var strCmd = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = GetObjParameter(methodParams[i], parameters[i]);
                if (objParams.Count > 0) strCmd.Append(", ");
                objParams.Add(p);
                strCmd.AppendFormat("@{0}", p.Name);
            }

            return context.ObjectContext().CreateQuery<T>(
                string.Format("{0}.{1}({2})",
                MetadataMappingProvider.QuoteIdentifier(functionAttrib.NamespaceName),
                MetadataMappingProvider.QuoteIdentifier(functionAttrib.FunctionName),
                strCmd.ToString()),
                objParams.ToArray());
        }
        public static IEnumerable<T> ExecuteTableValuedFunctionResult<T>(this DbContext context, params object[] parameters)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            var functionAttrib = callingMethod.GetCustomAttribute<FunctionAttribute>();

            var strCmd = new StringBuilder("SELECT ");
            strCmd.Append(string.Join(", ", typeof(T).GetProperties().Where(pi => pi.CanWrite && !pi.IsDefined<Golden.Annotations.IgnoreAttribute>()).Select(pi => MetadataMappingProvider.QuoteIdentifier(pi.Name))));
            strCmd.Append(" FROM ");
            if (functionAttrib.Schema != null) strCmd.AppendFormat("{0}.", MetadataMappingProvider.QuoteIdentifier(functionAttrib.Schema));
            var dbParams = callingMethod.GetParameters().Select((pi, i) => GetDbParameter(pi, parameters[i])).ToArray();
            strCmd.AppendFormat(
                "{0}({1})",
                MetadataMappingProvider.QuoteIdentifier(functionAttrib.FunctionName),
                string.Join(", ", dbParams.Select(p => p.ParameterName)));

            return context.Database.SqlQuery<T>(strCmd.ToString(), dbParams.Cast<object>().ToArray()).ToList();
        }
        public static void ExecuteProcedure(this DbContext context, object[] parameters = null)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            InternalExecuteMultipleResultProcedure(context, callingMethod, parameters);
        }
        public static IEnumerable<T> ExecuteProcedure<T>(this DbContext context, object[] parameters = null)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            return InternalExecuteMultipleResultProcedure(context, callingMethod, parameters).GetResult<T>(0);
        }
        public static IMultipleResult ExecuteMultipleResultProcedure(this DbContext context, object[] parameters = null)
        {
            var callingMethod = (new StackFrame(1, false)).GetMethod();
            return InternalExecuteMultipleResultProcedure(context, callingMethod, parameters);
        }
        private static IMultipleResult InternalExecuteMultipleResultProcedure(DbContext context, MethodBase functionMethod, object[] parameters)
        {
            var methodParams = functionMethod.GetParameters();
            var functionAttrib = functionMethod.GetCustomAttribute<FunctionAttribute>();
            var resultTypes = functionMethod.GetCustomAttribute<ResultTypesAttribute>()?.Types;
            if (resultTypes == null)
            {
                resultTypes = Type.EmptyTypes;
                var mi = functionMethod as MethodInfo;
                if (mi != null && mi.ReturnType != null && mi.ReturnType.IsGenericType && typeof(Collections.IEnumerable).IsAssignableFrom(mi.ReturnType))
                {
                    var genArgs = mi.ReturnType.GetGenericArguments();
                    if (genArgs.Length == 1) resultTypes = genArgs;
                }
            }

            var objContext = context.ObjectContext();
            var results = new List<Collections.IList>();

            // If using Code First we need to make sure the model is built before we open the connection 
            // This isn't required for models created with the EF Designer 
            context.Database.Initialize(false);

            // Create a SQL command to execute the procedure
            var cmd = context.Database.Connection.CreateCommand();
            cmd.CommandText = (
                functionAttrib.Schema != null ?
                string.Format("{0}.{1}", MetadataMappingProvider.QuoteIdentifier(functionAttrib.Schema), MetadataMappingProvider.QuoteIdentifier(functionAttrib.FunctionName)) :
                MetadataMappingProvider.QuoteIdentifier(functionAttrib.FunctionName));
            cmd.CommandType = CommandType.StoredProcedure;
            if (context.Database.CurrentTransaction != null)
                cmd.Transaction = context.Database.CurrentTransaction.UnderlyingTransaction;
            var cmdParams = methodParams.Select((pi, i) => GetDbParameter(pi, parameters[i])).ToList();
            cmdParams.ForEach(p => cmd.Parameters.Add(p));

            bool prevConn = (context.Database.Connection.State != ConnectionState.Closed);
            try
            {
                if (!prevConn) context.Database.Connection.Open();

                if (resultTypes.Length > 0)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        int resultTypeIndex = 0;
                        Collections.IList currResult = null;

                        do
                        {
                            if (resultTypeIndex >= resultTypes.Length) break;

                            var resultType = resultTypes[resultTypeIndex];
                            if (reader.HasRows)
                            {
                                var objRet = mTranslate.Value.MakeGenericMethod(resultType).Invoke(objContext, new object[] { reader });
                                currResult = (Collections.IList)mEnumerableToList.Value.MakeGenericMethod(resultType).Invoke(null, new object[] { objRet });
                            }
                            else
                            {
                                currResult = (Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType));
                            }
                            results.Add(currResult);

                            resultTypeIndex++;

                        } while (reader.NextResult());

                        if (!reader.IsClosed) reader.Close();
                    }
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
                methodParams.ForEach((pi, i) =>
                {
                    if (pi.IsOut || pi.ParameterType.IsByRef)
                    {
                        var pType = (pi.ParameterType.IsByRef ? pi.ParameterType.GetElementType() : pi.ParameterType);
                        parameters[i] = Golden.Utility.Utilities.Convert(pType, cmdParams[i].Value, true);
                    }
                });
            }
            finally
            {
                if (!prevConn) context.Database.Connection.Close();
            }

            return new MultipleResult(results);
        }
        private static ICollection<DataTable> GetInsertData(ObjectContext context, ICollection<DbEntityEntry> entities)
        {
            return entities.GroupBy(e => e.Entity.GetType()).Select(gx =>
            {
                var entityType = gx.Key;
                var tableInfo = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context);
                var table = new DataTable(MetadataMappingProvider.QuoteIdentifier(tableInfo.TableName));
                var props = tableInfo.PropertyMaps.Select(pn => entityType.GetProperty(pn.PropertyName));
                var instanceCreator = DataExtensions.CreateSchema(entityType, table, props);
                DataExtensions.ToDataTable(gx.Select(ge => ge.Entity), entityType, instanceCreator, table);
                return table;
            })
            .ToList();
        }
        private static ICollection<SqlCommand> GetUpdateCommands(ObjectContext context, ICollection<DbEntityEntry> entities, int batchSize = 0)
        {
            if (entities.Count == 0) return new SqlCommand[0];

            const int MaxCommandParameterCount = 2000;

            var cmdList = new List<SqlCommand>();
            var strCmd = new StringBuilder();
            int stmtCount = 0;
            Func<SqlCommand> fnGetNewCommand = () =>
            {
                var cmd = new SqlCommand();
                cmdList.Add(cmd);
                strCmd = new StringBuilder();
                stmtCount = 0;
                return cmd;
            };

            var sqlCmd = fnGetNewCommand.Invoke();
            foreach (var e in entities)
            {
                var entityType = e.Entity.GetType();
                var tableInfo = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context);
                var tableName = MetadataMappingProvider.QuoteIdentifier(tableInfo.TableName);
                var paramList = new List<SqlParameter>();
                var propValues = tableInfo.PropertyMaps.Select(pm => new
                {
                    ColumnName = pm.ColumnName,
                    Property = e.Property(pm.PropertyName),
                    IsKey = pm.IsKey
                })
                .ToList();
                var eCmd = string.Concat("UPDATE ", tableName, " SET ", propValues.Where(x => !x.IsKey && x.Property.IsModified).Select(x =>
                {
                    var sqlParam = new SqlParameter(string.Concat("@p", sqlCmd.Parameters.Count + paramList.Count + 1), x.Property.CurrentValue);
                    paramList.Add(sqlParam);
                    return string.Concat(MetadataMappingProvider.QuoteIdentifier(x.ColumnName), "=", sqlParam.ParameterName);

                }).Join(","), " WHERE ", propValues.Where(x => x.IsKey).Select(x =>
                {
                    var sqlParam = new SqlParameter(string.Concat("@p", sqlCmd.Parameters.Count + paramList.Count + 1), x.Property.CurrentValue);
                    paramList.Add(sqlParam);
                    return string.Concat(MetadataMappingProvider.QuoteIdentifier(x.ColumnName), "=", sqlParam.ParameterName);

                }).Join(" AND "), ";");

                if (sqlCmd.Parameters.Count >= MaxCommandParameterCount || (batchSize > 0 && stmtCount >= batchSize))
                {
                    sqlCmd.CommandText = strCmd.ToString();
                    sqlCmd = fnGetNewCommand.Invoke();
                }
                
                strCmd.Append(eCmd);
                sqlCmd.Parameters.AddRange(paramList.ToArray());
                stmtCount++;
            }

            if (strCmd.Length > 0)
            {
                sqlCmd.CommandText = strCmd.ToString();
            }
            else
            {
                cmdList.Remove(sqlCmd);
            }
            return cmdList;
        }
        private static ICollection<SqlCommand> GetDeleteCommands(ObjectContext context, ICollection<DbEntityEntry> entities, int batchSize = 0)
        {
            if (entities.Count == 0) return new SqlCommand[0];

            const int MaxCommandParameterCount = 2000;

            var cmdList = new List<SqlCommand>();
            var strCmd = new StringBuilder();
            int stmtCount = 0;
            Func<SqlCommand> fnGetNewCommand = () =>
            {
                var cmd = new SqlCommand();
                cmdList.Add(cmd);
                strCmd = new StringBuilder();
                stmtCount = 0;
                return cmd;
            };

            var sqlCmd = fnGetNewCommand();
            foreach (var e in entities)
            {
                var entityType = e.Entity.GetType();
                var tableInfo = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, context);
                var tableName = MetadataMappingProvider.QuoteIdentifier(tableInfo.TableName);
                var paramList = new List<SqlParameter>();
                var propValues = tableInfo.PropertyMaps.Where(pm => pm.IsKey).Select(pm => new
                {
                    ColumnName = pm.ColumnName,
                    Value = e.Property(pm.PropertyName).OriginalValue
                })
                .ToList();

                var eCmd = string.Concat("DELETE FROM ", tableName, " WHERE ", propValues.Select(x =>
                {
                    var sqlParam = new SqlParameter(string.Concat("@p", sqlCmd.Parameters.Count + paramList.Count + 1), x.Value);
                    paramList.Add(sqlParam);
                    return string.Concat(MetadataMappingProvider.QuoteIdentifier(x.ColumnName), "=", sqlParam.ParameterName);

                }).Join(" AND "), ";");

                if (sqlCmd.Parameters.Count >= MaxCommandParameterCount || (batchSize > 0 && stmtCount >= batchSize))
                {
                    sqlCmd.CommandText = strCmd.ToString();
                    sqlCmd = fnGetNewCommand.Invoke();
                }

                strCmd.Append(eCmd);
                sqlCmd.Parameters.AddRange(paramList.ToArray());
                stmtCount++;
            }
            
            if (strCmd.Length > 0)
            {
                sqlCmd.CommandText = strCmd.ToString();
            }
            else
            {
                cmdList.Remove(sqlCmd);
            }
            return cmdList;
        }
        public static void BulkSaveChanges(this DbContext context)
        {
            context.BulkSaveChanges(0);
        }
        public static void BulkSaveChanges(this DbContext context, int batchSize)
        {
            if (!context.ChangeTracker.HasChanges()) return;

            var addedEntities = new List<DbEntityEntry>();
            var modifiedEntities = new List<DbEntityEntry>();
            var deletedEntities = new List<DbEntityEntry>();
            context.ChangeTracker.Entries().ForEach(e =>
            {
                if (e.State.HasFlag(EntityState.Added)) addedEntities.Add(e);
                if (e.State.HasFlag(EntityState.Modified)) modifiedEntities.Add(e);
                if (e.State.HasFlag(EntityState.Deleted)) deletedEntities.Add(e);
            });
            var addedTables = GetInsertData(context.ObjectContext(), addedEntities);
            var updateCmds = GetUpdateCommands(context.ObjectContext(), modifiedEntities, batchSize);
            var deleteCmds = GetDeleteCommands(context.ObjectContext(), deletedEntities, batchSize);

            bool prevConn = (context.Database.Connection.State != ConnectionState.Closed);
            var prevTrans = (context.Database.CurrentTransaction != null);
            SqlTransaction trans = null;
            try
            {
                if (!prevConn) context.Database.Connection.Open();
                trans = (prevTrans ? (SqlTransaction)context.Database.CurrentTransaction.UnderlyingTransaction : context.Database.BeginTransaction().UnderlyingTransaction.As<SqlTransaction>());
                #region AddedEntities
                addedTables.ForEach(t =>
                {
                    var bulkCopy = new SqlBulkCopy(
                        context.Database.Connection.As<SqlConnection>(),
                        SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers,
                        trans);
                    bulkCopy.DestinationTableName = t.TableName;
                    t.Columns.OfType<DataColumn>().ForEach(c => bulkCopy.ColumnMappings.Add(c.ColumnName, c.ColumnName));
                    if (batchSize > 0) bulkCopy.BatchSize = batchSize;
                    bulkCopy.WriteToServer(t);
                });
                #endregion
                #region ModifiedEntities
                updateCmds.ForEach(cmd =>
                {
                    cmd.Connection = (SqlConnection)context.Database.Connection;
                    cmd.Transaction = trans;
                    cmd.ExecuteNonQuery();
                });
                #endregion
                #region DeletedEntities
                deleteCmds.ForEach(cmd =>
                {
                    cmd.Connection = (SqlConnection)context.Database.Connection;
                    cmd.Transaction = trans;
                    cmd.ExecuteNonQuery();
                });
                #endregion
                if (!prevTrans)
                {
                    trans.Commit();
                    trans.Dispose();
                }
                if (!prevConn) context.Database.Connection.Close();
            }
            catch
            {
                if (!prevTrans && trans != null)
                {
                    trans.Rollback();
                    trans.Dispose();
                }
                if (!prevConn) context.Database.Connection.Close();
                throw;
            }
        }
    }
}
namespace System.Data.Entity.ModelConfiguration
{
    using Configuration;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Linq;

    public static class ConfigurationExtensions
    {
        public static EntityTypeConfiguration<TEntityType> HasIdentityKey<TEntityType, TKey>(this EntityTypeConfiguration<TEntityType> configuration, Expression<Func<TEntityType, TKey>> keyExpression) where TEntityType : class where TKey : struct
        {
            configuration.HasKey(keyExpression);
            configuration.Property(keyExpression).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            return configuration;
        }
        private static string GetSqlColumnTypeName(SqlDbType sqlColumnType, bool exceptionOnInvalidType = true)
        {
            switch (sqlColumnType)
            {
                case SqlDbType.Udt:
                case SqlDbType.Structured:
                    if (exceptionOnInvalidType)
                        throw new NotSupportedException($"The type '{sqlColumnType.ToString()}' not supported.");
                    else
                        return null;
            }
            return Enum.GetName(typeof(SqlDbType), sqlColumnType).ToLower();
        }
        public static DateTimePropertyConfiguration HasColumnType(this DateTimePropertyConfiguration configuration, SqlDbType sqlColumnType)
        {
            return configuration.HasColumnType(GetSqlColumnTypeName(sqlColumnType));
        }
        public static DecimalPropertyConfiguration HasColumnType(this DecimalPropertyConfiguration configuration, SqlDbType sqlColumnType)
        {
            return configuration.HasColumnType(GetSqlColumnTypeName(sqlColumnType));
        }
        public static BinaryPropertyConfiguration HasColumnType(this BinaryPropertyConfiguration configuration, SqlDbType sqlColumnType)
        {
            return configuration.HasColumnType(GetSqlColumnTypeName(sqlColumnType));
        }
        public static StringPropertyConfiguration HasColumnType(this StringPropertyConfiguration configuration, SqlDbType sqlColumnType)
        {
            return configuration.HasColumnType(GetSqlColumnTypeName(sqlColumnType));
        }
        public static PrimitivePropertyConfiguration HasColumnType(this PrimitivePropertyConfiguration configuration, SqlDbType sqlColumnType)
        {
            return configuration.HasColumnType(GetSqlColumnTypeName(sqlColumnType));
        }
        public static void RegisterFromAssembly(this ConfigurationRegistrar configuration, Assembly assembly)
        {
            var configTypes = assembly.GetTypes()
                .Select(type =>
                {
                    Type elementType = null;
                    var configType = "Unknown";
                    var baseType = type.BaseType;
                    while (baseType != null && baseType != typeof(object))
                    {
                        if (baseType.IsGenericType)
                        {
                            var genType1 = baseType.GetGenericArguments().FirstOrDefault();
                            if (typeof(EntityTypeConfiguration<>).MakeGenericType(genType1) == baseType)
                            {
                                configType = "Entity";
                                elementType = genType1;
                                break;
                            }
                            else if (typeof(ComplexTypeConfiguration<>).MakeGenericType(genType1) == baseType)
                            {
                                configType = "ComplexType";
                                elementType = genType1;
                                break;
                            }
                        }
                        baseType = baseType.BaseType;
                    }
                    return new { ConfigType = configType, Type = type, ElementType = elementType };
                })
                .ToList();

            if (configTypes.Count == 0) return;

            var mAddEntity = typeof(ConfigurationRegistrar).GetMember("Add", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    if (!mi.IsGenericMethod && mi.GetGenericArguments().Length != 1) return false;
                    var param1Type = mi.GetParameters().FirstOrDefault()?.ParameterType;
                    if (param1Type == null) return false;
                    return (param1Type.IsGenericType && (string.Equals(param1Type.NonGenericName(), typeof(EntityTypeConfiguration<>).NonGenericName(), StringComparison.OrdinalIgnoreCase)));
                });
            var mAddComplexType = typeof(ConfigurationRegistrar).GetMember("Add", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public)
                .OfType<MethodInfo>()
                .FirstOrDefault(mi =>
                {
                    if (!mi.IsGenericMethod && mi.GetGenericArguments().Length != 1) return false;
                    var param1Type = mi.GetParameters().FirstOrDefault()?.ParameterType;
                    if (param1Type == null) return false;
                    return (param1Type.IsGenericType && (string.Equals(param1Type.NonGenericName(), typeof(ComplexTypeConfiguration<>).NonGenericName(), StringComparison.OrdinalIgnoreCase)));
                });
            configTypes.ForEach(tc =>
            {
                if ("Entity".EqualsOrdinal(tc.ConfigType, true))
                    mAddEntity.MakeGenericMethod(tc.ElementType).Invoke(configuration, new object[] { Activator.CreateInstance(tc.Type) });
                else if ("ComplexType".EqualsOrdinal(tc.ConfigType, true))
                    mAddComplexType.MakeGenericMethod(tc.ElementType).Invoke(configuration, new object[] { Activator.CreateInstance(tc.Type) });
            });
        }
    }
}
namespace Golden.Data.Extensions
{
    using System;
    using System.Data.Entity;
    using System.Text;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Golden;

    public static class ObjectQueryableExtensions
    {
        private class DataObjectQueryableVisitor : ExpressionVisitor
        {
            private static Lazy<MethodInfo> mLeft = new Lazy<MethodInfo>(() =>
            {
                return typeof(DbFunctions).GetMethod(nameof(DbFunctions.Left), BindingFlags.Public | BindingFlags.Static);
            });
            private static Lazy<MethodInfo> mRight = new Lazy<MethodInfo>(() =>
            {
                return typeof(DbFunctions).GetMethod(nameof(DbFunctions.Right), BindingFlags.Public | BindingFlags.Static);
            });
            private static Lazy<MethodInfo> mReverse = new Lazy<MethodInfo>(() =>
            {
                return typeof(DbFunctions).GetMethod(nameof(DbFunctions.Reverse), BindingFlags.Public | BindingFlags.Static);
            });
            private static Lazy<MethodInfo> mPatIndex = new Lazy<MethodInfo>(() =>
            {
                return typeof(System.Data.Entity.SqlServer.SqlFunctions).GetMethod(nameof(System.Data.Entity.SqlServer.SqlFunctions.PatIndex));
            });
            private static Lazy<MethodInfo> mDTTruncateTime = new Lazy<MethodInfo>(() =>
            {
                return typeof(DbFunctions).GetMethod(nameof(DbFunctions.TruncateTime), new Type[] { typeof(DateTime?) }, null);
            });
            private static Lazy<MethodInfo> mDTOTruncateTime = new Lazy<MethodInfo>(() =>
            {
                return typeof(DbFunctions).GetMethod(nameof(DbFunctions.TruncateTime), new Type[] { typeof(DateTimeOffset?) }, null);
            });
            private static Lazy<MethodInfo> mContains = new Lazy<MethodInfo>(() =>
            {
                return typeof(Enumerable).GetMember(nameof(Enumerable.Contains), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                    .OfType<MethodInfo>()
                    .FirstOrDefault(mi => mi.GetParameters().Length == 2);
            });
            private static Lazy<MethodInfo> mIsBetween = new Lazy<MethodInfo>(() =>
            {
                return typeof(Utility.Utilities).GetMember(nameof(Utility.Utilities.IsBetween), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                    .OfType<MethodInfo>()
                    .FirstOrDefault(mi => mi.IsGenericMethod && mi.GetParameters().Length == 3);
            });
            private static Lazy<MethodInfo> mIsIn = new Lazy<MethodInfo>(() =>
            {
                return typeof(Utility.Utilities).GetMember(nameof(Utility.Utilities.IsIn), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                    .OfType<MethodInfo>()
                    .FirstOrDefault(mi => mi.IsGenericMethod && mi.GetParameters().Length == 2);
            });
            private static Lazy<MethodInfo> mHasFlag = new Lazy<MethodInfo>(() =>
            {
                return typeof(Utility.Utilities).GetMember(nameof(Utility.Utilities.HasFlag), BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod)
                    .OfType<MethodInfo>()
                    .FirstOrDefault(mi => mi.IsGenericMethod && mi.GetParameters().Length == 2);
            });
            private readonly Expression source;

            protected override Expression VisitConstant(ConstantExpression node)
            {
                var valueType = node.Value?.GetType();
                if (valueType != null)
                {
                    if (valueType.IsGenericType && valueType == typeof(ObjectQueryable<>).MakeGenericType(valueType.GetGenericArguments()[0]))
                    {
                        return base.Visit(source);
                    }
                }
                return base.VisitConstant(node);
            }
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member.DeclaringType == typeof(DateTime))
                {
                    if (node.Member.Name.EqualsOrdinal(nameof(DateTime.Date)))
                    {
                        var arg1 = Expression.Convert(node.Expression, mDTTruncateTime.Value.GetParameters()[0].ParameterType);
                        var newMethodExp = Expression.Call(mDTTruncateTime.Value, arg1);
                        var retConvertExp = Expression.Convert(newMethodExp, node.Type);
                        return base.Visit(retConvertExp);
                    }
                }
                else if (node.Member.DeclaringType == typeof(DateTimeOffset))
                {
                    if (node.Member.Name.EqualsOrdinal(nameof(DateTimeOffset.Date)))
                    {
                        var arg1 = Expression.Convert(node.Expression, mDTOTruncateTime.Value.GetParameters()[0].ParameterType);
                        var newMethodExp = Expression.Call(mDTOTruncateTime.Value, arg1);
                        var retConvertExp = Expression.Convert(newMethodExp, node.Type);
                        return base.Visit(retConvertExp);
                    }
                }
                return base.VisitMember(node);
            }
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(StringExtensions))
                {
                    if (node.Method.Name.EqualsOrdinal(nameof(StringExtensions.Left)))
                    {
                        var arg2 = Expression.Convert(node.Arguments[1], mLeft.Value.GetParameters()[1].ParameterType);
                        var newMethodExp = Expression.Call(mLeft.Value, node.Arguments[0], arg2);
                        return base.VisitMethodCall(newMethodExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(StringExtensions.Right)))
                    {
                        var arg2 = Expression.Convert(node.Arguments[1], mRight.Value.GetParameters()[1].ParameterType);
                        var newMethodExp = Expression.Call(mRight.Value, node.Arguments[0], arg2);
                        return base.VisitMethodCall(newMethodExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(StringExtensions.Reverse)))
                    {
                        var newMethodExp = Expression.Call(mReverse.Value, node.Arguments[0]);
                        return base.VisitMethodCall(newMethodExp);
                    }
                }
                else if (node.Method.DeclaringType == typeof(DataStringExtensions))
                {
                    if (node.Method.Name.EqualsOrdinal(nameof(DataStringExtensions.Like)))
                    {
                        var newExp = Expression.GreaterThan(
                            Expression.Call(mPatIndex.Value, node.Arguments[1], node.Arguments[0]),
                            Expression.Constant(new int?(0), typeof(int?)));
                        return base.Visit(newExp);
                    }
                }
                else if (node.Method.DeclaringType == typeof(Utility.Utilities))
                {
                    if (node.Method.Name.EqualsOrdinal(nameof(Utility.Utilities.IsBetween)) && node.Method.IsGenericMethod && node.Method.GetParameters().Length == 3)
                    {
                        var newExp = Expression.AndAlso(
                            Expression.GreaterThanOrEqual(node.Arguments[0], node.Arguments[1]),
                            Expression.LessThanOrEqual(node.Arguments[0], node.Arguments[2]));
                        return base.Visit(newExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(Utility.Utilities.IsIn)) && node.Method.IsGenericMethod && node.Method.GetParameters().Length == 2)
                    {
                        var newMethodExp = Expression.Call(mContains.Value.MakeGenericMethod(node.Method.GetGenericArguments()[0]), node.Arguments[1], node.Arguments[0]);
                        return base.VisitMethodCall(newMethodExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(Utility.Utilities.HasFlag)))
                    {
                        var newExp = Expression.Equal(
                            Expression.And(node.Arguments[0], node.Arguments[1]),
                            node.Arguments[1]);
                        return base.Visit(newExp);
                    }
                }
                else if (node.Method.DeclaringType == typeof(GoldenExtensions.GoldenExtensions))
                {
                    if (node.Method.Name.EqualsOrdinal(nameof(GoldenExtensions.GoldenExtensions.IsBetween)))
                    {
                        var newExp = Expression.Call(mIsBetween.Value.MakeGenericMethod(node.Method.GetGenericArguments()[0]), node.Arguments[0], node.Arguments[1], node.Arguments[2]);
                        return base.Visit(newExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(GoldenExtensions.GoldenExtensions.IsIn)) && node.Method.IsGenericMethod && node.Method.GetParameters().Length == 2)
                    {
                        var newExp = Expression.Call(mIsIn.Value.MakeGenericMethod(node.Method.GetGenericArguments()[0]), node.Arguments[0], node.Arguments[1]);
                        return base.Visit(newExp);
                    }
                    else if (node.Method.Name.EqualsOrdinal(nameof(GoldenExtensions.GoldenExtensions.HasFlag)) && !node.Method.IsGenericMethod && node.Method.GetParameters().Length == 2)
                    {
                        var newExp = Expression.Call(mHasFlag.Value.MakeGenericMethod(node.Arguments[0].Type), node.Arguments[0], node.Arguments[1]);
                        return base.Visit(newExp);
                    }
                }
                return base.VisitMethodCall(node);
            }
            public DataObjectQueryableVisitor(Expression source)
            {
                this.source = source;
            }
        }

        public static IQueryable<TResult> ApplyDataQuery<TSource, TResult>(this IQueryable<TSource> source, IQueryable<TResult> query)
        {
            if (object.ReferenceEquals(source, null))
                throw new ArgumentNullException(nameof(source));

            if (object.ReferenceEquals(query, null))
                return (IQueryable<TResult>)source;

            var pSource = Expression.Parameter(typeof(IQueryable<TSource>), "source");
            var newExp = (new DataObjectQueryableVisitor(pSource)).Visit(query.Expression);
            var iqExp = Expression.Lambda<Func<IQueryable<TSource>, IQueryable<TResult>>>(newExp, pSource);

            return iqExp.Compile().Invoke(source);
        }
    }
}
namespace System.Linq
{
    using Data;
    using Text;
    using Data.Common;
    using Data.Entity.Core.EntityClient;
    using Data.Entity.Core.Objects;
    using Reflection;
    using System.Linq.Expressions;
    using Collections.Generic;
    using Golden.Data.Extensions;

    public static class DataQueryableExtensions
    {
        #region Fields

        private static Lazy<PropertyInfo> _IInternalQueryProperty = new Lazy<PropertyInfo>(() =>
            typeof(ObjectQuery<>).Assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(
                "System.Data.Entity.Internal.Linq.IInternalQueryAdapter", StringComparison.Ordinal))
                .GetProperty("InternalQuery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        private static Lazy<PropertyInfo> _ObjectQueryProperty = new Lazy<PropertyInfo>(() =>
            typeof(ObjectQuery).Assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(
                "System.Data.Entity.Internal.Linq.IInternalQuery", StringComparison.Ordinal))
                .GetProperty("ObjectQuery", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        #endregion

        internal static int ExecuteNonQuery(this ObjectContext objectContext, string commandText, IDictionary<string, object> commandParameters = null)
        {
            DbConnection connection = null;
            DbTransaction transaction = null;
            DbCommand command = null;
            bool ownConnection = false;
            bool ownTransaction = false;

            try
            {
                connection = objectContext.Connection;
                var storeConnection = connection as EntityConnection;
                if (storeConnection != null)
                {
                    connection = storeConnection.StoreConnection;
                    if (storeConnection.CurrentTransaction != null)
                    {
                        transaction = storeConnection.CurrentTransaction.StoreTransaction;
                    }
                }

                if (!connection.State.HasFlag(ConnectionState.Open))
                {
                    connection.Open();
                    ownConnection = true;
                }

                if (transaction == null)
                {
                    transaction = connection.BeginTransaction();
                    ownTransaction = true;
                }

                command = connection.CreateCommand();
                command.Transaction = transaction;
                if (objectContext.CommandTimeout.HasValue)
                    command.CommandTimeout = objectContext.CommandTimeout.Value;

                command.CommandText = commandText;
                if (commandParameters != null && commandParameters.Count > 0)
                {
                    foreach (var param in commandParameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = param.Key;
                        parameter.Value = param.Value;
                        command.Parameters.Add(parameter);
                    }
                }

                int result = command.ExecuteNonQuery();

                if (ownTransaction) transaction.Commit();

                return result;
            }
            finally
            {
                if (command != null) command.Dispose();
                if (ownTransaction && transaction != null) transaction.Dispose();
                if (ownConnection && connection != null) connection.Close();
            }
        }
        internal static ObjectQuery<TEntity> ToObjectQuery<TEntity>(this IQueryable<TEntity> query)
        {
            var internalQuery = _IInternalQueryProperty.Value.GetValue(query, null);
            var objectQuery = _ObjectQueryProperty.Value.GetValue(internalQuery, null);
            return (objectQuery as ObjectQuery<TEntity>);
        }
        internal static ObjectQuery ToObjectQuery(this IQueryable query)
        {
            var internalQuery = _IInternalQueryProperty.Value.GetValue(query, null);
            var objectQuery = _ObjectQueryProperty.Value.GetValue(internalQuery, null);
            return (objectQuery as ObjectQuery);
        }
        public static int DeleteDirectly<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return source.Where(predicate).DeleteDirectly();
        }
        public static int DeleteDirectly<TEntity>(this IQueryable<TEntity> source) where TEntity : class
        {
            var entityType = typeof(TEntity);
            var objectQuery = source.ToObjectQuery<TEntity>();
            var entityMap = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, objectQuery.Context);

            var queryCmdStr = objectQuery.ToTraceString();
            var cmdParams = objectQuery.Parameters.ToDictionary(op => op.Name, op => op.Value, StringComparer.OrdinalIgnoreCase);

            var cmdStr = new StringBuilder();
            cmdStr.AppendFormat("DELETE {0} FROM {0} AS T0 INNER JOIN ({1}) AS T1 ON ", MetadataMappingProvider.QuoteIdentifier(entityMap.TableName), queryCmdStr);
            var keyAdded = false;
            foreach (var keyMap in entityMap.PropertyMaps.Where(p => p.IsKey))
            {
                if (keyAdded) cmdStr.Append(" AND ");
                cmdStr.AppendFormat("T0.{0} = T1.{0}", MetadataMappingProvider.QuoteIdentifier(keyMap.ColumnName));
                keyAdded = true;
            }

            return objectQuery.Context.ExecuteNonQuery(cmdStr.ToString(), cmdParams);
        }
        //public static int Update<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity>> updateExpression)
        //{
        //	return source.Update<TEntity, TEntity>(updateExpression);
        //}
        public static int UpdateDirectly<TEntity, TUpdateExpression>(this IQueryable<TEntity> source, Expression<Func<TUpdateExpression>> updateExpression)
        {
            var entityType = typeof(TEntity);
            var objectQuery = source.ToObjectQuery<TEntity>();
            var entityMap = MetadataMappingProvider.DefaultInstance.GetEntityMap(entityType, objectQuery.Context);
            var updateParams = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var exp = updateExpression.Body.GetQuoted();
            if (exp is NewExpression)
            {
                var nexp = exp as NewExpression;
                nexp.Arguments.ForEach((arg, i) =>
                {
                    var value = (
                        arg.NodeType == ExpressionType.Constant ?
                        (arg as ConstantExpression).Value :
                        Expression.Lambda(arg).Compile().DynamicInvoke());
                    updateParams.Add(nexp.Members[i].Name, value);
                });
            }
            else if (exp is MemberInitExpression)
            {
                var miexp = exp as MemberInitExpression;

                miexp.Bindings.OfType<MemberAssignment>().ForEach(assign =>
                {
                    var value = (
                        assign.Expression.NodeType == ExpressionType.Constant ?
                        (assign.Expression as ConstantExpression).Value :
                        Expression.Lambda(assign.Expression).Compile().DynamicInvoke());
                    updateParams.Add(assign.Member.Name, value);
                });
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(updateExpression), $"Invalid update expression type, valid types is {typeof(NewExpression).Name} and {typeof(MemberInitExpression).Name}.");
            }

            if (updateParams.Count == 0) return 0;

            var queryCmdStr = objectQuery.ToTraceString();
            var cmdParams = objectQuery.Parameters.ToDictionary(op => op.Name, op => op.Value, StringComparer.OrdinalIgnoreCase);
            var cmdStr = new StringBuilder();
            #region fnGetNewParamName
            Func<int, string> fnGetNewParamName = startNumber =>
            {
                string name = null;
                do
                {
                    name = string.Concat("@p", startNumber.ToString());
                    startNumber++;

                } while (cmdParams.ContainsKey(name));
                return name;
            };
            #endregion
            cmdStr.AppendFormat("UPDATE {0} SET ", MetadataMappingProvider.QuoteIdentifier(entityMap.TableName));
            updateParams.ForEach((item, i) =>
            {
                if (i > 0) cmdStr.Append(", ");
                cmdStr.AppendFormat("{0} = ", MetadataMappingProvider.QuoteIdentifier(entityMap.PropertyMaps.First(pm => pm.PropertyName.Equals(item.Key, StringComparison.OrdinalIgnoreCase)).ColumnName));
                if (item.Value != null)
                {
                    var paramName = fnGetNewParamName(cmdParams.Count);
                    cmdParams[paramName] = item.Value;
                    cmdStr.Append(paramName);
                }
                else
                {
                    cmdStr.Append("NULL");
                }
            });

            cmdStr.AppendFormat(" FROM {0} AS T0 INNER JOIN ({1}) AS T1 ON ", MetadataMappingProvider.QuoteIdentifier(entityMap.TableName), queryCmdStr);
            var keyAdded = false;
            foreach (var keyMap in entityMap.PropertyMaps.Where(p => p.IsKey))
            {
                if (keyAdded) cmdStr.Append(" AND ");
                cmdStr.AppendFormat("T0.{0} = T1.{0}", MetadataMappingProvider.QuoteIdentifier(keyMap.ColumnName));
                keyAdded = true;
            }

            return objectQuery.Context.ExecuteNonQuery(cmdStr.ToString(), cmdParams);
        }
    }
}
