namespace PedestrianDetection_HOG_v3
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
            this.lblFile = new System.Windows.Forms.Label();
            this.txtFile = new System.Windows.Forms.TextBox();
            this.ibImage = new System.Windows.Forms.PictureBox();
            this.btnFile = new System.Windows.Forms.Button();
            this.ofdFile = new System.Windows.Forms.OpenFileDialog();
            this.btnProcess = new System.Windows.Forms.Button();
            this.txt1 = new System.Windows.Forms.TextBox();
            this.txt2 = new System.Windows.Forms.TextBox();
            this.lblUV = new System.Windows.Forms.Label();
            this.numScale = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numThreshold = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.numCompensation = new System.Windows.Forms.NumericUpDown();
            this.lblInfor = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ibImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCompensation)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(12, 9);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(59, 13);
            this.lblFile.TabIndex = 0;
            this.lblFile.Text = "Choose file";
            // 
            // txtFile
            // 
            this.txtFile.Location = new System.Drawing.Point(75, 6);
            this.txtFile.Name = "txtFile";
            this.txtFile.Size = new System.Drawing.Size(606, 20);
            this.txtFile.TabIndex = 1;
            this.txtFile.TextChanged += new System.EventHandler(this.txtFile_TextChanged);
            // 
            // ibImage
            // 
            this.ibImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ibImage.Location = new System.Drawing.Point(12, 32);
            this.ibImage.Name = "ibImage";
            this.ibImage.Size = new System.Drawing.Size(1121, 750);
            this.ibImage.TabIndex = 2;
            this.ibImage.TabStop = false;
            // 
            // btnFile
            // 
            this.btnFile.Location = new System.Drawing.Point(687, 6);
            this.btnFile.Name = "btnFile";
            this.btnFile.Size = new System.Drawing.Size(28, 21);
            this.btnFile.TabIndex = 3;
            this.btnFile.Text = "...";
            this.btnFile.UseVisualStyleBackColor = true;
            this.btnFile.Click += new System.EventHandler(this.btnFile_Click);
            // 
            // ofdFile
            // 
            this.ofdFile.FileName = "openFileDialog1";
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(1387, 7);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(75, 21);
            this.btnProcess.TabIndex = 4;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // txt1
            // 
            this.txt1.Location = new System.Drawing.Point(1139, 32);
            this.txt1.Multiline = true;
            this.txt1.Name = "txt1";
            this.txt1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt1.Size = new System.Drawing.Size(159, 680);
            this.txt1.TabIndex = 5;
            // 
            // txt2
            // 
            this.txt2.Location = new System.Drawing.Point(1304, 32);
            this.txt2.Multiline = true;
            this.txt2.Name = "txt2";
            this.txt2.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt2.Size = new System.Drawing.Size(158, 680);
            this.txt2.TabIndex = 6;
            // 
            // lblUV
            // 
            this.lblUV.AutoSize = true;
            this.lblUV.Location = new System.Drawing.Point(1016, 9);
            this.lblUV.Name = "lblUV";
            this.lblUV.Size = new System.Drawing.Size(32, 13);
            this.lblUV.TabIndex = 7;
            this.lblUV.Text = "scale";
            // 
            // numScale
            // 
            this.numScale.DecimalPlaces = 2;
            this.numScale.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numScale.Location = new System.Drawing.Point(1054, 7);
            this.numScale.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numScale.Name = "numScale";
            this.numScale.Size = new System.Drawing.Size(60, 20);
            this.numScale.TabIndex = 8;
            this.numScale.Value = new decimal(new int[] {
            12,
            0,
            0,
            65536});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1120, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "threshold";
            // 
            // numThreshold
            // 
            this.numThreshold.DecimalPlaces = 2;
            this.numThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numThreshold.Location = new System.Drawing.Point(1176, 8);
            this.numThreshold.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numThreshold.Name = "numThreshold";
            this.numThreshold.Size = new System.Drawing.Size(60, 20);
            this.numThreshold.TabIndex = 10;
            this.numThreshold.Value = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1242, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "compensation";
            // 
            // numCompensation
            // 
            this.numCompensation.DecimalPlaces = 2;
            this.numCompensation.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numCompensation.Location = new System.Drawing.Point(1321, 7);
            this.numCompensation.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCompensation.Name = "numCompensation";
            this.numCompensation.Size = new System.Drawing.Size(60, 20);
            this.numCompensation.TabIndex = 12;
            // 
            // lblInfor
            // 
            this.lblInfor.AutoSize = true;
            this.lblInfor.Location = new System.Drawing.Point(721, 10);
            this.lblInfor.Name = "lblInfor";
            this.lblInfor.Size = new System.Drawing.Size(28, 13);
            this.lblInfor.TabIndex = 13;
            this.lblInfor.Text = "Infor";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1474, 794);
            this.Controls.Add(this.lblInfor);
            this.Controls.Add(this.numCompensation);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numThreshold);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numScale);
            this.Controls.Add(this.lblUV);
            this.Controls.Add(this.txt2);
            this.Controls.Add(this.txt1);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.btnFile);
            this.Controls.Add(this.ibImage);
            this.Controls.Add(this.txtFile);
            this.Controls.Add(this.lblFile);
            this.Name = "Form1";
            this.Text = "Pedestrian Dectetion Example";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.ibImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCompensation)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.PictureBox ibImage;
        private System.Windows.Forms.Button btnFile;
        private System.Windows.Forms.OpenFileDialog ofdFile;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.TextBox txt1;
        private System.Windows.Forms.TextBox txt2;
        private System.Windows.Forms.Label lblUV;
        private System.Windows.Forms.NumericUpDown numScale;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numThreshold;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numCompensation;
        private System.Windows.Forms.Label lblInfor;
    }
}

