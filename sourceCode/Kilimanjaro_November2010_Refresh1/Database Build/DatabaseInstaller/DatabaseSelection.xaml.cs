using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Microsoft.SqlServer.DatabaseInstaller
{
	/// <summary>
	/// Interaction logic for DatabaseSelection.xaml
	/// </summary>
	public partial class DatabaseSelectionWindow : Window
	{

        public static readonly DependencyProperty AllServerInstancesProperty =
            DependencyProperty.Register("AllServerInstances", typeof(List<SqlServerInstance>),
            typeof(DatabaseSelectionWindow));

        public List<SqlServerInstance> AllServerInstances
        {
            get { return (List<SqlServerInstance>)GetValue(AllServerInstancesProperty); }
            set { SetValue(AllServerInstancesProperty, value); }
        }

        public static readonly DependencyProperty AllDatabaseInstallInfoProperty =
    DependencyProperty.Register("AllDatabaseInstallInfo", typeof(List<DatabaseInstallInfo>), typeof(DatabaseSelectionWindow));

        public List<DatabaseInstallInfo> AllDatabaseInstallInfo
        {
            get { return (List<DatabaseInstallInfo>)GetValue(AllDatabaseInstallInfoProperty); }
            set { SetValue(AllDatabaseInstallInfoProperty, value); }
        }

        private SqlServerInstance _CurrentInstance = null;
        private string _ManifestPath;
        private App _CurrentApplication;
        private int _PreviousMajor = 0;
        private int _PreviousMinor = 0;

        private Browser _InternetBrowser = new Browser();

		public DatabaseSelectionWindow()
		{
			this.InitializeComponent();

            if (AllDatabaseInstallInfo == null)
            {
                AllDatabaseInstallInfo = new List<DatabaseInstallInfo>();
            }
		}

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _CurrentApplication = (App)(Application.Current);
            AllServerInstances = _CurrentApplication.ServerInstances;
            InstanceComboBox.SelectedIndex = (AllServerInstances.Count == 0) ? -1 : 0;
            if (AreNoInstallableDatabases())
            {
                MaybeFindDifferentDefaultInstance();
            }
            InstallerTitleTextBlock.Text = Properties.Resources.InstallerTitle;

            if (AllServerInstances.Count == 0)
            {
                if (ScriptInstallDirectoryTextBox.Text.Length == 0)
                {
                    string roamingDir = InstallationExecutionWindow.EnsureTrailingDirectorySeperator(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + "SQLServerSamples\\";
                    if (!Directory.Exists(roamingDir))
                        Directory.CreateDirectory(roamingDir);

                    ScriptInstallDirectoryTextBox.Text = roamingDir;
                }
            }

        }

        const string ManifestPathPattern = "DatabaseManifest-{0}-{1}.xml";

        private DatabaseManifest GetDatabaseManifest(int majorVersion, int minorVersion)
        {
            XmlSchema specSchema = new XmlSchema();
            XmlSerializer serializer = new XmlSerializer(typeof(DatabaseManifest));
            XmlReaderSettings settings = new XmlReaderSettings();

            try
            {

                _ManifestPath = string.Format(ManifestPathPattern, majorVersion, minorVersion);
                //If we can't find a manifest for this version of SQL Server, but we know that this
                //version of SQL Server is compatible with the primary version for this installer,
                //go ahead and use the primary version if a manifest for it exists.
                if (!File.Exists(_ManifestPath) && Properties.Resources.CompatibleVersions.Length > 0)
                {
                    string[] versionTuple;
                    foreach (string compatibleVersion in Properties.Resources.CompatibleVersions.Split(','))
                    {
                        versionTuple = compatibleVersion.Split('.');
                        if (versionTuple.Length == 2 && majorVersion == int.Parse(versionTuple[0])
                            && minorVersion == int.Parse(versionTuple[1]))
                        {
                            versionTuple = Properties.Resources.PrimaryVersion.Split('.');
                            if (versionTuple.Length == 2
                                && File.Exists(string.Format(ManifestPathPattern, versionTuple[0], versionTuple[1])))
                            {
                                return GetDatabaseManifest(int.Parse(versionTuple[0]), int.Parse(versionTuple[1]));
                            }
                        }
                    }
                }


                using (StringReader schemaReader = new StringReader(Properties.Resources.DatabaseManifestSchema))
                {
                    settings.Schemas.Add(XmlSchema.Read(schemaReader, new ValidationEventHandler(SchemaValidationEventHandler)));
                }
                settings.ValidationEventHandler += new ValidationEventHandler(ManifestValidationEventHandler);
                settings.ValidationType = ValidationType.Schema;
                using (StreamReader reader = File.OpenText(_ManifestPath))
                {
                    using (XmlReader xReader = XmlReader.Create(reader, settings))
                    {
                        return (DatabaseManifest)(serializer.Deserialize(xReader));
                    }
                }
            }
            //Caller should be expecting this exception and handling it.
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                FatalErrorWindow.ReportFatalError(string.Format("The error below occurred during the acquisition of the database installer manifest.  Your installer may be corrupt.  Try to download and install again. \n{0}", e.Message));
                //Dead code since ReportFatalError never returns, but it makes the compiler happy.
                return null;
            }
        }

        private void SchemaValidationEventHandler(object sender, ValidationEventArgs e)
        {
            FatalErrorWindow.ReportFatalError(string.Format("Syntax error in AmplificationSpec.xsd: {0} \n\nYour installer may be corrupt.  Try to download and install again.", e.Message));
        }

        private void ManifestValidationEventHandler(object sender, ValidationEventArgs e)
        {
            FatalErrorWindow.ReportFatalError(string.Format("Database installer manifest {0} contains the following XSD schema validation error: {1}",
                _ManifestPath, e.Message));
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallationExecutionWindow installWindow = new InstallationExecutionWindow()
                {
                    ServerInstance = (SqlServerInstance)InstanceComboBox.SelectedItem,
                    AllDatabaseInstallInfo = this.AllDatabaseInstallInfo,
                    ScriptInstallDirectory = ScriptInstallDirectoryTextBox.Text
                };
            installWindow.Show();
            installWindow.Activate();
            this.Close();
        }

        private void ScriptInstallDirectoryBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                ScriptInstallDirectoryTextBox.Text = folderDialog.SelectedPath;
        }

        private void InstanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                SelectInstance((SqlServerInstance)e.AddedItems[0]);
            }
        }

        const string MissingManifestUrl = @"http://msftdbprodsamples.codeplex.com/wikipage?title=Missing%20Manifest";
        private void SelectInstance(SqlServerInstance instance)
        {
            if (instance == _CurrentInstance) return;
            _CurrentInstance = instance;
            try
            {
                this.Cursor = Cursors.Wait;
                List<DatabaseInstallInfo> allInstallInfo = new List<DatabaseInstallInfo>();

                try
                {
                    _CurrentApplication.InstallerManifest = GetDatabaseManifest(instance.MajorVersion, instance.MinorVersion);
                }
                catch (FileNotFoundException)
                {
                    allInstallInfo.Add(new DatabaseInstallInfo()
                    {
                        DatabaseName = "None Available",
                        CanInstall = false,
                        ShouldInstall = false,
                        Status = string.Format("{0} required, found v{1}.{2}", 
                            Properties.Resources.PrettyCompatibleSqlVersions,
                                            instance.MajorVersion, instance.MinorVersion),
                        MoreInformation = MissingManifestUrl
                    });
                    AllDatabaseInstallInfo = allInstallInfo;
                    return;
                }
                DatabaseManifest manifest = _CurrentApplication.InstallerManifest;

                DatabaseInstallInfo newInfo;
                string validationStatus;
                string moreInfo;
                foreach (DatabaseManifestDatabase database in manifest.Database)
                {
                    newInfo = new DatabaseInstallInfo()
                     {
                         CanInstall = instance.CheckValidDatabase(manifest, database,
                         out validationStatus, out moreInfo),
                         DatabaseName = database.PrettyName
                     };
                    newInfo.ShouldInstall = newInfo.CanInstall;
                    newInfo.Status = validationStatus;
                    newInfo.MoreInformation = moreInfo;
                    newInfo.DatabaseManifest = database;
                    allInstallInfo.Add(newInfo);
                }
                AllDatabaseInstallInfo = allInstallInfo;
                if (_PreviousMajor != instance.MajorVersion || _PreviousMinor != instance.MinorVersion ||
                    ScriptInstallDirectoryTextBox.Text.Length == 0)
                {
                    string directory = 
                         Environment.GetEnvironmentVariable("ProgramFiles") 
                            + System.IO.Path.DirectorySeparatorChar
                            + _CurrentApplication.InstallerManifest.DefaultScriptInstallPath;
                    //If the parent directory exists (typically ...\tools) but this directory does not
                    //(typically ...\tools\samples) go ahead and create the directory.  This can occur
                    //if client tools aren't installed on the server, for example.
                    if (!Directory.Exists(directory))
                    {
                        DirectoryInfo info = new DirectoryInfo(directory);
                        if (info.Parent.Exists)
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }

                    ScriptInstallDirectoryTextBox.Text = directory;

                    _PreviousMajor = instance.MajorVersion;
                    _PreviousMinor = instance.MinorVersion;
                }
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private bool AreNoInstallableDatabases()
        {
            if (AllDatabaseInstallInfo == null) return true;
            foreach (DatabaseInstallInfo databaseInfo in AllDatabaseInstallInfo)
            {
                if (databaseInfo.CanInstall) return false;
            }
            return true;
        }

        // Try to find an alternative instance which has installable databases.
        private void MaybeFindDifferentDefaultInstance()
        {
            if (AllServerInstances.Count < 2) return;
            foreach (SqlServerInstance alternateInstance in AllServerInstances)
            {
                if (alternateInstance == AllServerInstances[0]) continue;
                SelectInstance(alternateInstance);
                if (!AreNoInstallableDatabases())
                {
                    InstanceComboBox.SelectedItem = alternateInstance;
                    return;
                }
            }
            FatalErrorWindow.ReportFatalError(string.Format("This installer is only compatible with {0}. No such instance was found on this server.  Pick a different installer release in the Releases pane at http://msftdbprodsamples.codeplex.com/Release/ProjectReleases.aspx for other versions of SQL Server.",
                            Properties.Resources.PrettyCompatibleSqlVersions));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelConfirmDialog confirmDialog = new CancelConfirmDialog();
            bool? result = confirmDialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                Environment.Exit(-2);
            }
        }

        private void MoreInformationButton_Click(object sender, RoutedEventArgs e)
        {
            Button moreInfoButton = sender as Button;
            if (moreInfoButton != null)
            {
                Hyperlink link = moreInfoButton.Content as Hyperlink;
                if (link != null)
                {
                    try
                    {
                        _InternetBrowser.Navigate(link.NavigateUri.AbsoluteUri);
                    }
                    catch (System.Exception)
                    {
                        //Do your best but don't crash the installer if IE isn't working.
                    }
                }
            }
        }

        private void ScriptInstallDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string directory = ScriptInstallDirectoryTextBox.Text;
            if (Directory.Exists(directory) 
                && System.IO.Path.IsPathRooted(directory)
                && !directory.TrimStart().StartsWith(@"\\")
                && HasWriteAccess(directory))
            {
                WarningImage.Visibility = Visibility.Hidden;
                WarningTextBlock.Visibility = Visibility.Hidden;
                InstallButton.IsEnabled = true;
            }
            else
            {
                WarningTextBlock.Text = "Specify a local script install directory which exists.";
                WarningTextBlock.Visibility = Visibility.Visible;
                WarningImage.Visibility = Visibility.Visible;
                InstallButton.IsEnabled = false;
            }
        }

        private const string TestFileName = "DatabaseInstallationTestFile__.txt";

        private bool HasWriteAccess(string directory)
        {
            string testPath = InstallationExecutionWindow.EnsureTrailingDirectorySeperator(directory) + TestFileName;
            StreamWriter testWriter = null;
            try
            {
                testWriter = File.CreateText(testPath);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            finally
            {
                if (testWriter != null)
                {
                    try
                    {
                        testWriter.Close();
                        testWriter = null;
                        File.Delete(testPath);
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }

            return true;
        }
	}
}