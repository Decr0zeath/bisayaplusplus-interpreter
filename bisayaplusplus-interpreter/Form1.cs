using bisayaplusplus_interpreter.Core;
using bisayaplusplus_interpreter.Utils;
using System;
using System.Windows.Forms;

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
                var strhelper = new StringHelper();

                var lexer = new Lexer();
                var tokens = lexer.Tokenize(strhelper.Clean(txtboxCode.Text));

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
