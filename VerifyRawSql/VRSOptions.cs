using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using VerifyRawSql.Enums;

namespace VerifyRawSql
{
    public class VRSOptions : DialogPage
    {
        private bool CheckAtSaveDocuemnt = true;
        private bool CheckAtBuildSolution = true;
        private VerificationType VerificationType = VerificationType.Both;
        private string FirstKeywords = "Select, Update, Delete, Insert, With";

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

        [Category(VRSPackage.PackageFullName)]
        [DisplayName("Verification Type")]
        [Description("Static, Dynamic or Both")]
        public VerificationType OptionVerificationType
        {
            get { return VerificationType; }
            set { VerificationType = value; }
        }

        [Category(VRSPackage.PackageFullName)]
        [DisplayName("First Keywords")]
        [Description("Array, separator is ','")]
        public string OptionFirstKeywords
        {
            get { return FirstKeywords; }
            set { FirstKeywords = value; }
        }
    }
}
