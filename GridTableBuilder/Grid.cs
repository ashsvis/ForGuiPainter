using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace GridTableBuilder
{
    [Serializable]
    public enum GridWorkMode
    {
        Draw,
        Move,
        Erase
    }

    public enum SplitKind
    {
        None,
        Vertical,
        Horizontal
    }

    [Serializable]
    public partial class Grid
    {
        int pointCount = 0;
        int edgeCount = 0;

        bool down;
        Point firstPoint;
        Point lastPoint;
        Point drarPoint;

        Line splitLine;

        public Rectangle Area { get; set; } = new Rectangle();
        public List<PointNode> Nodes { get; set; } = new List<PointNode>();
        public List<Edge> Edges { get; set; } = new List<Edge>();

        public GridWorkMode WorkMode { get; set; }
        public SplitKind SplitKind { get; set; }

        public bool ShowNodeNames { get; set; }
        public bool ShowEdgeNames { get; set; }

        public void Init()
        {
            Nodes.Clear();
            // добавление основных узловых точек на границах фигуры
            Nodes.Add(new PointNode(new Point(Area.X, Area.Y)) { Index = pointCount++ });
            Nodes.Add(new PointNode(new Point(Area.X + Area.Width, Area.Y)) { Index = pointCount++ });
            Nodes.Add(new PointNode(new Point(Area.X + Area.Width, Area.Y + Area.Height)) { Index = pointCount++ });
            Nodes.Add(new PointNode(new Point(Area.X, Area.Y + Area.Height)) { Index = pointCount++ });
            Edges.Clear();
            Edges.Add(new Edge(Nodes[0], Nodes[1]) { Index = edgeCount++ });
            Edges.Add(new Edge(Nodes[1], Nodes[2]) { Index = edgeCount++ });
            Edges.Add(new Edge(Nodes[2], Nodes[3]) { Index = edgeCount++ });
            Edges.Add(new Edge(Nodes[3], Nodes[0]) { Index = edgeCount++ });
            Nodes[0].Edges.Add(Edges[0]);
            Nodes[0].Edges.Add(Edges[3]);
            Nodes[1].Edges.Add(Edges[0]);
            Nodes[1].Edges.Add(Edges[1]);
            Nodes[2].Edges.Add(Edges[1]);
            Nodes[2].Edges.Add(Edges[2]);
            Nodes[3].Edges.Add(Edges[2]);
            Nodes[3].Edges.Add(Edges[3]);

            WorkMode = GridWorkMode.Draw;
        }

        List<PointNode> V = new List<PointNode>();
        List<Edge> E = new List<Edge>();

        public void CyclesSearch()
        {
            CatalogCycles.Clear();

            V.Clear();
            NormIndexes();
            V.AddRange(Nodes);
            E.Clear();
            E.AddRange(Edges);
            var color = new int[V.Count];
            for (var i = 0; i < V.Count; i++)
            {
                for (var k = 0; k < V.Count; k++)
                    color[k] = 1;
                var cycle = new List<int>();
                //поскольку в C# нумерация элементов начинается с нуля, то для
                //удобочитаемости результатов поиска в список добавляем номер i + 1
                cycle.Add(i + 1);
                DFScycle(i, i, E, color, -1, cycle);
            }

            var list = new List<Tuple<string, Region>>();
            foreach (var key in CatalogCycles.Keys)
            {
                var cycle = CatalogCycles[key];
                using (var gp = new GraphicsPath())
                {
                    for (var i = 1; i < cycle.Length; i++)
                    {
                        var n1 = Nodes[cycle[i - 1] - 1];
                        var n2 = Nodes[cycle[i] - 1];
                        gp.AddLine(n1.Offset, n2.Offset);
                    }
                    using (var rgn = new Region(gp))
                    {
                        list.Add(new Tuple<string, Region>(key, rgn.Clone()));
                    }
                }
            }
            //
            var keys = new HashSet<string>();
            using (var image = new Bitmap(1000, 1000))
            using (var g = Graphics.FromImage(image))
                for (var i = 0; i < list.Count; i++)
                {
                    for (var j = i + 1; j < list.Count; j++)
                    {
                        using (var test = list[i].Item2.Clone())
                        {
                            test.Intersect(list[j].Item2);
                            if (test.Equals(list[i].Item2, g))
                            {
                                keys.Add(list[j].Item1);
                            }
                        }
                    }
                }
            foreach (var key in keys)
                CatalogCycles.Remove(key);
            //
            foreach (var rgn in list)
                rgn.Item2.Dispose();

            /*
            var keys = new List<string>();
            var nodes = new List<PointNode>();
            foreach (var key in CatalogCycles.Keys)
            {
                var cycle = CatalogCycles[key];
                // получаем список ребер, составляющий этот цикл
                var cycleEdges = new List<Edge>();
                for (var i = 1; i < cycle.Length; i++)
                {
                    var n1 = Nodes[cycle[i - 1] - 1];
                    var n2 = Nodes[cycle[i] - 1];
                    var edge = n1.Edges.Intersect(n2.Edges).First();
                    if (cycleEdges.Count > 0)
                    {
                        var lastEdge = cycleEdges[cycleEdges.Count - 1];
                        if (edge.IsSameOrientation(lastEdge))
                        {
                            if (!nodes.Contains(n1))
                                nodes.Add(n1);
                        }
                    }
                    cycleEdges.Add(edge);
                }
                foreach (var n in nodes)
                {
                    var node = n;
                    if (node.Edges.Any(e => node.Edges.Count > 2 && !cycleEdges.Contains(e)))
                    {
                        //var edge = node.Edges.First(e => !cycleEdges.Contains(e));
                        //if (edge.Node1 != node && cycle.Contains(edge.Node1.Index + 1) ||
                        //    edge.Node2 != node && cycle.Contains(edge.Node2.Index + 1))
                        //{
                        //    if (!keys.Contains(key))
                        //        keys.Add(key);
                        //}
                        //else 
                        //if (edge.Node1 != node && !cycle.Contains(edge.Node1.Index + 1) ||
                        //         edge.Node2 != node && !cycle.Contains(edge.Node2.Index + 1))
                        //{
                        //    var cn = edge.Node1 != node ? edge.Node1 : edge.Node2;
                        //    var minX = cycle.Select(c => Nodes[c - 1].Offset.X).Min();
                        //    var maxX = cycle.Select(c => Nodes[c - 1].Offset.X).Max();
                        //    var minY = cycle.Select(c => Nodes[c - 1].Offset.Y).Min();
                        //    var maxY = cycle.Select(c => Nodes[c - 1].Offset.Y).Max();
                        //    if (cn.Offset.X >= minX && cn.Offset.X <= maxX &&
                        //        cn.Offset.Y >= minY && cn.Offset.Y <= maxY)
                        //    {
                        //        if (!keys.Contains(key))
                        //            keys.Add(key);
                        //    }
                        //}
                    }
                }
            }
            foreach (var key in keys)
                CatalogCycles.Remove(key);

            */
        }

        private void NormIndexes()
        {
            var i = 0;
            foreach (var node in Nodes.OrderBy(n => n.Index).ToList())
                node.Index = i++;
            i = 0;
            foreach (var edge in Edges.OrderBy(e => e.Index).ToList())
                edge.Index = i++;
        }

        public Dictionary<string, int[]> CatalogCycles = new Dictionary<string, int[]>();

        public int[] SelectedCycle { get; internal set; } = new int[] { };

        private void DFScycle(int u, int endV, List<Edge> E, int[] color, int unavailableEdge, List<int> cycle)
        {
            //если u == endV, то эту вершину перекрашивать не нужно, иначе мы в нее не вернемся, а вернуться необходимо
            if (u != endV)
                color[u] = 2;
            else if (cycle.Count >= 2)
            {
                var s = string.Join("-", cycle.Skip(1).OrderBy(n => n));
                if (!CatalogCycles.ContainsKey(s))
                    CatalogCycles.Add(s, cycle.ToArray());
                return;
            }
            for (int w = 0; w < E.Count; w++)
            {
                if (w == unavailableEdge)
                    continue;
                if (color[E[w].Node2.Index] == 1 && E[w].Node1.Index == u)
                {
                    var cycleNEW = new List<int>(cycle);
                    cycleNEW.Add(E[w].Node2.Index + 1);
                    DFScycle(E[w].Node2.Index, endV, E, color, w, cycleNEW);
                    color[E[w].Node2.Index] = 1;
                }
                else if (color[E[w].Node1.Index] == 1 && E[w].Node2.Index == u)
                {
                    var cycleNEW = new List<int>(cycle);
                    cycleNEW.Add(E[w].Node1.Index + 1);
                    DFScycle(E[w].Node1.Index, endV, E, color, w, cycleNEW);
                    color[E[w].Node1.Index] = 1;
                }
            }
        }

        public void OnLeftMouseDown(Point location)
        {
            if (WorkMode == GridWorkMode.Draw)
                LeftMouseDownInDrawMode(location);
            else if (WorkMode == GridWorkMode.Move)
                LeftMouseDownInMoveMode(location);
            else if (WorkMode == GridWorkMode.Erase)
                LeftMouseDownInEraseMode(location);
        }

        public void OnMouseMove(Point location)
        {
            if (WorkMode == GridWorkMode.Draw)
                MouseMoveInDrawMode(location);
            else if (WorkMode == GridWorkMode.Move)
                MouseMoveInMoveMode(location);
            else if (WorkMode == GridWorkMode.Erase)
                MouseMoveInEraseMode(location);
        }

        public void OnLeftMouseUp(Point location)
        {
            if (WorkMode == GridWorkMode.Draw)
                LeftMouseUpInDrawMode(location);
            else if (WorkMode == GridWorkMode.Move)
                LeftMouseUpInMoveMode(location);
            else if (WorkMode == GridWorkMode.Erase)
                LeftMouseUpInEraseMode(location);
        }

        public void OnPaint(Graphics graphics)
        {
            var gr = graphics;
            using (var pen = new Pen(Color.Gray, 1))
            {
                pen.DashStyle = DashStyle.Dash;
                gr.DrawRectangle(pen, Area);
            }
            // рисуем узловые точки
            foreach (var np in Nodes)
            {
                var rect = new Rectangle(np.Offset, new Size(8, 8));
                rect.Offset(-4, -4);
                gr.FillEllipse(Brushes.Gray, rect);
                if (ShowNodeNames)
                {
                    rect.Offset(5, 5);
                    using (var font = new Font("Arial", 8))
                        //gr.DrawString($"p{np.Index}", font, Brushes.Black, rect.Location);
                        gr.DrawString($"{np.Index + 1}", font, Brushes.Black, rect.Location);
                }
            }
            // рисуем рёбра
            foreach (var ed in Edges)
            {
                using (var pen = new Pen(Color.Black, 1))
                    gr.DrawLine(pen, ed.Node1.Offset, ed.Node2.Offset);
                if (ShowEdgeNames)
                {
                    var p = new Point(ed.Node1.Offset.X, ed.Node1.Offset.Y);
                    p.Offset((ed.Node2.Offset.X - ed.Node1.Offset.X) / 2 - 8, (ed.Node2.Offset.Y - ed.Node1.Offset.Y) / 2 - 12);
                    using (var font = new Font("Arial", 8))
                        gr.DrawString($"e{ed.Index}", font, Brushes.Black, p);
                }
            }
            //
            if (WorkMode == GridWorkMode.Draw)
                PaintInDrawMode(graphics);
            else if (WorkMode == GridWorkMode.Move)
                PaintInMoveMode(graphics);
            else if (WorkMode == GridWorkMode.Erase)
                PaintInEraseMode(graphics);

            if (SelectedCycle.Length > 0)
            {
                var points = new List<Point>();
                foreach (var index in SelectedCycle)
                {
                    var node = Nodes.FirstOrDefault(n => n.Index == index - 1);
                    if (node == null) continue;
                    points.Add(node.Offset);
                }
                using (var pen = new Pen(Color.Black, 2))
                    gr.DrawLines(pen, points.ToArray());

            }
        }

    }
}
