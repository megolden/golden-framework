using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Data.Entity;
using Golden.Data.Extensions;
using System.Data;
using System.Collections.Generic;

namespace Golden.Tests
{
	[TestClass]
    public partial class GoldenDataExtensionsTests
    {
        [TestMethod]
        public void DataContext()
        {
			using (var db = new DBTestDbContext(@"Data Source=localhost;Initial Catalog=DBTest;Integrated Security=True"))
            {
                goto NewTests;

                //delete record(s) with query.
                var deleteResult = db.City
                    .Where(i => i.Id == 60000)
                    .DeleteDirectly(i => i.Name == null);

                //update partial fields record(s) with query.
                var updateResult = db.Student
                    .Where(i => i.Id == 40000)
                    .UpdateDirectly(() => new Student
                    {
                        Name = "Mohsen", 
                        CityRef = null,
                        BirthDate = DateTime.Today,
                    });

                //call db scalar function
                var fnResult = db.fnGetAgeYear(2005);

                //using db scalar function in query
                var fnResult2 = db.Student.Where(i => i.Id < db.fnGetAgeYear(i.Id)).ToList();

				//execute query on db table-valued function
				var fnResult3 = db.fnGetNames(1).Select(n => n.Name).Where(n => n.StartsWith("m")).ToList();

                //execute query on db table-valued function
                var fnResult4 = db.fnGetStudents().Where(s => s.Id > 10).ToList();

                //using SQL Server built-in function LEFT
                var fnResult5 = db.LEFT("Alireza", 3);

                //using SQL Server built-in function in query
                var fnResult6 = db.fnGetNames(null).Select(r => db.LEFT(r.Name, 3)).ToList();

                //using SQL Server niladic function CURRENT_USER
                var fnResult7 = db.ERROR();

                //using SQL Server niladic function in query
                var fnResult8 = db.fnGetNames(null).Select(r => db.CURRENT_USER()).ToList();

                //call db stored procedure with no result.
                int? id = 700;
                db.spInsertTest(ref id, "Aminzadeh");

				//call db stored procedure with single result.
				int? count = null;
				var spResult = db.spGetStudents(ref count).ToList();

				//call db stored procedure with multiple results.
				string searchKey = "i";
				var spMultiResults = db.spFindNamesAndCityNames(ref searchKey);
				var spMRResult1 = spMultiResults.GetResult<spGetNamesAndCityNamesResult>(0).ToList();
				var spMRResult2 = spMultiResults.GetResult<City>(1).ToList();
				var spMRResult3 = spMultiResults.GetResult<spGetNamesAndCityNamesResult>(2).ToList();

				db.City.Delete(1008);
				db.SaveChanges();

				db.City.Update(() => new City
				{
					Id = 1009, // Key

					Name = "Kerman",
				});
				db.SaveChanges();

                NewTests:

                //var udtResult = db.spTestTypes(Enumerable.Range(1, 10).Select(i => new udtIntArray(i)).ToArray());
                //var udtResult2 = db.fnTestTypes(Enumerable.Range(1, 10).Select(i => new udtIntStringArray(i, "Name: " + i)).ToArray());
                //var udtResult3 = db.fnGetMaxName(Enumerable.Range(1, 10).Select(i => new udtIntStringArray(i, "Ali-" + i)).ToArray());
                var filter = new List<udtKeyValueData>
                {
                    new udtKeyValueData("id", "0", "<>"),
                    //new udtKeyValueData("cityName", "'%ma%'", "LIKE"),
                    new udtKeyValueData("BirthDate", null, "<>"),
                };
                var sorting = new List<udtKeyValue>
                {
                    new udtKeyValue("name")
                };

                var spResult200 = 
                    db.spSearchStudent(filter.ToArray(), sorting.ToArray(), null, null)
                    .ToList();

                Debugger.Break();
            }
        }

        [TestMethod]
        public void Extensions()
        {
            using (var db = new DBTestDbContext(@"Data Source=localhost;Initial Catalog=DBTest;Integrated Security=True"))
            {
                var sourceList = db.City.AsNoTracking().Include(c=>c.Student).ToList();

                var table = sourceList.ToDataTable();

                var myList = table.ToEnumerable<City>();
            }
        }
	}
}
