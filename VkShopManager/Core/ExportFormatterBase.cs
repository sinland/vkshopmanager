using System;
using System.Collections.Generic;
using System.Windows.Documents;
using VkShopManager.Domain;

namespace VkShopManager.Core
{
    public abstract class ExportFormatterBase
    {
        protected Album WorkingAlbum;

        protected ExportFormatterBase(Album album)
        {
            WorkingAlbum = album;
        }

        public abstract void ExportCustomersSummary();
        public abstract void ExportProductsSummary();
        public abstract void ExportCustomerOrders(Customer customer);
        public abstract void ExportDeliveryList();
        public abstract void ExportPaymentsSummary();

        public event EventHandler<ExportProgressReportArgs> ProgressChanged;
        protected void OnProgressChanged(string stateMessage)
        {
            if(ProgressChanged != null) ProgressChanged(this, new ExportProgressReportArgs(stateMessage));
        }

        public bool IsIncludingEmpty { get; set; }
        public bool IsIncludingPartial { get; set; }
    }

    
    public class ExportProgressReportArgs : EventArgs
    {
        private readonly string m_message;

        public ExportProgressReportArgs(string message)
        {
            m_message = message;
        }

        public string StateMessage { get { return m_message; } }
    }

    public interface IFileExporter
    {
        string Filename { get; set; }
    }

    public interface IReportExporter
    {
        FlowDocument GetDocument();
    }
}