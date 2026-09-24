using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeiKit.Comp
{
	public partial class ProgressRing : UserControl
	{
		public ProgressRing()
		{
			InitializeComponent();
			this.Size = new Size(100, 100);
			this.DoubleBuffered = true;
		}

		private float _progress = 0f;
		[Category("Behavior")]
		[Browsable(true)]
		[Description("表示进度条进度值")]
		public float Progress
		{
			get { return _progress; }
			set
			{
				if (value < 0) _progress = 0;
				else if (value > 100) _progress = 100;
				else _progress = value;
				Invalidate(); // 触发重绘
			}
		}

		private Color _barColor = Color.Blue;
		[Category("Behavior")]
		[Browsable(true)]
		[Description("表示进度条的颜色")]
		public Color BarColor
		{
			get { return _barColor; }
			set { _barColor = value; Invalidate(); }
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			label1.Text = $"{_progress}%";
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle rect = ClientRectangle;
			int diameter = Math.Min(rect.Width, rect.Height) - 4;
			rect.Inflate(-4, -4); // 留出边距
			using (Pen pen = new Pen(BarColor, 4))
			{
				// 绘制背景圆圈
				g.DrawEllipse(Pens.LightGray, rect);
				// 计算角度并绘制进度圆弧
				float angle = (_progress / 100) * 360;
				g.DrawArc(pen, rect, -90, angle);
			}
		}

		private void ProgressRing_SizeChanged(object sender, EventArgs e)
		{
			this.Height = this.Width;
		}

		//// 重写 SetBoundsCore，确保控件始终为正方形
		//protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		//{
		//	int size = Math.Min(width, height);
		//	base.SetBoundsCore(x, y, size, size, specified);
		//}
	}
}
