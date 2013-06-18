using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for OrderEditWindow.xaml
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private readonly CustomerListViewItem m_customerItem;
        private readonly Customer m_customer;
        private readonly Album m_album;
        private ManagedRate m_comission;
        private Result m_result = Result.None;
        
        private string m_statusBackup;
        private Cursor m_cursorBackup;
        private RegistrySettings m_settings;

        public enum Result
        {
            None,
            Changed
        }

        public OrderEditWindow(Window owner, CustomerListViewItem customer, Album album)
        {
            m_customer = customer.Source;
            m_customerItem = customer;
            m_album = album;
            m_settings = RegistrySettings.GetInstance();

            InitializeComponent();
            Owner = owner;

            KeyUp += OnKeyUp;

            FillOrdersTable();
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
            if (keyEventArgs.Key == Key.P)
            {
                btnExportClickHandler(sender, new RoutedEventArgs());
            }
        }

        private void FillOrdersTable()
        {
            var bg = new BackgroundWorker { WorkerReportsProgress = true };

            tbCustomerTitle.Text = "";
            bg.DoWork += (sender, args) =>
                {
                    var orderRepo = Core.Repositories.DbManger.GetInstance().GetOrderRepository();
                    var prodRepo = Core.Repositories.DbManger.GetInstance().GetProductRepository();
                    var ratesRepo = Core.Repositories.DbManger.GetInstance().GetRatesRepository();

                    try
                    {
                        m_comission = ratesRepo.GetById(m_customer.AccountTypeId);
                    }
                    catch (Exception)
                    {
                        throw new BgWorkerException("Ошибка. Не удалось получить ставку пользователя.");
                    }

                    List<Order> orders;
                    try
                    {
                        orders = orderRepo.GetOrdersForCustomerFromAlbum(m_customer, m_album);
                    }
                    catch(Exception)
                    {
                        throw new BgWorkerException("Не удалось получить список заказов для пользователя.");
                    }
                    var list = new List<OrderListViewItem>();

                    bool empty = m_settings.ShowEmptyPositions;
                    bool partial = m_settings.ShowPartialPositions;
                    foreach (var order in orders)
                    {
                        Product p;
                        long totalOrdered = 0;
                        try
                        {
                            p = prodRepo.GetById(order.ProductId);
                            if (p == null)
                            {
                                //todo: add logging
                                continue;
                            }
                            totalOrdered = orderRepo.GetProductTotalOrderedAmount(p);
                            if (!empty && (totalOrdered == 0 || order.Amount == 0)) continue;
                            if (!partial && totalOrdered < p.MinAmount) continue;
                        }
                        catch (Exception)
                        {
                            throw new BgWorkerException("Ошибка БД.");
                        }
                        var item = new OrderListViewItem(order, p, m_comission)
                            {
                                Status = totalOrdered < p.MinAmount ? "" : "OK"
                            };
                        list.Add(item);
                    }

                    list.Sort((i1, i2) => String.Compare(i1.Title, i2.Title, StringComparison.CurrentCultureIgnoreCase));
                    args.Result = list;
                };
            bg.RunWorkerCompleted += (sender, args) =>
                {
                    lvOrderItems.Items.Clear();

                    if (args.Cancelled) return;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    tbCustomerTitle.Text = String.Format("{0} ({1})", m_customer.GetFullName(), m_comission.Comment);

                    var list = args.Result as List<OrderListViewItem>;
                    foreach (var lvi in list)
                    {
                        lvi.PropertyChanged += LviOnPropertyChanged;
                        lvOrderItems.Items.Add(lvi);
                    }

                    ShowOrderStatistics();
                };
            bg.RunWorkerAsync();
        }

        private void LviOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (String.Compare(propertyChangedEventArgs.PropertyName, "Amount", true) == 0)
            {
                ShowOrderStatistics();
            }
        }

        private void ShowOrderStatistics()
        {
            decimal totalComission = 0;
            decimal totalClean = 0;
            decimal totalSum = 0;
            for (int i = 0; i < lvOrderItems.Items.Count; i++)
            {
                var lvi = lvOrderItems.Items[i] as OrderListViewItem;
                totalComission += lvi.Comission;
                totalClean += lvi.Sum;
                totalSum += lvi.FinalSum;
            }

            tbTotal.Text = String.Format("Сумма: {0:C2} | Комиссия: {1:C2} | Итог: {2:C0} | Оплата: {3:C0}", totalClean,
                                         totalComission, totalSum, m_customerItem.Payment);
        }

        private void btnEditCustomer_Click(object sender, RoutedEventArgs e)
        {
            var f = new CustomerEditWindow(this, m_customer);
            f.ShowDialog();

            FillOrdersTable();
        }

        private void btnEditSelectedOrderAmountClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (lvOrderItems.SelectedItem == null) return;

            var lvi = lvOrderItems.SelectedItem as OrderListViewItem;
            if (lvi == null) return;

            var f = new ChangeAmountWindow(this, lvi.SourceOrder);
            f.ShowDialog();
            if (f.GetResult() == ChangeAmountWindow.Result.Changed)
            {
                m_result = Result.Changed;
                FillOrdersTable();
            }
        }

        public Result GetResult()
        {
            return m_result;
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListViewItem)) return;
            
            var lvi = ((sender as ListViewItem).Content) as OrderListViewItem;
            if (lvi == null)
                return;

            var f = new ProductCustomersView(this, lvi.SourceProduct);
            f.ShowDialog();
            FillOrdersTable();
        }

        private void btnExportClickHandler(object sender, RoutedEventArgs e)
        {
            var filePrefix = m_customer.GetFullName().Replace(" ", "_");
            var f = new AskExportSettingsWindow(this, m_album, filePrefix) { Owner = this };
            f.ShowDialog();
            if (f.GetResult() != AskExportSettingsWindow.Result.Ok) return;
            
            var bg = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
            bg.DoWork += (o, args) =>
                {
                    var worker = args.Argument as ExportFormatterBase;
                    worker.ExportCustomerOrders(m_customer);
                    args.Result = worker;
                };
            bg.RunWorkerCompleted += (o, args) =>
                {
                    tbTotal.Text = m_statusBackup;
                    Cursor = m_cursorBackup;

                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                    }
                    else
                    {
                        var worker = args.Result as ExportFormatterBase;
                        if (worker is IFileExporter)
                        {
                            if (this.ShowQuestion("Экспорт выполнен успешно. Открыть файл?"))
                            {
                                Process.Start(new ProcessStartInfo(((IFileExporter)worker).Filename) { Verb = "open" });
                            }
                        }
                        else if (worker is IReportExporter)
                        {
                            // open report in viewer
                            var f1 = new ReportsViewerWindow(((IReportExporter)worker).GetDocument()) { Owner = this };
                            f1.ShowDialog();
                        }
                    }
                };

            m_statusBackup = tbTotal.Text;
            tbTotal.Text = "Выполняется экспорт...";
            m_cursorBackup = Cursor;
            Cursor = Cursors.Wait;
            bg.RunWorkerAsync(f.GetSelectedFormatter());
        }

        private void btnAddPaymentClickHandler(object sender, RoutedEventArgs e)
        {
            var payment = new Payment {AlbumId = m_album.Id, PayerId = m_customer.Id, Date = DateTime.Now, Amount = 0};
            var f = new PaymentPropertiesWindow(this, payment);
            f.ShowDialog();

            if (f.GetResult() != PaymentPropertiesWindow.Result.Ok)
            {
                return;
            }

            var repo = Core.Repositories.DbManger.GetInstance().GetPaymentsRepository();
            try
            {
                repo.Add(payment);
                m_customerItem.Payment += payment.Amount;
                // m_customerItem.NotifyPropertiesChanged();
            }
            catch (Exception exception)
            {
                this.ShowError("Ошибка. Не удалось сохранить информацию в БД.\r\n\r\n" + exception.Message);
                return;
            }
        }
    }
}
