using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HostingEmap
{
    public partial class ZoomForm : Form
    {
        public string file = "";

        public ZoomForm()
        {
            InitializeComponent();

            this.Shown += ZoomForm_Shown;
        }

        private void ZoomForm_Shown(object sender, EventArgs e)
        {
            pictureBox1.Image = Image.FromFile(file);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }
    }
}
