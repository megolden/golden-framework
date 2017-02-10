
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
		private static Dictionary<Type, KeyValuePair<PropertyInfo[], Delegate>> _TableValuedTypeCache = new Dictionary<Type, KeyValuePair<PropertyInfo[], Delegate>>();

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
		private static Type GetRealParameterType(ParameterInfo parameter, bool nonNullable = true)
		{
			var type = parameter.ParameterType;
			if (type.IsByRef) type = type.GetElementType();
			if (nonNullable) type = TypeHelper.GetNonNullableType(type);
			return type;
		}
		private static DataTable MakeTableValuedType(Type type, object value)
		{
			KeyValuePair<PropertyInfo[], Delegate> info;
			if (_TableValuedTypeCache.TryGetValue(type, out info))
			{
				var table = new DataTable();
				table.Columns.AddRange(
					info.Key
					.Select(pi => new DataColumn(pi.Name, TypeHelper.GetNonNullableType(pi.PropertyType)) { AllowDBNull = TypeHelper.CanBeNull(pi.PropertyType) })
					.ToArray());
				foreach (var item in ((Collections.IEnumerable)value))
				{
					table.Rows.Add(((object[])info.Value.DynamicInvoke(item)).Select(v => (v ?? DBNull.Value)).ToArray());
				}
				return table;
			}

			var props = type.GetProperties().Where(pi => pi.CanWrite && !pi.IsDefined<Golden.Attributes.IgnoreAttribute>()).ToList();
			var p = Expression.Parameter(type, "obj");
			var initParamExprs = props.Select(pi =>
				(pi.PropertyType != typeof(object) ?
				(Expression)Expression.Convert(Expression.MakeMemberAccess(p, pi), typeof(object)) :
				Expression.MakeMemberAccess(p, pi)))
				.ToList();
			var del = Expression.Lambda(Expression.NewArrayInit(typeof(object), initParamExprs), p).Compile();
			_TableValuedTypeCache[type] = new KeyValuePair<PropertyInfo[], Delegate>(props.ToArray(), del);
			return MakeTableValuedType(type, value);
		}
		private static ObjectParameter GetObjParameter(ParameterInfo parameter, object value)
		{
			var pType = GetRealParameterType(parameter);
			if (value == null)
				return new ObjectParameter(parameter.Name, pType);
			if (Type.GetTypeCode(pType) == TypeCode.Object && pType.IsArray)
				value = MakeTableValuedType(pType.GetElementType(), value);
			return new ObjectParameter(parameter.Name, value);
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
		private static DbParameter GetDbParameter(ParameterInfo parameter, object value)
		{
			var pType = GetRealParameterType(parameter);
			
			var name = "@".Append((parameter.GetCustomAttribute<ParameterAttribute>()?.Name ?? parameter.Name));
			var dir = (parameter.ParameterType.IsByRef ? ParameterDirection.InputOutput : (parameter.IsOut ? ParameterDirection.Output : ParameterDirection.Input));
			SqlClient.SqlParameter param = null;
			if (pType.IsArray && pType != typeof(string) && pType != typeof(byte[]))
			{
				var elemType = pType.GetElementType();
				param = new SqlClient.SqlParameter(name, MakeTableValuedType(elemType, value));
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
			strCmd.Append(string.Join(", ", typeof(T).GetProperties().Where(pi=>pi.CanWrite && !pi.IsDefined<Golden.Attributes.IgnoreAttribute>()).Select(pi => MetadataMappingProvider.QuoteIdentifier(pi.Name))));
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
			InternalExecuteProcedure<VoidResult>(context, callingMethod, parameters);
		}
		public static IEnumerable<T> ExecuteProcedure<T>(this DbContext context, object[] parameters = null)
		{
			var callingMethod = (new StackFrame(1, false)).GetMethod();
			return InternalExecuteProcedure<T>(context, callingMethod, parameters);
		}
		public static IMultipleResult<T> ExecuteMultipleResultProcedure<T>(this DbContext context, object[] parameters = null)
		{
			var callingMethod = (new StackFrame(1, false)).GetMethod();
			return InternalExecuteMultipleResultProcedure<T>(context, callingMethod, parameters);
		}
		private static IEnumerable<T> InternalExecuteProcedure<T>(DbContext context, MethodBase functionMethod, object[] parameters)
		{
			return InternalExecuteMultipleResultProcedure<T>(context, functionMethod, parameters).GetResult<T>(0).ToList();
			#region OLDCode
			/*
			if (parameters == null) parameters = new object[0];

			var methodParams = functionMethod.GetParameters();
			var functionAttrib = functionMethod.GetCustomAttribute<FunctionAttribute>();

			var _ObjParameters = methodParams.Select((p, i) => GetObjParameter(p, parameters[i])).ToArray();

			object _ReturnValue = null;
			if (typeof(T) == typeof(VoidResult))
			{
				var procRetValue = context.ObjectContext()
					.ExecuteFunction(string.Format("{0}.{1}", functionAttrib.NamespaceName, functionAttrib.FunctionName), _ObjParameters);
				_ReturnValue = new[] { new VoidResult(procRetValue) };
			}
			else
			{
				_ReturnValue = context.ObjectContext()
					.ExecuteFunction<T>(string.Format("{0}.{1}", functionAttrib.NamespaceName, functionAttrib.FunctionName), _ObjParameters)
					.ToList();
			}

			for (int i = 0; i < _ObjParameters.Length; i++)
			{
				var objParam = _ObjParameters[i];
				var mParam = methodParams[i];
				if (mParam.IsOut || mParam.ParameterType.IsByRef) parameters[i] = objParam.Value;
			}

			return (IEnumerable<T>)_ReturnValue;
			*/
			#endregion
		}
		private static IMultipleResult<T> InternalExecuteMultipleResultProcedure<T>(this DbContext context, MethodBase functionMethod, object[] parameters)
		{
			var methodParams = functionMethod.GetParameters();
			var functionAttrib = functionMethod.GetCustomAttribute<FunctionAttribute>();
			var resultTypes = functionMethod.GetCustomAttribute<ResultTypesAttribute>()?.Types;

			var currentResult = new List<T>();
			var results = new List<System.Collections.IList> { currentResult };

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
			var cmdParams = methodParams.Select((pi, i) => GetDbParameter(pi, parameters[i])).ToList();
			cmdParams.ForEach(p => cmd.Parameters.Add(p));

			bool prevConn = (context.Database.Connection.State != ConnectionState.Closed);
			try
			{
				if (!prevConn) context.Database.Connection.Open();

				using (var reader = cmd.ExecuteReader())
				{
					currentResult.AddRange(context.ObjectContext().Translate<T>(reader).ToList());

					int resultTypeIndex = 0;
					while (reader.NextResult())
					{
						var objRet = mTranslate.Value.MakeGenericMethod(resultTypes[resultTypeIndex])
							.Invoke(context.ObjectContext(), new object[] { reader });
						var ilist = mEnumerableToList.Value.MakeGenericMethod(resultTypes[resultTypeIndex])
							.Invoke(null, new object[] { objRet }) as System.Collections.IList;

						results.Add(ilist);

						resultTypeIndex++;
					}

					if (!reader.IsClosed) reader.Close();
				}

				methodParams.ForEach((pi, i) =>
				{
					if (pi.IsOut || pi.ParameterType.IsByRef)
					{
						parameters[i] = cmdParams[i].Value;
					}
				});
			}
			finally
			{
				if (!prevConn) context.Database.Connection.Close();
			}

			return new MultipleResult<T>(results);
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
        public static PrimitivePropertyConfiguration HasColumnType(this PrimitivePropertyConfiguration configuration, SqlDbType columnType)
        {
            return configuration.HasColumnType(columnType, -1);
        }
        public static PrimitivePropertyConfiguration HasColumnType(this PrimitivePropertyConfiguration configuration, SqlDbType columnType, int sizeOrPrecision)
        {
            return configuration.HasColumnType(columnType, sizeOrPrecision, -1);
        }
        public static PrimitivePropertyConfiguration HasColumnType(this PrimitivePropertyConfiguration configuration, SqlDbType columnType, int sizeOrPrecision, int scale)
        {
            switch (columnType)
            {
                case SqlDbType.Udt:
                case SqlDbType.Structured:
                    throw new NotSupportedException($"The type '{columnType.ToString()}' not supported.");
            }

            var typeName = columnType.ToString().ToLower();
            if (scale != -1 && sizeOrPrecision != -1)
                typeName = typeName.Append("(", sizeOrPrecision.ToString(), ",", scale.ToString() , ")");
            else if (sizeOrPrecision != -1)
                typeName = typeName.Append("(", sizeOrPrecision.ToString(), ")");
            configuration.HasColumnType(typeName);
            return configuration;
        }
    }
}
namespace Golden.Data.Extensions
{
	using System;
	using System.Data.Entity;
	using System.Text;

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
