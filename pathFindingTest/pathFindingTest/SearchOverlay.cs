using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pathFindingTest
{
    public partial class SearchOverlay : Form
    {
        public SearchOverlay()
        {
            InitializeComponent();
        }

        private void SearchOverlay_Load(object sender, EventArgs e)
        {
            this.Opacity = (float)0.01f;
        }

        private void SearchOverlay_MouseClick(object sender, MouseEventArgs e)
        {
            Form1.lastKey = e.Location.X + "," + e.Location.Y;
            this.DialogResult = DialogResult.OK;
            Debug.WriteLine(Form1.lastKey);
            this.Close();
        }
    }
}
