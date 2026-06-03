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

namespace EventMapHpViewer.Views
{
    /// <summary>
    /// SettingsView.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var window = FindAncestor<Window>(this);
            if (window == null) return;
            window.Width = 800;
            window.Height = 600;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(current);
            while (parent != null)
            {
                if (parent is T typed) return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
