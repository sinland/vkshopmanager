using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for ProductSelectionWindow.xaml
    /// </summary>
    public partial class ProductSelectionWindow : Window
    {
        private Album m_currentAlbum;
        private readonly List<Product> m_products;
        private readonly List<Product> m_selectedProducts;
        private readonly List<Album> m_allAlbums;

        public delegate void OnAddProductsToOrderHandler(List<Product> products);
        public event OnAddProductsToOrderHandler OnAddProductsToOrder;

        protected virtual void OnOnAddProductsToOrder(List<Product> products)
        {
            OnAddProductsToOrderHandler handler = OnAddProductsToOrder;
            if (handler != null) handler(products);
        }

        public ProductSelectionWindow(Window owner, Album currentAlbum)
        {
            m_currentAlbum = currentAlbum;
            m_products = new List<Product>();
            m_allAlbums = new List<Album>(0);
            m_selectedProducts = new List<Product>(0);

            InitializeComponent();

            this.Owner = owner;
            this.DataContext = this;

            PopolateAlbumsList();
            PopulateProductsList();

        }

        private void PopulateProductsList()
        {
            var bg = new BackgroundWorker();
            bg.DoWork += (sender, args) =>
            {
                var productRepository = DbManger.GetInstance().GetProductRepository();

                try
                {
                    m_products.Clear();
                    m_products.AddRange(productRepository.GetAllFromAlbum(m_currentAlbum));
                }
                catch (Exception exception)
                {
                    throw new BgWorkerException("Ошибка. Не удалось получить информацию по альбому.");
                }

                m_products.Sort((a, b) => String.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
            };
            bg.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                lvOrderItems.Items.Clear();
                foreach (Product product in m_products)
                {
                    lvOrderItems.Items.Add(product);
                }
            };
            bg.RunWorkerAsync();
        }
        private void PopolateAlbumsList()
        {
            var bg = new BackgroundWorker();
            bg.DoWork += (sender, args) =>
            {
                var albumsRepository = DbManger.GetInstance().GetAlbumsRepository();
                var hiddenIds = RegistrySettings.GetInstance().GetHiddenList();
                try
                {
                    foreach (var album in albumsRepository.GetAll())
                    {
                        if (!hiddenIds.Contains(album.VkId))
                        {
                            m_allAlbums.Add(album);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new BgWorkerException("Ошибка. Не удалось получить информацию по альбому.");
                }

                m_allAlbums.Sort((a1, a2) => DateTime.Compare(a1.CreationDate, a2.CreationDate));
            };
            bg.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }
                foreach (Album album in m_allAlbums)
                {
                    var i = cbAlbumsCollection.Items.Add(album);
                    if (album.Id == m_currentAlbum.Id) cbAlbumsCollection.SelectedIndex = i;
                }
            };

            bg.RunWorkerAsync();
        }
        private void lvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Product item in e.AddedItems)
            {
                m_selectedProducts.Add(item);
            }
            foreach (Product item in e.RemovedItems)
            {
                m_selectedProducts.Remove(item);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAddProductsToOrder_Click(object sender, RoutedEventArgs e)
        {
            OnOnAddProductsToOrder(m_selectedProducts);
        }

        private void cbAlbumsCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            var a  = e.AddedItems[0] as Album;
            m_currentAlbum = a;
            
            m_selectedProducts.Clear();
            PopulateProductsList();
        }
    }
}
