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
    /// Interaction logic for AddPaymentWindow.xaml
    /// </summary>
    public partial class PaymentPropertiesWindow : Window
    {
        private readonly Payment m_payment;
        private Result m_result = Result.Cancel;

        public enum Result
        {
            Cancel,
            Ok
        }

        public PaymentPropertiesWindow(Window owner, Payment payment)
        {
            InitializeComponent();
            Owner = owner;
            m_payment = payment;

            tbAmount.Text = payment.Amount.ToString();
            tbComment.Text = payment.Comment;
        }

        public Result GetResult()
        {
            return m_result;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var oldValue = m_payment.Amount;
            try
            {
                m_payment.Amount = Decimal.Parse(tbAmount.Text);
            }
            catch (Exception)
            {
                m_payment.Amount = oldValue;
                this.ShowError("В поле суммы необходимо указать сумму платежа");
                return;
            }

            m_payment.Comment = tbComment.Text;
            m_result = Result.Ok;
            Close();
        }
    }
}
