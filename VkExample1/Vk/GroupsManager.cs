using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace VkApiNet.Vk
{
    public class GroupsManager : BaseManager
    {
        public GroupsManager(string token)
            : base(token)
        {
            Logger = log4net.LogManager.GetLogger("GroupsManager");
        }
        /// <summary>
        /// Возвращает список групп указанного пользователя. 
        /// </summary>
        /// <param name="user">ID пользователя, группы которого необходимо получить.</param>
        /// <returns></returns>
        public List<VkGroup> GetGroupsForUser(VkUser user)
        {
            var cmd = GetVkXmlCommand("groups.get", new Dictionary<string, object> {{"uid", user.Id}, {"extended", "1"}}, true);
            var response = cmd.GetResponseAsString();

            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(response);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("({0}) {1}", ex.GetType().Name, ex.Message);
                Logger.Debug(response);
                throw new ApplicationException("Сервер вернул некорректный ответ");
            }
            if (String.Compare(xml.DocumentElement.Name, "error", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Logger.Debug(response);
                throw new ApplicationException("Ошибка. Вызов процедуры ");
            }

            var result = new List<VkGroup>();
            foreach (XmlElement groupXml in xml.DocumentElement.GetElementsByTagName("group"))
            {
                long gid;
                try
                {
                    gid = Int32.Parse(groupXml["gid"].InnerText);
                }
                catch
                {
                    continue;
                }

                var g = new VkGroup(gid);
                if (groupXml["name"] != null) g.Name = groupXml["name"].InnerText;
                if (groupXml["photo"] != null) g.PhotoUrl = groupXml["photo"].InnerText;
                if (groupXml["screen_name"] != null) g.ScreenName = groupXml["screen_name"].InnerText;
                if(groupXml["is_admin"] != null) g.IsAdmin = String.Compare(groupXml["is_admin"].InnerText, "1") == 0;
                g.Owner = user;
                result.Add(g);
            }

            return result;
        }

    }
}
