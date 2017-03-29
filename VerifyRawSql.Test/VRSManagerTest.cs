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
            Assert.IsTrue(sqlRows.Count == 7, "Found not all SQL rows.");
        }
    }
}
