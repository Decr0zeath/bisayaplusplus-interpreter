using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace bisayaplusplus_interpreter.Core
{
    public class Parser
    {
        private RichTextBox _txtboxparsedComm;

        public Parser(RichTextBox outputBox)
        {
            _txtboxparsedComm = outputBox;
        }


        public List<string> Commands { get; private set; }

        public bool ParseStructure(List<string> tokens)
        {
            int start = tokens.FindIndex(l => l == "SUGOD");
            int end = tokens.FindIndex(l => l == "KATAPUSAN");

            int sugodCount = tokens.Count(l => l == "SUGOD");
            int katapusanCount = tokens.Count(l => l == "KATAPUSAN");

            if (sugodCount > 1 || katapusanCount > 1)
                throw new Exception("Error: Multiple SUGOD or KATAPUSAN found.");

            if (start == -1 || end == -1 || end <= start) 
                throw new Exception("Error: Missing or misplaced SUGOD / KATAPUSAN.");

            Commands = tokens.Skip(start + 1).Take(end - start - 1).ToList();

            _txtboxparsedComm.Text = string.Join(Environment.NewLine, Commands);

            return true;
        }
    }
}
