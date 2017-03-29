//------------------------------------------------------------------------------
// <copyright file="VRSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Configuration;
using VerifyRawSql.Manager;
using VerifyRawSql.Models;

namespace VerifyRawSql
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(VRSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideOptionPage(typeof (VRSOptions), VRSPackage.PackageFullName, "Options", 0, 0, true)]
    public sealed class VRSPackage : Package
    {
        /// <summary>
        /// VRSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "609d9ab8-de68-4a42-a779-6ec7f95c125e";
        public const string PackageFullName = "Verify Raw SQL";

        private DTE _dte = null;
        private DocumentEvents _solutionEvents = null;
        private BuildEvents _buildEvents = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="VRSPackage"/> class.
        /// </summary>
        public VRSPackage()
        {

        }

        #region Package Members
        protected override void Initialize()
        {
            base.Initialize();

            IServiceContainer serviceContainer = this as IServiceContainer;
            _dte = serviceContainer.GetService(typeof(SDTE)) as DTE;
            _solutionEvents = _dte.Events.DocumentEvents;
            _buildEvents = _dte.Events.BuildEvents;
            _solutionEvents.DocumentSaved += DocumentEvents_DocumentSaved;
            _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;

            LogManager.Initialize(_dte);
            TaskManager.Initialize(this);
            VRSManager.Initialize(this);
        }

        private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            VRSManager.OnBuildBegin();
        }
        private void DocumentEvents_DocumentSaved(EnvDTE.Document Document)
        {
            VRSManager.DocumentSaved(Document);
        }
        #endregion
    }
}
