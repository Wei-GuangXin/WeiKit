using System;
using System.Drawing;
using System.Windows.Forms;

namespace WeiKit.Window
{
	/// <summary>
	/// 消息框图标类型。
	/// </summary>
	public enum WMessageBoxIcon
	{
		/// <summary>无图标</summary>
		None,
		/// <summary>信息</summary>
		Info,
		/// <summary>成功</summary>
		Success,
		/// <summary>警告</summary>
		Warning,
		/// <summary>错误</summary>
		Error,
		/// <summary>询问</summary>
		Question
	}

	/// <summary>
	/// 消息框按钮组合。
	/// </summary>
	public enum WMessageBoxButtons
	{
		/// <summary>仅「确定」</summary>
		OK,
		/// <summary>「确定」+「取消」</summary>
		OKCancel,
		/// <summary>「是」+「否」</summary>
		YesNo,
		/// <summary>「是」+「否」+「取消」</summary>
		YesNoCancel,
		/// <summary>「重试」+「取消」</summary>
		RetryCancel
	}

	/// <summary>
	/// WeiKit 自定义消息框的基础窗体。
	/// 提供图标、消息文本、可嵌入自定义控件的内容区，以及可配置的按钮组合。
	/// 通常通过 <see cref="WeiKit.WMessageBox"/> 的静态方法调用，无需直接实例化。
	/// </summary>
	public partial class WMessageBoxBase : Form
	{
		#region 常量（与库自带窗口风格保持一致）

		/// <summary>库强调色（与 ConfigManag 的蓝色竖条一致）</summary>
		private static readonly Color Accent = Color.FromArgb(52, 152, 219);
		/// <summary>浅灰背景（与库工具栏一致）</summary>
		private static readonly Color LightBg = Color.FromArgb(240, 242, 245);
		/// <summary>正文文字色</summary>
		private static readonly Color TextColor = Color.FromArgb(64, 64, 64);

		private const int Edge = 20;
		private const int IconSize = 40;
		private const int IconGap = 16;
		private const int ButtonWidth = 84;
		private const int ButtonHeight = 32;
		private const int MinWidth = 400;
		private const int TitleBarHeight = 40;

		#endregion

		#region 控件

		private Panel pnlTitleBar;
		private Label lblTitle;
		private Button btnClose;
		private PictureBox picIcon;
		private Label lblMessage;
		private Panel pnlButtons;
		private Button btn1;
		private Button btn2;
		private Button btn3;

		// 注意：不能依赖 Control.Visible 判断布局——窗体尚未 Show 时其 getter 会递归返回 false。
		// 这里用独立字段跟踪“应该显示”的状态，布局计算只看这些字段。
		private bool iconVisible;
		private Button[] visibleButtons = new Button[0];

		#endregion

		#region 属性

		/// <summary>
		/// 内容区面板。调用方可往里添加自定义输入控件（文本框、开关、选择器等）。
		/// 内容区默认隐藏，添加控件后会自动显示并参与布局。
		/// </summary>
		public Panel ContentPanel
		{
			get { return panel1; }
		}

		/// <summary>
		/// 获取用户点击按钮后产生的结果。
		/// </summary>
		public DialogResult Result { get; private set; } = DialogResult.None;

		#endregion

		/// <summary>
		/// 创建一个空白消息框窗体。
		/// </summary>
		public WMessageBoxBase()
		{
			InitializeComponent();
			BuildUi();
		}

		#region 构建 UI

		private void BuildUi()
		{
			this.BackColor = Color.White;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.FormBorderStyle = FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.ShowInTaskbar = false;

			// 自绘标题栏（隐藏系统标题栏后需要自己实现：标题 + 关闭按钮 + 拖动）
			BuildTitleBar();

			// 图标
			picIcon = new PictureBox
			{
				Size = new Size(IconSize, IconSize),
				SizeMode = PictureBoxSizeMode.Zoom,
				BackColor = Color.Transparent
			};
			this.Controls.Add(picIcon);

			// 消息文本
			lblMessage = new Label
			{
				AutoSize = false,
				Font = new Font("微软雅黑", 10F),
				ForeColor = TextColor,
				BackColor = Color.Transparent
			};
			this.Controls.Add(lblMessage);

			// 内容区（来自 Designer 的 panel1）
			panel1.Visible = false;
			panel1.BackColor = Color.White;

			// 按钮区
			pnlButtons = new Panel
			{
				BackColor = LightBg
			};
			this.Controls.Add(pnlButtons);

			btn1 = CreateButton();
			btn2 = CreateButton();
			btn3 = CreateButton();
			pnlButtons.Controls.AddRange(new Control[] { btn1, btn2, btn3 });
		}

		/// <summary>
		/// 构建自绘标题栏：标题文字 + 关闭按钮，并支持拖动窗体。
		/// </summary>
		private void BuildTitleBar()
		{
			pnlTitleBar = new Panel
			{
				Height = TitleBarHeight,
				BackColor = Color.White,
				Cursor = Cursors.SizeAll
			};

			lblTitle = new Label
			{
				AutoSize = false,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Font = new Font("微软雅黑", 10F, FontStyle.Bold),
				ForeColor = TextColor,
				BackColor = Color.Transparent,
				Padding = new Padding(Edge, 0, 0, 0)
			};

			btnClose = new Button
			{
				Text = "✕",
				Dock = DockStyle.Right,
				Width = TitleBarHeight + 10,
				FlatStyle = FlatStyle.Flat,
				Font = new Font("微软雅黑", 10F),
				ForeColor = TextColor,
				Cursor = Cursors.Hand,
				TabStop = false
			};
			btnClose.FlatAppearance.BorderSize = 0;
			btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
			btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 0, 0);
			btnClose.Click += (s, e) =>
			{
				Result = DialogResult.Cancel;
				this.DialogResult = DialogResult.Cancel;
				this.Close();
			};

			pnlTitleBar.Controls.Add(lblTitle);
			pnlTitleBar.Controls.Add(btnClose);

			// 拖动窗体：在标题栏空白区域按住鼠标左键即可移动
			pnlTitleBar.MouseDown += TitleBar_MouseDown;
			lblTitle.MouseDown += TitleBar_MouseDown;

			this.Controls.Add(pnlTitleBar);
		}

		private void TitleBar_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// 释放鼠标捕获并通过系统消息实现拖动，避免手动计算位置抖动
				NativeMethods.ReleaseCapture();
				NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN,
					new IntPtr(NativeMethods.HT_CAPTION), IntPtr.Zero);
			}
		}

		private Button CreateButton()
		{
			Button b = new Button
			{
				Size = new Size(ButtonWidth, ButtonHeight),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("微软雅黑", 9F),
				Visible = false,
				Cursor = Cursors.Hand,
				BackColor = Color.White,
				ForeColor = TextColor
			};
			b.FlatAppearance.BorderSize = 1;
			b.FlatAppearance.BorderColor = Color.FromArgb(200, 202, 206);
			b.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 250);
			b.Click += Btn_Click;
			return b;
		}

		private void Btn_Click(object sender, EventArgs e)
		{
			Button b = sender as Button;
			if (b != null)
				Result = (DialogResult)b.Tag;
			this.DialogResult = Result;
			this.Close();
		}

		#endregion

		#region 配置

		/// <summary>
		/// 一次性配置消息框的标题、消息、图标和按钮。
		/// </summary>
		public void Configure(string title, string message,
			WMessageBoxIcon icon = WMessageBoxIcon.Info,
			WMessageBoxButtons buttons = WMessageBoxButtons.OK)
		{
			this.Text = title ?? string.Empty;
			lblTitle.Text = title ?? string.Empty;
			SetMessage(message);
			SetIcon(icon);
			SetButtons(buttons);
			LayoutContent();
		}

		/// <summary>
		/// 设置消息文本（支持换行），并重新计算布局。
		/// </summary>
		public void SetMessage(string message)
		{
			lblMessage.Text = message ?? string.Empty;
			LayoutContent();
		}

		/// <summary>
		/// 动态更新消息文本（不关闭窗体，适合动态消息框实时刷新）。
		/// </summary>
		public void UpdateMessage(string message)
		{
			lblMessage.Text = message ?? string.Empty;
			LayoutContent();
			this.Refresh();
		}

		/// <summary>
		/// 设置图标类型。图标使用库自带的 PNG 资源，与其它窗口风格一致。
		/// </summary>
		public void SetIcon(WMessageBoxIcon icon)
		{
			iconVisible = (icon != WMessageBoxIcon.None);
			picIcon.Image = GetIconImage(icon);
			picIcon.Visible = iconVisible;
			LayoutContent();
		}

		/// <summary>
		/// 设置按钮组合。
		/// </summary>
		public void SetButtons(WMessageBoxButtons buttons)
		{
			btn1.Visible = false; btn2.Visible = false; btn3.Visible = false;

			var list = new System.Collections.Generic.List<Button>();
			switch (buttons)
			{
				case WMessageBoxButtons.OK:
					SetupButton(btn1, "确定", DialogResult.OK, true); list.Add(btn1);
					break;
				case WMessageBoxButtons.OKCancel:
					SetupButton(btn1, "确定", DialogResult.OK, true); list.Add(btn1);
					SetupButton(btn2, "取消", DialogResult.Cancel, false); list.Add(btn2);
					break;
				case WMessageBoxButtons.YesNo:
					SetupButton(btn1, "是", DialogResult.Yes, true); list.Add(btn1);
					SetupButton(btn2, "否", DialogResult.No, false); list.Add(btn2);
					break;
				case WMessageBoxButtons.YesNoCancel:
					SetupButton(btn1, "是", DialogResult.Yes, true); list.Add(btn1);
					SetupButton(btn2, "否", DialogResult.No, false); list.Add(btn2);
					SetupButton(btn3, "取消", DialogResult.Cancel, false); list.Add(btn3);
					break;
				case WMessageBoxButtons.RetryCancel:
					SetupButton(btn1, "重试", DialogResult.Retry, true); list.Add(btn1);
					SetupButton(btn2, "取消", DialogResult.Cancel, false); list.Add(btn2);
					break;
			}
			visibleButtons = list.ToArray();

			ActiveControl = list.Count > 0 ? list[0] : null;
		}

		/// <summary>
		/// 配置单个按钮的文本、结果与主/次样式。
		/// </summary>
		private void SetupButton(Button b, string text, DialogResult result, bool primary)
		{
			b.Text = text;
			b.Tag = result;
			b.Visible = true;
			if (primary)
			{
				b.BackColor = Accent;
				b.ForeColor = Color.White;
				b.FlatAppearance.BorderColor = Accent;
				b.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
			}
			else
			{
				b.BackColor = Color.White;
				b.ForeColor = TextColor;
				b.FlatAppearance.BorderColor = Color.FromArgb(200, 202, 206);
				b.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 250);
			}
		}

		#endregion

		#region 布局

		/// <summary>
		/// 重新计算窗体尺寸与各控件位置，适应消息文本长度与内容区是否有控件。
		/// 全部采用手动定位（不使用 Dock），避免 Dock 与手动 Location 冲突导致的布局错乱。
		/// </summary>
		public void LayoutContent()
		{
			// 内容区是否可见
			bool hasContent = panel1.Controls.Count > 0;
			panel1.Visible = hasContent;

			// 内容区尺寸（由内容驱动的宽度/高度）
			int contentWidth = 0;
			int contentHeight = 0;
			if (hasContent)
			{
				foreach (Control c in panel1.Controls)
				{
					contentWidth = Math.Max(contentWidth, c.Right);
					contentHeight = Math.Max(contentHeight, c.Bottom);
				}
				contentWidth += 2;
				contentHeight += 2;
			}

			// 消息文本所需尺寸
			int iconAreaWidth = iconVisible ? IconSize + IconGap : 0;
			int textMaxWidth = 440;
			Size textSize = TextRenderer.MeasureText(lblMessage.Text, lblMessage.Font,
				new Size(textMaxWidth, int.MaxValue),
				TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
			int textHeight = Math.Max(textSize.Height, IconSize);

			// 窗体宽度 = 图标区 + 消息/内容取宽者，且不小于最小值
			int bodyWidth = Math.Max(textSize.Width, contentWidth);
			int width = Math.Max(MinWidth, Edge + iconAreaWidth + bodyWidth + Edge);

			// 消息区高度
			int messageAreaHeight = textHeight;

			// 总高度 = 标题栏 + 上边距 + 消息区 + 内容区(含间距) + 按钮区 + 下边距
			int contentBlockHeight = hasContent ? contentHeight + 16 : 0;
			int buttonBlockHeight = ButtonHeight + 16;
			int totalHeight = TitleBarHeight + Edge + messageAreaHeight + contentBlockHeight + buttonBlockHeight + Edge;

			this.ClientSize = new Size(width, totalHeight);

			// 标题栏
			pnlTitleBar.Size = new Size(width, TitleBarHeight);
			pnlTitleBar.Location = new Point(0, 0);

			// 图标（相对标题栏下方开始）
			picIcon.Location = new Point(Edge, TitleBarHeight + Edge + (messageAreaHeight - IconSize) / 2);

			// 消息文本
			lblMessage.Location = new Point(Edge + iconAreaWidth, TitleBarHeight + Edge);
			lblMessage.Size = new Size(bodyWidth, messageAreaHeight);

			// 内容区
			panel1.Location = new Point(Edge + iconAreaWidth, TitleBarHeight + Edge + messageAreaHeight + (hasContent ? 12 : 0));
			panel1.Size = new Size(bodyWidth, contentHeight);

			// 按钮区（底部通栏，浅灰背景）
			pnlButtons.Location = new Point(0, totalHeight - buttonBlockHeight);
			pnlButtons.Size = new Size(width, buttonBlockHeight);

			// 按钮右对齐
			LayoutButtons(width);
		}

		private void LayoutButtons(int formWidth)
		{
			int right = formWidth - Edge;
			int y = (pnlButtons.Height - ButtonHeight) / 2;

			// 从右往左排列可见按钮：visibleButtons 里主按钮在前，因此结果为主按钮在左、次按钮在右，
			// 与 Windows 标准消息框（[确定] [取消]）一致。
			for (int i = visibleButtons.Length - 1; i >= 0; i--)
			{
				Button b = visibleButtons[i];
				right -= ButtonWidth;
				b.Location = new Point(right, y);
				right -= 10;
			}
		}

		#endregion

		#region 图标资源映射

		/// <summary>
		/// 将图标类型映射到库自带的 PNG 资源。
		/// </summary>
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

		#endregion

		private void WMessageBoxBase_Load(object sender, EventArgs e)
		{
			// 给窗体设置圆角，与库其它窗口保持一致
			Tool.SetControlFillet(this, 8);
			LayoutContent();
		}

		/// <summary>
		/// 以模态方式显示消息框，返回用户点击的按钮结果。
		/// </summary>
		public new DialogResult ShowDialog()
		{
			base.ShowDialog();
			return Result;
		}

		/// <summary>
		/// 以指定所有者模态显示消息框。
		/// </summary>
		public new DialogResult ShowDialog(IWin32Window owner)
		{
			base.ShowDialog(owner);
			return Result;
		}

		/// <summary>
		/// 以非模态方式显示（用于动态消息框），调用方可通过 <see cref="UpdateMessage"/> 更新内容，
		/// 调用 <see cref="Close"/> 关闭。
		/// </summary>
		public new void Show()
		{
			base.Show();
		}
	}

	/// <summary>
	/// 用于无边框窗口拖动的 Win32 API。
	/// </summary>
	internal static class NativeMethods
	{
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
	}
}
