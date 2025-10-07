using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Core
{
    public class VariableTable
    {
        private Dictionary<string, object> variables = new Dictionary<string, object>();

        public void Declare(string name, object value)
        {
            if (variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' already declared.");
            variables[name] = value;
        }

        public void Assign(string name, object value)
        {
            if (!variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' not declared.");
            variables[name] = value;
        }

        public object Get(string name)
        {
            if (!variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' not declared.");
            return variables[name];
        }

        public bool Exists(string name)
        {
            return variables.ContainsKey(name);
        }
    }
}
