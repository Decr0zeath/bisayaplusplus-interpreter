using bisayaplusplus_interpreter.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic; // add reference to Microsoft.VisualBasic
using System.Text.RegularExpressions;

namespace bisayaplusplus_interpreter.Core
{
    public class Interpreter
    {
        private VariableTable vars = new VariableTable();
        private StringHelper strhelper = new StringHelper();

        public string Execute(List<string> commands)
        {
            var sb = new StringBuilder();

            // main execution using index so we can jump over blocks
            for (int i = 0; i < commands.Count; i++)
            {
                string line = commands[i].Trim();

                if (line.StartsWith("MUGNA"))
                {
                    HandleDeclaration(line);
                }
                else if (line.StartsWith("IPAKITA"))
                {
                    sb.AppendLine(HandleOutput(line));
                }
                else if (line.StartsWith("DAWAT:") || line.StartsWith("DAWAT"))
                {
                    HandleInput(line);
                }
                else if (line.StartsWith("KUNG"))
                {
                    // Evaluate KUNG (...) then find PUNDOK{ ... } block and execute accordingly
                    i = HandleIf(commands, i, sb);
                }
                else if (line.StartsWith("ALANG SA"))
                {
                    i = HandleFor(commands, i, sb);
                }
                else if (line.StartsWith("SAMTANG")) // implemented while-like loop as SAMTANG (<expr>) PUNDOK{ }
                {
                    i = HandleWhile(commands, i, sb);
                }
                else if (line.Contains("="))
                {
                    HandleAssignment(line);
                }
                else if (IsIncrementDecrementStatement(line))
                {
                    HandleIncDecStatement(line);
                }
                else
                {
                    // ignore or unknown
                }
            }

            return sb.ToString();
        }

        // ----------------------------
        // Declarations & Assignments
        // ----------------------------
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
                    // For initial value evaluate expression if necessary
                    object valueObj = EvaluateExpressionOrToken(valueToken);
                    vars.Declare(name, type, valueObj);
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
            // Split on '=' but only top-level (outside quotes).
            var parts = strhelper.SplitTopLevel(line, '=');
            if (parts.Count < 2) throw new Exception("Invalid assignment syntax.");

            // RHS token is last part
            string rhsToken = parts[parts.Count - 1].Trim();

            object rhsValue = EvaluateExpressionOrToken(rhsToken);

            // Assign from right-to-left: for x = y = 4 -> assign y then x (both get evaluated RHS)
            for (int i = parts.Count - 2; i >= 0; i--)
            {
                string varName = parts[i].Trim();
                if (!vars.Exists(varName))
                    throw new Exception("Variable '" + varName + "' not declared.");
                vars.Assign(varName, rhsValue);
            }
        }

        // ----------------------------
        // Output
        // ----------------------------
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
                    {
                        if (val is bool)
                            result.Append((bool)val ? "OO" : "DILI");
                        else
                            result.Append(val.ToString());
                    }
                }
                else if (s.Length > 0)
                    result.Append(s);
            }

            return result.ToString();
        }

        // ----------------------------
        // Input
        // ----------------------------
        private void HandleInput(string line)
        {
            // Syntax: DAWAT: x, y  OR DAWAT: x
            int colon = line.IndexOf(':');
            string rest = colon >= 0 ? line.Substring(colon + 1).Trim() : line.Substring("DAWAT".Length).Trim();
            var varsList = rest.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
            if (varsList.Count == 0) throw new Exception("DAWAT requires variable list.");

            // Show input dialog - ask user for comma-separated values
            string prompt = "Input values for: " + string.Join(", ", varsList) + " (separate by comma)";
            string input = Interaction.InputBox(prompt, "DAWAT", "");

            if (input == null) input = "";

            var inputs = input.Split(',').Select(s => s.Trim()).ToList();

            if (inputs.Count < varsList.Count)
                throw new Exception("Not enough input values provided for DAWAT.");

            for (int i = 0; i < varsList.Count; i++)
            {
                string varName = varsList[i];
                if (!vars.Exists(varName)) throw new Exception("Variable '" + varName + "' not declared.");
                object valueObj = inputs[i];
                // try to parse numeric/boolean if applicable
                object parsed = TryParseLiteral(valueObj.ToString());
                vars.Assign(varName, parsed);
            }
        }

        private object TryParseLiteral(string token)
        {
            token = token.Trim();
            int iv;
            if (int.TryParse(token, out iv)) return iv;
            double dv;
            if (double.TryParse(token, out dv)) return dv;
            var up = token.ToUpperInvariant();
            if (up == "OO" || up == "TRUE") return true;
            if (up == "DILI" || up == "FALSE") return false;
            if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
                return token.Substring(1, token.Length - 2);
            return token;
        }

        // ----------------------------
        // Blocks helpers (unchanged except for reuse)
        // ----------------------------
        // Helper: find index of matching closing brace '}' from startIndex (where PUNDOK{ is)
        private int FindMatchingBlockEnd(List<string> commands, int startIndex)
        {
            // Start index is the line that contains PUNDOK{ (could be on same line as PUNDOK{)
            int depth = 0;
            for (int i = startIndex; i < commands.Count; i++)
            {
                string l = commands[i];
                if (l.Contains("PUNDOK{")) depth++;
                if (l.Trim() == "}") depth--;
                if (depth == 0) return i;
            }
            throw new Exception("Missing closing '}' for block starting at " + startIndex);
        }

        private int HandleIf(List<string> commands, int idx, StringBuilder sb)
        {
            // line is like: KUNG (<expr>) or KUNG (<expr>)   (there could be spaces)
            string line = commands[idx].Trim();
            // extract expression inside parentheses
            int p1 = line.IndexOf('(');
            int p2 = line.LastIndexOf(')');
            if (p1 == -1 || p2 == -1 || p2 <= p1) throw new Exception("Invalid KUNG syntax.");
            string condExpr = line.Substring(p1 + 1, p2 - p1 - 1).Trim();

            // find block after this line (expect next token contains PUNDOK{)
            int blockStart = idx + 1;
            if (blockStart >= commands.Count) throw new Exception("Missing block after KUNG.");

            if (!commands[blockStart].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after KUNG.");

            int blockEnd = FindMatchingBlockEnd(commands, blockStart);

            bool cond = EvaluateBooleanExpression(condExpr);

            if (cond)
            {
                // execute block commands between blockStart+1 .. blockEnd-1
                ExecuteBlock(commands.Skip(blockStart + 1).Take(blockEnd - blockStart - 1).ToList(), sb);
                // return index after blockEnd
                return blockEnd;
            }
            else
            {
                // skip this block; check for KUNG DILI (else if) or KUNG WALA (else)
                int nextIdx = blockEnd + 1;
                // check for KUNG DILI (<expr>) PUNDOK{...}
                if (nextIdx < commands.Count && commands[nextIdx].StartsWith("KUNG DILI"))
                {
                    // format: KUNG DILI (<expr>)
                    string line2 = commands[nextIdx];
                    int p1b = line2.IndexOf('(');
                    int p2b = line2.LastIndexOf(')');
                    if (p1b == -1 || p2b == -1 || p2b <= p1b) throw new Exception("Invalid KUNG DILI syntax.");
                    string cond2 = line2.Substring(p1b + 1, p2b - p1b - 1).Trim();

                    int blockStart2 = nextIdx + 1;
                    if (!commands[blockStart2].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after KUNG DILI.");
                    int blockEnd2 = FindMatchingBlockEnd(commands, blockStart2);

                    bool cond2val = EvaluateBooleanExpression(cond2);
                    if (cond2val)
                    {
                        ExecuteBlock(commands.Skip(blockStart2 + 1).Take(blockEnd2 - blockStart2 - 1).ToList(), sb);
                    }
                    else
                    {
                        // check for KUNG WALA following
                        int after = blockEnd2 + 1;
                        if (after < commands.Count && commands[after].StartsWith("KUNG WALA"))
                        {
                            int blockStart3 = after + 1;
                            if (!commands[blockStart3].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after KUNG WALA.");
                            int blockEnd3 = FindMatchingBlockEnd(commands, blockStart3);
                            ExecuteBlock(commands.Skip(blockStart3 + 1).Take(blockEnd3 - blockStart3 - 1).ToList(), sb);
                            return blockEnd3;
                        }
                        return blockEnd2;
                    }
                    return blockEnd2;
                }
                // else check for KUNG WALA
                if (nextIdx < commands.Count && commands[nextIdx].StartsWith("KUNG WALA"))
                {
                    int blockStart3 = nextIdx + 1;
                    if (!commands[blockStart3].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after KUNG WALA.");
                    int blockEnd3 = FindMatchingBlockEnd(commands, blockStart3);
                    ExecuteBlock(commands.Skip(blockStart3 + 1).Take(blockEnd3 - blockStart3 - 1).ToList(), sb);
                    return blockEnd3;
                }

                return blockEnd;
            }
        }

        private int HandleFor(List<string> commands, int idx, StringBuilder sb)
        {
            // ALANG SA (init, cond, update) PUNDOK{ ... }
            string line = commands[idx];
            int p1 = line.IndexOf('(');
            int p2 = line.LastIndexOf(')');
            if (p1 == -1 || p2 == -1 || p2 <= p1) throw new Exception("Invalid ALANG SA syntax.");
            string inner = line.Substring(p1 + 1, p2 - p1 - 1).Trim();
            // split top-level commas
            var parts = strhelper.SplitTopLevel(inner, ',').Select(s => s.Trim()).ToList();
            if (parts.Count != 3) throw new Exception("ALANG SA requires 3 parts: init, condition, update.");

            string init = parts[0];
            string condExpr = parts[1];
            string update = parts[2];

            int blockLine = idx + 1;
            if (!commands[blockLine].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after ALANG SA.");
            int blockEnd = FindMatchingBlockEnd(commands, blockLine);

            // execute init
            if (!string.IsNullOrWhiteSpace(init))
            {
                if (init.Contains("=")) HandleAssignment(init);
            }

            while (EvaluateBooleanExpression(condExpr))
            {
                ExecuteBlock(commands.Skip(blockLine + 1).Take(blockEnd - blockLine - 1).ToList(), sb);
                // perform update
                if (!string.IsNullOrWhiteSpace(update))
                {
                    if (update.Contains("=")) HandleAssignment(update);
                    else
                    {
                        // could be ctr++ style
                        EvaluateExpressionOrToken(update);
                    }
                }
            }

            return blockEnd;
        }

        private int HandleWhile(List<string> commands, int idx, StringBuilder sb)
        {
            // SAMTANG (<expr>) PUNDOK{ ... }
            string line = commands[idx];
            int p1 = line.IndexOf('(');
            int p2 = line.LastIndexOf(')');
            if (p1 == -1 || p2 == -1 || p2 <= p1) throw new Exception("Invalid SAMTANG syntax.");
            string condExpr = line.Substring(p1 + 1, p2 - p1 - 1).Trim();

            int blockLine = idx + 1;
            if (!commands[blockLine].Contains("PUNDOK{")) throw new Exception("Expected PUNDOK{ after SAMTANG.");
            int blockEnd = FindMatchingBlockEnd(commands, blockLine);

            while (EvaluateBooleanExpression(condExpr))
            {
                ExecuteBlock(commands.Skip(blockLine + 1).Take(blockEnd - blockLine - 1).ToList(), sb);
            }

            return blockEnd;
        }

        private void ExecuteBlock(List<string> blockCommands, StringBuilder sb)
        {
            // very simple: create a nested interpreter that shares same vars and strhelper
            var nested = new InterpreterBlockRunner(vars, strhelper);
            nested.Run(blockCommands, sb);
        }

        // ----------------------------
        // Expression evaluation
        // ----------------------------
        // Evaluate either a single token (variable, literal) or a complex expression
        private object EvaluateExpressionOrToken(string token)
        {
            token = token.Trim();

            // Handle standalone or simple increment/decrement forms: var++ , var--, ++var, --var
            var mPost = Regex.Match(token, @"^([A-Za-z_][A-Za-z0-9_]*)\s*(\+\+|--)$");
            var mPre = Regex.Match(token, @"^(\+\+|--)\s*([A-Za-z_][A-Za-z0-9_]*)$");
            if (mPost.Success)
            {
                string name = mPost.Groups[1].Value;
                string op = mPost.Groups[2].Value;
                if (!vars.Exists(name)) throw new Exception($"Variable '{name}' not declared.");
                var cur = vars.GetValue(name);
                if (!(cur is int) && !(cur is double)) throw new Exception("++/-- only supported on numeric variables.");
                // post: return current value but update variable
                if (cur is int)
                {
                    int prev = (int)cur;
                    int upd = (op == "++") ? prev + 1 : prev - 1;
                    vars.Assign(name, upd);
                    return prev;
                }
                else
                {
                    double prev = (double)cur;
                    double upd = (op == "++") ? prev + 1 : prev - 1;
                    vars.Assign(name, upd);
                    return prev;
                }
            }
            else if (mPre.Success)
            {
                string op = mPre.Groups[1].Value;
                string name = mPre.Groups[2].Value;
                if (!vars.Exists(name)) throw new Exception($"Variable '{name}' not declared.");
                var cur = vars.GetValue(name);
                if (!(cur is int) && !(cur is double)) throw new Exception("++/-- only supported on numeric variables.");
                if (cur is int)
                {
                    int val = (int)cur;
                    val = (op == "++") ? val + 1 : val - 1;
                    vars.Assign(name, val);
                    return val;
                }
                else
                {
                    double val = (double)cur;
                    val = (op == "++") ? val + 1 : val - 1;
                    vars.Assign(name, val);
                    return val;
                }
            }

            // If whole token is a declared variable name -> return its value
            if (vars.Exists(token))
                return vars.GetValue(token);

            // If token is a quoted literal
            if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
            {
                // string or single char
                string inner = token.Substring(1, token.Length - 2);
                // if wrapped in single quotes and length 1 maybe LETRA
                return inner;
            }

            // attempt to parse as integer or double
            int iv;
            if (int.TryParse(token, out iv)) return iv;
            double dv;
            if (double.TryParse(token, out dv)) return dv;

            // If token uses boolean literal names
            var up = token.ToUpperInvariant();
            if (up == "OO") return true;
            if (up == "DILI") return false;

            // Otherwise, pass to expression evaluator (covers arithmetic, logical, unary, ++/--)
            return EvaluateExpression(token);
        }

        private bool EvaluateBooleanExpression(string expr)
        {
            var val = EvaluateExpressionOrToken(expr);
            if (val is bool) return (bool)val;
            if (val is int) return ((int)val) != 0;
            if (val is double) return Math.Abs((double)val) > double.Epsilon;
            if (val is string) return !string.IsNullOrEmpty((string)val);
            return false;
        }

        // Basic expression evaluator — shunting yard + RPN eval
        internal object EvaluateExpression(string expr)
        {
            var tokens = TokenizeExpression(expr);
            var rpn = InfixToRpn(tokens);
            var val = EvaluateRpn(rpn);
            return val;
        }

        private List<string> TokenizeExpression(string expr)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < expr.Length)
            {
                char c = expr[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }

                // strings
                if (c == '"' || c == '\'')
                {
                    char qc = c;
                    int j = i + 1;
                    var sb = new StringBuilder();
                    while (j < expr.Length && expr[j] != qc) { sb.Append(expr[j]); j++; }
                    if (j >= expr.Length) throw new Exception("Unterminated string in expression.");
                    tokens.Add(qc + sb.ToString() + qc);
                    i = j + 1;
                    continue;
                }

                // multi-char operators: <=, >=, ==, <>, ++, -- 
                if (i + 1 < expr.Length)
                {
                    string two = expr.Substring(i, 2);
                    if (two == "<=" || two == ">=" || two == "==" || two == "<>" || two == "++" || two == "--")
                    {
                        tokens.Add(two);
                        i += 2; continue;
                    }
                }

                // single char operators and parentheses and comma and & (concatenate)
                if ("()+-*/%><&=,".IndexOf(c) >= 0)
                {
                    tokens.Add(c.ToString());
                    i++; continue;
                }

                // identifiers / keywords (UG O DILI etc) and numbers
                if (char.IsLetter(c) || c == '_')
                {
                    int j = i;
                    var sb = new StringBuilder();
                    while (j < expr.Length && (char.IsLetterOrDigit(expr[j]) || expr[j] == '_' || expr[j] == '.')) { sb.Append(expr[j]); j++; }
                    string word = sb.ToString();
                    // normalize common boolean/logical keywords to our internal tokens
                    string upWord = word.ToUpperInvariant();
                    if (upWord == "AND" || upWord == "&&") word = "UG";
                    else if (upWord == "OR" || upWord == "||") word = "O";
                    else if (upWord == "NOT") word = "DILI";
                    // also accept 'UG' 'O' 'DILI' already
                    tokens.Add(word);
                    i = j; continue;
                }

                // numbers
                if (char.IsDigit(c))
                {
                    int j = i;
                    var sb = new StringBuilder();
                    while (j < expr.Length && (char.IsDigit(expr[j]) || expr[j] == '.')) { sb.Append(expr[j]); j++; }
                    tokens.Add(sb.ToString());
                    i = j; continue;
                }

                // otherwise unrecognized char
                tokens.Add(c.ToString());
                i++;
            }

            return tokens;
        }

        // update precedence: add & for concatenation, DILI, UG, O already present
        private static readonly Dictionary<string, int> Precedence = new Dictionary<string, int>()
        {
            { "DILI", 8 }, // unary NOT
            { "u-", 8 },   // unary minus (we will push u- as token)
            { "++", 8 }, { "--", 8 },
            { "*", 7 }, { "/", 7 }, { "%", 7 },
            { "+", 6 }, { "-", 6 },
            { "&", 6 }, // string concatenation (same precedence as +)
            { ">", 5 }, { "<", 5 }, { ">=", 5 }, { "<=", 5 }, { "==", 5 }, { "<>", 5 },
            { "UG", 4 }, // AND
            { "O", 3 },  // OR
        };

        private List<string> InfixToRpn(List<string> tokens)
        {
            var output = new List<string>();
            var ops = new Stack<string>();

            string prev = null;
            foreach (var tk in tokens)
            {
                string t = tk;
                double num;
                if ((t.StartsWith("\"") && t.EndsWith("\"")) || (t.StartsWith("'") && t.EndsWith("'")))
                {
                    output.Add(t); // literal
                }
                else if (double.TryParse(t, out num))
                {
                    output.Add(t);
                }
                else if (vars.Exists(t) || IsBooleanLiteral(t))
                {
                    output.Add(t);
                }
                else if (t == "(")
                {
                    ops.Push(t);
                }
                else if (t == ")")
                {
                    while (ops.Count > 0 && ops.Peek() != "(") output.Add(ops.Pop());
                    if (ops.Count == 0) throw new Exception("Mismatched parentheses.");
                    ops.Pop(); // pop "("
                }
                else
                {
                    // handle unary + and -: if prev is null or an operator or '(' then treat + or - as unary
                    if ((t == "+" || t == "-") && (prev == null || prev == "(" || Precedence.ContainsKey(prev) || prev == "UG" || prev == "O" || prev == "DILI"))
                    {
                        // treat unary + as no-op, unary - as "u-"
                        if (t == "+") { /* skip as no-op */ }
                        else
                        { // unary minus -> use "u-"
                            ops.Push("u-");
                        }
                        prev = t; continue;
                    }

                    // map textual logical operators already done in tokenization but ensure uppercase representation for precedence lookup
                    string opKey = t.ToUpperInvariant();
                    if (opKey == "AND" || opKey == "&&") opKey = "UG";
                    if (opKey == "OR" || opKey == "||") opKey = "O";
                    if (opKey == "NOT") opKey = "DILI";
                    if (opKey == "UG" || opKey == "O" || opKey == "DILI")
                        t = opKey;

                    // standard operator: pop higher-or-equal precedence operators first
                    while (ops.Count > 0 && ops.Peek() != "(")
                    {
                        string top = ops.Peek();
                        int topPrec = Precedence.ContainsKey(top) ? Precedence[top] : 0;
                        int curPrec = Precedence.ContainsKey(t) ? Precedence[t] : 0;
                        if (topPrec >= curPrec)
                            output.Add(ops.Pop());
                        else break;
                    }
                    ops.Push(t);
                }
                prev = t;
            }

            while (ops.Count > 0)
            {
                var o = ops.Pop();
                if (o == "(" || o == ")") throw new Exception("Mismatched parentheses in expression.");
                output.Add(o);
            }

            return output;
        }

        private object EvaluateRpn(List<string> rpn)
        {
            var st = new Stack<object>();

            for (int i = 0; i < rpn.Count; i++)
            {
                string tk = rpn[i];

                // literals
                if ((tk.StartsWith("\"") && tk.EndsWith("\"")) || (tk.StartsWith("'") && tk.EndsWith("'")))
                {
                    st.Push(tk.Substring(1, tk.Length - 2));
                    continue;
                }

                double num;
                if (double.TryParse(tk, out num))
                {
                    // integer or double: keep double if contains decimal
                    if (tk.Contains(".")) st.Push(num);
                    else st.Push((int)num);
                    continue;
                }

                if (vars.Exists(tk))
                {
                    var v = vars.GetValue(tk);
                    st.Push(v);
                    continue;
                }

                if (IsBooleanLiteral(tk))
                {
                    st.Push(tk.ToUpperInvariant() == "OO");
                    continue;
                }

                // unary u- (we used u- as unary minus)
                if (tk == "u-")
                {
                    var a = st.Pop();
                    if (a is int) st.Push(-(int)a);
                    else if (a is double) st.Push(-(double)a);
                    else throw new Exception("Invalid operand for unary -");
                    continue;
                }

                // unary NOT
                if (tk == "DILI")
                {
                    var a = st.Pop();
                    bool av = ConvertToBool(a);
                    st.Push(!av);
                    continue;
                }

                // ++ or -- operator applied to top operand if numeric
                if (tk == "++")
                {
                    var a = st.Pop();
                    if (a is int)
                    {
                        int v = (int)a;
                        v = v + 1;
                        st.Push(v);
                    }
                    else if (a is double)
                    {
                        double v = (double)a;
                        v = v + 1;
                        st.Push(v);
                    }
                    else throw new Exception("Invalid operand for ++/--");
                    continue;
                }

                if (tk == "--")
                {
                    var a = st.Pop();
                    if (a is int)
                    {
                        int v = (int)a;
                        v = v - 1;
                        st.Push(v);
                    }
                    else if (a is double)
                    {
                        double v = (double)a;
                        v = v - 1;
                        st.Push(v);
                    }
                    else throw new Exception("Invalid operand for --");
                    continue;
                }



                // binary arithmetic
                if (tk == "+" || tk == "-" || tk == "*" || tk == "/" || tk == "%")
                {
                    var b = st.Pop(); var a = st.Pop();
                    object r = EvalArithmetic(a, b, tk);
                    st.Push(r);
                    continue;
                }

                // concatenation operator '&' -> string concat (coerce to string)
                if (tk == "&")
                {
                    var b = st.Pop(); var a = st.Pop();
                    string sa = a?.ToString() ?? "";
                    string sb2 = b?.ToString() ?? "";
                    st.Push(sa + sb2);
                    continue;
                }

                if (tk == ">" || tk == "<" || tk == ">=" || tk == "<=" || tk == "==" || tk == "<>")
                {
                    var b = st.Pop(); var a = st.Pop();
                    bool cmp = EvalComparison(a, b, tk);
                    st.Push(cmp);
                    continue;
                }

                if (tk == "UG" || tk == "O")
                {
                    var b = st.Pop(); var a = st.Pop();
                    bool av = ConvertToBool(a);
                    bool bv = ConvertToBool(b);
                    bool res = (tk == "UG") ? (av && bv) : (av || bv);
                    st.Push(res);
                    continue;
                }

                // unexpected token
                throw new Exception("Unknown token in RPN: " + tk);
            }

            if (st.Count != 1) throw new Exception("Invalid expression evaluation.");
            return st.Pop();
        }

        private object EvalArithmetic(object a, object b, string op)
        {
            // convert to double if any operand is double
            if (a is string || b is string)
                throw new Exception("Cannot do arithmetic on strings.");

            bool aIsDouble = a is double || b is double;
            double da = Convert.ToDouble(a);
            double db = Convert.ToDouble(b);

            switch (op)
            {
                case "+": return (aIsDouble ? (object)(da + db) : (object)((int)(Convert.ToInt32(a) + Convert.ToInt32(b))));
                case "-": return (aIsDouble ? (object)(da - db) : (object)((int)(Convert.ToInt32(a) - Convert.ToInt32(b))));
                case "*": return (aIsDouble ? (object)(da * db) : (object)((int)(Convert.ToInt32(a) * Convert.ToInt32(b))));
                case "/":
                    if (Math.Abs(db) < double.Epsilon) throw new Exception("Division by zero.");
                    return (aIsDouble ? (object)(da / db) : (object)((int)(Convert.ToInt32(a) / Convert.ToInt32(b))));
                case "%":
                    if (Math.Abs(db) < double.Epsilon) throw new Exception("Division by zero.");
                    return (aIsDouble ? (object)(da % db) : (object)((int)(Convert.ToInt32(a) % Convert.ToInt32(b))));
                default: throw new Exception("Unknown arithmetic operator " + op);
            }
        }

        private bool EvalComparison(object a, object b, string op)
        {
            // If numeric compare numerically, else string compare
            if ((a is int || a is double) && (b is int || b is double))
            {
                double da = Convert.ToDouble(a), db = Convert.ToDouble(b);
                switch (op)
                {
                    case ">": return da > db;
                    case "<": return da < db;
                    case ">=": return da >= db;
                    case "<=": return da <= db;
                    case "==": return Math.Abs(da - db) < 1e-9;
                    case "<>": return Math.Abs(da - db) >= 1e-9;
                }
            }
            else
            {
                string sa = a?.ToString() ?? "";
                string sb = b?.ToString() ?? "";
                int cmp = string.Compare(sa, sb, StringComparison.Ordinal);
                switch (op)
                {
                    case ">": return cmp > 0;
                    case "<": return cmp < 0;
                    case ">=": return cmp >= 0;
                    case "<=": return cmp <= 0;
                    case "==": return cmp == 0;
                    case "<>": return cmp != 0;
                }
            }
            return false;
        }

        private bool ConvertToBool(object o)
        {
            if (o is bool) return (bool)o;
            if (o is int) return (int)o != 0;
            if (o is double) return Math.Abs((double)o) > double.Epsilon;
            if (o is string) return !string.IsNullOrEmpty((string)o);
            return false;
        }

        private bool IsBooleanLiteral(string token)
        {
            var up = token.ToUpperInvariant();
            return up == "OO" || up == "DILI" || up == "TRUE" || up == "FALSE";
        }

        // ----------------------------
        // Increment/Decrement statement handler (standalone like x++ or ++x or x-- or --x)
        // ----------------------------
        private bool IsIncrementDecrementStatement(string line)
        {
            var t = line.Trim();
            if (Regex.IsMatch(t, @"^[A-Za-z_][A-Za-z0-9_]*\s*(\+\+|--)$")) return true;
            if (Regex.IsMatch(t, @"^(\+\+|--)\s*[A-Za-z_][A-Za-z0-9_]*$")) return true;
            return false;
        }

        private void HandleIncDecStatement(string line)
        {
            var t = line.Trim();
            var mPost = Regex.Match(t, @"^([A-Za-z_][A-Za-z0-9_]*)\s*(\+\+|--)$");
            var mPre = Regex.Match(t, @"^(\+\+|--)\s*([A-Za-z_][A-Za-z0-9_]*)$");
            if (mPost.Success)
            {
                string name = mPost.Groups[1].Value;
                string op = mPost.Groups[2].Value;
                if (!vars.Exists(name)) throw new Exception($"Variable '{name}' not declared.");
                var cur = vars.GetValue(name);
                if (cur is int)
                {
                    int v = (int)cur;
                    v = (op == "++") ? v + 1 : v - 1;
                    vars.Assign(name, v);
                    return;
                }
                if (cur is double)
                {
                    double v = (double)cur;
                    v = (op == "++") ? v + 1 : v - 1;
                    vars.Assign(name, v);
                    return;
                }
                throw new Exception("++/-- only supported on numeric variables.");
            }
            else if (mPre.Success)
            {
                string op = mPre.Groups[1].Value;
                string name = mPre.Groups[2].Value;
                if (!vars.Exists(name)) throw new Exception($"Variable '{name}' not declared.");
                var cur = vars.GetValue(name);
                if (cur is int)
                {
                    int v = (int)cur;
                    v = (op == "++") ? v + 1 : v - 1;
                    vars.Assign(name, v);
                    return;
                }
                if (cur is double)
                {
                    double v = (double)cur;
                    v = (op == "++") ? v + 1 : v - 1;
                    vars.Assign(name, v);
                    return;
                }
                throw new Exception("++/-- only supported on numeric variables.");
            }

            throw new Exception("Invalid increment/decrement statement.");
        }
    }

    // Helper runner class to execute blocks with shared VariableTable and StringHelper
    internal class InterpreterBlockRunner
    {
        private VariableTable vars;
        private StringHelper strhelper;

        public InterpreterBlockRunner(VariableTable varsRef, StringHelper sh)
        {
            vars = varsRef;
            strhelper = sh;
        }

        // Runs commands in block; uses same logic as outer Interpreter but simplified: we create a lightweight executor
        public void Run(List<string> commands, StringBuilder sb)
        {
            var interpreter = new BlockExecutor(vars, strhelper, sb);
            interpreter.ExecuteBlock(commands);
        }
    }

    internal class BlockExecutor
    {
        private VariableTable vars;
        private StringHelper strhelper;
        private StringBuilder sb;

        public BlockExecutor(VariableTable varsRef, StringHelper sh, StringBuilder sbRef)
        {
            vars = varsRef;
            strhelper = sh;
            sb = sbRef;
        }

        public void ExecuteBlock(List<string> commands)
        {
            // reuse many methods by creating a small interpreter-like instance — but for brevity call the main Interpreter logic through reflection-like reimplementation
            var interpreter = new InterpreterExecutionHelper(vars, strhelper, sb);
            interpreter.ExecuteList(commands);
        }
    }

    // Minimal helper that exposes a small subset needed for blocks (assignment, declaration, ipakita, input, expression eval)
    internal class InterpreterExecutionHelper
    {
        private VariableTable vars;
        private StringHelper strhelper;
        private StringBuilder sb;

        public InterpreterExecutionHelper(VariableTable varsRef, StringHelper sh, StringBuilder sbRef)
        {
            vars = varsRef;
            strhelper = sh;
            sb = sbRef;
        }

        public void ExecuteList(List<string> commands)
        {
            foreach (var line in commands)
            {
                var l = line.Trim();
                if (l.StartsWith("MUGNA"))
                {
                    // reuse Interpreter.Declare logic by constructing a small temporary parse (same as main)
                    var parts = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) throw new Exception("Invalid declaration syntax.");
                    string type = parts[1].Trim();
                    string rest = l.Substring(l.IndexOf(type) + type.Length).Trim();
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
                            object valueObj = EvaluateExpressionOrToken(valueToken);
                            vars.Declare(name, type, valueObj);
                        }
                        else
                        {
                            vars.Declare(decl, type, null);
                        }
                    }
                }
                else if (l.StartsWith("IPAKITA"))
                {
                    int colonIndex = l.IndexOf(':');
                    if (colonIndex == -1) throw new Exception("Invalid IPAKITA syntax.");
                    string expr = l.Substring(colonIndex + 1).Trim();
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
                            if (val is bool) result.Append((bool)val ? "OO" : "DILI");
                            else result.Append(val?.ToString());
                        }
                        else if (s.Length > 0)
                            result.Append(s);
                    }
                    sb.AppendLine(result.ToString());
                }
                else if (l.StartsWith("DAWAT"))
                {
                    int colon = l.IndexOf(':');
                    string rest = colon >= 0 ? l.Substring(colon + 1).Trim() : l.Substring("DAWAT".Length).Trim();
                    var varsList = rest.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
                    string prompt = "Input values for: " + string.Join(", ", varsList) + " (separate by comma)";
                    string input = Interaction.InputBox(prompt, "DAWAT", "");
                    if (input == null) input = "";
                    var inputs = input.Split(',').Select(s => s.Trim()).ToList();
                    if (inputs.Count < varsList.Count)
                        throw new Exception("Not enough input values provided for DAWAT.");
                    for (int i = 0; i < varsList.Count; i++)
                    {
                        string varName = varsList[i];
                        if (!vars.Exists(varName)) throw new Exception("Variable '" + varName + "' not declared.");
                        object valueObj = inputs[i];
                        vars.Assign(varName, valueObj);
                    }
                }
                else if (l.Contains("="))
                {
                    var parts = strhelper.SplitTopLevel(l, '=');
                    if (parts.Count < 2) throw new Exception("Invalid assignment syntax.");
                    string rhsToken = parts[parts.Count - 1].Trim();
                    object rhsValue = EvaluateExpressionOrToken(rhsToken);
                    for (int i = parts.Count - 2; i >= 0; i--)
                    {
                        string varName = parts[i].Trim();
                        if (!vars.Exists(varName)) throw new Exception("Variable '" + varName + "' not declared.");
                        vars.Assign(varName, rhsValue);
                    }
                }
                else if (Regex.IsMatch(l, @"^[A-Za-z_][A-Za-z0-9_]*\s*(\+\+|--)$") || Regex.IsMatch(l, @"^(\+\+|--)\s*[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    // handle simple inc/dec in blocks
                    var mPost = Regex.Match(l, @"^([A-Za-z_][A-Za-z0-9_]*)\s*(\+\+|--)$");
                    var mPre = Regex.Match(l, @"^(\+\+|--)\s*([A-Za-z_][A-Za-z0-9_]*)$");
                    if (mPost.Success)
                    {
                        string name = mPost.Groups[1].Value;
                        string op = mPost.Groups[2].Value;
                        var cur = vars.GetValue(name);
                        if (cur is int) vars.Assign(name, (op == "++") ? (int)cur + 1 : (int)cur - 1);
                        else if (cur is double) vars.Assign(name, (op == "++") ? (double)cur + 1 : (double)cur - 1);
                        else throw new Exception("++/-- only supported on numeric variables.");
                    }
                    else if (mPre.Success)
                    {
                        string op = mPre.Groups[1].Value;
                        string name = mPre.Groups[2].Value;
                        var cur = vars.GetValue(name);
                        if (cur is int) vars.Assign(name, (op == "++") ? (int)cur + 1 : (int)cur - 1);
                        else if (cur is double) vars.Assign(name, (op == "++") ? (double)cur + 1 : (double)cur - 1);
                        else throw new Exception("++/-- only supported on numeric variables.");
                    }
                }
                else
                {
                    // ignore others (control flow should not be nested here)
                }
            }
        }

        // Minimal EvaluateExpressionOrToken for block executor (duplicate logic)
        private object EvaluateExpressionOrToken(string token)
        {
            token = token.Trim();
            if (vars.Exists(token)) return vars.GetValue(token);
            if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
            {
                string inner = token.Substring(1, token.Length - 2);
                return inner;
            }
            int iv;
            if (int.TryParse(token, out iv)) return iv;
            double dv;
            if (double.TryParse(token, out dv)) return dv;
            var up = token.ToUpperInvariant();
            if (up == "OO") return true;
            if (up == "DILI") return false;
            // fallback: try to evaluate expression using a fresh Interpreter instance
            var outer = new Interpreter();
            return outer.EvaluateExpression(token);
        }
    }
}
