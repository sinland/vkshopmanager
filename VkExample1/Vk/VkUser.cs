using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkApiNet.Vk
{
    public class VkUser : VkEntity
    {
        public VkUser(Int64 uid)
        {
            Id = uid;
            FirstName = String.Empty;
            LastName = String.Empty;
            BirthYear = DateTime.Now.Year;
        }
        /// <summary>
        /// Имя
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Половая принадлежность (0 - не указано, 1 - женщина, 2 - мужчина)
        /// </summary>
        public int Sex { get; set; }
        /// <summary>
        /// Мобильный телефон
        /// </summary>
        public string MobilePhone { get; set; }
        /// <summary>
        /// Домашний телефон
        /// </summary>
        public string HomePhone { get; set; }

        public int BirthYear { get; set; }

        /// <summary>
        /// Полное имя пользователя
        /// </summary>
        /// <returns></returns>
        public string GetFullName()
        {
            return String.Format("{0} {1}", FirstName, LastName);
        }

        public override string ToString()
        {
            return (FirstName.Length > 0) ? GetFullName() : base.ToString();
        }
    }
}
