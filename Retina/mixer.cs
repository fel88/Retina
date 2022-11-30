using OpenCvSharp;
using OpenCvSharp.Extensions;
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
    public partial class mixer : Form
    {
        public mixer()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            await Task.Run(() =>
            {
                int ofps = 30;
                int ww = 1920;
                int hh = 1080;
                var out_vid = new VideoWriter(textBox1.Text, FourCC.XVID, ofps < 0.5 ? 30 : ofps,
                    new OpenCvSharp.Size(ww * 2, hh));

                var v1 = openCam(0, true, false, ww, hh);
                var v2 = openCam(1, true, false, ww, hh);
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
            fixedFrames = true;
        }
    }
}
