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
    /// Interaction logic for ProductCheckWindow.xaml
    /// </summary>
    public partial class ProductCheckWindow : Window
    {
        public enum Result
        {
            Accept,
            Ignore,
            Break
        }

        private readonly Product m_product;
        private Result m_dialogResult;

        public ProductCheckWindow(Window owner, Product product, string sourceTitle)
        {
            m_product = product;

            InitializeComponent();
            Owner = owner;

            tbGenericUrl.Text = product.GenericUrl;
            tbTitle.Text = product.Title;
            tbPrice.Text = product.Price.ToString();
            tbMinAmount.Text = product.MinAmount.ToString();
            tbSourceDescription.Text = sourceTitle;

            if (!String.IsNullOrEmpty(product.ImageFile))
            {
                image1.Source =
                    new BitmapImage(new Uri(String.Format("file://{0}", 
                        System.IO.Path.Combine(RegistrySettings.GetInstance().GalleryPath, product.ImageFile))));
            }
            else
            {
                // set default image
                image1.Source =
                    new BitmapImage(new Uri("pack://application:,,,/Images/default.png"));
            }

            m_dialogResult = Result.Ignore;
        }
        public Result GetResult()
        {
            return m_dialogResult;
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            m_dialogResult = Result.Ignore;
            DialogResult = false;
            Close();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            var oldVal = m_product.Price;
            try
            {
                m_product.Price = Decimal.Parse(tbPrice.Text);
            }
            catch
            {
                this.ShowError("В поле цены допустимы только цифры и ','");
                m_product.Price = oldVal;
                return;
            }
            var intVal = m_product.MinAmount;
            try
            {
                m_product.MinAmount = Int32.Parse(tbMinAmount.Text);
            }
            catch
            {
                this.ShowError("В поле минимального количества допустимы только цифры");
                m_product.MinAmount = intVal;
                return;
            }

            m_product.CodeNumber = tbCodeNumber.Text.Trim();
            m_product.Title = tbTitle.Text.Trim();
            m_product.GenericUrl = tbGenericUrl.Text.Trim();
            m_dialogResult = Result.Accept;
            DialogResult = true;
            Close();
        }

        private void btnBreak_Click(object sender, RoutedEventArgs e)
        {
            m_dialogResult = Result.Break;
            DialogResult = null;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                btnAccept_Click(sender, null);
            }
            else if (e.Key == Key.N)
            {
                btnSkip_Click(sender, null);
            }
        }
    }
}
