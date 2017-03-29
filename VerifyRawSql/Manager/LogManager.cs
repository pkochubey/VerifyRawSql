using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifyRawSql.Manager
{
    internal static class LogManager
    {
        private static DTE _dte;
        private static OutputWindowPane outputWindowPane;

        public static void Initialize(DTE dte)
        {
            _dte = dte;
        }

        public static void AddDebug(string message)
        {
#if DEBUG
            Write("DEBUG", message);
#endif
        }

        public static void AddInfo(string message)
        {
            Write("INFO", message);
        }

        public static void AddWarning(string message)
        {
            Write("WARNING", message);
        }

        public static void AddError(string message)
        {
            Write("ERROR", message);
        }

        private static void Write(string type, string message)
        {
            OutputWindow.OutputString($"{DateTime.Now:HH:mm:ss.fff} {type}: {message}" + Environment.NewLine);
        }

        private static OutputWindowPane OutputWindow
        {
            get
            {
                if (outputWindowPane != null) return outputWindowPane;

                var window = _dte.Windows.Item(Constants.vsWindowKindOutput);
                var outputWindow = (OutputWindow)window.Object;

                for (uint i = 1; i <= outputWindow.OutputWindowPanes.Count; i++)
                {
                    if (outputWindow.OutputWindowPanes.Item(i).Name.Equals(VRSPackage.PackageFullName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        outputWindowPane = outputWindow.OutputWindowPanes.Item(i);
                        break;
                    }
                }

                return outputWindowPane ?? (outputWindowPane = outputWindow.OutputWindowPanes.Add(VRSPackage.PackageFullName));
            }
        }
    }
}
