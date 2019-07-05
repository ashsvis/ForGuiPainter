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
            Rect,
            Line,
            Drag
        }

        bool down;
        Point firstPoint;
        Point lastPoint;
        Rectangle selRect;
        DrawMode drawMode;
        Rectangle tableRect;
        bool drag;
        Point dragPoint;

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
                firstPoint = lastPoint = dragPoint = e.Location;
                drag = !tableRect.IsEmpty && tableRect.Contains(e.Location);
                drawMode = drag ? DrawMode.Drag : tableRect.IsEmpty ? DrawMode.Rect : DrawMode.Line;
                selRect.Location = drag ? tableRect.Location : e.Location;
                selRect.Size = drag ? tableRect.Size : Size.Empty;
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
                    case DrawMode.Drag:
                        selRect.Offset(e.X - dragPoint.X, e.Y - dragPoint.Y);
                        dragPoint = e.Location;
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
                    case DrawMode.Drag:
                        tableRect.Location = selRect.Location;
                        selRect = Rectangle.Empty;
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
                    case DrawMode.Drag:
                        gr.DrawRectangle(pen, selRect);
                        break;
                }
            }
        }
    }
}
