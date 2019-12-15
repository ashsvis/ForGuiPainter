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

            foreach (var node in Nodes)
                node.NormEdges();

            WorkMode = GridWorkMode.Draw;
        }

        /*
         * http://www.cyberforum.ru/algorithms/thread2008601.html#post10576549
         * 
var np:integer;  // число узлов графа
var xa,ya:array [0..100] of integer;  // координаты узлов
adj:array [0..100,0..100] of integer; // матрица сопряжений
stack,visited:array [0..100] of integer; // стек для поиска в глубину
seg:array [0..1,0..100] of integer;nseg:integer; // массив ребер графа
 
type TCircle=record
             els:array [0..100] of integer;
             nel:integer;
             end;
var circles:array [0..100] of TCircle; // массив найденных циклов графа
ncl:integer; // число найденных циклов графа
t:integer; // переменная стека
 
function CompareCircles(i1,i2:integer):Boolean;
var i,j,k:integer;
begin
Result:=False;
if circles[i1].nel<>circles[i2].nel then exit;
k:=0;
for i:=0 to circles[i1].nel-2 do
   begin
   for j:=0 to circles[i2].nel-2 do
      begin
      if circles[i1].els[i]=circles[i2].els[j] then k:=k+1;
      end;
   end;
if k=(circles[i1].nel-1) then Result:=True;
end;
 
function rDirAngle(x1,y1,x2,y2:Extended):Extended;
var x:Extended;
begin
if Abs(y2-y1)<0.000001 then
   begin
   if x2>x1 then begin Result:=PI/2;exit;end;
   if x2<x1 then begin Result:=3*PI/2;exit;end
   end;
x:=Arctan(Abs(x2-x1)/Abs(y2-y1))*180/PI;
if (x2>=x1) and (y2>y1) then Result:=x;
if (x2>=x1) and (y2<y1) then Result:=180-x;
if (x2<=x1) and (y2<y1) then Result:=180+x;
if (x2<=x1) and (y2>y1) then Result:=360-x;
Result:=Result*PI/180;
end;
 
function IsHorderCircle(n:integer):Boolean;
var i,j,d,k,i1,i2:integer;
a1,a2,a3:double;
begin
d:=0;
for i:=0 to circles[n].nel-2 do
   begin
   d:=d+xa[circles[n].els[i]]*ya[circles[n].els[i+1]]-xa[circles[n].els[i+1]]*ya[circles[n].els[i]];
   end;
d:=sign(d); // -1 - против, =1 - по  (или наоборот)
//---------Если против часовой стрелки, поменять направление
if d<0 then
   begin
   for i:=0 to circles[n].nel div 2 - 1 do
      begin
      k:=circles[n].els[i];
      circles[n].els[i]:=circles[n].els[circles[n].nel-1-i];
      circles[n].els[circles[n].nel-1-i]:=k;
      end;
   end;
//---------Обход по часовой, проверка ответвлений вправо
circles[n].els[circles[n].nel]:=circles[n].els[1];  //--Добавляем ещё один элемент в цикл
for i:=0 to circles[n].nel-2 do
   begin
   for j:=0 to np-1 do  //---поиск сопряжённых точек
      begin
      if (adj[circles[n].els[i+1],j]=1) and (j<>circles[n].els[i]) and (j<>circles[n].els[i+1]) and (j<>circles[n].els[i+2]) then
         begin
         a1:=rDirAngle(xa[circles[n].els[i+1]],ya[circles[n].els[i+1]],xa[circles[n].els[i]],ya[circles[n].els[i]]);
         a2:=rDirAngle(xa[circles[n].els[i+1]],ya[circles[n].els[i+1]],xa[circles[n].els[i+2]],ya[circles[n].els[i+2]]);
         a3:=rDirAngle(xa[circles[n].els[i+1]],ya[circles[n].els[i+1]],xa[j],ya[j]);
         a1:=a1*180/PI;a2:=a2*180/PI;a3:=a3*180/PI;
         a1:=a1-a2; while (a1<360) do a1:=a1+360;while (a1>360) do a1:=a1-360;
         a3:=a3-a2; while (a3<360) do a3:=a3+360;while (a3>360) do a3:=a3-360;
         if a3>a1 then begin Result:=true;exit;end;
         end;
      end;
   end;
IntToStr(n);
Result:=False;
end;
 
procedure push(k:integer);
begin
stack[t]:=k;t:=t+1;
end;
 
procedure pop(k:integer);
begin
t:=t-1;
end;
 
procedure DFS(k,v:integer);
var i,j,m,i1,j1:integer;
begin
push(k);
visited[k]:=1;
for i:=0 to np-1 do
    begin
    if i=k then continue;
    if i=v then continue;
    if (visited[i]=1) and (adj[i,k]<>0) then //--если не сама с себя
       begin
       for j:=0 to t-1 do
          begin
          circles[ncl].els[j]:=stack[j];
          end;
       for j:=0 to t-1 do if stack[j]=i then break;
       for m:=j to t-1 do circles[ncl].els[m-j]:=stack[m]; //---переносим цикл из стека
       circles[ncl].els[t-j]:=i;
       circles[ncl].nel:=t-j+1;
//-------------проверка, а может такой цикл уже есть?
       j1:=0;
       for i1:=0 to ncl-1 do
          begin
          if CompareCircles(i1,ncl)=True then begin j1:=1;break;end;
          end;
//-------------проверка, цикл с внутренними хордами?
       if IsHorderCircle(ncl)=True then j1:=1;
       if j1=0 then ncl:=ncl+1; //--Если такого цикла не было, то добавляем, иначе - нет
       continue;
       end;
    if adj[i,k]<>0 then DFS(i,k);
    end;
visited[k]:=0;
pop(k);
end;
 
procedure TForm13.Button2Click(Sender: TObject);
var i,j,k,minx,maxx:integer;
begin
//----------Сформировать adj
for i:=0 to 100 do for j:=0 to 100 do adj[i,j]:=0;
for i:=0 to nseg-1 do
   begin
   adj[seg[0,i],seg[1,i]]:=1;
   adj[seg[1,i],seg[0,i]]:=1;
   end;
//----------Запустить DFS
t:=0;ncl:=0;
for i:=0 to np-1 do visited[i]:=0;
for i:=0 to np-1 do
   begin
   if visited[i]=0 then DFS(i,-1);
   end;
minx:=100000;maxx:=-100000;for i:=0 to np-1 do begin if xa[i]<minx then minx:=xa[i];if xa[i]>maxx then maxx:=xa[i];end;
for i:=0 to ncl-1 do begin Draw(i*(maxx-minx+30),300);DrawCircles(i,i*(maxx-minx+30),300);end;
end
         */

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

            FilterCycles();
        }

        private void NormIndexes()
        {
            var i = 0;
            foreach (var node in Nodes.OrderBy(n => n.Index).ToList())
            {
                node.Index = i++;
                node.NormEdges();
            }
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
                // набираем точки для рисования выбранного цикла 
                var points = new List<Point>();
                foreach (var index in SelectedCycle)
                {
                    var node = Nodes.FirstOrDefault(n => n.Index == index - 1);
                    if (node == null) continue;
                    points.Add(node.Offset);
                }
                // заливка жёлтым выбранного региона
                using (var path = new GraphicsPath())
                {
                    path.AddLines(points.ToArray());
                    using (var rgn = new Region(path))
                    {
                        gr.FillRegion(Brushes.Yellow, rgn);
                    }
                }
                // рисуем чёрным границу выделенного цикла
                using (var pen = new Pen(Color.Black, 2))
                    gr.DrawLines(pen, points.ToArray());
            }
        }

        public void FilterCycles()
        {
            var regions = new List<Tuple<Region, string>>();
            try
            {
                // подготовка списка регионов
                foreach (var key in CatalogCycles.Keys)
                {
                    var cycle = CatalogCycles[key];
                    var points = new List<Point>();
                    foreach (var index in cycle)
                    {
                        var node = Nodes.FirstOrDefault(n => n.Index == index - 1);
                        if (node == null) continue;
                        points.Add(node.Offset);
                    }
                    using (var path = new GraphicsPath())
                    {
                        path.AddLines(points.ToArray());
                        regions.Add(new Tuple<Region, string>(new Region(path), key));
                    }
                }
                // обработка списка регионов
                var comboCycles = new List<string>();
                using (var bmp = new Bitmap(1000, 1000))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        for (var i = 0; i < regions.Count; i++)
                        {
                            var rgn1 = regions[i].Item1;
                            var key1 = regions[i].Item2;
                            for (var j = i + 1; j < regions.Count; j++)
                            {
                                var rgn2 = regions[j].Item1;
                                using (var rgn = new Region(rgn2.GetRegionData()))
                                {
                                    rgn.Intersect(rgn1);
                                    if (rgn.Equals(rgn2, g) && !comboCycles.Contains(key1))
                                        comboCycles.Add(key1);
                                }
                            }
                        }
                    }
                }
                // исключение составных циклов
                foreach (var cycle in comboCycles)
                    CatalogCycles.Remove(cycle);
            }
            finally
            {
                for (var i = 0; i < regions.Count; i++)
                    regions[i].Item1.Dispose();
            }
        }

    }
}
