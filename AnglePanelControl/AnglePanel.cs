using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace AnglePanelControl
{
    [DefaultProperty("Altitude")]
    public partial class AnglePanel: Control
    {
        RectangleF rect;
        float R;
        //GraphicsPath areaPath;
        PointF center;

        const float rulerSize = 3f;
        const float ruler2Size = rulerSize * 2;

        PointF ruler;

        float angle = 135;
        float altitude = 45;

        private bool down;
        private Point fixRuler;

        public AnglePanel()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(40, 40);
            TuningSize();
        }

        private void TuningSize()
        {
            var side = Size.Width > Size.Height ? Size.Height : Size.Width;
            rect = new RectangleF(0, 0, side - 1, side - 1);
            rect.Inflate(-rulerSize, -rulerSize);

            R = rect.Width / 2;
            ruler = center = new PointF(rect.Left + R, rect.Top + R);
            ruler = PointF.Add(center, new SizeF(-R / 2, -R / 2));
        }

        private GraphicsPath GetAreaPath()
        {
            var path = new GraphicsPath();
            path.AddEllipse(rect);
            return path;
        }
        private GraphicsPath GetRulerPath()
        {
            var path = new GraphicsPath();
            var rulerRect = new RectangleF(new PointF(ruler.X - rulerSize, ruler.Y - rulerSize), new SizeF(ruler2Size, ruler2Size));
            path.AddEllipse(rulerRect);
            return path;
        }

        private PointF Sub(PointF vector1, PointF vector2)
        {
            return new PointF(vector1.X - vector2.X, vector1.Y - vector2.Y);
        }

        private float CalcAngle(PointF c)
        {
            return (float)Math.Atan2(c.Y, c.X);
        }

        private float CalcAngle(PointF v1, PointF v2)
        {
            const float PI = (float)Math.PI;
            var a = CalcAngle(v1) - CalcAngle(v2);
            a += (a > PI) ? -2 * PI : (a < -PI) ? 2 * PI : 0;
            return a;
        }

        private float GetAngle(PointF marker, PointF anchor, PointF mouse)
        {
            const float TO_DEGREES = 180 / (float)Math.PI;
            var a = Sub(marker, anchor);
            var m = Sub(mouse, anchor);
            return CalcAngle(m, a) * TO_DEGREES;
        }

        private event EventHandler onAngleChange;

        [Category("Action"), Description("Occurs when the altitude or the angle in then anglepanel control changes.")]
        public event EventHandler OnAngleChange
        {
            add
            {
                onAngleChange += value;
            }
            remove
            {
                onAngleChange -= value;
            }
        }

        [Category("Appearance"), Description("The current value of the altitude anglepanel control."),
         DefaultValue(typeof(float), "45")]
        public float Altitude
        {
            get
            {
                return altitude;
            }
            set
            {
                if (value < 0 || value > 90) return;
                var changed = Math.Abs(altitude - value) > 0.0001;
                altitude = value;
                if (changed)
                {
                    SetRuler(altitude, angle);
                    OnChangeProperties();
                }
            }
        }

        [Category("Appearance"), Description("The current value of the angle anglepanel control."),
         DefaultValue(typeof(float), "135")]
        public float Angle
        {
            get
            {
                return angle;
            }
            set
            {
                if (value < -180 || value > 180) return;
                var changed = Math.Abs(angle - value) > 0.0001;
                angle = value;
                if (changed)
                {
                    SetRuler(altitude, angle);
                    OnChangeProperties();
                }
            }
        }

        private void OnChangeProperties()
        {
            Invalidate();
            onAngleChange?.Invoke(this, new EventArgs());
        }

        private void SetRuler(float altitude, float angle)
        {
            if (altitude < 0 || altitude > 90) return;
            if (angle < -180 || angle > 180) return;
            const float TO_RADIANS = (float)Math.PI / 180;
            var r = altitude * R / 90.0;
            var x = r * Math.Cos(angle * TO_RADIANS) + center.X;
            var y = r * -Math.Sin(angle * TO_RADIANS) + center.Y;
            ruler = new Point((int)x, (int)y);
        }

        private void UpdateData()
        {
            var t = new PointF(ruler.X - center.X, ruler.Y - center.Y);
            var r = (float)Math.Sqrt(t.X * t.X + t.Y * t.Y);
            altitude = r / R * 90;
            if (altitude < 0)
                altitude = 0;
            else if (altitude > 90)
                altitude = 90;
            var ancor = new PointF(center.X + R, center.Y);
            angle = GetAngle(ruler, center, ancor);
            if (angle < -180)
                angle = -180;
            else if (angle > 180)
                angle = 180;
            OnChangeProperties();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var gr = e.Graphics;
            using (var path = GetAreaPath())
            {
                var rect = path.GetBounds();
                var point1 = rect.Location;
                var point2 = rect.Location;
                using (var brush = new LinearGradientBrush(PointF.Add(point1, new SizeF(rect.Width / 2, 0)),
                                       PointF.Add(point2, new SizeF(rect.Width / 2, rect.Height)), Color.Gray, Color.White))
                    gr.FillPath(brush, path);
                using (var pen = new Pen(Color.White, 1))
                {
                    rect.Inflate(-1, -1);
                    gr.DrawArc(pen, rect, 0, -180);
                }
                using (var pen = new Pen(Color.Black, 1))
                    gr.DrawPath(pen, path);
            }
            // draw center
            gr.FillEllipse(Brushes.DimGray, new RectangleF(new PointF(center.X - 2, center.Y - 2), new SizeF(4, 4)));
            // draw ruler
            using (var path = GetRulerPath())
            {
                gr.FillPath(Brushes.White, path);
                gr.DrawPath(Pens.Black, path);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                using (var path = GetRulerPath())
                    if (path.IsVisible(e.Location))
                    {
                        down = true;
                        fixRuler = Point.Ceiling(ruler);
                    }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (down)
            {
                var dx = e.Location.X - fixRuler.X;
                var dy = e.Location.Y - fixRuler.Y;

                var test = new PointF(ruler.X + dx, ruler.Y + dy);
                using (var path = GetAreaPath())
                if (path.IsVisible(test) || path.IsOutlineVisible(test, Pens.Black))
                {
                    ruler = PointF.Add(ruler, new SizeF(dx, dy));
                    UpdateData();
                    fixRuler = e.Location;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
                down = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            TuningSize();
        }
    }
}
