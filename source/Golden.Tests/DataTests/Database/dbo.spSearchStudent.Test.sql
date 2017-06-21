DECLARE @Filter udtKeyValueData;
INSERT @Filter([Key], Data, Value) VALUES 
	 (NULL,			'(',		NULL)
	,('Id',			'<>',		'0')
	,(NULL,			'AND',		NULL)
	,('birthDate',	'>=',		'2009/01/01')
	,(NULL,			')',		NULL)
	,(NULL,			'OR',		NULL)
	,('Name',		'=',		'mehdi')

DECLARE @Sorting udtKeyValue;
INSERT @Sorting VALUES
	('Name', NULL)
	,('BirthDate', 'desc')


EXECUTE dbo.spSearchStudent 
	@Filter = @Filter
	,@Sorting = @Sorting
	,@PageNumber = 1
	,@PageSize = 10
