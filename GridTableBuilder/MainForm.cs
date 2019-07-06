using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;

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

        enum SplitKind
        {
            None,
            Vertical,
            Horizontal
        }

        struct Line
        {
            public Point First { get; set; }
            public Point Last { get; set; }

            public bool IsEmpty { get { return First.IsEmpty && Last.IsEmpty; } }

            public static Line Empty { get { return new Line(); } }

            public Line Offset(int dx, int dy)
            {
                return new Line
                {
                    First = new Point(First.X + dx, First.Y + dy),
                    Last = new Point(Last.X + dx, Last.Y + dy)
                };
            }

            public override string ToString()
            {
                return $"{First} {Last}";
            }
        }

        class Edge
        {
            public PointNode First { get; set; } = new PointNode();
            public PointNode Last { get; set; } = new PointNode();

        }

        class PointNode
        {
            public Point Offset { get; set; }
            public List<Edge> Edges { get; set; } = new List<Edge>();
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
        SplitKind splitKind;
        List<int> vOffsets = new List<int>();
        List<int> hOffsets = new List<int>();
        int splitOffset;
        int splitOffsetIndex = -1;
        List<PointNode> pointNodes = new List<PointNode>();
        List<Edge> edges = new List<Edge>();

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

                if (drag)
                {
                    if (MouseInVSplit(e.Location))
                    {
                        var offset = hOffsets.Find(item => Math.Abs(item - (e.Location.X - tableRect.X)) <= epsilon);
                        splitOffsetIndex = hOffsets.IndexOf(offset);
                        var lp = tableRect.Location;
                        splitLine.First = new Point(lp.X + offset, lp.Y);
                        splitLine.Last = new Point(lp.X + offset, lp.Y + tableRect.Height);
                        splitKind = SplitKind.Vertical;
                    }
                    else if (MouseInHSplit(e.Location))
                    {
                        var offset = vOffsets.Find(item => Math.Abs(item - (e.Location.Y - tableRect.Y)) <= epsilon);
                        splitOffsetIndex = vOffsets.IndexOf(offset);
                        var lp = tableRect.Location;
                        splitLine.First = new Point(lp.X, lp.Y + offset);
                        splitLine.Last = new Point(lp.X + tableRect.Width, lp.Y + offset);
                        splitKind = SplitKind.Horizontal;
                    }
                    else
                    {
                        splitLine = Line.Empty;
                        splitKind = SplitKind.None;
                        splitOffsetIndex = -1;
                    }
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
                    case DrawMode.WaitRect:
                        var width = Math.Abs(firstPoint.X - e.X);
                        var heigth = Math.Abs(firstPoint.Y - e.Y);
                        var location = Point.Empty;
                        location.X = Math.Min(firstPoint.X, e.X);
                        location.Y = Math.Min(firstPoint.Y, e.Y);
                        selRect = new Rectangle(location, new Size(width, heigth));
                        break;
                    case DrawMode.Drag:
                        if (!splitLine.IsEmpty)
                        {
                            // перемещение вертикального разделителя
                            if (splitKind == SplitKind.Vertical)
                            {
                                var dx = e.X - dragPoint.X;
                                // защита зоны
                                if (dx != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < hOffsets.Count)
                                {
                                    var low = splitOffsetIndex > 0 
                                        ? tableRect.X + hOffsets[splitOffsetIndex - 1] : tableRect.X;
                                    var high = splitOffsetIndex < hOffsets.Count - 1
                                        ? tableRect.X + hOffsets[splitOffsetIndex + 1] : tableRect.X + tableRect.Width;
                                    var x = splitLine.Offset(dx, 0).First.X;
                                    if (x < low + epsilon * 2 || x > high - epsilon * 2)
                                        dx = 0;
                                }
                                splitLine = splitLine.Offset(dx, 0);
                            }
                            else if (splitKind == SplitKind.Horizontal) // перемещение горизонтального разделителя
                            {
                                var dy = e.Y - dragPoint.Y;
                                // защита зоны
                                if (dy != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < vOffsets.Count)
                                {
                                    var low = splitOffsetIndex > 0
                                        ? tableRect.Y + vOffsets[splitOffsetIndex - 1] : tableRect.Y;
                                    var high = splitOffsetIndex < vOffsets.Count - 1
                                        ? tableRect.Y + vOffsets[splitOffsetIndex + 1] : tableRect.Y + tableRect.Height;
                                    var y = splitLine.Offset(0, dy).First.Y;
                                    if (y < low + epsilon * 2 || y > high - epsilon * 2)
                                        dy = 0;
                                }
                                splitLine = splitLine.Offset(0, dy);
                            }
                        }
                        else
                            selRect.Offset(e.X - dragPoint.X, e.Y - dragPoint.Y);
                        dragPoint = e.Location;
                        break;
                }
                Invalidate();
            }
            else
            {
                // если курсор на рамке таблицы, покажем положение возможного разделителя
                if (drawMode == DrawMode.WaitLine && MouseInBorder(e.Location))
                {
                    Cursor = Cursors.Cross;
                    if (Math.Abs(tableRect.Y - e.Location.Y) <= epsilon ||
                        Math.Abs(tableRect.Y + tableRect.Height - e.Location.Y) <= epsilon) // check top or bottom line
                    {
                        splitLine.First = new Point(e.Location.X, tableRect.Y);
                        splitLine.Last = Point.Add(splitLine.First, new Size(0, tableRect.Height));
                        splitKind = SplitKind.Vertical;
                    }
                    else if (Math.Abs(tableRect.X - e.Location.X) <= epsilon ||
                             Math.Abs(tableRect.X + tableRect.Width - e.Location.X) <= epsilon) // check left or right line
                    {
                        splitLine.First = new Point(tableRect.X, e.Location.Y);
                        splitLine.Last = Point.Add(splitLine.First, new Size(tableRect.Width, 0));
                        splitKind = SplitKind.Horizontal;
                    }
                }
                else if (drawMode == DrawMode.WaitLine && MouseInVSplit(e.Location))
                {
                    Cursor = Cursors.VSplit;
                    splitKind = SplitKind.Vertical;
                    splitOffset = e.Location.X;
                }
                else if (drawMode == DrawMode.WaitLine && MouseInHSplit(e.Location))
                {
                    Cursor = Cursors.HSplit;
                    splitKind = SplitKind.Horizontal;
                    splitOffset = e.Location.Y;
                }
                else
                {
                    splitLine = Line.Empty;
                    splitKind = SplitKind.None;
                    splitOffset = 0;
                    Cursor = Cursors.Default;
                }
                Invalidate();
            }
        }

        private bool MouseInVSplit(Point location, float width = epsilon)
        {
            if (tableRect.IsEmpty) return false;
            if (hOffsets.Count == 0) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                var lp = tableRect.Location;
                foreach (var offset in hOffsets)
                {
                    grp.Reset();
                    grp.AddLine(new Point(lp.X + offset, lp.Y), new Point(lp.X + offset, lp.Y + tableRect.Height));
                    if (grp.IsOutlineVisible(location, pen))
                        return true;
                }
            }
            return false;
        }

        private bool MouseInHSplit(Point location, float width = epsilon)
        {
            if (tableRect.IsEmpty) return false;
            if (vOffsets.Count == 0) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                var lp = tableRect.Location;
                foreach (var offset in vOffsets)
                {
                    grp.Reset();
                    grp.AddLine(new Point(lp.X, lp.Y + offset), new Point(lp.X + tableRect.Width, lp.Y + offset));
                    if (grp.IsOutlineVisible(location, pen))
                        return true;
                }
            }
            return false;
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
                        // добавим данные разделителя в списки вертикальных и горизонтальных смещений
                        if (!splitLine.IsEmpty)
                        {
                            int offset;
                            List<int> list;
                            switch (splitKind)
                            {
                                case SplitKind.Vertical:
                                    offset = splitLine.First.X - tableRect.Location.X;
                                    list = new List<int>(hOffsets) { 0, tableRect.Width - 1 };
                                    // защита зоны при добавлении
                                    if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
                                    {
                                        hOffsets.Add(offset);
                                        hOffsets.Sort();
                                        BuildPointNodes();
                                    }
                                    break;
                                case SplitKind.Horizontal:
                                    offset = splitLine.First.Y - tableRect.Location.Y;
                                    list = new List<int>(vOffsets) { 0, tableRect.Height - 1 };
                                    // защита зоны при добавлении
                                    if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
                                    {
                                        vOffsets.Add(offset);
                                        vOffsets.Sort();
                                        BuildPointNodes();
                                    }
                                    break;
                            }
                        }
                        break;
                    case DrawMode.WaitRect:
                        if (tableRect.IsEmpty)
                        {
                            tableRect = selRect;
                            BuildPointNodes();
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
                        if (!splitLine.IsEmpty)
                        {
                            if (splitKind == SplitKind.Vertical &&
                                splitOffsetIndex >= 0 && splitOffsetIndex < hOffsets.Count)
                            {
                                hOffsets[splitOffsetIndex] = splitLine.First.X - tableRect.X;
                                BuildPointNodes();
                            }
                            else if (splitKind == SplitKind.Horizontal &&
                                splitOffsetIndex >= 0 && splitOffsetIndex < vOffsets.Count)
                            {
                                vOffsets[splitOffsetIndex] = splitLine.First.Y - tableRect.Y;
                                BuildPointNodes();
                            }
                            splitLine = Line.Empty;
                        }
                        else
                        {
                            tableRect.Location = selRect.Location;
                            selRect = Rectangle.Empty;
                        }
                        drawMode = DrawMode.WaitLine;
                        break;
                }
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right && drawMode == DrawMode.WaitLine)
            {
                // удаление разделителей под курсором
                if (MouseInVSplit(e.Location))
                {
                    var offset = e.Location.X - tableRect.X;
                    hOffsets.RemoveAll(item => Math.Abs(item - offset) <= epsilon);
                    BuildPointNodes();
                }
                else if (MouseInHSplit(e.Location))
                {
                    var offset = e.Location.Y - tableRect.Y;
                    vOffsets.RemoveAll(item => Math.Abs(item - offset) <= epsilon);
                    BuildPointNodes();
                }
                Invalidate();
            }
        }

        private void BuildPointNodes()
        {
            pointNodes.Clear();
            // добавление основных узловых точек на границах фигуры
            pointNodes.Add(new PointNode() { Offset = new Point(0, 0) });
            pointNodes.Add(new PointNode() { Offset = new Point(tableRect.Width, 0) });
            pointNodes.Add(new PointNode() { Offset = new Point(tableRect.Width, tableRect.Height) });
            pointNodes.Add(new PointNode() { Offset = new Point(0, tableRect.Height) });
            foreach (var ho in hOffsets)
            {
                pointNodes.Add(new PointNode() { Offset = new Point(ho, 0) });
                pointNodes.Add(new PointNode() { Offset = new Point(ho, tableRect.Height) });
            }
            foreach (var vo in vOffsets)
            {
                pointNodes.Add(new PointNode() { Offset = new Point(0, vo) });
                pointNodes.Add(new PointNode() { Offset = new Point(tableRect.Width, vo) });
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var gr = e.Graphics;
            if (!tableRect.IsEmpty)
            {
                using (var pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = DashStyle.Dot;
                    gr.DrawRectangle(pen, tableRect);
                    var lp = tableRect.Location;
                    // строим вертикальные разделители
                    foreach (var offset in hOffsets)
                        gr.DrawLine(pen, new Point(lp.X + offset, lp.Y), new Point(lp.X + offset, lp.Y + tableRect.Height));
                    // строим горизонтальные разделители
                    foreach (var offset in vOffsets)
                        gr.DrawLine(pen, new Point(lp.X, lp.Y + offset), new Point(lp.X + tableRect.Width, lp.Y + offset));
                }
                // рисуем узловые точки
                foreach (var np in pointNodes)
                {
                    var lp = tableRect.Location;
                    lp.Offset(np.Offset);
                    var rect = new Rectangle(lp, new Size(8, 8));
                    rect.Offset(-4, -4);
                    gr.FillEllipse(Brushes.Gray, rect);
                }
            }
            if (!splitLine.IsEmpty) // рисуем возможное положение разделителя
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
                        if (!splitLine.IsEmpty)
                            gr.DrawLine(pen, splitLine.First, splitLine.Last);
                        else
                            gr.DrawRectangle(pen, selRect);
                        break;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Text = drawMode.ToString();
        }
    }
}
