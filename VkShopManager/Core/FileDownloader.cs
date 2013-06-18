using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace VkShopManager.Core
{
    public class FileDownloader
    {
        private readonly Uri _url;

        public FileDownloader(Uri url)
        {
            _url = url;
        }

        public void DownloadTo(string destFile)
        {
            var request = WebRequest.Create(_url);
            var response = request.GetResponse();

            var responseStream = response.GetResponseStream();
            if (responseStream == null) throw new ApplicationException("Не удалось получить ответ от сервера");

            var buffer = new byte[1024 * 64];
            using (var mem = new MemoryStream())
            {
                while (true)
                {
                    var bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    mem.Write(buffer, 0, bytesRead);
                }

                File.WriteAllBytes(destFile, mem.ToArray());
            }
        }
    }
}
