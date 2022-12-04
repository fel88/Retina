using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UniCutSheetRecognizerPlugin
{
    public partial class CalibratingWindow : Form
    {

        public CalibratingWindow()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;

            FormClosing += CalibrateWindow_FormClosing;
        }


        double calibrationAccuracy;

        
        private string doubleToString(double d)
        {
            if (double.IsNaN(d)) return string.Empty;
            return d.ToString(CultureInfo.InvariantCulture);
        }


        
        private void CalibrateWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Tag is Bitmap bmp)
                {
                    bmp.Dispose();
                }
            }
        }

        private void NewImage(Mat obj)
        {
            listView1.Invoke((Action)(() =>
            {
                listView1.Items.Add(new ListViewItem(new string[] { DateTime.Now.ToLongTimeString(), "", "", "" }) { Tag = obj });
                pictureBox1.Image = obj.ToBitmap();

            }));
        }

        Mat projMtx = null;
        private void tsbRecalibrate_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == 0)
            {
                MessageBox.Show("minimum two images required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<Point2f[]> imgpoints = new List<Point2f[]>();
            List<Point3f[]> objpoints = new List<Point3f[]>();

            int patternw = chessW;
            int patternh = chessH;
            List<Point3f> objp = new List<Point3f>();
            for (int j = 0; j < patternh; j++)
            {
                for (int i = 0; i < patternw; i++)
                {
                    objp.Add(new Point3f(i, j, 0));
                }
            }
            TermCriteria criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.001);

            int imgw = 0;
            int imgh = 0;
            int cnt = 0;
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                var item = listView1.Items[i];

                var b = listView1.Items[i].Tag as Mat;

                imgw = b.Width;
                imgh = b.Height;
                var mat = b;

                var gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
                List<Point2f> corners = new List<Point2f>();
                var out1 = OutputArray.Create(corners);


                bool res = false;

                if (comboBox1.SelectedIndex == 0)
                {
                    res = Cv2.FindChessboardCorners(gray, new OpenCvSharp.Size(chessW, chessH), out1);
                }
                if (comboBox1.SelectedIndex == 1)
                {
                    res = Cv2.FindCirclesGrid(gray, new OpenCvSharp.Size(chessW, chessH), out1);
                }
                item.SubItems[1].Text = res ? "pattern found" : "pattern not found";
                item.BackColor = res ? Color.LightBlue : Color.Pink;
                if (res)
                {
                    var corners2 = Cv2.CornerSubPix(gray, corners, new OpenCvSharp.Size(11, 11), new OpenCvSharp.Size(-1, -1), criteria);
                    objpoints.Add(objp.ToArray());
                    imgpoints.Add(corners2.ToArray());
                    cnt++;
                }
            }
            if (imgpoints.Count == 0)
            {
                MessageBox.Show("calibration can't be done: no one pattern was found. ", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cnt < 5)
            {
                MessageBox.Show("calibration can't be done: minimum 5 images required. ", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Vec3d[] rvecs;
            Vec3d[] tvecs;
            camMtx = new double[3, 3];
            distCoeffs = new double[5];
            var d = Cv2.CalibrateCamera(
                objpoints.Select(z => z.ToList()).ToList(),
                imgpoints.Select(z => z.ToList()).ToList(),
                new OpenCvSharp.Size(imgw, imgh),
                 camMtx, distCoeffs, out rvecs, out tvecs, CalibrationFlags.None);



            /*If you are using cameraCalibrate(), you must be getting mtx, rvecs and tvecs. 
             * R is 3x1 which you need to convert to 3x3 using Rodrigues method of opencv.
             
             R = cv2.Rodrigues(rvecs[0])[0]
t = tvecs[0]
Rt = np.concatenate([R,t], axis=-1) # [R|t]
P = np.matmul(mtx,Rt) # A[R|t]*/
            double[] outr = null;
            double[,] jac = null;
            Mat outr2 = new Mat();
            Cv2.Rodrigues(rvecs[0], outr2);

            var R = outr2;
            var t = tvecs[0];
            var Rt = concat(R, t);
            projMtx = Matmul(camMtx, Rt);

            // error calc
            double mean_error = 0;

            for (int i = 0; i < objpoints.Count; i++)
            {
                Mat imgpoints2 = new Mat();
                var rvec1 = rvecs.Select(z => new double[] { z.Item0, z.Item1, z.Item2 }).ToArray();
                var tvec1 = tvecs.Select(z => new double[] { z.Item0, z.Item1, z.Item2 }).ToArray();

                Cv2.ProjectPoints(InputArray.Create(objpoints[i]), InputArray.Create(rvec1[i]), InputArray.Create(tvec1[i]), InputArray.Create(camMtx), InputArray.Create(distCoeffs), imgpoints2);
                var error = Cv2.Norm(InputArray.Create(imgpoints[i]), InputArray.Create(imgpoints2), NormTypes.L2) / imgpoints2.Width;
                mean_error += error;
            }

            double calibAccuracy = mean_error / objpoints.Count;
            calibrationAccuracy = calibAccuracy;
            lCalibAccuracy.Text = "calibration error: " + doubleToString(calibAccuracy);
            MessageBox.Show("calibration succesfully done.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Mat Matmul(double[,] camMtx, Mat rt)
        {
            Mat cam = new Mat(camMtx.GetLength(0), camMtx.GetLength(1), rt.Type(), camMtx);
            var res = cam * rt;
            return res;
        }

        private Mat concat(Mat r, Vec3d t)
        {
            Mat dst = new Mat();
            Mat mt = new Mat(3, 1, r.Type(), new double[] { t.Item0, t.Item1, t.Item2 });
            Cv2.HConcat(r, mt, dst);
            return dst;
        }

        double[,] camMtx;
        double[] distCoeffs;

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                Mat mat = new Mat();
                if (cap != null)
                {
                    cap.Read(mat);

                    NewImage(mat);
                }
            });


        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count == 0) return;
                if (listView1.SelectedItems[0].Tag is Mat m)
                {
                    pictureBox1.Image = m.ToBitmap();
                    if (camMtx != null)
                    {
                        var mat = m;
                        Rect roi;
                        var c = Cv2.GetOptimalNewCameraMatrix(camMtx, distCoeffs, mat.Size(), 1, mat.Size(), out roi);
                        if (roi.Width == 0 || roi.Height == 0)
                        {
                            MessageBox.Show("ROI error.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        var dst = mat.Clone();
                        Cv2.Undistort(mat, dst, InputArray.Create(camMtx), InputArray.Create(distCoeffs), InputArray.Create(c));
                        dst = dst.Clone(roi);
                        pictureBox2.Image = BitmapConverter.ToBitmap(dst);
                    }

                }
            }
            catch (Exception ex)
            {

            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        double[] lensCorrectionDist;
        double[,] lensCorrectionMtx;
        private void tsbSaveMatrix_Click(object sender, EventArgs e)
        {
            if (camMtx == null || distCoeffs == null)
            {
                MessageBox.Show("calibrate required first.", Text);
                return;
            }

            lensCorrectionDist = distCoeffs;
            lensCorrectionMtx = camMtx;
            StringBuilder sb = new StringBuilder();
            var mat = listView1.Items[0].Tag as Mat;
            sb.AppendLine("[width]");
            sb.AppendLine(mat.Width.ToString());
            sb.AppendLine("[height]");
            sb.AppendLine(mat.Height.ToString());
            sb.AppendLine("[distortion]");
            foreach (var item in lensCorrectionDist)
            {
                sb.Append(item + " ");
            }
            sb.AppendLine();
            sb.AppendLine("[distortion_mtx]");
            foreach (var item in lensCorrectionMtx)
            {
                sb.Append(item + " ");
            }

            sb.AppendLine();
            sb.AppendLine("[camera]");
            for (int i = 0; i < camMtx.GetLength(0); i++)
            {
                for (int j = 0; j < camMtx.GetLength(1); j++)
                {
                    var p = camMtx[i, j];
                    sb.Append(p + " ");
                }
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("[projection]");
            for (int i = 0; i < projMtx.Rows; i++)
            {
                for (int j = 0; j < projMtx.Cols; j++)
                {
                    var p = projMtx.At<double>(i, j);
                    sb.Append(p + " ");
                }
                sb.AppendLine();
            }
            Clipboard.SetText(sb.ToString());

            MessageBox.Show("koef saved to clipboard.", Text);
        }


        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedPhotos();
        }

        private void сохранитьВБуфферОбменаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var item = listView1.SelectedItems[0];

            if (item.Tag is Bitmap bmp)
            {
                Clipboard.SetImage(bmp);
            }
        }

        private void загрузитьИзФайлаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != DialogResult.OK) return;
            for (int i = 0; i < ofd.FileNames.Length; i++)
            {
                var bmp = Cv2.ImRead(ofd.FileNames[i]);
                listView1.Items.Add(new ListViewItem(new string[] { DateTime.Now.ToLongTimeString(), "", "", "" }) { Tag = bmp });
            }

        }

        private void сохранитьВыбранныеВФайлыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == 0)
            {
                MessageBox.Show("there are not pictures.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var fn = sfd.FileName;
            int index = 0;

            var dir = Path.GetDirectoryName(fn);
            foreach (var item in listView1.Items)
            {
                var lvi = item as ListViewItem;
                if (lvi.Tag is Mat bmp)
                {
                    var fnn = Path.GetFileNameWithoutExtension(fn);
                    var ext = Path.GetExtension(fn);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";

                    bmp.SaveImage(Path.Combine(dir, fnn + index + ext));
                    index++;
                }
            }
            MessageBox.Show("files saved!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void очиститьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteAllPhotos();
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedPhotos();
            }
        }
        
        private void DeleteSelectedPhotos()
        {
            try
            {
                if (listView1.SelectedItems.Count == 0) return;
                var items = listView1.SelectedItems;
                foreach (var item in items)
                {
                    ListViewItem lvItem = (ListViewItem)item;
                    listView1.Items.Remove(lvItem);
                    if (lvItem.Tag is Bitmap bmp)
                    {
                        bmp.Dispose();
                    }
                    pictureBox1.Image = null;
                    pictureBox2.Image = null;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void DeleteAllPhotos()
        {
            List<Bitmap> bmps = new List<Bitmap>();
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                var item = listView1.Items[i];
                if (item.Tag is Bitmap bmp)
                {
                    bmps.Add(bmp);
                }
            }

            pictureBox1.Image = null;
            pictureBox2.Image = null;

            listView1.Items.Clear();
            foreach (var bitem in bmps)
            {
                bitem.Dispose();
            }
        }
        

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;
        }
        VideoCapture cap = null;
        int resW = 1920;
        int resH = 1080;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (cap == null)
            {
                cap = new VideoCapture(0);
                cap.Set(VideoCaptureProperties.FrameWidth, resW);
                cap.Set(VideoCaptureProperties.FrameHeight, resH);

                /*cap.Set(VideoCaptureProperties.FrameWidth, 800);
                cap.Set(VideoCaptureProperties.FrameHeight, 600);*/
                cap.Set(VideoCaptureProperties.FourCC, FourCC.MJPG);
            }

            Mat mat = new Mat();
            var r = cap.Read(mat);
            if (!r)
            {
                cap = null;
                return;
            }
            var gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            List<Point2f> corners = new List<Point2f>();
            var out1 = OutputArray.Create(corners);
            var res = Cv2.FindChessboardCorners(gray, new OpenCvSharp.Size(chessW, chessH), out1, ChessboardFlags.AdaptiveThresh | ChessboardFlags.FastCheck | ChessboardFlags.NormalizeImage);

            //calc X,Y, size,  skew
            var tmat = mat.Clone();
            if (res)
            {
                var hull = Cv2.ConvexHull(corners);

                tmat.DrawContours(new[] { hull.Select(z => new OpenCvSharp.Point(z.X, z.Y)).ToArray() }, 0, Scalar.Green);
                int row = 0;
                int cntr = 0;
                Scalar[] colors = new[] { Scalar.Blue, Scalar.Red, Scalar.Green, Scalar.Yellow, Scalar.Violet };
                //Cv2.DrawChessboardCorners(tmat, new OpenCvSharp.Size(chessW, chessH), corners, true);
                foreach (var item in corners)
                {

                    if (cntr % chessW == 0)
                    {
                        row++;
                    }
                    cntr++;

                    tmat.Circle((int)item.X, (int)item.Y, 10, colors[row % colors.Length], 2);
                }
            }
            pictureBox1.Image = tmat.ToBitmap();
        }

        int chessW = 9;
        int chessH = 6;

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            chessW = (int)numericUpDown1.Value;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                resW = 640;
                resH = 480;
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                resW = 800;
                resH = 600;
            }
            else if (comboBox2.SelectedIndex == 2)
            {
                resW = 1920;
                resH = 1080;
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            chessH = (int)numericUpDown2.Value;
        }
    }
}