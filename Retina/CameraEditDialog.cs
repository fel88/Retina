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
        bool editMode = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                RtspCamera cam = new RtspCamera();
                if (editMode)                
                    cam = VideoSource as RtspCamera;
                
                cam.Name = textBox2.Text;

                if (!editMode)
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

        internal void Init(IVideoSource vs)
        {
            textBox2.Text = vs.Name;
            if (vs is RtspCamera r)
            {
                radioButton1.Checked = true;
                textBox1.Text = r.Source;
            }
            editMode = true;
            VideoSource = vs;

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
