using System.Drawing;
using System.Windows.Forms;

namespace GridTableBuilder
{
    public partial class MainForm : Form
    {
        bool down;
        Point first = Point.Empty;
        Point current = Point.Empty;

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                down = true;
                current = first = e.Location;
                Invalidate();
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (down)
            {
                current = e.Location;
                Invalidate();
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (down)
            {
                down = false;
                first = current = e.Location;
                Invalidate();
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var gr = e.Graphics;
            gr.DrawLine(Pens.Black, first, current);
        }
    }
}
