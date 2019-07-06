using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GridTableBuilder
{
    public partial class MainForm : Form
    {
        const float epsilon = 3f;

        enum DrawMode
        {
            WaitRect,
            WaitLine,
            Drag
        }

        struct Line
        {
            public Point First { get; set; }
            public Point Last { get; set; }

            public bool IsEmpty
            {
                get { return First.IsEmpty && Last.IsEmpty; }
                set { First = Last = Point.Empty; }
            }
        }

        bool down;
        Point firstPoint;
        Point lastPoint;
        Rectangle selRect;
        DrawMode drawMode;
        Rectangle tableRect;
        bool drag;
        Point dragPoint;
        Line splitLine;

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
                var rect = tableRect;
                rect.Inflate(-2, -2);
                drag = !tableRect.IsEmpty && rect.Contains(e.Location);
                drawMode = drag ? DrawMode.Drag : tableRect.IsEmpty ? DrawMode.WaitRect : DrawMode.WaitLine;
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
                    //case DrawMode.WaitLine:
                    //    lastPoint = e.Location;
                    //    break;
                    case DrawMode.WaitRect:
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
            else
            {
                // курсор на рамке таблицы, покажем положение возможного разделителя
                if (drawMode == DrawMode.WaitLine && MouseInBorder(e.Location))
                {
                    Cursor = Cursors.Cross;
                    if (Math.Abs(tableRect.Y - e.Location.Y) <= epsilon ||
                        Math.Abs(tableRect.Y + tableRect.Height - e.Location.Y) <= epsilon) // check top or bottom line
                    {
                        splitLine.First = new Point(e.Location.X, tableRect.Y);
                        splitLine.Last = Point.Add(splitLine.First, new Size(0, tableRect.Height));
                    }
                    else if (Math.Abs(tableRect.X - e.Location.X) <= epsilon ||
                             Math.Abs(tableRect.X + tableRect.Width - e.Location.X) <= epsilon) // check left or right line
                    {
                        splitLine.First = new Point(tableRect.X, e.Location.Y);
                        splitLine.Last = Point.Add(splitLine.First, new Size(tableRect.Width, 0));
                    }
                }
                else
                {
                    splitLine.IsEmpty = true;
                    Cursor = Cursors.Default;
                }
                Invalidate();
            }
        }

        private bool MouseInBorder(Point location, float width = epsilon)
        {
            if (tableRect.IsEmpty) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                grp.AddRectangle(tableRect);
                return grp.IsOutlineVisible(location, pen);
            }
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (down)
            {
                down = false;
                switch (drawMode)
                {
                    case DrawMode.WaitLine:
                        firstPoint = lastPoint = e.Location;
                        break;
                    case DrawMode.WaitRect:
                        if (tableRect.IsEmpty)
                        {
                            tableRect = selRect;
                            selRect = Rectangle.Empty;
                            drawMode = DrawMode.WaitLine;
                            firstPoint = lastPoint = Point.Empty;
                        }
                        else
                        {
                            drawMode = DrawMode.WaitRect;
                            selRect = tableRect = Rectangle.Empty;
                        }
                        break;
                    case DrawMode.Drag:
                        tableRect.Location = selRect.Location;
                        selRect = Rectangle.Empty;
                        drawMode = DrawMode.WaitLine;
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
                using (var pen = new Pen(Color.Black, 1))
                    gr.DrawRectangle(pen, tableRect);
            }
            if (!splitLine.IsEmpty)
            {
                using (var pen = new Pen(Color.Magenta, 1))
                {
                    pen.DashStyle = DashStyle.Dash;
                    gr.DrawLine(pen, splitLine.First, splitLine.Last);
                }
            }
            using (var pen = new Pen(Color.Magenta))
            {
                pen.DashStyle = DashStyle.Dash;                     
                switch (drawMode)
                {
                    case DrawMode.WaitLine:
                        gr.DrawLine(pen, firstPoint, lastPoint);
                        break;
                    case DrawMode.WaitRect:
                    case DrawMode.Drag:
                        gr.DrawRectangle(pen, selRect);
                        break;
                }
            }
        }
    }
}
