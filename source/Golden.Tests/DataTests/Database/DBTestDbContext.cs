using System;
using Golden.Data.Extensions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using Golden.Annotations;

namespace Golden.Tests
{
	public partial class DBTestDbContext : DbContext
	{
		public virtual DbSet<Student> Student { get; set; }
		public virtual DbSet<City> City { get; set; }
		public virtual DbSet<Province> Province { get; set; }
		public virtual DbSet<StudentView> StudentView { get; set; }

		public DBTestDbContext(string connectionString) : base(connectionString)
		{
			base.Configuration.AutoDetectChangesEnabled = true;
			base.Configuration.LazyLoadingEnabled = false;
			base.Configuration.ProxyCreationEnabled = false;
			base.Configuration.ValidateOnSaveEnabled = false;
			Database.SetInitializer<DBTestDbContext>(null);
		}
		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			//Conventions
			modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
			modelBuilder.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingEntitySetNameConvention>();

			//Schema
			string defaultSchema = "dbo";
			modelBuilder.HasDefaultSchema(defaultSchema);
			modelBuilder.SetEntitySchema("Addressing", typeof(City), typeof(Province));

			//Keys
			modelBuilder.Entity<Student>().HasKey(e => e.Id).Property(e => e.Id)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			modelBuilder.Entity<City>().HasKey(e => e.Id).Property(e => e.Id)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			modelBuilder.Entity<Province>().HasKey(e => e.Id).Property(e => e.Id)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			modelBuilder.Entity<StudentView>().HasKey(e => e.Id);

			//Relationships
			modelBuilder.Entity<City>().HasMany<Student>(e => e.Student).WithOptional(e => e.City).HasForeignKey(e => e.CityRef);

			//ComplexTypes
			modelBuilder.ComplexType<fnGetNamesResult>();
			modelBuilder.AddComplexTypes
			(
				typeof(fnGetNamesResult),
				typeof(spGetNamesResult),
				typeof(fnGetStudentsResult),
				typeof(spGetStudentsResult),
				typeof(spGetNamesAndCityNamesResult),
				typeof(spGetNamesAndCityNamesResult2)
			);
			//modelBuilder.AddComplexTypes(typeof(Student).Assembly);

			//Function Conventions
			modelBuilder.AddFunctions<DBTestDbContext>(defaultSchema);

            base.OnModelCreating(modelBuilder);
		}

		[ScalarFunction("fnGetAgeYear", "dbo")]
		[return: Parameter(typeof(int), "INT")]
		public int? fnGetAgeYear
		(
			[Parameter("BirthYear", "INT", typeof(int))]
            int? birthYear
		)
		{
			return this.ExecuteScalarFunction<int?>(birthYear);
		}

		[TableValuedFunction(typeof(DBTestDbContext), "dbo.fnGetNames")]
		public IQueryable<fnGetNamesResult> fnGetNames(int? value)
		{
			return this.ExecuteTableValuedFunction<fnGetNamesResult>(value);
		}

		[TableValuedFunction(typeof(DBTestDbContext), "dbo.fnGetStudents")]
		public IQueryable<fnGetStudentsResult> fnGetStudents()
		{
			return this.ExecuteTableValuedFunction<fnGetStudentsResult>();
		}

		[StoredProcedure(typeof(DBTestDbContext), "dbo.spInsertTest")]
		public void spInsertTest(ref int? id, string name, ref int? @class)
		{
			var _Parameters = new object[] { id, name, @class };
			this.ExecuteProcedure(_Parameters);
			id =  (int?)_Parameters[0];
			@class = (int?)_Parameters[2];
        }

        [StoredProcedure(typeof(DBTestDbContext), "dbo.spGetStudents")]
		public IEnumerable<spGetStudentsResult> spGetStudents(ref int? count)
		{
			var _Parameters = new object[] { count };
			var _ReturnValue = this.ExecuteProcedure<spGetStudentsResult>(_Parameters);
			count = (int?)_Parameters[0];
			return _ReturnValue;
		}

		[StoredProcedure(typeof(DBTestDbContext), "dbo.spGetNames")]
		public IEnumerable<spGetNamesResult> spGetNames(ref int? count)
		{
			var _Parameters = new object[] { count };
			var _ReturnValue = this.ExecuteProcedure<spGetNamesResult>(_Parameters);
			count = (int?)_Parameters[0];
			return _ReturnValue;
		}

		[StoredProcedure(typeof(DBTestDbContext), "dbo.spFindNamesAndCityNames")]
		[ResultTypes(typeof(spGetNamesAndCityNamesResult), typeof(City))]
		public IMultipleResult spFindNamesAndCityNames(ref string name)
		{
			var _Parameters = new object[] { name };
			var _ReturnValue = this.ExecuteMultipleResultProcedure(_Parameters);
			name = (string)_Parameters[0];
			return _ReturnValue;
		}

		[StoredProcedure(typeof(DBTestDbContext), nameof(spTestTypes), "dbo")]
		public IEnumerable<spGetNamesResult> spTestTypes(udtIntArray[] keys)
		{
			var _Parameters = new object[] { keys };
			var _ReturnValue = this.ExecuteProcedure<spGetNamesResult>(_Parameters);
			return _ReturnValue;
		}

		[TableValuedFunction(typeof(DBTestDbContext), nameof(fnTestTypes), "dbo")]
		[Ignore]
		public IEnumerable<fnGetNamesResult> fnTestTypes(udtIntStringArray[] dictionary)
		{
			return this.ExecuteTableValuedFunctionResult<fnGetNamesResult>(dictionary);
		}

		[ScalarFunction(nameof(fnGetMaxName), "dbo")]
		[Ignore]
		public string fnGetMaxName(udtIntStringArray[] values)
		{
			return this.ExecuteScalarFunction<string>(values);
		}

        [StoredProcedure(typeof(DBTestDbContext), "dbo.spSearchStudent")]
        public IEnumerable<spSearchStudentResult> spSearchStudent(udtKeyValueData[] filter, udtKeyValue[] sorting, int? pageNumber, int? pageSize)
        {
            var _Parameters = new object[] { filter, sorting, pageNumber, pageSize };
            return this.ExecuteProcedure<spSearchStudentResult>(_Parameters);
        }
    }
}
