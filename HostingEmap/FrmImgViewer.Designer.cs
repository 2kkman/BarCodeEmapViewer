namespace HostingEmap
{
    partial class FrmImgViewer
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
            this.uiTlp_Main = new System.Windows.Forms.TableLayoutPanel();
            this.uiTlp_Sub = new System.Windows.Forms.TableLayoutPanel();
            this.uiFlp_Thumnail = new System.Windows.Forms.FlowLayoutPanel();
            this.uiPic_Main = new System.Windows.Forms.PictureBox();
            this.uiTlp_Main.SuspendLayout();
            this.uiTlp_Sub.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uiPic_Main)).BeginInit();
            this.SuspendLayout();
            // 
            // uiTlp_Main
            // 
            this.uiTlp_Main.BackColor = System.Drawing.Color.DimGray;
            this.uiTlp_Main.ColumnCount = 1;
            this.uiTlp_Main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTlp_Main.Controls.Add(this.uiTlp_Sub, 0, 0);
            this.uiTlp_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTlp_Main.Location = new System.Drawing.Point(0, 0);
            this.uiTlp_Main.Name = "uiTlp_Main";
            this.uiTlp_Main.RowCount = 1;
            this.uiTlp_Main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTlp_Main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.uiTlp_Main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.uiTlp_Main.Size = new System.Drawing.Size(859, 587);
            this.uiTlp_Main.TabIndex = 0;
            this.uiTlp_Main.Paint += new System.Windows.Forms.PaintEventHandler(this.uiTlp_Main_Paint);
            // 
            // uiTlp_Sub
            // 
            this.uiTlp_Sub.BackColor = System.Drawing.Color.Black;
            this.uiTlp_Sub.ColumnCount = 2;
            this.uiTlp_Sub.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.56389F));
            this.uiTlp_Sub.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 76.43611F));
            this.uiTlp_Sub.Controls.Add(this.uiFlp_Thumnail, 0, 0);
            this.uiTlp_Sub.Controls.Add(this.uiPic_Main, 1, 0);
            this.uiTlp_Sub.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTlp_Sub.Location = new System.Drawing.Point(3, 3);
            this.uiTlp_Sub.Name = "uiTlp_Sub";
            this.uiTlp_Sub.RowCount = 1;
            this.uiTlp_Sub.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.uiTlp_Sub.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.uiTlp_Sub.Size = new System.Drawing.Size(853, 581);
            this.uiTlp_Sub.TabIndex = 3;
            // 
            // uiFlp_Thumnail
            // 
            this.uiFlp_Thumnail.AutoScroll = true;
            this.uiFlp_Thumnail.BackColor = System.Drawing.Color.DimGray;
            this.uiFlp_Thumnail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiFlp_Thumnail.Location = new System.Drawing.Point(3, 3);
            this.uiFlp_Thumnail.Name = "uiFlp_Thumnail";
            this.uiFlp_Thumnail.Size = new System.Drawing.Size(194, 575);
            this.uiFlp_Thumnail.TabIndex = 0;
            // 
            // uiPic_Main
            // 
            this.uiPic_Main.BackColor = System.Drawing.Color.DimGray;
            this.uiPic_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPic_Main.Location = new System.Drawing.Point(203, 3);
            this.uiPic_Main.Name = "uiPic_Main";
            this.uiPic_Main.Size = new System.Drawing.Size(647, 575);
            this.uiPic_Main.TabIndex = 1;
            this.uiPic_Main.TabStop = false;
            this.uiPic_Main.Click += new System.EventHandler(this.uiPic_Main_Click);
            // 
            // FrmImgViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(859, 587);
            this.Controls.Add(this.uiTlp_Main);
            this.Name = "FrmImgViewer";
            this.Text = "Image Processing Program";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FrmImgViewer_Load);
            this.uiTlp_Main.ResumeLayout(false);
            this.uiTlp_Sub.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uiPic_Main)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel uiTlp_Main;
        private System.Windows.Forms.TableLayoutPanel uiTlp_Sub;
        private System.Windows.Forms.FlowLayoutPanel uiFlp_Thumnail;
        private System.Windows.Forms.PictureBox uiPic_Main;
    }
}

