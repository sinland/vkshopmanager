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
    /// Interaction logic for DeliveryTypeSelection.xaml
    /// </summary>
    public partial class DeliveryTypeSelection : Window
    {
        private BackgroundWorker m_bgw;

        private DeliveryType m_selectedMember;

        public DeliveryTypeSelection(Window owner)
        {
            InitializeComponent();

            this.Owner = owner;

            KeyUp += OnKeyUpHandler;

            m_bgw = new BackgroundWorker();
            m_bgw.DoWork += (sender, args) =>
            {
                var db = Core.Repositories.DbManger.GetInstance();
                var repo = db.GetDeliveryRepository();

                var deliverys = new List<DeliveryType>(0);
                try
                {
                    deliverys.AddRange(repo.All());
                }
                catch (Exception)
                {
                    throw new BgWorkerException("Не удалось загрузить список покупателей из БД");
                }

                deliverys.Sort((c1, c2) => c1.Id - c2.Id);
                args.Result = deliverys;
            };
            m_bgw.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                var deliverys = (List<DeliveryType>)args.Result;
                foreach (DeliveryType c in deliverys)
                {
                    lbDeliveries.Items.Add(c);
                }
            };
            m_bgw.RunWorkerAsync();
        }

        private void OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Insert)
            {
                btnAdd_Click(this, new RoutedEventArgs());
            }
        }

        private void lbDeliveries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var obj = e.AddedItems[0] as DeliveryType;
                if (obj != null) m_selectedMember = obj;
            }
        }

        private void lbDeliveries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (m_selectedMember == null) return;

            var cew = new DeliveryTypeEdit(this, m_selectedMember);
            cew.ShowDialog();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var c = new DeliveryType
            {
                Comment = "",
                IsActive = false,
                Price = 0
            };
            var cew = new DeliveryTypeEdit(this, c);
            cew.ShowDialog();

            if (cew.DialogResult.HasValue && cew.DialogResult.Value)
            {
                lbDeliveries.Items.Add(c);
            }
        }

        public DeliveryType GetSelected()
        {
            return m_selectedMember;
        }
    }
}
