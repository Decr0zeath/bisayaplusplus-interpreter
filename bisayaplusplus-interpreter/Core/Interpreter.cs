using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Core
{
    public class Interpreter
    {
        private VariableTable vars = new VariableTable();

        public string Execute(List<string> commands)
        {
            var sb = new StringBuilder();

            foreach (var line in commands)
            {
                if (line.StartsWith("MUGNA"))
                    HandleDeclaration(line);
                else if (line.StartsWith("IPAKITA"))
                    sb.AppendLine(HandleOutput(line));
                else if (line.Contains("="))
                    HandleAssignment(line);
            }

            return sb.ToString();
        }

        private void HandleDeclaration(string line)
        {
            // Example: MUGNA NUMERO x, y, z=5
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) throw new Exception("Invalid declaration syntax.");

            string type = parts[1];
            string varsPart = line.Substring(line.IndexOf(type) + type.Length).Trim();
            var declarations = varsPart.Split(',');

            foreach (var d in declarations)
            {
                var decl = d.Trim();
                if (decl.Contains("="))
                {
                    var kv = decl.Split('=');
                    string name = kv[0].Trim();
                    string value = kv[1].Trim();
                    vars.Declare(name, value);
                }
                else
                {
                    vars.Declare(decl, null);
                }
            }
        }

        private void HandleAssignment(string line)
        {
            var parts = line.Split('=');
            if (parts.Length != 2) throw new Exception("Invalid assignment syntax.");

            string varName = parts[0].Trim();
            string value = parts[1].Trim();
            vars.Assign(varName, value);
        }

        private string HandleOutput(string line)
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1) throw new Exception("Invalid IPAKITA syntax.");

            string expr = line.Substring(colonIndex + 1).Trim();
            string[] segments = expr.Split('&');

            var result = new StringBuilder();

            foreach (string seg in segments)
            {
                string s = seg.Trim();

                if (s == "$")
                    result.Append("\n");
                else if (s == "[&]")
                    result.Append("&");
                else if (s.StartsWith("\"") && s.EndsWith("\""))
                    result.Append(s.Substring(1, s.Length - 2));
                else if (s.StartsWith("'") && s.EndsWith("'"))
                    result.Append(s.Substring(1, s.Length - 2));
                else if (vars.Exists(s))
                    result.Append(vars.Get(s));
                else if (s.Length > 0)
                    result.Append(s);
            }

            return result.ToString();
        }
    }
}
