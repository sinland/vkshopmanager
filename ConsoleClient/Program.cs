using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using VkApiNet.Vk;

namespace ConsoleClient
{
    class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            const string accessToken = "";

            var album = new VkAlbum(171831917)
                {
                    OwnerId = Int32.Parse("-51777326")
                };
            var photosMgr = new PhotosManager(accessToken);
            var um = new UsersManager(accessToken);

            var photos = photosMgr.GetAlbumPhotos(album);

            var sb = new StringBuilder();
            var dic = new Dictionary<Int64, List<VkPhoto>>();
            var comments = new Dictionary<VkPhoto, List<VkComment>>();
            var usersCache = new Dictionary<Int64, VkUser>();

            foreach (VkPhoto vkPhoto in photos)
            {
                if (vkPhoto.CommentsCount == 0) continue;

                sb.AppendLine(String.Format("({0}) {1}:", vkPhoto.Id, vkPhoto.Text));
                Console.WriteLine("Looking in " + vkPhoto.Id);
                comments.Add(vkPhoto, new List<VkComment>());

                foreach (var comment in photosMgr.GetPhotoComments(vkPhoto))
                {
                    if (!usersCache.ContainsKey(comment.SenderId))
                    {
                        var sender = um.GetUserById(comment.SenderId);
                        usersCache.Add(comment.SenderId, sender);
                    }

                    sb.AppendLine(String.Format("\t[{0}]: {1}", usersCache[comment.SenderId].GetFullName(), comment.Message));
                    comments[vkPhoto].Add(comment);

                    if (!dic.ContainsKey(comment.SenderId))
                    {
                        dic.Add(comment.SenderId, new List<VkPhoto>());
                    }
                    if (!dic[comment.SenderId].Contains(vkPhoto)) dic[comment.SenderId].Add(vkPhoto);
                }
                sb.AppendLine();
            }
            sb.AppendLine();


            foreach (KeyValuePair<Int64, List<VkPhoto>> pair in dic)
            {
                VkUser user = usersCache[pair.Key];

                sb.AppendLine(user.GetFullName() + ":");
                foreach (VkPhoto photo in pair.Value)
                {
                    sb.AppendLine(String.Format("\t{0}", photo.Text));
                    foreach (KeyValuePair<VkPhoto, List<VkComment>> valuePair in comments)
                    {
                        if (valuePair.Key.Id != photo.Id) continue;

                        foreach (VkComment vkComment in valuePair.Value)
                        {
                            if (vkComment.SenderId != user.Id) continue;

                            sb.AppendLine(String.Format("\t\t[{0}]: {1}", vkComment.Date, vkComment.Message));
                        }
                    }
                }
                sb.AppendLine();
            }

            File.WriteAllText(@"f:\temp\result1.txt", sb.ToString(), Encoding.UTF8);
        }
    }
}
