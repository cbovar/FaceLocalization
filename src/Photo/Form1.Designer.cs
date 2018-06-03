namespace Photo
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.divisorUpDown = new System.Windows.Forms.NumericUpDown();
            this.divisorLabel = new System.Windows.Forms.Label();
            this.nmsLabel = new System.Windows.Forms.Label();
            this.nmsUpDown = new System.Windows.Forms.NumericUpDown();
            this.probaLabel = new System.Windows.Forms.Label();
            this.minProbaUpDown = new System.Windows.Forms.NumericUpDown();
            this.dumpBoundingBoxButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.divisorUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmsUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minProbaUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(840, 503);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 526);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Filename:";
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.Location = new System.Drawing.Point(300, 550);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Evalutate";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.evaluate_Click);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBox1.Location = new System.Drawing.Point(87, 521);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(207, 20);
            this.textBox1.TabIndex = 8;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button1.Location = new System.Drawing.Point(300, 521);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "Browse";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // divisorUpDown
            // 
            this.divisorUpDown.Location = new System.Drawing.Point(447, 521);
            this.divisorUpDown.Name = "divisorUpDown";
            this.divisorUpDown.Size = new System.Drawing.Size(120, 20);
            this.divisorUpDown.TabIndex = 12;
            this.divisorUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // divisorLabel
            // 
            this.divisorLabel.AutoSize = true;
            this.divisorLabel.Location = new System.Drawing.Point(399, 524);
            this.divisorLabel.Name = "divisorLabel";
            this.divisorLabel.Size = new System.Drawing.Size(42, 13);
            this.divisorLabel.TabIndex = 13;
            this.divisorLabel.Text = "Divisor:";
            // 
            // nmsLabel
            // 
            this.nmsLabel.AutoSize = true;
            this.nmsLabel.Location = new System.Drawing.Point(399, 554);
            this.nmsLabel.Name = "nmsLabel";
            this.nmsLabel.Size = new System.Drawing.Size(34, 13);
            this.nmsLabel.TabIndex = 15;
            this.nmsLabel.Text = "NMS:";
            // 
            // nmsUpDown
            // 
            this.nmsUpDown.DecimalPlaces = 1;
            this.nmsUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nmsUpDown.Location = new System.Drawing.Point(447, 551);
            this.nmsUpDown.Name = "nmsUpDown";
            this.nmsUpDown.Size = new System.Drawing.Size(120, 20);
            this.nmsUpDown.TabIndex = 14;
            this.nmsUpDown.Value = new decimal(new int[] {
            970,
            0,
            0,
            65536});
            // 
            // probaLabel
            // 
            this.probaLabel.AutoSize = true;
            this.probaLabel.Location = new System.Drawing.Point(586, 526);
            this.probaLabel.Name = "probaLabel";
            this.probaLabel.Size = new System.Drawing.Size(57, 13);
            this.probaLabel.TabIndex = 17;
            this.probaLabel.Text = "Proba min:";
            // 
            // minProbaUpDown
            // 
            this.minProbaUpDown.DecimalPlaces = 1;
            this.minProbaUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.minProbaUpDown.Location = new System.Drawing.Point(649, 521);
            this.minProbaUpDown.Name = "minProbaUpDown";
            this.minProbaUpDown.Size = new System.Drawing.Size(120, 20);
            this.minProbaUpDown.TabIndex = 16;
            this.minProbaUpDown.Value = new decimal(new int[] {
            97,
            0,
            0,
            0});
            // 
            // dumpBoundingBoxButton
            // 
            this.dumpBoundingBoxButton.Location = new System.Drawing.Point(589, 547);
            this.dumpBoundingBoxButton.Name = "dumpBoundingBoxButton";
            this.dumpBoundingBoxButton.Size = new System.Drawing.Size(180, 23);
            this.dumpBoundingBoxButton.TabIndex = 18;
            this.dumpBoundingBoxButton.Text = "Dump bounding boxes";
            this.dumpBoundingBoxButton.UseVisualStyleBackColor = true;
            this.dumpBoundingBoxButton.Click += new System.EventHandler(this.dumpBoundingBox_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 576);
            this.Controls.Add(this.dumpBoundingBoxButton);
            this.Controls.Add(this.probaLabel);
            this.Controls.Add(this.minProbaUpDown);
            this.Controls.Add(this.nmsLabel);
            this.Controls.Add(this.nmsUpDown);
            this.Controls.Add(this.divisorLabel);
            this.Controls.Add(this.divisorUpDown);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form1";
            this.Text = "Photo";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.divisorUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmsUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minProbaUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.NumericUpDown divisorUpDown;
        private System.Windows.Forms.Label divisorLabel;
        private System.Windows.Forms.Label nmsLabel;
        private System.Windows.Forms.NumericUpDown nmsUpDown;
        private System.Windows.Forms.Label probaLabel;
        private System.Windows.Forms.NumericUpDown minProbaUpDown;
        private System.Windows.Forms.Button dumpBoundingBoxButton;
    }
}

