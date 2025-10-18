namespace bisayaplusplus_interpreter
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtboxCode = new System.Windows.Forms.TextBox();
            this.btnInterpret = new System.Windows.Forms.Button();
            this.txtboxOutput = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // txtboxCode
            // 
            this.txtboxCode.Location = new System.Drawing.Point(21, 55);
            this.txtboxCode.Multiline = true;
            this.txtboxCode.Name = "txtboxCode";
            this.txtboxCode.Size = new System.Drawing.Size(593, 279);
            this.txtboxCode.TabIndex = 0;
            // 
            // btnInterpret
            // 
            this.btnInterpret.Location = new System.Drawing.Point(270, 12);
            this.btnInterpret.Name = "btnInterpret";
            this.btnInterpret.Size = new System.Drawing.Size(94, 37);
            this.btnInterpret.TabIndex = 1;
            this.btnInterpret.Text = "Interpret";
            this.btnInterpret.UseVisualStyleBackColor = true;
            this.btnInterpret.Click += new System.EventHandler(this.btnInterpret_Click);
            // 
            // txtboxOutput
            // 
            this.txtboxOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtboxOutput.Location = new System.Drawing.Point(21, 350);
            this.txtboxOutput.Name = "txtboxOutput";
            this.txtboxOutput.Size = new System.Drawing.Size(593, 237);
            this.txtboxOutput.TabIndex = 3;
            this.txtboxOutput.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(637, 635);
            this.Controls.Add(this.txtboxOutput);
            this.Controls.Add(this.btnInterpret);
            this.Controls.Add(this.txtboxCode);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtboxCode;
        private System.Windows.Forms.Button btnInterpret;
        private System.Windows.Forms.RichTextBox txtboxOutput;
    }
}

