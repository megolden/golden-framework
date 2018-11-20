--User Defined Types
SELECT
	t.user_type_id AS UserTypeId,
	SCHEMA_NAME(t.[schema_id]) AS SchemaName,
	t.name AS Name, 
	(CASE WHEN t.max_length <> -1 AND syst.name IN('nchar', 'nvarchar') THEN t.max_length / 2 ELSE t.max_length END) AS [MaxLength],
	t.[precision] AS NumericPrecision,
	t.scale AS NumericScale,
	t.is_nullable AS IsNullable,
	t.is_table_type AS IsTableType
FROM sys.types AS t 
LEFT JOIN
(
	SELECT it.system_type_id, MIN(it.name) AS name 
	FROM sys.types AS it
	WHERE it.is_user_defined = 0 
	GROUP BY it.system_type_id
) AS syst ON t.system_type_id = syst.system_type_id
WHERE t.is_user_defined = 1;

--User Defined Table Types
SELECT 
	tt.user_type_id AS UserTypeId,
	c.name AS [Name], 
	SCHEMA_NAME(tp.[schema_id]) AS TypeSchemaName,
	tp.name AS [TypeName],
	(CASE WHEN c.max_length <> -1 AND tp.name IN('nchar', 'nvarchar') THEN c.max_length / 2 ELSE c.max_length END) AS [MaxLength], 
	c.[precision] AS [NumericPrecision], 
	c.scale AS [NumericScale], 
	c.is_nullable AS [IsNullable], 
	c.is_identity AS [IsIdentity], 
	c.is_computed AS [IsComputed]
FROM 
	sys.columns AS c
	INNER JOIN sys.types AS tp ON c.user_type_id = tp.user_type_id
	INNER JOIN sys.table_types AS tt ON c.[object_id] = tt.type_table_object_id;