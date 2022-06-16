using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Retina
{
    public partial class Form1 : Form
    {
        OpenCvSharp.Dnn.Net detector;
        public Form1()
        {
            InitializeComponent();
            face.Init();
            AllowDrop = true;
            pictureBox1.AllowDrop = true;
        }

        RetinaFace face = new RetinaFace();
        FsaDetector fsa = new FsaDetector();

        public RectangleF[] GetFacesSSD(Mat img)
        {
            var sz = img.Size();
            var crop = img.Resize(new OpenCvSharp.Size(300, 300));

            var blob = OpenCvSharp.Dnn.CvDnn.BlobFromImage(crop, 1, new OpenCvSharp.Size(300, 300), new Scalar(104, 177, 123), false);
            var dims1 = blob.Dims;


            detector.SetInput(blob);
            var detections = detector.Forward();

            int[] dims = new int[detections.Dims];
            for (int i = 0; i < detections.Dims; i++)
            {
                var dim = detections.Total(i, i + 1); ;
                dims[i] = (int)dim;
            }
            var confidence = 0.7;

            List<RectangleF> ret = new List<RectangleF>();
            for (int i = 0; i < dims[2]; i++)
            {
                var score = detections.At<float>(0, 0, i, 2);

                if (score > confidence)
                {
                    float[] box = new float[4];
                    for (int j = 0; j < 4; j++)
                    {
                        var b1 = detections.At<float>(0, 0, i, j + 3);
                        box[j] = b1;
                    }

                    box[0] *= sz.Width;
                    box[1] *= sz.Height;
                    box[2] *= sz.Width;
                    box[3] *= sz.Height;
                    Rect rect = new Rect((int)box[0], (int)box[1], (int)(box[2] - box[0]), (int)(box[3] - box[1]));
                    var b = new Rect(0, 0, img.Cols, img.Rows) & rect;
                    if (b == rect)
                    {
                        var sub = img.SubMat(rect);
                        ret.Add(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
                    }


                }
            }
            return ret.ToArray();
        }

        bool forward = false;
        bool backward = false;

        Thread th;
        private void open(string path, bool webcam = false)
        {
            if (th != null)
            {
                th.Abort();
            }


            if (!webcam)
            {
                string[] exts = { "jpg", "png", "bmp" };
                if (exts.Any(z => path.EndsWith(z)))
                {
                    var mat = Cv2.ImRead(path);
                    var session = new InferenceSession("nets\\FaceDetector.onnx");

                    var rr = ProcessMat(mat, session);
                    pictureBox1.Image = rr.Item1;
                    return;
                }
            }

            th = new Thread(() =>
            {
                VideoCapture cap = null;
                if (webcam)
                    cap = new VideoCapture(0);
                else
                    cap = new VideoCapture(path);

                Mat mat = new Mat();
                var ofps = cap.Get(VideoCaptureProperties.Fps);
                cap.Set(VideoCaptureProperties.Fps, 5);
                var session = new InferenceSession("nets\\FaceDetector.onnx");
                bool first = true;
                while (true)
                {
                    if (forwTo)
                    {
                        forwTo = false;
                        var frm = cap.Get(VideoCaptureProperties.FrameCount);
                        var secs = (frm / ofps) * 1000;

                        cap.Set(VideoCaptureProperties.PosMsec, forwPosPercetange * secs);
                    }
                    if (!pause || oneFrameStep || recalc)
                    {
                        oneFrameStep = false;

                        if (!recalc || first)
                        {
                            if (!cap.Read(mat)) break;
                        }

                        first = false;
                        recalc = false;

                        var msec = cap.Get(VideoCaptureProperties.PosMsec);
                        var frames = cap.Get(VideoCaptureProperties.PosFrames);
                        var framesCnt = cap.Get(VideoCaptureProperties.FrameCount);
                        if (double.IsNaN(msec))
                        {
                            msec = (frames / ofps) * 1000;
                        }
                        var span = new TimeSpan(0, 0, 0, 0, (int)msec);

                        label1.Invoke((Action)(() =>
                        {
                            label1.Text = "pos: " + span.ToString();
                        }));

                        if (forward)
                        {
                            cap.Set(VideoCaptureProperties.PosMsec, msec + 5000);
                            forward = false;
                        }
                        if (backward)
                        {
                            cap.Set(VideoCaptureProperties.PosMsec, (msec - 5000) > 0 ? (msec - 5000) : 0);
                            backward = false;
                        }

                        var rr = ProcessMat(mat, session);
                        var gr = rr.Item2;
                        var bmp = rr.Item1;
                        if (checkBox6.Checked)
                        {
                            gr.DrawString(span.ToString(), new Font("Arial", 12), Brushes.White, 10, 10);
                        }
                        if (out_vid == null && checkBox5.Checked)
                        {
                            out_vid = new VideoWriter(outputFileName, FourCC.XVID, ofps, new OpenCvSharp.Size(cap.FrameWidth, cap.FrameHeight));
                        }
                        if (checkBox5.Checked && out_vid != null)
                        {
                            out_vid.Write(BitmapConverter.ToMat(bmp));
                        }
                        pictureBox1.Image = bmp;
                    }
                    //Thread.Sleep(40);
                }
            });
            th.IsBackground = true;
            th.Start();
        }

        int eyeWidth = 8;
        bool faceDetectEnabled = true;

        public Tuple<Bitmap, Graphics> ProcessMat(Mat mat, InferenceSession session)
        {
            List<FaceInfo> faceInfos = new List<FaceInfo>();
            RectangleF[] faces = null;
            var bmp = BitmapConverter.ToBitmap(mat);
            var gr = Graphics.FromImage(bmp);
            Tuple<RectangleF[], Point2f[][], float[]> ret = null;

            if (faceDetectEnabled)
            {
                if (radioButton1.Checked)
                {
                    faces = GetFacesSSD(mat.Clone());
                }
                else
                {
                    ret = face.Forward(mat.Clone(), session);
                    faces = ret.Item1;
                    if (checkBox3.Checked)
                    {
                        foreach (var item in ret.Item2)
                        {
                            for (int i = 0; i < item.Length; i++)
                            {
                                Point2f ii2 = item[i];
                                gr.FillEllipse(brushes[i], ii2.X - eyeWidth / 2, ii2.Y - eyeWidth / 2, eyeWidth, eyeWidth);

                            }
                        }
                        for (int i = 0; i < ret.Item3.Length; i++)
                        {
                            float item = (float)ret.Item3[i];
                            var pos = ret.Item1[i];
                            gr.DrawString(Math.Round(item, 4) + "", new Font("Arial", 12), Brushes.Yellow, pos.X, pos.Y - 30);
                        }
                    }
                }

                for (int i1 = 0; i1 < faces.Length; i1++)
                {
                    RectangleF item = faces[i1];
                    if (checkBox1.Checked)
                    {
                        gr.DrawRectangle(new Pen(Color.Yellow, faceBoxWidth), new Rectangle((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
                    }

                    //fix rect to quad
                    float inflx = 0;
                    float infly = 0;
                    var orig = new Rect((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height);

                    float expandKoef = 1.1f;
                    OpenCvSharp.Point center = new OpenCvSharp.Point(orig.Left + orig.Width / 2, orig.Top + orig.Height / 2);

                    float ww = orig.Width * expandKoef;
                    float hh = orig.Height * expandKoef;

                    if (ww > hh)
                    {
                        infly = (int)(ww - hh);
                    }
                    else
                    {
                        inflx = (int)(hh - ww);
                    }
                    ww += inflx;
                    hh += infly;

                    Rect corrected = new Rect((int)(center.X - ww / 2), (int)(center.Y - hh / 2), (int)ww, (int)hh);

                    bool is_inside = (corrected & new Rect(0, 0, mat.Cols, mat.Rows)) == corrected;
                    if (is_inside)
                    {
                        var cor = mat.Clone(corrected);

                        var axis = fsa.GetAxis(cor.Clone(), corrected);
                        if (faceRecognitionEnabled)
                        {
                            var fn = new FaceNet();
                            var (label, mdist) = fn.Recognize(cor.Clone());
                            var txt = (label == null || mdist.Value < faceRecognitionThreshold) ? "(unknown)" : $"{label.Label} ({mdist.Value:N2})";
                            var font = new Font("Arial", 12);
                            var ms = gr.MeasureString(txt, font);
                            gr.FillRectangle(new SolidBrush(Color.FromArgb(64, Color.White)), orig.X, orig.Y - 30, ms.Width, ms.Height);
                            gr.DrawString(txt, font, Brushes.Blue, (int)orig.X, (int)orig.Y - 30);
                        }

                        var val = Math.Abs(axis.Item2[0]) + Math.Abs(axis.Item2[1]);
                        var val1 = Math.Abs(axis.Item2[0]);
                        var val2 = Math.Abs(axis.Item2[1]);

                        if (checkBox1.Checked)
                        {
                            gr.DrawRectangle(new Pen(Color.Green, faceBoxWidth * 2), new Rectangle((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
                        }

                        faceInfos.Add(new FaceInfo() { Label = val.ToString(), Mat = cor, Rect = new Rect2f(item.X, item.Y, item.Width, item.Height) });

                        Pen[] pens = new Pen[] { Pens.Red, Pens.Green, Pens.Blue };

                        if (checkBox2.Checked)
                        {
                            gr.DrawString("yaw: " + axis.Item2[0], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y);
                            gr.DrawString("pitch: " + axis.Item2[1], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y + 16);
                            gr.DrawString("roll: " + axis.Item2[2], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y + 32);
                        }

                        if (checkBox4.Checked)
                        {
                            for (int i = 0; i < axis.Item1.Length; i++)
                            {
                                int[] axis1 = axis.Item1[i];
                                gr.DrawLine(new Pen(pens[i].Color, faceBoxWidth), axis1[0], axis1[1], axis1[2], axis1[3]);
                            }
                        }
                    }
                }
            }
            if (faceInfos.Any())
            {
                var fr = faceInfos.OrderBy(x => x.Rect.Left).ThenBy(z => z.Rect.Top).First();
                pictureBox2.Invoke((Action)(() =>
                {
                    label4.Text = fr.Label;
                    label4.BackColor = Color.LightGreen;
                    pictureBox2.Image = BitmapConverter.ToBitmap(fr.Mat);
                }));
            }

            return new Tuple<Bitmap, Graphics>(bmp, gr);
        }

        bool pause = false;

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pause = !pause;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            forward = true;
        }

        bool oneFrameStep = false;
        int faceBoxWidth = 3;

        private void button4_Click(object sender, EventArgs e)
        {
            pause = true;
            oneFrameStep = true;
        }

        bool forwTo = false;
        double forwPosPercetange = 0;
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            var gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);


            var pc = pictureBox3.PointToClient(Cursor.Position);
            var ff = pc.X / (float)pictureBox3.Width;
            forwTo = true;
            forwPosPercetange = ff;

            gr.FillRectangle(Brushes.LightBlue, 0, 0, (int)(forwPosPercetange * bmp.Width), bmp.Height);
            pictureBox3.Image = bmp;

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            backward = true;
        }
        bool inited = false;
        private void toolStripButton1_Click(object sender, EventArgs e)
        {            
            InitSessions();
            open(null, true);
        }


        bool recalc = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }
        int yprShift = 0;
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                yprShift = int.Parse(textBox2.Text);
                recalc = true;
            }
            catch
            {

            }
        }
        Brush[] brushes = new Brush[] { Brushes.Red, Brushes.Yellow,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Violet };


        public void InitSessions()
        {
            if (inited) return;
            inited = true;
            try
            {
                fsa.Init();

                var config_path = "nets\\resnet10_ssd.prototxt";
                var face_model_path = "nets\\res10_300x300_ssd_iter_140000.caffemodel";

                detector = OpenCvSharp.Dnn.CvDnn.ReadNetFromCaffe(config_path, face_model_path);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            recalc = true;
        }


        VideoWriter out_vid = null;

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var frame = Cv2.ImRead(ofd.FileName);
            var session = new InferenceSession("nets\\FaceDetector.onnx");
            var bmp = BitmapConverter.ToBitmap(frame);
            var ret = face.Forward(frame, session);
            var gr = Graphics.FromImage(bmp);

            foreach (var item in ret.Item1)
            {
                gr.DrawRectangle(new Pen(Color.Green, faceBoxWidth), item.X, item.Y, item.Width, item.Height);
            }

            if (checkBox3.Checked)
            {
                foreach (var item in ret.Item2)
                {
                    for (int i = 0; i < item.Length; i++)
                    {
                        Point2f ii2 = item[i];
                        int ww = 10;
                        gr.FillEllipse(brushes[i], ii2.X - ww / 2, ii2.Y - ww / 2, ww, ww);

                    }
                }
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "jpg (*.jpg)|*.jpg";
            if (sfd.ShowDialog() != DialogResult.OK) return;
            bmp.Save(sfd.FileName);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            faceBoxWidth = (int)numericUpDown1.Value;
            recalc = true;
        }

        string outputFileName = "output.mp4";
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            outputFileName = textBox1.Text;
        }


        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            var ar = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (ar == null) return;
            InitSessions();
            open(ar[0]);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            eyeWidth = (int)numericUpDown2.Value;
            recalc = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (th != null) th.Abort();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            faceDetectEnabled = checkBox7.Checked;
        }

        bool faceRecognitionEnabled = false;
        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            faceRecognitionEnabled = checkBox8.Checked;
        }

        double faceRecognitionThreshold = 0.3;
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                faceRecognitionThreshold = double.Parse(textBox3.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            }
            catch
            {

            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            InitSessions();
            open(ofd.FileName);
        }
    }
    public class FaceInfo
    {
        public string Label;
        public Mat Mat;
        public Rect2f Rect;
    }
}
