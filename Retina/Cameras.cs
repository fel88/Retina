using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Retina
{
    public partial class Cameras : Form
    {
        public Cameras()
        {
            InitializeComponent();
            listView1.ShowItemToolTips = true;
            listView1.MouseDoubleClick += ListView1_MouseDoubleClick;
            UpdateList();
        }

        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!PickMode) 
                return;

            if (listView1.SelectedItems.Count == 0)
                return;

            var vs = listView1.SelectedItems[0].Tag as IVideoSource;
            Picked = vs;
            Close();
            DialogResult = DialogResult.OK;
        }
        public IVideoSource Picked;

        public static List<IVideoSource> VideoSources = new List<IVideoSource>();

        public bool PickMode { get; internal set; }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraEditDialog ced = new CameraEditDialog();
            if (ced.ShowDialog() == DialogResult.OK)
            {
                VideoSources.Add(ced.VideoSource);
                UpdateList();
            }
        }
        public void UpdateList()
        {
            listView1.Items.Clear();
            foreach (var item in VideoSources)
            {
                listView1.Items.Add(new ListViewItem(new string[] { item.Name, item.Description, item.GetType().Name }) { Tag = item, ToolTipText = item.Description });
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var vs = listView1.SelectedItems[0].Tag as IVideoSource;
            VideoSources.Remove(vs);
            UpdateList();
        }
    }
}
