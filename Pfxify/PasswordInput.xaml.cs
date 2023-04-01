using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pfxify
{
    /// <summary>
    /// Interaction logic for PasswordInput.xaml
    /// </summary>
    public partial class PasswordInput : Window
    {
        public string Password
        {
            get
            {
                return txtPassword.Text;
            }
        }

        public PasswordInput(string fileName)
        {
            InitializeComponent();
            lblTitle.Content = $"Passwort für {fileName}";
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtPassword.SelectAll();
            txtPassword.Focus();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
