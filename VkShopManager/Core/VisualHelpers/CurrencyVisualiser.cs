using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace VkShopManager.Core.VisualHelpers
{
    class CurrencyVisualiser : IValueConverter
    {
        private readonly string m_format;

        public CurrencyVisualiser()
        {
            m_format = "{0:C2}";
        }
        public CurrencyVisualiser(int precision)
        {
            m_format = "{0:C" + precision + "}";
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal)
            {
                return String.Format(m_format, value);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
