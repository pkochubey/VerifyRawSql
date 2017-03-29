using Dapper;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VerifyRawSql.Models;

namespace VerifyRawSql.Manager
{
    public static class VRSManager
    {
        private static DTE _dte = null;
        private static IVsSolution _ivsSolution = null;
        private static string _connectionString = "";
        private static Regex _regex = new Regex("\"[^\"]*\"", RegexOptions.Compiled);
        private static Regex _replacementCSharpParameters = new Regex("'[^\']*'", RegexOptions.Compiled);
        private static Regex _replacementCSharpStringsParameters = new Regex(@"{\d+}", RegexOptions.Compiled);
        private static Regex _replacementSqlParameters = new Regex(@"@\w+", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyParameters = new Regex(@"=\s+(?![0-9])(?![a-zA-Z])(?![\S])", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyAndParameters = new Regex(@"[=]\s+and", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyOrParameters = new Regex(@"[=]\s+or", RegexOptions.Compiled);
        private static string _replacementConstant = "'1'";
        private static string[] _sqlCommands = { "select ", "update ", "delete ", "insert into" };
        private static VRSOptions _optionsPage;

        public static void Initialize(VRSPackage serviceProvider)
        {
            IServiceContainer serviceContainer = serviceProvider as IServiceContainer;
            _dte = serviceContainer.GetService(typeof(SDTE)) as DTE;
            _ivsSolution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
            _optionsPage = serviceProvider.GetDialogPage(typeof(VRSOptions)) as VRSOptions;
        }
        public static void OnBuildBegin()
        {
            if (!_optionsPage.OptionCheckAtBuildSolution)
            {
                return;
            }

            if (GeneralChecking())
            {
                return;
            }

            LogManager.AddInfo("Checking for all projects");

            var csFiles = FindSolutionItemByName(".cs", true);
            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file.FilePath);
                var sqlCommends = GetListOfClearSqlExpression(content, Path.GetFileName(file.FilePath), file.ProjectPath);
                CheckSqlExpression(sqlCommends);
            }
        }
        public static void DocumentSaved(EnvDTE.Document Document)
        {
            if (!_optionsPage.OptionCheckAtSaveDocuemnt)
            {
                return;
            }

            if (Document.Language != "CSharp")
            {
                return;
            }

            if (GeneralChecking())
            {
                return;
            }

            LogManager.AddInfo($"Checking for document: {_dte.ActiveDocument.Name}");

            var sqlCommends = GetListOfClearSqlExpression(GetCurrentTextFile(), _dte.ActiveDocument.Name, _dte.ActiveDocument.ProjectItem.ProjectItems.ContainingProject.FileName);
            CheckSqlExpression(sqlCommends);
        }
        private static bool GeneralChecking()
        {
            ConfigurationManager();

            TaskManager.ClearMessage();

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                TaskManager.AddWarning("Connection string 'VRS' not found. Extension verifying raw SQL not working.", 1, "*.config");
                LogManager.AddWarning("Connection string 'VRS' not found. Extension verifying raw SQL not working.");
                return true;
            }

            LogManager.AddInfo("Connection string: " + _connectionString);

            return false;
        }
        private static List<string> FindFilesInFolder(ProjectItem item, string pattern)
        {
            var fileList = new List<string>();
            var items = item.ProjectItems.GetEnumerator();
            while (items.MoveNext())
            {
                var currentItem = (ProjectItem)items.Current;
                if (!string.IsNullOrWhiteSpace(currentItem.FileNames[0]))
                {
                    if (currentItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                    {
                        fileList.AddRange(FindFilesInFolder(currentItem, pattern));
                    }
                    else
                    {
                        if (Path.GetExtension(currentItem.Name) == pattern)
                        {
                            fileList.Add(Path.Combine(currentItem.FileNames[0].Replace(Path.GetFileName(currentItem.FileNames[0]), ""), currentItem.Name));
                        }
                    }
                }
            }
            return fileList;
        }
        private static List<FileDescription> FindSolutionItemByName(string pattern, bool recursive)
        {
            List<FileDescription> projectItems = new List<FileDescription>();
            foreach (EnvDTE.Project project in _dte.Solution.Projects)
            {
                var projectItem = FindProjectItemInProject(project, pattern, recursive);
                if (projectItem.Count() > 0)
                {
                    foreach (var item in projectItem)
                    {
                        projectItems.Add(new FileDescription
                        {
                            FilePath = item,
                            ProjectPath = project.FileName
                        });
                    }
                }
            }
            return projectItems;
        }
        private static List<string> FindProjectItemInProject(EnvDTE.Project project, string pattern, bool recursive)
        {
            var fileList = new List<string>();
            if (project.Kind != EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                {

                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                        {
                            if (Path.GetExtension(item.Name) == pattern)
                            {
                                fileList.Add(Path.Combine(item.FileNames[0].Replace(Path.GetFileName(item.FileNames[0]), ""), item.Name));
                            }
                        }
                        else if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder && recursive)
                        {
                            fileList.AddRange(FindFilesInFolder(item, pattern));
                        }
                    }
                }
            }
            else
            {
                // if solution folder, one of its ProjectItems might be a real project
                foreach (ProjectItem item in project.ProjectItems)
                {
                    EnvDTE.Project realProject = item.Object as EnvDTE.Project;

                    if (realProject != null)
                    {
                        fileList.AddRange(FindProjectItemInProject(realProject, pattern, recursive));
                    }
                }
            }

            return fileList;
        }
        private static void ConfigurationManager()
        {
            var appconfigs = FindSolutionItemByName(".config", false);
            foreach (var config in appconfigs)
            {
                ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = config.FilePath;
                try
                {
                    var configManager = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                    if (configManager == null || 
                        configManager.ConnectionStrings == null || 
                        configManager.ConnectionStrings.ConnectionStrings == null)
                    {
                        continue;
                    }

                    var connectionString = configManager.ConnectionStrings.ConnectionStrings["VRS"];
                    if (connectionString != null)
                    {
                        _connectionString = connectionString.ConnectionString;
                        break;
                    }
                }
                catch
                {
                    // can't mapped config file
                }
            }
        }
        private static string GetCurrentTextFile()
        {
            var doc = (EnvDTE.TextDocument)(_dte.ActiveDocument.Object("TextDocument"));
            return doc.StartPoint.CreateEditPoint().GetText(doc.EndPoint);
        }
        public static List<RowDescription> GetListOfClearSqlExpression(string text, string fileName, string projectName)
        {
            var tree = CSharpSyntaxTree.ParseText(text);
            var rawSqlStrings = new List<RowDescription>();
            var clearSqlExpression = new List<RowDescription>();

            var allNodes = ((CompilationUnitSyntax)tree.GetRoot()).DescendantNodes();
            var allStringsArgument = allNodes
                  .Where(x => x.Kind().ToString() == "ArgumentList")
                    .Select(x => new RowDescription
                    {
                        Value = ((ArgumentListSyntax)x).ToFullString().ToString().ToLower(),
                        Line = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                        FileName = fileName,
                        ProjectName = projectName
                    })
                    .ToList();

            var allStringsEquals = allNodes
                     .Where(x => x.Kind().ToString() == "EqualsValueClause")
                     .Select(x => new RowDescription
                     {
                         Value = ((EqualsValueClauseSyntax)x).Value.ToString().ToLower(),
                         Line = x.GetLocation().GetLineSpan().StartLinePosition.Line,
                         FileName = fileName,
                         ProjectName = projectName
                     })
                     .ToList();

            var allStrings = allStringsArgument;
            allStrings.AddRange(allStringsEquals);

            foreach (string command in _sqlCommands)
            {
                rawSqlStrings.AddRange(allStrings.Where(x => x.Value.Contains(command)).ToList());
            }

            foreach (var rawSql in rawSqlStrings)
            {
                var matches = _regex.Matches(rawSql.Value);
                if (matches.Count > 0)
                {
                    var buffer = "";
                    foreach (Match match in matches)
                    {
                        buffer += match.Value.Substring(1, match.Value.Length - 2).Trim() + " ";
                    }
                    if (!string.IsNullOrWhiteSpace(buffer))
                    {
                        string result = _replacementCSharpParameters.Replace(buffer, _replacementConstant);
                        result = _replacementCSharpStringsParameters.Replace(result, _replacementConstant);
                        result = _replacementSqlParameters.Replace(result, _replacementConstant);
                        result = _replacementSqlEmptyParameters.Replace(result, "= " + _replacementConstant);
                        result = _replacementSqlEmptyAndParameters.Replace(result, "= " + _replacementConstant);
                        result = _replacementSqlEmptyOrParameters.Replace(result, "= " + _replacementConstant);

                        foreach (string command in _sqlCommands)
                        {
                            if (result.Contains(command))
                            {
                                IVsHierarchy hierarchyItem = null;
                                if(_ivsSolution != null)
                                {
                                    _ivsSolution.GetProjectOfUniqueName(rawSql.ProjectName, out hierarchyItem);
                                }

                                clearSqlExpression.Add(new RowDescription
                                {
                                    Value = result,
                                    Line = rawSql.Line,
                                    FileName = rawSql.FileName,
                                    HierarchyItem = hierarchyItem
                                });
                                break;
                            }
                        }

                    }
                }
            }

            return clearSqlExpression.GroupBy(x => x.Value).Select(x => x.First()).ToList();
        }
        private static void CheckSqlExpression(List<RowDescription> clearSqlExpression)
        {
            if (clearSqlExpression.Count() == 0)
            {
                return;
            }


            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                foreach (var sql in clearSqlExpression)
                {
                    try
                    {
                        LogManager.AddDebug($"Execute: {sql.Value}");
                        var rawData = db.Query<object>(sql.Value);
                    }
                    catch (SqlException ex)
                    {
                        LogManager.AddError($"Message: {ex.Message} in file: {sql.FileName} line: {sql.Line}");
                        TaskManager.AddError(ex.Message, sql.Line, sql.FileName, sql.HierarchyItem);
                    }
                }
            }
        }
    }
}
