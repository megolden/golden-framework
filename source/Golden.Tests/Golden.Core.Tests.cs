using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Golden.Data.Extensions;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Linq;
using System.Data.Entity;
using System.Reflection;
using System.Data;

namespace Golden.Tests
{
    [TestClass]
    public class GoldenCoreTests
    {
        [TestMethod]
        public void Utilities()
        {
            object value = DBNull.Value;
            decimal? result = Utility.Utilities.Convert<decimal?>(value, true); // null

            bool result2 = Utility.Utilities.CheckInternetConnection();

            string result3 = Utility.Utilities.ConvertToPersian("A123"); // A۱۲۳
            string enResult = Utility.Utilities.GetEnglishNumberName(1265); //One Thousand Two Hundred Sixty Five
            string faResult = Utility.Utilities.GetPersianNumberName(1265); //هزار و دویست و شصت و پنج

            try
            {
                DateTime _NIST_LocalTime = Utility.Utilities.GetNistInternetTime();
            }
            catch { }

            bool bitResult = Utility.Utilities.HasFlag(value: 3, flag: 1); // a & b == b
            bool bitResult2 = Utility.Utilities.HasFlag(AttributeTargets.All, AttributeTargets.Class);

            bool betResult = Utility.Utilities.IsBetween(value: 5, minValue: 1, maxValue: 5, includeMaxBound: true); // true

            var inResult = Utility.Utilities.IsIn(value: 2, items: new[] { 3, 4, 5, 2, 0 }); // true

            var bitsValue = Utility.Utilities.ConvertBits<byte>(true, false, true); // 00000101 = 5

            Debugger.Break();
        }

        [TestMethod]
        public void BinaryExpression()
        {
            //Creates an instance of BinaryExpression with initial value 13
            var be1 = new BinaryExpression("1101");
            //be1.Length equals to 4 (number of bits) 

            //Creates an instance of BinaryExpression with initial value 11
            BinaryExpression be2 = (short)11;
            //be2.Length equals to 16 (number of 'short' type bits) 
            var be2_str_2 = be2.ToString();

            //Creates an instance of BinaryExpression with array of bytes constructor parameter.
            var be3 = new BinaryExpression(new byte[] { 128, 58, 15 });
            //be3.Length equals to 24

            //Gets boolean value of bit with specified index.
            bool be1_bit_3 = be1[2];

            be1[2] = false;
            //be1 = 1001 = 9

            int be1_be2_cmp = be1.CompareTo(be2); // = -1, 9 < 11

            bool be1_IsAllTrue = be1.IsAll(true); // = false

            //be2 = 00000000 00001011 = 11, Length = 16
            be2.Length = 10;
            //be2 = 00 00001011 = 11, Length = 10

            be2.Trim();
            //be2 = 1011 = 11, Length = 4

            be1.And((byte)3);
            //be1 = 00000001 = 1, Length = 8

            be1 |= new BinaryExpression("11110000"); // be1.Or
                                                     //be1 = 11110001 = 241, Length = 8

            be3 = be1 << 2; //Shift bits to the left
                            //be3 = 11000100 = 196, Length = 8
            be3 = be1.RotateLeft(2); //Shift circular bits to the left
                                     //be3 = 11000111 = 199, Length = 8

            be1.Not().Trim();
            //be1 = 1110 = 14, Length = 4

            string binStr = be1.ToString(2); //Base: 2, 8, 10, 16
                                             //binStr = "1110"

            bool[] bitValues = be1.ToArray();
            //bitValues = [false, true, true, true]
        }

        [TestMethod]
        public void ObjectQueryableTests()
        {
            //Create test IEnumerable data source.
            var enumDataSource = new List<Student>
            {
                new Student { Id = 0, Name = "Alireza", BirthDate = DateTime.Today.AddMonths(-50) },
                new Student { Id = 1, Name = "Mohammad", BirthDate = DateTime.Today.AddMonths(-70) },
                new Student { Id = 2, Name = "Zahra", BirthDate = DateTime.Today.AddMonths(-10) },
                new Student { Id = 3, Name = "Mohsen", BirthDate = DateTime.Today.AddMonths(-20) },
                new Student { Id = 4, Name = "Ali", BirthDate = DateTime.Today.AddMonths(-31) },
                new Student { Id = 5, Name = "Amin", BirthDate = DateTime.Today.AddMonths(-8) },
                new Student { Id = 6, Name = "Hosein", BirthDate = DateTime.Today.AddMonths(-5) },
                new Student { Id = 7, Name = "Hasan", BirthDate = DateTime.Today.AddMonths(-20) },
                new Student { Id = 8, Name = "Hamid", BirthDate = DateTime.Today.AddMonths(-41) },
                new Student { Id = 9, Name = "Reza", BirthDate = DateTime.Today.AddMonths(-62) },
                new Student { Id = 10, Name = "Rahim", BirthDate = DateTime.Today.AddMonths(-20) },
                new Student { Id = 12, Name = "ali", BirthDate = DateTime.Today.AddMonths(-21) },
            };
            
            //These variables values can be filled from user interface controls.
            bool nameOrdering = false;
            bool nameFiltering = true;
            //...

            IQueryable<Student> objQuery = new ObjectQueryable<Student>()
                .SetUserData("Scenario", "Edit")
                .SetUserData("CanRelease", true)
                .Where<Student, string>("Name", name => name.StartsWith("Ha"))
                .Sort(" Name asc , id descendnig")
                ;

            //objQuery = objQuery.TakePage(page: 2, count: 10);

            var parameters = ObjectQueryable.GetUserData(objQuery);
            var list = enumDataSource.ApplyQuery(objQuery).ToList();

            Debugger.Break();

            //Using string extension methods.
            objQuery = objQuery.Where(c => c.Name.Reverse().Left(100).Right(50) == "");

            if (nameOrdering)
            {
                objQuery = objQuery.OrderBy(c => c.Name).ThenByDescending(c => c.Id);
            }

            //Create a new instance of ObjectQuery from previous instance and new result type.
            var newObjQuery = objQuery.Select(c => new
            {
                c.Id,
                Name = c.Name.Left(2),
                UId = c.Name + c.Id
            })
            .Where(i => i.Name.Length > 0 && i.UId.StartsWith("H"))
            .GroupBy(i => i.UId);

            //Applying to IEnumerable data source.
            var result = enumDataSource.ApplyQuery(newObjQuery).ToList();

            Debugger.Break();
        }

        [TestMethod]
        public void DataObjectQueryableTests()
        {
            //Applying for IQueryable data source.
            using (var db = DbContextUtilities.Create<DBTestDbContext>("localhost", "DBTest"))
            {
                var dbQuery =
                    new ObjectQueryable<Student>()
                    .SetUserData("Includes", "City")
                    .Where(c => c.Name.Like("%li%"))
                    //.Where(c => c.Id < 3)
                    //.Where(c => c.Name.Left(1) == "S")
                    //.Where(c=>c.Name.Reverse().Right(1) != "1")
                    //.Where(c => c.BirthDate.Value.Date == DateTime.Now.Date)
                    //.Sort("Name DESC, Id ASC")
                    //.Where<Student, string>("Name", (s, name) => name != s.Id.ToString())
                    //.Where<Student, string>("Name", name => name.Contains("m"))
                    //.Skip(countAccessor: () => db.CURRENT_USER().Length) // Call 'Skip' method with an expression as count parameter.
                    //.OrderBy(c => c.Id).TakePage(page: 5, count: 10) // Take page 5 (size of each page is 10)
                    ;

                var parameters = dbQuery.GetUserData();
                var dataResult = db.Student.AsNoTracking().Include(parameters["Includes"].ToString()).ApplyDataQuery(dbQuery).ToList();

                Debugger.Break();
            }
        }

        [TestMethod]
        public void Extensions()
        {
            #region String

            string result = "Debugger".Left(5); // = "Debug"
            result = "Debug".Left(10); // = "Debug"
            bool cResult = "You are fine".Contains("FINE", StringComparison.OrdinalIgnoreCase); // = true
            result = "You are fine".Replace(startIndex: 8, length: 4, newValue: "good"); // = Are you good ?
            result = "Hello".Reverse(); // = olleH
            result = "You".Append(" are").Append(null).Append(" very", " fine"); // = You are very fine
            result = "".EmptyAsNull(); // = null
            result = "  \t".EmptyAsNullWhiteSpace(); // = null
            cResult = result.IsNullOrEmpty();
            cResult = "golden".EqualsOrdinal("GOLDEN", ignoreCase: true); // = true
            int cmdResult = "golden".CompareOrdinal("GOLDEN", ignoreCase: true); // = 0

            result = "You are fine today";
            KeyValuePair<int, int> iaResult = result.IndexOfAny(values: new[] { "YOU", "are", "today", "good" }, comparisonType: StringComparison.Ordinal);
            // iaResult = { Key = 4, Value = 1 }
            //Key: Index of "are" string in 'Result' variable value.
            //Value: Index of "are" item in input strings('values' parameter).
            //Key and Value = -1, if any values not found.

            var strFormat = "Hello {Id}: {Name}".Format(new { Id = 10, Name = "Reza" });

            #endregion
            #region Enumerable

            ICollection<int?> coll = new ObservableCollection<int?>();
            coll.AddRange(new int?[] { 1, 2, null, 5, 3, null, 3 });

            var nonNulls = coll.ExcludeNull();

            nonNulls.ForEach(v => Debug.WriteLine(v));

            int? fi;
            if (coll.Any(i => i > 3, out fi))
            {
                Debug.WriteLine(fi.Value); // 5
            }

            nonNulls.Distinct(ni => ni.Value); // = { 1, 2, 5, 3 }
            //nonNulls.Distinct(EqualityComparer<int?>.ByProperty(ni => ni.Value)); // = { 1, 2, 5, 3 }
            nonNulls.Except(new int?[] { 5, 3 }, ni => ni.Value); // = { 1, 2 }
            nonNulls.Intersect(new int?[] { 5, 3, 0 }, ni => ni.Value); // = { 5, 3 }

            #endregion

            Debugger.Break();
        }

        [TestMethod]
        public void PersianDateTimeTests()
        {
            //PersianDateTime is utility data type for GregorianDateTime to/from PersianDateTime conversions.

            var pNow = PersianDateTime.Now;

            var clrDate = DateTime.Now;
            //Convert an instance of CLR-DateTime to PersianDateTime
            PersianDateTime faDateTime = clrDate.ToPersianDateTime();
            //Convert an instance of PersianDateTime to CLR-DateTime
            clrDate = faDateTime.ToDateTime();

            Debug.WriteLine(faDateTime.Hour12); // Hour based on 12Hours
            Debug.WriteLine(faDateTime.IsAfternoon); // ...

            //Convert persian date time string to PersianDateTime
            faDateTime = PersianDateTime.Parse("1395/10/1 5:1 ب.ظ");

            //writes name of week day
            Debug.WriteLine("MonthName: {0}\nDayName: {1}", faDateTime.MonthName, faDateTime.DayName);

            //well form date time display :-)
            Debug.WriteLine(PersianDateTime.Parse("1395/10/19").ToString("dddd d MMMM yyyy")); // یکشنبه 19 دی 1395

            Debugger.Break();
        }

        [TestMethod]
        public void ResourceManager()
        {
            var resourceManager = new EmbeddedResourceManager(typeof(GoldenCoreTests).Assembly, "Golden.Tests.Resources");

            var xSampleData = resourceManager.GetFileAsText("Files/SampleData.xml");
            var marks =
                XDocument.Parse(xSampleData).Root
                .Elements("Mark")
                .Select(e => float.Parse(e.Value))
                .ToArray();

            var winIcon = resourceManager.GetIcon("Icons/Home.ico", size: 128);
            var winImage = resourceManager.GetImage("Images/VS2010_256.png");

            var wpfIcon = resourceManager.GetIconImageSource("Icons/Home.ico");
            var wpfImage = resourceManager.GetImageSource("Images/VS2010_256.png");

            Debugger.Break();
        }

        [TestMethod]
        public void EqualityComparerTest()
        {
            var srcList = new List<City>
            {
                new City { Id = 1, Name = "A" },
            };
            var desList = new List<City>
            {
                new City { Id = 1, Name = "B" },
                new City { Id = 2, Name = "M" },
                new City { Id = 3, Name = "N" },
            };

            var diff = desList.Except(srcList, i => i.Id).ToList();

            Debugger.Break();
        }
    }
}
