namespace VkApiNet.Vk
{
    /// <summary>
    /// Коды локальных ошибок. Всегда меньше 0. Коды ошибок ВКонтакте всегда больше 0.
    /// </summary>
    public enum VkMethodInvocationErrorCode
    {
        FailedGetResponse = -1,
        IncorrectReplyFormat = -2,
    }
}