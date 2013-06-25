using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Core
{
    class CodeNumberGenerator
    {
        public enum Type
        {
            NumericOnly = 0,
            AlphaNumeric = 1,
            None
        }

        public CodeNumberGenerator()
        {
            
        }

        public string GetAlphaNumericCode(int length, string prefix)
        {
            var rnd = new Random();
            var result = prefix.ToUpper();
            for (int i = 0; i < length; i++)
            {
                result += String.Format("{0}", rnd.Next(0, 9));
            }
            return result;
        }
        public string GetNumericCode(int length)
        {
            var rnd = new Random();
            var result = "";
            for (int i = 0; i < length; i++)
            {
                result += String.Format("{0}", rnd.Next(0, 9));
            }
            return result;
        }
        
        public Type ConvertToType(int code)
        {
            if (code == (int)Type.AlphaNumeric) return Type.AlphaNumeric;
            else if (code == (int) Type.NumericOnly) return Type.NumericOnly;
            else return Type.None;
        }
    }
}
