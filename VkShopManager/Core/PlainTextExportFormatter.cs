using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;

namespace VkShopManager.Core
{
    public class PlainTextExportFormatter : ExportFormatterBase, IFileExporter
    {
        private readonly log4net.ILog m_logger;
        private string m_filename;

        public PlainTextExportFormatter(Album album) : base(album)
        {
            m_logger = log4net.LogManager.GetLogger("PlainTextExportFormatter");
        }

        public override void ExportCustomersSummary()
        {
            var sb = new StringBuilder();
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

            decimal totalClean = 0;
            decimal totalComission = 0;
            decimal totalSum = 0;
            decimal totalDelivery = 0;

            foreach (var custObj in customers)
            {
                var customerExport = new StringBuilder();

                customerExport.AppendLine(String.Format("{0}", custObj.GetFullName().ToUpper()));
                customerExport.AppendLine();

                var comission = custObj.GetCommissionInfo();
                if (comission == null)
                    throw new ApplicationException("Для покупателя не задана ставка комиссии: " + custObj.GetFullName());
                
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

                int positionNum = 1;
                decimal cleanSum = 0;

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
                            else partialIndicator = "(!)";
                        }
                        
                    }
                    catch (Exception exception)
                    {
                        m_logger.ErrorException(exception);
                        continue;
                    }

                    decimal sum = order.Amount * productInfo.Price;
                    cleanSum += sum;

                    var title = !String.IsNullOrEmpty(order.Comment)
                                    ? String.Format("{0} ({1})", productInfo.Title, order.Comment)
                                    : productInfo.Title;
                    customerExport.AppendLine(String.Format("{0}{1}. {2} | {3}шт. | {4:C}", positionNum, partialIndicator, title, order.Amount, sum));
                    positionNum++;
                }

                var comissionValue = cleanSum*(comission.Rate/100);
                decimal deliveryValue = 0;
                var summary = cleanSum*(1 + (comission.Rate/100));

                customerExport.AppendLine(String.Format("Сумма: {0:C2}", cleanSum));
                customerExport.AppendLine(String.Format("Сбор ({0}): {1:C2}", comission.Comment, comissionValue));
                
                var deliveryInfo = custObj.GetDeliveryInfo();
                if (deliveryInfo != null && deliveryInfo.IsActive)
                {
                    if ((deliveryInfo.IsConditional &&
                         deliveryInfo.MinimumOrderSummaryCondition > summary) ||
                        deliveryInfo.IsConditional == false)
                    {
                        deliveryValue = deliveryInfo.Price;
                        summary += deliveryInfo.Price;
                        customerExport.AppendLine(String.Format("Доставка: {0:C0}", deliveryInfo.Price));
                    }
                }

                customerExport.AppendLine(String.Format("Итог: {0:C0}", summary));
                customerExport.AppendLine();
                customerExport.AppendLine("".PadRight(80, '-'));

                if (cleanSum > 0)
                {
                    totalClean += cleanSum;
                    totalComission += comissionValue;
                    totalSum += summary;
                    totalDelivery += deliveryValue;

                    sb.Append(customerExport);
                    sb.AppendLine();
                }
            }

            sb.AppendLine(String.Format("Сумма: {0:C2}", totalClean));
            sb.AppendLine(String.Format("Комиссия: {0:C2}", totalComission));
            sb.AppendLine(String.Format("Доставка: {0:C2}", totalDelivery));
            sb.AppendLine(String.Format("Итог: {0:C2}", totalSum));

            System.IO.File.WriteAllText(m_filename, sb.ToString(), Encoding.UTF8);
        }

        public override void ExportProductsSummary()
        {
            var productRepository = DbManger.GetInstance().GetProductRepository();
            var orderRepository = DbManger.GetInstance().GetOrderRepository();
            var sb =
                new StringBuilder(String.Format("Заказы товаров в альбоме '{0}' от {1}", WorkingAlbum.GetCleanTitle(),
                                                DateTime.Now.ToLongDateString()).ToUpper());
            sb.AppendLine();
            sb.AppendLine();

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

            int position = 1;
            decimal totalSum = 0;
            foreach (Product product in products)
            {
                int ordered = orders.Where(order => order.ProductId == product.Id).Sum(order => order.Amount);
                if (ordered < product.MinAmount && !IsIncludingPartial) continue;
                if (ordered == 0 && !IsIncludingEmpty) continue;
                

                sb.AppendLine(String.Format("{0}. {1} (цена: {2:C2}, мин: {3}шт.) | {4}шт | {5:C2}", position, product.Title,
                                            product.Price, product.MinAmount, ordered, ordered*product.Price));
                position++;
                totalSum += ordered*product.Price;
            }
            sb.AppendLine();
            sb.AppendLine(String.Format("Итог: {0:C2}", totalSum));

            System.IO.File.WriteAllText(m_filename, sb.ToString(), Encoding.UTF8);
        }

        public override void ExportCustomerOrders(Customer customer)
        {
            var orderRepository = DbManger.GetInstance().GetOrderRepository();
            var productRepository = DbManger.GetInstance().GetProductRepository();
//            var ratesRepository = DbManger.GetInstance().GetRatesRepository();

            var customerExport = new StringBuilder();
            customerExport.AppendLine(String.Format("{0}", customer.GetFullName().ToUpper()));
            customerExport.AppendLine();

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

            int positionNum = 1;
            decimal cleanSum = 0;

            foreach (Order order in orders)
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
                        else partialIndicator = "(!)";
                    }

                }
                catch (Exception exception)
                {
                    m_logger.ErrorException(exception);
                    continue;
                }

                decimal sum = order.Amount * productInfo.Price;
                cleanSum += sum;

                customerExport.AppendLine(String.Format("{0} {1}. {2} {3}: {4:C}", positionNum, partialIndicator, productInfo.Title, order.Amount, sum));
                positionNum++;
            }

            var comission = customer.GetCommissionInfo();
            var comissionValue = cleanSum*(comission.Rate/100);
            var summary = cleanSum*(1 + (comission.Rate/100));

            customerExport.AppendLine(String.Format("Сумма: {0:C2}", cleanSum));
            customerExport.AppendLine(String.Format("{0}: {1:C2}", comission.Comment, comissionValue));

            var delivery = customer.GetDeliveryInfo();
            if (delivery != null && delivery.IsActive)
            {
                if ((delivery.IsConditional &&
                         delivery.MinimumOrderSummaryCondition > summary) ||
                        delivery.IsConditional == false)
                {
                    summary += delivery.Price;
                    customerExport.AppendLine(String.Format("Доставка: {0:C0}", delivery.Price));
                }
            }
        
            customerExport.AppendLine(String.Format("Итог: {0:C0}", summary));
            System.IO.File.WriteAllText(m_filename, customerExport.ToString(), Encoding.UTF8);
        }

        public override void ExportDeliveryList()
        {
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

            var report = new StringBuilder();
            report.AppendLine(String.Format("Лист доставки от {0}", DateTime.Now.ToLongDateString()).ToUpper());
            report.AppendLine();

            const int numWidth = 3;
            const int nameWidth = 30;
            const int adressWidth = 43;

            int counter = 1;
            var cp = Encoding.GetEncoding(866);

            // line length = 80 symbols
            report
                .AppendLine(
                    cp.GetString(new byte[] {0xDA}) +
                    "".PadRight(numWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                    cp.GetChars(new byte[] { 0xC2 })[0] +
                    "".PadRight(nameWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                    cp.GetChars(new byte[] { 0xC2 })[0] +
                    "".PadRight(adressWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                    cp.GetChars(new byte[] { 0xBF })[0]);
            
            foreach (Customer customer in albumCustomers)
            {
                IGrouping<int, Order> orders = null;

                var totalItems = 0;
                var totalPosition = 0;

                foreach (IGrouping<int, Order> group in groupedAlbumOrders)
                {
                    if (group.Key != customer.Id) continue;
                    orders = group;
                }
                if (orders != null)
                {
                    foreach (Order order in orders)
                    {
                        totalPosition += 1;
                        totalItems += order.Amount;
                    }
                }
                if (totalItems == 0)
                {
                    // у покупателя в заказе одни нули - пропускаем
                    continue;
                }

                report
                    .AppendLine(
                        cp.GetChars(new byte[] {0xB3})[0] +
                        counter.ToString().PadLeft(1, ' ').PadRight(numWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0] +
                        customer.LastName.PadRight(nameWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0] +
                        "".PadRight(adressWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0])
                    .AppendLine(
                        cp.GetChars(new byte[] {0xB3})[0] +
                        "".PadRight(numWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0] +
                        customer.FirstName.PadRight(nameWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0] +
                        "".PadRight(adressWidth, ' ') +
                        cp.GetChars(new byte[] {0xB3})[0])
                    .AppendLine(
                        cp.GetChars(new byte[] { 0xC3 })[0] +
                        "".PadRight(numWidth, cp.GetChars(new byte[] {0xC4})[0]) +
                        cp.GetChars(new byte[] {0xC5})[0] +
                        "".PadRight(nameWidth, cp.GetChars(new byte[] {0xC4})[0]) +
                        cp.GetChars(new byte[] {0xC5})[0] +
                        "".PadRight(adressWidth, cp.GetChars(new byte[] {0xC4})[0]) +
                        cp.GetChars(new byte[] {0xB4})[0]);
                counter++;
            }

            report.AppendLine(
                        cp.GetChars(new byte[] { 0xC0 })[0] +
                        "".PadRight(numWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                        cp.GetChars(new byte[] { 0xC1 })[0] +
                        "".PadRight(nameWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                        cp.GetChars(new byte[] { 0xC1 })[0] +
                        "".PadRight(adressWidth, cp.GetChars(new byte[] { 0xC4 })[0]) +
                        cp.GetChars(new byte[] { 0xD9 })[0]);

            System.IO.File.WriteAllText(m_filename, report.ToString(), cp);
        }

        public override void ExportPaymentsSummary()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Текстовый файл (*.txt)";
        }

        public string Filename { get { return m_filename; } set { m_filename = value; } }
    }
}