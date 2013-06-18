using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VkApiNet.Vk;
using VkShopManager.Core;
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager
{
    public partial class MainWindow : Window
    {
        private void ShowDialogForListViewItem(CustomListViewItem item)
        {
            if (item is ProductListViewItem)
            {
                var prod = item as ProductListViewItem;

                // детализация заказа в разрезе продукта
                var f = new ProductCustomersView(this, prod);
                f.ShowDialog();
            }
            else if (item is CustomerListViewItem)
            {
                var customer = item as CustomerListViewItem;

                // детализация заказа в разрезе покупателя
                var f = new OrderEditWindow(this, customer, SelectedAlbum);
                f.ShowDialog();
            }
        }

        private TreeViewItem CreateTreeItem(object tag, string text, string imagePath)
        {
            var node = new TreeViewItem { Tag = tag };
            var headerContainer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(1) };
            headerContainer.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(imagePath))
            });
            headerContainer.Children.Add(new TextBlock
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = text
            });
            node.Header = headerContainer;
            return node;
        }

        private void SetStatus(string msg = "")
        {
            if (!AppStatusText.Dispatcher.CheckAccess())
            {
                AppStatusText.Dispatcher.BeginInvoke(new Action<string>(SetStatus), DispatcherPriority.Normal, msg);
                return;
            }

            AppStatusText.Content = msg;
        }
        private void ClearDetailsView()
        {
            lvDetails.Items.Clear();
            (lvDetails.View as GridView).Columns.Clear();
        }
        private void UpdatePhotosInAlbumThreadProc(object sender, DoWorkEventArgs args)
        {
            var worker = sender as BackgroundWorker;
            var album = args.Argument as Album;
            if (album == null) return;

            var vkWraper = new VkObjectsWrapper();
            var photosMgr = new PhotosManager(m_settings.AccessToken);
            var usersMgr = new UsersManager(m_settings.AccessToken);

            var commentsRepo = DbManger.GetInstance().GetCommentsRepository();
            var customersRepo = DbManger.GetInstance().GetCustomersRepository();
            var ordersRepo = DbManger.GetInstance().GetOrderRepository();
            var prodsRepo = DbManger.GetInstance().GetProductRepository();

            var workGroup = vkWraper.GetVkGroupObject(m_settings.WorkGroupId, m_settings.LoggedUserId);
            List<VkPhoto> vkPhotos;
            try
            {
                worker.ReportProgress(0, "Загрузка фотографий альбома");
                vkPhotos = photosMgr.GetAlbumPhotos(vkWraper.GetVkGroupAlbum(workGroup, album.VkId));
            }
            catch (VkMethodInvocationException vkError)
            {
                m_logger.ErrorException(vkError);
                throw new BgWorkerException(vkError.Message);
            }
            catch (Exception e)
            {
                m_logger.ErrorException(e);
                throw new BgWorkerException("Ошибка. Не удалось загрузить фотографии для альбома '" + album.Title + "'");
            }

            int total = vkPhotos.Count;
            for (var n = 0; n < vkPhotos.Count; n++)
            {
                worker.ReportProgress(0, String.Format("Обработка {0} из {1}", (n + 1), total));
                if (vkPhotos[n].CommentsCount == 0) continue;

                Product productInfo = null;
                try
                {
                    var products = prodsRepo.GetByVkId(vkPhotos[n].Id);
                    if (products.Count > 0)
                    {
                        // по этому коду найдены продукты, найдем продукт из текущего альбома
                        productInfo = products.FirstOrDefault(product => product.AlbumId == album.Id);
                        if (productInfo == null)
                        {
                            // продукты с таким кодом есть, но они не в этом альбоме -> скопируем
                            var p = new Product
                            {
                                AlbumId = album.Id,
                                GenericUrl = products[0].GenericUrl,
                                ImageFile = products[0].ImageFile,
                                MinAmount = products[0].MinAmount,
                                Price = products[0].Price,
                                Title = products[0].Title,
                                VkId = products[0].VkId
                            };
                            prodsRepo.Add(p);
                            productInfo = p;
                            
                            // var ordersToTranslate = new List<Order>();
//                            foreach (var product in products)
//                            {
//                                var orders = ordersRepo.GetOrdersForProduct(product);
//                                foreach (var order in orders)
//                                {
//                                    try
//                                    {
//                                        ordersRepo.Add(new Order
//                                            {
//                                                Amount = order.Amount,
//                                                Comment = order.Comment,
//                                                CustomerId = order.CustomerId,
//                                                Date = order.Date,
//                                                InitialVkCommentId = order.InitialVkCommentId,
//                                                ProductId = productInfo.Id
//                                            });
//                                    }
//                                    catch (Exception exception)
//                                    {
//                                        m_logger.ErrorException(exception);
//                                    }
//                                }
//                            }

                            m_logger.DebugFormat("Создан дубликат продукта '{0}' с ID={1}", p.Title, p.Id);
                        }
                    }
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    throw new BgWorkerException("Ошибка. Не удалось загрузить описание продукта из БД.");
                }

                #region Добавление нового продукта в репозиторий если productInfo = null
                if (productInfo == null)
                {
                    // добавим продукт в репозиторий
                    productInfo = new Product
                    {
                        AlbumId = album.Id,
                        VkId = vkPhotos[n].Id,
                    };

                    #region если есть url к изображению, надо его закачать
                    if (!String.IsNullOrEmpty(vkPhotos[n].SourceUrl))
                    {
                        var name = String.Format("{0}_{1}", album.VkId, vkPhotos[n].Id);
                        var imgExtensions = new List<string> { ".jpg", ".png", ".gif", ".bmp" };
                        foreach (string imgExtension in imgExtensions)
                        {
                            if (!vkPhotos[n].SourceUrl.EndsWith(imgExtension)) continue;
                            name += imgExtension;
                            break;
                        }
                        var remoteFile = new FileDownloader(new Uri(vkPhotos[n].SourceUrl));
                        try
                        {
                            var file = System.IO.Path.Combine(m_settings.GalleryPath, name);
                            remoteFile.DownloadTo(file);
                            productInfo.ImageFile = name;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    #endregion

                    // парсинг описания
                    productInfo.ParsePhotoDescription(vkPhotos[n].Text);

                    // проверка пользователем
                    var dialogResult = ProductCheckWindow.Result.Ignore;
                    var sourceText = vkPhotos[n].Text;
                    var handler = Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                         new Action(() =>
                                                             {
                                                                 var f = new ProductCheckWindow(this, productInfo,
                                                                                                sourceText);
                                                             f.ShowDialog();
                                                             dialogResult = f.GetResult();
                                                         }));
                    handler.Wait();

                    if (dialogResult == ProductCheckWindow.Result.Break)
                    {
                        break;
                    }
                    else if (dialogResult == ProductCheckWindow.Result.Accept)
                    {
                        try
                        {
                            prodsRepo.Add(productInfo);
                        }
                        catch (Exception e)
                        {
                            m_logger.ErrorException(e);
                            throw new BgWorkerException("Ошибка. Не удалось сохранить информацию о товаре.");
                        }
                    }
                }
                #endregion

                // загрузить комменты к этой фотографии
                List<VkComment> vkComments;
                try
                {
                    vkComments = photosMgr.GetPhotoComments(vkPhotos[n]);
                }
                catch (VkMethodInvocationException vkError)
                {
                    m_logger.ErrorException(vkError);
                    throw new BgWorkerException(vkError.Message);
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    throw new BgWorkerException("Не удалось получить комментарии к фото.");
                }

                foreach (VkComment vkComment in vkComments)
                {
                    // создание объекта хранимого комментария
                    var parsedComment = new ParsedComment
                    {
                        ParsingDate = DateTime.Now,
                        VkId = vkComment.Id,
                    };
                    parsedComment.SetUniqueKey(album.Id, vkPhotos[n].Id);

                    try
                    {
                        // проверка, не обрабатывался ли этот комментарий раньше
                        if (commentsRepo.Contains(parsedComment)) continue;
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Ошибка: База данных недоступна!");
                    }

                    // поиск юзера оставившего этот комментарий
                    Customer customer;
                    try
                    {
                        customer = customersRepo.GetByVkId(vkComment.SenderId);
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        throw new BgWorkerException("Не удалось получить информацию о пользователе из БД!");
                    }

                    #region заведение заказчика в базе
                    if (customer == null)
                    {
                        // если нашего заказчика нет в базе, значит он заказывает первый раз - добавим его в базу
                        VkUser vkUser;
                        try
                        {
                            // скачиваем инфо из Вконтакте
                            vkUser = usersMgr.GetUserById(vkComment.SenderId);
                        }
                        catch (Exception exception)
                        {
                            m_logger.ErrorException(exception);
                            throw new BgWorkerException("Не удалось получить информацию о пользователе из Вконтакте.");
                        }
                        customer = new Customer
                        {
                            FirstName = vkUser.FirstName,
                            LastName = vkUser.LastName,
                            VkId = vkUser.Id,
                        };
                        try
                        {
                            customersRepo.Add(customer);
                        }
                        catch (Exception exception)
                        {
                            m_logger.ErrorException(exception);
                            throw new BgWorkerException("Ошибка. Не удалось сохранить информацию в БД.");
                        }

                        DispatcherOperation h = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            var f = new CustomerEditWindow(this, customer);
                            f.ShowDialog();
                        }));
                        h.Wait();
                    }

                    #endregion

                    parsedComment.Message = vkComment.Message;
                    parsedComment.SenderName = customer.GetFullName();
                    parsedComment.PostingDate = vkComment.Date.ToString();
                    //String.Format("{0}|{1}|{2}", customer.GetFullName(), vkComment.Message, vkComment.Date);

                    // начальная инициализация
                    var orderObj = new Order
                    {
                        ProductId = productInfo.Id,
                        CustomerId = customer.Id,
                        Comment = "",
                        Date = vkComment.Date,
                        Amount = 0
                    };
                    var requestedOperation = OrderOperation.Add;

                    #region парсинг текста комментария

                    /*
                     * ищем чтото типа "+N", "-N"
                     * */
                    var msg = vkComment.Message;
                    for (var i = 0; i < msg.Length; i++)
                    {
                        if (!Char.IsDigit(msg[i]) && msg[i] != '-' && msg[i] != '+' && msg[i] != ' ')
                        {
                            msg = msg.Replace(msg[i], ' ');
                        }
                    }
                    msg = msg.Replace(" ", "");
                    if (msg.Length > 0)
                    {
                        // если чтото осталось от сообщения
                        if (msg[0] == '+')
                        {
                            // добавляем чтото к заказу
                            requestedOperation = OrderOperation.Add;
                            for (int i = 0; i < msg.Length; i++)
                            {
                                if (!Char.IsDigit(msg[i]) && msg[i] != ' ')
                                {
                                    msg = msg.Replace(msg[i], ' ');
                                }
                                msg = msg.Replace(" ", "");
                            }
                            if (msg.Length > 0)
                            {
                                try
                                {
                                    orderObj.Amount = Int32.Parse(msg);
                                }
                                catch (Exception)
                                {
                                    orderObj.Amount = 0;
                                }
                            }
                        }
                        else if (msg[0] == '-')
                        {
                            requestedOperation = OrderOperation.Remove;
                            for (int i = 0; i < msg.Length; i++)
                            {
                                if (!Char.IsDigit(msg[i]) && msg[i] != ' ')
                                {
                                    msg = msg.Replace(msg[i], ' ');
                                }
                                msg = msg.Replace(" ", "");
                            }
                            if (msg.Length > 0)
                            {
                                try{orderObj.Amount = Int32.Parse(msg);}
                                catch (Exception)
                                {
                                    orderObj.Amount = 0;
                                }
                            }
                        }
                    }

                    #endregion

                    // открыть форму проверки комметнария
                    var dialogResult = CommentCheckWindow.Result.Reject;
                    var resultAction = OrderOperation.Add;
                    msg = vkComment.Message;
                    DispatcherOperation ch = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        var f = new CommentCheckWindow(this, msg, customer, orderObj, productInfo, requestedOperation);
                        f.ShowDialog();

                        dialogResult = f.GetResult();
                        resultAction = f.GetRequestedOperation();
                    }));
                    ch.Wait();

                    if (dialogResult == CommentCheckWindow.Result.Accept)
                    {
                        // сохранить объект комметария в БД
                        try
                        {
                            commentsRepo.Add(parsedComment);
                            orderObj.InitialVkCommentId = parsedComment.Id;
                        }
                        catch (Exception exception)
                        {
                            m_logger.ErrorException(exception);
                            throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                        }

                        if (resultAction == OrderOperation.Add)
                        {
                            #region сохранить заказ в БД или добавить к существующему
                            List<Order> personOrders;
                            try
                            {
                                personOrders = ordersRepo.GetOrdersForCustomerFromAlbum(customer, album);
                            }
                            catch (Exception exception)
                            {
                                m_logger.ErrorException(exception);
                                throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                            }
                            var storedOrder = personOrders.FirstOrDefault(t => t.ProductId == orderObj.ProductId);
                            if (storedOrder == null)
                            {
                                try
                                {
                                    ordersRepo.Add(orderObj);
                                }
                                catch (Exception exception)
                                {
                                    m_logger.ErrorException(exception);
                                    throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                                }
                            }
                            else
                            {
                                storedOrder.Amount += orderObj.Amount;
                                try
                                {
                                    ordersRepo.Update(storedOrder);
                                }
                                catch (Exception exception)
                                {
                                    m_logger.ErrorException(exception);
                                    throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                                }
                            }

                            #endregion
                        }
                        else if (resultAction == OrderOperation.Remove)
                        {
                            #region удалить в БД из заказов все что связано с этим товаром у этого заказчика

                            List<Order> personOrders;
                            try
                            {
                                personOrders = ordersRepo.GetOrdersForCustomerFromAlbum(customer, album);
                            }
                            catch (Exception exception)
                            {
                                m_logger.ErrorException(exception);
                                throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                            }
                            bool clean = true;
                            for (int i = 0; i < personOrders.Count; i++)
                            {
                                if (personOrders[i].ProductId == orderObj.ProductId)
                                {
                                    try
                                    {
                                        ordersRepo.Delete(personOrders[i]);
                                    }
                                    catch (Exception exception)
                                    {
                                        m_logger.ErrorException(exception);
                                        clean = false;
                                    }
                                }
                            }
                            if (!clean)
                            {
                                throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                            }

                            #endregion
                        }
                        else if (resultAction == OrderOperation.Decrease)
                        {
                            #region уменьшить кол-во заказаного товара в позиции

                            List<Order> personOrders;
                            try
                            {
                                personOrders = ordersRepo.GetOrdersForCustomerFromAlbum(customer, album);
                            }
                            catch (Exception exception)
                            {
                                m_logger.ErrorException(exception);
                                throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                            }
                            var storedOrder = personOrders.FirstOrDefault(t => t.ProductId == orderObj.ProductId);
                            if (storedOrder != null)
                            {
                                storedOrder.Amount -= orderObj.Amount;
                                if (storedOrder.Amount > 0)
                                {
                                    try
                                    {
                                        ordersRepo.Update(storedOrder);
                                    }
                                    catch (Exception exception)
                                    {
                                        m_logger.ErrorException(exception);
                                        throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                                    }
                                }
                                else
                                {
                                    // позиция нулевая, можно удалить
                                    try
                                    {
                                        ordersRepo.Delete(storedOrder);
                                    }
                                    catch (Exception exception)
                                    {
                                        m_logger.ErrorException(exception);
                                        throw new BgWorkerException("Ошибка соединения с БД. Операция не выполнена");
                                    }
                                }
                            }
                            else
                            {
                                m_logger.DebugFormat("Внимание! Невозможно уменьшить заказ которого нет!");
                            }

                            #endregion
                        }
                        else
                        {
                            // 'Forget' action is skiped too!
                        }
                    }
                }
            }
        }

        private void cmdOpenCustomrsList_OnClick(object sender, RoutedEventArgs e)
        {
            var f = new CustomersSelectionWindow(this);
            f.ShowDialog();
        }

        private void cmdPrintDeliveryListClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (SelectedAlbum == null)
            {
                this.ShowMessage("Для выполнения операции выберите альбом.");
                return;
            }

            var f = new AskExportSettingsWindow(this, SelectedAlbum, @"ЛистДоставки");
            f.ShowDialog();

            if (f.GetResult() != AskExportSettingsWindow.Result.Ok) return;

            var formatter = f.GetSelectedFormatter();
            var bg = new BackgroundWorker {WorkerReportsProgress = true};
            bg.ProgressChanged += (o, args) =>
                {
                    if (m_waitingWindow != null)
                    {
                        m_waitingWindow.SetState(args.UserState);
                    }
                };
            bg.DoWork += (o, args) =>
                {
                    var worker = (BackgroundWorker) o;

                    worker.ReportProgress(0, "Выполняется экспорт");
                    var formObj = (ExportFormatterBase) args.Argument;
                    formObj.ExportDeliveryList();
                    args.Result = formObj;
                };
            bg.RunWorkerCompleted += (o, args) =>
                {
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

                    var exporter = (ExportFormatterBase) args.Result;
                    if (exporter is IFileExporter)
                    {
                        if (this.ShowQuestion("Экспорт выполнен успешно. Открыть файл?"))
                        {
                            Process.Start(new ProcessStartInfo(((IFileExporter)exporter).Filename) { Verb = "open" });
                        }
                    }
                    else if (exporter is IReportExporter)
                    {
                        // open report in viewer
                        var f1 = new ReportsViewerWindow(((IReportExporter) exporter).GetDocument()){Owner = this};
                        f1.ShowDialog();
                    }
                    
                };
            bg.RunWorkerAsync(formatter);

            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }

        private void CmdTest_OnClick(object sender, RoutedEventArgs e)
        {
            var bg = new BackgroundWorker() {WorkerReportsProgress = true};
            bg.ProgressChanged += (o, args) => m_waitingWindow.SetState(args.UserState);
            bg.DoWork += (o, args) =>
                {
                    var w = (BackgroundWorker) o;

                    w.ReportProgress(0, "Ожидание");
                    System.Threading.Thread.Sleep(1000);

                    w.ReportProgress(0, "Открытие диалога");
                    var h = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            var f = new CustomersSelectionWindow(this);
                            f.ShowDialog();
                        }));
                    h.Wait();

                    w.ReportProgress(0, "Ожидание");
                    System.Threading.Thread.Sleep(1000);
                };
            bg.RunWorkerCompleted += (o, args) =>
                {
                    m_waitingWindow.Close();
                    m_waitingWindow = null;
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                    }
                };
            bg.RunWorkerAsync();

            m_waitingWindow = new WaitingWindow(this);
            m_waitingWindow.ShowDialog();
        }
    }
}
