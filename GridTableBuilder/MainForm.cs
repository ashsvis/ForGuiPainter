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

        enum WorkMode
        {
            Create,
            Change,
            Delete
        }

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
            public int Index { get; set; }

            public override string ToString()
            {
                return $"Ребро {Index}";
            }
        }

        class PointNode
        {
            public Point Offset { get; set; }
            public List<Edge> Edges { get; set; } = new List<Edge>();
        }

        WorkMode workMode = WorkMode.Create;
        Rectangle table = new Rectangle(200, 50, 400, 300);

        bool down;
        Point firstPoint;
        Point lastPoint;
        Rectangle selRect;
        DrawMode drawMode;
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
            BuildNodesAndEdges();
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (workMode == WorkMode.Create)
            {
                if (e.Button == MouseButtons.Left)
                {
                    down = true;
                    firstPoint = lastPoint = e.Location;
                    splitLine = Line.Empty;
                    Invalidate();
                }
            }
            #region
            /*
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
            */
            #endregion
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (workMode == WorkMode.Create)
            {
                if (down)
                {
                    // рисуем линию, только если внутри области
                    if (table.Contains(firstPoint))
                    {
                        lastPoint = e.Location;

                        var dx = Math.Abs(lastPoint.X - firstPoint.X);
                        var dy = Math.Abs(lastPoint.Y - firstPoint.Y);
                        // распознавание перпендикулярной линии
                        if (dy > 0 && dx / dy > 5)
                        {
                            // это горизонталь
                            lastPoint.Y = firstPoint.Y;
                            splitLine = FindAmongHorizontalEdges(firstPoint.X, lastPoint.X, firstPoint.Y);
                        }
                        else if (dx > 0 && dy / dx > 5)
                        {
                            // это вертикаль
                            lastPoint.X = firstPoint.X;
                            splitLine = FindAmongVertivalEdges(firstPoint.X, firstPoint.Y, lastPoint.Y);
                        }
                    }
                    Invalidate();
                }
            }
            #region
            /*
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
            */
            #endregion
        }

        private Line FindAmongVertivalEdges(int x, int y1, int y2)
        {
            var firstY = Math.Min(y1, y2);
            var lastY = Math.Max(y1, y2);
            var edgeA = edges.Where(edge => edge.First.Offset.X == edge.Last.Offset.X)
                             .OrderBy(edge => Math.Abs(edge.First.Offset.Y - firstY)).ElementAt(0);
            var edgeB = edges.Where(edge => edge.First.Offset.X == edge.Last.Offset.X)
                             .OrderBy(edge => Math.Abs(edge.First.Offset.Y - lastY)).ElementAt(0);
            if (edgeA != edgeB)
            {
                return new Line() { First = edgeB.First.Offset, Last = edgeB.Last.Offset };
            }
            return Line.Empty;
        }

        private Line FindAmongHorizontalEdges(int x1, int x2, int y)
        {
            var firstX = Math.Min(x1, x2);
            var lastX = Math.Max(x1, x2);
            var edgeA = edges.Where(edge => edge.First.Offset.Y == edge.Last.Offset.Y)
                             .OrderBy(edge => Math.Abs(edge.First.Offset.X - firstX)).ElementAt(0);
            var edgeB = edges.Where(edge => edge.First.Offset.Y == edge.Last.Offset.Y)
                             .OrderBy(edge => Math.Abs(edge.First.Offset.X - lastX)).ElementAt(0);
            if (edgeA != edgeB)
            {
                return new Line();
            }
            return Line.Empty;
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (workMode == WorkMode.Create)
            {
                if (down)
                {
                    down = false;
                    firstPoint = lastPoint = e.Location;
                    Invalidate();
                }
            }
            #region
            /*
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
            */
            #endregion
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            var gr = e.Graphics;
            using (var pen = new Pen(Color.Gray, 1))
            {
                pen.DashStyle = DashStyle.Dash;
                gr.DrawRectangle(pen, table);
            }
            // рисуем узловые точки
            foreach (var np in nodes)
            {
                var lp = table.Location;
                lp.Offset(np.Offset);
                var rect = new Rectangle(lp, new Size(10, 10));
                rect.Offset(-5, -5);
                gr.FillEllipse(Brushes.Gray, rect);
            }
            // рисуем рёбра
            foreach (var ed in edges)
            {
                var lp = table.Location;
                using (var pen = new Pen(Color.Black, 2))
                {
                    var first = ed.First.Offset;
                    first.Offset(lp);
                    var last = ed.Last.Offset;
                    last.Offset(lp);
                    gr.DrawLine(pen, first, last);
                }
       
            }
            //
            if (workMode == WorkMode.Create)
            {
                using (var pen = new Pen(Color.Magenta))
                {
                    pen.DashStyle = DashStyle.Dash;
                    if (splitLine.IsEmpty)
                        gr.DrawLine(pen, firstPoint, lastPoint);
                    else
                        gr.DrawLine(pen, splitLine.First, splitLine.Last);
                }

            }
            #region
            /*
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
            */
            #endregion
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

        private void BuildNodesAndEdges()
        {
            nodes.Clear();
            // добавление основных узловых точек на границах фигуры
            nodes.Add(new PointNode() { Offset = new Point(0, 0) });
            nodes.Add(new PointNode() { Offset = new Point(table.Width, 0) });
            nodes.Add(new PointNode() { Offset = new Point(table.Width, table.Height) });
            nodes.Add(new PointNode() { Offset = new Point(0, table.Height) });
            edges.Clear();
            edges.Add(new Edge() { First = nodes[0], Last = nodes[1], Index = 0 });
            edges.Add(new Edge() { First = nodes[1], Last = nodes[2], Index = 1 });
            edges.Add(new Edge() { First = nodes[2], Last = nodes[3], Index = 2 });
            edges.Add(new Edge() { First = nodes[3], Last = nodes[0], Index = 3 });
            nodes[0].Edges.Add(edges[0]);
            nodes[0].Edges.Add(edges[3]);
            nodes[1].Edges.Add(edges[0]);
            nodes[1].Edges.Add(edges[1]);
            nodes[2].Edges.Add(edges[1]);
            nodes[2].Edges.Add(edges[2]);
            nodes[3].Edges.Add(edges[2]);
            nodes[3].Edges.Add(edges[3]);
        }

        private void rbCreate_CheckedChanged(object sender, EventArgs e)
        {
            if (rbCreate.Checked)
                workMode = WorkMode.Create;
            else if (rbMove.Checked)
                workMode = WorkMode.Change;
            else if (rbDelete.Checked)
                workMode = WorkMode.Delete;
        }
    }
}
