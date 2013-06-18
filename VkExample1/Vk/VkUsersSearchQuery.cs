using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkApiNet.Vk
{
    /// <summary>
    /// Параметры поискового запроса
    /// </summary>
    public enum UserSearchParam
    {
        /// <summary>
        /// Строка поискового запроса (строка)
        /// </summary>
        Query,

        /// <summary>
        /// Сортировака результатов (1 - по дате регистрации, 0 - по популярности) (целое)
        /// </summary>
        Sort,

        /// <summary>
        /// Смещение отностительно первого найденного пользователя (целое положительное число)
        /// </summary>
        Offset,

        /// <summary>
        /// Количество возвращаемых пользователей (20 - 1000)
        /// </summary>
        Count,

        /// <summary>
        /// Перечисленные через запятую поля анкет для запроса (IEnumerable(VkProfile.EntryType))
        /// </summary>
        Fields,

        /// <summary>
        /// Идентификатор города (целое положительное число)
        /// </summary>
        CityId,

        /// <summary>
        /// Идентификатор страны (целое положительное число)
        /// </summary>
        CountryId,

        /// <summary>
        /// Название города строкой (строка)
        /// </summary>
        HomeTown,

        /// <summary>
        /// Пол. 1 - женщина, 2 - мужчина, 0 (по умолчанию) - все. (целое положительное число)
        /// </summary>
        Sex,

        /// <summary>
        /// Начиная с какого возраста (целое положительное число)
        /// </summary>
        AgeFrom,

        /// <summary>
        /// До какого возраста (целое положительное число)
        /// </summary>
        AgeTo,

        /// <summary>
        /// День рождения (номер дня месяца) (целое положительное число)
        /// </summary>
        BirthDay,

        /// <summary>
        /// Месяц рождения (целое положительное число)
        /// </summary>
        BirthMonth,

        /// <summary>
        /// Год рождения (целое положительное число)
        /// </summary>
        BirthYear,

        /// <summary>
        /// 1 - только в сети, 0 - все пользователи
        /// </summary>
        Online,

        /// <summary>
        /// Интересы (строка)
        /// </summary>
        Interests,

        /// <summary>
        /// Идентификатор группы, среди которой проводить поиск
        /// </summary>
        Gid

    }

    public class VkUsersSearchQuery
    {
        private readonly Dictionary<UserSearchParam, object> m_parameters;

        public VkUsersSearchQuery()
        {
            m_parameters = new Dictionary<UserSearchParam, object>(0);
        }

        public void SetParameter(UserSearchParam p, object value)
        {
            if (m_parameters.ContainsKey(p))
            {
                m_parameters[p] = value;
            }
            else
            {
                m_parameters.Add(p, value);
            }    
        }

        public object this[UserSearchParam p]
        {
            get { return m_parameters.ContainsKey(p) ? m_parameters[p] : null; }
            set
            {
                SetParameter(p, value);
            }
        }

        public Dictionary<UserSearchParam, object> GetParameters()
        {
            return m_parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value);
        }
    }
}
