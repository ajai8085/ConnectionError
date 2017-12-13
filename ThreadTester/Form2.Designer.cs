namespace ThreadTester
{
    partial class Form2
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
            if(disposing && (components != null))
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
            this.btnReal = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnReal
            // 
            this.btnReal.Location = new System.Drawing.Point(152, 172);
            this.btnReal.Name = "btnReal";
            this.btnReal.Size = new System.Drawing.Size(131, 23);
            this.btnReal.TabIndex = 5;
            this.btnReal.Text = "real - open conn";
            this.btnReal.UseVisualStyleBackColor = true;
            this.btnReal.Click += new System.EventHandler(this.btnReal_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(435, 367);
            this.Controls.Add(this.btnReal);
            this.Name = "Form2";
            this.Text = "Form2";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnReal;
    }
}