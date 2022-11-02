namespace WindowsFormsApp1
{
    partial class FrmMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.chk_Test = new System.Windows.Forms.CheckBox();
            this.txt_Result = new System.Windows.Forms.TextBox();
            this.btn_Test = new System.Windows.Forms.Button();
            this.btn_modify = new System.Windows.Forms.Button();
            this.btn_Result = new System.Windows.Forms.Button();
            this.txt_OCR = new System.Windows.Forms.TextBox();
            this.btn_OCR = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chk_Test
            // 
            this.chk_Test.AutoSize = true;
            this.chk_Test.Location = new System.Drawing.Point(8, 11);
            this.chk_Test.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chk_Test.Name = "chk_Test";
            this.chk_Test.Size = new System.Drawing.Size(108, 16);
            this.chk_Test.TabIndex = 1;
            this.chk_Test.Text = "Barcode Parse";
            this.chk_Test.UseVisualStyleBackColor = true;
            // 
            // txt_Result
            // 
            this.txt_Result.Location = new System.Drawing.Point(131, 9);
            this.txt_Result.Multiline = true;
            this.txt_Result.Name = "txt_Result";
            this.txt_Result.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt_Result.Size = new System.Drawing.Size(865, 402);
            this.txt_Result.TabIndex = 2;
            // 
            // btn_Test
            // 
            this.btn_Test.Location = new System.Drawing.Point(26, 52);
            this.btn_Test.Name = "btn_Test";
            this.btn_Test.Size = new System.Drawing.Size(75, 23);
            this.btn_Test.TabIndex = 3;
            this.btn_Test.Text = "Start";
            this.btn_Test.UseVisualStyleBackColor = true;
            this.btn_Test.Click += new System.EventHandler(this.btn_Test_Click);
            // 
            // btn_modify
            // 
            this.btn_modify.Location = new System.Drawing.Point(26, 81);
            this.btn_modify.Name = "btn_modify";
            this.btn_modify.Size = new System.Drawing.Size(75, 23);
            this.btn_modify.TabIndex = 4;
            this.btn_modify.Text = "Modify";
            this.btn_modify.UseVisualStyleBackColor = true;
            this.btn_modify.Click += new System.EventHandler(this.btn_modify_Click);
            // 
            // btn_Result
            // 
            this.btn_Result.Location = new System.Drawing.Point(26, 110);
            this.btn_Result.Name = "btn_Result";
            this.btn_Result.Size = new System.Drawing.Size(75, 23);
            this.btn_Result.TabIndex = 5;
            this.btn_Result.Text = "Result";
            this.btn_Result.UseVisualStyleBackColor = true;
            this.btn_Result.Click += new System.EventHandler(this.btn_Result_Click);
            // 
            // txt_OCR
            // 
            this.txt_OCR.Location = new System.Drawing.Point(131, 417);
            this.txt_OCR.Multiline = true;
            this.txt_OCR.Name = "txt_OCR";
            this.txt_OCR.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt_OCR.Size = new System.Drawing.Size(865, 217);
            this.txt_OCR.TabIndex = 6;
            // 
            // btn_OCR
            // 
            this.btn_OCR.Location = new System.Drawing.Point(26, 149);
            this.btn_OCR.Name = "btn_OCR";
            this.btn_OCR.Size = new System.Drawing.Size(75, 23);
            this.btn_OCR.TabIndex = 7;
            this.btn_OCR.Text = "OCR Start";
            this.btn_OCR.UseVisualStyleBackColor = true;
            this.btn_OCR.Click += new System.EventHandler(this.btn_OCR_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.btn_OCR);
            this.Controls.Add(this.txt_OCR);
            this.Controls.Add(this.btn_Result);
            this.Controls.Add(this.btn_modify);
            this.Controls.Add(this.btn_Test);
            this.Controls.Add(this.txt_Result);
            this.Controls.Add(this.chk_Test);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FrmMain";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox chk_Test;
        private System.Windows.Forms.TextBox txt_Result;
        private System.Windows.Forms.Button btn_Test;
        private System.Windows.Forms.Button btn_modify;
        private System.Windows.Forms.Button btn_Result;
        private System.Windows.Forms.TextBox txt_OCR;
        private System.Windows.Forms.Button btn_OCR;
    }
}

