using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Retina
{
    public partial class FaceRecognition : Form
    {
        FaceNet net = new FaceNet();
        public FaceRecognition()
        {
            InitializeComponent();
            updateList();
        }

        private void addPersonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddReferenceFace a = new AddReferenceFace();
            if (a.ShowDialog() != DialogResult.OK) return;
            FaceNet ff = new FaceNet();
            FaceNet.Faces.Add(new RecFaceInfo() { Label = a.Label, Face = a.Mat, Embedding = ff.Inference(a.Mat) });
            updateList();

        }
        void updateList()
        {
            listView1.Items.Clear();
            foreach (var item in FaceNet.Faces)
            {
                listView1.Items.Add(new ListViewItem(new string[] { item.Label }) { Tag = item });
            }
        }

        private void loadFaceDatasetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists("faces")) return;
            var dir = new DirectoryInfo("faces");
            var embs = dir.GetFiles("*.emb").ToArray();
            FaceNet.Faces.Clear();
            foreach (var item in embs)
            {
                var nm = Path.GetFileNameWithoutExtension(item.Name);
                RecFaceInfo f = new RecFaceInfo();
                f.Embedding = loadArray(item.FullName);
                f.Label = File.ReadAllText(Path.Combine("faces", nm + ".label"));
                f.Face = OpenCvSharp.Cv2.ImRead(Path.Combine("faces", nm + ".png"));
                FaceNet.Faces.Add(f);

            }
            updateList();
        }

        private float[] loadArray(string fullName)
        {
            List<float> ret = new List<float>();
            var bb = File.ReadAllBytes(fullName);
            for (int i = 0; i < bb.Length; i += 4)
            {
                ret.Add(BitConverter.ToSingle(bb, i));
            }
            return ret.ToArray();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var ff = listView1.SelectedItems[0].Tag as RecFaceInfo;
            pictureBox1.Image = BitmapConverter.ToBitmap(ff.Face);

        }

        private void saveFacesDatasetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("faces");
            int index = 0;
            foreach (var item in FaceNet.Faces)
            {
                var p = Path.Combine("faces", index + ".png");
                item.Face.SaveImage(p);
                File.WriteAllText(Path.Combine("faces", index + ".label"), item.Label);
                File.WriteAllBytes(Path.Combine("faces", index + ".emb"), getBytes(item.Embedding));
                index++;
            }
        }
        byte[] getBytes(float[] array)
        {
            List<byte> dd = new List<byte>();
            foreach (var item in array)
            {
                dd.AddRange(BitConverter.GetBytes(item));
            }
            return dd.ToArray();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var ff = listView1.SelectedItems[0].Tag as RecFaceInfo;
            if (MessageBox.Show($"Are you sure to delete: {ff.Label}?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                ff = listView1.SelectedItems[i].Tag as RecFaceInfo;
                FaceNet.Faces.Remove(ff);
            }
          
            pictureBox1.Image = null;
            updateList();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"Are you sure to delete all?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            FaceNet.Faces.Clear();
            pictureBox1.Image = null;
            updateList();
        }

        private void fromScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var img = Clipboard.GetImage() as Bitmap;
            var mat = img.ToMat();
            pictureBox1.Image = img;            
            var best = net.Recognize(mat);
            if (best != null && best.Item1 != null)
            {
                MessageBox.Show(best.Item1.Label);
            }
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            pictureBox1.Image = Bitmap.FromFile(ofd.FileName);
            var mat = OpenCvSharp.Cv2.ImRead(ofd.FileName);
            var best = net.Recognize(mat);
            if (best != null && best.Item1 != null)
            {
                MessageBox.Show(best.Item1.Label);
            }
        }
    }
}