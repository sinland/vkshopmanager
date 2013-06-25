using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Domain
{
    public class ParsedComment
    {
        /// <summary>
        /// Идентификатор в БД
        /// </summary>
        public virtual int Id { get; set; }
        /// <summary>
        /// Идентификатор начального объекта вконтакте (может повторяться для разных товаров)
        /// </summary>
        public virtual Int64 VkId { get; set; }
        /// <summary>
        /// Идентификатор товара из БД
        /// </summary>
        public virtual int ProductId { get; set; }
        /// <summary>
        /// Дата парсинга комментария
        /// </summary>
        public virtual DateTime ParsingDate { get; set; }
        /// <summary>
        /// Текст комментария
        /// </summary>
        public virtual string Message { get; set; }
        /// <summary>
        /// Имя отправителя
        /// </summary>
        public virtual string SenderName { get; set; }
        /// <summary>
        /// Дата постинга комментария (по данным вконтакте)
        /// </summary>
        public virtual string PostingDate { get; set; }
        

        public ParsedComment()
        {

        }

        public Product GetCommentedProduct()
        {
            var prods = Core.Repositories.DbManger.GetInstance().GetProductRepository();
            try
            {
                var prod = prods.GetById(this.ProductId);
                return prod;
            }
            catch (Exception)
            {
                
                
            }
            return null;
        }

    }
}
