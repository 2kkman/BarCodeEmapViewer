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
    public partial class FrmViewConfig : Form
    {
        public static string _sSelectedView = string.Empty;
        public FrmViewConfig()
        {
            InitializeComponent();
            _sSelectedView = string.Empty;
        }
         
        private void rb_V1_CheckedChanged(object sender, EventArgs e)
        {
            Control ctl = (Control)(sender);
            _sSelectedView = ctl.Name;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (_sSelectedView.Equals(string.Empty))
            {
                MessageBox.Show("저장할 뷰를 지정하여 주시기 바랍니다.");
            }
            else
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            _sSelectedView = string.Empty;
            this.DialogResult = DialogResult.Cancel;
        }

        private void FrmViewConfig_Load(object sender, EventArgs e)
        {

        }
    }
}
