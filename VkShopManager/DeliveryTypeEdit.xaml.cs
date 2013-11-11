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
            tbMinOrderAmount.Text = m_deliveryType.MinimumOrderSummaryCondition.ToString();
            cbMinOrderActivation.IsChecked = m_deliveryType.IsConditional;

            Title = tbComment.Text.Length < 1 ? "[Новая]" : String.Format("Доставка: {0}", m_deliveryType.Comment);
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
            if (cbMinOrderActivation.IsChecked.HasValue && cbMinOrderActivation.IsChecked.Value)
            {
                m_deliveryType.IsConditional = true;
                try
                {
                    m_deliveryType.MinimumOrderSummaryCondition = Decimal.Parse(tbMinOrderAmount.Text);
                }
                catch (Exception)
                {
                    this.ShowError("Ошибка: Неверно задана минимальная сумма счета для включения доставка.");
                    return;
                }
            }
            else
            {
                m_deliveryType.IsConditional = false;
            }

            m_deliveryType.Comment = tbComment.Text.Trim();
            if (cbIsActive.IsChecked != null) m_deliveryType.IsActive = (cbIsActive.IsChecked.Value) ;

            var repo = Core.Repositories.DbManger.GetInstance().GetDeliveryRepository();
            try
            {
                if (m_deliveryType.Id == Int32.MinValue)
                {
                    repo.Add(m_deliveryType);
                }
                else
                {
                    repo.Update(m_deliveryType);
                }
                
                DialogResult = true;
                Close();
            }
            catch
            {
                this.ShowError("Ошибка: Не удалось сохранить информацию по способу достаки. Ошибка выполенения запроса.");
            }
        }

        private void CbMinOrderActivation_OnChecked(object sender, RoutedEventArgs e)
        {
            if (cbMinOrderActivation.IsChecked.HasValue && cbMinOrderActivation.IsChecked.Value)
            {
                tbMinOrderAmount.IsEnabled = true;
            }
            else
            {
                tbMinOrderAmount.IsReadOnly = false;
            }
        }
    }
}
