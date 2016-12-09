using System;
using Golden.Data.Extensions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace Golden.Tests
{
	public partial class Test1DbContext : DbContext
	{
		public virtual DbSet<Student> Student { get; set; }
		public virtual DbSet<City> City { get; set; }
		public virtual DbSet<Province> Province { get; set; }
		public virtual DbSet<StudentView> StudentView { get; set; }

		public Test1DbContext(string connectionString) : base(connectionString)
		{
			base.Configuration.AutoDetectChangesEnabled = true;
			base.Configuration.LazyLoadingEnabled = false;
			base.Configuration.ProxyCreationEnabled = false;
			base.Configuration.ValidateOnSaveEnabled = false;
			Database.SetInitializer<Test1DbContext>(null);
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
			modelBuilder.AddFunctions<Test1DbContext>(defaultSchema);

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

		[TableValuedFunction(typeof(Test1DbContext), "dbo.fnGetNames")]
		public IQueryable<fnGetNamesResult> fnGetNames()
		{
			return this.ExecuteTableValuedFunction<fnGetNamesResult>();
		}

		[TableValuedFunction(typeof(Test1DbContext), "fnGetStudents")]
		public IQueryable<fnGetStudentsResult> fnGetStudents()
		{
			return this.ExecuteTableValuedFunction<fnGetStudentsResult>();
		}

		[StoredProcedure(typeof(Test1DbContext), "spInsertTest")]
		public void spInsertTest(ref int? id, string name)
		{
			var _Parameters = new object[] { id, name };
			this.ExecuteProcedure(_Parameters);
			id = (int?)_Parameters[0];
		}

		[StoredProcedure(typeof(Test1DbContext), "spGetStudents")]
		public IEnumerable<spGetStudentsResult> spGetStudents(ref int? count)
		{
			var _Parameters = new object[] { count };
			var _ReturnValue = this.ExecuteProcedure<spGetStudentsResult>(_Parameters);
			count = (int?)_Parameters[0];
			return _ReturnValue;
		}

		[StoredProcedure(typeof(Test1DbContext), "spGetNames")]
		public IEnumerable<spGetNamesResult> spGetNames(ref int? count)
		{
			var _Parameters = new object[] { count };
			var _ReturnValue = this.ExecuteProcedure<spGetNamesResult>(_Parameters);
			count = (int?)_Parameters[0];
			return _ReturnValue;
		}

		[StoredProcedure(typeof(Test1DbContext), "dbo.spGetNamesAndCityNames")]
		[ResultType(typeof(spGetNamesAndCityNamesResult2))]
		public IMultipleResult<spGetNamesAndCityNamesResult> spGetNamesAndCityNames()
		{
			var _ReturnValue = this.ExecuteMultipleResultProcedure<spGetNamesAndCityNamesResult>();
			return _ReturnValue;
		}

        #region BuiltinFunctions

        [BuiltInFunction("LEFT")]
        public string LEFT(string expression, int? length)
        {
            return this.ExecuteBuiltInFunction<string>(expression, length);
        }
        [BuiltInFunction("RIGHT")]
        public string RIGHT(string expression, int? length)
        {
            return this.ExecuteBuiltInFunction<string>(expression, length);
        }
        [BuiltInFunction("REVERSE")]
        public string REVERSE(string expression)
        {
            return this.ExecuteBuiltInFunction<string>(expression);
        }

        #endregion
        #region NiladicFunctions

        [NiladicFunction("CURRENT_USER")]
        public string CURRENT_USER()
        {
            return this.ExecuteNiladicFunction<string>();
        }
        [NiladicFunction("@@ERROR")]
        public int? ERROR()
        {
            return this.ExecuteNiladicFunction<int?>();
        }

        #endregion
    }
}
