using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeiKit.Comp
{
    public partial class Led : UserControl
    {
        [Browsable(true)]
        [Category("功能")]
        [Description("LED点亮时的颜色")]
        public Color OnColor
        {
            get
            {
                return _OnColor;
            }
            set
            {
                if (_OnColor != value)
                {
                    _OnColor = value;
                    this.Invalidate();
                }
            }
        }
        Color _OnColor = Color.DarkGreen;

        [Browsable(true)]
        [Category("功能")]
        [Description("LED熄灭时的颜色")]
        public Color OffColor
        {
            get
            {
                return _OffColor;
            }
            set
            {
                if (_OffColor != value)
                {
                    _OffColor = value;
                    this.Invalidate();
                }
            }
        }
        Color _OffColor = Color.Gray;
        [Browsable(true)]
        [Category("功能")]
        [Description("LED是否点亮")]
        [DefaultValue(true)]
        public bool IsOpen
        {
            get
            {
                return _IsOpen;
            }
            set
            {
                if (_IsOpen != value)
                {
                    _IsOpen = value;
                    this.Invalidate();
                }
            }
        }
        bool _IsOpen;

        public Led()
        {
            InitializeComponent();
        }

        private void Led_Load(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            //抗锯齿
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // 设置画笔和画刷
            Pen blackPen = new Pen(Color.Gray, 1);
            SolidBrush blueBrush;
            //设置颜色
            if (_IsOpen)
                blueBrush = new SolidBrush(OnColor);
            else
                blueBrush = new SolidBrush(OffColor);
            //计算大小
            if (this.Width >= this.Height)
            {
                Rectangle ellipseRect = new Rectangle((this.Width - this.Height) / 2 + 1, 1, this.Height - 2, this.Height - 2);
                g.DrawEllipse(blackPen, ellipseRect);
                g.FillEllipse(blueBrush, ellipseRect);
            }
            else
            {
                Rectangle ellipseRect = new Rectangle(1, (this.Height - this.Width) / 2 + 1, this.Width - 2, this.Width - 2);
                g.DrawEllipse(blackPen, ellipseRect);
                g.FillEllipse(blueBrush, ellipseRect);
            }
            // 清理资源
            blackPen.Dispose();
            blueBrush.Dispose();
        }

		private void Led_SizeChanged(object sender, EventArgs e)
		{
            this.Height = this.Width;
		}
	}
}
