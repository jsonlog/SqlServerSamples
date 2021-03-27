using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    public class DatabaseInstallInfo
    {
        public bool CanInstall { get; internal set; }
        public bool ShouldInstall { get; set; }
        public string DatabaseName { get;  internal set; }
        public string Status { get; internal set; }
        public string MoreInformation { get; internal set; }

        internal DatabaseManifestDatabase DatabaseManifest { get; set; }

    }
}
