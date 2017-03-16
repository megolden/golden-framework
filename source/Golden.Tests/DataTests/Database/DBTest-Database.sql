use master;
go

create database DBTest
go
use DBTest
go

create schema Addressing
go

create type udtIntArray as table(Value int);
create type udtIntStringArray as table([Key] int, Value nvarchar(max));
create type udtKeyValue as table([Key] nvarchar(max), Value nvarchar(max));
create type udtKeyValueData as table([Key] nvarchar(max), Value nvarchar(max), Data nvarchar(max));
go

create table Addressing.City
(
    Id int identity primary key not null,
    Name nvarchar(50) null
)
go
create table Book
(
    Id int identity primary key not null,
    Title nvarchar(100)
)
go
create table Student
(
    Id int identity primary key not null,
    Name nvarchar(100) null,
	CityRef int null,
	BirthDate datetime null,
	constraint FK_Student_City foreign key(CityRef) references Addressing.City(Id)
)
go
create table StudentInfo
(
    StudentRef int primary key not null,
    CAddress nvarchar(500) null
	constraint FK_StudentInfo_Student foreign key(StudentRef) references Student(Id)
)
go
create table StudentBook
(
    StudentRef int not null,
    BookRef int not null,
	constraint PK_StudentBook primary key(StudentRef, BookRef),
	constraint FK_SBook_Student foreign key(StudentRef) references Student(Id),
	constraint FK_BStudent_Book foreign key(BookRef) references Book(Id)
)
go
create table Addressing.Province
(
    Id int identity primary key not null,
    Name nvarchar(50) null
)
go

insert Addressing.City(Name) values (N'Tehran'), (N'Shiraz');
go

insert Addressing.City(Name) values (N'Mashhad'), (N'Tehran'), (N'Shiraz'), (N'Sari'), (N'Ardebil');
insert Student(Name, CityRef) values (N'Ali', 2), (N'Abbas', null), (N'Mehdi', 3), (N'Mohsen', 2), (N'Alireza', 3);
insert Addressing.Province(Name) values (N'Fars'), (N'Tehran'), (N'Khorasan'), (N'Kerman'), (N'Esfahan');
go
create view dbo.StudentView
as
	select s.Id, s.Name, s.BirthDate, c.Name as CityName 
	from dbo.Student as s
	left join Addressing.City as c on s.CityRef = c.Id
go
create function fnGetAgeYear
(
	@BirthYear INT
)
returns int
AS
begin
	return (YEAR(GETDATE()) - @BirthYear);
end
go

create function fnGetNames(@value int)
returns table
AS
return
(
	select 'Ali' AS Name
	union
	select 'Alireza'
	union
	select 'Mehdi'
	union
	select 'Aghil'
	union
	select 'Mohsen'
	union
	select CONVERT(nvarchar, @value)
)
go

create function fnGetStudents()
returns table
AS
return
(
	select 1 AS Id, 'Ali' AS Name
	union
	select 2, 'Alireza'
	union
	select 3, 'Mehdi'
	union
	select 4, 'Aghil'
	union
	select 5, 'Mohsen'
)
go


create procedure spGetStudents
(
	@Count int output
)
AS
begin
	SET NOCOUNT OFF;
	
	select 1 AS Id, 'Ali' AS Name
	union
	select 2, 'Alireza'
	union
	select 3, 'Mehdi'
	union
	select 4, 'Aghil'
	union
	select 5, 'Mohsen'

	SET @Count = @@ROWCOUNT;
end
go

create procedure spGetNames
(
	@Count int output
)
AS
begin
	SET NOCOUNT OFF;
	
	select 'Ali' as Name
	union
	select 'Alireza'
	union
	select 'Mehdi'
	union
	select 'Aghil'
	union
	select 'Mohsen'

	SET @Count = @@ROWCOUNT;
end
go

create procedure spFindNamesAndCityNames
(
	@Name nvarchar(100) output
)
AS
begin
	SET NOCOUNT ON;
	
	select Name 
	from dbo.Student
	where Name like N'%' + @Name + N'%'
	order by Name

	select *
	from Addressing.City
	where Name like N'%' + @Name + N'%'

	select Name 
	from dbo.Student
	where Name like N'%' + @Name + N'%'
	order by Name desc

	set @Name = 'Golden';
end
go

create procedure spInsertTest
(
	@Id int output,
	@Name nvarchar(100)
)
AS
begin
	if @Id is null or @Id = 0 begin
		insert Student(Name) values(@Name);
		set @Id = CONVERT(INT, SCOPE_IDENTITY());
	end else begin
		update Student set Name = @Name where Id = @Id
	end
end
go
--#####################################################
create procedure spTestTypes
(
	@Keys udtIntArray readonly
)
AS
begin
	select 'Value: ' + CONVERT(nvarchar, Value) AS Name from @Keys
end
go
create function fnTestTypes
(
	@Dictionary udtIntStringArray readonly
)
returns table
AS
return
(
	select Value AS Name from @Dictionary
)
go
create function fnGetMaxName
(
	@Values udtIntStringArray readonly
)
returns nvarchar(max)
AS
begin
	return (select top(1) Value FROM @Values order by [Key] desc);
end
go

CREATE PROCEDURE spSearchStudent
(
	@Filter udtKeyValueData READONLY,
	@Sorting udtKeyValue READONLY,
	@PageNumber int = NULL,
	@PageSize int = NULL
)
AS
BEGIN
	SET NOCOUNT ON; --Prevents display record count
	
	DECLARE 
		@Temp nvarchar(max),
		@CmdColumns nvarchar(max), 
		@CmdFrom nvarchar(max),
		@CmdJoins nvarchar(max),
		@CmdCondition nvarchar(max),
		@CmdOrders nvarchar(max),
		@Cmd nvarchar(max);
	
	SET @CmdColumns = 'std.Id, std.Name, std.BirthDate, city.Name AS CityName';
	SET @CmdFrom = 'dbo.Student AS std';
	SET @CmdJoins = 'LEFT JOIN Addressing.City AS city ON std.CityRef = city.Id';

	--Filter
	IF (EXISTS(SELECT NULL FROM @Filter)) BEGIN
		SET @Temp = 
		(
			SELECT CONCAT(' AND ', 
				(
					CASE 
						WHEN filter.[Key] IN('Id', 'Name', 'BirthDate') THEN CONCAT('std.', filter.[Key])
						WHEN filter.[Key] = 'CityName' THEN 'city.Name'
					END
				), ' ', 
				(
					CASE 
						WHEN filter.Value IS NULL AND filter.Data = '=' THEN 'IS NULL'
						WHEN filter.Value IS NULL AND filter.Data = '<>' THEN 'IS NOT NULL'
						ELSE CONCAT(filter.Data, ' ', filter.Value)
					END	
				))
			FROM @Filter AS filter
			FOR XML PATH('')
		)
		SET @Temp = CAST('<value>'+ SUBSTRING(@Temp, 6, 4000) + '</value>' AS XML).value('/value[1]', 'nvarchar(max)')
		IF (@Temp IS NOT NULL AND @Temp <> '')
			SET @CmdCondition = CONCAT(@CmdCondition + ' AND ', @Temp);
	END
	
	--Sorting
	IF (EXISTS(SELECT NULL FROM @Sorting)) BEGIN
		SET @Temp = 
		(
			SELECT CONCAT(', ', 
				(
					CASE 
						WHEN sort.[Key] IN('Id', 'Name', 'BirthDate') THEN CONCAT('std.', sort.[Key])
						WHEN sort.[Key] = 'CityName' THEN 'city.Name'
					END
				), 
				(CASE WHEN LEFT(sort.Value, 1) = 'D' THEN ' DESC' ELSE ' ASC' END))
			FROM @Sorting AS sort
			FOR XML PATH('')
		)
		SET @CmdOrders = CONCAT(@CmdOrders + ', ', SUBSTRING(@Temp, 3, 4000));
	END

	--Paging
	IF (@PageNumber IS NULL)
		SET @CmdColumns = CONCAT(@CmdColumns, ', NULL AS _TotalRowCount');
	ELSE BEGIN
		IF (@CmdOrders IS NULL) SET @CmdOrders = 'std.Id';
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
