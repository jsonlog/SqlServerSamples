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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Threading;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            EulaTextBlock.Text = "Microsoft Public License (Ms-PL)\n\n"
                + "This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.\n\n\n"
                + "1. Definitions\n\n"
                + "The terms \"reproduce,\" \"reproduction,\" \"derivative works,\" and \"distribution\" have the same meaning here as under U.S. copyright law.\n\n"
                + "A \"contribution\" is the original software, or any additions or changes to the software.\n\n"
                + "A \"contributor\" is any person that distributes its contribution under this license.\n\n"
                + "\"Licensed patents\" are a contributor's patent claims that read directly on its contribution.\n\n"
                + "2. Grant of Rights\n\n"
                + "(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.\n\n"
                + "(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.\n\n"
                + "3. Conditions and Limitations\n\n"
                + "(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.\n\n"
                + "(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.\n\n"
                + "(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.\n\n"
                + "(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.\n\n"
                + "(E) The software is licensed \"as-is.\" You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.\n\n";
            InstallerTitleTextBlock.Text = Properties.Resources.InstallerTitle;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            try
            {
                DatabaseSelectionWindow nextWindow = new DatabaseSelectionWindow();
                nextWindow.Show();
                nextWindow.Activate();
                this.Close();
            }
            catch (System.Exception err)
            {
                if (err.Message.IndexOf("Failed to connect to server",
                    StringComparison.InvariantCultureIgnoreCase) > -1)
                {
                    FatalErrorWindow.ReportFatalError("Can't connect to the local SQL Server service.  Please check the service status using SQL Server Configuration Manager.");
                }
                else if (err.Message.IndexOf("Could not load file or assembly", StringComparison.InvariantCultureIgnoreCase) > -1
                    && err.Message.IndexOf("Microsoft.SqlServer.SqlWmiManagement", StringComparison.InvariantCultureIgnoreCase) > -1)
                {
                    FatalErrorWindow.ReportFatalError(string.Format("This installer is only compatible with {0}.  No such instance was found on this server.  Pick a different installer release in the Releases pane at http://msftdbprodsamples.codeplex.com/Release/ProjectReleases.aspx for other versions of SQL Server.",
                            Properties.Resources.PrettyCompatibleSqlVersions));
                }
                else
                {
                    FatalErrorWindow.ReportFatalError(err.Message);
                }
                this.Close();
                return;
            }
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

    }
}
