using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for WaitingWindow.xaml
    /// </summary>
    public partial class WaitingWindow : Window
    {
        public WaitingWindow(Window owner, string msg = "")
        {
            InitializeComponent();

            Owner = owner;
            label1.Content = msg;
        }

        public void SetState(object msg)
        {
            if (!label1.Dispatcher.CheckAccess())
            {
                label1.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<object>(SetState), msg);
                return;
            }
            
            label1.Content = msg.ToString();
        }
    }
}
