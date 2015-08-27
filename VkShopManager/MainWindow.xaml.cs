using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VkApiNet;
using VkApiNet.Vk;
using VkShopManager.Core;
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        private Album m_selectedAlbum;
        protected Album SelectedAlbum
        {
            get { return m_selectedAlbum; }
            set
            {
                m_selectedAlbum = value;
                Title = m_selectedAlbum == null ? @"Совместные покупки - Снежинск" : m_selectedAlbum.GetCleanTitle();
            }
        }

        private readonly RegistrySettings m_settings;
        private readonly log4net.ILog m_logger;
        
        private ServiceTreeNodes m_selectedView;
        private WaitingWindow m_waitingWindow;

        private static readonly IValueConverter s_currencyVisualiser = new CurrencyVisualiser(2);
        private static readonly IValueConverter s_roundedCurrencyVisualiser = new CurrencyVisualiser(0);
        private readonly List<BackgroundWorker> m_workersPool;

        private enum ServiceTreeNodes
        {
            None,
            AlbumCustomers,
            AlbumProducts,
            AlbumPaymets
        }

        private enum TableSearchType
        {
            Title,
            CodeNumber
        }

        public MainWindow()
        {
            InitializeComponent();

            KeyUp += WindowKeyPressEventHandler;

            InitializeTemplates();

            log4net.Config.XmlConfigurator.Configure();
            m_settings = RegistrySettings.GetInstance();
            m_logger = log4net.LogManager.GetLogger("ManagerMainWindow");
            m_workersPool = new List<BackgroundWorker>(0);
            m_selectedView = ServiceTreeNodes.None;

            Application.Current.Exit += ApplicationExitEventHandler;
            
            cbSearchFieldType.Items.Add(new ComboBoxItem()
                {
                    Tag = TableSearchType.Title,
                    Content = "Наименование (Имя)",
                    IsSelected = true

                });
            cbSearchFieldType.Items.Add(new ComboBoxItem()
            {
                Tag = TableSearchType.CodeNumber,
                Content = "Артикул (Код)"
            });

            // создать каталог галереи
            try
            {
                Directory.CreateDirectory(m_settings.GalleryPath);
                Directory.CreateDirectory(m_settings.ReportsPath);
            }
            catch
            {

            }

            FillAlbumsListAsync();
        }

        private void CmdFileGetStoredAlbums_OnClick(object sender, RoutedEventArgs e)
        {
            ClearDetailsView();
            FillAlbumsListAsync();
        }

        private void InitializeTemplates()
        {
            var templatesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                      "Templates");

            WordExportFormatter.CustomerOrderTemplate = System.IO.Path.Combine(templatesDir,
                                                                               "CustomerOrderTemplate.tpl");
            WordExportFormatter.DeliveryListTemplate = System.IO.Path.Combine(templatesDir,
                                                                               "DeliveryListTemplate.tpl");   
        }
        private void WindowKeyPressEventHandler(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Enter)
            {
                if (lvDetails.SelectedItems.Count == 0) return;

                var customItem = lvDetails.SelectedItem as CustomListViewItem;
                if(customItem != null) ShowDialogForListViewItem(customItem);
            }
            if (keyEventArgs.Key == Key.Insert && m_selectedView == ServiceTreeNodes.AlbumProducts)
            {
                cmdAlbumAddProduct_OnClick(sender, new RoutedEventArgs());
            }
        }

        private void FillAlbumsListAsync()
        {
            var bg = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
            bg.ProgressChanged += (sender, args) => SetStatus(args.UserState.ToString());
            bg.DoWork += (sender, args) =>
                {
                    var worker = sender as BackgroundWorker;
                    worker.ReportProgress(0, "Инициализация");
                    DbManger db;
                    try
                    {
                        db = DbManger.GetInstance();
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Ошибка: Не удалось инициализировать БД");
                    }

                    var albumRepo = db.GetAlbumsRepository();
                    
                    List<Album> albums;
                    try
                    {
                        albums = albumRepo.GetAll();
                        
                    }
                    catch (Exception e)
                    {
                        m_logger.ErrorException(e);
                        throw new BgWorkerException("Не удалось получить список альбомов. Проверьте настройки БД");
                    }

                    albums.Sort((a1, a2) => String.Compare(a1.GetCleanTitle(), a2.GetCleanTitle(), StringComparison.CurrentCultureIgnoreCase));
                    args.Result = albums;
                };
            bg.RunWorkerCompleted += (sender, args) =>
                {
                    AlbumsView.Items.Clear();

                    SelectedAlbum = null;
                    m_selectedView = ServiceTreeNodes.None;

                    if (args.Cancelled) return;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    var albums = args.Result as List<Album>;
                    if (albums != null)
                    {
                        var hiddens = m_settings.GetHiddenList();
                        foreach (Album album in albums)
                        {
                            if (hiddens.Contains(album.VkId)) continue;

                            var tvi = CreateTreeItem(album, album.GetCleanTitle(),
                                                     @"pack://application:,,,/Images/photo_album.png");
                            tvi.FontSize = 14;
                            var usersNode = CreateTreeItem(ServiceTreeNodes.AlbumCustomers, "Покупатели",
                                                           @"pack://application:,,,/Images/group.png");
                            tvi.Items.Add(usersNode);

                            var prodsNode = CreateTreeItem(ServiceTreeNodes.AlbumProducts, "Товары",
                                                           @"pack://application:,,,/Images/cart.png");
                            tvi.Items.Add(prodsNode);

                            var payNode = CreateTreeItem(ServiceTreeNodes.AlbumPaymets, "Платежи",
                                                           @"pack://application:,,,/Images/coins.png");
                            tvi.Items.Add(payNode);


                            AlbumsView.Items.Add(tvi);
                        }
                    }

                    SetStatus();
                };

            bg.RunWorkerAsync();
        }

        private void ApplicationExitEventHandler(object sender, ExitEventArgs e)
        {
            if (m_settings.ClearReportsOnExit)
            {
                foreach (var file in Directory.GetFiles(m_settings.ReportsPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка альбомов рабочей группы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmdFileLoadAlbums_OnClick(object sender, RoutedEventArgs e)
        {
            if (m_settings.ExpirationDate.CompareTo(DateTime.Now) < 0)
            {
                var af = new AuthForm(3550451);
                if (af.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                m_settings.AccessToken = af.GetAccessToken();
                m_settings.LoggedUserId = af.GetTokenUserId();
                m_settings.SetExpiration(Int32.Parse(af.GetExiprationValue()));
            }

            var bg = new BackgroundWorker {WorkerReportsProgress = true};
            bg.ProgressChanged += (o, args) =>
                {
                    if (m_waitingWindow != null) m_waitingWindow.SetState(args.UserState.ToString());
                };
            bg.DoWork += AlbumsUpdateThreadProc;
            bg.RunWorkerCompleted += (o, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.Close();
                        m_waitingWindow = null;
                    }

                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                    }
                    lock (m_workersPool)
                    {
                        m_workersPool.Remove((BackgroundWorker)o);
                    }

                    // обновить список альбомов на форме
                    FillAlbumsListAsync();
                };
            lock (m_workersPool)
            {
                m_workersPool.Add(bg);
            }

            bg.RunWorkerAsync();
            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void AlbumsUpdateThreadProc(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            var worker = sender as BackgroundWorker;
            var mgr = new PhotosManager(m_settings.AccessToken);
            var vkWrapper = new VkObjectsWrapper();
            
            List<VkAlbum> vkAlbums;
            
            worker.ReportProgress(0, "Загрузка альбомов из сети");
            try
            {
                vkAlbums =
                    mgr.GetAlbumsForGroup(vkWrapper.GetVkGroupObject(m_settings.WorkGroupId, m_settings.LoggedUserId));
            }
            catch (Exception ex)
            {
                m_logger.ErrorException(ex);
                throw new BgWorkerException("Ошибка. Не удалось загрузить список альбомов группы.");
            }

            worker.ReportProgress(0, "Обновление репозитория");
            var albumsRepo = DbManger.GetInstance().GetAlbumsRepository();
            foreach (VkAlbum vkAlbum in vkAlbums)
            {
                Album storedAlbumObj;
                try
                {
                    storedAlbumObj = albumsRepo.GetByVkId(vkAlbum.Id);
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    continue;
                }

                if (storedAlbumObj == null)
                {
                    // добавим альбом в репозиторий
                    try
                    {
                        albumsRepo.Add(new Album
                            {
                                CreationDate = vkAlbum.CreationDate,
                                VkId = vkAlbum.Id,
                                Title = vkAlbum.Title,
                                ThumbImg = vkAlbum.ThumbUrl
                            });
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Ошибка. Не удалось сохранить информацию об альбоме.");
                    }
                }
            }
        }

        private void CmdFileExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void AlbumsView_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeViewItem;
            item.IsSelected = true;
            e.Handled = false;
        }
        
        private void CmdFileOpenSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var f = new SettingsWindow(this);
            f.ShowDialog();
            if (f.GetResult() == SettingsWindow.Result.Ok)
            {
                ClearDetailsView();
                FillAlbumsListAsync();    
            }
        }

        private void AlbumsViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            if (element == null) return;

            if (!(element.Parent is StackPanel)) return;
            var panel = element.Parent as StackPanel;
            if (!(panel.Parent is HeaderedItemsControl)) return;
            var p = ((element.Parent as StackPanel).Parent as HeaderedItemsControl).Tag as Product;

            if (p != null)
            {
                var f = new ProductEditWindow(this, p);
                f.ShowDialog();
                if (f.GetResult() == ProductEditWindow.Result.Saved)
                {
                    (panel.Children[1] as TextBlock).Text = p.Title;
                }
            }
        }

        private void ShowAlbumProductsDetails(Album album)
        {
            #region приготовить view
            var view = lvDetails.View as GridView;
            if (view == null) return;
            
            lvDetails.Items.Clear();
            view.Columns.Clear();

            view.Columns.Add(new GridViewColumn
                {
                    Width = 90,
                    Header = "Артикул",
                    DisplayMemberBinding = new Binding("CodeNumber"){Mode = BindingMode.OneWay } ,
                });
            view.Columns.Add(new GridViewColumn
                {
                    Width = 350,
                    Header = "Наименование",
                    DisplayMemberBinding = new Binding("Title"),
                });
            view.Columns.Add(new GridViewColumn
            {
                Width = 100,
                Header = "Цена",
                DisplayMemberBinding = new Binding("Price") { Converter = s_currencyVisualiser },
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 90,
                Header = "Минимум",
                DisplayMemberBinding = new Binding("MinAmount"),
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 90,
                Header = "Заказано",
                DisplayMemberBinding = new Binding("OrderedAmount"),
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 90,
                Header = "Статус",
                DisplayMemberBinding = new Binding("Status"),
            });
            #endregion

            var bgw = new BackgroundWorker() { WorkerReportsProgress = true };
            bgw.ProgressChanged += (sender, args) =>
                {
                    if (m_waitingWindow != null) m_waitingWindow.SetState(args.UserState.ToString());
                };
            bgw.DoWork += (sender, args) =>
                {
                    var worker = (BackgroundWorker) sender;
                    worker.ReportProgress(0, "Загрузка...");

                    var productRepository = DbManger.GetInstance().GetProductRepository();
                    var orderRepository = DbManger.GetInstance().GetOrderRepository();

                    List<Product> products;
                    List<Order> orders;
                    try
                    {
                        products = productRepository.GetAllFromAlbum(album);
                        orders = orderRepository.GetOrdersForAlbum(album);
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Ошибка. Не удалось получить информацию по альбому.");
                    }
                    var result = new List<ProductListViewItem>();

                    foreach (Product product in products)
                    {
                        var ordered = orders.Where(order => order.ProductId == product.Id).Sum(order => order.Amount);
                        result.Add(new ProductListViewItem(product) {OrderedAmount = ordered});
                    }

                    result.Sort((a, b) => String.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));

                    args.Result = result;
                };
            bgw.RunWorkerCompleted += (sender, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.Close();
                        m_waitingWindow = null;
                    }

                    var worker = sender as BackgroundWorker;
                    if (worker != null)
                    {
                        lock (m_workersPool)
                        {
                            m_workersPool.Remove(worker);
                        }
                    }

                    if(args.Cancelled) return;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }
                    foreach (ProductListViewItem plvi in ((List<ProductListViewItem>)args.Result))
                    {
                        lvDetails.Items.Add(plvi);
                    }
                };
            lock (m_workersPool)
            {
                m_workersPool.Add(bgw);
            }
            bgw.RunWorkerAsync();

            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }
        
        private void ShowAlbumPaymentsDetails(Album album)
        {
            #region приготовить view
            var view = lvDetails.View as GridView;
            lvDetails.Items.Clear();
            view.Columns.Clear();

            view.Columns.Add(new GridViewColumn
            {
                Width = 40,
                Header = "№",
                DisplayMemberBinding = new Binding("Id") { Mode = BindingMode.OneWay },
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 350,
                Header = "Покупатель",
                DisplayMemberBinding = new Binding("Title"),
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 100,
                Header = "Сумма",
                DisplayMemberBinding = new Binding("Amount"),
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 200,
                Header = "Дата",
                DisplayMemberBinding = new Binding("Date"),
            });
            view.Columns.Add(new GridViewColumn
            {
                Width = 300,
                Header = "Комментарий",
                DisplayMemberBinding = new Binding("Comment"),
            });
            #endregion

            var bgw = new BackgroundWorker { WorkerReportsProgress = true };
            bgw.ProgressChanged += (sender, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.SetState(args.UserState.ToString());
                    }
                };
            bgw.DoWork += (sender, args) =>
                {
                    var worker = (BackgroundWorker) sender;
                    worker.ReportProgress(0, "Загрузка");

                    var payRepo = DbManger.GetInstance().GetPaymentsRepository();
                    var custRepo = DbManger.GetInstance().GetCustomersRepository();

                    var albumPayments = payRepo.AllForAlbum(album);
                    var result = (from albumPayment in albumPayments
                                  let customer = custRepo.GetById(albumPayment.PayerId)
                                  select new PaymentsListViewItem(albumPayment, customer)).ToList();

                    result.Sort((i1, i2) => String.Compare(i1.Title, i2.Title, StringComparison.CurrentCultureIgnoreCase));
                    args.Result = result;
                };
            bgw.RunWorkerCompleted += (sender, args) =>
            {
                var worker = sender as BackgroundWorker;
                if (worker != null)
                {
                    lock (m_workersPool)
                    {
                        m_workersPool.Remove(worker);
                    }
                }
                if (m_waitingWindow != null)
                {
                    m_waitingWindow.Close();
                    m_waitingWindow = null;
                }

                if (args.Cancelled) return;
                if (args.Error != null)
                {
                    this.ShowError(args.Error.Message);
                    return;
                }

                foreach (PaymentsListViewItem plvi in ((List<PaymentsListViewItem>)args.Result))
                {
                    lvDetails.Items.Add(plvi);
                }
            };

            lock (m_workersPool)
            {
                m_workersPool.Add(bgw);
            }
            bgw.RunWorkerAsync();

            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void ShowAlbumCutomersDetails(Album a)
        {
            #region приготовить view

            var view = lvDetails.View as GridView;
            lvDetails.Items.Clear();
            view.Columns.Clear();

            view.Columns.Add(new GridViewColumn
                {
                    Width = 45,
                    Header = "Код",
                    DisplayMemberBinding = new Binding("Id")
                });
            view.Columns.Add(new GridViewColumn
                {
                    Width = 250,
                    Header = "Имя",
                    DisplayMemberBinding = new Binding("FullName"),
                });
            view.Columns.Add(new GridViewColumn
                {
                    Width = 100,
                    Header = "Комиссия",
                    DisplayMemberBinding = new Binding("AccountType"),
                });

//            view.Columns.Add(new GridViewColumn
//                {
//                    Width = 110,
//                    Header = "Позиций в заказе",
//                    DisplayMemberBinding = new Binding("OrderedItemsCount"),
//                });
            view.Columns.Add(new GridViewColumn
                {
                    Width = 110,
                    Header = "Сумма заказа",
                    DisplayMemberBinding = new Binding("CleanSum") { Converter = s_currencyVisualiser },
                });
            view.Columns.Add(new GridViewColumn
                {
                    Width = 110,
                    Header = "Комиссия",
                    DisplayMemberBinding = new Binding("CommissionSum") { Converter = s_currencyVisualiser },
                });
            view.Columns.Add(new GridViewColumn
            {
                Width = 110,
                Header = "Итог",
                DisplayMemberBinding = new Binding("TotalSum") { Converter = s_roundedCurrencyVisualiser },
            });

            view.Columns.Add(new GridViewColumn
            {
                Width = 90,
                Header = "Статус",
                DisplayMemberBinding = new Binding("Status") { Converter = s_currencyVisualiser },
            });

            #endregion

            var bgw = new BackgroundWorker {WorkerReportsProgress = true};
            bgw.ProgressChanged += (sender, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.SetState(args.UserState.ToString());
                    }
                };
            bgw.DoWork += (sender, args) =>
                {
                    var worker = sender as BackgroundWorker;

                    var album = args.Argument as Album;
                    var orderRepository = DbManger.GetInstance().GetOrderRepository();
                    var customersRepository = DbManger.GetInstance().GetCustomersRepository();
                    var payments = DbManger.GetInstance().GetPaymentsRepository();

                    List<Order> albumOrders = null;
                    
                    worker.ReportProgress(0, "Получение заказов для альбома");
                    try
                    {
                        albumOrders = orderRepository.GetOrdersForAlbum(album);
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Не удалось получить заказы альбома из БД.");
                    }

                    var result = new List<CustomerListViewItem>();
                    foreach (IGrouping<int, Order> ordersGroup in albumOrders.GroupBy(order => order.CustomerId))
                    {
                        Customer custObj;
                        try
                        {
                            worker.ReportProgress(0, "Получение данных о заказчике.");
                            custObj = customersRepository.GetById(ordersGroup.Key);
                        }
                        catch (Exception exception)
                        {
                            m_logger.ErrorException(exception);
                            throw new BgWorkerException("Ошибка получения данных из БД");
                        }
                        if (custObj == null)
                        {
                            m_logger.ErrorFormat(
                                "Обнаружено нарушение целостности БД!. Для заказа '{0}' не удалось найти покупателя",
                                ordersGroup.Key);
                            continue;
                        }
                        if (custObj.GetCommissionInfo() == null)
                        {
                            throw new BgWorkerException(String.Format("Ошибка: Для покупателя '{0}' неверно установлена ставка комиссии.",
                                custObj.GetFullName()));
                        }
                        
                        // создание объекта записи в таблице
                        var clvi = new CustomerListViewItem(custObj);// {OrderedItemsCount = 0};
                        foreach (Order order in ordersGroup)
                        {
                            Product productInfo = null;
                            long totalProductOrders;

                            worker.ReportProgress(0, "Получение информации о продукте");
                            try
                            {
                                productInfo = order.GetOrderedProduct();
                                
                                // посчитать сколько всего заказано этого товара
                                totalProductOrders = orderRepository.GetProductTotalOrderedAmount(productInfo);
                            }
                            catch (Exception exception)
                            {
                                m_logger.ErrorException(exception);
                                continue;
                            }
                            
                            // добавляем к сумме заказа стоимость очередной позиции (кол-во на стоимость единицы)
                            clvi.CleanSum += order.Amount * productInfo.Price;
                            clvi.HasPartialPosition = (totalProductOrders < productInfo.MinAmount && totalProductOrders > 0) || (totalProductOrders == 0);
                        }
                        
                        if (clvi.CleanSum == 0) continue;

                        payments.AllForCustomerInAlbum(custObj, SelectedAlbum)
                                    .ForEach(payment => clvi.Payment += payment.Amount);

                        result.Add(clvi);
                    }

                    result.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.CurrentCultureIgnoreCase));
                    args.Result = result;
                };
            bgw.RunWorkerCompleted += (sender, args) =>
                {
                    var worker = sender as BackgroundWorker;
                    if (worker != null)
                    {
                        lock (m_workersPool)
                        {
                            m_workersPool.Remove(worker);
                        }
                    }
                    
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.Close();
                        m_waitingWindow = null;
                    }

                    if (args.Cancelled) return;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    var list = args.Result as List<CustomerListViewItem>;
                    if (list != null)
                    {
                        foreach (var clvi in list)
                        {
                            lvDetails.Items.Add(clvi);
                        }
                    }
                };
            lock (m_workersPool)
            {
                m_workersPool.Add(bgw);
            }
            bgw.RunWorkerAsync(a);

            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void AlbumsView_OnSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = false;
            SetStatus();

            HeaderedItemsControl item = null;
            if (e.Source is TextBlock)
            {
                var panel = (e.Source as TextBlock).Parent as StackPanel;
                if (panel == null) return;
                item = panel.Parent as HeaderedItemsControl;
            }
            else if (e.Source is TreeViewItem)
            {
                item = e.Source as HeaderedItemsControl;
            }
            else if (e.Source is Image)
            {
                var panel = (e.Source as Image).Parent as StackPanel;
                if (panel == null) return;
                item = panel.Parent as HeaderedItemsControl;
            }
            if (item == null || item.Parent == null) return;

            ClearDetailsView();

            if (item.Tag is Album)
            {
                SelectedAlbum = item.Tag as Album;
            }
            else
            {
                SelectedAlbum = (item.Parent as HeaderedItemsControl).Tag as Album;
            }

            tbSearchBox.Clear();
            if (item.Tag.Equals(ServiceTreeNodes.AlbumProducts))
            {
                m_selectedView = ServiceTreeNodes.AlbumProducts;
                ShowAlbumProductsDetails(SelectedAlbum);
            }
            else if (item.Tag.Equals(ServiceTreeNodes.AlbumCustomers))
            {
                m_selectedView = ServiceTreeNodes.AlbumCustomers;
                ShowAlbumCutomersDetails(SelectedAlbum);
            }
            else if (item.Tag.Equals(ServiceTreeNodes.AlbumPaymets))
            {
                m_selectedView = ServiceTreeNodes.AlbumPaymets;
                ShowAlbumPaymentsDetails(SelectedAlbum);
            }
            else
            {
                m_selectedView = ServiceTreeNodes.None;
            }
        }

        private void DetailsListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.Source as ListViewItem) == null) return;

            var item = (e.Source as ListViewItem).Content as CustomListViewItem;
            if(item != null) ShowDialogForListViewItem(item);
        }

        private void cmdAlbumLoadData_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null)
            {
                this.ShowError("Для выполнения этой операции необходимо выбрать хотябы один альбом из списка.");
                return;
            }

            if (!this.ShowQuestion(String.Format("Обновить информацию по альбому '{0}'?", SelectedAlbum.GetCleanTitle()))) return;

            if (m_settings.ExpirationDate.CompareTo(DateTime.Now) < 0)
            {
                var af = new AuthForm(3550451);
                if (af.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    this.ShowError("Для работы нужно авторизоваться в сети!");
                    return;
                }

                m_settings.AccessToken = af.GetAccessToken();
                m_settings.LoggedUserId = af.GetTokenUserId();
                m_settings.SetExpiration(Int32.Parse(af.GetExiprationValue()));
            }

            var bg = new BackgroundWorker { WorkerReportsProgress = true };
            bg.DoWork += UpdatePhotosInAlbumThreadProc;
            bg.ProgressChanged += (o, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.SetState(args.UserState.ToString());
                    }
                };
            bg.RunWorkerCompleted += (o, args) =>
                {
                    var worker = sender as BackgroundWorker;
                    if (worker != null)
                    {
                        lock (m_workersPool)
                        {
                            m_workersPool.Remove(worker);
                        }
                    }

                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.Close();
                        m_waitingWindow = null;
                    }

                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    if (m_selectedView == ServiceTreeNodes.AlbumPaymets)
                    {
                        ShowAlbumPaymentsDetails(SelectedAlbum);
                    }
                    else if (m_selectedView == ServiceTreeNodes.AlbumProducts)
                    {
                        ShowAlbumProductsDetails(SelectedAlbum);
                    }
                    else if (m_selectedView == ServiceTreeNodes.AlbumCustomers)
                    {
                        ShowAlbumCutomersDetails(SelectedAlbum);
                    }
                };

            lock (m_workersPool)
            {
                m_workersPool.Add(bg);
            }
            bg.RunWorkerAsync(SelectedAlbum);
            
            m_waitingWindow =new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void cmdAlbumDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null) return;
            if (!this.ShowQuestion(String.Format("Удалить альбом '{0}'?", SelectedAlbum.GetCleanTitle())))
            {
                return;
            }

            var db = DbManger.GetInstance();
            var prRepo = db.GetProductRepository();
            var orderRepo = db.GetOrderRepository();
            var albumRepo = db.GetAlbumsRepository();

            int albumOrdersCount;
            try
            {
                albumOrdersCount = orderRepo.GetOrdersForAlbum(SelectedAlbum).Count;
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                this.ShowError("Ошибка. Соединение с БД потеряно.", exception.GetType().Name);
                return;
            }

            if (albumOrdersCount > 0)
            {
                this.ShowError("Нельзя удалить альбом для которого зарегистрированы заказы.");
                return;
            }

            SetStatus("Удаление продуктов из альбома");
            List<Product> albumProds;
            try
            {
                albumProds = prRepo.GetAllFromAlbum(SelectedAlbum);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                albumProds = new List<Product>(0);
            }

            foreach (Product albumProd in albumProds)
            {
                try
                {
                    prRepo.Delete(albumProd);
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                }
            }

            SetStatus("Удаление альбома");
            try
            {
                albumRepo.Delete(SelectedAlbum);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                this.ShowError("Ошибка, не удалось удалить альбом.", exception.GetType().Name);
            }

            SetStatus();

            FillAlbumsListAsync();
        }

        private void btnExportCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null || m_selectedView == ServiceTreeNodes.None)
            {
                this.ShowMessage("Для выполнения операции выберите альбом и категорию.");
                return;
            }

            var f = new AskExportSettingsWindow(this, SelectedAlbum, m_selectedView.ToString()) { Owner = this };
            f.ShowDialog();

            if (f.GetResult() != AskExportSettingsWindow.Result.Ok) return;

            var selectedFormatter = f.GetSelectedFormatter();
            selectedFormatter.ProgressChanged += (o, args) => SetStatus(args.StateMessage);
            
            var bg = new BackgroundWorker {WorkerReportsProgress = true};
            bg.ProgressChanged += (o, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.SetState(args.UserState.ToString());
                    }
                };
            bg.DoWork += (o, args) =>
                {
                    var worker = (BackgroundWorker) o;
                    var formatter = (ExportFormatterBase) args.Argument;
                    
                    worker.ReportProgress(0, "Формирование отчета...");
                    if (m_selectedView == ServiceTreeNodes.AlbumProducts)
                    {
                        formatter.ExportProductsSummary();
                    }
                    else if (m_selectedView == ServiceTreeNodes.AlbumCustomers)
                    {
                        formatter.ExportCustomersSummary();
                    }
                    else if (m_selectedView == ServiceTreeNodes.AlbumPaymets)
                    {
                        formatter.ExportPaymentsSummary();
                    }
                    else
                    {
                        throw new BgWorkerException("Не найдены отчеты для выбранного представления!");
                    }

                    args.Result = formatter;
                };
            bg.RunWorkerCompleted += (o, args) =>
                {
                    var worker = o as BackgroundWorker;
                    if (worker != null)
                    {
                        lock (m_workersPool)
                        {
                            m_workersPool.Remove(worker);
                        }
                    }

                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.Close();
                        m_waitingWindow = null;
                    }

                    if (args.Cancelled) return;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }
                    
                    var formatter = (ExportFormatterBase)args.Result;
                    if (formatter is IFileExporter)
                    {
                        if (this.ShowQuestion("Экспорт выполнен успешно. Открыть файл?"))
                        {
                            Process.Start(new ProcessStartInfo(((IFileExporter)formatter).Filename) { Verb = "open" });
                        }
                    }
                    else if (formatter is IReportExporter)
                    {
                        var doc = ((IReportExporter)formatter).GetDocument();
                        var f1 = new ReportsViewerWindow(doc) { Owner = this };
                        f1.Show();
                    }
                };

            lock (m_workersPool)
            {
                m_workersPool.Add(bg);
            }
            
            bg.RunWorkerAsync(selectedFormatter);
            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void lvDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ISelectable item in e.AddedItems.OfType<ISelectable>())
            {
                item.Select();
            }
            foreach (ISelectable item in e.RemovedItems.OfType<ISelectable>())
            {
                item.Unselect();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_selectedView == ServiceTreeNodes.None) return;
            
            var content = tbSearchBox.Text;
            var searchType = (TableSearchType)((ComboBoxItem) cbSearchFieldType.SelectedItem).Tag;

            for (int i = 0; i < lvDetails.Items.Count; i++)
            {
                var item = lvDetails.Items[i] as CustomListViewItem;
                if(item == null) continue;

                if ((searchType == TableSearchType.Title && item.TitleSearchHit(content)) ||
                    (searchType == TableSearchType.CodeNumber && item.CodeNumberSearchHit(content)))
                {
                    lvDetails.SelectedIndex = i;
                    lvDetails.ScrollIntoView(lvDetails.Items[i]);
                    break;
                }
            }
        }


        private void cmdAlbumHide_OnClick(object sender, RoutedEventArgs e)
        {
            var hiddens = m_settings.GetHiddenList();
            hiddens.Add(SelectedAlbum.VkId);
            m_settings.SetHiddenList(hiddens);

            ClearDetailsView();
            FillAlbumsListAsync();
        }

        private void cmdAlbumAddProduct_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null)
            {
                this.ShowError("Необходимо выбрать альбом.");
                return;
            }

            var p = new Product
                {
                    AlbumId = SelectedAlbum.Id,
                    VkId = Int32.MinValue,
                    Id = Int32.MinValue,
                    MinAmount = 1
                };
            var f = new ProductEditWindow(this, p);
            f.ShowDialog();

            if (f.GetResult() == ProductEditWindow.Result.Saved && m_selectedView == ServiceTreeNodes.AlbumProducts)
            {
                ShowAlbumProductsDetails(SelectedAlbum);
            }
        }

        private void cmdOpenDeliveryList_OnClick(object sender, RoutedEventArgs e)
        {
            new DeliveryTypeSelection(this).ShowDialog();
        }

        private void btnOpenReportsFolder_Click(object sender, RoutedEventArgs e)
        {
            new Process
                {
                    StartInfo =
                        {
                            FileName = "explorer.exe",
                            Arguments = m_settings.ReportsPath
                        }
                }.Start();

        }

        private void CmdFileAddAlbum_OnClick(object sender, RoutedEventArgs e)
        {
            var a = new Album
                {
                    CreationDate = DateTime.Now,
                    VkId = 0,
                    ThumbImg = "",
                    Title = "",
                };
            var w = new AddAlbumWindow(a);
            var res = w.ShowDialog();
            if (!res.HasValue || !res.Value) return;

            var repo = DbManger.GetInstance().GetAlbumsRepository();
            try
            {
                repo.Add(a);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                this.ShowError("Не удалось добавить альбом. " + String.Format("({0}) {1}", exception.GetType().Name, exception.Message));
                return;
            }

            FillAlbumsListAsync();
        }

        private void cmdAlbumDeleteProduct_OnClick(object sender, RoutedEventArgs e)
        {
            var item = lvDetails.SelectedItems.Count > 0 ? lvDetails.SelectedItems[0] as ProductListViewItem : null;
            if (item == null) return;

            if(this.ShowQuestion(String.Format("Удалить '{0}' из закупки?"))) item.Source.
        }
    }
}
