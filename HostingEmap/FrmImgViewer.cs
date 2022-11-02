using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HostingEmap
{
    public partial class FrmImgViewer : Form
    {
        List<string> imgList = null;
        string[] files = null;
        public FrmImgViewer(string[] saFileList)
        {
            InitializeComponent();
            files = saFileList;
            //this.uiBtn_Load.Click += UiBtn_Load_Click;
        }

        private void UiBtn_Load_Click(object sender, EventArgs e)
        {
            this.uiFlp_Thumnail.Controls.Clear();
            imgList = files.Where(x => x.IndexOf(".jpg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       x.IndexOf(".png", StringComparison.OrdinalIgnoreCase) >= 0)
                           .Select(x => x).ToList();

            for (int i = 0; i < imgList.Count; i++)
            {
                Image img = Image.FromFile(imgList[i]);

                Panel pPanel = new Panel();
                pPanel.BackColor = Color.Black;
                pPanel.Size = new Size(150, 150);
                pPanel.Padding = new System.Windows.Forms.Padding(4);

                PictureBox pBox = new PictureBox();
                pBox.BackColor = Color.DimGray;
                pBox.Dock = DockStyle.Fill;
                pBox.SizeMode = PictureBoxSizeMode.Zoom;
                pBox.Image = img.GetThumbnailImage(150, 150, null, IntPtr.Zero);
                pBox.Click += PBox_Click;
                pBox.DoubleClick += PBox_DoubleClick;
                pBox.Tag = i.ToString();
                pPanel.Controls.Add(pBox);

                this.uiFlp_Thumnail.Controls.Add(pPanel);
            }

            if (imgList.Count > 0)
            {
                Panel pnl = this.uiFlp_Thumnail.Controls[0] as Panel;
                PictureBox pb = pnl.Controls[0] as PictureBox;
                this.Text = this.imgList[0];
                PBox_Click(pb, null);
            }
        }

        private void PBox_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            int idx = Convert.ToInt32(pb.Tag.ToString());
            string file = this.imgList[idx].ToString();

            ZoomForm frm = new ZoomForm();
            frm.file = file;

            frm.ShowDialog();
        }

        private void PBox_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.uiFlp_Thumnail.Controls.Count; i++)
            {
                if (this.uiFlp_Thumnail.Controls[i] is Panel)
                {
                    Panel pnl = this.uiFlp_Thumnail.Controls[i] as Panel;
                    pnl.BackColor = Color.Black;
                }
            }

            PictureBox pb = sender as PictureBox;
            pb.Parent.BackColor = Color.Red;

            int idx = Convert.ToInt32(pb.Tag.ToString());

            Image img = Image.FromFile(imgList[idx]);
            uiPic_Main.Image = img;
            uiPic_Main.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Text = this.imgList[idx];
            int iVerticalPoint = idx * pb.Height;
            int iVerticalScroll = uiFlp_Thumnail.Height > iVerticalPoint ? iVerticalPoint : uiFlp_Thumnail.Height;
            //uiFlp_Thumnail.VerticalScroll.Value = iVerticalScroll;
        }

        private void uiTlp_Main_Paint(object sender, PaintEventArgs e)
        {

        }

        private void FrmImgViewer_Load(object sender, EventArgs e)
        {
            UiBtn_Load_Click(sender, e);
        }

        private void uiPic_Main_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
