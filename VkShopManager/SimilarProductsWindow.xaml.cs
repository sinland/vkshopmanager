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
using VkShopManager.Core;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for SimilarProductsWindow.xaml
    /// </summary>
    public partial class SimilarProductsWindow : Window
    {
        private Product m_selectedProduct;

        public SimilarProductsWindow(IEnumerable<Product> similarProducts)
        {
            InitializeComponent();

            foreach (Product similarProduct in similarProducts)
            {
                lvSimilars.Items.Add(similarProduct);
            }
        }

        private void btnSkipClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAcceptClick(object sender, RoutedEventArgs e)
        {
            if (lvSimilars.SelectedItems.Count < 0)
            {
                this.ShowError("Необходимо выбрать товар из списка");
                return;
            }

            m_selectedProduct = lvSimilars.SelectedItems[0] as Product;
            DialogResult = true;
            Close();
        }

        public Product GetSelectedProduct()
        {
            return m_selectedProduct;
        }
    }
}
