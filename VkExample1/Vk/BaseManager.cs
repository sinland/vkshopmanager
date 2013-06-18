using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using VkApiNet.Vk.Utility;

namespace VkApiNet.Vk
{
    public class BaseManager
    {
        protected class VkWebCommand
        {
            private readonly string m_methodName;
            private readonly string m_uri;
            private static DateTime s_lastReqTime;
            private readonly log4net.ILog m_logger;

            static VkWebCommand()
            {
                s_lastReqTime = DateTime.Now;
            }

            public VkWebCommand(string methodName, string uri)
            {
                m_methodName = methodName;
                m_logger = log4net.LogManager.GetLogger("VkWebCommand");
                m_uri = Uri.EscapeUriString(uri);
            }
            
            public string MethodName { get { return m_methodName; } }

            public string GetResponseAsString()
            {
                while((DateTime.Now - s_lastReqTime).TotalMilliseconds < 400)
                {
                    Thread.Sleep(100);
                }
                
                var result = new StringBuilder();
#if DEBUG 
                m_logger.DebugFormat("Request to '{0}'", m_uri);
                result.Append(PacketSampler.GetSampleXmlResponse(m_methodName));
#else
                var request = WebRequest.Create(m_uri);
                var response = request.GetResponse();
                var responseStream = response.GetResponseStream();
                if (responseStream == null) return "";

                var buffer = new byte[1024 * 64];
                
                
                while (true)
                {
                    var bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    result.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }
#endif
                s_lastReqTime = DateTime.Now;
                return result.ToString();
            }
        }

        protected readonly string AccessToken;
        protected log4net.ILog Logger;

        public BaseManager(string accessToken)
        {
            AccessToken = accessToken;
            Logger = log4net.LogManager.GetLogger("BaseManager");
        }
        
        protected VkWebCommand GetVkXmlCommand(string methodName, Dictionary<string, object> parameters, bool includeToken)
        {
            if (includeToken)
            {
                parameters.Add("access_token", AccessToken);
            }
            var uri = String.Format("https://api.vk.com/method/{0}.xml?app=1", methodName);
            uri = parameters.Aggregate(uri, (current, pair) => current + String.Format("&{0}={1}", pair.Key, pair.Value));
            
            Logger.DebugFormat("XML Command: '{0}'", uri);
            return new VkWebCommand(methodName, uri);
        }

        protected DateTime GetDateTimeFromUnix(int stamp)
        {
            return new DateTime(1970, 1, 1).AddSeconds(stamp);
        }
    }
}
