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
    /// Interaction logic for CustomersSelectionWindow.xaml
    /// </summary>
    public partial class CustomersSelectionWindow : Window
    {
        private Customer m_selectedCustomer;
        private BackgroundWorker m_bgw;

        public CustomersSelectionWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;

            KeyUp += OnKeyUp;

            ReloadList();
        }
        private void ReloadList()
        {
            m_bgw = new BackgroundWorker();
            m_bgw.DoWork += (sender, args) =>
            {
                var db = Core.Repositories.DbManger.GetInstance();
                var custRepo = db.GetCustomersRepository();

                var customers = new List<Customer>(0);
                try
                {
                    customers.AddRange(custRepo.All());
                }
                catch (Exception)
                {
                    throw new BgWorkerException("Не удалось загрузить список покупателей из БД");
                }

                customers.Sort(
                    (customer, customer1) =>
                    String.Compare(customer.GetFullName(), customer1.GetFullName(),
                                   StringComparison.CurrentCultureIgnoreCase));
                args.Result = customers;
            };
            m_bgw.RunWorkerCompleted += (sender, args) =>
            {
                lbCustomers.Items.Clear();
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                var customers = (List<Customer>)args.Result;
                foreach (Customer customer in customers)
                {
                    lbCustomers.Items.Add(customer);
                }
            };
            m_bgw.RunWorkerAsync();
        }
        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void lbCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var obj = e.AddedItems[0] as Customer;
                if (obj != null) m_selectedCustomer = obj;
            }
        }

        public Customer GetSelectedCustomer()
        {
            return m_selectedCustomer;
        }

        private void lbCustomers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (m_selectedCustomer == null) return;

            var cew = new CustomerEditWindow(this, m_selectedCustomer);
            cew.ShowDialog();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var c = new Customer
                {
                    FirstName = @"Новый покупатель",
                    AccountTypeId = 0,
                    VkId = Int32.MinValue,
                    Id = Int32.MinValue
                };
            var cew = new CustomerEditWindow(this, c);
            cew.ShowDialog();
            
            if (cew.DialogResult.HasValue && cew.DialogResult.Value)
            {
                ReloadList();
            }
        }
    }
}
