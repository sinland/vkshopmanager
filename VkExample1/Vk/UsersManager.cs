using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using log4net;

namespace VkApiNet.Vk
{
    public class UsersManager : BaseManager
    {
        
        public UsersManager(string accessToken) : base(accessToken)
        {
            Logger = LogManager.GetLogger("UsersManager");
        }

        public List<VkUser> Search(VkUsersSearchQuery query)
        {
            var cmdParams = new Dictionary<string, object>(0);
            foreach (KeyValuePair<UserSearchParam, object> param in query.GetParameters())
            {
                switch (param.Key)
                {
                    case UserSearchParam.Query:
                        cmdParams.Add("q", param.Value);
                        break;
                    case UserSearchParam.Sort:
                        cmdParams.Add("sort", param.Value);
                        break;
                    case UserSearchParam.Offset:
                        cmdParams.Add("offset", param.Value);
                        break;
                    case UserSearchParam.Count:
                        cmdParams.Add("count", param.Value);
                        break;
                    case UserSearchParam.Fields:
                        var sb = new StringBuilder();
                        foreach (VkProfile.EntryType type in param.Value as IEnumerable<VkProfile.EntryType>)
                        {
                            sb.Append(String.Format("{0},", VkProfile.EntryTypeToString(type)));
                        }
                        sb.Remove(sb.Length - 1, 1);
                        cmdParams.Add("fields", sb.ToString());
                        break;
                    case UserSearchParam.CityId:
                        cmdParams.Add("city", param.Value);
                        break;
                    case UserSearchParam.CountryId:
                        cmdParams.Add("country", param.Value);
                        break;
                    case UserSearchParam.HomeTown:
                        cmdParams.Add("hometown", param.Value);
                        break;
                    case UserSearchParam.Sex:
                        cmdParams.Add("sex", param.Value);
                        break;
                    case UserSearchParam.AgeFrom:
                        cmdParams.Add("age_from", param.Value);
                        break;
                    case UserSearchParam.AgeTo:
                        cmdParams.Add("age_to", param.Value);
                        break;
                    case UserSearchParam.BirthDay:
                        cmdParams.Add("birth_day", param.Value);
                        break;
                    case UserSearchParam.BirthMonth:
                        cmdParams.Add("birth_month", param.Value);
                        break;
                    case UserSearchParam.BirthYear:
                        cmdParams.Add("birth_year", param.Value);
                        break;
                    case UserSearchParam.Online:
                        cmdParams.Add("online", param.Value);
                        break;
                    case UserSearchParam.Interests:
                        cmdParams.Add("interests", param.Value);
                        break;
                    case UserSearchParam.Gid:
                        cmdParams.Add("gid", param.Value);
                        break;
                    default:
                        continue;
                }
            }

            var cmd = GetVkXmlCommand("users.search", cmdParams, true);
            string response;
            try
            {
                response = cmd.GetResponseAsString();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
                throw new VkMethodInvocationException((int)VkMethodInvocationErrorCode.FailedGetResponse,
                                                  "Не удалось получить ответ от сервера");
            }
            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(response);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("XML parsing failed. ({0}) {1}", ex.GetType().Name, ex.Message);
                Logger.Debug(response);
                throw new VkMethodInvocationException((int)VkMethodInvocationErrorCode.IncorrectReplyFormat, "Сервер вернул некорректный ответ");
            }
            if (VkErrorResponse.HasErrorInResponse(xml))
            {
                var err = VkErrorResponse.Parse(xml);
                Logger.DebugFormat("VK Error ({0}) : {1}", err.ErrorCode, err.Message);
                throw new VkMethodInvocationException(err.ErrorCode, err.Message);
            }

            var result = new List<VkUser>();
            // todo: Сделать парсинг дополнительных полей анкеты

            foreach (XmlElement resultNode in xml.DocumentElement.GetElementsByTagName("user"))
            {
                Int64 uid;
                if (resultNode["uid"] == null) continue;
                try
                {
                    uid = Int64.Parse(resultNode["uid"].InnerText);
                }
                catch (Exception)
                {
                    Logger.ErrorFormat("Cant parse UID {0}", resultNode["uid"].InnerText);
                    continue;
                }
                var usr = new VkUser(uid);
                if (resultNode["first_name"] != null)
                {
                    usr.FirstName = resultNode["first_name"].InnerText;
                }
                if (resultNode["last_name"] != null)
                {
                    usr.LastName = resultNode["last_name"].InnerText;
                }
                if (resultNode["mobile_phone"] != null && resultNode["mobile_phone"].InnerText.Length > 0)
                {
                    usr.MobilePhone = resultNode["mobile_phone"].InnerText;
                }
                if (resultNode["home_phone"] != null && resultNode["home_phone"].InnerText.Length > 0)
                {
                    usr.HomePhone = resultNode["home_phone"].InnerText;
                }
                if (resultNode["sex"] != null)
                {
                    try
                    {
                        usr.Sex = Int32.Parse(resultNode["sex"].InnerText);
                    }
                    catch (Exception)
                    {
                        usr.Sex = 0;
                    }
                }
                if (query[UserSearchParam.BirthYear] != null)
                {
                    usr.BirthYear = Int32.Parse(query[UserSearchParam.BirthYear].ToString());
                }
                result.Add(usr);
            }

            return result;
        }

        public VkUser GetUserById(Int64 uid)
        {
            var parameters = new Dictionary<string, object> { { "uids", uid } };
            var cmd = GetVkXmlCommand("users.get", parameters, false);

            string response;
            try
            {
                response = cmd.GetResponseAsString();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
                throw new ApplicationException("Не удалось получить ответ от сервера");
            }

            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(response);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("XML parsing failed. ({0}) {1}", ex.GetType().Name, ex.Message);
                Logger.Debug(response);
                throw new ApplicationException("Сервер вернул некорректный ответ");
            }

            var user = new VkUser(uid) {Sex = 0};
            foreach (XmlElement albumXml in xml.DocumentElement.GetElementsByTagName("user"))
            {
                user.FirstName = albumXml["first_name"].InnerText;
                user.LastName = albumXml["last_name"].InnerText;

                break;
            }
            
            return user;
        }
    }
}
