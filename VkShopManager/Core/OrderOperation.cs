using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Core
{
    public enum OrderOperation
    {
        /// <summary>
        /// Добавить позицию к заказу
        /// </summary>
        Add,
        /// <summary>
        /// Удлалить позицию из заказа
        /// </summary>
        Remove,
        /// <summary>
        /// Уменьшить сисмло товара по позиции
        /// </summary>
        Decrease,
        /// <summary>
        /// Забыть про этот комментацрий
        /// </summary>
        Forget
    }
}
