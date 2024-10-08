﻿using Azure.ScannerEUI.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Azure.ScannerEUI.View
{
    /// <summary>
    /// ChemiSOLOView.xaml 的交互逻辑
    /// </summary>
    public partial class ChemiSOLOView : UserControl
    {
        public ChemiSOLOView()
        {
            InitializeComponent();
            this.Loaded += ChemiSOLOView_Loaded;
        }

        private void ChemiSOLOView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = Workspace.This.ChemiSOLOViewModel;
        }
    }
}
