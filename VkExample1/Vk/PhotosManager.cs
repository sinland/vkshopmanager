using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace VkApiNet.Vk
{
    public class PhotosManager : BaseManager
    {
        public PhotosManager(string accessToken) : base(accessToken)
        {
            Logger = log4net.LogManager.GetLogger("PhotosManager");
        }
        /// <summary>
        /// Возвращает список альбомов в группе пользователя. 
        /// </summary>
        /// <param name="group">Группа альбомы которой запрашиваются</param>
        /// <returns></returns>
        public List<VkAlbum> GetAlbumsForGroup(VkGroup group)
        {
            var parameters = new Dictionary<string, object> { { "gid", group.Id } };
            var cmd = GetVkXmlCommand("photos.getAlbums", parameters, false);

            string response;
            try
            {
                response = cmd.GetResponseAsString();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
                throw new VkMethodInvocationException((int) VkMethodInvocationErrorCode.FailedGetResponse,
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

            var result = new List<VkAlbum>();
            foreach (XmlElement albumXml in xml.DocumentElement.GetElementsByTagName("album"))
            {
                long aid;
                try
                {
                    aid = Int64.Parse(albumXml["aid"].InnerText);
                }
                catch (Exception e) 
                {
                    Logger.ErrorFormat("Cant parse album ID. ({0}) {1}", e.GetType().Name, e.Message);
                    continue;
                }
                var g = new VkAlbum(aid);

                if (albumXml["created"] != null)
                {
                    try
                    {
                        g.CreationDate = GetDateTimeFromUnix(Int32.Parse(albumXml["created"].InnerText));
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'created' property of album '{0}' : {1}", g.Id, albumXml["created"].InnerText);
                    }
                }
                if (albumXml["updated"] != null)
                {
                    try
                    {
                        g.UpdateDate = GetDateTimeFromUnix(Int32.Parse(albumXml["updated"].InnerText));
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'updated' property of album '{0}' : {1}", g.Id, albumXml["updated"].InnerText);
                    }
                }
                if (albumXml["size"] != null)
                {
                    try
                    {
                        g.Size = Int32.Parse(albumXml["size"].InnerText);
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'size' property of album '{0}' : {1}", g.Id, albumXml["size"].InnerText);
                    }
                }
                if (albumXml["owner_id"] != null)
                {
                    try
                    {
                        g.OwnerId = Int32.Parse(albumXml["owner_id"].InnerText);
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'owner_id' property of album '{0}' : {1}", g.Id, albumXml["owner_id"].InnerText);
                    }
                }
                if (albumXml["title"] != null)
                {
                    g.Title = albumXml["title"].InnerText;
                }
                if (albumXml["description"] != null)
                {
                    g.Description = albumXml["description"].InnerText;
                }
                if (albumXml["thumb_src"] != null)
                {
                    g.ThumbUrl = albumXml["thumb_src"].InnerText;
                }

                result.Add(g);
            }

            return result;
        }
        /// <summary>
        /// Возвращает список фотографий в альбоме. 
        /// </summary>
        /// <param name="album">Альбом, содержащий фотографии</param>
        /// <returns>Список фотографий альбома</returns>
        public List<VkPhoto> GetAlbumPhotos(VkAlbum album)
        {
            var parameters = new Dictionary<string, object> {{"oid", album.OwnerId}, {"aid", album.Id}, {"extended", 1}};
            var cmd = GetVkXmlCommand("photos.get", parameters, false);
            string response;
            try
            {
                response = cmd.GetResponseAsString();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
                throw new VkMethodInvocationException((int) VkMethodInvocationErrorCode.FailedGetResponse,
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
                throw new VkMethodInvocationException((int) VkMethodInvocationErrorCode.IncorrectReplyFormat,
                                                  "Сервер вернул некорректный ответ");
            }

            var result = new List<VkPhoto>(0);
            foreach (XmlElement photoXml in xml.DocumentElement.GetElementsByTagName("photo"))
            {
                Int64 pid;
                try
                {
                    pid = Int64.Parse(photoXml["pid"].InnerText);
                }
                catch
                {
                    Logger.ErrorFormat("Cant parse 'pid' property of photo : {0}",photoXml["pid"].InnerText);
                    continue;
                }
                var g = new VkPhoto(pid);
                
                if (photoXml["owner_id"] != null)
                {
                    try
                    {
                        g.OwnerId = Int32.Parse(photoXml["owner_id"].InnerText);
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'owner_id' property of photo '{0}' : {1}", g.Id, photoXml["owner_id"].InnerText);
                        g.OwnerId = 0;
                    }
                }
                if (photoXml["comments"] != null && photoXml["comments"]["count"] != null)
                {
                    try
                    {
                        g.CommentsCount = Int32.Parse(photoXml["comments"]["count"].InnerText);
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'comments.count' property of photo '{0}' : {1}", g.Id,
                                           photoXml["comments"]["count"].InnerText);
                        g.CommentsCount = 0;
                    }
                    
                }
                if (photoXml["src"] != null)
                {
                    g.SourceUrl = photoXml["src"].InnerText;
                }
                if (photoXml["text"] != null)
                {
                    g.Text = photoXml["text"].InnerText;
                }
                if (photoXml["created"] != null)
                {
                    try
                    {
                        g.CreationDate = GetDateTimeFromUnix(Int32.Parse(photoXml["created"].InnerText));
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'created' property of photo '{0}' : {1}", g.Id,
                                           photoXml["created"].InnerText);
                        g.CreationDate = DateTime.MinValue;
                    }
                }
                

                result.Add(g);
            }

            return result;
        }

        public List<VkComment> GetPhotoComments(VkPhoto photo)
        {
            var parameters = new Dictionary<string, object> { { "pid", photo.Id }, { "owner_id", photo.OwnerId }, { "count", 100 } };
            var cmd = GetVkXmlCommand("photos.getComments", parameters, true);
            string response;
            try
            {
                response = cmd.GetResponseAsString();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("({0}) {1}", e.GetType().Name, e.Message);
                throw new VkMethodInvocationException((int) VkMethodInvocationErrorCode.FailedGetResponse,
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
                throw new VkMethodInvocationException((int) VkMethodInvocationErrorCode.IncorrectReplyFormat,
                                                  "Сервер вернул некорректный ответ");
            }

            var result = new List<VkComment>(0);
            foreach (XmlElement cmntXml in xml.DocumentElement.GetElementsByTagName("comment"))
            {
                long cid;
                try
                {
                    cid = Int64.Parse(cmntXml["cid"].InnerText);
                }
                catch
                {
                    Logger.ErrorFormat("Cant parse 'cid' property of comment: {0}", cmntXml["cid"].InnerText);
                    continue;
                }

                var g = new VkComment(cid);

                if (cmntXml["from_id"] != null)
                {
                    try
                    {
                        g.SenderId = Int32.Parse(cmntXml["from_id"].InnerText);
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'from_id' property of comment '{0}' : {1}", g.Id,
                                           cmntXml["from_id"].InnerText);
                        g.SenderId = 0;
                    }
                }
                if (cmntXml["date"] != null)
                {
                    try
                    {
                        g.Date = GetDateTimeFromUnix(Int32.Parse(cmntXml["date"].InnerText));
                    }
                    catch
                    {
                        Logger.DebugFormat("Cant parse 'date' property of comment '{0}' : {1}", g.Id,
                                           cmntXml["date"].InnerText);
                        g.Date = DateTime.MinValue;
                    }
                }
                if (cmntXml["message"] != null)
                {
                    g.Message = cmntXml["message"].InnerText;
                }
                result.Add(g);
            }
            return result;
        }
    }
}
