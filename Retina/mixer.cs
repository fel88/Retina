using OpenCvSharp;
using OpenCvSharp.Extensions;
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

namespace Retina
{
    public partial class mixer : Form
    {
        public mixer()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int ofps = 30;
            int ww = 1920;
            int hh = 1080;
            var fn = textBox1.Text;
            if (File.Exists(fn))
            {
                var res = MessageBox.Show($"File {fn} exists. Overwrite?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                switch (res)
                {
                    case DialogResult.Yes:
                        break;
                    case DialogResult.No:
                        int ii = 0;
                        while (true)
                        {
                            var nfn = Path.GetFileNameWithoutExtension(fn) + "_" + ii + Path.GetExtension(fn);
                            if (!File.Exists(nfn))
                            {
                                fn = nfn; 
                                break;
                            }
                            ii++;
                            if (ii > 1000) return;

                        }

                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
            button1.Enabled = false;

            var out_vid = new VideoWriter(fn, FourCC.XVID, ofps < 0.5 ? 30 : ofps,
                new OpenCvSharp.Size(ww * 2, hh));

            await Task.Run(() =>
            {


                var v1 = openCam(cam1Idx, true, false, ww, hh);
                var v2 = openCam(cam2Idx, true, false, ww, hh);
                int cnt = (int)numericUpDown1.Value;
                int i = 0;
                //for (int i = 0; i < cnt; i++)
                Mat mat1 = new Mat();
                Mat mat2 = new Mat();
                while (true)
                {
                    if (fixedFrames)
                    {
                        if (i >= cnt) break;

                    }
                    else if (stop)
                        break;
                    i++;

                    v1.Read(mat1);
                    v2.Read(mat2);
                    Mat res = new Mat();
                    Cv2.HConcat(new[] { mat1, mat2 }, res);
                    pictureBox1.Image = res.ToBitmap();
                    out_vid.Write(res);
                    // out_vid.Write(mat2);
                    if (fixedFrames)
                        toolStripStatusLabel1.Text = $"{i} / {cnt}";
                    else
                        toolStripStatusLabel1.Text = $"frames: {i}";

                }
                button1.Enabled = true;
            });
            toolStripStatusLabel1.Text = "done!";
        }

        VideoCapture openCam(int idx, bool mjpeg, bool autoFocus, int w, int h)
        {
            var cap = new VideoCapture(idx);

            cap.Set(VideoCaptureProperties.FrameWidth, w);
            cap.Set(VideoCaptureProperties.FrameHeight, h);
            if (mjpeg)
            {
                var res = cap.Set(VideoCaptureProperties.FourCC, FourCC.MJPG);
                var res2 = cap.Get(VideoCaptureProperties.FourCC);

            }

            cap.Set(VideoCaptureProperties.AutoFocus, autoFocus ? 1 : 0);
            return cap;
        }
        bool stop = false;
        private void button2_Click(object sender, EventArgs e)
        {
            stop = true;
        }

        bool fixedFrames = false;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            fixedFrames = checkBox1.Checked;
        }

        int cam1Idx = 0;
        int cam2Idx = 0;
        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            cam1Idx = (int)numericUpDown2.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            cam2Idx = (int)numericUpDown3.Value;
        }
    }
}
