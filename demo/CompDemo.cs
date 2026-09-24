using System;
using System.Drawing;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;
using WinPanel = System.Windows.Forms.Panel;

namespace WeiKit.Demo
{
	/// <summary>
	/// ⑤ 自绘控件演示：WeiKit.Comp 下的 6 个控件 + DataFlowList 驱动的实时曲线。
	/// </summary>
	internal class CompDemo
	{
		// 控件实例
		private Comp.Switch sw;
		private Comp.Led led;
		private Comp.Chart chart;
		private Comp.ProgressBar bar;
		private Comp.ProgressRing ring;
		private Comp.Panels panels;

		// 状态显示
		private Label lbSwitchState;
		private Label lbLedState;
		private Label lbBarState;
		private Label lbRingState;

		// 定时刷新
		private Timer animTimer;
		private Tool.DataFlowList flow = new Tool.DataFlowList();
		private Random rnd = new Random();
		private int tick;

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"这 6 个控件全部在 WeiKit.Comp 命名空间下，只依赖 WinForms 与 GDI+，" +
				"在窗体设计器里可以直接从工具箱拖入（属性面板有中文说明）。下面每个演示区都可实时改属性。");

			BuildSwitch(content);
			BuildLed(content);
			BuildChart(content);
			BuildBar(content);
			BuildRing(content);
			BuildPanels(content);

			Ui.AddHeader(content, "整体说明");
			Ui.AddHint(content, "· Switch 的 IsOpenChange 只在鼠标点击时触发，代码给 IsOpen 赋值不会触发" +
							  "（需要时用 SetIsOpen(value, raiseEvent: true)）；\r\n" +
							  "· Led 会强制 Height = Width，Chart 的 Value 只接受 int[] 或 float[]；\r\n" +
							  "· ProgressRing 与 Comp.ProgressBar 的 Progress 都会被自动钳制；\r\n" +
							  "· Panels 的圆角用 Win32 区域裁剪实现，是硬边缘，尺寸变化后需要重绘才生效。");

			// 构造完成后直接启动曲线/进度的实时动画
			StartAnimation();
			Ui.Log("自绘控件页已就绪，Chart 实时刷新已启动。", "Info");

			return host;
		}

		/// <summary>颜色列表里取下一个颜色（循环）</summary>
		private static Color NextColor(Color[] colors, Color current)
		{
			int idx = Array.FindIndex(colors, c => c.ToArgb() == current.ToArgb());
			return colors[(idx + 1 + colors.Length) % colors.Length];
		}

		#region Switch

		private void BuildSwitch(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "1. Switch 开关");
			var g = Ui.Group(content, "属性：IsOpen / Lock　事件：IsOpenChange");

			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 4, 0, 4)
			};

			sw = new Comp.Switch
			{
				Width = 92,
				Height = 34,
				Margin = new Padding(0, 2, 16, 2)
			};
			// 事件只在点击时触发
			sw.IsOpenChange += (s, isOpen) =>
			{
				lbSwitchState.Text = $"IsOpen = {isOpen}（由点击触发 IsOpenChange）";
				Ui.Log($"Switch.IsOpenChange 触发：IsOpen = {isOpen}", "Info");
			};

			row.Controls.Add(sw);
			row.Controls.Add(Ui.Btn("切换 IsOpen（不触发事件）", (s, e) =>
			{
				sw.IsOpen = !sw.IsOpen;
				lbSwitchState.Text = $"IsOpen = {sw.IsOpen}（代码赋值，事件未触发）";
				Ui.Log($"代码设置 Switch.IsOpen = {sw.IsOpen}，IsOpenChange 不会触发。", "Info");
			}, 190));
			row.Controls.Add(Ui.Btn("锁定 / 解锁", (s, e) =>
			{
				sw.Lock = !sw.Lock;
				lbSwitchState.Text = $"Lock = {sw.Lock}（锁定后点击无效）";
			}, 110));
			row.Controls.Add(Ui.Btn("放大控件", (s, e) => { sw.Width += 20; sw.Height += 8; }, 100));
			row.Controls.Add(Ui.Btn("缩小控件", (s, e) => { sw.Width = Math.Max(40, sw.Width - 20); sw.Height = Math.Max(18, sw.Height - 8); }, 100));
			g.Controls.Add(row);

			lbSwitchState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 4, 0, 4) };
			lbSwitchState.Text = "IsOpen = False（点一下开关试试）";
			g.Controls.Add(lbSwitchState);
			Ui.AddHint(g, "注意：Demo 里「切换 IsOpen」按钮故意用代码赋值，你会看到状态变了但事件没触发 —— 这是真实行为。");
		}

		#endregion

		#region Led

		private void BuildLed(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "2. Led 指示灯");
			var g = Ui.Group(content, "属性：IsOpen / OnColor / OffColor");

			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 4, 0, 4)
			};

			led = new Comp.Led
			{
				Size = new Size(48, 48),
				OnColor = Color.LimeGreen,
				OffColor = Color.DimGray,
				Margin = new Padding(0, 2, 20, 2)
			};

			row.Controls.Add(led);
			row.Controls.Add(Ui.Btn("点亮 / 熄灭", (s, e) =>
			{
				led.IsOpen = !led.IsOpen;
				lbLedState.Text = $"IsOpen = {led.IsOpen}　OnColor = {led.OnColor.Name}　OffColor = {led.OffColor.Name}";
			}, 110));
			row.Controls.Add(Ui.Btn("换 OnColor", (s, e) =>
			{
				led.OnColor = NextColor(new[] { Color.LimeGreen, Color.OrangeRed, Color.DodgerBlue, Color.Gold, Color.Magenta }, led.OnColor);
				lbLedState.Text = $"OnColor = {led.OnColor.Name}";
			}, 120));
			row.Controls.Add(Ui.Btn("换 OffColor", (s, e) =>
			{
				led.OffColor = NextColor(new[] { Color.DimGray, Color.LightGray, Color.DarkSlateGray, Color.Silver }, led.OffColor);
				lbLedState.Text = $"OffColor = {led.OffColor.Name}";
			}, 120));
			row.Controls.Add(Ui.Btn("改变尺寸", (s, e) =>
			{
				int w = led.Width >= 80 ? 40 : led.Width + 16;
				led.Width = w;              // 高度会自动跟着宽度变（SizeChanged 中 Height = Width）
				lbLedState.Text = $"尺寸 = {led.Width}×{led.Height}（控件强制正方形）";
			}, 110));
			g.Controls.Add(row);

			lbLedState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 4, 0, 4) };
			lbLedState.Text = "IsOpen = False　OnColor = LimeGreen　OffColor = DimGray";
			g.Controls.Add(lbLedState);
		}

		#endregion

		#region Chart

		private void BuildChart(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "3. Chart 曲线图");
			var g = Ui.Group(content, "属性：Value / Text / TitleBar / LineColor / LineBackgroundColor / LineWide / Tension / IsDisplayNumber");

			chart = new Comp.Chart
			{
				Width = 880,
				Height = 200,
				Text = "实时数据流（每秒追加一个随机值，窗口 40 个点）",
				LineColor = Color.DodgerBlue,
				LineBackgroundColor = Color.FromArgb(220, 235, 255),
				LineWide = 2f,
				Tension = 0.4f,
				Margin = new Padding(0, 4, 0, 6)
			};
			g.Controls.Add(chart);

			flow.SetFlowMaxLeng(40);
			for (int i = 0; i < 40; i++) flow.FlowAdd(rnd.Next(10, 90));
			PushChartData();

			Ui.Row(g,
				Ui.Btn("开始 / 暂停实时刷新", (s, e) =>
				{
					if (animTimer.Enabled)
					{
						animTimer.Stop();
						Ui.Log("曲线实时刷新已暂停。", "Info");
					}
					else
					{
						animTimer.Start();
						Ui.Log("曲线实时刷新已启动（每秒 1 个点）。", "Info");
					}
				}, 170),
				Ui.Btn("灌入一次随机数据", (s, e) =>
				{
					for (int i = 0; i < 40; i++) flow.FlowAdd(rnd.Next(10, 90));
					PushChartData();
				}, 150),
				Ui.Btn("切换标题栏", (s, e) =>
				{
					chart.TitleBar = !chart.TitleBar;
					Ui.Log($"Chart.TitleBar = {chart.TitleBar}", "Info");
				}, 120),
				Ui.Btn("切换数值显示", (s, e) => { chart.IsDisplayNumber = !chart.IsDisplayNumber; }, 130),
				Ui.Btn("换配色", (s, e) =>
				{
					var pairs = new[]
					{
						new { L = Color.DodgerBlue, B = Color.FromArgb(220, 235, 255) },
						new { L = Color.SeaGreen,   B = Color.FromArgb(220, 245, 228) },
						new { L = Color.OrangeRed,  B = Color.FromArgb(255, 232, 224) },
						new { L = Color.MediumPurple,B = Color.FromArgb(238, 230, 255) }
					};
					var p = pairs[tick % pairs.Length];
					chart.LineColor = p.L;
					chart.LineBackgroundColor = p.B;
					Ui.Log($"Chart 配色已切换：{p.L.Name} / {p.B.Name}", "Info");
				}, 100));

			Ui.AddHint(g,
				"Value 只接受 int[] 或 float[]，传 double[] 会抛 ArgumentException。" +
				"空数组与单点数据现在都能安全处理（已修复：旧版空数组会索引越界、单点会除以 0）。");
		}

		private void PushChartData()
		{
			object[] data = flow.GetDataArray();
			if (data == null || data.Length == 0) return;
			// DataFlowList 元素是 int，必须显式转成 float[]（Chart 只认 int[]/float[]）
			var values = new float[data.Length];
			for (int i = 0; i < data.Length; i++) values[i] = Convert.ToSingle(data[i]);
			chart.Value = values;
		}

		#endregion

		#region ProgressBar

		private void BuildBar(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "4. Comp.ProgressBar 条形进度");
			var g = Ui.Group(content, "属性：Progress / MinValue / MaxValue / BarColor / BarBackColor");

			bar = new Comp.ProgressBar
			{
				Width = 880,
				Height = 26,
				MinValue = 0,
				MaxValue = 100,
				Progress = 35,
				BarColor = Color.SeaGreen,
				BarBackColor = Color.WhiteSmoke,
				Margin = new Padding(0, 4, 0, 6)
			};
			g.Controls.Add(bar);

			var slider = new TrackBar
			{
				Minimum = -20,      // 故意允许越界，演示不钳制的后果
				Maximum = 120,
				Value = 35,
				TickFrequency = 10,
				Width = 400,
				Margin = new Padding(0, 2, 12, 2)
			};
			slider.Scroll += (s, e) =>
			{
				bar.Progress = slider.Value;
				lbBarState.Text = $"Progress = {bar.Progress}　范围 [{bar.MinValue}, {bar.MaxValue}]" +
								  (bar.Progress < bar.MinValue || bar.Progress > bar.MaxValue
									  ? "　← 已越界！宽度会被 MapValue 外推，进度条可能消失或溢出"
									  : "");
			};

			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 2, 0, 4)
			};
			row.Controls.Add(Ui.Text("拖动：", 45));
			row.Controls.Add(slider);
			row.Controls.Add(Ui.Btn("换颜色", (s, e) =>
			{
				bar.BarColor = NextColor(new[] { Color.SeaGreen, Color.DodgerBlue, Color.OrangeRed, Color.MediumPurple }, bar.BarColor);
				lbBarState.Text = $"BarColor = {bar.BarColor.Name}";
			}, 90));
			g.Controls.Add(row);

			lbBarState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 4, 0, 4) };
			lbBarState.Text = $"Progress = {bar.Progress}　范围 [{bar.MinValue}, {bar.MaxValue}]";
			g.Controls.Add(lbBarState);
			Ui.AddHint(g, "已修复：Progress 现在会在内部钳制到 [MinValue, MaxValue] 与 [0, 控件宽度]。" +
						  "把滑块拖到 -20 或 120 可以看到进度条停在边界而不会消失或溢出。");
		}

		#endregion

		#region ProgressRing

		private void BuildRing(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "5. Comp.ProgressRing 环形进度");
			var g = Ui.Group(content, "属性：Progress(0-100 自动钳制) / BarColor");

			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 4, 0, 4)
			};

			ring = new Comp.ProgressRing
			{
				Size = new Size(120, 120),
				Progress = 25f,
				BarColor = Color.OrangeRed,
				Margin = new Padding(0, 2, 20, 2)
			};

			var slider = new TrackBar
			{
				Minimum = 0,
				Maximum = 100,
				Value = 25,
				TickFrequency = 10,
				Width = 360,
				Margin = new Padding(0, 30, 12, 2)
			};
			slider.Scroll += (s, e) =>
			{
				ring.Progress = slider.Value;
				lbRingState.Text = $"Progress = {ring.Progress}　BarColor = {ring.BarColor.Name}";
			};

			row.Controls.Add(ring);
			row.Controls.Add(slider);
			row.Controls.Add(Ui.Btn("换颜色", (s, e) =>
			{
				ring.BarColor = NextColor(new[] { Color.OrangeRed, Color.DodgerBlue, Color.SeaGreen, Color.Gold }, ring.BarColor);
				lbRingState.Text = $"BarColor = {ring.BarColor.Name}";
			}, 90));
			g.Controls.Add(row);

			lbRingState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 4, 0, 4) };
			lbRingState.Text = $"Progress = {ring.Progress}　BarColor = {ring.BarColor.Name}";
			g.Controls.Add(lbRingState);
			Ui.AddHint(g, "圆环从 12 点方向顺时针绘制，中心自动显示百分比文本。Progress 超出 0-100 会被自动钳制。");
		}

		#endregion

		#region Panels

		private void BuildPanels(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "6. Comp.Panels 圆角面板");
			var g = Ui.Group(content, "属性：CornerRadius（≥0，负值被忽略）");

			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 4, 0, 4)
			};

			panels = new Comp.Panels
			{
				Size = new Size(300, 110),
				CornerRadius = 18,
				BackColor = Color.FromArgb(230, 240, 255),
				Margin = new Padding(0, 2, 20, 2)
			};
			panels.Controls.Add(new Label
			{
				Text = "我是 Comp.Panels\r\n一个带圆角的 Panel，可以直接当容器用",
				AutoSize = true,
				Location = new Point(16, 16),
				ForeColor = Color.FromArgb(40, 60, 110)
			});

			// 对照用的普通 Panel
			var plain = new WinPanel
			{
				Size = new Size(300, 110),
				BackColor = Color.FromArgb(240, 240, 240),
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(0, 2, 0, 2)
			};
			plain.Controls.Add(new Label
			{
				Text = "我是普通 System.Windows.Forms.Panel\r\n（直角，用于对照）",
				AutoSize = true,
				Location = new Point(16, 16),
				ForeColor = Color.FromArgb(90, 90, 90)
			});

			row.Controls.Add(panels);
			row.Controls.Add(plain);
			g.Controls.Add(row);

			Ui.Row(g,
				Ui.Btn("圆角 +5", (s, e) => SetRadius(panels.CornerRadius + 5), 90),
				Ui.Btn("圆角 -5", (s, e) => SetRadius(panels.CornerRadius - 5), 90),
				Ui.Btn("设为 0（直角）", (s, e) => SetRadius(0), 110),
				Ui.Btn("试试负数 -10", (s, e) => SetRadius(-10), 120),
				Ui.Btn("换背景色", (s, e) =>
				{
					var colors = new[]
					{
						Color.FromArgb(230, 240, 255), Color.FromArgb(232, 248, 236),
						Color.FromArgb(255, 240, 232), Color.FromArgb(245, 236, 255)
					};
					int idx = Array.FindIndex(colors, c => c.ToArgb() == panels.BackColor.ToArgb());
					panels.BackColor = colors[(idx + 1 + colors.Length) % colors.Length];
				}, 110));
			Ui.AddHint(g,
				"CornerRadius 的 setter 里有 value >= 0 判断，传负数会被静默忽略并保留原值 —— " +
				"点「试试负数 -10」可以看到圆角没有任何变化。圆角由 Tool.SetControlFillet 用 Win32 区域实现，属硬边缘。");
		}

		private void SetRadius(int r)
		{
			panels.CornerRadius = r;
			Ui.Log($"Comp.Panels.CornerRadius 设为 {r}，实际生效值 {panels.CornerRadius}" +
				   (r < 0 ? "（负数被忽略）" : ""), "Info");
		}

		#endregion

		#region 定时刷新

		/// <summary>由 MainForm 在需要时启动动画定时器</summary>
		public void StartAnimation()
		{
			if (animTimer == null)
			{
				animTimer = new Timer { Interval = 1000 };
				animTimer.Tick += (s, e) =>
				{
					tick++;
					flow.FlowAdd(rnd.Next(10, 90));
					PushChartData();

					// 让条形进度与环形进度同步动起来
					int v = 50 + (int)(45 * Math.Sin(tick / 6.0));
					bar.Progress = v;
					ring.Progress = v;
					lbBarState.Text = $"Progress = {bar.Progress}（动画）　范围 [{bar.MinValue}, {bar.MaxValue}]";
					lbRingState.Text = $"Progress = {ring.Progress}（动画）　BarColor = {ring.BarColor.Name}";
				};
			}
			animTimer.Start();
		}

		#endregion
	}
}
