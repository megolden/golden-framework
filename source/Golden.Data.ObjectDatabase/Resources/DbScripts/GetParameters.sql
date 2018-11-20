SELECT 
	p.name AS Name, 
	tp.name AS TypeName, 
	SCHEMA_NAME(tp.[schema_id]) AS SchemaName,
	(CASE WHEN p.max_length <> -1 AND tp.name IN('nchar', 'nvarchar') THEN p.max_length / 2 ELSE p.max_length END) AS MaxLength,
	p.[precision] AS NumericPrecision, 
	p.scale AS NumericScale, 
	p.is_output AS IsOutput,
	p.is_readonly AS IsReadOnly, 
	tp.is_user_defined AS IsUserDefinedType,
	tp.is_table_type AS IsTableType
FROM 
	sys.parameters AS p
	INNER JOIN sys.types AS tp ON p.user_type_id = tp.user_type_id
WHERE 
	p.[object_id] = OBJECT_ID(@FullObjectName)
	AND ISNULL(p.name, '') <> '' --If 'name' is empty string(or null), then parameter is return type of function. 