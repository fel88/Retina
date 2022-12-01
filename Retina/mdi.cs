﻿using System;
using System.Windows.Forms;

namespace Retina
{
    public partial class mdi : Form
    {
        public mdi()
        {
            InitializeComponent();          
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
    }
}


