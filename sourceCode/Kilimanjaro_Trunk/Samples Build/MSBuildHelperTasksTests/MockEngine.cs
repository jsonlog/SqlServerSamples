using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.SqlServer.Samples.MSBuildHelperTasks
{
    class MockEngine : IBuildEngine
    {
        // HACK This is a very weak mock implementation.
        // HACK Extend logging to write to the build output of VS someday.

        #region IBuildEngine Members

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
        {
            return false;
        }

        public int ColumnNumberOfTaskNode
        {
            get { return 0; }
        }

        public bool ContinueOnError
        {
            get { return false; }
        }

        public int LineNumberOfTaskNode
        {
            get { return 0; }
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            Console.WriteLine("Custom: {0}", e.Message);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            Console.WriteLine("Error: {0}", e.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine("Message: {0}", e.Message);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            Console.WriteLine("Warning: {0}", e.Message);
        }

        public string ProjectFileOfTaskNode
        {
            get { return string.Empty; }
        }

        #endregion
    }
}
