using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Wmi;

namespace Microsoft.SqlServer.DatabaseInstaller
{

    public class SqlServerInstance
    {
        const string DefaultOlapServiceNameRoot = "MSSQLServerOLAPService";
        const string NamedInstanceOlapServiceRoot = "MSOLAP$";
        public string Name { get; set; }
        public string PrettyName { get; private set; }
        public string PrettyVersionName { get; private set; }
        public int MajorVersion { get; private set; }
        public int MinorVersion  { get; private set; }

        public string Edition { get; private set; }
        public bool IsDefault { get; private set; }
        public bool IsOLAP { get; private set; }
        public bool IsFTS { get; private set; }
        public bool IsFileStream { get; private set; }
        public string InstallDirectory { get; private set; }


        public static List<string> ServiceNames = new List<string>();

        //Key is instance name, value is whether full text daemon is running
        public static Dictionary<string, bool> FTSRunning = new Dictionary<string, bool>();

        const string ftsServicePattern = "MSSQLFDLauncher";
        const char namedInstanceMarkerCharacter = '$';

        public static List<SqlServerInstance> FindInstances()
        {
            List<SqlServerInstance> results = new List<SqlServerInstance>();

            ManagedComputer mc = new ManagedComputer();

            foreach (Service sqlService in mc.Services)
            {
                ServiceNames.Add(sqlService.Name);
                if (sqlService.Name.StartsWith(ftsServicePattern, StringComparison.InvariantCultureIgnoreCase)
                    && sqlService.ServiceState == ServiceState.Running)
                {
                    int namedInstanceMarkerPosition = sqlService.Name.IndexOf(namedInstanceMarkerCharacter);
                    FTSRunning.Add((namedInstanceMarkerPosition < 0) ? "MSSQLSERVER"
                        : sqlService.Name.Substring(namedInstanceMarkerPosition + 1), true);

                }

            }

            SqlServerInstance newInstance;

            foreach (ServerInstance si in mc.ServerInstances)
            {
                newInstance = new SqlServerInstance(si.Name);

                //Try to collect information about the instance, but it might be stopped
                //so deal with that possibility by skipping it.
                try
                {
                    newInstance.CollectInformation();
                    results.Add(newInstance);
                }
                catch (Microsoft.SqlServer.Management.Common.ConnectionFailureException)
                {
                }
            }

            return results;
        }

        public SqlServerInstance(string name)
        {
            this.Name = name;
            IsDefault = Name.Equals("MSSQLSERVER", StringComparison.InvariantCultureIgnoreCase);
            PrettyName = IsDefault ? "Default" : name;
        }


        private void CollectInformation()
        {
            Microsoft.SqlServer.Management.Smo.Server sqlServer = new Microsoft.SqlServer.Management.Smo.Server((Name.Equals("MSSQLSERVER", StringComparison.InvariantCultureIgnoreCase))
    ? Environment.MachineName : Environment.MachineName + "\\" + Name);
            this.MajorVersion = sqlServer.VersionMajor;
            this.MinorVersion = sqlServer.VersionMinor;
            this.Edition = sqlServer.Edition;
            this.IsFTS = sqlServer.IsFullTextInstalled && FTSRunning.ContainsKey(this.Name) 
                && FTSRunning[this.Name];
            this.IsFileStream = this.MajorVersion >= 10 
                && sqlServer.FilestreamLevel != FileStreamEffectiveLevel.Disabled;
            this.InstallDirectory = sqlServer.InstallDataDirectory;

            string OlapServiceName = IsDefault 
                ? DefaultOlapServiceNameRoot : NamedInstanceOlapServiceRoot + Name;
            this.IsOLAP = ServiceNames.Contains(OlapServiceName, StringComparer.InvariantCultureIgnoreCase);
        }

        const string DatabasePrerequisitesInfo = @"http://msftdbprodsamples.codeplex.com/wikipage?title=Database%20Prerequisites%20for%20SQL%20Server%202008R2";
        const string AnalysisServicesDatabaseInfo = @"http://msftdbprodsamples.codeplex.com/wikipage?title=Installing%20Analysis%20Services%20Database";

        internal bool CheckValidDatabase(DatabaseManifest databaseManifest, DatabaseManifestDatabase database,
            out string validationStatus, out string moreInfo)
        {
            if (database.IsFileStreamRequired && !this.IsFileStream)
            {
                validationStatus = "File stream not enabled";
                moreInfo = DatabasePrerequisitesInfo;
                return false;
            }
            if (database.IsFTSRequired && !this.IsFTS)
            {
                validationStatus = "Full text search not running";
                moreInfo = DatabasePrerequisitesInfo;
                return false;
            }
            //Dead code right now, but useful later when OLAP installs supported.
            if (database.IsOLAPRequired && !this.IsOLAP)
            {
                validationStatus = "OLAP not available";
                moreInfo = DatabasePrerequisitesInfo;
            }
            //Currently we don't support automatic OLAP installs.
            if (database.IsOLAPRequired)
            {
                validationStatus = "Manually deploy via BIDS after install";
                moreInfo = AnalysisServicesDatabaseInfo;
                return false;
            }

            //TODO: Check for existing DB, check for files from detached DB

            //Otherwise we're ready to roll!
            validationStatus = "Ready to install";
            moreInfo = "";
            return true;
        }
    }
}
