using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class CameraEditDialog : Form
    {
        public CameraEditDialog()
        {
            InitializeComponent();
        }
        public IVideoSource VideoSource;
        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                RtspCamera cam = new RtspCamera();
                cam.Name = textBox2.Text;
                VideoSource = cam;
                cam.Source = textBox1.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
            if (radioButton3.Checked)
            {
                VideoFile cam = new VideoFile();
                cam.Name = textBox2.Text;
                VideoSource = cam;
                cam.Path = textBox3.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = ofd.FileName;
            }
        }
    }
}
