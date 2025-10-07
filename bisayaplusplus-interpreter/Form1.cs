using bisayaplusplus_interpreter.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace bisayaplusplus_interpreter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnInterpret_Click(object sender, EventArgs e)
        {
            try
            {
                var lexer = new Lexer();
                var tokens = lexer.Tokenize(txtboxCode.Text);

                var parser = new Parser();
                parser.ParseStructure(tokens);

                var interpreter = new Interpreter();
                string result = interpreter.Execute(parser.Commands);

                txtboxOutput.Text = result;
            }
            catch (Exception ex)
            {
                txtboxOutput.Text = "Error: " + ex.Message;
            }
        }
    }
}
