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
        Rectangle table;
        bool drag;
        Point dragPoint;
        Line splitLine;
        SplitKind splitKind;
        List<int> verticals = new List<int>();
        List<int> horizontals = new List<int>();
        int splitOffset;
        int splitOffsetIndex = -1;
        List<PointNode> nodes = new List<PointNode>();
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
                var rect = table;
                rect.Inflate(-2, -2);
                drag = !table.IsEmpty && rect.Contains(e.Location);
                drawMode = drag ? DrawMode.Drag : table.IsEmpty ? DrawMode.WaitRect : DrawMode.WaitLine;
                selRect.Location = drag ? table.Location : e.Location;
                selRect.Size = drag ? table.Size : Size.Empty;

                if (drag)
                {
                    if (MouseInVSplit(e.Location))
                    {
                        var offset = horizontals.Find(item => Math.Abs(item - (e.Location.X - table.X)) <= epsilon);
                        splitOffsetIndex = horizontals.IndexOf(offset);
                        var lp = table.Location;
                        splitLine.First = new Point(lp.X + offset, lp.Y);
                        splitLine.Last = new Point(lp.X + offset, lp.Y + table.Height);
                        splitKind = SplitKind.Vertical;
                    }
                    else if (MouseInHSplit(e.Location))
                    {
                        var offset = verticals.Find(item => Math.Abs(item - (e.Location.Y - table.Y)) <= epsilon);
                        splitOffsetIndex = verticals.IndexOf(offset);
                        var lp = table.Location;
                        splitLine.First = new Point(lp.X, lp.Y + offset);
                        splitLine.Last = new Point(lp.X + table.Width, lp.Y + offset);
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
                    case DrawMode.WaitLine:
                        // рисуем линию, только если внутри области
                        if (table.Contains(firstPoint))
                            lastPoint = e.Location;
                        break;
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
                            ShowMovedSplitter(e);
                        //else
                        //    selRect.Offset(e.X - dragPoint.X, e.Y - dragPoint.Y);
                        dragPoint = e.Location;
                        break;
                }
                Invalidate();
            }
            else
            {
                // пока исключено
                //
                // если курсор на рамке таблицы, покажем положение возможного разделителя
                //if (drawMode == DrawMode.WaitLine && MouseInBorder(e.Location))
                //{
                //    Cursor = Cursors.Cross;
                //    if (Math.Abs(table.Y - e.Location.Y) <= epsilon ||
                //        Math.Abs(table.Y + table.Height - e.Location.Y) <= epsilon) // check top or bottom line
                //    {
                //        splitLine.First = new Point(e.Location.X, table.Y);
                //        splitLine.Last = Point.Add(splitLine.First, new Size(0, table.Height));
                //        splitKind = SplitKind.Vertical;
                //    }
                //    else if (Math.Abs(table.X - e.Location.X) <= epsilon ||
                //             Math.Abs(table.X + table.Width - e.Location.X) <= epsilon) // check left or right line
                //    {
                //        splitLine.First = new Point(table.X, e.Location.Y);
                //        splitLine.Last = Point.Add(splitLine.First, new Size(table.Width, 0));
                //        splitKind = SplitKind.Horizontal;
                //    }
                //}
                //else 
                if (drawMode == DrawMode.WaitLine && MouseInVSplit(e.Location))
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

        private void ShowMovedSplitter(MouseEventArgs e)
        {
            // перемещение вертикального разделителя
            if (splitKind == SplitKind.Vertical)
            {
                var dx = e.X - dragPoint.X;
                // защита зоны
                if (dx != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < horizontals.Count)
                {
                    var low = splitOffsetIndex > 0
                        ? table.X + horizontals[splitOffsetIndex - 1] : table.X;
                    var high = splitOffsetIndex < horizontals.Count - 1
                        ? table.X + horizontals[splitOffsetIndex + 1] : table.X + table.Width;
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
                if (dy != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < verticals.Count)
                {
                    var low = splitOffsetIndex > 0
                        ? table.Y + verticals[splitOffsetIndex - 1] : table.Y;
                    var high = splitOffsetIndex < verticals.Count - 1
                        ? table.Y + verticals[splitOffsetIndex + 1] : table.Y + table.Height;
                    var y = splitLine.Offset(0, dy).First.Y;
                    if (y < low + epsilon * 2 || y > high - epsilon * 2)
                        dy = 0;
                }
                splitLine = splitLine.Offset(0, dy);
            }
        }

        private bool MouseInVSplit(Point location, float width = epsilon)
        {
            if (table.IsEmpty) return false;
            if (horizontals.Count == 0) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                var lp = table.Location;
                foreach (var offset in horizontals)
                {
                    grp.Reset();
                    grp.AddLine(new Point(lp.X + offset, lp.Y), new Point(lp.X + offset, lp.Y + table.Height));
                    if (grp.IsOutlineVisible(location, pen))
                        return true;
                }
            }
            return false;
        }

        private bool MouseInHSplit(Point location, float width = epsilon)
        {
            if (table.IsEmpty) return false;
            if (verticals.Count == 0) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                var lp = table.Location;
                foreach (var offset in verticals)
                {
                    grp.Reset();
                    grp.AddLine(new Point(lp.X, lp.Y + offset), new Point(lp.X + table.Width, lp.Y + offset));
                    if (grp.IsOutlineVisible(location, pen))
                        return true;
                }
            }
            return false;
        }

        private bool MouseInBorder(Point location, float width = epsilon)
        {
            if (table.IsEmpty) return false;
            using (var grp = new GraphicsPath())
            using (var pen = new Pen(Color.Black, width))
            {
                grp.AddRectangle(table);
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
                            AddNewSplitter();
                        break;
                    case DrawMode.WaitRect:
                        if (table.IsEmpty)
                        {
                            table = selRect;
                            BuildFirstPointNodes();
                            selRect = Rectangle.Empty;
                            drawMode = DrawMode.WaitLine;
                            firstPoint = lastPoint = Point.Empty;
                        }
                        else
                        {
                            drawMode = DrawMode.WaitRect;
                            selRect = table = Rectangle.Empty;
                        }
                        break;
                    case DrawMode.Drag:
                        if (!splitLine.IsEmpty)
                        {
                            if (splitKind == SplitKind.Vertical &&
                                splitOffsetIndex >= 0 && splitOffsetIndex < horizontals.Count)
                            {
                                var offset = horizontals[splitOffsetIndex];
                                horizontals[splitOffsetIndex] = splitLine.First.X - table.X;
                                MoveHorizontalPointNodes(offset, horizontals[splitOffsetIndex]);
                            }
                            else if (splitKind == SplitKind.Horizontal &&
                                splitOffsetIndex >= 0 && splitOffsetIndex < verticals.Count)
                            {
                                var offset = verticals[splitOffsetIndex];
                                verticals[splitOffsetIndex] = splitLine.First.Y - table.Y;
                                MoveVerticalPointNodes(offset, verticals[splitOffsetIndex]);
                            }
                            splitLine = Line.Empty;
                        }
                        else
                        {
                            table.Location = selRect.Location;
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
                    var offset = e.Location.X - table.X;
                    var value = horizontals.FirstOrDefault(item => Math.Abs(item - offset) <= epsilon);
                    horizontals.Remove(value);
                    nodes.RemoveAll(item => item.Offset.X == value);
                }
                else if (MouseInHSplit(e.Location))
                {
                    var offset = e.Location.Y - table.Y;
                    var value = verticals.FirstOrDefault(item => Math.Abs(item - offset) <= epsilon);
                    verticals.Remove(value);
                    nodes.RemoveAll(item => item.Offset.Y == value);
                }
                Invalidate();
            }
        }

        private void AddNewSplitter()
        {
            int offset;
            List<int> list;
            switch (splitKind)
            {
                case SplitKind.Vertical:
                    offset = splitLine.First.X - table.Location.X;
                    list = new List<int>(horizontals) { 0, table.Width - 1 };
                    // защита зоны при добавлении
                    if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
                    {
                        horizontals.Add(offset);
                        horizontals.Sort();
                        AddToHorizontalPointNodes(offset);
                    }
                    break;
                case SplitKind.Horizontal:
                    offset = splitLine.First.Y - table.Location.Y;
                    list = new List<int>(verticals) { 0, table.Height - 1 };
                    // защита зоны при добавлении
                    if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
                    {
                        verticals.Add(offset);
                        verticals.Sort();
                        AddToVerticalPointNodes(offset);
                    }
                    break;
            }
        }

        private void MoveHorizontalPointNodes(int first, int last)
        {
            foreach (var pn in nodes)
            {
                if (pn.Offset.X == first)
                    pn.Offset = new Point(last, pn.Offset.Y);
            }
        }

        private void MoveVerticalPointNodes(int first, int last)
        {
            foreach (var pn in nodes)
            {
                if (pn.Offset.Y == first)
                    pn.Offset = new Point(pn.Offset.X, last);
            }
        }

        private void AddToHorizontalPointNodes(int offset)
        {
            nodes.Add(new PointNode() { Offset = new Point(offset, 0) });
            nodes.Add(new PointNode() { Offset = new Point(offset, table.Height) });
            foreach (var ofs in verticals)
                nodes.Add(new PointNode() { Offset = new Point(offset, ofs) });
        }

        private void AddToVerticalPointNodes(int offset)
        {
            nodes.Add(new PointNode() { Offset = new Point(0, offset) });
            nodes.Add(new PointNode() { Offset = new Point(table.Width, offset) });
            foreach (var ofs in horizontals)
                nodes.Add(new PointNode() { Offset = new Point(ofs, offset) });
        }

        private void BuildFirstPointNodes()
        {
            nodes.Clear();
            // добавление основных узловых точек на границах фигуры
            nodes.Add(new PointNode() { Offset = new Point(0, 0) });
            nodes.Add(new PointNode() { Offset = new Point(table.Width, 0) });
            nodes.Add(new PointNode() { Offset = new Point(table.Width, table.Height) });
            nodes.Add(new PointNode() { Offset = new Point(0, table.Height) });
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var gr = e.Graphics;
            if (!table.IsEmpty)
            {
                using (var pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = DashStyle.Dot;
                    gr.DrawRectangle(pen, table);
                    var lp = table.Location;
                    // строим вертикальные разделители
                    foreach (var offset in horizontals)
                        gr.DrawLine(pen, new Point(lp.X + offset, lp.Y), new Point(lp.X + offset, lp.Y + table.Height));
                    // строим горизонтальные разделители
                    foreach (var offset in verticals)
                        gr.DrawLine(pen, new Point(lp.X, lp.Y + offset), new Point(lp.X + table.Width, lp.Y + offset));
                }
                // рисуем узловые точки
                foreach (var np in nodes)
                {
                    var lp = table.Location;
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
    }
}
