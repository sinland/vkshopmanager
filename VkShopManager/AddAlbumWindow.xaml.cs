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
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class AddAlbumWindow : Window
    {
        private readonly Album m_album;

        public AddAlbumWindow(Album album)
        {
            m_album = album;
            InitializeComponent();

            tbAlbumName.Text = m_album.Title;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            m_album.Title = tbAlbumName.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
