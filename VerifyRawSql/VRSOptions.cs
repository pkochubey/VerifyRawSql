using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace VerifyRawSql
{
    public class VRSOptions : DialogPage
    {
        private bool CheckAtSaveDocuemnt = true;
        private bool CheckAtBuildSolution = true;

   
        [Category(VRSPackage.PackageFullName)]
        [DisplayName("At Saving Document")]
        [Description("Checking expression at saving document")]
        public bool OptionCheckAtSaveDocuemnt
        {
            get { return CheckAtSaveDocuemnt; }
            set { CheckAtSaveDocuemnt = value; }
        }

        [Category(VRSPackage.PackageFullName)]
        [DisplayName("At Building Solution")]
        [Description("Checking expression at building solution")]
        public bool OptionCheckAtBuildSolution
        {
            get { return CheckAtBuildSolution; }
            set { CheckAtBuildSolution = value; }
        }
    }
}
