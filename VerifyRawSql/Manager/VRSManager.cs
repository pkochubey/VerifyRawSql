using Dapper;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
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
        private static Regex _replacementCSharpStringsInterpolate = new Regex(@"{\w+}", RegexOptions.Compiled);
        private static Regex _replacementSqlParameters = new Regex(@"@\w+", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyParameters = new Regex(@"=\s+(?![0-9])(?![a-zA-Z])(?![\S])", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyAndParameters = new Regex(@"[=]\s+and", RegexOptions.Compiled);
        private static Regex _replacementSqlEmptyOrParameters = new Regex(@"[=]\s+or", RegexOptions.Compiled);
        private static string _replacementConstant = "'1'";
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

            if (!IsValidConnectionString())
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

            if (!IsValidConnectionString())
            {
                return;
            }

            LogManager.AddInfo($"Checking for document: {_dte.ActiveDocument.Name}");

            var sqlCommends = GetListOfClearSqlExpression(GetCurrentTextFile(), _dte.ActiveDocument.Name, _dte.ActiveDocument.ProjectItem.ProjectItems.ContainingProject.FileName);
            CheckSqlExpression(sqlCommends);
        }
        private static bool IsValidConnectionString()
        {
            TaskManager.ClearMessage();

            if(_optionsPage.OptionVerificationType == Enums.VerificationType.StaticOnly)
            {
                return true;
            }
            
            ConfigurationManager();

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                TaskManager.AddWarning("Connection string 'VRS' not found. Extension verifying raw SQL not working.", 1, "*.config");
                LogManager.AddWarning("Connection string 'VRS' not found. Extension verifying raw SQL not working.");
                return false;
            }

            LogManager.AddInfo("Connection string: " + _connectionString);
           
            return true;
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

            foreach (var rawSql in allStrings)
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
                        var isValidSqlString = false;
                        var staticErrors = new List<string>();

                        string result = _replacementCSharpParameters.Replace(buffer, _replacementConstant);
                        result = _replacementCSharpStringsParameters.Replace(result, _replacementConstant);
                        result = _replacementSqlParameters.Replace(result, _replacementConstant);
                        result = _replacementCSharpStringsInterpolate.Replace(result, _replacementConstant);
                        result = _replacementSqlEmptyParameters.Replace(result, "= " + _replacementConstant);
                        result = _replacementSqlEmptyAndParameters.Replace(result, "= " + _replacementConstant);
                        result = _replacementSqlEmptyOrParameters.Replace(result, "= " + _replacementConstant);

                        var sqlCommands = new List<string>();
                        if(_optionsPage != null)
                        {
                            sqlCommands = _optionsPage.OptionFirstKeywords.Split(',').Select(x => x.Trim().ToLower()).ToList();
                        }
                        else
                        {
                            sqlCommands = new VRSOptions().OptionFirstKeywords.Split(',').Select(x => x.Trim().ToLower()).ToList();
                        }

                        foreach (var command in sqlCommands)
                        {
                            if (LevenshteinDistance(result.Split(' ')[0], command) < 3)
                            {
                                staticErrors = StaticAnalyse(result);
                                isValidSqlString = true;
                                break;
                            }
                        }

                        IVsHierarchy hierarchyItem = null;
                        if (_ivsSolution != null)
                        {
                            _ivsSolution.GetProjectOfUniqueName(rawSql.ProjectName, out hierarchyItem);
                        }

                        if (!isValidSqlString)
                        {
                            continue;
                        }

                        clearSqlExpression.Add(new RowDescription
                        {
                            Value = result,
                            Line = rawSql.Line,
                            FileName = rawSql.FileName,
                            HierarchyItem = hierarchyItem
                        });
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

            if (_optionsPage.OptionVerificationType == Enums.VerificationType.StaticOnly ||
                _optionsPage.OptionVerificationType == Enums.VerificationType.Both)
            {
                foreach (var sql in clearSqlExpression)
                {
                    var sqlErrors = StaticAnalyse(sql.Value);
                    if (sqlErrors.Count > 0)
                    {
                        var message = sqlErrors.Aggregate((i, j) => i + "\n" + j).ToString();

                        LogManager.AddError($"Message: {message} in file: {sql.FileName} line: {sql.Line}");
                        TaskManager.AddError(message, sql.Line, sql.FileName, sql.HierarchyItem);

                        sql.HasErrorInStaticAnalyse = true;
                    }
                }

                if (_optionsPage.OptionVerificationType == Enums.VerificationType.StaticOnly)
                {
                    return;
                }
            }

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                foreach (var sql in clearSqlExpression.Where(x=>!x.HasErrorInStaticAnalyse))
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

        public static List<string> StaticAnalyse(string sql)
        {
            TSql120Parser parser = new TSql120Parser(false);

            IList<ParseError> errors;
            parser.Parse(new StringReader(sql), out errors);
            if (errors != null && errors.Count > 0)
            {
                List<string> errorList = new List<string>();
                foreach (var error in errors)
                {
                    errorList.Add(error.Message);
                }
                return errorList;
            }

            return new List<string>();
        }

        private static int LevenshteinDistance(string string1, string string2)
        {
            if (string.IsNullOrWhiteSpace(string1) || 
                string.IsNullOrWhiteSpace(string2))
            {
                return 999;
            }

            int diff;
            int[,] m = new int[string1.Length + 1, string2.Length + 1];

            for (int i = 0; i <= string1.Length; i++) { m[i, 0] = i; }
            for (int j = 0; j <= string2.Length; j++) { m[0, j] = j; }

            for (int i = 1; i <= string1.Length; i++)
            {
                for (int j = 1; j <= string2.Length; j++)
                {
                    diff = (string1[i - 1] == string2[j - 1]) ? 0 : 1;

                    m[i, j] = Math.Min(Math.Min(m[i - 1, j] + 1,
                                             m[i, j - 1] + 1),
                                             m[i - 1, j - 1] + diff);
                }
            }
            return m[string1.Length, string2.Length];
        }
    }
}
