using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GridTableBuilder
{
    public partial class MainForm : Form
    {
        enum DrawMode
        {
            Line,
            Rect
        }

        bool down;
        Point firstPoint = Point.Empty;
        Point lastPoint = Point.Empty;
        Rectangle selRect = Rectangle.Empty;
        DrawMode drawMode = DrawMode.Rect;

        Rectangle tableRect = Rectangle.Empty;

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
                firstPoint = e.Location;
                switch (drawMode)
                {
                    case DrawMode.Line:
                        lastPoint = e.Location;
                        break;
                    case DrawMode.Rect:
                        selRect.Location = e.Location;
                        break;
                }
                Invalidate();
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (down)
            {
                switch (drawMode)
                {
                    case DrawMode.Line:
                        lastPoint = e.Location;
                        break;
                    case DrawMode.Rect:
                        var width = Math.Abs(firstPoint.X - e.X);
                        var heigth = Math.Abs(firstPoint.Y - e.Y);
                        var location = Point.Empty;
                        location.X = Math.Min(firstPoint.X, e.X);
                        location.Y = Math.Min(firstPoint.Y, e.Y);
                        selRect = new Rectangle(location, new Size(width, heigth));
                        break;
                }
                Invalidate();
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (down)
            {
                down = false;
                switch (drawMode)
                {
                    case DrawMode.Line:
                        firstPoint = lastPoint = e.Location;
                        break;
                    case DrawMode.Rect:
                        if (tableRect.IsEmpty)
                        {
                            tableRect = selRect;
                            selRect = Rectangle.Empty;
                            drawMode = DrawMode.Line;
                            firstPoint = lastPoint = Point.Empty;
                        }
                        else
                        {
                            drawMode = DrawMode.Rect;
                            selRect = tableRect = Rectangle.Empty;
                        }
                        break;
                }
                Invalidate();
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var gr = e.Graphics;
            if (!tableRect.IsEmpty)
            {
                gr.DrawRectangle(Pens.Black, tableRect);
            }
            using (var pen = new Pen(Color.Magenta))
            {
                pen.DashStyle = DashStyle.Dash;                     
                switch (drawMode)
                {
                    case DrawMode.Line:
                        gr.DrawLine(pen, firstPoint, lastPoint);
                        break;
                    case DrawMode.Rect:
                        gr.DrawRectangle(pen, selRect);
                        break;
                }
            }
        }
    }
}
