using System;
using System.Drawing;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// 演示程序自己的界面小工具。只为了让 Demo 代码紧凑，与 WeiKit 库本身无关。
	/// 注意：本命名空间是 WeiKit.Demo，直接写 Tool 会解析到 WeiKit.Demo.Tool（不存在），
	/// 因此文件头统一使用 using Tool = WeiKit.Tool; 别名。
	/// </summary>
	internal static class Ui
	{
		public static readonly Font BaseFont = new Font("Microsoft YaHei UI", 9F);
		public static readonly Font TitleFont = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
		public static readonly Font MonoFont = new Font("Consolas", 9.5F);

		public static readonly Color Accent = Color.FromArgb(47, 111, 235);
		public static readonly Color Muted = Color.FromArgb(110, 118, 132);

		/// <summary>写日志（转发到全局 Logs 实例）</summary>
		public static void Log(string text, string type = "Info") => Program.Log.Println(text, type);

		/// <summary>
		/// 创建一个「可滚动内容页」。返回外层宿主（Dock=Fill，负责滚动），
		/// content 是竖向流式容器，所有子控件都加进 content 即可。
		/// </summary>
		public static Panel ScrollHost(out FlowLayoutPanel content)
		{
			var host = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				Padding = new Padding(14, 10, 14, 14),
				BackColor = SystemColors.Control
			};
			content = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				Margin = new Padding(0)
			};
			host.Controls.Add(content);
			return host;
		}

		/// <summary>小节标题</summary>
		public static Label Header(string text)
		{
			return new Label
			{
				Text = text,
				Font = TitleFont,
				ForeColor = Accent,
				AutoSize = true,
				Margin = new Padding(0, 14, 0, 6)
			};
		}

		/// <summary>灰色说明文字（自动换行，高度随内容自适应）</summary>
		public static Label Hint(string text, int width = 920)
		{
			return new Label
			{
				Text = text,
				ForeColor = Muted,
				AutoSize = true,
				MaximumSize = new Size(width, 0),
				Margin = new Padding(0, 0, 0, 8)
			};
		}

		/// <summary>普通标签</summary>
		public static Label Text(string text, int width = 200)
		{
			return new Label
			{
				Text = text,
				AutoSize = false,
				Width = width,
				Height = 26,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0, 3, 6, 3)
			};
		}

		/// <summary>按钮</summary>
		public static Button Btn(string text, EventHandler onClick, int width = 128)
		{
			var b = new Button
			{
				Text = text,
				Width = width,
				Height = 30,
				Margin = new Padding(0, 3, 8, 3),
				FlatStyle = FlatStyle.System
			};
			if (onClick != null) b.Click += onClick;
			return b;
		}

		/// <summary>输入框</summary>
		public static TextBox In(string text = "", int width = 380, bool readOnly = false)
		{
			return new TextBox
			{
				Text = text,
				Width = width,
				ReadOnly = readOnly,
				Margin = new Padding(0, 4, 8, 3),
				BackColor = readOnly ? SystemColors.Control : Color.White
			};
		}

		/// <summary>只读单行输出框（等宽字体）</summary>
		public static TextBox Out(string text = "", int width = 380)
		{
			var t = In(text, width, true);
			t.Font = MonoFont;
			return t;
		}

		/// <summary>多行框</summary>
		public static TextBox Multi(string text = "", int width = 620, int height = 120, bool readOnly = true)
		{
			return new TextBox
			{
				Text = text,
				Width = width,
				Height = height,
				Multiline = true,
				ReadOnly = readOnly,
				ScrollBars = ScrollBars.Vertical,
				Font = MonoFont,
				Margin = new Padding(0, 4, 8, 6),
				BackColor = readOnly ? SystemColors.Control : Color.White
			};
		}

		/// <summary>把控件排成一行（可换行），追加到容器</summary>
		public static void Row(Control parent, params Control[] items)
		{
			var flow = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				WrapContents = true,
				Margin = new Padding(0, 2, 0, 4),
				MaximumSize = new Size(1000, 0)
			};
			flow.Controls.AddRange(items);
			parent.Controls.Add(flow);
		}

		public static void AddHint(Control parent, string text, int width = 920)
			=> parent.Controls.Add(Hint(text, width));

		public static void AddHeader(Control parent, string text) => parent.Controls.Add(Header(text));

		/// <summary>结果表格</summary>
		public static DataGridView Grid(int height = 150)
		{
			var g = new DataGridView
			{
				Height = height,
				Width = 940,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				BackgroundColor = SystemColors.Window,
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(0, 4, 0, 8),
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
			};
			g.ColumnHeadersDefaultCellStyle.Font = new Font(BaseFont, FontStyle.Bold);
			g.DefaultCellStyle.Font = BaseFont;
			return g;
		}

		public static void SetupGrid(DataGridView grid, params string[] columns)
		{
			grid.Columns.Clear();
			grid.Rows.Clear();
			foreach (var c in columns) grid.Columns.Add(c, c);
		}

		public static void AddRow(DataGridView grid, params object[] cells)
		{
			grid.Rows.Add(cells);
		}

		/// <summary>带标题的分组框，返回内部竖向容器</summary>
		public static FlowLayoutPanel Group(FlowLayoutPanel parent, string title, int width = 940)
		{
			// GroupBox 的 AutoSize 对 Dock 子控件高度计算不可靠（WinForms 已知问题），
			// 这里改用 FlowLayoutPanel 模拟分组框：标题 Label + 带边框的内容容器，高度完全由内容驱动。
			// AutoSizeMode 用 GrowOnly，保证宽度不小于 width，高度随内容增长。
			var box = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowOnly,
				Width = width,
				BackColor = SystemColors.Control,
				Margin = new Padding(0, 6, 0, 10)
			};

			var titleLbl = new Label
			{
				Text = title,
				AutoSize = true,
				Font = new Font(BaseFont, FontStyle.Bold),
				ForeColor = Accent,
				Margin = new Padding(0, 0, 0, 4)
			};
			box.Controls.Add(titleLbl);

			var inner = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowOnly,
				Width = width,
				BorderStyle = BorderStyle.FixedSingle,
				Padding = new Padding(10, 8, 10, 10),
				BackColor = Color.White,
				Font = BaseFont
			};
			box.Controls.Add(inner);
			parent.Controls.Add(box);
			return inner;
		}

		public static void Info(string text, string caption = "提示")
			=> MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

		public static void Error(string text, string caption = "错误")
			=> MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
	}
}
