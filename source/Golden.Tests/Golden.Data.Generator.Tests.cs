using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Golden.Data.ObjectDatabase;
using System.Linq;

namespace Golden.Tests
{
    [TestClass]
    public class GoldenDataGeneratorTests
    {
        [TestMethod]
        public void CodeGenerator()
        {
            var generator = new Golden.Data.Generator.DbContextGenerator(@"Data Source=localhost;Initial Catalog=Test1;Integrated Security=True");
            generator.ModelNamespace = "MyApp.Data";
            generator.ModelName = "Test1DbContext";
            generator.EntityNamespace = "MyApp.Common.Entities";
            generator.GenerateAssociations = true;
            generator.IndentString = "\t";
            generator.Tables.AddRange(generator.GetDbTables());
            generator.Views.AddRange(generator.GetDbViews());
            generator.StoredProcedures.AddRange(generator.GetDbStoredProcedures());
            generator.UserDefinedFunctions.AddRange(generator.GetDbUserDefinedFunctions());

            generator.EnsureEntityKeys();

            //generator.GenerateToFiles(@"DbContextGenerated");
            var genStrings = generator.GenerateToStrings();

            Debugger.Break();
        }
    }
}
