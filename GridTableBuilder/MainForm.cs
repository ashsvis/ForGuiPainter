using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace GridTableBuilder
{
    public partial class MainForm : Form
    {
        Grid grid;

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            grid = new Grid() { Area = new Rectangle(200, 50, 400, 300) };
            grid.Init();
            Text = $"Nodes: {grid.Nodes.Count}, Edges: {grid.Edges.Count}";
            FillTreeView();

            grid.CyclesSearch();
            FillCyclesList();
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                grid.OnLeftMouseDown(e.Location);
                Invalidate();
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            grid.OnMouseMove(e.Location);
            Invalidate();
            if (grid.WorkMode == GridWorkMode.Move)
            {
                if (e.Button == MouseButtons.Left)
                    Cursor = grid.SplitKind == SplitKind.Horizontal
                        ? Cursors.HSplit : grid.SplitKind == SplitKind.Vertical ? Cursors.VSplit : Cursors.Default;
                else
                    Cursor = grid.MouseInVSplit(e.Location) ? Cursors.HSplit : grid.MouseInHSplit(e.Location) ? Cursors.VSplit : Cursors.Default;
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            grid.OnLeftMouseUp(e.Location);
            Invalidate();
            Text = $"Nodes: {grid.Nodes.Count}, Edges: {grid.Edges.Count}";
            FillTreeView();

            grid.SelectedCycle = new int[] { };
            grid.CyclesSearch();
            FillCyclesList();
        }

        class Cycle
        {
            public int[] Indexes { get; set; }

            public Cycle(int[] items)
            {
                Indexes = new List<int>(items).ToArray();
            }

            public override string ToString()
            {
                return string.Join("-", Indexes);
            }
        }

        private void FillCyclesList()
        {
            listBox1.Items.Clear();
            foreach (var item in grid.CatalogCycles.Values)
                listBox1.Items.Add(new Cycle(item));
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            if (_image != null)
                e.Graphics.DrawImage(_image, grid.Area.Location);

            grid.OnPaint(e.Graphics);
        }

        private void FillTreeView()
        {
            try
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
                foreach (var pn in grid.Nodes)
                {
                    var nd = new TreeNode($"p{pn.Index}");
                    treeView1.Nodes.Add(nd);
                    foreach (var ed in pn.Edges)
                    {
                        var name1 = ed.Node1 != null ? $"p{ed.Node1.Index}" : "?";
                        var name2 = ed.Node2 != null ? $"p{ed.Node2.Index}" : "?";
                        nd.Nodes.Add($"e{ed.Index} ({name1},{name2})");
                    }
                }
                treeView1.ExpandAll();
            }
            finally
            {
                treeView1.EndUpdate();
            }
        }

        private void rbCreate_CheckedChanged(object sender, EventArgs e)
        {
            if (rbCreate.Checked)
                grid.WorkMode = GridWorkMode.Draw;
            else if (rbMove.Checked)
                grid.WorkMode = GridWorkMode.Move;
            else if (rbDelete.Checked)
                grid.WorkMode = GridWorkMode.Erase;
            Invalidate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            grid.ShowNodeNames = ((CheckBox)sender).Checked;
            Invalidate();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            grid.ShowEdgeNames = ((CheckBox)sender).Checked;
            Invalidate();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            treeView1.Visible = ((CheckBox)sender).Checked;
        }

        private void anglePanel1_OnAngleChange(object sender, EventArgs e)
        {
            textBox1.Text = anglePanel1.Angle.ToString("0");
            textBox2.Text = anglePanel1.Altitude.ToString("0");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            grid.ShowNodeNames = checkBox1.Checked;

            anglePanel1_OnAngleChange(anglePanel1, new EventArgs());
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            anglePanel1.Enabled = ((CheckBox)sender).Checked;
        }

        /// <source>
        /// http://www.cyberforum.ru/csharp-net/thread522535.html#post3956206
        /// </source>
        public static Image EraseFon(Image original, byte deviation = 128)
        {
            Bitmap myImage = new Bitmap(original);

            BitmapData imageData = myImage.LockBits(
                                        new Rectangle(0, 0, myImage.Width, myImage.Height),
                                        ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = imageData.Stride;
            IntPtr Scan0 = imageData.Scan0;
            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                int nOffset = stride - myImage.Width * 4;
                int nWidth = myImage.Width;
                for (int y = 0; y < myImage.Height; y++)
                {
                    for (int x = 0; x < nWidth; x++)
                    {
                        //p[0] =... // задаём синий
                        //p[1] =... // задаём зелёный
                        //p[2] =... // задаём красный
                        p[3] = deviation; // задаём альфа канал 0 - полностью прозрачный
                        p += 4;
                    }
                    p += nOffset;
                }
            }
            myImage.UnlockBits(imageData);
            return (Image)myImage;
        }

        private Image _image;

        private void button1_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = @"*Файлы графических форматов (.png;*.jpg;*.bmp;*.gif)|*.png;*.jpg;*.bmp;*.gif"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var original = (Bitmap)Image.FromFile(dlg.FileName);
            _image = EraseFon(original);
            var location = grid.Area.Location;
            grid.Area = new Rectangle(location, new Size(_image.Width, _image.Height));
            grid.Init();
            Invalidate();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cycle = listBox1.SelectedItem as Cycle;
            grid.SelectedCycle = cycle != null ? cycle.Indexes : new int[] { };
            Invalidate();
        }
    }

}
