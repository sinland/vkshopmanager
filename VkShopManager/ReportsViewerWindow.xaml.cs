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

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for ReportsViewerWindow.xaml
    /// </summary>
    public partial class ReportsViewerWindow : Window
    {
        private readonly FlowDocument m_doc;

        public ReportsViewerWindow(FlowDocument doc)
        {
            m_doc = doc;
            InitializeComponent();

            viewer.Document = m_doc;
            this.Title = m_doc.Tag.ToString();

            KeyUp += OnKeyUp;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
