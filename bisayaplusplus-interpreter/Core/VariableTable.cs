using bisayaplusplus_interpreter.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bisayaplusplus_interpreter.Core
{
    public class VariableTable
    {
        private Dictionary<string, Variable> variables = new Dictionary<string, Variable>(StringComparer.Ordinal);
 
        public void Declare(string name, string type, object initialValue = null)
        {
            // should not print if no ampersand

            // put if for regular expression   @"^[A-Za-z_][A-Za-z0-9_]*$"
            // if(
            if (ReservedWords.Words.Contains(name))
                throw new Exception("Cannot use reserved words as variable");

            if (variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' already declared.");

            object val;
            
            if (initialValue != null) val = ConvertObjectToType(initialValue, type);
            else val = DefaultValueForType(type);

            variables[name] = new Variable(name, type, val);
        }

        public void Assign(string name, object value)
        {
            if (!variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' not declared.");

            var variable = variables[name];
            variable.Value = ConvertObjectToType(value, variable.Type);
        }

        public object GetValue(string name)
        {
            if (!variables.ContainsKey(name))
                throw new Exception("Variable '" + name + "' not declared.");

            return variables[name].Value;
        }

        public bool Exists(string name)
        {
            return variables.ContainsKey(name);
        }

        private object DefaultValueForType(string type)
        {
            switch (type)
            {
                case "NUMERO": return 0;
                case "TIPIK": return 0.0;
                case "LETRA": return '\0';
                case "TINUOD": return false;
                default: return null;
            }
        }

        private object ConvertObjectToType(object value, string targetType)
        {
            if (value == null) return DefaultValueForType(targetType);

            // If the caller has passed an already-typed object, accept/cast where sensible:
            if (targetType == "NUMERO") return DataTypeNUMERO(value);
            else if (targetType == "TIPIK") return DataTypeTIPIK(value);
            else if (targetType == "LETRA") return DataTypeLETRA(value);
            else if (targetType == "TINUOD") return DataTypeTINUOD(value);
            else return value; // Unknown type - keep as-is
        }

        private object DataTypeNUMERO(object value)
        {
            if (value is int) return value;
            if (value is double)
            {
                double d = (double)value;
                if (Math.Abs(d - Math.Truncate(d)) > double.Epsilon)
                    throw new Exception("Cannot convert non-integer value to NUMERO.");
                return Convert.ToInt32(d);
            }

            var s = value as string;
            if (s != null)
            {
                s = s.Trim();
                // strip surrounding quotes if present
                if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                    s = s.Substring(1, s.Length - 2);

                int iv;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out iv))
                    return iv;

                double dv;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out dv))
                {
                    if (Math.Abs(dv - Math.Truncate(dv)) > double.Epsilon)
                        throw new Exception("Cannot convert non-integer value to NUMERO.");
                    return Convert.ToInt32(dv);
                }
            }

            throw new Exception("Cannot convert value to NUMERO.");

        }
        private object DataTypeTIPIK(object value)
        {
            if (value is double) return value;
            if (value is int) return Convert.ToDouble(value);

            var s = value as string;
            if (s != null)
            {
                s = s.Trim();
                if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                    s = s.Substring(1, s.Length - 2);

                double dv;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out dv))
                    return dv;
            }

            throw new Exception("Cannot convert value to TIPIK.");
        }
        private object DataTypeLETRA(object value)
        {
            if (value is char) return value;

            var s = value as string;

            if (s != null)
            {
                s = s.Trim();
                if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                    s = s.Substring(1, s.Length - 2);

                if (s.Length == 1) return s[0];

                throw new Exception("LETRA must be a single character.");
            }

            throw new Exception("Cannot convert value to LETRA.");

        }
        private object DataTypeTINUOD(object value)
        {
            if (value is bool) return value;

            var s = value as string;

            MessageBox.Show(s.ToString());
            if (s != null)
            {
                s = s.Trim();
                if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                    s = s.Substring(1, s.Length - 2);

                var up = s.ToUpperInvariant();
                if (up == "OO" || up == "TRUE") return "OO";
                if (up == "DILI" || up == "FALSE") return "DILI";
            }

            throw new Exception("Cannot convert value to TINUOD.");
        }



    }
}
