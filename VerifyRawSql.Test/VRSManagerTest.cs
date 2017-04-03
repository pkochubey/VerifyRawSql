using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyRawSql.Manager;
using System.IO;

namespace VerifyRawSql.Test
{
    [TestClass]
    public class VRSManagerTest
    {
        [TestMethod]
        public void GetListOfClearSqlExpression_Test()
        {
            var sqlRows = VRSManager.GetListOfClearSqlExpression(File.ReadAllText(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "TestingFile.cs")), "fileName", "projectName");
            Assert.IsTrue(sqlRows.Count == 8, "Found not all SQL rows.");
        }

        [TestMethod]
        public void StaticAtalyse_Test()
        {
            var sqlRows = VRSManager.GetListOfClearSqlExpression(File.ReadAllText(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "TestingStaticAnalyseFile.cs")), "fileName", "projectName");
            var errorsCount = 0;
            foreach (var row in sqlRows)
            {
                var sqlErrors = VRSManager.StaticAnalyse(row.Value);
                errorsCount += sqlErrors.Count;
            }
            Assert.IsTrue(sqlRows.Count == 3, "Found not all SQL rows.");
            Assert.IsTrue(errorsCount == 3, "Found not all issues in SQL code.");
        }
    }
}
