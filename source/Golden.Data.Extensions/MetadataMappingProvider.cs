using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Golden.Data.Extensions
{
	internal struct PropertyMap
	{
		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		public string PropertyName { get; set; }
		/// <summary>
		/// Gets or sets the name of the column.
		/// </summary>
		public string ColumnName { get; set; }
		/// <summary>
		/// Gets or sets a value indicating whether this column is in primary keys.
		/// </summary>
		public bool IsKey { get; set; }
	}
	internal class EntityMap
	{
		private readonly Type _EntityType;
		private readonly List<PropertyMap> _PropertyMaps;

		/// <summary>
		/// Gets or sets the conceptual model EntitySet.
		/// </summary>
		public EntitySet ModelSet { get; set; }
		/// <summary>
		/// Gets or sets the store model EntitySet.
		/// </summary>
		public EntitySet StoreSet { get; set; }

		/// <summary>
		/// Gets or sets the conceptual model EntityType.
		/// </summary>
		public EntityType ModelType { get; set; }
		/// <summary>
		/// Gets or sets the store model EntityType.
		/// </summary>
		public EntityType StoreType { get; set; }

		/// <summary>
		/// Gets the type of the entity.
		/// </summary>
		public Type EntityType
		{
			get { return _EntityType; }
		}
		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		/// <value>
		/// The name of the table.
		/// </value>
		public string TableName { get; set; }

		/// <summary>
		/// Gets the property maps.
		/// </summary>
		public List<PropertyMap> PropertyMaps
		{
			get { return _PropertyMaps; }
		}

		public EntityMap(Type entityType)
		{
			_EntityType = entityType;
			_PropertyMaps = new List<PropertyMap>();
		}
	}
	internal class MetadataMappingProvider
	{
		#region Fields
		private static Lazy<MetadataMappingProvider> _DefaultInstance =
			new Lazy<MetadataMappingProvider>(() => new MetadataMappingProvider());
		#endregion
		#region Properties
		public static MetadataMappingProvider DefaultInstance
		{
			get { return _DefaultInstance.Value; }
		}
		#endregion
		#region Methods
		public EntityMap GetEntityMap(Type type, ObjectContext objectContext)
		{
			var entityMap = new EntityMap(type);
			var metadata = objectContext.MetadataWorkspace;

			var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));
			var entityType = metadata.GetItems<EntityType>(DataSpace.OSpace).Single(e => objectItemCollection.GetClrType(e) == type);

			var entitySet = metadata.GetItems<EntityContainer>(DataSpace.CSpace)
				.SelectMany(a => a.EntitySets).FirstOrDefault(s => s.ElementType.Name == entityType.Name);

			var entitySetMappings = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single().EntitySetMappings.ToList();
			var mapping = GetMapping(entitySetMappings, metadata.GetItems(DataSpace.CSpace)
				.Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType)
				.Cast<EntityType>()
				.Single(x => x.Name == entityType.Name));

			// Find the storage entity set (table) that the entity is mapped
			var mappingFragment =
				(mapping.EntityTypeMappings.FirstOrDefault(a => a.IsHierarchyMapping) ??
				 mapping.EntityTypeMappings.First()).Fragments.First();

			entityMap.ModelType = entityType;
			entityMap.ModelSet = entitySet;
			entityMap.StoreSet = mappingFragment.StoreEntitySet;
			entityMap.StoreType = mappingFragment.StoreEntitySet.ElementType;

			//Set table name
			SetTableName(entityMap);

			//Set properties
			SetProperties(entityMap, mapping, type);

			return entityMap;
		}
		private static EntitySetMapping GetMapping(List<EntitySetMapping> entitySetMappings, EntityType entitySet)
		{
			var mapping = entitySetMappings.SingleOrDefault(x => x.EntitySet.Name == entitySet.Name);
			if (mapping != null)
			{
				return mapping;
			}
			mapping = entitySetMappings.SingleOrDefault(
				x => x.EntityTypeMappings.Where(y => y.EntityType != null).Any(y => y.EntityType.Name == entitySet.Name));
			if (mapping != null)
			{
				return mapping;
			}
			return entitySetMappings.Single(x => x.EntityTypeMappings.Any(y => y.IsOfEntityTypes.Any(z => z.Name == entitySet.Name)));
		}
		private static void SetTableName(EntityMap entityMap)
		{
			var builder = new StringBuilder();

			var storeSet = entityMap.StoreSet;

			string table = null;
			string schema = null;

			MetadataProperty tableProperty;
			MetadataProperty schemaProperty;

			storeSet.MetadataProperties.TryGetValue("Table", true, out tableProperty);
			if (tableProperty == null || tableProperty.Value == null)
			{
				storeSet.MetadataProperties.TryGetValue("http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator:Table", true, out tableProperty);
			}

			if (tableProperty != null)
			{
				table = tableProperty.Value as string;
			}

			// Table will be null if its the same as Name
			if (table == null)
			{
				table = storeSet.Name;
			}

			storeSet.MetadataProperties.TryGetValue("Schema", true, out schemaProperty);
			if (schemaProperty == null || schemaProperty.Value == null)
			{
				storeSet.MetadataProperties.TryGetValue("http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator:Schema", true, out schemaProperty);
			}

			if (schemaProperty != null)
			{
				schema = schemaProperty.Value as string;
			}

			if (!string.IsNullOrEmpty(schema))
			{
				builder.Append(schema);
				builder.Append(".");
			}

			builder.Append(table);

			entityMap.TableName = builder.ToString();
		}
		private static void SetProperties(EntityMap entityMap, EntitySetMapping mapping, Type type)
		{
			var modelTypeKeyMembers = entityMap.ModelType.KeyMembers.ToArray();
			var isTypeOf = new HashSet<string>(GetParentTypes(type).Union(new[] { type }).Select(o => o.Name), StringComparer.Ordinal);

			foreach (var propertyMapping in mapping.EntityTypeMappings.Where(
				o => o.EntityTypes == null || o.EntityTypes.Count < 1 || o.EntityTypes.Any(et => isTypeOf.Contains(et.Name)))
				.SelectMany(o => o.Fragments)
				.SelectMany(o => o.PropertyMappings)
				//.Where(o => o.Property.DeclaringType)
				.GroupBy(o => o.Property.Name).Select(o => o.First()))
			{
				var map = new PropertyMap
				{
					PropertyName = propertyMapping.Property.Name,
					IsKey = modelTypeKeyMembers.Any(km => km.Name.Equals(propertyMapping.Property.Name, StringComparison.Ordinal))
				};

				if (propertyMapping is ScalarPropertyMapping)
				{
					map.ColumnName = ((ScalarPropertyMapping)propertyMapping).Column.Name;
				}
				//else if (propertyMapping is ConditionPropertyMapping)
				//{
				//	map.ColumnName = ((ConditionPropertyMapping)propertyMapping).Column.Name;
				//}
				//else if (propertyMapping is ComplexPropertyMapping)
				//{
				//}

				entityMap.PropertyMaps.Add(map);
			}
		}
		private static IEnumerable<Type> GetParentTypes(Type type)
		{
			// is there any base type?
			if ((type == null) || (type.BaseType == null))
			{
				yield break;
			}

			// return all implemented or inherited interfaces
			foreach (var i in type.GetInterfaces())
			{
				yield return i;
			}

			// return all inherited types
			var currentBaseType = type.BaseType;
			while (currentBaseType != null)
			{
				yield return currentBaseType;
				currentBaseType = currentBaseType.BaseType;
			}
		}
		internal static string QuoteIdentifier(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			return string.Concat("[", name.Replace(".", "].["), "]");
		}
		#endregion
	}
}