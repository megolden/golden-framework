using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Golden.Data.Extensions
{
	internal class FunctionConvention : IStoreModelConvention<EntityContainer>
	{
		private readonly string _DefaultSchema;
		private readonly Type _MethodClassType;
		
		public FunctionConvention(string defaultSchema, Type methodClassType)
		{
			_DefaultSchema = defaultSchema;
			_MethodClassType = methodClassType;
		}
		public void Apply(EntityContainer item, DbModel model)
		{
			var functions = DiscoverFunctions(_MethodClassType);
			foreach (var function in functions)
			{
				var functionName = (string.IsNullOrWhiteSpace(function.Value.FunctionName) ? function.Key.Name : function.Value.FunctionName);

				var storeFunction = EdmFunction.Create(
					functionName,
					FunctionAttribute.CodeFirstDatabaseSchema,
					DataSpace.SSpace,
					new EdmFunctionPayload
					{
						Schema = (function.Value.Schema ?? _DefaultSchema),
						IsAggregate = function.Value.IsAggregate,
						IsBuiltIn = function.Value.IsBuiltIn,
						IsNiladic = function.Value.IsNiladic,
						IsComposable = function.Value.IsComposable,
						ParameterTypeSemantics = function.Value.ParameterTypeSemantics,
						Parameters = GetStoreParameters(model, function.Key, function.Value),
						ReturnParameters = GetStoreReturnParameters(model, function.Key, function.Value),
					},
					null);
				model.StoreModel.AddItem(storeFunction);

				switch (function.Value.Type)
				{
					case FunctionType.ScalarValuedFunction:
					case FunctionType.BuiltInFunction:
					case FunctionType.NiladicFunction:
						continue;
				}

				var modelFunction = EdmFunction.Create(
					storeFunction.Name,
					model.ConceptualModel.Container.Name,
					DataSpace.CSpace,
					new EdmFunctionPayload
					{
						IsFunctionImport = true,
						IsComposable = storeFunction.IsComposableAttribute,
						Parameters = GetModelParameters(model, function.Key, storeFunction),
						ReturnParameters = GetModelReturnParameters(model, function.Key, function.Value),
						EntitySets = GetModelEntitySets(model, function.Key, function.Value),
					},
					null);
				model.ConceptualModel.Container.AddFunctionImport(modelFunction);

				if (modelFunction.IsComposableAttribute)
				{
					model.ConceptualToStoreMapping.AddFunctionImportMapping(new FunctionImportMappingComposable(
						modelFunction,
						storeFunction,
						new FunctionImportResultMapping(),
						model.ConceptualToStoreMapping));
				}
				else
				{
					model.ConceptualToStoreMapping.AddFunctionImportMapping(new FunctionImportMappingNonComposable(
						modelFunction,
						storeFunction,
						Enumerable.Empty<FunctionImportResultMapping>(),
						model.ConceptualToStoreMapping));
				}
			}
		}
		private static KeyValuePair<MethodInfo, FunctionAttribute>[] DiscoverFunctions(Type containerType)
		{
			var result = new List<KeyValuePair<MethodInfo, FunctionAttribute>>();
			foreach (var m in containerType.GetMethods(BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Static).Where(m=>!m.IsDefined<Annotations.IgnoreAttribute>()))
			{
				var functionAttrib = m.GetCustomAttribute<FunctionAttribute>(true);
				if (functionAttrib != null && !(functionAttrib is StoredProcedureAttribute))
				{
					result.Add(new KeyValuePair<MethodInfo, FunctionAttribute>(m, functionAttrib));
				}
			}
			return result.ToArray();
		}

		private static IList<EntitySet> GetModelEntitySets(DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
		{
			ParameterInfo returnParameterInfo = methodInfo.ReturnParameter;
			if (returnParameterInfo == null || (returnParameterInfo.ParameterType == typeof(void) && functionAttribute.Type!= FunctionType.StoredProcedure))
			{
				throw new NotSupportedException(string.Format("The return parameter type of {0} is not supported.", methodInfo.Name));
			}

			if (returnParameterInfo.ParameterType == typeof(void))
			{
				return new EntitySet[0];
			}
			else if (functionAttribute.Type == FunctionType.StoredProcedure && returnParameterInfo.ParameterType != typeof(void))
			{
				//// returnParameterInfo.ParameterType is ObjectResult<T>.
				//var returnParameterClrTypes = GetStoredProcedureReturnTypes(methodInfo).ToArray();
				//if (returnParameterClrTypes.Length > 1)
				//{
				//	// Stored procedure has more than one result. 
				//	// EdmFunctionPayload.EntitySets must be provided. Otherwise, an ArgumentException will be thrown:
				//	// The EntitySets parameter must not be null for functions that return multiple result sets.
				//	return returnParameterClrTypes.Select(clrType =>
				//	{
				//		var modelEntitySet = model
				//			.ConceptualModel
				//			.Container
				//			.EntitySets
				//			.FirstOrDefault(entitySet => entitySet.ElementType == GetModelEntityType(model, clrType, methodInfo)); // TODO: bug.
						
				//		//if (modelEntitySet == null)
				//		//	throw new NotSupportedException(
				//		//		string.Format("{0} for method {1} is not supported in conceptual model as entity set.", clrType.FullName, methodInfo.Name));

				//		return modelEntitySet;

				//	}).Where(s=>s!=null).ToArray();
				//}
			}
			else if (functionAttribute.Type == FunctionType.TableValuedFunction)
			{
				// returnParameterInfo.ParameterType is IQueryable<T>.
				var returnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
				var returnParameterEntityType = GetModelEntityType(model, returnParameterClrType, methodInfo);
				if (returnParameterEntityType != null)
				{
					var modelEntitySet = model
						.ConceptualModel
						.Container
						.EntitySets
						.FirstOrDefault(entitySet => entitySet.ElementType == returnParameterEntityType);
					if (modelEntitySet == null)
					{
						throw new NotSupportedException(
							string.Format("{0} for method {1} is not supported in conceptual model as entity set.",
								returnParameterInfo.ParameterType.FullName, methodInfo.Name));
					}

					return new[] { modelEntitySet };
				}
			}

			// Do not return new EntitySet[0], which causes a ArgumentException:
			// The number of entity sets should match the number of return parameters.
			return null;
		}
		private static IList<FunctionParameter> GetModelReturnParameters(DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
		{
			var returnParameterInfo = methodInfo.ReturnParameter;
			if (returnParameterInfo == null || (returnParameterInfo.ParameterType == typeof(void) && functionAttribute.Type != FunctionType.StoredProcedure))
				throw new NotSupportedException(string.Format("The return parameter type of {0} is not supported.", methodInfo.Name));

			var returnParameterAttribute = returnParameterInfo.GetCustomAttribute<ParameterAttribute>();
			//var returnTypeAttributes = methodInfo.GetCustomAttributes<ResultTypeAttribute>();
			IEnumerable<EdmType> modelReturnParameterEdmTypes;
			if (functionAttribute.Type == FunctionType.StoredProcedure)
			{
				if (returnParameterAttribute != null)
					throw new NotSupportedException(string.Format("ParameterAttribute for method {0} is not supported.", methodInfo.Name));

				modelReturnParameterEdmTypes =
					GetStoredProcedureReturnTypes(methodInfo)
					.Select(clrType => GetModelStructualType(model, clrType, methodInfo));
			}
			else
			{
				//if (returnTypeAttributes.Length > 0)
				//{
				//	throw new NotSupportedException(
				//		string.Format("ResultTypeAttribute for method {0} is not supported.", methodInfo.Name));
				//}

				if (functionAttribute.Type == FunctionType.TableValuedFunction)
				{
					// returnParameterInfo.ParameterType is IQueryable<T>.
					Type returnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
					StructuralType modelReturnParameterStructuralType = GetModelStructualType(model, returnParameterClrType, methodInfo);
					modelReturnParameterEdmTypes = Enumerable.Repeat(modelReturnParameterStructuralType, 1);
				}
				else
				{
					Type returnParameterClrType = returnParameterInfo.ParameterType;
					Type returnParameterAttributeClrType = (returnParameterAttribute != null ? returnParameterAttribute.ClrType : null);
					if (returnParameterAttributeClrType != null && returnParameterAttributeClrType != returnParameterClrType)
					{
						throw new NotSupportedException(
							string.Format("Return parameter of method {0} is of {1}, but its ParameterAttribute.ClrType has a different type {2}",
								methodInfo.Name, returnParameterClrType.FullName, returnParameterAttributeClrType.FullName));
					}

					PrimitiveType returnParameterPrimitiveType = GetModelPrimitiveType(model, returnParameterClrType, methodInfo);
					modelReturnParameterEdmTypes = Enumerable.Repeat(returnParameterPrimitiveType, 1);
				}
			}

			return modelReturnParameterEdmTypes
				.Select((edmType, index) => FunctionParameter.Create(
					string.Format("ReturnType{0}", index),
					edmType.GetCollectionType(),
					ParameterMode.ReturnValue))
				.ToArray();
		}
		private static PrimitiveType GetModelParameterPrimitiveType(DbModel model, MethodInfo methodInfo, ParameterInfo parameterInfo)
		{
			Type parameterClrType = parameterInfo.ParameterType;
			if (parameterClrType.IsByRef) parameterClrType = parameterClrType.GetElementType();
			ParameterAttribute parameterAttribute = parameterInfo.GetCustomAttribute<ParameterAttribute>();
			//Type parameterAttributeClrType = (parameterAttribute != null ? parameterAttribute.ClrType : null);
			if (parameterAttribute != null && parameterAttribute.ClrType != null) parameterClrType = parameterAttribute.ClrType;
			//if (parameterClrType == typeof(ObjectParameter))
			//{
			//	// ObjectParameter.Type is available only when methodInfo is called.
			//	// When building model, its store type/clr type must be provided by ParameterAttribute.
			//	if (parameterAttributeClrType == null)
			//	{
			//		throw new NotSupportedException(
			//			string.Format("Parameter {0} of method {1} is not supported. ObjectParameter parameter must have ParameterAttribute with ParameterAttribute.ClrType specified.",
			//				parameterInfo.Name, methodInfo.Name));
			//	}

			//	parameterClrType = parameterAttributeClrType;
			//}
			//else
			//{
			//	// When parameter is not ObjectParameter, ParameterAttribute.ClrType should be the same as parameterClrType, or not specified.
			//	if (parameterAttributeClrType != null && parameterAttributeClrType != parameterClrType)
			//	{
			//		throw new NotSupportedException(
			//			string.Format("Parameter {0} of method {1} if of {2}, but its ParameterAttribute.ClrType has a different type {3}",
			//				parameterInfo.Name, methodInfo.Name, parameterClrType.FullName, parameterAttributeClrType.FullName));
			//	}
			//}

			return GetModelPrimitiveType(model, parameterClrType, methodInfo);
		}
		private static IList<FunctionParameter> GetModelParameters(DbModel model, MethodInfo methodInfo, EdmFunction storeFunction)
		{
			var parameters = GetDataParameters(methodInfo);
			return storeFunction
				.Parameters
				.Select((p, i) =>
				{
					var parameterInfo = parameters[i];
					return FunctionParameter.Create(
						p.Name,
						GetModelParameterPrimitiveType(model, methodInfo, parameterInfo),
						p.Mode);
				})
				.ToArray();
		}
		private static EntityType GetModelEntityType(DbModel model, Type clrType, MethodInfo methodInfo)
		{
			var clrProperties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
			var entityTypes = model
				.ConceptualModel
				.EntityTypes
				.Where(entityType =>
					entityType.FullName.Equals(clrType.FullName, StringComparison.Ordinal)
					|| entityType.Name.Equals(clrType.Name, StringComparison.Ordinal)
					&& entityType
						.Properties
						.All(edmProperty => clrProperties
							.Any(clrProperty =>
							{
								if (!edmProperty.Name.Equals(clrProperty.Name, StringComparison.Ordinal))
								{
									return false;
								}

								// Entity type's property can be either primitive type or another complex type.
								if (edmProperty.PrimitiveType != null)
								{
									// Entity type's property is primitive type.
									return edmProperty.PrimitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrProperty.PropertyType) ?? clrProperty.PropertyType);
								}

								if (edmProperty.ComplexType != null)
								{
									// Entity type's property is complex type.
									return edmProperty.ComplexType.Name.Equals(clrProperty.PropertyType.Name, StringComparison.Ordinal);
								}

								return false;
							})))
				.ToArray();

			if (entityTypes.Length > 1)
			{
				throw new InvalidOperationException(
					string.Format("{0} for method {1} has multiple ambiguous matching entity types in conceptual model: {2}.",
						clrType.FullName, methodInfo.Name, string.Join(", ", entityTypes.Select(entityType => entityType.FullName))));
			}

			return entityTypes.SingleOrDefault();
		}
		private static ComplexType GetModelComplexType(DbModel model, Type clrType, MethodInfo methodInfo)
		{
			// Cannot add missing complex type instantly. The following code does not work.
			// if (Attribute.IsDefined(clrType, typeof(ComplexTypeAttribute)))
			// {
			//    MethodInfo complexTypeMethod = typeof(DbModelBuilder).GetMethod(nameof(modelBuilder.ComplexType));
			//    complexTypeMethod.MakeGenericMethod(clrType).Invoke(modelBuilder, null);
			//    model.Compile();
			//    modelStructualType = model
			//        .ConceptualModel
			//        .ComplexTypes
			//        .FirstOrDefault(complexType => complexType.FullName.EqualsOrdinal(clrType.FullName));
			// }

			var clrProperties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
			var modelComplexTypes = model
				.ConceptualModel
				.ComplexTypes
				.Where(complexType =>
					complexType.FullName.Equals(clrType.FullName, StringComparison.Ordinal)
					|| complexType.Name.Equals(clrType.Name, StringComparison.Ordinal)
					&& complexType
						.Properties
						.All(edmProperty => clrProperties
							.Any(clrProperty =>
							{
								if (!edmProperty.Name.Equals(clrProperty.Name, StringComparison.Ordinal))
								{
									return false;
								}

								// Complex type's property can be either primitive type or another complex type.
								if (edmProperty.PrimitiveType != null)
								{
									// Complex type's property is primitive type.
									return edmProperty.PrimitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrProperty.PropertyType) ?? clrProperty.PropertyType);
								}

								if (edmProperty.ComplexType != null)
								{
									// Complex type's property is complex type.
									return edmProperty.ComplexType.Name.Equals(clrProperty.PropertyType.Name, StringComparison.Ordinal);
								}

								return false;
							})))
				.ToArray();

			if (modelComplexTypes.Length > 1)
			{
				throw new InvalidOperationException(
					string.Format("{0} for method {1} has multiple ambiguous matching complex types in conceptual model: {2}.",
						clrType.FullName, methodInfo.Name, string.Join(", ", modelComplexTypes.Select(complexType => complexType.FullName))));
			}

			return modelComplexTypes.SingleOrDefault();
		}
		private static StructuralType GetModelStructualType(DbModel model, Type clrType, MethodInfo methodInfo)
		{
			var modelEntityType = GetModelEntityType(model, clrType, methodInfo);
			if (modelEntityType != null)
			{
				return modelEntityType;
			}

			var complexType = GetModelComplexType(model, clrType, methodInfo);
			if (complexType != null)
			{
				return complexType;
			}

			throw new NotSupportedException(
				string.Format("{0} for method {1} is not supported in conceptual model as a structural type.",
					clrType.FullName, methodInfo.Name));
		}
		private static PrimitiveType GetModelPrimitiveType(DbModel model, Type clrType, MethodInfo methodInfo, bool errorIfNotFound = true)
		{
			// Parameter and return parameter can be Nullable<T>.
			// Return parameter can be IQueryable<T>, ObjectResult<T>.
			if (clrType.IsGenericType)
			{
				var genericTypeDefinition = clrType.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(Nullable<>)
					|| genericTypeDefinition == typeof(IQueryable<>)
					|| genericTypeDefinition == typeof(ObjectResult<>))
				{
					clrType = clrType.GetGenericArguments().Single(); // Gets T from Nullable<T>.
				}
			}

			if (clrType.IsEnum)
			{
				var modelEnumType = model
					.ConceptualModel
					.EnumTypes
					.FirstOrDefault(enumType => enumType.FullName.Equals(clrType.FullName, StringComparison.Ordinal));
				if (modelEnumType == null)
				{
					throw new NotSupportedException(
						string.Format("Enum type {1} in method {0} is not supported in conceptual model.", methodInfo.Name, clrType.FullName));
				}

				return modelEnumType.UnderlyingType;
			}

			// clrType is not enum.
			PrimitiveType modelPrimitiveType = PrimitiveType
				.GetEdmPrimitiveTypes()
				.FirstOrDefault(primitiveType => primitiveType.ClrEquivalentType == clrType);
			if (modelPrimitiveType == null)
			{
				if (!errorIfNotFound) return null;

				throw new NotSupportedException(
					string.Format("Type {1} in method {0} is not supported in conceptual model.", methodInfo.Name, clrType.FullName));
			}

			return modelPrimitiveType;
		}

		private static PrimitiveType GetStoreParameterPrimitiveType(DbModel model, MethodInfo methodInfo, ParameterInfo parameterInfo, FunctionAttribute functionAttribute)
		{
			var parameterClrType = parameterInfo.ParameterType;
			if (parameterClrType.IsByRef) parameterClrType = parameterClrType.GetElementType();
			var parameterAttribute = parameterInfo.GetCustomAttribute<ParameterAttribute>();
			//var parameterAttributeClrType = (parameterAttribute != null ? parameterAttribute.ClrType : null);
			if (parameterAttribute != null && parameterAttribute.ClrType != null) parameterClrType = parameterAttribute.ClrType;

            //if (parameterClrType.IsGenericType)
            //{
            //	var parameterClrTypeDefinition = parameterClrType.GetGenericTypeDefinition();
            //	if (parameterClrTypeDefinition == typeof(IEnumerable<>) || parameterClrTypeDefinition == typeof(IQueryable<>))
            //	{
            //		//if (functionAttribute.Type == FunctionType.AggregateFunction)
            //		//	parameterClrType = parameterClrType.GetGenericArguments().Single();
            //		//else
            //		throw new NotSupportedException(
            //			string.Format("Parameter {0} of method {1} is not supported. {2} parameter must be used for FunctionType.AggregateFunction method.",
            //				parameterInfo.Name, methodInfo.Name, typeof(IEnumerable<>).FullName));
            //	}
            //}

            //if (parameterClrType == typeof(ObjectParameter))
            //{
            //	// ObjectParameter must be used for stored procedure parameter.
            //	if (functionAttribute.Type != FunctionType.StoredProcedure)
            //	{
            //		throw new NotSupportedException(
            //			string.Format("Parameter {0} of method {1} is not supported. ObjectParameter parameter must be used for FunctionType.StoredProcedure method.",
            //				parameterInfo.Name, methodInfo.Name));
            //	}

            //	// ObjectParameter.Type is available only when methodInfo is called. 
            //	// When building model, its store type/clr type must be provided by ParameterAttribute.
            //	if (parameterAttributeClrType == null)
            //	{
            //		throw new NotSupportedException(
            //			string.Format("Parameter {0} of method {1} is not supported. ObjectParameter parameter must have ParameterAttribute with ClrType specified, with optional DbType.",
            //				parameterInfo.Name, methodInfo.Name));
            //	}

            //	parameterClrType = parameterAttributeClrType;
            //}
            //else
            //{
            //	// When parameter is not ObjectParameter, ParameterAttribute.ClrType should be either not specified, or the same as parameterClrType.
            //	if (parameterAttributeClrType != null && parameterAttributeClrType != parameterClrType)
            //	{
            //		throw new NotSupportedException(
            //			string.Format("Parameter {0} of method {1} is not supported. It is of {2} type, but its ParameterAttribute.ClrType has a different type {3}",
            //				parameterInfo.Name, methodInfo.Name, parameterClrType.FullName, parameterAttributeClrType.FullName));
            //	}
            //}


            var storePrimitiveTypeName = (parameterAttribute != null ? parameterAttribute.DbType : null);
			return (!string.IsNullOrEmpty(storePrimitiveTypeName)
				? GetStorePrimitiveType(model, storePrimitiveTypeName, methodInfo, parameterInfo)
				: GetStorePrimitiveType(model, parameterClrType, methodInfo, parameterInfo));
		}
		private static PrimitiveType GetStorePrimitiveType(DbModel model, string storeEdmTypeName, MethodInfo methodInfo, ParameterInfo parameterInfo)
		{
            // targetStoreEdmType = model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(primitiveEdmType)).EdmType;
            var storePrimitiveType = model
				.ProviderManifest
				.GetStoreTypes()
				.FirstOrDefault(primitiveType => primitiveType.Name.Equals(storeEdmTypeName, StringComparison.OrdinalIgnoreCase));
			if (storePrimitiveType == null)
			{
				throw new NotSupportedException(
					string.Format("The specified ParameterAttribute.DbType '{0}' for parameter {1} of method {2} is not supported in database.",
						storeEdmTypeName, parameterInfo.Name, methodInfo.Name));
			}

			return storePrimitiveType;
		}
		private static PrimitiveType GetStorePrimitiveType(DbModel model, Type clrType, MethodInfo methodInfo, ParameterInfo parameterInfo)
		{
            // targetStoreEdmType = model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(primitiveEdmType)).EdmType;
            var storePrimitiveType = model
				.ProviderManifest
				.GetStoreTypes()
				.FirstOrDefault(primitiveType => primitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrType) ?? clrType));
			if (storePrimitiveType == null)
			{
				throw new NotSupportedException(
					string.Format("The specified type {0} for parameter {1} of method {2} is not supported in database.",
						clrType.FullName, parameterInfo.Name, methodInfo.Name));
			}

			return storePrimitiveType;
		}
		private static IList<FunctionParameter> GetStoreParameters(DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
		{
			var parameters = GetDataParameters(methodInfo);
			if (parameters.Length > 0 && functionAttribute.Type == FunctionType.NiladicFunction)
				throw new NotSupportedException(string.Format("The Niladic Function can not have parameter.", methodInfo.Name));

            var result = new List<FunctionParameter>();
			for (int i = 0; i < parameters.Length; i++)
			{
                var parameterInfo = parameters[i];
				var parameterAttrib = parameterInfo.GetCustomAttribute<ParameterAttribute>();
				var parameterName = (parameterAttrib != null ? parameterAttrib.Name : null);
				if (string.IsNullOrWhiteSpace(parameterName)) parameterName = parameterInfo.Name;

				result.Add(
					FunctionParameter.Create(
					parameterName,
					GetStoreParameterPrimitiveType(model, methodInfo, parameterInfo, functionAttribute),
					(parameterInfo.IsOut || parameterInfo.ParameterType.IsByRef ? ParameterMode.InOut : ParameterMode.In)));
			}
			return result.ToArray();
		}
		private static IList<FunctionParameter> GetStoreReturnParameters(DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
		{
			if (methodInfo.ReturnParameter == null || (methodInfo.ReturnParameter.ParameterType == typeof(void) && functionAttribute.Type != FunctionType.StoredProcedure))
				throw new NotSupportedException(string.Format("The return type of {0} is not supported.", methodInfo.Name));

			var returnParameterAttribute = methodInfo.ReturnParameter.GetCustomAttribute<ParameterAttribute>();
			//var returnTypeAttributes = methodInfo.GetCustomAttributes<ResultTypeAttribute>();

			if (functionAttribute.Type == FunctionType.StoredProcedure || functionAttribute.Type == FunctionType.TableValuedFunction)
			{
				if (returnParameterAttribute != null)
					throw new NotSupportedException(string.Format("ParameterAttribute for return value of method {0} is not supported.", methodInfo.Name));
			}

			if (functionAttribute.Type == FunctionType.StoredProcedure || methodInfo.ReturnType == typeof(void))
			{
				//Stored Procedure
				return new FunctionParameter[0];
				//return new[]
				//{
				//	FunctionParameter.Create("ReturnType", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), ParameterMode.ReturnValue)
				//};
			}

			//if (returnTypeAttributes.Length > 0)
			//	throw new NotSupportedException(string.Format("ResultTypeAttribute for method {0} is not supported.", methodInfo.Name));

			if (functionAttribute.Type == FunctionType.TableValuedFunction)
			{
				//if (returnParameterAttribute != null)
				//	throw new NotSupportedException(string.Format("ParameterAttribute for return value of method {0} is not supported.", methodInfo.Name));

				/*
				<CollectionType>
				  <RowType>
					<Property Name="PersonID" Type="int" Nullable="false" />
					<Property Name="FirstName" Type="nvarchar" MaxLength="50" />
					<Property Name="LastName" Type="nvarchar" MaxLength="50" />
					<Property Name="JobTitle" Type="nvarchar" MaxLength="50" />
					<Property Name="BusinessEntityType" Type="nvarchar" MaxLength="50" />
				  </RowType>
				</CollectionType>
				*/

				// returnParameterInfo.ParameterType is IQueryable<T>.
				var storeReturnParameterClrType = methodInfo.ReturnParameter.ParameterType.GetGenericArguments().Single();
				var modelReturnParameterStructuralType = GetModelStructualType(model, storeReturnParameterClrType, methodInfo);
				var modelReturnParameterComplexType = modelReturnParameterStructuralType as ComplexType;

				RowType storeReturnParameterRowType = null;
				if (modelReturnParameterComplexType != null)
				{
					storeReturnParameterRowType = RowType.Create(
						modelReturnParameterComplexType.Properties.Select(property =>
							EdmProperty.Create(property.Name, model.ProviderManifest.GetStoreType(property.TypeUsage))), null);
				}
				else
				{
					var modelReturnParameterEntityType = modelReturnParameterStructuralType as EntityType;
					if (modelReturnParameterEntityType != null)
					{
						storeReturnParameterRowType = RowType.Create(
							modelReturnParameterEntityType.Properties.Select(property => CloneEdmProperty(property)), null);
					}
					else
					{
						throw new NotSupportedException(string.Format("Structural type {0} of method {1} cannot be converted to RowType.", modelReturnParameterStructuralType.FullName, methodInfo.Name));
					}
				}

				return new[]
                {
                    FunctionParameter.Create(
                        "ReturnType",
                        storeReturnParameterRowType.GetCollectionType(), // Collection of RowType.
                        ParameterMode.ReturnValue)
                };
			}

			//if (functionAttribute.Type == FunctionType.NonComposableScalarValuedFunction)
			//{
			//	// Non-composable scalar-valued function.
			//	return new FunctionParameter[0];
			//}

			// Composable scalar-valued/Aggregate/Built in/Niladic function.
			// <Function Name="ufnGetProductListPrice" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="true" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo" 
			//    ReturnType ="money">
			var storeReturnParameterPrimitiveType = GetStoreParameterPrimitiveType(model, methodInfo, methodInfo.ReturnParameter, functionAttribute);
			return new[]
            {
                FunctionParameter.Create("ReturnType", storeReturnParameterPrimitiveType, ParameterMode.ReturnValue)
            };
		}
		private static IEnumerable<Type> GetStoredProcedureReturnTypes(MethodInfo methodInfo)
		{
			var returnParameterInfo = methodInfo.ReturnParameter;

			if (returnParameterInfo.ParameterType == typeof(void))
			{
				return new Type[0];
			}

			// returnParameterInfo.ParameterType is ObjectResult<T> Or IEnumerable<T>.
			var returnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
			var returnParameterClrTypes = new[] { returnParameterClrType };
			//var returnParameterClrTypes =
			//	(new[] { returnParameterClrType }).Concat(
			//	methodInfo
			//	.GetCustomAttributes<ResultTypeAttribute>()
			//	.Select(returnTypeAttribute => returnTypeAttribute.Type))
			//	.Distinct();
			return returnParameterClrTypes;
		}
		private static ParameterInfo[] GetDataParameters(MethodInfo methodInfo)
		{
            if (methodInfo.IsDefined(typeof(ExtensionAttribute), false))
            {
                return methodInfo.GetParameters().Where(p => p.Position != 0 && !p.IsDefined<Annotations.IgnoreAttribute>()).ToArray();
            }
            else
            {
                return methodInfo.GetParameters().Where(p => !p.IsDefined<Annotations.IgnoreAttribute>()).ToArray();
            }
		}
		#region Utility
		private static EdmProperty CloneEdmProperty(EdmProperty property)
		{
			var clone = EdmProperty.Create(property.Name, property.TypeUsage);
			clone.CollectionKind = property.CollectionKind;
			clone.ConcurrencyMode = property.ConcurrencyMode;
			clone.IsFixedLength = property.IsFixedLength;
			clone.IsMaxLength = property.IsMaxLength;
			clone.IsUnicode = property.IsUnicode;
			clone.MaxLength = property.MaxLength;
			clone.Precision = property.Precision;
			clone.Scale = property.Scale;
			clone.StoreGeneratedPattern = property.StoreGeneratedPattern;
			clone.SetMetadataProperties(
				property.MetadataProperties
					.Where(metadataProerty => !clone.MetadataProperties.Any(cloneMetadataProperty => cloneMetadataProperty.Name.Equals(metadataProerty.Name, StringComparison.Ordinal))));
			return clone;
		}
		#endregion
	}
}
