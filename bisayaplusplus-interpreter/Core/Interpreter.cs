using bisayaplusplus_interpreter.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bisayaplusplus_interpreter.Core
{
    public class Interpreter
    {
        private VariableTable vars = new VariableTable();
        private StringHelper strhelper = new StringHelper();
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

            string type = parts[1].Trim();
            string rest = line.Substring(line.IndexOf(type) + type.Length).Trim();

            // split declarations by comma (simple split is ok for declarations)
            var declarations = rest.Split(',');

            foreach (var d in declarations)
            {
                var decl = d.Trim();
                if (decl.Length == 0) continue;

                if (decl.Contains("="))
                {
                    int eq = decl.IndexOf('=');
                    string name = decl.Substring(0, eq).Trim();
                    string valueToken = decl.Substring(eq + 1).Trim();
                    // pass raw token — VariableTable will convert it to declared type
                    vars.Declare(name, type, valueToken);
                }
                else
                {
                    string name = decl;
                    vars.Declare(name, type, null);
                }
            }
        }

        private void HandleAssignment(string line)
        {
            // Split on '=' but only top-level (outside quotes). Use helper.
            var parts = strhelper.SplitTopLevel(line, '=');
            if (parts.Count < 2) throw new Exception("Invalid assignment syntax.");

            // RHS token is last part
            string rhsToken = parts[parts.Count - 1].Trim();

            object rhsValue;
            // If rhsToken is a declared variable, use its value. Otherwise pass the raw token string
            if (vars.Exists(rhsToken))
                rhsValue = vars.GetValue(rhsToken);
            else
                rhsValue = rhsToken;

            // Assign from right-to-left: for x = y = 4 -> assign y then x (both get evaluated RHS)
            for (int i = parts.Count - 2; i >= 0; i--)
            {
                string varName = parts[i].Trim();
                if (!vars.Exists(varName))
                    throw new Exception("Variable '" + varName + "' not declared.");
                vars.Assign(varName, rhsValue);
            }
        }

        private string HandleOutput(string line)
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex == -1) throw new Exception("Invalid IPAKITA syntax.");

            string expr = line.Substring(colonIndex + 1).Trim();
            var segments = strhelper.SplitExpression(expr);
            var result = new StringBuilder();

            foreach (string seg in segments)
            {
                string s = seg.Trim();

                if (s == "$")
                    result.Append("\n");
                else if (s == "[&]")
                    result.Append("&");
                else if (s == "[-]")
                    result.Append("-");
                else if (s == "[]")
                    result.Append(" ");
                else if (s.StartsWith("\"") && s.EndsWith("\""))
                    result.Append(s.Substring(1, s.Length - 2));
                else if (s.StartsWith("'") && s.EndsWith("'"))
                    result.Append(s.Substring(1, s.Length - 2));
                else if (vars.Exists(s))
                {
                    var val = vars.GetValue(s);
                    if (val != null)
                        result.Append(val.ToString());
                }
                else if (s.Length > 0)
                    result.Append(s);
            }

            return result.ToString();
        }
    }
}
