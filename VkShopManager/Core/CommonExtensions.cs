using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace VkShopManager.Core
{
    static class CommonExtensions
    {
        public static void ErrorException(this log4net.ILog log, Exception e)
        {
            log.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
        }
        public static void ShowError(this Window wnd, string msg, string title)
        {
            if (!wnd.Dispatcher.CheckAccess())
            {
                wnd.Dispatcher.BeginInvoke(new Action<Window, string, string>(ShowError), DispatcherPriority.Normal, wnd, msg, title);
                return;
            }

            MessageBox.Show(wnd, msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public static void ShowError(this Window wnd, string msg)
        {
            if (!wnd.Dispatcher.CheckAccess())
            {
                wnd.Dispatcher.BeginInvoke(new Action<Window, string>(ShowError), DispatcherPriority.Normal, wnd, msg);
                return;
            }

            MessageBox.Show(wnd, msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public static void ShowMessage(this Window wnd, string msg)
        {
            if (!wnd.Dispatcher.CheckAccess())
            {
                wnd.Dispatcher.BeginInvoke(new Action<Window, string>(ShowError), DispatcherPriority.Normal, wnd, msg);
                return;
            }

            MessageBox.Show(wnd, msg, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public static bool ShowQuestion(this Window wnd, string msg)
        {
            if (!wnd.Dispatcher.CheckAccess())
            {
                var h = wnd.Dispatcher.BeginInvoke(new Action<Window, string>(ShowError), DispatcherPriority.Normal, wnd, msg);
                h.Wait();
                return h.Result != null && (bool) h.Result;
            }

            return MessageBox.Show(wnd, msg, "Вопрос", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
