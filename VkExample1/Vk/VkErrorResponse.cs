using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace VkApiNet.Vk
{
    public class VkErrorResponse
    {
        public static bool HasErrorInResponse(string rawResponse)
        {
            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(rawResponse);
            }
            catch
            {
                return true;
            }
            return HasErrorInResponse(xml);
        }
        public static bool HasErrorInResponse(XmlDocument response)
        {
            return String.Compare(response.DocumentElement.Name, "error", true) == 0;
        }

        public static VkErrorResponse Parse(string rawData)
        {
            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(rawData);
            }
            catch
            {
                return null;
            }

            return Parse(xml);
        }
        public static VkErrorResponse Parse(XmlDocument xml)
        {
            var obj = new VkErrorResponse();
            obj.ErrorCode = xml.DocumentElement["error_code"] == null
                                ? 0
                                : Int32.Parse(xml.DocumentElement["error_code"].InnerText);
            obj.Message = xml.DocumentElement["error_msg"] == null
                                ? ""
                                : xml.DocumentElement["error_code"].InnerText;

            return obj;
        }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }
}
