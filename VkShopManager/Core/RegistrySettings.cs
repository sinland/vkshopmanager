using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.Win32;

namespace VkShopManager.Core
{
    public class RegistrySettings
    {
        private enum OpenType
        {
            Write,
            ReadOnly
        }
        
        private static RegistrySettings s_sigleton;

        public static RegistrySettings GetInstance()
        {
            if (s_sigleton == null)
            {
                s_sigleton = new RegistrySettings();
            }

            return s_sigleton;
        }

        public RegistrySettings()
        {
            
        }

        private RegistryKey OpenConfigKey(OpenType openType)
        {
            bool writable = openType == OpenType.Write;
            var r = Registry.CurrentUser.OpenSubKey(@"software\sinland\VkApps\SnzShop", writable) ??
                    Registry.CurrentUser.CreateSubKey(@"software\sinland\VkApps\SnzShop");
            return r;
        }
        private object GetValue(string name)
        {
            using (var k = OpenConfigKey(OpenType.ReadOnly))
            {
                return k.GetValue(name, null);
            }
        }
        private void SetValue(string name, object value, RegistryValueKind rvk)
        {
            using (var k = OpenConfigKey(OpenType.Write))
            {
                k.SetValue(name, value, rvk);
            }
        }

        public bool ClearReportsOnExit {
            get
            {
                bool val = false;
                var data = GetValue("ClearReportsOnExit");
                if (data != null)
                {
                    Boolean.TryParse(data.ToString(), out val);
                }
                return val;
            }
            set
            {
                SetValue("ClearReportsOnExit", value, RegistryValueKind.String);
            } 
        }
        public string AccessToken
        {
            get { return GetValue("AccessToken") == null ? "" : GetValue("AccessToken").ToString(); }
            set { SetValue("AccessToken", value, RegistryValueKind.String); }
        }
        public string GalleryPath
        {
            get
            {
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Gallery");
            }
        }
        public string ReportsPath
        {
            get
            {
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            }
        }
        public string TempPath
        {
            get
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VKGM");
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                catch (Exception)
                {

                }
                return path;
            }
        }

        public int LoggedUserId
        {
            get { return GetValue("UserId") == null ? 0 : Int32.Parse(GetValue("UserId").ToString()); }
            set { SetValue("UserId", value, RegistryValueKind.DWord); }
        }
        public int WorkGroupId
        {
            get { return GetValue("WorkGroupId") == null ? 0 : Int32.Parse(GetValue("WorkGroupId").ToString()); }
            set { SetValue("WorkGroupId", value, RegistryValueKind.DWord); }
        }
        public int SelectedCodeNumberGenerator
        {
            get { return GetValue("SelectedCodeNumberGenerator") == null ? 0 : Int32.Parse(GetValue("SelectedCodeNumberGenerator").ToString()); }
            set { SetValue("SelectedCodeNumberGenerator", value, RegistryValueKind.DWord); }
        }
        public int CodeNumberNumericLength
        {
            get
            {
                var v = GetValue("CodeNumberNumericLength");
                if (v == null)
                {
                    SetValue("CodeNumberNumericLength", 6, RegistryValueKind.DWord);
                    return 6;
                }
                return Int32.Parse(v.ToString());

            }
            set { SetValue("CodeNumberNumericLength", value, RegistryValueKind.DWord); }
        }
        public string CodeNumberAlphaPrefix
        {
            get
            {
                var v = GetValue("CodeNumberAlphaPrefix");
                if (v == null)
                {
                    SetValue("CodeNumberAlphaPrefix", @"SM", RegistryValueKind.String);
                    return @"SM";
                }

                return v.ToString();
            }
            set { SetValue("CodeNumberNumericLength", value, RegistryValueKind.String); }
        }
        public void SetExpiration(int secondsToExpire)
        {
            var dt = DateTime.Now.AddSeconds(secondsToExpire);
            SetValue("ExpirationDate", dt.Ticks, RegistryValueKind.QWord);
        }

        public DateTime ExpirationDate
        {
            get
            {
                long d;
                if (!Int64.TryParse(GetValue("ExpirationDate") == null ? "0" : GetValue("ExpirationDate").ToString(), out d))
                {
                    d = 0;
                }
                
                return new DateTime(d);
            }
        }

        public bool ShowPartialPositions
        {
            get { return GetValue("ShowPartialPositions") != null && Boolean.Parse(GetValue("ShowPartialPositions").ToString()); }
            set { SetValue("ShowPartialPositions", value, RegistryValueKind.String); }
        }
        public bool ShowEmptyPositions
        {
            get { return GetValue("ShowEmptyPositions") != null && Boolean.Parse(GetValue("ShowEmptyPositions").ToString()); }
            set { SetValue("ShowEmptyPositions", value, RegistryValueKind.String); }
        }
        /// <summary>
        /// Список vk_id альбомов, отображение которых скрыто
        /// </summary>
        /// <returns></returns>
        public List<long> GetHiddenList()
        {
            var res = new List<long>();
                var storedVal = GetValue("HiddenList") as byte[];
                if (storedVal != null)
                {
                    var stream = new MemoryStream(storedVal);
                    var formatter = new BinaryFormatter();
                    res = (List<long>)formatter.Deserialize(stream);
                }
                return res;
        }
        public void SetHiddenList(List<long> value)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, value);

            SetValue("HiddenList", stream.ToArray(), RegistryValueKind.Binary);
        }
        public void ClearRegistry()
        {
            using (var k = OpenConfigKey(OpenType.Write))
            {
                k.DeleteValue("AccessToken", false);
                k.DeleteValue("ExpirationDate", false);
            }
        }
    }
}
