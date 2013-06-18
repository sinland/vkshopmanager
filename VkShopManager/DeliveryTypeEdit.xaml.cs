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
    /// Interaction logic for DeliveryTypeEdit.xaml
    /// </summary>
    public partial class DeliveryTypeEdit : Window
    {
        private readonly DeliveryType m_deliveryType;

        public DeliveryTypeEdit(Window owner, DeliveryType deliveryType)
        {
            m_deliveryType = deliveryType;
            this.Owner = owner;

            InitializeComponent();

            cbIsActive.IsChecked = m_deliveryType.IsActive;
            tbComment.Text = m_deliveryType.Comment;
            tbPrice.Text = m_deliveryType.Price.ToString();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_deliveryType.Price = Decimal.Parse(tbPrice.Text);
            }
            catch
            {
                this.ShowError("Ошибка: Неверно задана цена доставки.");
                return;
            }

            m_deliveryType.Comment = tbComment.Text.Trim();
            if (cbIsActive.IsChecked != null) m_deliveryType.IsActive = cbIsActive.IsChecked.Value;

            var repo = Core.Repositories.DbManger.GetInstance().GetDeliveryRepository();
            try
            {
                repo.Update(m_deliveryType);
                this.DialogResult = true;
                Close();
            }
            catch
            {
                this.ShowError("Ошибка: Не удалось сохранить информацию по способу достаки. Ошибка выполенения запроса.");
            }
        }
    }
}
