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
using VkShopManager.Domain;
using VkShopManager.Core;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for ChangeAmountWindow.xaml
    /// </summary>
    public partial class ChangeAmountWindow : Window
    {
        private readonly Order m_order;
        private readonly log4net.ILog m_logger;
        private Result m_result = Result.None;

        public enum Result
        {
            None,
            Changed
        }

        public ChangeAmountWindow(Window owner, Order order)
        {
            m_order = order;
            m_logger = log4net.LogManager.GetLogger("ManagerMainWindow");

            InitializeComponent();
            KeyUp += OnKeyUp;

            textBox1.Text = m_order.Amount.ToString();

            Owner = owner;
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
            var oldValue = m_order.Amount;
            try
            {
                m_order.Amount = Int32.Parse(textBox1.Text);
            }
            catch
            {
                this.ShowError("Укажите корректное значение количества!");
                m_order.Amount = oldValue;
                return;
            }

            var db = Core.Repositories.DbManger.GetInstance();
            var repo = db.GetOrderRepository();
            try
            {
                repo.Update(m_order);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                this.ShowError(String.Format("Ошибка: Не удалось сохранить изменения. ({0})", exception.GetType().Name));
            }

            m_result = Result.Changed;
            Close();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public Result GetResult()
        {
            return m_result;
        }

        private void BtnUp_OnClick(object sender, RoutedEventArgs e)
        {
            int amount;
            try
            {
                amount = Int32.Parse(textBox1.Text);
            }
            catch
            {
                this.ShowError("Укажите корректное значение количества!");
                return;
            }

            amount += 1;
            textBox1.Text = amount.ToString();
        }

        private void BtnDown_OnClick(object sender, RoutedEventArgs e)
        {
            int amount;
            try
            {
                amount = Int32.Parse(textBox1.Text);
            }
            catch
            {
                this.ShowError("Укажите корректное значение количества!");
                return;
            }
            amount -= 1;
            if (amount < 0) amount = 0;

            textBox1.Text = amount.ToString();
        }
    }
}
