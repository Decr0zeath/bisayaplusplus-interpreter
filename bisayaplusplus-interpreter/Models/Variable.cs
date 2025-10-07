using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Models
{
    public class Variable
    {
        public string Name { get; set; }
        public string Type { get; set; } 
        public object Value { get; set; }

        public Variable(string name, string type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }
}
