using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private List<SqlServerInstance> _ServerInstances = null;

        public List<SqlServerInstance> ServerInstances
        {
            get 
            {
                if (_ServerInstances == null)
                {
                    _ServerInstances = SqlServerInstance.FindInstances();
                    _ServerInstances.Sort(new Comparison<SqlServerInstance>(CompareSqlServerInstance));
                }
                return _ServerInstances; 
            }
        }

        internal DatabaseManifest InstallerManifest { get; set; }

        private int CompareSqlServerInstance(SqlServerInstance x, SqlServerInstance y)
        {
            if (x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return 0;
            }
            if (x.IsDefault) return -1;
            if (y.IsDefault) return 1;
            return String.Compare(x.Name, y.Name);
        }

    }
}
