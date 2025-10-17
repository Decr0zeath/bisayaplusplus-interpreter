using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Core
{
    public class Token
    {
        public string Value { get; set; }
        public string Type { get; set; }   // Keyword, Identifier, Literal

        public Token(string value, string type)
        {
            Value = value;
            Type = type;
        }
    }
}
