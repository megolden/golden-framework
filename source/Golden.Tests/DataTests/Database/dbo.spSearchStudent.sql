IF TYPE_ID('dbo.udtKeyValue') IS NULL CREATE TYPE dbo.udtKeyValue AS TABLE([Key] nvarchar(max), Value nvarchar(max));
GO
IF TYPE_ID('dbo.udtKeyValueData') IS NULL CREATE TYPE dbo.udtKeyValueData AS TABLE([Key] nvarchar(max), Value nvarchar(max), Data nvarchar(max));
GO
IF OBJECT_ID('dbo.spSearchStudent') IS NOT NULL DROP PROCEDURE dbo.spSearchStudent;
GO

CREATE PROCEDURE dbo.spSearchStudent
(
	@Filter udtKeyValueData READONLY,
	@Sorting udtKeyValue READONLY,
	@PageNumber int = NULL,
	@PageSize int = NULL
)
AS
BEGIN
	--Prevents display record count
	SET NOCOUNT ON;
	
	DECLARE 
		@Temp nvarchar(max),
		@CmdColumns nvarchar(max), 
		@CmdFrom nvarchar(max),
		@CmdJoins nvarchar(max),
		@CmdCondition nvarchar(max),
		@CmdOrders nvarchar(max),
		@Cmd nvarchar(max);

	DECLARE @Columns table
	(
		Id int NOT NULL PRIMARY KEY,
		Name varchar(128),
		Alias varchar(128) NOT NULL UNIQUE,
		CastTypeName varchar(128) NULL
	);
	INSERT @Columns VALUES (1, 'std.Id', 'Id', NULL), (2, 'std.Name', 'Name', 'NC'), (3, 'std.BirthDate', 'BirthDate', 'C'), (4, 'city.Name', 'CityName', 'NC');
	
	SET @CmdColumns = 'std.Id, std.Name, std.BirthDate, city.Name AS CityName';
	SET @CmdFrom = 'dbo.Student AS std';
	SET @CmdJoins = 'LEFT JOIN Addressing.City AS city ON std.CityRef = city.Id';

	--Filter
	IF (EXISTS(SELECT NULL FROM @Filter)) BEGIN
		SET @Temp = 
		(
			SELECT ' ' + 
				IIF(filter.Data IN('AND', 'OR', '(', ')'), filter.Data, 
				CONCAT(col.Name, ' ', (CASE 
					WHEN filter.Value IS NULL AND filter.Data = '=' THEN 'IS NULL'
					WHEN filter.Value IS NULL AND filter.Data = '<>' THEN 'IS NOT NULL'
					WHEN filter.Data IN('=', '<>', '>', '>=', '<', '<=', 'LIKE') THEN 
						CONCAT(filter.Data, ' ', (CASE col.CastTypeName 
							WHEN 'C' THEN ('''' + REPLACE(filter.Value, '''', '''''') + '''') 
							WHEN 'NC' THEN ('N''' + REPLACE(filter.Value, '''', '''''') + '''')
							ELSE filter.Value
						END))
					ELSE NULL
				END)))
			FROM @Filter AS filter
			LEFT JOIN @Columns AS col ON filter.[Key] = col.Alias
			FOR XML PATH('')
		);
		SET @Temp = CAST('<value>'+ SUBSTRING(@Temp, 2, 4000) + '</value>' AS XML).value('/value[1]', 'nvarchar(max)');
		IF (@Temp IS NOT NULL AND @Temp <> '')
			SET @CmdCondition = CONCAT(@CmdCondition + ' AND ', @Temp);
	END
	
	--Sorting
	IF (EXISTS(SELECT NULL FROM @Sorting)) BEGIN
		SET @Temp = 
		(
			SELECT CONCAT(', ', col.Name, ' ', IIF(LEFT(sort.Value, 1) = 'D', 'DESC', 'ASC'))
			FROM @Sorting AS sort
			INNER JOIN @Columns AS col ON sort.[Key] = col.Alias
			FOR XML PATH('')
		);
		SET @Temp = SUBSTRING(@Temp, 3, 4000);
		IF (@Temp IS NOT NULL AND @Temp <> '')
			SET @CmdOrders = CONCAT(@CmdOrders + ', ', @Temp);
	END

	--Paging
	IF (@PageNumber IS NULL)
		SET @CmdColumns = CONCAT(@CmdColumns, ', NULL AS _TotalRowCount');
	ELSE BEGIN
		IF (@CmdOrders IS NULL) SET @CmdOrders = (SELECT Name FROM @Columns WHERE Id = 1);
		SET @CmdColumns = CONCAT(@CmdColumns, ', COUNT(*) OVER() AS _TotalRowCount');
		SET @CmdOrders = CONCAT(@CmdOrders, ' OFFSET ', (@PageNumber - 1) * @PageSize, ' ROWS FETCH NEXT ', @PageSize, ' ROWS ONLY');
	END

	SET @Cmd = CONCAT
	(
		'SELECT ', 
		@CmdColumns,
		NCHAR(13),
		'FROM ', 
		@CmdFrom, 
		NCHAR(13),
		@CmdJoins + NCHAR(13), 
		'WHERE ' + @CmdCondition + NCHAR(13),
		'ORDER BY ' + @CmdOrders
	);

	--Results
	EXECUTE sp_executesql @Cmd;
	
	--SELECT @Cmd; --for test only
END
GO
