using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class SearchFaces : Form
    {
        public SearchFaces()
        {
            InitializeComponent();
            face.Init();
            var config_path = "nets\\resnet10_ssd.prototxt";
            var face_model_path = "nets\\res10_300x300_ssd_iter_140000.caffemodel";

            detector = OpenCvSharp.Dnn.CvDnn.ReadNetFromCaffe(config_path, face_model_path);

            UpdateFacesList();
        }

        void UpdateFacesList()
        {
            listView2.Items.Clear();
            foreach (var item in FaceNet.Faces)
            {
                listView2.Items.Add(new ListViewItem(new string[] { item.Label }) { Tag = item });
            }
        }
        bool SingleMatProcess(Mat mat, InferenceSession session, FileInfo item)
        {
            var rr = GetFaces(mat, session);
            var ret = (rr != null && rr.Select(z => z.Face).Intersect(facesToSearch).Any());
            if (ret)
            {
                listView1.Invoke((Action)(() =>
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { item.Name, string.Join(", ", rr.Where(z => facesToSearch.Contains(z.Face)).Select(z => z.Face.Label)) })
                        {
                            Tag = new SearchFaceResult()
                            {
                                FilePath = item.FullName,
                                Target = mat.Clone(),
                                Faces = rr
                            }
                        });

                    }));
            }
            return ret;
        }

        Random rand = new Random();
        private async void button1_Click(object sender, EventArgs e)
        {
            var d = new DirectoryInfo(textBox1.Text);
            int cntr = 0;
            int cntr2 = 0;
            int total = 0;
            stopSearch = false;
            button1.Enabled = false;
            await Task.Run(() =>
            {
                List<string> exts = new List<string>() { "jpg", "png", "bmp" };
                List<string> vidExts = new List<string>() { "mp4", "avi" };
                if (includeVideos)
                {
                    exts.AddRange(vidExts);
                }
                foreach (var item in d.GetFiles())
                {
                    if (stopSearch) break;

                    var path = item.FullName;

                    if (exts.Any(z => path.EndsWith(z)))
                    {
                        total++;
                    }
                }
                if (stopSearch) return;
                var session = new InferenceSession("nets\\FaceDetector.onnx");

                foreach (var item in d.GetFiles())
                {
                    if (stopSearch) break;

                    var path = item.FullName;

                    if (exts.Any(z => path.EndsWith(z)))
                    {
                        if (vidExts.Any(z => path.EndsWith(z)))
                        {
                            var b = File.ReadAllBytes(path);
                            Mat mat = new Mat();

                            using (var cap = new VideoCapture(path))
                            {
                                var ofps = cap.Get(VideoCaptureProperties.Fps);

                                int frame = 0;

                                while (cap.Read(mat))
                                {
                                    if (stopSearch) break;
                                    if (randomLimitMode)
                                    {
                                        var frm = cap.Get(VideoCaptureProperties.FrameCount);
                                        var secs = (frm / ofps) * 1000;
                                        var forwPosPercetange = rand.NextDouble();
                                        cap.Set(VideoCaptureProperties.PosMsec, forwPosPercetange * secs);
                                    }

                                    toolStripStatusLabel2.Text = path + $" (frame: {frame})";

                                    frame++;
                                    if (videoFramesLimitEnabled && frame > framesLimit)
                                        break;

                                    if (SingleMatProcess(mat, session, item))
                                        break;

                                }
                            }
                        }
                        else
                        {
                            var mat = Cv2.ImRead(path);
                            SingleMatProcess(mat, session, item);
                            mat.Dispose();
                            cntr++;
                        }
                        GC.Collect();

                    }
                    cntr2++;
                    listView1.Invoke((Action)(() =>
                    {
                        toolStripStatusLabel3.Text = cntr + " / " + cntr2 + " / " + total;
                    }));
                }
            });
            button1.Enabled = true;

        }

        OpenCvSharp.Dnn.Net detector;

        RetinaFace face = new RetinaFace();
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

        double faceRecognitionThreshold = 0.6;
        public RecognizedFaceInfo[] GetFaces(Mat mat, InferenceSession session)
        {
            List<RecognizedFaceInfo> faceInfos = new List<RecognizedFaceInfo>();
            RectangleF[] faces = null;


            Tuple<RectangleF[], Point2f[][], float[]> ret = null;

            //  if (faceDetectEnabled)
            {
                //  if (radioButton1.Checked)
                {
                    faces = GetFacesSSD(mat.Clone());
                }
                // else
                {
                    ret = face.Forward(mat.Clone(), session);
                    faces = faces.Union(ret.Item1).ToArray();
                    //  if (checkBox3.Checked)
                    {
                        foreach (var item in ret.Item2)
                        {
                            for (int i = 0; i < item.Length; i++)
                            {
                                Point2f ii2 = item[i];
                                //  gr.FillEllipse(brushes[i], ii2.X - eyeWidth / 2, ii2.Y - eyeWidth / 2, eyeWidth, eyeWidth);

                            }
                        }
                        for (int i = 0; i < ret.Item3.Length; i++)
                        {
                            float item = (float)ret.Item3[i];
                            var pos = ret.Item1[i];
                            // gr.DrawString(Math.Round(item, 4) + "", new Font("Arial", 12), Brushes.Yellow, pos.X, pos.Y - 30);
                        }
                    }
                }

                for (int i1 = 0; i1 < faces.Length; i1++)
                {
                    RectangleF item = faces[i1];
                    //if (checkBox1.Checked)
                    //{
                    //    gr.DrawRectangle(new Pen(Color.Yellow, faceBoxWidth), new Rectangle((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
                    //}

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

                        //var axis = fsa.GetAxis(cor.Clone(), corrected);
                        //if (faceRecognitionEnabled)
                        {
                            var fn = new FaceNet();
                            var (label, mdist) = fn.Recognize(cor.Clone());
                            var txt = (label == null || mdist.Value < faceRecognitionThreshold) ? "(unknown)" : $"{label.Label} ({mdist.Value:N2})";
                            var font = new Font("Arial", 12);
                            //var ms = gr.MeasureString(txt, font);
                            //gr.FillRectangle(new SolidBrush(Color.FromArgb(64, Color.White)), orig.X, orig.Y - 30, ms.Width, ms.Height);
                            //gr.DrawString(txt, font, Brushes.Blue, (int)orig.X, (int)orig.Y - 30);
                            faceInfos.Add(new RecognizedFaceInfo() { Face = label, Mat = cor, Rect = new Rect2f(item.X, item.Y, item.Width, item.Height) });

                        }

                        //var val = Math.Abs(axis.Item2[0]) + Math.Abs(axis.Item2[1]);
                        //var val1 = Math.Abs(axis.Item2[0]);
                        //var val2 = Math.Abs(axis.Item2[1]);

                        //if (checkBox1.Checked)
                        //{
                        //    gr.DrawRectangle(new Pen(Color.Green, faceBoxWidth * 2), new Rectangle((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
                        //}


                        Pen[] pens = new Pen[] { Pens.Red, Pens.Green, Pens.Blue };

                        //if (checkBox2.Checked)
                        //{
                        //    gr.DrawString("yaw: " + axis.Item2[0], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y);
                        //    gr.DrawString("pitch: " + axis.Item2[1], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y + 16);
                        //    gr.DrawString("roll: " + axis.Item2[2], new Font("Arial", 12), Brushes.Red, (int)orig.X + yprShift, (int)orig.Y + 32);
                        //}

                        //if (checkBox4.Checked)
                        //{
                        //    for (int i = 0; i < axis.Item1.Length; i++)
                        //    {
                        //        int[] axis1 = axis.Item1[i];
                        //        gr.DrawLine(new Pen(pens[i].Color, faceBoxWidth), axis1[0], axis1[1], axis1[2], axis1[3]);
                        //    }
                        //}
                    }
                }
            }
            if (faceInfos.Any())
            {
                var fr = faceInfos.OrderBy(x => x.Rect.Left).ThenBy(z => z.Rect.Top).First();
                //pictureBox2.Invoke((Action)(() =>
                //{
                //    label4.Text = fr.Label;
                //    label4.BackColor = Color.LightGreen;
                //    pictureBox2.Image = BitmapConverter.ToBitmap(fr.Mat);
                //}));
            }

            return faceInfos.ToArray();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var sfr = listView1.SelectedItems[0].Tag as SearchFaceResult;
            //pictureBox1.Image = Bitmap.FromFile(sfr.FilePath);
            pictureBox1.Image = sfr.Target.ToBitmap();
        }

        List<RecFaceInfo> facesToSearch = new List<RecFaceInfo>();

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var r = listView2.SelectedItems[0].Tag as RecFaceInfo;
            if (!facesToSearch.Contains(r))
            {
                facesToSearch.Add(r);
                updateFacesToSearchList();
            }
        }

        private void updateFacesToSearchList()
        {
            listView3.Items.Clear();
            foreach (var item in facesToSearch)
            {
                listView3.Items.Add(new ListViewItem(new string[] { item.Label }) { Tag = item });
            }
        }
        bool stopSearch = false;
        private void button3_Click(object sender, EventArgs e)
        {
            stopSearch = true;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            faceRecognitionThreshold = (double)numericUpDown1.Value / 100.0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            textBox1.Text = new FileInfo(ofd.FileName).Directory.FullName;
        }
        bool includeVideos = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            includeVideos = checkBox1.Checked;
        }
        bool videoFramesLimitEnabled = false;
        int framesLimit = 150;

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            videoFramesLimitEnabled = checkBox2.Checked;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            framesLimit = (int)numericUpDown2.Value;
        }

        bool randomLimitMode = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            randomLimitMode = comboBox1.SelectedIndex == 1;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateFacesList();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var sfr = listView1.SelectedItems[0].Tag as SearchFaceResult;
            
            Process.Start(sfr.FilePath);
        }
    }
}
