using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager.Core
{
    class ReportsExportFormatter : ExportFormatterBase, IReportExporter
    {
        private class CustomerOrderItem
        {
            public bool IsPartial;
            public string Title;
            public int Amount;
            public decimal Price;
            public decimal Total { get { return Amount * Price; } }
            public string Comment { get; set; }
        }

        private class CustomerOrderInfo
        {
            public Customer Customer { get; private set; }
            public readonly List<CustomerOrderItem> Items;

            public CustomerOrderInfo(Customer customer)
            {
                Customer = customer;
                Items = new List<CustomerOrderItem>(0);
            }

            public decimal GetOrderCleanSum()
            {
                return Items.Sum(item => item.Total);
            }
            public decimal GetOrderComission()
            {
                return (Items.Sum(item => item.Amount*item.Price)*(Customer.GetCommissionInfo().Rate)/100);
            }
        }

        private class ProductOrderItem
        {
            public string Title { get; set; }

            public decimal Price { get; set; }

            public int Amount { get; set; }

            public decimal Total
            {
                get { return this.Amount*this.Price; }
            }
        }
        /*
         *  !!!
         *  m_document - потокозависимый объект. потому для работы с ним здесь
         *  надо пользоваться диспетчером
         *  !!!
         *  
         * */
        private readonly FlowDocument m_document;
        private readonly log4net.ILog m_logger;

        public ReportsExportFormatter(Album album) : base(album)
        {
            m_logger = log4net.LogManager.GetLogger("ReportsExportFormatter");
            m_document = new FlowDocument {FontFamily = new FontFamily("Verdana"), FontSize = 10};
        }

        public override void ExportCustomersSummary()
        {
            ClearDocument();

            var orderRepository = DbManger.GetInstance().GetOrderRepository();
            var customersRepository = DbManger.GetInstance().GetCustomersRepository();
            var productRepository = DbManger.GetInstance().GetProductRepository();
            var ratesRepository = DbManger.GetInstance().GetRatesRepository();

            List<Order> albumOrders;

            OnProgressChanged("Получение заказов для альбома");
            try
            {
                albumOrders = orderRepository.GetOrdersForAlbum(WorkingAlbum);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                throw new ApplicationException("Не удалось получить заказы альбома из БД");
            }

            var customers = new List<Customer>();
            IEnumerable<IGrouping<int, Order>> byCustomerOrders = albumOrders.GroupBy(order => order.CustomerId);
            foreach (IGrouping<int, Order> byCustomerOrder in byCustomerOrders)
            {
                Customer custObj;
                try
                {
                    OnProgressChanged("Получение данных о заказчике.");
                    custObj = customersRepository.GetById(byCustomerOrder.Key);
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    throw new ApplicationException("Не удалось получить информацию о заказчике " + byCustomerOrder.Key);
                }

                customers.Add(custObj);
            }
            customers.Sort((c1, c2) => String.Compare(c1.LastName, c2.LastName, StringComparison.CurrentCultureIgnoreCase));

            var bills = new List<CustomerOrderInfo>();
            foreach (var custObj in customers)
            {
                // прелоад информации о способе доставки покупателю
                custObj.GetDeliveryInfo();

                // информация о комиссии пользователя
//                ManagedRate comission;
//                try
//                {
//                    comission = ratesRepository.GetById(custObj.AccountTypeId);
//                }
//                catch (Exception exception)
//                {
//                    m_logger.ErrorException(exception);
//                    comission = new ManagedRate { Comment = "???", Id = 0, Rate = 0 };
//                }

                if (custObj.GetCommissionInfo() == null)
                    throw new ApplicationException("Для покупателя не задана ставка комиссии: " + custObj.GetFullName());

                var coi = new CustomerOrderInfo(custObj);
                var customerOrders = byCustomerOrders.FirstOrDefault(orders => orders.Key == custObj.Id);
                foreach (Order order in customerOrders)
                {
                    if (!IsIncludingEmpty && order.Amount == 0) continue;

                    Product productInfo = null;
                    OnProgressChanged("Получение информации о продукте");

                    string partialIndicator = "";
                    try
                    {
                        productInfo = productRepository.GetById(order.ProductId);
                        var total = orderRepository.GetProductTotalOrderedAmount(productInfo);
                        if (total < productInfo.MinAmount)
                        {
                            if (!IsIncludingPartial)
                            {
                                continue;
                            }
                            partialIndicator = "(!)";
                        }
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        continue;
                    }

                    coi.Items.Add(new CustomerOrderItem
                        {
                            Amount = order.Amount,
                            IsPartial = partialIndicator.Length > 0,
                            Price = productInfo.Price,
                            Title = productInfo.Title,
                            Comment = order.Comment
                        });
                }

                if (coi.GetOrderCleanSum() > 0)
                {
                    bills.Add(coi);
                }
            }

            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object>(o =>
                {
                    var data = (List<CustomerOrderInfo>) o;

                    var borderThick = new Thickness(1);
                    var borderColor = Brushes.LightGray;

                    var table = new Table { CellSpacing = 1, FontSize = 12 };
                    table.Columns.Add(new TableColumn());
                    table.Columns.Add(new TableColumn());
                    table.Columns[0].Width = new GridLength(0.7, GridUnitType.Star);
                    table.Columns[1].Width = new GridLength(0.3, GridUnitType.Star);

                    table.RowGroups.Add(new TableRowGroup());
                    
                    var r = new TableRow { FontWeight = FontWeights.Bold };
                    r.Cells.Add(
                        new TableCell(
                            new Paragraph(new Run(String.Format("Заказы в альбоме '{0}'", WorkingAlbum.GetCleanTitle()).ToUpper())))
                            {
                                TextAlignment = TextAlignment.Left
                            });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(DateTime.Now.ToString())))
                        {
                            TextAlignment = TextAlignment.Right,
                            FontSize = 10
                        });
                    
                    table.RowGroups[0].Rows.Add(r);
                    m_document.Blocks.Add(table);

                    decimal totalSum = 0;
                    decimal totalComission = 0;
                    decimal totalDelivery = 0;

                    const int numberOfColumns = 5;
                    foreach (CustomerOrderInfo orderInfo in data)
                    {
                        totalSum += orderInfo.GetOrderCleanSum();
                        totalComission += orderInfo.GetOrderComission();

                        var paragraph = new Paragraph(
                            new Run(orderInfo.Customer.GetFullName().ToUpper()))
                            {
                                FontSize = 12,
                                FontWeight = FontWeights.Bold
                            };

                        m_document.Blocks.Add(paragraph);

                        table = new Table { CellSpacing = 0, FontSize = 11 };
                        for (int x = 0; x < numberOfColumns; x++)
                        {
                            table.Columns.Add(new TableColumn());
                        }

                        table.Columns[0].Width = new GridLength(30);
                        table.Columns[1].Width = new GridLength(400);
                        table.Columns[2].Width = new GridLength(100);
                        table.Columns[3].Width = new GridLength(60);
                        table.Columns[4].Width = new GridLength(100);

                        var rowGroup = new TableRowGroup();
                        table.RowGroups.Add(rowGroup);

                        var row = new TableRow { FontWeight = FontWeights.Bold, FontSize = 11};
                        rowGroup.Rows.Add(row);

                        #region Table title

                        row.Cells.Add(new TableCell(new Paragraph(new Run("№")))
                        {
                            TextAlignment = TextAlignment.Center,
                            Padding = new Thickness(3),
                            BorderThickness = borderThick,
                            BorderBrush = borderColor
                        });
                        row.Cells.Add(new TableCell(new Paragraph(new Run("Наименование")))
                        {
                            TextAlignment = TextAlignment.Center,
                            Padding = new Thickness(3),
                            BorderThickness = borderThick,
                            BorderBrush = borderColor
                        });
                        row.Cells.Add(new TableCell(new Paragraph(new Run("Цена")))
                        {
                            TextAlignment = TextAlignment.Center,
                            Padding = new Thickness(3),
                            BorderThickness = borderThick,
                            BorderBrush = borderColor
                        });
                        row.Cells.Add(new TableCell(new Paragraph(new Run("Кол-во")))
                        {
                            TextAlignment = TextAlignment.Center,
                            Padding = new Thickness(3),
                            BorderThickness = borderThick,
                            BorderBrush = borderColor
                        });
                        row.Cells.Add(new TableCell(new Paragraph(new Run("Сумма")))
                        {
                            TextAlignment = TextAlignment.Center,
                            Padding = new Thickness(3),
                            BorderThickness = borderThick,
                            BorderBrush = borderColor
                        });

                        #endregion

                        var rowNum = 1;
                        foreach (var prod in orderInfo.Items)
                        {
                            row = new TableRow
                                {
                                     Background = prod.IsPartial ? Brushes.Gainsboro : Brushes.White
                                };
                            rowGroup.Rows.Add(row);

                            row.Cells.Add(new TableCell(new Paragraph(new Run(rowNum.ToString())))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(3),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });

                            var title = !String.IsNullOrEmpty(prod.Comment)
                                            ? String.Format("{0} ({1})", prod.Title, prod.Comment)
                                            : prod.Title;
                            row.Cells.Add(new TableCell(new Paragraph(new Run(title)))
                            {
                                TextAlignment = TextAlignment.Left,
                                Padding = new Thickness(3),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                            row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Price.ToString("C2"))))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(3),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                            row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Amount.ToString())))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(3),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor,
                            });
                            row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Total.ToString("C2"))))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(3),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                            rowNum++;
                        }
                        m_document.Blocks.Add(table);

                        var delivery = orderInfo.Customer.GetDeliveryInfo();
                        var summary = orderInfo.GetOrderCleanSum() + orderInfo.GetOrderComission();

                        var p = new Paragraph(new Run(String.Format("Сумма: {0}", orderInfo.GetOrderCleanSum().ToString("C2"))))
                        {
                            FontSize = 11,
                        };
                        p.Inlines.Add(new LineBreak());
                        p.Inlines.Add(
                            new Run(String.Format("Сбор ({0}): {1}", orderInfo.Customer.GetCommissionInfo().Comment,
                                                  orderInfo.GetOrderComission().ToString("C2"))));
                        p.Inlines.Add(new LineBreak());

                        if (delivery != null && delivery.IsActive)
                        {
                            if ((delivery.IsConditional &&
                                 delivery.MinimumOrderSummaryCondition > summary) ||
                                delivery.IsConditional == false)
                            {
                                summary += delivery.Price;
                                totalDelivery += delivery.Price;
                                p.Inlines.Add(new Run(String.Format("Доставка: {0:C0}", delivery.Price)));
                                p.Inlines.Add(new LineBreak());
                            }
                        }

                        p.Inlines.Add(new Run(String.Format("Итог: {0}", summary.ToString("C0"))));
                        p.Inlines.Add(new LineBreak());
                        m_document.Blocks.Add(p);
                    }
                    
                    var p1 = new Paragraph(new Run(String.Format("Сумма: {0}", totalSum.ToString("C2"))))
                    {
                        FontSize = 11,
                        FontWeight = FontWeights.Bold
                    };
                    p1.Inlines.Add(new LineBreak());
                    p1.Inlines.Add(new Run(String.Format("Комиссия: {0}", totalComission.ToString("C2"))));
                    p1.Inlines.Add(new LineBreak());
                    p1.Inlines.Add(new Run(String.Format("Доставка: {0}", totalDelivery.ToString("C2"))));
                    p1.Inlines.Add(new LineBreak());
                    p1.Inlines.Add(
                        new Run(String.Format("Итог: {0}",
                                              (totalSum + totalComission)
                                                  .ToString("C2"))));
                    p1.Inlines.Add(new LineBreak());
                    m_document.Blocks.Add(p1);

                    m_document.Tag = "Заказы";
                }), bills);

        }

        public override void ExportProductsSummary()
        {
            ClearDocument();
            var productRepository = DbManger.GetInstance().GetProductRepository();
            var orderRepository = DbManger.GetInstance().GetOrderRepository();

            List<Product> products;
            List<Order> orders;
            try
            {
                products = productRepository.GetAllFromAlbum(WorkingAlbum);
                orders = orderRepository.GetOrdersForAlbum(WorkingAlbum);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                throw new BgWorkerException("Ошибка. Не удалось получить информацию по альбому.");
            }

            products.Sort((a, b) => String.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));

            var list = new List<ProductOrderItem>();
            foreach (Product product in products)
            {
                int ordered = orders.Where(order => order.ProductId == product.Id).Sum(order => order.Amount);
                if (ordered < product.MinAmount && !IsIncludingPartial) continue;
                if (ordered == 0 && !IsIncludingEmpty) continue;

                list.Add(new ProductOrderItem()
                    {
                        Title = String.Format("{0} (мин: {1} шт.)",product.Title, product.MinAmount),
                        Price = product.Price,
                        Amount = ordered,
                    });
            }
            
            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object>(o =>
            {
                var borderThick = new Thickness(1);
                var borderColor = Brushes.LightGray;

                var prods = (List<ProductOrderItem>)o;

                m_document.Blocks.Add(
                    new Paragraph(new Run(String.Format("Заказы товаров в альбоме '{0}' от {1}", WorkingAlbum.GetCleanTitle(),
                                      DateTime.Now.ToLongDateString()).ToUpper()))
                    {
                        FontSize = 13,
                        FontWeight = FontWeights.Bold
                    });


                var table = new Table { CellSpacing = 3, FontSize = 11 };

                const int numberOfColumns = 5;
                for (int x = 0; x < numberOfColumns; x++)
                {
                    table.Columns.Add(new TableColumn());
                    table.Columns[x].Background = x % 2 == 0 ? Brushes.White : Brushes.WhiteSmoke;
                }

                table.Columns[0].Width = new GridLength(30);
                table.Columns[1].Width = new GridLength(400);
                table.Columns[2].Width = new GridLength(100);
                table.Columns[3].Width = new GridLength(60);
                table.Columns[4].Width = new GridLength(100);

                var rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                var row = new TableRow { FontWeight = FontWeights.Bold, FontSize = 11};
                rowGroup.Rows.Add(row);

                #region Table title
                row.Cells.Add(new TableCell(new Paragraph(new Run("№")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Наименование")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Цена")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Кол-во")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Сумма")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                #endregion

                var rowNum = 1;
                decimal summary = 0;
                foreach (var prod in prods)
                {
                    summary += prod.Total;

                    row = new TableRow { Background = rowNum % 2 == 0 ? Brushes.WhiteSmoke : Brushes.White };
                    rowGroup.Rows.Add(row);

                    row.Cells.Add(new TableCell(new Paragraph(new Run(rowNum.ToString())))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Title)))
                    {
                        TextAlignment = TextAlignment.Left,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Price.ToString("C2"))))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Amount.ToString())))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(prod.Total.ToString("C2"))))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    rowNum++;
                }

                var p = new Paragraph(new Run(String.Format("Сумма: {0}", summary.ToString("C2"))))
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };
                p.Inlines.Add(new LineBreak());

                m_document.Blocks.Add(table);
                m_document.Blocks.Add(p);


                m_document.Tag = "Заказы товаров";
            }), list); 
        }

        public override void ExportCustomerOrders(Customer customer)
        {
            ClearDocument();

            var orderRepository = DbManger.GetInstance().GetOrderRepository();
            var productRepository = DbManger.GetInstance().GetProductRepository();
//            var ratesRepository = DbManger.GetInstance().GetRatesRepository();

            if (customer.GetCommissionInfo() == null)
                throw new ApplicationException("Для покупателя неверно установлена ставка комиссии!");

            // информация о комиссии пользователя
//            ManagedRate comission;
            List<Order> orders;
            try
            {
//                comission = ratesRepository.GetById(customer.AccountTypeId);
                orders = orderRepository.GetOrdersForCustomerFromAlbum(customer, WorkingAlbum);
            }
            catch (Exception exception)
            {
                m_logger.ErrorException(exception);
                throw new ApplicationException("Ошибка: Не удалось выполнить чтение из БД");
            }

            // прелоад информации о способе доставки
            customer.GetDeliveryInfo();

            var orderInfo = new CustomerOrderInfo(customer);
            foreach (Order order in orders)
            {
                if (!IsIncludingEmpty && order.Amount == 0) continue;

                Product productInfo;
                OnProgressChanged("Получение информации о продукте");

                string partialIndicator = "";
                try
                {
                    productInfo = productRepository.GetById(order.ProductId);
                    var total = orderRepository.GetProductTotalOrderedAmount(productInfo);
                    if (total < productInfo.MinAmount)
                    {
                        if (!IsIncludingPartial)
                        {
                            continue;
                        }
                        
                        partialIndicator = "(!)";
                    }
                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    continue;
                }

                orderInfo.Items.Add(new CustomerOrderItem
                    {
                        Amount = order.Amount,
                        Price = productInfo.Price,
                        IsPartial = partialIndicator.Length > 0,
                        Title = productInfo.Title
                    });
            }

            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object>(o =>
            {
                var borderThick = new Thickness(1);
                var borderColor = Brushes.LightGray;

                var corders = (CustomerOrderInfo) o;

                m_document.Blocks.Add(
                    new Paragraph(new Run(corders.Customer.GetFullName().ToUpper()))
                        {
                            FontSize = 16,
                            FontWeight = FontWeights.Bold
                        });


                var table = new Table { CellSpacing = 3, FontSize = 12 };

                const int numberOfColumns = 5;
                for (int x = 0; x < numberOfColumns; x++)
                {
                    table.Columns.Add(new TableColumn());
                    table.Columns[x].Background = x % 2 == 0 ? Brushes.White : Brushes.WhiteSmoke;
                }

                table.Columns[0].Width = new GridLength(30);
                table.Columns[1].Width = new GridLength(400);
                table.Columns[2].Width = new GridLength(100);
                table.Columns[3].Width = new GridLength(60);
                table.Columns[4].Width = new GridLength(100);

                var rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                var row = new TableRow { FontWeight = FontWeights.Bold };
                rowGroup.Rows.Add(row);

                #region Table title
                row.Cells.Add(new TableCell(new Paragraph(new Run("№")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Наименование")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Цена")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Кол-во")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Сумма")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                #endregion

                var rowNum = 1;
                decimal summary = 0;
                foreach (var order in corders.Items)
                {
                    summary += order.Total;

                    row = new TableRow { Background = rowNum % 2 == 0 ? Brushes.WhiteSmoke : Brushes.White };
                    rowGroup.Rows.Add(row);

                    row.Cells.Add(new TableCell(new Paragraph(new Run(rowNum.ToString())))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Title)))
                    {
                        TextAlignment = TextAlignment.Left,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Price.ToString("C2"))))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Amount.ToString())))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(order.Total.ToString("C2"))))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    rowNum++;
                }

                var commission = corders.Customer.GetCommissionInfo();
                var comissionValue = summary * (commission.Rate / 100);
                var total = summary * (1 + (commission.Rate / 100));
                var delivery = corders.Customer.GetDeliveryInfo();

                var p = new Paragraph(new Run(String.Format("Сумма: {0}", summary.ToString("C2"))))
                    {
                        FontSize = 13,
                        FontWeight = FontWeights.Bold
                    };
                p.Inlines.Add(new LineBreak());
                p.Inlines.Add(new Run(String.Format("Сбор ({0}): {1:C2}", commission.Comment, comissionValue))
                    {
                        FontSize = 13,
                        FontWeight = FontWeights.Bold
                    });
                p.Inlines.Add(new LineBreak());

                if (delivery != null && delivery.IsActive)
                {
                    if ((delivery.IsConditional &&
                         delivery.MinimumOrderSummaryCondition > summary) ||
                        delivery.IsConditional == false)
                    {
                        total += delivery.Price;
                        p.Inlines.Add(new Run(String.Format("Доставка: {0:C2}", delivery.Price))
                        {
                            FontSize = 13,
                            FontWeight = FontWeights.Bold
                        });
                        p.Inlines.Add(new LineBreak());
                    }
                }

                p.Inlines.Add(new Run(String.Format("Итог: {0:C0}", total))
                {
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                });
                p.Inlines.Add(new LineBreak());

                m_document.Blocks.Add(table);
                m_document.Blocks.Add(p);


                m_document.Tag = corders.Customer.GetFullName();
            }), orderInfo); 
        }

        public override void ExportDeliveryList()
        {
            ClearDocument();
            var orderRepository = DbManger.GetInstance().GetOrderRepository();
            var customersRepo = DbManger.GetInstance().GetCustomersRepository();

            var albumOrders = orderRepository.GetOrdersForAlbum(WorkingAlbum);
            var groupedAlbumOrders = albumOrders.GroupBy(order => order.CustomerId);

            var albumCustomers = new List<Customer>();
            foreach (IGrouping<int, Order> group in groupedAlbumOrders)
            {
                var customer = customersRepo.GetById(group.Key);
                if (customer == null) continue;

                albumCustomers.Add(customer);
            }
            albumCustomers.Sort((c1, c2) => String.Compare(c1.LastName, c2.LastName, StringComparison.CurrentCultureIgnoreCase));

            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object>(o =>
            {
                var borderThick = new Thickness(1);
                var borderColor = Brushes.LightGray;

                var customers = (List<Customer>)o;

                m_document.Blocks.Add(
                    new Paragraph(
                        new Run(
                            String.Format("Лист доставки от {0}", DateTime.Now.ToLongDateString()).ToUpper()))
                        {
                            FontSize = 16,
                            FontWeight = FontWeights.Bold
                        });


                var table = new Table { CellSpacing = 3, FontSize = 12 };

                const int numberOfColumns = 4;
                for (int x = 0; x < numberOfColumns; x++)
                {
                    table.Columns.Add(new TableColumn());
                }

                table.Columns[0].Width = new GridLength(30);
                table.Columns[1].Width = new GridLength(200);
                table.Columns[2].Width = new GridLength(350);
                table.Columns[3].Width = new GridLength(100);

                var rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                var row = new TableRow { FontWeight = FontWeights.Bold };
                rowGroup.Rows.Add(row);

                row.Cells.Add(new TableCell(new Paragraph(new Run("№")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Покупатель")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Адрес")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });
                row.Cells.Add(new TableCell(new Paragraph(new Run("Отметка")))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(5),
                    BorderThickness = borderThick,
                    BorderBrush = borderColor
                });

                var counter = 1;
                foreach (Customer customer in customers)
                {
                    IGrouping<int, Order> orders = null;

                    var totalItems = 0;
//                    var totalPosition = 0;

                    foreach (IGrouping<int, Order> group in groupedAlbumOrders)
                    {
                        if (group.Key != customer.Id) continue;
                        orders = group;
                    }
                    if (orders != null)
                    {
                        totalItems += orders.Sum(order => order.Amount);
                    }
                    if (totalItems == 0)
                    {
                        // у покупателя в заказе одни нули - пропускаем
                        continue;
                    }

                    row = new TableRow { Background = counter % 2 == 0 ? Brushes.WhiteSmoke : Brushes.White };
                    rowGroup.Rows.Add(row);

                    row.Cells.Add(new TableCell(new Paragraph(new Run(counter.ToString())))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run(customer.GetFullName())))
                    {
                        TextAlignment = TextAlignment.Left,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });

                    string adr;
                    if (!String.IsNullOrEmpty(customer.Address))
                    {
                        adr = customer.Address.ToString();
                        if (!String.IsNullOrEmpty(customer.Phone))
                        {
                            adr += String.Format(" ({0})", customer.Phone);
                        }
                    }
                    else adr = "";

                    row.Cells.Add(new TableCell(new Paragraph(new Run(adr)))
                    {
                        TextAlignment = TextAlignment.Left,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run("")))
                    {
                        TextAlignment = TextAlignment.Left,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    
                    counter++;
                }

                m_document.Blocks.Add(table);
                m_document.Tag = @"Лист доставки";
            }), albumCustomers);
        }

        public override void ExportPaymentsSummary()
        {
            ClearDocument();
            
            var payRepo = DbManger.GetInstance().GetPaymentsRepository();
            var custRepo = DbManger.GetInstance().GetCustomersRepository();

            var albumPayments = payRepo.AllForAlbum(WorkingAlbum);

            var result = (from albumPayment in albumPayments
                          let customer = custRepo.GetById(albumPayment.PayerId)
                          select new PaymentsListViewItem(albumPayment, customer)).ToList();
            result.Sort((i1, i2) => String.Compare(i1.Title, i2.Title, StringComparison.CurrentCultureIgnoreCase));

            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<object>(o =>
                {
                    var borderThick = new Thickness(1);
                    var borderColor = Brushes.LightGray;

                    var payments = (List<PaymentsListViewItem>) o;

                    m_document.Blocks.Add(
                        new Paragraph(
                            new Run(
                                String.Format("Платежи для альбома '{0}' на {1}",
                                              WorkingAlbum.GetCleanTitle(),
                                              DateTime.Now.ToLongDateString())))
                            {
                                FontSize = 16,
                                FontWeight = FontWeights.Bold
                            });


                    var table = new Table { CellSpacing = 3, FontSize = 12 };

                    const int numberOfColumns = 5;
                    for (int x = 0; x < numberOfColumns; x++)
                    {
                        table.Columns.Add(new TableColumn());
                    }

                    table.Columns[0].Width = new GridLength(30);
                    table.Columns[1].Width = new GridLength(200);
                    table.Columns[2].Width = new GridLength(80);
                    table.Columns[3].Width = new GridLength(150);
                    table.Columns[4].Width = new GridLength(250);

                    var rowGroup = new TableRowGroup();
                    table.RowGroups.Add(rowGroup);

                    var row = new TableRow {FontWeight = FontWeights.Bold };
                    rowGroup.Rows.Add(row);

                    row.Cells.Add(new TableCell(new Paragraph(new Run("№")))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Покупатель")))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Сумма")))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Дата")))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Комментарий")))
                    {
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(5),
                        BorderThickness = borderThick,
                        BorderBrush = borderColor
                    });

                    var rowNum = 1;
                    decimal total = 0;
                    foreach (PaymentsListViewItem payment in payments)
                    {
                        total += payment.SourcePayment.Amount;

                        row = new TableRow {Background = rowNum%2 == 0 ? Brushes.WhiteSmoke : Brushes.White};
                        rowGroup.Rows.Add(row);
                        
                        row.Cells.Add(new TableCell(new Paragraph(new Run(rowNum.ToString())))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(5),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(payment.Title)))
                            {
                                TextAlignment = TextAlignment.Left,
                                Padding = new Thickness(5),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(payment.Amount)))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(5),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(payment.Date)))
                            {
                                TextAlignment = TextAlignment.Center,
                                Padding = new Thickness(5),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                        row.Cells.Add(new TableCell(new Paragraph(new Run(payment.Comment)))
                            {
                                TextAlignment = TextAlignment.Left,
                                Padding = new Thickness(5),
                                BorderThickness = borderThick,
                                BorderBrush = borderColor
                            });
                        rowNum++;
                    }

                    m_document.Blocks.Add(table);
                    m_document.Blocks.Add(new Paragraph(new Run(String.Format("Всего: {0}", total.ToString("C2"))))
                        {
                            FontSize = 13,
                            FontWeight = FontWeights.Bold
                        });
                    m_document.Tag = @"Платежи альбома";
                }), result);
        }

        public FlowDocument GetDocument()
        {
            return m_document;
        }

        private void ClearDocument()
        {
            m_document.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => m_document.Blocks.Clear()));
        }

        public override string ToString()
        {
            return "Внутренний отчет";
        }
    }
}