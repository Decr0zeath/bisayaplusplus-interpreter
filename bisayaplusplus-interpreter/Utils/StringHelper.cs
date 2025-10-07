using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Utils
{
    public static class StringHelper
    {
        public static string Clean(string input)
        {
            if (input == null) return "";
            return input.Replace("\r", "").Trim();
        }
    }
}
