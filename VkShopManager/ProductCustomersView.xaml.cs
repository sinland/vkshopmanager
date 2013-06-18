using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;
using VkShopManager.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace VkShopManager
{
    
    /// <summary>
    /// Interaction logic for ProductCustomersView.xaml
    /// </summary>
    public partial class ProductCustomersView : Window
    {
        private readonly ProductListViewItem m_lvi;
        private readonly Product m_product;
        private readonly log4net.ILog m_logger;
        private BackgroundWorker m_bgworker;

        public ProductCustomersView(Window owner, ProductListViewItem lvi)
        {
            InitializeComponent();
            Owner = owner;

            m_logger = log4net.LogManager.GetLogger("ManagerMainWindow");
            m_lvi = lvi;

            m_product = lvi.Source;
            m_product.PropertyChanged += ProductOnPropertyChanged;

            tbProductTitle.Text = String.Format("{0} ({1:C}; min: {2})", m_product.Title, m_product.Price,
                                                m_product.MinAmount);
            Closed += (sender, args) => { m_product.PropertyChanged -= ProductOnPropertyChanged; };
            KeyUp += OnKeyUp;

            GetContent();
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
            else if (keyEventArgs.Key == Key.P)
            {
                var f = new ProductEditWindow(this, m_product);
                f.ShowDialog();
            }
        }

        public ProductCustomersView(Window owner, Product product)
        {
            InitializeComponent();
            Owner = owner;

            m_logger = log4net.LogManager.GetLogger("ManagerMainWindow");

            m_product = product;
            m_product.PropertyChanged += ProductOnPropertyChanged;

            tbProductTitle.Text = String.Format("{0} ({1:C}; min: {2})", m_product.Title, m_product.Price,
                                                m_product.MinAmount);
            Closed += (sender, args) => { m_product.PropertyChanged -= ProductOnPropertyChanged; };

            // заглушка
            m_lvi = new ProductListViewItem(m_product);
            
            KeyUp += OnKeyUp;

            GetContent();
        }

        private void GetContent()
        {
            m_bgworker = new BackgroundWorker { WorkerReportsProgress = true };
            m_bgworker.DoWork += (sender, args) =>
            {
                var db = Core.Repositories.DbManger.GetInstance();
                var orderRepo = db.GetOrderRepository();
                var custRepo = db.GetCustomersRepository();

                List<Order> orders;
                try
                {
                    orders = orderRepo.GetOrdersForProduct(m_product);
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    throw new BgWorkerException("Не удалось получить список заказов для продукта. ");
                }

                var list = new List<ProductCustomerListViewItem>();
                foreach (Order order in orders)
                {
                    Customer c;
                    try
                    {
                        c = custRepo.GetById(order.CustomerId);
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        c = new Customer { AccountTypeId = 0, FirstName = "???", LastName = "???", Id = 0 };
                    }

                    list.Add(new ProductCustomerListViewItem(order, c));
                }

                list.Sort(
                    (i1, i2) =>
                    String.Compare(i1.SourceCustomerInfo.LastName, i2.SourceCustomerInfo.LastName,
                                   StringComparison.CurrentCultureIgnoreCase));

                args.Result = list;
            };
            m_bgworker.RunWorkerCompleted += (sender, args) =>
            {
                lvOrderItems.Items.Clear();

                if (args.Cancelled) return;
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                foreach (ProductCustomerListViewItem item in args.Result as List<ProductCustomerListViewItem>)
                {
                    lvOrderItems.Items.Add(item);
                }
            };

            m_bgworker.RunWorkerAsync(); 
        }
        
        private void ProductOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            tbProductTitle.Text = String.Format("{0} ({1:C}; min: {2})", m_product.Title, m_product.Price,
                                                m_product.MinAmount);
        }

        private void btnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            var f = new ProductEditWindow(this, m_product);
            f.ShowDialog();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.Source as ListViewItem) == null) return;
            if ((e.Source as ListViewItem).Content is ProductCustomerListViewItem)
            {
                var lviObj = (e.Source as ListViewItem).Content as ProductCustomerListViewItem;
                var oldAmount = lviObj.SourceOrderInfo.Amount;
                var f = new ChangeAmountWindow(this, lviObj.SourceOrderInfo);
                f.ShowDialog();

                if (oldAmount != lviObj.SourceOrderInfo.Amount)
                {
                    m_lvi.OrderedAmount += (lviObj.SourceOrderInfo.Amount - oldAmount);
                }
            }
        }

        private void btnAddCutomerToProductClickHandler(object sender, RoutedEventArgs e)
        {
            var f = new CustomersSelectionWindow(this);
            f.ShowDialog();
            var selectedCustomer = f.GetSelectedCustomer();
            if (selectedCustomer == null)
            {
                this.ShowError("Необходимо выбрать минимум одного покупателя из списка.");
                return;
            }

            m_bgworker = new BackgroundWorker();
            m_bgworker.DoWork += (o, args) =>
                {
                    var customer = args.Argument as Customer;
                    var ordersRepo = Core.Repositories.DbManger.GetInstance().GetOrderRepository();


                    try
                    {
                        var allProductOrders = ordersRepo.GetOrdersForProduct(m_product);
                        if (allProductOrders.Any(productOrder => productOrder.CustomerId == customer.Id))
                        {
                            throw new BgWorkerException("Указанный покупатель уже заказывал данный товар!");
                        }
                    }
                    catch (BgWorkerException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        throw new BgWorkerException("Не удалось получить данные из БД");
                    }

                    var order = new Order
                        {
                            Amount = 0,
                            CustomerId = customer.Id,
                            Date = DateTime.Now,
                            InitialVkCommentId = 0,
                            ProductId = m_product.Id,
                            Comment = ""
                        };
                    try
                    {
                        ordersRepo.Add(order);
                    }
                    catch (Exception)
                    {
                        throw new BgWorkerException("Не удалось сохранить информацию о заказе в БД");
                    }
                    
                    args.Result = new ProductCustomerListViewItem(order, selectedCustomer);
                };
            m_bgworker.RunWorkerCompleted += (o, args) =>
                {
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    lvOrderItems.Items.Add((ProductCustomerListViewItem) args.Result);
                };

            m_bgworker.RunWorkerAsync(selectedCustomer);
        }

        private void btnViewCommentsOnProductClick(object sender, RoutedEventArgs e)
        {
            var cvw = new CommentsViewWindow(this, m_product);
            cvw.ShowDialog();

        }
    }
}
