using System.Text;

namespace VkApiNet.Vk.Utility
{
    class PacketSampler
    {
        public static string GetSampleXmlResponse(string methodName)
        {
            methodName = methodName.ToLower();
            switch (methodName)
            {
                case "users.get":
                    return UsersGetResponse();
                case "users.search":
                    return UsersSearchResponse();
                default:
                    return ErrorResponse(0, "Unknown method name");
            }
        }

        private static string UsersSearchResponse()
        {
            var sb = new StringBuilder("<?xml version=\"1.0\"?>");
            sb.AppendLine("<response>");
            sb.AppendFormat("<user><uid>1</uid><first_name>1111</first_name><last_name>111111</last_name></user>");
            sb.AppendFormat("<user><uid>2</uid><sex>1</sex><first_name>222</first_name><last_name>2222222</last_name></user>");
            sb.AppendLine("</response>");
            return sb.ToString();
        }

        private static string UsersGetResponse()
        {
            var sb = new StringBuilder("<?xml version=\"1.0\"?>");
            sb.AppendLine("<response>");
            sb.AppendFormat("<user><uid>1</uid><first_name>???</first_name><last_name>???????</last_name></user>");
            sb.AppendLine("</response>");
            return sb.ToString();
        }

        private static string Response()
        {
            var sb = new StringBuilder();
            return sb.ToString();
        }
        private static string ErrorResponse(int code, string msg)
        {
            var sb = new StringBuilder("<?xml version=\"1.0\"?>");
            sb.AppendLine("<error>");
            sb.AppendFormat("<error_code>{0}</error_code>", code);
            sb.AppendFormat("<error_msg>(Local) {0}</error_msg>", msg);
            sb.AppendLine("</error>");
            return sb.ToString();
        }
    }
}
