using Azure.ScannerEUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Azure.ScannerEUI.View
{
    /// <summary>
    /// CameraCreateDarkMasters.xaml 的交互逻辑
    /// </summary>
    public partial class CameraCreateDarkMasters : UserControl
    {
        public CameraCreateDarkMasters()
        {
            InitializeComponent();
            this.Loaded += CameraCreateDarkMasters_Loaded;
        }
        private void CameraCreateDarkMasters_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Workspace.This.CreateDarkmastersViewModel;
        }
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9,.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void _MoreOptions_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height = 365;
        }

        private void _MoreOptions_Expanded(object sender, RoutedEventArgs e)
        {
            this.Height = 560;
        }
    }
}
