using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Data.Entity;
using Golden.Data.Extensions;
using System.Data;
using System.Collections.Generic;
using Golden.GoldenExtensions;
using System.Linq.Expressions;

namespace Golden.Tests
{
    [TestClass]
    public partial class GoldenDataExtensionsTests
    {
        public class StdRet
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public long RCB { get; set; }
            public int? _TotalRowCount { get; set; }
        }

        [TestMethod]
        public void DataContext()
        {
            using (var db = new DBTestDbContext(@"Data Source=localhost;Initial Catalog=DBTest;Integrated Security=True"))
            {
                //goto NewTests;

                //a simple pagination method
                var pagedResults = db.City.AsNoTracking()
                    .Where(c => c.Name.StartsWith("t"))
                    .OrderBy(c => c.Id)
                    .TakePage(page: 2, count: 2)
                    .SelectNew(c => new // Append optional extra columns(NameLength, _TotalRowCount) to output result (Note: Return type is non generic IQueryable)
                    {
                        NameLength = c.Name.Length,
                        _TotalRowCount = DBTestDbContext.TotalRowCount()
                    })
                    .MapTo<StdRet>() // Map non generic 'IEnumerable' to IEnumerable<StdRet>
                    .ToList();

                Debugger.Break();

                var idQueryExp = ((Expression<Func<Student, bool>>)(std => std.Id < 0))
                    .Or(s => s.Id.IsBetween(10, 20))
                    .Or(s => s.Id.IsIn(30, 40, 50));
                var dbQuery =
                    new ObjectQueryable<Student>()
                    .SetUserData("Includes", "City")
                    //.Where(c => c.Name.Like("%li%"))
                    .Where(idQueryExp)
                    //.Where(c => c.Id < 3)
                    //.Where(c => c.Name.Left(1) == "S")
                    //.Where(c => c.Name.Reverse().Right(1) != "1")
                    //.Where(c => c.BirthDate.Value.Date == DateTime.Now.Date)
                    //.Sort("Name DESC, Id ASC")
                    //.Where<Student, string>("Name", (s, name) => name != s.Id.ToString())
                    //.Where<Student, string>("Name", name => name.Contains("m"))
                    //.Skip(countAccessor: () => db.CURRENT_USER().Length) // Call 'Skip' method with an expression as count parameter.
                    //.OrderBy(c => c.Id).TakePage(page: 5, count: 10) // Take page 5 (size of each page is 10)
                    ;
                var dataResult = db.Student.AsNoTracking()
                    .Include(dbQuery.GetUserData()["Includes"].ToString())
                    .ApplyDataQuery(dbQuery)
                    .ToList();

                Debugger.Break();

                //Additional methods for EF linq
                IQueryable<City> query = new ObjectQueryable<City>();
                query = query.Where(c => c.Id.IsBetween(2, 5));
                query = query.Where(c => c.Id.IsIn(2, 5, 7));
                query = query.Where(c => c.Id.HasFlag(5));
                var retlist = db.City.AsNoTracking().ApplyDataQuery(query).ToList();

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
                int? id = 1006;
                int? cls = null;
                db.spInsertTest(ref id, "Aminzadeh", ref cls);

                //call db stored procedure with single result.
                int? count = null;
                var spResult = db.spGetStudents(ref count).ToList();

                //call db stored procedure with multiple results.
                string searchKey = "i";
                var spMultiResults = db.spFindNamesAndCityNames(ref searchKey);
                var spMRResult1 = spMultiResults.GetResult<spGetNamesAndCityNamesResult>(0).ToList();
                var spMRResult2 = spMultiResults.GetResult<City>(1).ToList();
                //var spMRResult3 = spMultiResults.GetResult<spGetNamesAndCityNamesResult>(2).ToList();

                db.City.Delete(1008);
                db.SaveChanges();

                db.City.Update(() => new City
                {
                    Id = 1009, // Key

                    Name = "Kerman",
                });
                db.SaveChanges();

                NewTests:

                var trans = db.Database.BeginTransaction();
                db.LockTable<Student>();
                trans.Rollback();
                //var udtResult = db.spTestTypes(Enumerable.Range(1, 10).Select(i => new udtIntArray(i)).ToArray());
                //var udtResult2 = db.fnTestTypes(Enumerable.Range(1, 10).Select(i => new udtIntStringArray(i, "Name: " + i)).ToArray());
                //var udtResult3 = db.fnGetMaxName(Enumerable.Range(1, 10).Select(i => new udtIntStringArray(i, "Ali-" + i)).ToArray());
                var filter = new List<udtKeyValueData>
                {
                    new udtKeyValueData("Id", "0", "<>"),
                    new udtKeyValueData("AND"),
                    //new udtKeyValueData("cityName", "%ma%", "LIKE"),
                    new udtKeyValueData("BirthDate", "2009/10/09", "<>"),
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
                var sourceList = db.City.AsNoTracking().Include(c => c.Student).ToList();

                var table = sourceList.ToDataTable();

                var myList = table.ToEnumerable<City>();
            }
        }
    }
}
