using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifyRawSql.Manager
{
    internal static class TaskManager
    {
        private static ErrorListProvider _errorListProvider;
        private static List<ErrorTask> _errorTask = new List<ErrorTask>();
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _errorListProvider = new ErrorListProvider(serviceProvider);
        }

        public static void AddError(string message, int line, string fileName, IVsHierarchy hierarchyItem)
        {
            AddTask(message, TaskErrorCategory.Error, line, fileName, hierarchyItem);
        }

        public static void AddWarning(string message, int line, string fileName, IVsHierarchy hierarchyItem = null)
        {
            AddTask(message, TaskErrorCategory.Warning, line, fileName, hierarchyItem);
        }

        public static void ClearMessage()
        {
            foreach(var task in _errorTask)
            {
                _errorListProvider.Tasks.Remove(task);
            }
        }

        private static void AddTask(string message, TaskErrorCategory category, int line, string fileName, IVsHierarchy hierarchyItem)
        {
            var task = new ErrorTask
            {
                Category = TaskCategory.User,
                ErrorCategory = category,
                Text = message,
                Line = line,
                Document = fileName,
                HierarchyItem = hierarchyItem
            };

            task.Navigate += (sender, e) => {
                LogManager.AddInfo($"Move to task, text: {task.Text}, line: {task.Line}");

                task.Line++;
                _errorListProvider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindCode));
                task.Line--;
            };

            _errorTask.Add(task);
            _errorListProvider.Tasks.Add(task);
            _errorListProvider.Refresh();
            _errorListProvider.Show();
        }
    }
}
