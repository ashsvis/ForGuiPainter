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
            public PointNode First { get; set; }
            public PointNode Last { get; set; }

            public int Index { get; set; } 

            public Edge(PointNode p1, PointNode p2)
            {
                if (p1.Offset.X == p2.Offset.X)
                {
                    if (p1.Offset.Y < p2.Offset.Y)
                    {
                        First = p1;
                        Last = p2;
                    }
                    else
                    {
                        First = p2;
                        Last = p1;
                    }
                }
                else
                {
                    if (p1.Offset.X < p2.Offset.X)
                    {
                        First = p1;
                        Last = p2;
                    }
                    else
                    {
                        First = p2;
                        Last = p1;
                    }
                }
            }

            public bool IsVertical
            {
                get { return First.Offset.X == Last.Offset.X; }
            }

            public bool IsHorizontal
            {
                get { return First.Offset.Y == Last.Offset.Y; }
            }

            public override string ToString()
            {
                var info = IsVertical
                    ? $"x:{First.Offset.X} y1:{First.Offset.Y}, y2:{Last.Offset.Y}" 
                    : IsHorizontal ? $"x1:{First.Offset.X}, x2:{Last.Offset.X} y:{First.Offset.Y}" : "?";
                return $"Ребро ({info})";
            }
        }

        class PointNode
        {
            public Point Offset { get; set; }
            public List<Edge> Edges { get; set; } = new List<Edge>();

            public int Index { get; set; }

            public PointNode(Point offset)
            {
                Offset = offset;
            }

            public bool IsEmpty
            {
                get { return Edges.Count == 0; }
            }

            public bool IsAnadromous
            {
                get
                {
                    var verticals = Edges.Count(x => x.IsVertical);
                    var horizontals = Edges.Count(x => x.IsHorizontal);
                    return !(verticals > 0 && horizontals > 0);
                }
            }

            public override string ToString()
            {
                return $"Узел ({Offset})";
            }
        }

        WorkMode workMode = WorkMode.Create;
        Rectangle table = new Rectangle(200, 50, 400, 300);

        bool down;
        Point firstPoint;
        Point lastPoint;
        Rectangle ribberRect;
        List<Edge> edgesToDelete = new List<Edge>();

        Line splitLine;
        List<int> verticals = new List<int>();
        List<int> horizontals = new List<int>();
        List<PointNode> nodes = new List<PointNode>();
        List<Edge> edges = new List<Edge>();

        int pointCount = 0;
        int edgeCount = 0;

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
            else if (workMode == WorkMode.Delete)
            {
                down = true;
                firstPoint = lastPoint = e.Location;
                ribberRect = new Rectangle(Point.Subtract(e.Location, new Size(1, 1)), new Size(3, 3));
                edgesToDelete = GetEdgesSecantRect(ribberRect);
                Invalidate();
            }
        }

        //private Line GetNearEdge(Point location, float width = epsilon)
        //{
        //    using (var grp = new GraphicsPath())
        //    using (var pen = new Pen(Color.Black, width))
        //    {
        //        foreach (var edge in edges)
        //        {
        //            grp.Reset();
        //            grp.AddLine(edge.First.Offset, edge.Last.Offset);
        //            if (grp.IsOutlineVisible(location, pen))
        //                return new Line() { First = edge.First.Offset, Last = edge.Last.Offset };
        //        }
        //    }
        //    return Line.Empty;
        //}

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
                            splitLine = FindAmongVerticalEdges(firstPoint.X, lastPoint.X, firstPoint.Y);
                        }
                        else if (dx > 0 && dy / dx > 5)
                        {
                            // это вертикаль
                            lastPoint.X = firstPoint.X;
                            splitLine = FindAmongHorizontalEdges(firstPoint.X, firstPoint.Y, lastPoint.Y);
                        }
                        else
                            splitLine = Line.Empty;
                    }
                    Invalidate();
                }
            }
            else if (workMode == WorkMode.Delete)
            {
                if (down)
                {
                    lastPoint = e.Location;
                    var location = Point.Subtract(new Point(Math.Min(firstPoint.X, lastPoint.X), Math.Min(firstPoint.Y, lastPoint.Y)), new Size(1, 1));
                    var size = new Size(Math.Abs(lastPoint.X - firstPoint.X), Math.Abs(lastPoint.Y - firstPoint.Y));
                    ribberRect = new Rectangle(location, size);

                    edgesToDelete = GetEdgesSecantRect(ribberRect);

                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Список рёбер, которые пересекает обоасть действия ластика
        /// </summary>
        /// <param name="ribberRect">Область действия ластика</param>
        /// <returns></returns>
        private List<Edge> GetEdgesSecantRect(Rectangle ribberRect)
        {
            var list = new List<Edge>();
            foreach (var edge in edges)
            {
                if (edge.IsVertical) // если ребро вертикальное
                {
                    var x = edge.First.Offset.X;
                    if (x >= ribberRect.X && x < ribberRect.X + ribberRect.Width &&
                        edge.First.Offset.Y <= ribberRect.Y + ribberRect.Height && edge.Last.Offset.Y >= ribberRect.Y)
                        list.Add(edge);
                }
                else if(edge.IsHorizontal) // если ребро горизонтальное
                {
                    var y = edge.First.Offset.Y;
                    if (y >= ribberRect.Y && y < ribberRect.Y + ribberRect.Height &&
                        edge.First.Offset.X <= ribberRect.X + ribberRect.Width && edge.Last.Offset.X >= ribberRect.X)
                        list.Add(edge);
                }
            }
            return list;
        }

        /// <summary>
        /// Поиск вертикального отрезка среди горизонтальных рёбер
        /// </summary>
        /// <param name="x">X-координата вертикального отрезка</param>
        /// <param name="y1">Начальная Y-координата</param>
        /// <param name="y2">Конечная Y-координата</param>
        /// <returns></returns>
        private Line FindAmongHorizontalEdges(int x, int y1, int y2)
        {
            var firstY = Math.Min(y1, y2);
            var lastY = Math.Max(y1, y2);
            // смотрим горизонтальные рёбра
            var horizontalEdges = edges.Where(edge => edge.IsHorizontal)
                                       .Where(edge => edge.First.Offset.X < x && edge.Last.Offset.X > x).ToList();
            if (horizontalEdges.Count < 2) return Line.Empty;
            var edgeA = horizontalEdges.OrderBy(edge => Math.Abs(edge.First.Offset.Y - firstY)).ElementAt(0);
            var edgeB = horizontalEdges.OrderBy(edge => Math.Abs(edge.First.Offset.Y - lastY)).ElementAt(0);
            return edgeA == edgeB ? Line.Empty 
                : new Line() { First = new Point(x, edgeA.First.Offset.Y), Last = new Point(x, edgeB.Last.Offset.Y) };
        }

        /// <summary>
        /// Поиск горизонтального отрезка среди вертикальных рёбер
        /// </summary>
        /// <param name="x1">Начальная X-координата</param>
        /// <param name="x2">Конечная X-координата</param>
        /// <param name="y">Y-координата горизонтального отрезка</param>
        /// <returns></returns>
        private Line FindAmongVerticalEdges(int x1, int x2, int y)
        {
            var firstX = Math.Min(x1, x2);
            var lastX = Math.Max(x1, x2);
            // смотрим вертикальные рёбра
            var verticalEdges = edges.Where(edge => edge.IsVertical)
                                       .Where(edge => edge.First.Offset.Y < y && edge.Last.Offset.Y > y).ToList();
            if (verticalEdges.Count < 2) return Line.Empty;
            var edgeA = verticalEdges.OrderBy(edge => Math.Abs(edge.First.Offset.X - firstX)).ElementAt(0);
            var edgeB = verticalEdges.OrderBy(edge => Math.Abs(edge.First.Offset.X - lastX)).ElementAt(0);
            return edgeA == edgeB ? Line.Empty
                : new Line() { First = new Point(edgeA.First.Offset.X, y), Last = new Point(edgeB.Last.Offset.X, y) };
        }

        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (workMode == WorkMode.Create)
            {
                if (down)
                {
                    down = false;
                    if (!splitLine.IsEmpty)
                    {
                        // добавляем новые узлы
                        var pn1 = new PointNode(splitLine.First);
                        var pn2 = new PointNode(splitLine.Last);
                        if (!nodes.Any(pn => pn.Offset.X == pn1.Offset.X && pn.Offset.Y == pn1.Offset.Y) &&
                            !nodes.Any(pn => pn.Offset.X == pn2.Offset.X && pn.Offset.Y == pn2.Offset.Y))
                        {
                            pn1.Index = pointCount++;
                            nodes.Add(pn1);
                            // разбиваем ребро на два при добавлении узла
                            SplitEdge(pn1);

                            pn2.Index = pointCount++;
                            nodes.Add(pn2);
                            // разбиваем ребро на два при добавлении узла
                            SplitEdge(pn2);

                            // добавляем новое ребро
                            var edge = new Edge(pn1, pn2) { Index = edgeCount++ };
                            pn1.Edges.Add(edge);
                            pn2.Edges.Add(edge);
                            // добавляем новые узлы в местах пересечения новым ребром
                            // новое ребро может делиться старыми на несколько частей
                            AddCrossNodesByEdge(edge);
                        }
                    }

                    firstPoint = lastPoint = e.Location;
                    splitLine = Line.Empty;
                    Invalidate();
                }
            }
            else if (workMode == WorkMode.Delete)
            {
                if (down)
                {
                    down = false;
                    foreach (var edge in edgesToDelete)
                    {
                        edges.Remove(edge);
                        foreach (var pn in nodes)
                            pn.Edges.Remove(edge);
                    }
                    edgesToDelete.Clear();
                    RemoveIsAnadromousNodes();
                    firstPoint = lastPoint = e.Location;
                    ribberRect = Rectangle.Empty;
                    Invalidate();
                }
            }
            Text = $"Nodes: {nodes.Count}, Edges: {edges.Count}";
            FillTreeView();
        }

        private void RemoveIsAnadromousNodes()
        {
            var list = new List<PointNode>();
            foreach (var pn in nodes)
            {
                if (pn.IsEmpty)
                    list.Add(pn);
                if (pn.IsAnadromous)
                    list.Add(pn);
            }
            foreach (var pn in list)
            {
                if (pn.IsAnadromous)
                {

                }
                nodes.Remove(pn);
            }
        }

        /// <summary>
        /// Новое ребро пересекает старые и образует новые узлы, которые будут делить старые рёбра на две части
        /// Новое ребро может делиться старыми на несколько частей
        /// </summary>
        /// <param name="edge">Ссылка на новое ребро</param>
        private void AddCrossNodesByEdge(Edge edge)
        {
            var verticals = GetCrossEdges(edge, edges.Where(e => edge.IsHorizontal && e.First.Offset.X == e.Last.Offset.X &&
                                             e.First.Offset.X != edge.First.Offset.X && e.Last.Offset.X != edge.Last.Offset.X).ToList());
            var horizontals = GetCrossEdges(edge, edges.Where(e => edge.IsVertical && e.First.Offset.Y == e.Last.Offset.Y &&
                                               e.First.Offset.Y != edge.First.Offset.Y && e.Last.Offset.Y != edge.Last.Offset.Y).ToList());
            var points = new List<PointNode>();
            if (verticals.Count > 0)
            {
                foreach (var e in verticals)
                    points.Add(new PointNode(new Point(e.First.Offset.X, edge.First.Offset.Y)));
            }
            else if (horizontals.Count > 0)
            {
                foreach (var e in horizontals)
                    points.Add(new PointNode(new Point(edge.First.Offset.X, e.First.Offset.Y)));
            }
            // добавляем новое ребро
            edges.Add(edge);
            // которое, возможно, будем делить
            foreach (var pn in points)
            {
                pn.Index = pointCount++;
                nodes.Add(pn);
                SplitEdge(pn);
            }
        }

        /// <summary>
        /// Новый узел разбивает существующее ребро на два
        /// </summary>
        /// <param name="pn">Ссылка на новый узел</param>
        private void SplitEdge(PointNode pn)
        {
            var verticals = edges.Where(edge => edge.IsVertical && pn.Offset.X == edge.First.Offset.X &&
                                        pn.Offset.Y >= edge.First.Offset.Y && pn.Offset.Y <= edge.Last.Offset.Y).ToList();
            var horizontals = edges.Where(edge => edge.IsHorizontal && pn.Offset.Y == edge.First.Offset.Y &&
                                          pn.Offset.X >= edge.First.Offset.X && pn.Offset.X <= edge.Last.Offset.X).ToList();
            foreach (var edge in verticals.Union(horizontals))
            {
                // добавляем новые рёбра
                var edg1 = new Edge(edge.First, pn) { Index = edgeCount++ };
                edges.Add(edg1);
                pn.Edges.Add(edg1);
                var edg2 = new Edge(pn, edge.Last) { Index = edgeCount++ };
                edges.Add(edg2);
                pn.Edges.Add(edg2);
                // удаляем старое ребро
                edge.First.Edges.Remove(edge);
                edge.First.Edges.Add(edg1);
                edge.Last.Edges.Remove(edge);
                edge.Last.Edges.Add(edg2);
                edges.Remove(edge);
            }
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
                var rect = new Rectangle(np.Offset, new Size(8, 8));
                rect.Offset(-4, -4);
                gr.FillEllipse(Brushes.Gray, rect);
                rect.Offset(5, 5);
                gr.DrawString($"p{np.Index}", DefaultFont, Brushes.Black, rect.Location);
            }
            // рисуем рёбра
            foreach (var ed in edges)
            {
                using (var pen = new Pen(Color.Black, 1))
                {
                    gr.DrawLine(pen, ed.First.Offset, ed.Last.Offset);
                }
                var p = new Point(ed.First.Offset.X, ed.First.Offset.Y);
                p.Offset((ed.Last.Offset.X - ed.First.Offset.X) / 2 - 8, (ed.Last.Offset.Y - ed.First.Offset.Y) / 2 - 12);
                gr.DrawString($"e{ed.Index}", DefaultFont, Brushes.Black, p);
            }
            //
            if (workMode == WorkMode.Create)
            {
                using (var pen = new Pen(Color.Magenta))
                {
                    pen.DashStyle = DashStyle.Dash;                    
                    gr.DrawLine(pen, firstPoint, lastPoint);
                }
                // рисуем возможное ребро
                if (!splitLine.IsEmpty)
                {
                    using (var pen = new Pen(Color.Black, 1))
                    {
                        pen.DashStyle = DashStyle.Dot;
                        gr.DrawLine(pen, splitLine.First, splitLine.Last);
                    }
                }
            }
            else if (workMode == WorkMode.Delete)
            {
                if (!ribberRect.IsEmpty)
                {
                    using (var pen = new Pen(Color.Red, 1))
                    {
                        gr.DrawRectangle(pen, ribberRect);
                    }
                }
                // рисуем рёбра для удаления
                foreach (var ed in edgesToDelete)
                {
                    using (var pen = new Pen(Color.FromArgb(100, Color.Red), 3))
                    {
                        gr.DrawLine(pen, ed.First.Offset, ed.Last.Offset);
                    }
                }
            }
        }

        //private void ShowMovedSplitter(MouseEventArgs e)
        //{
        //    // перемещение вертикального разделителя
        //    if (splitKind == SplitKind.Vertical)
        //    {
        //        var dx = e.X - dragPoint.X;
        //        // защита зоны
        //        if (dx != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < horizontals.Count)
        //        {
        //            var low = splitOffsetIndex > 0
        //                ? table.X + horizontals[splitOffsetIndex - 1] : table.X;
        //            var high = splitOffsetIndex < horizontals.Count - 1
        //                ? table.X + horizontals[splitOffsetIndex + 1] : table.X + table.Width;
        //            var x = splitLine.Offset(dx, 0).First.X;
        //            if (x < low + epsilon * 2 || x > high - epsilon * 2)
        //                dx = 0;
        //        }
        //        splitLine = splitLine.Offset(dx, 0);
        //    }
        //    else if (splitKind == SplitKind.Horizontal) // перемещение горизонтального разделителя
        //    {
        //        var dy = e.Y - dragPoint.Y;
        //        // защита зоны
        //        if (dy != 0 && splitOffsetIndex >= 0 && splitOffsetIndex < verticals.Count)
        //        {
        //            var low = splitOffsetIndex > 0
        //                ? table.Y + verticals[splitOffsetIndex - 1] : table.Y;
        //            var high = splitOffsetIndex < verticals.Count - 1
        //                ? table.Y + verticals[splitOffsetIndex + 1] : table.Y + table.Height;
        //            var y = splitLine.Offset(0, dy).First.Y;
        //            if (y < low + epsilon * 2 || y > high - epsilon * 2)
        //                dy = 0;
        //        }
        //        splitLine = splitLine.Offset(0, dy);
        //    }
        //}

        //private bool MouseInVSplit(Point location, float width = epsilon)
        //{
        //    if (table.IsEmpty) return false;
        //    if (horizontals.Count == 0) return false;
        //    using (var grp = new GraphicsPath())
        //    using (var pen = new Pen(Color.Black, width))
        //    {
        //        var lp = table.Location;
        //        foreach (var offset in horizontals)
        //        {
        //            grp.Reset();
        //            grp.AddLine(new Point(lp.X + offset, lp.Y), new Point(lp.X + offset, lp.Y + table.Height));
        //            if (grp.IsOutlineVisible(location, pen))
        //                return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Ищем список ребёр, имеющих точку пересечения с указанным ребром
        /// </summary>
        /// <param name="edge">Новое ребро</param>
        /// <param name="list">Список ребёр для тестирования</param>
        /// <returns></returns>
        private List<Edge> GetCrossEdges(Edge edge, IEnumerable<Edge> list)
        {
            var result = new List<Edge>();
            var r1 = RectangleF.Empty;
            using (var pen = new Pen(Color.Black, 1))
            using (var mx = new Matrix())
            {
                using (var grp = new GraphicsPath())
                {
                    grp.AddLine(edge.First.Offset, edge.Last.Offset);
                    r1 = grp.GetBounds(mx, pen);
                }
                using (var grp = new GraphicsPath())
                {
                    foreach (var e in list)
                    {
                        grp.Reset();
                        grp.AddLine(e.First.Offset, e.Last.Offset);
                        var rect = grp.GetBounds(mx, pen);
                        if (rect.IntersectsWith(r1))
                            result.Add(e);
                    }
                }
            }
            return result;
        }


        //private bool MouseInHSplit(Point location, float width = epsilon)
        //{
        //    if (table.IsEmpty) return false;
        //    if (verticals.Count == 0) return false;
        //    using (var grp = new GraphicsPath())
        //    using (var pen = new Pen(Color.Black, width))
        //    {
        //        var lp = table.Location;
        //        foreach (var offset in verticals)
        //        {
        //            grp.Reset();
        //            grp.AddLine(new Point(lp.X, lp.Y + offset), new Point(lp.X + table.Width, lp.Y + offset));
        //            if (grp.IsOutlineVisible(location, pen))
        //                return true;
        //        }
        //    }
        //    return false;
        //}

        //private bool MouseInBorder(Point location, float width = epsilon)
        //{
        //    if (table.IsEmpty) return false;
        //    using (var grp = new GraphicsPath())
        //    using (var pen = new Pen(Color.Black, width))
        //    {
        //        grp.AddRectangle(table);
        //        return grp.IsOutlineVisible(location, pen);
        //    }
        //}

        //private void AddNewSplitter()
        //{
        //    int offset;
        //    List<int> list;
        //    switch (splitKind)
        //    {
        //        case SplitKind.Vertical:
        //            offset = splitLine.First.X - table.Location.X;
        //            list = new List<int>(horizontals) { 0, table.Width - 1 };
        //            // защита зоны при добавлении
        //            if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
        //            {
        //                horizontals.Add(offset);
        //                horizontals.Sort();
        //                AddToHorizontalPointNodes(offset);
        //            }
        //            break;
        //        case SplitKind.Horizontal:
        //            offset = splitLine.First.Y - table.Location.Y;
        //            list = new List<int>(verticals) { 0, table.Height - 1 };
        //            // защита зоны при добавлении
        //            if (!list.Any(item => Math.Abs(item - offset) < epsilon * 2))
        //            {
        //                verticals.Add(offset);
        //                verticals.Sort();
        //                AddToVerticalPointNodes(offset);
        //            }
        //            break;
        //    }
        //}

        //private void MoveHorizontalPointNodes(int first, int last)
        //{
        //    foreach (var pn in nodes)
        //    {
        //        if (pn.Offset.X == first)
        //            pn.Offset = new Point(last, pn.Offset.Y);
        //    }
        //}

        //private void MoveVerticalPointNodes(int first, int last)
        //{
        //    foreach (var pn in nodes)
        //    {
        //        if (pn.Offset.Y == first)
        //            pn.Offset = new Point(pn.Offset.X, last);
        //    }
        //}

        //private void AddToHorizontalPointNodes(int offset)
        //{
        //    nodes.Add(new PointNode(new Point(offset, 0)));
        //    nodes.Add(new PointNode(new Point(offset, table.Height)));
        //    foreach (var ofs in verticals)
        //        nodes.Add(new PointNode(new Point(offset, ofs)));
        //}

        //private void AddToVerticalPointNodes(int offset)
        //{
        //    nodes.Add(new PointNode(new Point(0, offset)));
        //    nodes.Add(new PointNode(new Point(table.Width, offset)));
        //    foreach (var ofs in horizontals)
        //        nodes.Add(new PointNode(new Point(ofs, offset)));
        //}

        private void BuildNodesAndEdges()
        {
            nodes.Clear();
            // добавление основных узловых точек на границах фигуры
            nodes.Add(new PointNode(new Point(table.X, table.Y)) { Index = pointCount++ });
            nodes.Add(new PointNode(new Point(table.X + table.Width, table.Y)) { Index = pointCount++ });
            nodes.Add(new PointNode(new Point(table.X + table.Width, table.Y + table.Height)) { Index = pointCount++ });
            nodes.Add(new PointNode(new Point(table.X, table.Y + table.Height)) { Index = pointCount++ });
            edges.Clear();
            edges.Add(new Edge(nodes[0], nodes[1]) { Index = edgeCount++ });
            edges.Add(new Edge(nodes[1], nodes[2]) { Index = edgeCount++ });
            edges.Add(new Edge(nodes[2], nodes[3]) { Index = edgeCount++ });
            edges.Add(new Edge(nodes[3], nodes[0]) { Index = edgeCount++ });
            nodes[0].Edges.Add(edges[0]);
            nodes[0].Edges.Add(edges[3]);
            nodes[1].Edges.Add(edges[0]);
            nodes[1].Edges.Add(edges[1]);
            nodes[2].Edges.Add(edges[1]);
            nodes[2].Edges.Add(edges[2]);
            nodes[3].Edges.Add(edges[2]);
            nodes[3].Edges.Add(edges[3]);

            Text = $"Nodes: {nodes.Count}, Edges: {edges.Count}";
            FillTreeView();
        }

        private void FillTreeView()
        {
            try
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
                foreach (var pn in nodes)
                {
                    var nd = new TreeNode($"p{pn.Index}");
                    treeView1.Nodes.Add(nd);
                    foreach (var ed in pn.Edges)
                        nd.Nodes.Add($"e{ed.Index}");
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
                workMode = WorkMode.Create;
            else if (rbMove.Checked)
                workMode = WorkMode.Change;
            else if (rbDelete.Checked)
                workMode = WorkMode.Delete;
            Invalidate();
        }
    }
}
