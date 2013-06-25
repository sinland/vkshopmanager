using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using VkShopManager.Core;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for AlbumSelectionWindow.xaml
    /// </summary>
    public partial class AlbumSelectionWindow : Window
    {
        private BackgroundWorker m_bgw;
        private Album m_selectedAlbum;

        public AlbumSelectionWindow(Window parent)
        {
            InitializeComponent();
            
            this.Owner = parent;

            KeyUp += OnKeyUp;

            ReloadList();
        }
        private void ReloadList()
        {
            m_bgw = new BackgroundWorker();
            m_bgw.DoWork += (sender, args) =>
            {
                var db = Core.Repositories.DbManger.GetInstance();
                var albumsRepo = db.GetAlbumsRepository();
                var hiddens = RegistrySettings.GetInstance().GetHiddenList();

                var albums = new List<Album>(0);
                try
                {
                    var all = albumsRepo.GetAll();
                    albums.AddRange(all.Where(album => !hiddens.Contains(album.VkId)));
                }
                catch (Exception)
                {
                    throw new BgWorkerException("Не удалось загрузить список альбомов из БД");
                }

                albums.Sort(
                    (a1, a2) =>
                    String.Compare(a1.GetCleanTitle(), a2.GetCleanTitle(),
                                   StringComparison.CurrentCultureIgnoreCase));
                args.Result = albums;
            };
            m_bgw.RunWorkerCompleted += (sender, args) =>
                {
                    lbAlbums.Items.Clear();
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                var albums = (List<Album>)args.Result;
                foreach (Album album in albums)
                {
                    lbAlbums.Items.Add(album);
                }
            };
            m_bgw.RunWorkerAsync();
        }
        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
        }

        private void lbAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var obj = e.AddedItems[0] as Album;
                if (obj != null) m_selectedAlbum = obj;
            }
        }

        public Album GetSelectedAlbum()
        {
            return m_selectedAlbum;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
