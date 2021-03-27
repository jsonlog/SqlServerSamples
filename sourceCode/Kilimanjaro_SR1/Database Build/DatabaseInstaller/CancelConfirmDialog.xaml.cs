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
    /// Interaction logic for CancelConfirmDialog.xaml
    /// </summary>
    public partial class CancelConfirmDialog : Window
    {
        public CancelConfirmDialog()
        {
            InitializeComponent();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
