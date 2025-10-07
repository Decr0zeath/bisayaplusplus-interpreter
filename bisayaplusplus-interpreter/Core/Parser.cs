using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bisayaplusplus_interpreter.Core
{
    public class Parser
    {
        public List<string> Commands { get; private set; }

        public bool ParseStructure(List<string> tokens)
        {
            int start = tokens.FindIndex(l => l == "SUGOD");
            int end = tokens.FindIndex(l => l == "KATAPUSAN");

            if (start == -1 || end == -1 || end <= start)
                throw new Exception("Error: Missing or misplaced SUGOD / KATAPUSAN.");

            Commands = tokens.Skip(start + 1).Take(end - start - 1).ToList();
            return true;
        }
    }
}
