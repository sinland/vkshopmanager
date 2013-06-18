using System;

namespace VkApiNet.Vk
{
    public class VkMethodInvocationException : Exception
    {
        public int ErrorCode { get; set; }

        public VkMethodInvocationException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}