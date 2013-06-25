using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for CommentCheckWindow.xaml
    /// </summary>
    public partial class CommentCheckWindow : Window
    {
        private readonly Order m_order;
        private OrderOperation m_requestedOperation;
        private Result m_result = Result.Reject;

        public enum Result
        {
            Accept,
            Reject,
        }

        public CommentCheckWindow(Window owner, string sourceMessage, Customer sender, Order order, Product product, OrderOperation requestedOperation)
        {
            InitializeComponent();

            Owner = owner;
            m_order = order;
            m_requestedOperation = requestedOperation;
            

            tbOriginalText.IsReadOnly = true;
            tbOriginalText.Text = sourceMessage;
            tbAmount.Text = order.Amount.ToString();
            lblPrice.Content = product.Price.ToString("C", CultureInfo.CurrentCulture);
            lblMin.Content = product.MinAmount.ToString("");
            tbTitle.Text = product.Title;
            lblCustomer.Content = sender.GetFullName();

            switch (requestedOperation)
            {
                case OrderOperation.Add:
                    rbAppendOrder.IsChecked = true;
                    break;
                case OrderOperation.Remove:
                    rbRemovePosition.IsChecked = true;
                    break;
                case OrderOperation.Decrease:
                    rbSkipComment.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("requestedOperation");
            }
            
            if (!String.IsNullOrEmpty(product.ImageFile)
                && System.IO.File.Exists(System.IO.Path.Combine(RegistrySettings.GetInstance().GalleryPath, product.ImageFile)))
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
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        public Result GetResult()
        {
            return m_result;
        }
        public OrderOperation GetRequestedOperation()
        {
            return m_requestedOperation;
        }

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_order.Amount = Int32.Parse(tbAmount.Text);
            }
            catch
            {
                this.ShowError("В поле количества допускаются только цифры.");
                return;
            }
            if (m_requestedOperation == OrderOperation.Add && m_order.Amount == 0)
            {
                this.ShowError("Ошибка: Невозможно добавить 0 позиций в заказ!");
                return;
            }
            m_order.Comment = tbComment.Text.Trim();

            Close();
        }

        private void rbSkipComment_Checked(object sender, RoutedEventArgs e)
        {
            if (rbSkipComment.IsChecked.HasValue && rbSkipComment.IsChecked.Value)
            {
                tbAmount.IsEnabled = false;
                tbAmount.IsReadOnly = true;
            }
            else
            {
                tbAmount.IsEnabled = true;
                tbAmount.IsReadOnly = false;
            }

            m_result = Result.Reject;
        }

        private void rbRemovePosition_Checked(object sender, RoutedEventArgs e)
        {
            if (rbRemovePosition.IsChecked.HasValue && rbRemovePosition.IsChecked.Value)
            {
                tbAmount.IsEnabled = false;
                tbAmount.IsReadOnly = true;
            }
            else
            {
                tbAmount.IsEnabled = true;
                tbAmount.IsReadOnly = false;
            }

            m_requestedOperation = OrderOperation.Remove;
            m_result = Result.Accept;
        }

        private void rbDecreaseOrder_Checked(object sender, RoutedEventArgs e)
        {
            m_result = Result.Accept;
            m_requestedOperation = OrderOperation.Decrease;
            tbAmount.IsEnabled = true;
            tbAmount.IsReadOnly = false;
        }

        private void rbAppendOrder_Checked(object sender, RoutedEventArgs e)
        {
            m_result = Result.Accept;
            m_requestedOperation = OrderOperation.Add;
            tbAmount.IsEnabled = true;
            tbAmount.IsReadOnly = false;
        }

        private void rbAlwaysSkipComment_Checked(object sender, RoutedEventArgs e)
        {
            if (rbAlwaysSkipComment.IsChecked.HasValue && rbAlwaysSkipComment.IsChecked.Value)
            {
                tbAmount.IsEnabled = false;
                tbAmount.IsReadOnly = true;
            }
            else
            {
                tbAmount.IsEnabled = true;
                tbAmount.IsReadOnly = false;
            }

            m_result = Result.Accept;
            m_requestedOperation = OrderOperation.Forget;
        }
    }
}
