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

namespace HiveUplink
{
    /// <summary>
    /// Interaktionslogik für HiveControl.xaml
    /// </summary>
    public partial class HiveControl : UserControl
    {
        private HivePlugin Plugin { get; }

        public HiveControl()
        {
            InitializeComponent();
        }

        public HiveControl(HivePlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveConfig_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
