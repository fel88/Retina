using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class StereoCalibrate : Form
    {
        public StereoCalibrate()
        {
            InitializeComponent();
        }

        int chessW = 9;
        int chessH = 6;

        public class StereoPair
        {
            public Mat Left;
            public Mat Right;
        }

        private void NewImage(StereoPair pair)
        {
            listView1.Invoke((Action)(() =>
            {
                listView1.Items.Add(new ListViewItem(new string[] { DateTime.Now.ToLongTimeString(), "", "", "" }) { Tag = pair });
                pictureBox1.Image = pair.Left.ToBitmap();
                pictureBox2.Image = pair.Right.ToBitmap();

            }));
        }

        int resW = 1920;
        int resH = 1080;
        private void button2_Click(object sender, EventArgs e)
        {
            var cap1 = new VideoCapture((int)numericUpDown1.Value);
            var cap2 = new VideoCapture((int)numericUpDown2.Value);

            cap1.Set(VideoCaptureProperties.FrameWidth, resW);
            cap1.Set(VideoCaptureProperties.FrameHeight, resH);
            cap1.Set(VideoCaptureProperties.AutoFocus, 0);

            cap2.Set(VideoCaptureProperties.FrameWidth, resW);
            cap2.Set(VideoCaptureProperties.FrameHeight, resH);
            cap2.Set(VideoCaptureProperties.AutoFocus, 0);
            Mat left = new Mat();
            Mat right = new Mat();
            cap1.Read(left);
            cap2.Read(right);

            NewImage(new StereoPair() { Left = left, Right = right });
            pictureBox1.Image = left.ToBitmap();
            pictureBox2.Image = right.ToBitmap();

        }

        private async void toolStripButton2_Click(object sender, EventArgs e)
        {
            List<Point2f[]> imgpoints = new List<Point2f[]>();
            List<Point2f[]> imgpoints2 = new List<Point2f[]>();
            List<Point3f[]> objpoints = new List<Point3f[]>();
            List<Point3f> objp = new List<Point3f>();

            int patternw = chessW;
            int patternh = chessH;

            for (int j = 0; j < patternh; j++)
            {
                for (int ii = 0; ii < patternw; ii++)
                {
                    objp.Add(new Point3f(ii, j, 0) * (squareSize / 1000f));
                }
            }
            int imgw = 0;
            int imgh = 0;
            TermCriteria criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 100, 1e-5f);
            toolStripButton2.Enabled = false;
            await Task.Run(() =>
            {
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    toolStripStatusLabel1.Text = $"calibration: {i} / {listView1.Items.Count}";

                    var b = listView1.Items[i].Tag as StereoPair;
                    var left = b.Left;
                    var right = b.Right;
                    imgw = left.Width;
                    imgh = left.Height;
                    var leftg = left.CvtColor(ColorConversionCodes.BGR2GRAY);
                    var rightg = right.CvtColor(ColorConversionCodes.BGR2GRAY);
                    List<Point2f> corners_l = new List<Point2f>();
                    List<Point2f> corners_r = new List<Point2f>();
                    var out1_l = OutputArray.Create(corners_l);
                    var out1_r = OutputArray.Create(corners_r);
                    var res_l = Cv2.FindChessboardCorners(leftg, new OpenCvSharp.Size(9, 6), out1_l, ChessboardFlags.AdaptiveThresh | ChessboardFlags.FastCheck | ChessboardFlags.NormalizeImage);
                    var res_r = Cv2.FindChessboardCorners(rightg, new OpenCvSharp.Size(9, 6), out1_r, ChessboardFlags.AdaptiveThresh | ChessboardFlags.FastCheck | ChessboardFlags.NormalizeImage);



                    if (res_l && res_r)
                    {
                        var corners2 = Cv2.CornerSubPix(leftg, corners_l, new OpenCvSharp.Size(11, 11), new OpenCvSharp.Size(-1, -1), criteria);
                        var corners3 = Cv2.CornerSubPix(rightg, corners_r, new OpenCvSharp.Size(11, 11), new OpenCvSharp.Size(-1, -1), criteria);
                        objpoints.Add(objp.ToArray());

                        imgpoints.Add(corners2.ToArray());
                        imgpoints2.Add(corners3.ToArray());
                    }
                }
                Vec3d[] rvecs1;
                Vec3d[] rvecs2;
                Vec3d[] tvecs1;
                Vec3d[] tvecs2;
                var camMtx1 = new double[3, 3];
                var camMtx2 = new double[3, 3];
                var distCoeffs1 = new double[5];
                var distCoeffs2 = new double[5];
                var d = Cv2.CalibrateCamera(
                    objpoints.Select(z => z.ToList()).ToList(),
                    imgpoints.Select(z => z.ToList()).ToList(),
                    new OpenCvSharp.Size(imgw, imgh),
                     camMtx1, distCoeffs1, out rvecs1, out tvecs1, CalibrationFlags.None);

                var d2 = Cv2.CalibrateCamera(
                  objpoints.Select(z => z.ToList()).ToList(),
                  imgpoints2.Select(z => z.ToList()).ToList(),
                  new OpenCvSharp.Size(imgw, imgh),
                   camMtx2, distCoeffs2, out rvecs2, out tvecs2, CalibrationFlags.None);



                Mat R = new Mat();
                Mat T = new Mat();
                Mat E = new Mat();
                Mat F = new Mat();
                var ims = new OpenCvSharp.Size(imgw, imgh);
                Mat[] cameraMatrix = new Mat[2];
                Mat[] distCoeffs = new Mat[2];
            

                var rr = OpenCvSharp.Cv2.StereoCalibrate(objpoints,
                    imgpoints,
                    imgpoints2,
                    camMtx1,
                    distCoeffs1,
                    camMtx2,
                    distCoeffs2,
                 ims, R, T, E, F,
                 /*

                 CalibrationFlags.SameFocalLength |

                 CalibrationFlags.ZeroTangentDist |
                 CalibrationFlags.FixK3 |
                 CalibrationFlags.RationalModel |
                 CalibrationFlags.FixK4 |
                 CalibrationFlags.FixK5,*/criteria:
                 criteria);


                Mat R1 = new Mat();
                Mat R2 = new Mat();
                Mat P1 = new Mat();
                Mat P2 = new Mat();
                Mat Q = new Mat();

                Cv2.StereoRectify(
                    Mat.FromArray(camMtx1),
                    Mat.FromArray(distCoeffs1),
                    Mat.FromArray(camMtx2),
                    Mat.FromArray(distCoeffs2),
                    new OpenCvSharp.Size(imgw, imgh), R, T, R1, R2, P1, P2, Q);


                Cv2.InitUndistortRectifyMap(Mat.FromArray(camMtx1),
                    Mat.FromArray(distCoeffs1), R1, P1, ims, MatType.CV_32FC1, map1, map2);
                Cv2.InitUndistortRectifyMap(Mat.FromArray(camMtx2),
                    Mat.FromArray(distCoeffs2), R2, P2, ims, MatType.CV_32FC1, map11, map22);


            });
            toolStripButton2.Enabled = true;
            toolStripStatusLabel1.Text = "calibration complete";
        }

        Mat map1 = new Mat();
        Mat map2 = new Mat();
        Mat map11 = new Mat();
        Mat map22 = new Mat();
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count == 0) return;
                if (listView1.SelectedItems[0].Tag is StereoPair m)
                {
                    pictureBox1.Image = m.Left.ToBitmap();
                    pictureBox2.Image = m.Right.ToBitmap();
                    if (checkBox1.Checked)
                    {
                        var res = m.Left.Remap(map1, map2);
                        pictureBox1.Image = res.ToBitmap();
                        res = m.Right.Remap(map11, map22);
                        pictureBox2.Image = res.ToBitmap();
                    }                  

                }
            }
            catch (Exception ex)
            {

            }
        }

        private void saveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
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
                if (lvi.Tag is StereoPair sp)
                {
                    var fnn = Path.GetFileNameWithoutExtension(fn);
                    var ext = Path.GetExtension(fn);
                    if (string.IsNullOrEmpty(ext)) ext = ".png";
                    Mat dst = new Mat();
                    Cv2.HConcat(sp.Left, sp.Right, dst);
                    dst.SaveImage(Path.Combine(dir, fnn + index + ext));
                    index++;
                }
            }
            MessageBox.Show("files saved!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            for (int i = 0; i < ofd.FileNames.Length; i++)
            {
                var mat = Cv2.ImRead(ofd.FileNames[i]);

                var s = mat.Width / 2;
                var left = new Mat(mat, new Rect(0, 0, s, mat.Height));
                var right = new Mat(mat, new Rect(s, 0, s, mat.Height));

                var sp = new StereoPair() { Left = left, Right = right };
                listView1.Items.Add(new ListViewItem(new string[] {
                    DateTime.Now.ToLongTimeString(), "", "", "" })
                {
                    Tag = sp
                });
            }
        }

        int squareSize = 50;//50mm
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            squareSize = (int)numericUpDown3.Value;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.Save("temp.jpg");
            Process.Start("temp.jpg");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBox2.Image.Save("temp.jpg");
            Process.Start("temp.jpg");
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var left = (pictureBox1.Image as Bitmap).ToMat();
            var right = (pictureBox2.Image as Bitmap).ToMat();
            Mat dst = new Mat();
            Cv2.HConcat(left, right, dst);
            if (checkBox2.Checked)
                for (int i = 0; i < left.Height; i += 50)
                {
                    dst.Line(0, i, dst.Width, i, Scalar.Red);
                }
            dst.SaveImage("combined.jpg");
            Process.Start("combined.jpg");

        }
    }
}
