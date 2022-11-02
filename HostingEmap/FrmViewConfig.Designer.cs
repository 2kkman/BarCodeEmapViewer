namespace HostingEmap
{
    partial class FrmViewConfig
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
            this.gb_View = new System.Windows.Forms.GroupBox();
            this.rb_V4 = new System.Windows.Forms.RadioButton();
            this.rb_V2 = new System.Windows.Forms.RadioButton();
            this.rb_V3 = new System.Windows.Forms.RadioButton();
            this.rb_V1 = new System.Windows.Forms.RadioButton();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Close = new System.Windows.Forms.Button();
            this.gb_View.SuspendLayout();
            this.SuspendLayout();
            // 
            // gb_View
            // 
            this.gb_View.Controls.Add(this.rb_V4);
            this.gb_View.Controls.Add(this.rb_V2);
            this.gb_View.Controls.Add(this.rb_V3);
            this.gb_View.Controls.Add(this.rb_V1);
            this.gb_View.Location = new System.Drawing.Point(17, 8);
            this.gb_View.Name = "gb_View";
            this.gb_View.Size = new System.Drawing.Size(316, 156);
            this.gb_View.TabIndex = 0;
            this.gb_View.TabStop = false;
            this.gb_View.Text = "뷰 목록";
            // 
            // rb_V4
            // 
            this.rb_V4.AutoSize = true;
            this.rb_V4.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rb_V4.Location = new System.Drawing.Point(168, 82);
            this.rb_V4.Name = "rb_V4";
            this.rb_V4.Size = new System.Drawing.Size(78, 29);
            this.rb_V4.TabIndex = 0;
            this.rb_V4.TabStop = true;
            this.rb_V4.Text = "선반2";
            this.rb_V4.UseVisualStyleBackColor = true;
            this.rb_V4.CheckedChanged += new System.EventHandler(this.rb_V1_CheckedChanged);
            // 
            // rb_V2
            // 
            this.rb_V2.AutoSize = true;
            this.rb_V2.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rb_V2.Location = new System.Drawing.Point(6, 82);
            this.rb_V2.Name = "rb_V2";
            this.rb_V2.Size = new System.Drawing.Size(87, 29);
            this.rb_V2.TabIndex = 0;
            this.rb_V2.TabStop = true;
            this.rb_V2.Text = "기본뷰";
            this.rb_V2.UseVisualStyleBackColor = true;
            this.rb_V2.CheckedChanged += new System.EventHandler(this.rb_V1_CheckedChanged);
            // 
            // rb_V3
            // 
            this.rb_V3.AutoSize = true;
            this.rb_V3.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rb_V3.Location = new System.Drawing.Point(168, 20);
            this.rb_V3.Name = "rb_V3";
            this.rb_V3.Size = new System.Drawing.Size(78, 29);
            this.rb_V3.TabIndex = 0;
            this.rb_V3.TabStop = true;
            this.rb_V3.Text = "선반1";
            this.rb_V3.UseVisualStyleBackColor = true;
            this.rb_V3.CheckedChanged += new System.EventHandler(this.rb_V1_CheckedChanged);
            // 
            // rb_V1
            // 
            this.rb_V1.AutoSize = true;
            this.rb_V1.Font = new System.Drawing.Font("맑은 고딕", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rb_V1.Location = new System.Drawing.Point(6, 20);
            this.rb_V1.Name = "rb_V1";
            this.rb_V1.Size = new System.Drawing.Size(68, 29);
            this.rb_V1.TabIndex = 0;
            this.rb_V1.TabStop = true;
            this.rb_V1.Text = "탑뷰";
            this.rb_V1.UseVisualStyleBackColor = true;
            this.rb_V1.CheckedChanged += new System.EventHandler(this.rb_V1_CheckedChanged);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(40, 178);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(114, 29);
            this.btn_OK.TabIndex = 1;
            this.btn_OK.Text = "&OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Close
            // 
            this.btn_Close.Location = new System.Drawing.Point(185, 178);
            this.btn_Close.Name = "btn_Close";
            this.btn_Close.Size = new System.Drawing.Size(109, 29);
            this.btn_Close.TabIndex = 1;
            this.btn_Close.Text = "&Close";
            this.btn_Close.UseVisualStyleBackColor = true;
            this.btn_Close.Click += new System.EventHandler(this.btn_Close_Click);
            // 
            // FrmViewConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 216);
            this.Controls.Add(this.btn_Close);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.gb_View);
            this.Name = "FrmViewConfig";
            this.Text = "저장할 뷰 선택";
            this.Load += new System.EventHandler(this.FrmViewConfig_Load);
            this.gb_View.ResumeLayout(false);
            this.gb_View.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gb_View;
        private System.Windows.Forms.RadioButton rb_V4;
        private System.Windows.Forms.RadioButton rb_V2;
        private System.Windows.Forms.RadioButton rb_V3;
        private System.Windows.Forms.RadioButton rb_V1;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Close;
    }
}