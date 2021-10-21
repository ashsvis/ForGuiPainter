using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SlidePanelControl
{
    public partial class SlidePanel : Panel, ISupportInitialize
    {
        public DockStyle Appearance { get; set; }
        public string TextCollapsed { get; set; }
        public string TextExpanded { get; set; }
        public int ButtonSize { get; set; }
        public int AnimationSpeed { get; set; }
        public Color ButtonColor { get; set; }
        public Color ButtonColor2 { get; set; }
        public Color ButtonBorderColor { get; set; }

        public bool Collapsed
        {
            get { return Location == GetCollapsePoint(); }
        }

        public SlidePanel()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            AnimationSpeed = 10;
            ButtonSize = 18;
            Appearance = DockStyle.Left;
            TextCollapsed = "Open";
            TextExpanded = "Close";
            ButtonColor = Color.FromArgb(185, 236, 245);
            ButtonColor2 = Color.FromArgb(185, 226, 235);
            ButtonBorderColor = Color.FromArgb(180, 200, 235);
        }

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            if (!DesignMode)
            {
                Location = GetCollapsePoint();
                SetAnchors();
            }
        }
        void StartAnimation(Point target)
        {
            var tm = new Timer() { Enabled = true, Interval = 20 };
            tm.Tick += delegate
            {
                var dx = Location.X - target.X;
                var dy = Location.Y - target.Y;
                var dd = Math.Max(Math.Abs(dx), Math.Abs(dy));

                var d = dd <= AnimationSpeed ? 1 : AnimationSpeed;
                if (Location == target)
                {
                    tm.Dispose();
                    Invalidate();
                }
                else
                    Location = new Point(Location.X - d * Math.Sign(dx), Location.Y - d * Math.Sign(dy));
            };
        }

        public bool Collapse()
        {
            if (!Collapsed)
            {
                StartAnimation(GetCollapsePoint());
                return true;
            }
            return false;
        }

        public bool Expand()
        {
            if (Collapsed)
            {
                StartAnimation(GetExpandPoint());
                return true;
            }
            return false;
        }

        Point GetCollapsePoint()
        {
            switch (Appearance)
            {
                case DockStyle.Left: return new Point(-Width + ButtonSize, Top);
                case DockStyle.Right: return new Point(Parent.ClientSize.Width - ButtonSize, Top);
                case DockStyle.Top: return new Point(Left, -Height + ButtonSize);
                case DockStyle.Bottom: return new Point(Left, Parent.ClientSize.Height - ButtonSize);
            }
            return Point.Empty;
        }

        Point GetExpandPoint()
        {
            switch (Appearance)
            {
                case DockStyle.Left: return new Point(0, Top);
                case DockStyle.Right: return new Point(Parent.ClientSize.Width - Width, Top);
                case DockStyle.Top: return new Point(Left, 0);
                case DockStyle.Bottom: return new Point(Left, Parent.ClientSize.Height - Height);
            }
            return Point.Empty;
        }

        void SetAnchors()
        {
            switch (Appearance)
            {
                case DockStyle.Left: Anchor = AnchorStyles.Left; break;
                case DockStyle.Right: Anchor = AnchorStyles.Right; break;
                case DockStyle.Top: Anchor = AnchorStyles.Top; break;
                case DockStyle.Bottom: Anchor = AnchorStyles.Bottom; break;
            }
            // для выравнивания панели по высоте формы:
            //switch (Appearance)
            //{
            //    case DockStyle.Left: Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom; break;
            //    case DockStyle.Right: Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom; break;
            //    case DockStyle.Top: Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; break;
            //    case DockStyle.Bottom: Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; break;
            //}
        }

        Rectangle GetButtonRect()
        {
            switch (Appearance)
            {
                case DockStyle.Left: return new Rectangle(ClientRectangle.Right - ButtonSize, ClientRectangle.Top, ButtonSize, ClientRectangle.Height);
                case DockStyle.Right: return new Rectangle(ClientRectangle.Left, ClientRectangle.Top, ButtonSize, ClientRectangle.Height);
                case DockStyle.Top: return new Rectangle(ClientRectangle.Left, ClientRectangle.Bottom - ButtonSize, ClientRectangle.Width, ButtonSize);
                case DockStyle.Bottom: return new Rectangle(ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Width, ButtonSize);
            }

            return ClientRectangle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var buttonRect = GetButtonRect();
            var pos = PointToClient(MousePosition);

            using (var brush = new SolidBrush(buttonRect.Contains(pos) ? ButtonColor2 : ButtonColor))
                e.Graphics.FillRectangle(brush, buttonRect);

            using (var pen = new Pen(ButtonBorderColor))
            {
                var r = new Rectangle(buttonRect.Left, buttonRect.Top, buttonRect.Width - 1, buttonRect.Height - 1);
                e.Graphics.DrawRectangle(pen, r);
            }

            var sf = StringFormat.GenericTypographic;
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            var text = Collapsed ? TextCollapsed : TextExpanded;

            using (var brush = new SolidBrush(ForeColor))
                if (Appearance == DockStyle.Left || Appearance == DockStyle.Right)
                {
                    e.Graphics.TranslateTransform(buttonRect.Left, buttonRect.Bottom);
                    e.Graphics.RotateTransform(-90);
                    e.Graphics.DrawString(text, Font, brush, new Rectangle(0, 0, buttonRect.Height, buttonRect.Width), sf);
                }
                else
                    e.Graphics.DrawString(text, Font, brush, buttonRect, sf);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (GetButtonRect().Contains(e.Location))
            {
                if (Collapsed)
                    Expand();
                else
                    Collapse();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            Invalidate();
        }
    }
}
