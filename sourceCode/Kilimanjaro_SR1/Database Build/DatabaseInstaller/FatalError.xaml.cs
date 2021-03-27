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

namespace Microsoft.SqlServer.DatabaseInstaller
{
    /// <summary>
    /// Interaction logic for FatalError.xaml
    /// </summary>
    public partial class FatalErrorWindow : Window
    {
        public string ErrorMessage { get; set; }
        public FatalErrorWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnInitialized(e);
            if (ErrorMessage == null)
                ErrorMessage = "Unknown failure.";
            ErrorTextBlock.Text = ErrorMessage;
        }

        public static void ReportFatalError(string message)
        {
            FatalErrorWindow newWindow = new FatalErrorWindow();
            newWindow.ErrorMessage = message;
            newWindow.Show();
            newWindow.Activate();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(-2);
        }
    }
}
