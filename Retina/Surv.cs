using OpenCvSharp;
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
        Cancel exit = null;
        public class Cancel
        {
            public bool Flag;
        }

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
                    var ofps = cap.Get(VideoCaptureProperties.Fps);

                    var msec = cap.Get(VideoCaptureProperties.PosMsec);
                    var frames = cap.Get(VideoCaptureProperties.PosFrames);
                    var framesCnt = cap.Get(VideoCaptureProperties.FrameCount);

                    if (double.IsNaN(msec))
                        msec = (frames / ofps) * 1000;

                    var total = (framesCnt / ofps) * 1000;

                    var span = new TimeSpan(0, 0, 0, 0, (int)msec);
                    var spant = new TimeSpan(0, 0, 0, 0, (int)total);
                    toolStripStatusLabel3.Text = $"{span} / {spant}";
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


            if (exit != null)
            {
                exit.Flag = true;
            }
            exit = new Cancel();
            Thread th = new Thread((arg) =>
            {
                var cn = arg as Cancel;
                Stopwatch sw = Stopwatch.StartNew();
                
                while (cap.Read(mat))
                {
                    if (cn.Flag)
                        break;

                    var ms1 = sw.ElapsedMilliseconds;
                    sw.Restart();

                    var ofps = cap.Get(VideoCaptureProperties.Fps);

                    var msec = cap.Get(VideoCaptureProperties.PosMsec);
                    var frames = cap.Get(VideoCaptureProperties.PosFrames);
                    var framesCnt = cap.Get(VideoCaptureProperties.FrameCount);

                    if (double.IsNaN(msec))
                        msec = (frames / ofps) * 1000;

                    var total = (framesCnt / ofps) * 1000;

                    var span = new TimeSpan(0, 0, 0, 0, (int)msec);
                    var spant = new TimeSpan(0, 0, 0, 0, (int)total);
                    statusStrip1.Invoke((Action)(() =>
                    {
                        toolStripStatusLabel3.Text = $"{span} / {spant}";
                    }));
                    

                    lock (lock1)
                    {
                        if (lastMat != null)
                            lastMat.Dispose();

                        lastMat = mat.Clone();
                    }
                }
            });
            
            th.IsBackground = true;
            th.Start(exit);

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
