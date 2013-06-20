using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using VkShopManager.Core.Repositories;
using VkShopManager.Core.VisualHelpers;
using VkShopManager.Domain;
using Word = Microsoft.Office.Interop.Word;

namespace VkShopManager.Core
{
    class WordExportFormatter : ExportFormatterBase, IFileExporter
    {
        public static string CustomerOrderTemplate;
        public static string DeliveryListTemplate;

        private readonly log4net.ILog m_log;
        private string m_filename;

        public WordExportFormatter(Album album) : base(album)
        {
            m_log = log4net.LogManager.GetLogger("WordExportFormatter");
        }
        public override string ToString()
        {
            return "Экспорт в MS Word 2007";
        }

        public override void ExportCustomersSummary()
        {
            throw new NotImplementedException();
        }

        public override void ExportProductsSummary()
        {
            throw new NotImplementedException();
        }

        public override void ExportCustomerOrders(Customer customer)
        {
            if (!System.IO.File.Exists(CustomerOrderTemplate))
            {
                throw new ApplicationException("Файл шаблона для данного документа не найден!");
            }

            const string itemsTableBookmark = @"customer_orders_table";
            const string customerNameBookmark = @"customer_name";
            const string dateBookmark = @"current_date";
            const string clearSumBookmark = @"clear_sum";
            const string comissionSumBookmark = @"comission";
            const string totalSumBookmark = @"total";
            const string addressBookmark = @"customer_address";
            const string deliveryBookmark = @"delivery_value";

            var orderRepo = DbManger.GetInstance().GetOrderRepository();
            var prodRepo = DbManger.GetInstance().GetProductRepository();
//            var ratesRepo = DbManger.GetInstance().GetRatesRepository();
            

//            ManagedRate comission;
//            try
//            {
//                comission = ratesRepo.GetById(customer.AccountTypeId);
//            }
//            catch (Exception e)
//            {
//                m_log.ErrorException(e);
//                throw new ApplicationException("Ошибка. Не удалось получить ставку покупателя.");
//            }
            if(customer.GetCommissionInfo() == null) 
                throw new ApplicationException("Ошибка. Не удалось получить ставку покупателя.");

            List<Order> orders;
            try
            {
                orders = orderRepo.GetOrdersForCustomerFromAlbum(customer, WorkingAlbum);
            }
            catch (Exception e)
            {
                m_log.ErrorException(e);
                throw new ApplicationException("Не удалось получить список заказов для покупателя.");
            }

            Word.Application word;
            try
            {
                word = new Word.Application();
            }
            catch (Exception e)
            {
                m_log.ErrorException(e);
                throw new ApplicationException("Не удалось запустить MS Word. Пожалуйста удостоверьтесь, что установлена версия MS MSWord не ниже 2007.");
            }

            // word.Visible = true;
            var doc = word.Documents.Open(CustomerOrderTemplate);
            if (!doc.Bookmarks.Exists(itemsTableBookmark))
            {
                m_log.ErrorFormat("Content bookmark '{0}' not found in template!", itemsTableBookmark);
                doc.Close();
                word.Quit();
                throw new ApplicationException(String.Format("Шаблон '{0}' некорректен!", CustomerOrderTemplate));
            }

            var range = doc.Bookmarks[itemsTableBookmark].Range;
            if (range.Tables.Count == 0)
            {
                m_log.ErrorFormat("Content bookmark '{0}' is not containing table to fill!", itemsTableBookmark);
                doc.Close();
                word.Quit();
                throw new ApplicationException(String.Format("Шаблон '{0}' некорректен!", CustomerOrderTemplate));
            }

            var table = range.Tables[1];
            int row = 2;
            decimal clearSum = 0;
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];

                if (order.Amount == 0 && !IsIncludingEmpty) continue;

                //string partialIndicator = "";
                Product p;
                try
                {
                    p = prodRepo.GetById(order.ProductId);
                    if (p == null)
                    {
                        continue;
                    }
                    var total = orderRepo.GetProductTotalOrderedAmount(p);
                    if (total < p.MinAmount)
                    {
                        if (!IsIncludingPartial)
                        {
                            continue;
                        }
                       // else partialIndicator = "(!)";
                    }
                }
                catch (Exception e)
                {
                    m_log.ErrorException(e);
                    throw new ApplicationException("Ошибка БД.");
                }

                var title = p.GenericUrl.Length > 0 ? String.Format("{0} ({1})", p.Title, p.GenericUrl) : p.Title;

                table.Rows.Add();
                table.Cell(row, 1).Range.Text = (row - 1).ToString();
                table.Cell(row, 2).Range.Text = title;
                table.Cell(row, 3).Range.Text = p.Price.ToString("C2");
                table.Cell(row, 4).Range.Text = order.Amount.ToString();
                table.Cell(row, 5).Range.Text = (p.Price*order.Amount).ToString("C2");

                clearSum += (p.Price*order.Amount);
                row++;
            }

            try
            {
                table.Rows[row].Delete();
            }
            catch (Exception)
            {
                
            }

            var comission = customer.GetCommissionInfo();
            var comissionValue = (clearSum*(comission.Rate/100));
            var summary = clearSum*(1 + comission.Rate/100);
            decimal deliveryPrice = 0;

            var delivery = customer.GetDeliveryInfo();
            if (delivery != null && delivery.IsActive)
            {
                if ((delivery.IsConditional &&
                         delivery.MinimumOrderSummaryCondition > summary) ||
                        delivery.IsConditional == false)
                {
                    summary += delivery.Price;
                    deliveryPrice = delivery.Price;
                }
            }

            if (doc.Bookmarks.Exists(customerNameBookmark))
            {
                doc.Bookmarks[customerNameBookmark].Range.Text = customer.GetFullName();
            }
            if (doc.Bookmarks.Exists(dateBookmark))
            {
                doc.Bookmarks[dateBookmark].Range.Text = DateTime.Now.ToLongDateString();
            }
            if (doc.Bookmarks.Exists(clearSumBookmark))
            {
                doc.Bookmarks[clearSumBookmark].Range.Text = String.Format("Сумма: {0:C2}", clearSum);
            }
            if (doc.Bookmarks.Exists(comissionSumBookmark))
            {
                doc.Bookmarks[comissionSumBookmark].Range.Text = String.Format("{0}: {1:C2}", comission.Comment, comissionValue);
            }
            if (doc.Bookmarks.Exists(deliveryBookmark))
            {
                doc.Bookmarks[deliveryBookmark].Range.Text = String.Format("Доставка: {0:C0}", deliveryPrice);
            }
            if (doc.Bookmarks.Exists(totalSumBookmark))
            {
                doc.Bookmarks[totalSumBookmark].Range.Text = String.Format("Итого: {0:C0}", summary);
            }
            if (doc.Bookmarks.Exists(addressBookmark))
            {
                doc.Bookmarks[addressBookmark].Range.Text = customer.Address;
            }

            doc.SaveAs(m_filename);
            doc.Close();
            word.Quit();
        }

        public override void ExportDeliveryList()
        {
            // date
            // data_table
            if (!System.IO.File.Exists(DeliveryListTemplate))
            {
                throw new ApplicationException("Файл шаблона для данного документа не найден!");
            }
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

            Word.Application word;
            try
            {
                word = new Word.Application();
            }
            catch (Exception e)
            {
                m_log.ErrorException(e);
                throw new ApplicationException("Не удалось запустить MS Word. Пожалуйста удостоверьтесь, что установлена версия MS MSWord не ниже 2007.");
            }

            var doc = word.Documents.Open(DeliveryListTemplate);
            if (doc == null) return;

            if (!doc.Bookmarks.Exists("data_table"))
            {
                m_log.ErrorFormat("Content bookmark 'data_table' not found in template!");
                doc.Close();
                word.Quit();
                throw new ApplicationException(String.Format("Шаблон '{0}' некорректен!", CustomerOrderTemplate));
            }
            var range = doc.Bookmarks["data_table"].Range;
            if (range.Tables.Count == 0)
            {
                m_log.ErrorFormat("Content bookmark 'data_table' is not containing table to fill!");
                doc.Close();
                word.Quit();
                throw new ApplicationException(String.Format("Шаблон '{0}' некорректен!", CustomerOrderTemplate));
            }
            var table = range.Tables[1];

            int counter = 2;
            foreach (Customer customer in albumCustomers)
            {
                IGrouping<int, Order> orders = null;

                var totalItems = 0;
//                var totalPosition = 0;

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

                table.Rows.Add();
                table.Cell(counter, 1).Range.Text = (counter - 1).ToString();
                table.Cell(counter, 2).Range.Text = customer.GetFullName();
                table.Cell(counter, 4).Range.Text = totalItems.ToString();
                table.Cell(counter, 5).Range.Text = string.Format("{0} ({1})", customer.Address, customer.Phone);
                counter++;
            }
            try
            {
                table.Rows[counter].Delete();
            }
            catch (Exception)
            {

            }
            if (doc.Bookmarks.Exists("date"))
            {
                doc.Bookmarks["date"].Range.Text = DateTime.Now.ToLongDateString();
            }

            doc.SaveAs(m_filename);
            doc.Close();
            word.Quit();
        }

        public override void ExportPaymentsSummary()
        {
            throw new NotImplementedException();
        }

        public string Filename { get { return m_filename; } set { m_filename = value; } }
    }
}
