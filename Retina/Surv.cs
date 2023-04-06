﻿using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Retina
{
    public partial class Surv : Form
    {
        public Surv()
        {
            InitializeComponent();
        }

        Mat lastMat;
        object lock1 = new object();
        IVideoSource picked = null;
        private void button1_Click(object sender, EventArgs e)
        {
            //var temp = Environment.GetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS");
            //Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;udp");
            //OpenCvSharp.VideoCapture cap = new OpenCvSharp.VideoCapture(textBox1.Text, VideoCaptureAPIs.FFMPEG);
            OpenCvSharp.VideoCapture cap = null;
            if (picked != null)
            {
                if (picked is RtspCamera r)
                {
                    cap = new VideoCapture(r.Source, VideoCaptureAPIs.FFMPEG);
                }
                if (picked is VideoFile f)
                {
                    cap = new VideoCapture(f.Path, VideoCaptureAPIs.FFMPEG);
                }
            }
            else if (webcam)
            {
                cap = new VideoCapture(0);

                cap.Set(VideoCaptureProperties.FrameWidth, 1920);
                cap.Set(VideoCaptureProperties.FrameHeight, 1080);
                var res = cap.Set(VideoCaptureProperties.FourCC, FourCC.MJPG);
                var res2 = cap.Get(VideoCaptureProperties.FourCC);

            }
            else
                cap = new VideoCapture(textBox1.Text, VideoCaptureAPIs.FFMPEG);

            if (!cap.IsOpened())
            {
                MessageBox.Show("open failed", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Mat mat = new Mat();
            Thread th = new Thread(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (cap.Read(mat))
                {
                    var ms1 = sw.ElapsedMilliseconds;
                    sw.Restart();

                    lock (lock1)
                    {
                        if (lastMat != null)
                            lastMat.Dispose();

                        lastMat = mat.Clone();
                    }
                }
            });
            th.IsBackground = true;
            th.Start();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (lastMat == null)
                return;

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            lock (lock1)
            {
                pictureBox1.Image = BitmapConverter.ToBitmap(lastMat);
            }
        }

        bool webcam = false;
        private void button2_Click(object sender, EventArgs e)
        {
            webcam = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Cameras cc = new Cameras();
            cc.PickMode = true;
            cc.StartPosition = FormStartPosition.CenterParent;
            if (cc.ShowDialog(this) == DialogResult.OK)
            {
                picked = cc.Picked;
            }
        }
    }
}
