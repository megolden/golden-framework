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
        private static Func<T, object[]> CreateSchema<T>(DataTable table)
        {
            var type = typeof(T);
            var objExp = Expression.Parameter(type, "obj");
            var propsExps = new List<Expression>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => (p.GetCustomAttribute<ColumnAttribute>()?.Order).GetValueOrDefault(int.MaxValue)))
            {
                if (prop.IsDefined<IgnoreAttribute>(true)) continue;

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
            return Expression.Lambda<Func<T, object[]>>(Expression.NewArrayInit(typeof(object), propsExps), objExp).Compile();
        }
        private static Func<object[], T> InstanceCreator<T>(DataTable table)
        {
            var type = typeof(T);
            var itemArrayExp = Expression.Parameter(typeof(object[]), "itemArray");
            var propsBindExps = new List<MemberBinding>();
            Expression exp = null;
            var mIsDBNull = new Lazy<MethodInfo>(() => typeof(Convert).GetMethod(nameof(Convert.IsDBNull), BindingFlags.Public | BindingFlags.Static));
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (((!prop.CanWrite) || (prop.GetSetMethod() == null)) || prop.GetIndexParameters().Length > 0) continue;

                var column = table.Columns[prop.Name];

                if (column == null) continue;

                if (Golden.Utility.TypeHelper.CanBeNull(prop.PropertyType))
                {
                    //Name = (Convert.IsDBNull(itemArray[1]) ? null : (string)itemArray[1])
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
            return Expression.Lambda<Func<object[], T>>(Expression.MemberInit(Expression.New(type), propsBindExps), itemArrayExp).Compile();
        }
        public static DataTable ToDataTable(this Collections.IEnumerable collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var elementType = Golden.Utility.TypeHelper.GetElementType(collection.GetType());
            var table = new DataTable(elementType.NonGenericName());
            var mCreateSchema =
                typeof(DataExtensions).GetMethod(nameof(DataExtensions.CreateSchema), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(elementType);
            var dataRowExtractor = (Delegate)mCreateSchema.Invoke(null, new object[] { table });
            table.BeginLoadData();
            foreach (var item in collection)
            {
                var row = table.NewRow();
                row.ItemArray = (object[])dataRowExtractor.DynamicInvoke(item);
                table.Rows.Add(row);
            }
            table.EndLoadData();

            return table;
        }
        public static DataTable ToDataTable<T>(this IEnumerable<T> collection)
        {
            return ((Collections.IEnumerable)collection).ToDataTable();
        }
        public static IEnumerable<T> ToEnumerable<T>(this DataTable table) where T : new()
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (table.Rows.Count == 0)
                return Enumerable.Empty<T>();

            var result = new List<T>();
            var creator = InstanceCreator<T>(table);
            foreach (var item in table.AsEnumerable())
            {
                result.Add(creator.Invoke(item.ItemArray));
            }
            return result;
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
        private struct VoidResult
        {
            public readonly object Result;

            public VoidResult(object result)
            {
                this.Result = result;
            }
        }

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

            //var currentResult = new List<object>();
            var results = new List<Collections.IList>();// { currentResult };

            // If using Code First we need to make sure the model is built before we open the connection 
            // This isn't required for models created with the EF Designer 
            context.Database.Initialize(false);

            // Create a SQL command to execute the sproc 
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
                                var objRet = mTranslate.Value.MakeGenericMethod(resultType).Invoke(context.ObjectContext(), new object[] { reader });
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
    }
}
namespace System.Data.Entity.ModelConfiguration
{
    using Configuration;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq.Expressions;

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

    public static class DbContextUtilities
    {
        /// <summary>
        /// Creates an instance of <see cref="DbContext"/> class with SQL Server connection string (Integrated Security = SSPI).
        /// </summary>
        /// <typeparam name="T">Type of DbContext</typeparam>
        [Obsolete]
        public static T Create<T>(string serverAddress, string databaseName, string applicationName = null) where T : DbContext
        {
            return Create<T>(serverAddress, databaseName, null, null, applicationName);
        }
        /// <summary>
        /// Creates an instance of <see cref="DbContext"/> class with SQL Server connection string parameters.
        /// </summary>
        /// <typeparam name="T">Type of DbContext</typeparam>
        [Obsolete]
        public static T Create<T>(string serverAddress, string databaseName, string userName, string password, string applicationName = null) where T : DbContext
        {
            var connStr = new StringBuilder();
            connStr.AppendFormat(
                "Data Source={0};Initial Catalog={1};MultipleActiveResultSets=True;",
                serverAddress,
                databaseName);
            if (!string.IsNullOrEmpty(applicationName)) connStr.AppendFormat("Application Name={0};", applicationName);
            if (string.IsNullOrEmpty(userName))
                connStr.Append("Integrated Security=SSPI;");
            else
                connStr.AppendFormat("User ID={0};Password={1};", userName, password);
            return (T)Activator.CreateInstance(typeof(T), new object[] { connStr.ToString() });
        }
    }
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
