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

        private Product m_product;
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
            tbCodeNumber.TextChanged += TbCodeNumberOnTextChanged;
            tbCodeNumber.SelectionChanged += TbCodeNumberOnSelectionChanged;

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

        private void TbCodeNumberOnSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            tbCodeNumber.Text = tbCodeNumber.Text.Trim();
        }

        private void TbCodeNumberOnTextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            
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
                m_product.CodeNumber = tbCodeNumber.Text.Trim();
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
                if(m_product.MinAmount < 1) throw new ApplicationException();
            }
            catch
            {
                this.ShowError("Ошибка: Поле количества заполнено не верно. Минимальное количество - 1 шт.");
                return;
            }
            
            try
            {
                var db = DbManger.GetInstance().GetProductRepository();

                if (m_product.Id == Int32.MinValue)
                {
                    Product selectedFromExisting = null;
                    if (m_product.CodeNumber.Length > 0)
                    {
                        // check for code number...
                        var similars = db.GetByCodeNumber(m_product.CodeNumber);
                        if (similars != null && similars.Count > 0)
                        {
                            // there are some products with such code number
                            var w = new SimilarProductsWindow(similars);
                            var wres = w.ShowDialog();
                            if (wres.HasValue && wres.Value)
                            {
                                selectedFromExisting = w.GetSelectedProduct();
                            }
                        }
                    }
                    if (selectedFromExisting == null)
                    {
                        db.Add(m_product);
                    }
                    else
                    {
                        m_product = selectedFromExisting;
                    }
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

        private void btnCreateCode_Click(object sender, RoutedEventArgs e)
        {
            var generator = new CodeNumberGenerator();
            var repo = DbManger.GetInstance().GetProductRepository();
            var setting = generator.ConvertToType(RegistrySettings.GetInstance().SelectedCodeNumberGenerator);
            var prefix = RegistrySettings.GetInstance().CodeNumberAlphaPrefix;
            var length = RegistrySettings.GetInstance().CodeNumberNumericLength;
            var code = "";
            for (int i = 0; i < 1000; i++)
            {
                switch (setting)
                {
                    case CodeNumberGenerator.Type.NumericOnly:
                        code = generator.GetNumericCode(length);
                        break;
                    case CodeNumberGenerator.Type.AlphaNumeric:
                        code = generator.GetAlphaNumericCode(length, prefix);
                        break;
                    default:
                        this.ShowError("Ошибка: В конфигурации неверно указан тип артикула!");
                        break;
                }

                if (repo.GetByCodeNumber(code).Count == 0) break;
                else code = "";
            }

            if (String.IsNullOrEmpty(code))
            {
                this.ShowError("Ошибка: Не удалось придумать уникальный артикул!");
            }

            tbCodeNumber.Text = code;
        }
    }
}
