using System;
namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            // string 1
            var sql1 = String.Format("Select1 * From Test1 Where Id = {0}", 0);

            // string 2
            var sql2 = "Select * FromTest2 Where Id = 1";

            // string 2
            var sql3 = "Insrt into table_name";
        }
    }
}