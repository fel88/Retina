using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using UniCutSheetRecognizerPlugin;

namespace Retina
{
    public partial class mdi : Form
    {
        public mdi()
        {
            InitializeComponent();
            FormClosing += Mdi_FormClosing;
            Load += Mdi_Load;
        }

        private void Mdi_Load(object sender, EventArgs e)
        {
            RestoreCameras();
        }

        private void RestoreCameras()
        {
            if (!File.Exists("cameras.xml"))
                return;

            var doc = XDocument.Load("cameras.xml");
            foreach (var item in doc.Root.Elements())
            {
                IVideoSource vs = null;
                if (item.Name == "rtsp")
                {
                    var rtsp = new RtspCamera();
                    vs = rtsp;
                    vs.RestoreXml(item);

                }
                else if (item.Name == "file")
                {
                    var rtsp = new VideoFile();
                    vs = rtsp;
                    vs.RestoreXml(item);

                }
                else if (item.Name == "usbCam")
                {
                    var rtsp = new UsbWebCam();
                    vs = rtsp;
                    vs.RestoreXml(item);
                }
                if (vs != null)
                    Cameras.VideoSources.Add(vs);
            }
        }

        private void Mdi_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCameras();
        }

        public void SaveCameras()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<root>");
            foreach (var item in Cameras.VideoSources)
            {
                item.StoreXml(sb);
            }
            sb.AppendLine("</root>");
            File.WriteAllText("cameras.xml", sb.ToString());
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.MdiParent = this;
            f.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Surv s = new Surv();
            s.MdiParent = this;
            s.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            FaceRecognition ff = new FaceRecognition();
            ff.MdiParent = this;
            ff.Show();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            mixer sf = new mixer();
            sf.MdiParent = this;
            sf.Show();
        }

        private void facesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SearchFaces sf = new SearchFaces();
            sf.MdiParent = this;
            sf.Show();
        }

        private void imageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageSearch sf = new ImageSearch();
            sf.MdiParent = this;
            sf.Show();
        }

        private void toolStripButton4_Click_1(object sender, EventArgs e)
        {
            CalibratingWindow sf = new CalibratingWindow();
            sf.MdiParent = this;
            sf.Show();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            StereoCalibrate sf = new StereoCalibrate();
            sf.MdiParent = this;
            sf.Show();
        }

        private void hikvisionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PreviewDemo.Preview p = new PreviewDemo.Preview();
            p.MdiParent = this;
            p.Show();
        }

        private void mdi_Load(object sender, EventArgs e)
        {

        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Cameras cc = new Cameras();
            cc.MdiParent = this;
            cc.Show();
        }
    }
}


