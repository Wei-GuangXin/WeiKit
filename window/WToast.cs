using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeiKit.Window
{
	/// <summary>
	/// Toast 提示框出现的位置锚点（相对屏幕或宿主窗口的边缘）。
	/// </summary>
	public enum ToastPosition
	{
		/// <summary>左上角</summary>
		TopLeft,
		/// <summary>右上角</summary>
		TopRight,
		/// <summary>左下角</summary>
		BottomLeft,
		/// <summary>右下角</summary>
		BottomRight
	}

	/// <summary>
	/// 从边缘动态滑入、停留一段时间后滑出并自动销毁的小提示框（Toast）。
	/// 支持相对屏幕（默认）或相对某个宿主窗口（owner）定位。
	/// 风格与 <see cref="WMessageBoxBase"/> 保持一致（圆角、浅灰边框、库图标、微软雅黑）。
	/// 通过 <see cref="WeiKit.WMessageBox.ShowToast"/> 使用。
	/// </summary>
	public class WToast : Form
	{
		#region 常量

		private static readonly Color BorderColor = Color.FromArgb(200, 202, 206);
		private static readonly Color TextColor = Color.FromArgb(64, 64, 64);
		private static readonly Color MutedColor = Color.FromArgb(110, 118, 132);

		private const int Edge = 16;
		private const int IconSize = 32;
		private const int IconGap = 12;
		private const int MaxTextWidth = 280;
		private const int SlideDuration = 250;   // 滑入/滑出各耗时（毫秒）
		private const int FrameInterval = 15;    // 动画帧间隔（毫秒）
		private const int CornerRadius = 8;

		#endregion

		#region 动画状态

		private enum ToastPhase { SlidingIn, Waiting, SlidingOut }

		private readonly Timer timer;
		private ToastPhase phase = ToastPhase.SlidingIn;
		private DateTime phaseStart;
		private readonly Point hiddenLocation;    // 边缘外（滑入前/滑出后）
		private readonly Point visibleLocation;   // 边缘内（停留位置）
		private readonly int stayDuration;

		#endregion

		/// <summary>
		/// 创建一个 Toast 提示框。
		/// </summary>
		/// <param name="message">消息正文</param>
		/// <param name="title">可选标题（加粗显示在正文上方）</param>
		/// <param name="icon">图标类型</param>
		/// <param name="position">出现的边缘位置</param>
		/// <param name="durationMs">停留时长（毫秒），不含滑入滑出动画时间</param>
		/// <param name="owner">宿主窗口。为 null 时相对屏幕工作区定位；否则相对该窗口客户区定位。</param>
		public WToast(string message, string title, WMessageBoxIcon icon, ToastPosition position,
			int durationMs, IWin32Window owner)
		{
			stayDuration = Math.Max(0, durationMs);

			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition = FormStartPosition.Manual;
			this.ShowInTaskbar = false;
			this.TopMost = (owner == null);
			this.BackColor = Color.White;

			BuildUi(message, title, icon);
			ApplyRoundedRegion();
			ComputeLocations(position, owner, out visibleLocation, out hiddenLocation);
			this.Location = hiddenLocation;

			timer = new Timer { Interval = FrameInterval };
			timer.Tick += Timer_Tick;
		}

		#region UI 构建

		private void BuildUi(string message, string title, WMessageBoxIcon icon)
		{
			bool hasIcon = icon != WMessageBoxIcon.None;
			bool hasTitle = !string.IsNullOrEmpty(title);
			int iconAreaWidth = hasIcon ? IconSize + IconGap : 0;
			int textX = Edge + iconAreaWidth;

			using (Font titleFont = new Font("微软雅黑", 10F, FontStyle.Bold))
			using (Font msgFont = new Font("微软雅黑", 9F))
			{
				// 标题（AutoSize + MaximumSize，让 Label 自己按宽度换行，避免文字被截断）
				Label lblTitle = null;
				Size titlePref = Size.Empty;
				if (hasTitle)
				{
					lblTitle = new Label
					{
						Text = title,
						AutoSize = true,
						MaximumSize = new Size(MaxTextWidth, 0),
						Font = titleFont,
						ForeColor = TextColor,
						BackColor = Color.Transparent,
						Location = new Point(textX, Edge)
					};
					titlePref = lblTitle.GetPreferredSize(new Size(MaxTextWidth, 0));
					this.Controls.Add(lblTitle);
				}

				// 正文
				var lblMsg = new Label
				{
					Text = message,
					AutoSize = true,
					MaximumSize = new Size(MaxTextWidth, 0),
					Font = msgFont,
					ForeColor = MutedColor,
					BackColor = Color.Transparent,
					Location = new Point(textX, Edge + (hasTitle ? titlePref.Height + 4 : 0))
				};
				Size msgPref = lblMsg.GetPreferredSize(new Size(MaxTextWidth, 0));
				this.Controls.Add(lblMsg);

				// 图标（垂直居中于文本区）
				int textHeight = (hasTitle ? titlePref.Height + 4 : 0) + msgPref.Height;
				int textWidth = Math.Max(titlePref.Width, msgPref.Width);
				if (hasIcon)
				{
					var pic = new PictureBox
					{
						Image = GetIconImage(icon),
						Size = new Size(IconSize, IconSize),
						SizeMode = PictureBoxSizeMode.Zoom,
						BackColor = Color.Transparent,
						Location = new Point(Edge, Edge + (textHeight - IconSize) / 2)
					};
					this.Controls.Add(pic);
				}

				// 窗体尺寸（文本宽度取测量值，保证与 Label 实际渲染一致）
				int height = Math.Max(textHeight, hasIcon ? IconSize : 0) + Edge * 2;
				int width = textX + textWidth + Edge;
				this.ClientSize = new Size(width, height);
			}
		}

		private static Image GetIconImage(WMessageBoxIcon icon)
		{
			switch (icon)
			{
				case WMessageBoxIcon.Info:
				case WMessageBoxIcon.Question:
					return Properties.Resources.信息_info;
				case WMessageBoxIcon.Success:
					return Properties.Resources.正确的_correct;
				case WMessageBoxIcon.Warning:
					return Properties.Resources.注意_attention;
				case WMessageBoxIcon.Error:
					return Properties.Resources.错误_error;
				default:
					return null;
			}
		}

		/// <summary>
		/// 给窗体设置圆角 Region（真正裁掉四角）。Region 基于窗体客户区坐标，
		/// 与窗口位置无关，因此滑入滑出动画移动窗口时圆角不会失效。
		/// </summary>
		private void ApplyRoundedRegion()
		{
			using (GraphicsPath path = CreateRoundRectPath(
				new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height), CornerRadius))
			{
				this.Region = new Region(path);
			}
		}

		#endregion

		#region 位置计算

		private void ComputeLocations(ToastPosition position, IWin32Window owner,
			out Point visible, out Point hidden)
		{
			// 定位基准区域：有 owner 用其客户区屏幕矩形，否则用主屏工作区
			Rectangle bounds;
			if (owner != null)
			{
				Control c = owner as Control ?? Control.FromHandle(owner.Handle);
				bounds = c != null ? c.RectangleToScreen(c.ClientRectangle) : Screen.PrimaryScreen.WorkingArea;
			}
			else
			{
				bounds = Screen.PrimaryScreen.WorkingArea;
			}

			int w = this.Width;
			int h = this.Height;

			int x;
			int y;
			switch (position)
			{
				case ToastPosition.TopLeft:
					x = bounds.Left + Edge;
					y = bounds.Top + Edge;
					break;
				case ToastPosition.TopRight:
					x = bounds.Right - w - Edge;
					y = bounds.Top + Edge;
					break;
				case ToastPosition.BottomLeft:
					x = bounds.Left + Edge;
					y = bounds.Bottom - h - Edge;
					break;
				default: // BottomRight
					x = bounds.Right - w - Edge;
					y = bounds.Bottom - h - Edge;
					break;
			}

			visible = new Point(x, y);

			// 根据左右位置决定滑入方向：左侧从左边滑入，右侧从右边滑入
			bool slideFromLeft = (position == ToastPosition.TopLeft || position == ToastPosition.BottomLeft);
			if (slideFromLeft)
				hidden = new Point(bounds.Left - w - Edge, y);
			else
				hidden = new Point(bounds.Right + Edge, y);
		}

		#endregion

		#region 动画

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			phase = ToastPhase.SlidingIn;
			phaseStart = DateTime.Now;
			timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			double elapsed = (DateTime.Now - phaseStart).TotalMilliseconds;

			switch (phase)
			{
				case ToastPhase.SlidingIn:
					if (elapsed >= SlideDuration)
					{
						this.Location = visibleLocation;
						phase = ToastPhase.Waiting;
						phaseStart = DateTime.Now;
					}
					else
					{
						double t = elapsed / SlideDuration;
						this.Location = Lerp(hiddenLocation, visibleLocation, EaseOutCubic(t));
					}
					break;

				case ToastPhase.Waiting:
					if (elapsed >= stayDuration)
					{
						phase = ToastPhase.SlidingOut;
						phaseStart = DateTime.Now;
					}
					break;

				case ToastPhase.SlidingOut:
					if (elapsed >= SlideDuration)
					{
						timer.Stop();
						this.Location = hiddenLocation;
						this.Close();
					}
					else
					{
						double t = elapsed / SlideDuration;
						this.Location = Lerp(visibleLocation, hiddenLocation, EaseInCubic(t));
					}
					break;
			}
		}

		private static Point Lerp(Point a, Point b, double t)
		{
			return new Point(
				(int)Math.Round(a.X + (b.X - a.X) * t),
				(int)Math.Round(a.Y + (b.Y - a.Y) * t));
		}

		private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
		private static double EaseInCubic(double t) => t * t * t;

		#endregion

		#region 绘制

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rect = ClientRectangle;
			rect.Width -= 1;
			rect.Height -= 1;

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			using (GraphicsPath path = CreateRoundRectPath(rect, CornerRadius))
			{
				using (SolidBrush white = new SolidBrush(Color.White))
					e.Graphics.FillPath(white, path);

				using (Pen pen = new Pen(BorderColor, 1f))
					e.Graphics.DrawPath(pen, path);
			}
		}

		private static GraphicsPath CreateRoundRectPath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			int d = radius * 2;
			path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
			path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
			path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
			path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
			path.CloseFigure();
			return path;
		}

		#endregion

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			timer.Stop();
			timer.Dispose();
			base.OnFormClosed(e);
		}
	}
}
