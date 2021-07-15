using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Retina
{
    public partial class AddReferenceFace : Form
    {
        public AddReferenceFace()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            

        }
        public string Label { get; set; }
        
        public Mat Mat;

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text)) return;
            DialogResult = DialogResult.OK;
            Close();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Label = textBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            pictureBox1.Image = Bitmap.FromFile(ofd.FileName);
            Mat = Cv2.ImRead(ofd.FileName);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var img = Clipboard.GetImage() as Bitmap;
            pictureBox1.Image = img;
            Mat = BitmapConverter.ToMat(img);
        }
    }
}
