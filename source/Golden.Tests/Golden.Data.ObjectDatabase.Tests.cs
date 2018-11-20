using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Data;

namespace Golden.Tests
{
    [TestClass]
    public class GoldenDataObjectDatabaseTests
    {
        private const string ConnectionString = @"Data Source=localhost;Initial Catalog=DBTest;Integrated Security=SSPI";

        [TestMethod]
        public void Test()
        {
            var db = Data.ObjectDatabase.Database.FromConnectionString(ConnectionString);

            //var srv = new Data.ObjectDatabase.Server("localhost");
            //var db = srv.Database("DBTest");

            goto NewTests;

            var table = db.Table("Student", "dbo");
            var foreignKeys = table.ForeignKeys.ToList();
            var column = table.Columns.First();
            Debug.WriteLine(column.DataType.GetClrType().FullName);

            //var views = db.Views.ToList();

            NewTests:

            var schemaList = db.Schemas.ToList();

            //var udTypes = db.UserDefinedTypes.ToList();

            var sp1 = db.StoredProcedure("spSearchStudent");
            //var spRets = sp1.OutputResults.ToList();
            var paramss = sp1.Parameters.ToList();

            var spResults = db.StoredProcedures[2].OutputResults;
            var fnTableValued = db.UserDefinedFunctions[0].Columns;

            string qResult = (string)db.ExecuteScalar("SELECT @Name", new[] { new SqlParameter("@Name", "Golden") });

            int qResult2 = db.ExecuteNonQuery("UPDATE [Student] SET [Name] = NULL WHERE [Id] = -1");

            db.ExecuteReader("SELECT [Name] FROM [Student]", reader =>
            {
                while (reader.Read())
                {
                    Debug.WriteLine(reader.GetString(0));
                }
            });

            DataTable students = db.ExecuteWithResult("SELECT * FROM [Student] WHERE [Id] > @Id", new[] { new SqlParameter("@Id", 2) });

            DataSet someData = db.ExecuteWithResults(
                "SELECT * FROM [Student] WHERE [Id] > @Id;" +
                "SELECT * FROM [Book] WHERE [Id] > @Id",
                new[] { new SqlParameter("@Id", 2) });


            Debugger.Break();
        }
    }
}
