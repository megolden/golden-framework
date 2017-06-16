SELECT 
	c.name AS [Name], 
	tp.name AS [TypeName],
	(CASE WHEN c.max_length <> -1 AND tp.name IN('nchar', 'nvarchar') THEN c.max_length / 2 ELSE c.max_length END) AS [MaxLength], 
	c.[precision] AS [NumericPrecision], 
	c.scale AS [NumericScale], 
	c.is_nullable AS [IsNullable], 
	c.is_identity AS [IsIdentity], 
	c.is_computed AS [IsComputed]
	--,sc.[text] AS [DefaultValueText]
FROM 
	sys.columns AS c
INNER JOIN sys.types AS tp ON c.user_type_id = tp.user_type_id
WHERE 
	c.[object_id] = OBJECT_ID(@FullTableName)