using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class mixer : Form
    {
        public mixer()
        {
            InitializeComponent();
            pictureBox1.Paint += PictureBox1_Paint;
            ctx.Init(pictureBox1);
            Load += Form1_Load;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            hoveredLineIdx = null;
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            hoveredLineIdx = null;
            var pos = pictureBox1.PointToClient(Cursor.Position);

            var bt = ctx.BackTransform(pos);
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    int item = lines[i];
                    var diff = Math.Abs(item - bt.Y);
                    if (diff < 5)
                    {                     
                        hoveredLineIdx = i;
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mf = new MessageFilter();
            Application.AddMessageFilter(mf);
        }


        MessageFilter mf = null;
        DrawingContext ctx = new DrawingContext();


        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (!calibrateMode)
                return;

            ctx.gr = e.Graphics;
            e.Graphics.Clear(Color.White);
            ctx.DrawLine(Pens.Red, new PointF(0, 0), new PointF(100, 0));
            ctx.DrawLine(Pens.Blue, new PointF(0, 0), new PointF(0, 100));

            var z = ctx.Transform(new PointF(0, 0));
            if (renderImg != null)
            {
                ctx.gr.DrawImage(renderImg, z.X, z.Y, renderImg.Width * ctx.zoom, renderImg.Height * ctx.zoom);
            }

            foreach (var item in lines)
            {
                var t0 = ctx.Transform(new PointF(-10000, item));
                Pen pen = new Pen(Color.Blue);
                pen.DashPattern = new float[] { 5, 5 };
                
                ctx.gr.DrawLine(pen, -1000, t0.Y, 5000, t0.Y);

                pen = new Pen(Color.White);
                pen.DashPattern = new float[] { 5, 5 };
                pen.DashOffset = 5;

                ctx.gr.DrawLine(pen, -1000, t0.Y, 5000, t0.Y);
            }
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
                timer1.Enabled = false;
                while (true)
                {
                    if (fixedFrames)
                    {
                        if (i >= cnt) break;

                    }
                    else if (stop)
                        break;
                    i++;

                    if (i % 10 == 0) GC.Collect();

                    v1.Read(mat1);
                    v2.Read(mat2);
                    Mat res = new Mat();
                    Cv2.HConcat(new[] { mat1, mat2 }, res);


                    out_vid.Write(res);
                    // out_vid.Write(mat2);
                    if (calibrateMode)
                    {
                        renderImg = res.ToBitmap();
                        render();
                    }
                    else
                    {
                        pictureBox1.Image = res.ToBitmap();
                    }

                    if (fixedFrames)
                        toolStripStatusLabel1.Text = $"{i} / {cnt}";
                    else
                        toolStripStatusLabel1.Text = $"frames: {i}";

                }
                button1.Enabled = true;
            });
            toolStripStatusLabel1.Text = "done!";
        }

        Bitmap renderImg;

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
        int cam2Idx = 1;
        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            cam1Idx = (int)numericUpDown2.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            cam2Idx = (int)numericUpDown3.Value;
        }

        private void addCalibrateLineToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        List<int> lines = new List<int>();

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var c = pictureBox1.PointToClient(Cursor.Position);
            lines.Add(c.Y);
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        int? hoveredLineIdx = null;
        void render()
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            var bt = ctx.BackTransform(pos);
            Cursor = Cursors.Default;
          
           

            if (hoveredLineIdx == null) ctx.UpdateDrag();
            else
            {
                Cursor = Cursors.HSplit;
                lines[hoveredLineIdx.Value] = (int)bt.Y;
            }

            pictureBox1.Invalidate();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            render();
        }

        bool calibrateMode = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            calibrateMode = comboBox1.SelectedIndex == 1;
        }
    }
}
