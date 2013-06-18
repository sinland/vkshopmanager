using System;
using System.Collections.Generic;
using System.Globalization;
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
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for ProductEditWindow.xaml
    /// </summary>
    public partial class ProductEditWindow : Window
    {
        public enum Result
        {
            None,
            Saved,
        }

        private readonly Product m_product;
        private Result m_result = Result.None;

        public ProductEditWindow(Window owner, Product product)
        {
            
            InitializeComponent();

            m_product = product;
            Owner = owner;

            KeyUp += OnKeyUp;

            tbTitle.Text = product.Title;
            tbGenericUrl.Text = product.GenericUrl;
            tbPrice.Text = product.Price.ToString("F2");
            tbMinAmount.Text = product.MinAmount.ToString();
            tbCodeNumber.Text = product.CodeNumber;

            try
            {
                var imgFile = System.IO.Path.Combine(RegistrySettings.GetInstance().GalleryPath, product.ImageFile);
                if(!System.IO.File.Exists(imgFile))
                {
                    throw new ApplicationException();
                }

                image1.Source =
                    new BitmapImage(new Uri(String.Format("file://{0}", imgFile)));
            }
            catch
            {
                // set default image
                image1.Source =
                    new BitmapImage(new Uri("pack://application:,,,/Images/default.png"));
            }

        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
        }

        public Result GetResult()
        {
            return m_result;
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (tbTitle.Text.Length > 0)
            {
                m_product.Title = tbTitle.Text;
            }
            else
            {
                this.ShowError("Ошибка: Поле наименование должно быть заполнено.");
                return;
            }
            if (tbCodeNumber.Text.Length > 0)
            {
                m_product.CodeNumber = tbCodeNumber.Text;
            }

            m_product.GenericUrl = tbGenericUrl.Text;
            try
            {
                m_product.Price = Decimal.Parse(tbPrice.Text);
            }
            catch
            {
                this.ShowError("Ошибка: Поле цены заполнено не верно.");
                return;
            }
            
            try
            {
                m_product.MinAmount = Int32.Parse(tbMinAmount.Text);
            }
            catch
            {
                this.ShowError("Ошибка: Поле количества заполнено не верно.");
                return;
            }
            
            try
            {
                var db = DbManger.GetInstance().GetProductRepository();
                if (m_product.Id == Int32.MinValue)
                {
                    db.Add(m_product);
                }
                else db.Update(m_product);
            }
            catch (Exception exception)
            {
                this.ShowError("Ошибка. Не удалось сохранить запись", exception.GetType().Name);
                return;
            }

            m_result = Result.Saved;
            Close();
        }
    }
}
