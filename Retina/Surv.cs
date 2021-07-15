using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
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
        private void button1_Click(object sender, EventArgs e)
        {
            //var temp = Environment.GetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS");
            //Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;udp");
            OpenCvSharp.VideoCapture cap = new OpenCvSharp.VideoCapture(textBox1.Text, VideoCaptureAPIs.FFMPEG);

            if (!cap.IsOpened())
            {
                MessageBox.Show("open failed", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Mat mat = new Mat();
            Thread th = new Thread(() =>
            {
                while (cap.Read(mat))
                {
                    lock (lock1)
                    {
                        lastMat = mat.Clone();
                    }
                }
            });
            th.IsBackground = true;
            th.Start();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (lastMat != null)
            {
                lock (lastMat)
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(lastMat);
                }
            }
        }        
    }
}
