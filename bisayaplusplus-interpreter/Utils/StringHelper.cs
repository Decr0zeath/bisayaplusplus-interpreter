using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Utils
{
    public class StringHelper
    {
        public string Clean(string input)
        {
            if (input == null) return "";
            return input.Replace("\r", "").Trim();
        }

        public List<string> SplitExpression(string expr)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                // Toggle inside quotes
                if ((c == '"' || c == '\''))
                {
                    if (inQuotes && c == quoteChar)
                        inQuotes = false;
                    else if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                }

                // Handle escape sequence like [&]
                if (!inQuotes && c == '[' && i + 2 < expr.Length && expr[i + 2] == ']')
                {
                    string code = expr.Substring(i, 3); // e.g. "[&]" or "[]"
                    current.Append(code);
                    i += 2;
                    continue;
                }

                // Split only on & that are outside quotes and not part of [..]
                if (c == '&' && !inQuotes)
                {
                    string token = current.ToString().Trim();
                    if (token.Length > 0)
                        parts.Add(token);
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parts.Add(current.ToString().Trim());

            return parts;
        }

        // Generic splitter for a single character delimiter outside of quotes
        public List<string> SplitTopLevel(string input, char delimiter)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if ((c == '"' || c == '\''))
                {
                    if (inQuotes && c == quoteChar)
                        inQuotes = false;
                    else if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                }

                if (c == delimiter && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            parts.Add(current.ToString());
            return parts;
        }
    }
}
