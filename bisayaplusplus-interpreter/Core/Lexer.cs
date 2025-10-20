using System;
using System.Collections.Generic;

namespace bisayaplusplus_interpreter.Core
{
    public class Lexer
    {
        public List<string> Tokenize(string code)
        {
            var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("@@")) continue;

                int commentIndex = line.IndexOf("@@");
                if (commentIndex != -1) line = line.Substring(0, commentIndex).Trim();

                if (!string.IsNullOrWhiteSpace(line)) tokens.Add(line);
            }

            return tokens;
        }
    }
}
