using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifyRawSql.Models
{
    public class RowDescription
    {
        public string Value { get; set; }
        public int Line { get; set; }
        public string FileName { get; set; }
        public IVsHierarchy HierarchyItem { get; set; }
        public string ProjectName { get; set; }
        public bool HasErrorInStaticAnalyse { get; set; }
    }
}
