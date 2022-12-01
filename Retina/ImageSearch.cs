using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.ImgHash;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class ImageSearch : Form
    {
        public ImageSearch()
        {
            InitializeComponent();
        }
        BlockMeanHash hash = BlockMeanHash.Create();

        private async void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null) return;
            button1.Enabled = false;
            var d = new DirectoryInfo(textBox1.Text);
            string[] exts = new string[] { "mp4", "avi" };
            
            var tree = d.GetFiles("*", SearchOption.AllDirectories).Where(z => exts.Any(u => z.Name.EndsWith(u))).ToArray();
            
            var pattern = (pictureBox1.Image as Bitmap).ToMat();
            //var threshold = errorRate / 100.0;
            var threshold = hashErrorRate * 1000;
            listView1.Items.Clear();

            await Task.Run(() =>
            {

                for (int i = 0; i < tree.Length; i++)
                {
                    FileInfo item = tree[i];
                    toolStripStatusLabel3.Text = (i + 1) + " / " + tree.Length;                    
                    using (VideoCapture cap = new VideoCapture(item.FullName))
                    {
                        int frame = 0;
                        Mat mat = new Mat();
                        while (cap.Read(mat))
                        {
                            frame++;
                            if (framesLimitEnabled && frame > framesLimit)
                            {
                                break;
                            }
                            if (IsMatEquals(mat, pattern, threshold))
                            {
                                listView1.Items.Add(new ListViewItem(new string[] { item.FullName }) { Tag = item.FullName });
                                break;
                            }
                        }
                    }
                    GC.Collect();
                }
            });
            button1.Enabled = true;
        }

        private bool IsMatEquals(Mat mat, Mat pattern, double threshold = 0.01)
        {
            //Mat res = new Mat();
            var mat_bg = mat.CvtColor(ColorConversionCodes.BGR2GRAY).Resize(pattern.Size());
            var pattern_bg = pattern.CvtColor(ColorConversionCodes.BGR2GRAY);
            var cres = hash.Compare(mat_bg, pattern_bg);
            //Cv2.Absdiff(mat_bg, pattern_bg, res);
            //double err = (double)Cv2.CountNonZero(res) / (pattern.Width * pattern.Height);
            return cres < threshold;
        }
        

        private void fromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Clipboard.GetImage();
        }
        bool framesLimitEnabled = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            framesLimitEnabled = checkBox1.Checked;
        }
        int framesLimit = 50;
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            framesLimit = (int)numericUpDown1.Value;
        }

        double errorRate = 10;
        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            errorRate = (double)numericUpDown2.Value;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var str = listView1.SelectedItems[0].Tag as string;
            Process.Start(str);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            textBox1.Text = new FileInfo(ofd.FileName).Directory.FullName;
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            pictureBox1.Image = Bitmap.FromFile(ofd.FileName);
        }

        int hashErrorRate = 300;
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            hashErrorRate = (int)numericUpDown3.Value;
        }
    }
}
