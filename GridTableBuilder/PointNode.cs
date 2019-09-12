using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GridTableBuilder
{
    public class PointNode
    {
        public Point Offset { get; set; }
        public List<Edge> Edges { get; set; } = new List<Edge>();

        public Edge East { get; set; }
        public Edge West { get; set; }
        public Edge Nord { get; set; }
        public Edge South { get; set; }

        public int Index { get; set; }

        public PointNode(Point offset)
        {
            Offset = offset;
        }

        public bool IsEmpty
        {
            get { return Edges.Count == 0; }
        }

        /// <summary>
        /// Проходная узловая точка на линии рёбер
        /// </summary>
        public bool IsAnadromous
        {
            get
            {
                var verticals = Edges.Count(x => x.IsVertical);
                var horizontals = Edges.Count(x => x.IsHorizontal);
                return !(verticals > 0 && horizontals > 0);
            }
        }

        public void NormEdges()
        {
            East = null;
            West = null;
            Nord = null;
            South = null;

            var list = new List<Edge>(Edges);
            Edges.Clear();
            // ищем горизонтальное ребро справа от узла 
            var edge = list.Where(item => item.IsHorizontal).FirstOrDefault(item => item.Node1 != this && item.Node1.Offset.X > this.Offset.X ||
                                                                                    item.Node2 != this && item.Node2.Offset.X > this.Offset.X);
            if (edge != null)
            {
                Edges.Add(edge);
                list.Remove(edge);
                East = edge;
            }
            // далее ищем вертикальное ребро снизу от узла 
            edge = list.Where(item => item.IsVertical).FirstOrDefault(item => item.Node1 != this && item.Node1.Offset.Y > this.Offset.Y ||
                                                                              item.Node2 != this && item.Node2.Offset.Y > this.Offset.Y);
            if (edge != null)
            {
                Edges.Add(edge);
                list.Remove(edge);
                South = edge;
            }
            // ищем горизонтальное ребро слева от узла 
            edge = list.Where(item => item.IsHorizontal).FirstOrDefault(item => item.Node1 != this && item.Node1.Offset.X < this.Offset.X ||
                                                                                item.Node2 != this && item.Node2.Offset.X < this.Offset.X);
            if (edge != null)
            {
                Edges.Add(edge);
                list.Remove(edge);
                West = edge;
            }
            // далее ищем вертикальное ребро сверху от узла 
            edge = list.Where(item => item.IsVertical).FirstOrDefault(item => item.Node1 != this && item.Node1.Offset.Y < this.Offset.Y ||
                                                                              item.Node2 != this && item.Node2.Offset.Y < this.Offset.Y);
            if (edge != null)
            {
                Edges.Add(edge);
                list.Remove(edge);
                Nord = edge;
            }
        }

        public override string ToString()
        {
            return $"p{Index + 1}";
        }
    }
}
