using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace Golden.Data.Extensions
{
	internal enum FunctionType
	{
		StoredProcedure = 0,
		TableValuedFunction,
		/// <summary>
		/// Composable and NonComposable ScalarValuedFunction
		/// </summary>
		ScalarValuedFunction,
		BuiltInFunction,
		NiladicFunction,
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public abstract class FunctionAttribute : DbFunctionAttribute
	{
		public const string CodeFirstDatabaseSchema = "CodeFirstDatabaseSchema";

		private readonly FunctionType _Type;
		private readonly bool _IsComposable;
		private readonly bool _IsAggregate;
		private readonly bool _IsBuiltIn;
		private readonly bool _IsNiladic;

		internal FunctionType Type { get { return _Type; } }
		public bool IsComposable { get { return _IsComposable; } }
		internal bool IsAggregate { get { return _IsAggregate; } }
		internal bool IsBuiltIn { get { return _IsBuiltIn; } }
		internal bool IsNiladic { get { return _IsNiladic; } }
		public string Schema { get; private set; }
		internal ParameterTypeSemantics ParameterTypeSemantics { get; set; }

		internal FunctionAttribute(FunctionType type, string name, string schema = null, string namespaceName = CodeFirstDatabaseSchema)
			: base(namespaceName, (name.Contains(".") ? name.Substring(name.IndexOf('.') + 1) : name))
		{
			ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion;

			_Type = type;
			var i = name.LastIndexOf('.');
			if (i >= 0) this.Schema = name.Remove(i);
			if (schema != null) this.Schema = schema;

			switch (type)
			{
				case FunctionType.TableValuedFunction:
				case FunctionType.StoredProcedure:
					if (CodeFirstDatabaseSchema.Equals(namespaceName, StringComparison.Ordinal))
						throw new ArgumentException("The namespaceName parameter must be set for Table Valued Functions and Stored Procedures.");
					break;
				//case FunctionType.ModelDefinedFunction:
				//	if (CodeFirstDatabaseSchema.EqualsOrdinal(namespaceName))
				//	{
				//		throw new ArgumentException("For Model Defined Functions the namespaceName parameter must be set to the namespace of the DbContext class.");
				//	}
				//	break;
				//default:
				//	if (!CodeFirstDatabaseSchema.Equals(namespaceName, StringComparison.Ordinal))
				//		throw new ArgumentException("The namespaceName parameter may only be set for Table Valued Functions.");
				//	break;
			}

			switch (type)
			{
				case FunctionType.StoredProcedure:
					//case FunctionType.NonComposableScalarValuedFunction:
					_IsComposable = false;
					_IsAggregate = false;
					_IsBuiltIn = false;
					_IsNiladic = false;
					break;

				case FunctionType.TableValuedFunction:
				case FunctionType.ScalarValuedFunction:
				//case FunctionType.ModelDefinedFunction:
					_IsComposable = true;
					_IsAggregate = false;
					_IsBuiltIn = false;
					_IsNiladic = false;
					break;

				//case FunctionType.AggregateFunction:
				//	this.IsComposable = true;
				//	this.IsAggregate = true;
				//	this.IsBuiltIn = false;
				//	this.IsNiladic = false;
				//	break;

				case FunctionType.BuiltInFunction:
					_IsComposable = true;
					_IsAggregate = false;
					_IsBuiltIn = true;
					_IsNiladic = false;
					break;

				case FunctionType.NiladicFunction:
					_IsComposable = true;
					_IsAggregate = false;
					_IsBuiltIn = true;
					_IsNiladic = true;
					break;
			}
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class StoredProcedureAttribute : FunctionAttribute
	{
		public StoredProcedureAttribute(Type dbContextType, string name)
			: this(dbContextType, name, null)
		{
		}
		public StoredProcedureAttribute(string namespaceName, string name)
			: this(namespaceName, name, null)
		{
		}
		public StoredProcedureAttribute(Type dbContextType, string name, string schema)
			: base(FunctionType.StoredProcedure, name, schema, dbContextType.Name)
		{
		}
		public StoredProcedureAttribute(string namespaceName, string name, string schema)
			: base(FunctionType.StoredProcedure, name, schema, namespaceName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class TableValuedFunctionAttribute : FunctionAttribute
	{
		public TableValuedFunctionAttribute(Type dbContextType, string name)
			: this(dbContextType, name, null)
		{
		}
		public TableValuedFunctionAttribute(string namespaceName, string name)
			: this(namespaceName, name, null)
		{
		}

		public TableValuedFunctionAttribute(Type dbContextType, string name, string schema)
			: base(FunctionType.TableValuedFunction, name, schema, dbContextType.Name)
		{
		}
		public TableValuedFunctionAttribute(string namespaceName, string name, string schema)
			: base(FunctionType.TableValuedFunction, name, schema, namespaceName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class ScalarFunctionAttribute : FunctionAttribute
	{
		public ScalarFunctionAttribute(string name)
			: this(name, null)
		{
		}
		public ScalarFunctionAttribute(string name, string schema)
			: base(FunctionType.ScalarValuedFunction, name, schema)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class BuiltInFunctionAttribute : FunctionAttribute
	{
		public BuiltInFunctionAttribute(string name)
			: base(FunctionType.BuiltInFunction, name)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class NiladicFunctionAttribute : FunctionAttribute
	{
		public NiladicFunctionAttribute(string name)
			: base(FunctionType.NiladicFunction, name)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class ParameterAttribute : Attribute
	{
		public string Name { get; set; }
		public string DbType { get; set; }
		public string DbSchema { get; set; }
		public Type ClrType { get; set; }

		public ParameterAttribute() : this((string)null, (string)null, (string)null, (Type)null)
		{
		}
		public ParameterAttribute(string name) : this(name, (string)null)
		{
		}
		public ParameterAttribute(string name, string dbType) : this(name, dbType, (string)null)
		{
		}
		public ParameterAttribute(string name, string dbType, string schema) : this(name, dbType, schema, (Type)null)
		{
		}
		public ParameterAttribute(Type clrType) : this(clrType, (string)null)
		{
		}
		public ParameterAttribute(Type clrType, string dbType) : this(clrType, dbType, (string)null)
		{
		}
		public ParameterAttribute(Type clrType, string dbType, string schema) : this((string)null, dbType, schema, clrType)
		{
		}
		public ParameterAttribute(string name, Type clrType) : this(name, (string)null, clrType)
		{
		}
		public ParameterAttribute(string name, string dbType, Type clrType) : this(name, dbType, (string)null, clrType)
		{
		}
		public ParameterAttribute(string name, string dbType, string schema, Type clrType)
		{
			this.Name = name;
			this.ClrType = clrType;
			this.DbType = dbType;
			this.DbSchema = schema;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class ResultTypesAttribute : Attribute
	{
		private readonly Type[] _Types;

		public Type[] Types
		{
			get { return _Types; }
		}

		public ResultTypesAttribute(params Type[] types)
		{
			_Types = types;
		}
	}

	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class UserDefinedTypeAttribute : Attribute
	{
		public string Name { get; private set; }
		public string Schema { get; private set; }

		public UserDefinedTypeAttribute() : this(null)
		{
		}
		public UserDefinedTypeAttribute(string name):this(name, null)
		{
		}
		public UserDefinedTypeAttribute(string name, string schema)
		{
			this.Name = name;
			this.Schema = schema;
		}
	}

	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class UserDefinedTableTypeAttribute : UserDefinedTypeAttribute
	{
		public UserDefinedTableTypeAttribute() : this(null)
		{
		}
		public UserDefinedTableTypeAttribute(string name):this(name, null)
		{
		}
		public UserDefinedTableTypeAttribute(string name, string schema):base(name, schema)
		{
		}
	}

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ColumnAttribute : Attribute
    {
        public int Order { get; set; }

        public ColumnAttribute()
        {
        }
    }
}
