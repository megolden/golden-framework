SELECT
	TABLE_SCHEMA AS [Schema],
	TABLE_NAME AS [Name]
FROM
	INFORMATION_SCHEMA.VIEWS
WHERE
	NOT 
	(
		TABLE_SCHEMA = 'dbo' 
		AND TABLE_NAME IN('syssegments', 'sysconstraints') 
		AND SUBSTRING(CAST(SERVERPROPERTY('productversion') AS varchar(20)), 1, 1) = 8
	)