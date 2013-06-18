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
using VkShopManager.Core.Repositories;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for CustomerEditWindow.xaml
    /// </summary>
    public partial class CustomerEditWindow : Window
    {
        private readonly Customer m_customer;

        public CustomerEditWindow(Window owner, Customer customer)
        {
            m_customer = customer;
        
            InitializeComponent();
            Owner = owner;

            KeyUp += OnKeyUp;

            tbName.Text = customer.FirstName;
            tbSurname.Text = customer.LastName;

            var ratesRepo = DbManger.GetInstance().GetRatesRepository();
            List<ManagedRate> rates;
            try
            {
                rates = ratesRepo.GetAll();
            }
            catch (Exception)
            {
                rates = new List<ManagedRate>(0);
            }
            
            int selectedIndex = 0;
            for (int index = 0; index < rates.Count; index++)
            {
                ManagedRate managedRate = rates[index];
                if (m_customer.AccountTypeId == managedRate.Id) selectedIndex = index;
                comboBox1.Items.Add(managedRate);
            }

            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = selectedIndex;

            List<DeliveryType> delivery;
            try
            {
                delivery = DbManger.GetInstance().GetDeliveryRepository().All();
            }
            catch
            {
                delivery = new List<DeliveryType>(0);
            }
            selectedIndex = 0;
            for (int i = 0; i < delivery.Count; i++)
            {
                DeliveryType dt = delivery[i];
                if (m_customer.DeliveryTypeId == dt.Id) selectedIndex = i;
                comboBox2.Items.Add(dt);
            }
            if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = selectedIndex;

            tbAddress.Text = customer.Address;
            tbPhone.Text = customer.Phone;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape) { Close(); }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var repo = DbManger.GetInstance().GetCustomersRepository();
            var rate = comboBox1.SelectedItem as ManagedRate;
            var delivery = comboBox2.SelectedItem as DeliveryType;

            m_customer.FirstName = tbName.Text.Trim();
            m_customer.LastName = tbSurname.Text.Trim();
            m_customer.Address = tbAddress.Text.Trim();
            m_customer.Phone = tbPhone.Text.Trim();
            m_customer.AccountTypeId = rate.Id;
            m_customer.DeliveryTypeId = delivery.Id;

            try
            {
                if (m_customer.Id == Int32.MinValue)
                {
                    repo.Add(m_customer);
                }
                else repo.Update(m_customer);
            }
            catch (Exception)
            {
                this.ShowError("Ошибка. Не удалось сохранить информацию в БД!");
                return;
            }

            this.DialogResult = true;
            Close();
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void comboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
