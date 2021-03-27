using System;
using System.Collections;
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
using System.Threading;
using System.IO;
using Microsoft.VisualBasic.MyServices;
using Microsoft.Win32;

namespace Microsoft.SqlServer.DatabaseInstaller
{
	/// <summary>
	/// Interaction logic for InstallationExecution.xaml
	/// </summary>
	public partial class InstallationExecutionWindow : Window, ICancel
	{
        public string ScriptInstallDirectory { get; set; }
        public SqlServerInstance ServerInstance { get; set; }
        public List<DatabaseInstallInfo> AllDatabaseInstallInfo { get; set; }
        public DatabaseInstallInfo CurrentDatabaseBeingInstalled { get; set; }

        private bool _IsCancelRequested = false;
        public bool IsCancelRequested
        {
            get
            {
                return _IsCancelRequested;
            }
        }

        private Thread _InstallerThread = null;

		public InstallationExecutionWindow()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
		}

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            InstallerTitleTextBlock.Text = Properties.Resources.InstallerTitle;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (_InstallerThread == null)
            {
                CurrentDatabaseBeingInstalled = null;
                InstallProgressBar.Maximum = AllDatabaseInstallInfo.Count + 2;
                InstallProgressBar.Value = 0;
                MajorStepTextBlock.Text = "Initializing";
                _InstallerThread = new Thread(new ParameterizedThreadStart(this.ExecuteInstall));
                _InstallerThread.Start(null);
                this.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CopyScriptsAndData()
        {
            Microsoft.VisualBasic.Devices.ServerComputer computer = new Microsoft.VisualBasic.Devices.ServerComputer();
            FileSystemProxy fileSystem = computer.FileSystem;
            fileSystem.CopyDirectory(Environment.CurrentDirectory, ScriptInstallDirectory, true);
        }

        private void ExecuteInstall(object ignore)
        {
            Installer oltpInstaller;

            ScriptInstallDirectory = EnsureTrailingDirectorySeperator(ScriptInstallDirectory);

            try
            {
                AddMessage(string.Format("Installation started at {0}.", DateTime.Now.ToString("g")));

                /*
                MaybeSetPCARegistryKey();
                 */

                
                switch (GetScriptCopyStatus())
                {
                    case CopyStatus.ReadyToCopy:
                        UpdateMajorStep("Copying scripts and data files");
                        AddMessage(string.Format("Copying scripts and CSV files to {0}.",
                            ScriptInstallDirectory));
                        if (MaybeHandleCancel()) return;
                        try
                        {
                            CopyScriptsAndData();
                            AddMessage("Script copying succeeded.");
                        }
                        catch (IOException ioe)
                        {
                            if (IsAccessDeniedError(ioe))
                            {
                                AddMessage(string.Format(
                                    "Access to the script install directory you specified ({0}) is denied.  You may have to run the database installer with elevation and/or provide full access to the install directory for the user account running the database installer.",
                                    ScriptInstallDirectory));
                                UpdateMajorStep("Installation Failed");
                                return;
                            }
                        }
                        break;

                    case CopyStatus.AlreadyCopied:
                        AddMessage("Scripts already installed in the specified target location.");
                        break;
                    case CopyStatus.MissingSource:
                        AddMessage("The database installer must be invoked in a directory where the scripts exist, or you must select the directory where the scripts are already installed.  Try downloading from http://msftdbprodsamples.codeplex.com/ again.");
                        UpdateMajorStep("Installation Failed");
                        return;
                    default:
                        AddMessage("Unknown copy status.  Try downloading from http://msftdbprodsamples.codeplex.com/ again.");
                        UpdateMajorStep("Installation Failed");
                        return;
                }
                 

                foreach (DatabaseInstallInfo installInfo in AllDatabaseInstallInfo)
                {
                    if (installInfo.CanInstall)
                    {
                        if (installInfo.ShouldInstall)
                        {
                            CurrentDatabaseBeingInstalled = installInfo;
                            UpdateMajorStep("Installing database " + installInfo.DatabaseName);
                            AddMessage(string.Format("Beginning installation of database {0} on {1}.",
                                installInfo.DatabaseManifest.DatabaseName, DateTime.Now.ToString("g")));

                            oltpInstaller = new Installer()
                            {
                                ScriptPath = ScriptInstallDirectory + installInfo.DatabaseManifest.SourceDatabaseScriptRelativePath,
                                SubstitutionVars = new string[] { "SqlSamplesDatabasePath", "SqlSamplesSourceDataPath" },
                                SubstitutionValues = new string[] { ServerInstance.InstallDirectory + @"\DATA\",
                            ScriptInstallDirectory},
                                DatabaseName = installInfo.DatabaseManifest.DatabaseName,
                                InstanceName = (ServerInstance.IsDefault) ? Environment.MachineName
                                : Environment.MachineName + "\\" + ServerInstance.Name
                            };
                            oltpInstaller.InfoMessage += new ConnectionInfoMessageEventHandler(OnInstallerInfoMessage);
                            if (MaybeHandleCancel()) return;
                            oltpInstaller.Install(this);
                            if (MaybeHandleCancel()) return;
                            AddMessage(string.Format("Finished installation of database {0} on {1}.",
                                installInfo.DatabaseManifest.DatabaseName, DateTime.Now.ToString("g")));
                        }
                        else
                        {
                            UpdateMajorStep("Skipping database " + installInfo.DatabaseName);
                            AddMessage(string.Format("Skipping installation of database {0} because user deselected.",
                                installInfo.DatabaseName));
                        }
                    }
                    else
                    {
                        UpdateMajorStep("Skipping database " + installInfo.DatabaseName);
                        AddMessage(string.Format("Skipping installation of database {0} because {1}.",
                            installInfo.DatabaseName, installInfo.Status
                            ));
                    }

                }
                UpdateMajorStep("Installation Complete");

                AddMessage(string.Format("Installation ended on {0}.", DateTime.Now.ToString("g")));

            }
            catch (System.Exception error)
            {
                AddMessage(string.Format("Error {0}: {1}\nInstallation Terminated.", MajorStepTextBlock.Text, error.Message));
                UpdateMajorStep("Installation Failed");
            }
        }

        /*
        private void MaybeSetPCARegistryKey()
        {
            RegistryKey hiveKey = Registry.CurrentUser;
            RegistryKey rootPcaKey = hiveKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags", true);
            if (rootPcaKey != null)
            {
                //We're running on Vista and PCA is overly paranoid about failing installers.  Disable PCA
                //for this installer.
                RegistryKey persistedPcaKey = rootPcaKey.CreateSubKey(@"Compatibility Assistant\Persisted");
                persistedPcaKey.SetValue(EnsureTrailingDirectorySeperator(Environment.CurrentDirectory)
                    + Properties.Resources.InstallerName, 1, RegistryValueKind.DWord);
            }

        }
         */

        private bool IsAccessDeniedError(IOException ioe)
        {
            string testString;
            foreach (DictionaryEntry de in ioe.Data)
            {
                //A bit heuristic but the framework doesn't give us much choice here.
                testString = de.Value.ToString();
                if (testString.IndexOf("Access to", StringComparison.InvariantCultureIgnoreCase) > -1 
                    && testString.IndexOf("is denied", StringComparison.InvariantCultureIgnoreCase) > -1)
                    return true;
            }
            return false;
        }

        private bool MaybeHandleCancel()
        {
            if (_IsCancelRequested)
            {
                UpdateMajorStep("Installation Cancelled");
                AddMessage(string.Format("Installation cancelled by user on {0}.", DateTime.Now.ToString("g")));
                return true;
            }
            return false;
        }

        public static string EnsureTrailingDirectorySeperator(string path)
        {
            return (path[path.Length - 1] == System.IO.Path.DirectorySeparatorChar) ? path 
                : path + System.IO.Path.DirectorySeparatorChar;
        }

        private void AddMessageInternal(string message)
        {
            SingleMessageTextBlock.Text = LastLine(message.Trim());
            DetailsTextBox.Text += "\n" + message;
            DetailsScrollViewer.ScrollToBottom();
            if (CurrentDatabaseBeingInstalled != null)
                DatabaseProgressBar.Value += 1;
        }

        private string LastLine(string multiLineString)
        {
            int lastNewline = multiLineString.LastIndexOf('\n');
            if (lastNewline < 0)
                return multiLineString;
            else
            {
                if (lastNewline + 1 >= multiLineString.Length && multiLineString.Length > 2)
                {
                    lastNewline = multiLineString.LastIndexOf('\n', lastNewline - 1);
                }
            }
            return multiLineString.Substring(lastNewline + 1);
        }

        public void AddMessage(string message)
        {
            //Need to be careful as this might be called from a different thread.
            Dispatcher.BeginInvoke(new MessageDelegate(this.AddMessageInternal), message);
        }

        private void OnInstallerInfoMessage(object sender, ConnectionInfoMessageEventArgs e)
        {
            AddMessage(e.Message);
        }

        delegate void MessageDelegate(string message);

        private void ReportFatalErrorInternal(string message)
        {
            AddMessageInternal(message);
            FatalErrorWindow.ReportFatalError(message);
            this.Close();
        }

        public void ReportFatalError(string message)
        {
            //Need to be careful as this might be called from a different thread.
            Dispatcher.BeginInvoke(new MessageDelegate(this.ReportFatalErrorInternal), message);
        }

        public void UpdateMajorStep(string message)
        {
            //Need to be careful as this might be called from a different thread.
            Dispatcher.BeginInvoke(new MessageDelegate(this.UpdateMajorStepInternal), message);
        }

        private void UpdateMajorStepInternal(string message)
        {
            if (CurrentDatabaseBeingInstalled != null)
            {
                DatabaseProgressBar.Value = 0;
                if (CurrentDatabaseBeingInstalled.DatabaseManifest.ApproximateMessageCountSpecified)
                {
                    DatabaseProgressBar.Maximum
                        = CurrentDatabaseBeingInstalled.DatabaseManifest.ApproximateMessageCount;
                    DatabaseProgressBar.IsIndeterminate = false;
                    DatabaseProgressBar.Value = 0;
                }
                else
                {
                    DatabaseProgressBar.IsIndeterminate = true;
                }
                
                DatabaseLabel.Visibility = Visibility.Visible;
                DatabaseProgressBar.Visibility = Visibility.Visible;
            }

            //Hack to get message values
            /*
            if (InstallProgressBar.Value > 1)
            {
                AddMessage(string.Format("Completed {0} message count = {1}", MajorStepTextBlock.Text, DatabaseProgressBar.Value));
            }
            */

            if (message.Equals("Installation Complete", StringComparison.InvariantCultureIgnoreCase)
                || message.Equals("Installation Cancelled", StringComparison.InvariantCultureIgnoreCase)
                || message.Equals("Installation Failed", StringComparison.InvariantCultureIgnoreCase))
            {
                if (message.Equals("Installation Failed", StringComparison.InvariantCultureIgnoreCase)
                    && SingleMessageTextBlock.Visibility == Visibility.Visible)
                {
                    SingleMessageTextBlock.Visibility = Visibility.Collapsed;
                    DetailsScrollViewer.Visibility = Visibility.Visible;
                    ShowDetailsButton.Content = "Hide Details";
                }

                DatabaseLabel.Visibility = Visibility.Hidden;
                DatabaseProgressBar.Visibility = Visibility.Hidden;
                InstallProgressBar.Value = InstallProgressBar.Maximum - 1;
                FinishButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }



            MajorStepTextBlock.Text = message;
            InstallProgressBar.Value += 1;
            DatabaseProgressBar.Value = 0;
        }

        private void ShowDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SingleMessageTextBlock.Visibility == Visibility.Visible)
            {
                SingleMessageTextBlock.Visibility = Visibility.Collapsed;
                DetailsScrollViewer.Visibility = Visibility.Visible;
                ShowDetailsButton.Content = "Hide Details";
            }
            else
            {
                DetailsScrollViewer.Visibility = Visibility.Collapsed;
                SingleMessageTextBlock.Visibility = Visibility.Visible;
                ShowDetailsButton.Content = "Show Details";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _IsCancelRequested = true;
            AddMessageInternal("Cancel requested by user.");
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private CopyStatus GetScriptCopyStatus()
        {
            string currentDirectory = EnsureTrailingDirectorySeperator(Environment.CurrentDirectory);
            if (AreScriptFilesPresent(currentDirectory))
            {
                if (currentDirectory.Equals(ScriptInstallDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    return CopyStatus.AlreadyCopied;
                }
                else
                {
                    return CopyStatus.ReadyToCopy;
                }
            }
            else
            {
                if (AreScriptFilesPresent(ScriptInstallDirectory))
                {
                    return CopyStatus.AlreadyCopied;
                }
                else
                {
                    return CopyStatus.MissingSource;
                }
            }
        }

        private bool AreScriptFilesPresent(string directory)
        {
            foreach (DatabaseInstallInfo installInfo in AllDatabaseInstallInfo)
            {
                if (!File.Exists(directory + installInfo.DatabaseManifest.SourceDatabaseScriptRelativePath))
                {
                    return false;
                }
            }
            return true;
        }


    }
    internal enum CopyStatus { ReadyToCopy, AlreadyCopied, MissingSource }
}