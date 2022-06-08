using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnityShared
{
    /// <summary>
    /// Interaction logic for singleProjet.xaml
    /// </summary>
    public partial class singleProjet : UserControl
    {

        bool _updated = false;

        public singleProjet()
        {
            InitializeComponent();
            bg.Background = Brushes.OrangeRed;
            Path = Path;
        }

        public bool Updated {
            get {
                UpdateColors();
                return _updated;
            }
            set {
                _updated = value;
                UpdateColors();
            }
        }

        private void UpdateColors()
        {
            if (bg != null)
                bg.Background = !Directory.Exists(txtPath.Text) ? Brushes.OrangeRed : Brushes.LightGreen;
            if (codebg != null)
                codebg.Background = _updated ? Brushes.LightGreen : Brushes.Yellow;
        }

        public string Path { get {

                UpdateColors();
                return txtPath.Text;
            }
            internal set { 
                txtPath.Text = value;
                UpdateColors();
            }
        }

        public bool IsTemp { get => (bool)isTemp.IsChecked; set => isTemp.IsChecked = value; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PushGit(this);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow.Pull(this);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // goto
            Helpers.OpenFolder(txtPath.Text, false);
        }

        private void txtPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Updated = false;
        }

    }
}
