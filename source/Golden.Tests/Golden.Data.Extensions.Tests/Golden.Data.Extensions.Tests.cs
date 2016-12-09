using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Golden.Data.Extensions;
using System.Linq;

namespace Golden.Tests
{
    [TestClass]
    public class GoldenDataExtensionsTests
    {
        [TestMethod]
        public void DataContext()
        {
            using (var db = DbContextUtilities.Create<Test1DbContext>("localhost", "Test1"))
            {
                //delete record(s) with query.
                var deleteResult = db.City
                    .Where(i => i.Id == 60000)
                    .Delete();

                //update partial fields record(s) with query.
                var updateResult = db.Student
                    .Where(i => i.Id == 40000)
                    .Update(i => new
                    {
                        Name = "Mohsen",
                        CityRef = (int?)null,
                        BirthDate = DateTime.Today,
                    });

                //call db scalar function
                var fnResult = db.fnGetAgeYear(2005);

                //using db scalar function in query
                var fnResult2 = db.Student.Where(i => i.Id < db.fnGetAgeYear(i.Id)).ToList();

                //execute query on db table-valued function
                var fnResult3 = db.fnGetNames().Select(n => n.Name).Where(n => n.StartsWith("m")).ToList();

                //execute query on db table-valued function
                var fnResult4 = db.fnGetStudents().Where(s => s.Id > 10).ToList();

                //using SQL Server built-in function LEFT
                var fnResult5 = db.LEFT("Alireza", 3);

                //using SQL Server built-in function in query
                var fnResult6 = db.fnGetNames().Select(r => db.LEFT(r.Name, 3)).ToList();

                //using SQL Server niladic function CURRENT_USER
                var fnResult7 = db.ERROR();

                //using SQL Server niladic function in query
                var fnResult8 = db.fnGetNames().Select(r => db.CURRENT_USER()).ToList();

                //call db stored procedure with no result.
                int? id = 700;
                db.spInsertTest(ref id, "Aminzadeh");

                //call db stored procedure with single result.
                int? count = null;
                var spResult = db.spGetStudents(ref count).ToList();

                //call db stored procedure with multiple results.
                var spMultiResult = db.spGetNamesAndCityNames();
                var spMRResult = spMultiResult.ToList();
                var spMRResult2 = spMultiResult.GetNextResult<spGetNamesAndCityNamesResult2>().ToList();

                Debugger.Break();
            }
        }
    }
}
