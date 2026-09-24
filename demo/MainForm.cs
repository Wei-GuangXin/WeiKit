using System;
using System.Drawing;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// 演示主窗体。用 TabControl 把库的五个模块 + 自绘控件分成独立的演示页。
	/// </summary>
	public class MainForm : Form
	{
		private readonly TabControl tabs = new TabControl();
		private readonly StatusStrip status = new StatusStrip();
		private readonly ToolStripStatusLabel lbLogCount = new ToolStripStatusLabel();
		private readonly ToolStripStatusLabel lbConfig = new ToolStripStatusLabel();
		private readonly ToolStripStatusLabel lbVersion = new ToolStripStatusLabel();


		public MainForm()
		{
			Text = $"{Program.DisplayName} 示例演示程序";
			StartPosition = FormStartPosition.CenterScreen;
			ClientSize = new Size(1024, 720);
			MinimumSize = new Size(900, 620);
			Font = Ui.BaseFont;

			BuildMenu();
			BuildTabs();
			BuildStatus();

			// 配置里保存的「窗口置顶」立即生效
			TopMost = ReadBool("App.TopMost", false);

			// 日志窗口打开/关闭都由库管理，这里只在合适时机调用
			Program.Log.Println("主窗体已显示。", "Info");
		}

		#region 菜单

		private void BuildMenu()
		{
			var menu = new MenuStrip { Font = Ui.BaseFont };

			var mFile = new ToolStripMenuItem("文件(&F)");
			mFile.DropDownItems.Add("打开程序目录(&O)", null, (s, e) => Tool.OpenTypeFile(Tool.GetProgramPath()));
			mFile.DropDownItems.Add("打开工作目录(&W)", null, (s, e) => Tool.OpenTypeFile(Program.WorkDir));
			mFile.DropDownItems.Add(new ToolStripSeparator());
			mFile.DropDownItems.Add("退出(&X)", null, (s, e) => Close());

			var mView = new ToolStripMenuItem("视图(&V)");
			mView.DropDownItems.Add("日志查看窗口(&L)", null, (s, e) => OpenLogWindow());
			mView.DropDownItems.Add("配置管理窗口(&C)", null, (s, e) => OpenConfigWindow());
			mView.DropDownItems.Add(new ToolStripSeparator());
			mView.DropDownItems.Add("窗口置顶(&T)", null, (s, e) => ToggleTopMost());

			var mTools = new ToolStripMenuItem("工具(&T)");
			mTools.DropDownItems.Add("以管理员身份重启(&A)", null, (s, e) => ProgTool.RunAsAdmin());
			mTools.DropDownItems.Add("重启程序(&R)", null, (s, e) =>
			{
				if (MessageBox.Show("确定要立即重启程序吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					ProgTool.ProgramRestart();
			});
			mTools.DropDownItems.Add(new ToolStripSeparator());
			mTools.DropDownItems.Add("导出日志(&E)…", null, ExportLog);

			var mHelp = new ToolStripMenuItem("帮助(&H)");
			mHelp.DropDownItems.Add("库信息(&A)", null, (s, e) => Ui.Info(
				$"项目名称：{Program.DisplayName}\r\n程序集：WeiKit.dll\r\n库回报的名称：{Info.LibraryName}\r\n作者：{Info.LibraryAuthor}\r\n网址：{Info.LibraryUrl}\r\n\r\n" +
				$"程序目录：{Tool.GetProgramPath()}\r\n配置文件：{Program.ConfigPath}",
				"关于"));

			menu.Items.AddRange(new ToolStripItem[] { mFile, mView, mTools, mHelp });
			MainMenuStrip = menu;
			Controls.Add(menu);
		}

		#endregion

		#region 标签页

		private void BuildTabs()
		{
			tabs.Dock = DockStyle.Fill;
			tabs.Font = Ui.BaseFont;
			tabs.Padding = new Point(14, 6);

			tabs.TabPages.Add(BuildHomeTab());
			tabs.TabPages.Add(WrapTab("① 配置管理", new ConfigDemo().Build()));
			tabs.TabPages.Add(WrapTab("② 日志・加密解密", new LogCryptoDemo().Build()));
			tabs.TabPages.Add(WrapTab("③ 程序工具", new ToolDemo().Build()));

			var net = new NetDemo();
			tabs.TabPages.Add(WrapTab("④ 网络", net.Build()));

			tabs.TabPages.Add(WrapTab("⑤ 自绘控件", new CompDemo().Build()));

			tabs.TabPages.Add(BuildAboutTab());

			Controls.Add(tabs);
		}

		private static TabPage WrapTab(string title, Control content)
		{
			var page = new TabPage(title) { BackColor = SystemColors.Control };
			content.Dock = DockStyle.Fill;
			page.Controls.Add(content);
			return page;
		}

		private TabPage BuildHomeTab()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			content.Controls.Add(new Label
			{
				Text = $"{Program.DisplayName} · 示例演示程序",
				Font = new Font(Ui.BaseFont.FontFamily, 17F, FontStyle.Bold),
				ForeColor = Ui.Accent,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			});

			content.Controls.Add(Ui.Hint(
				"这个 Demo 用最少的代码演示 WeiKit 精简后的全部能力：配置、日志、加密解密、程序工具、网络，以及 6 个自绘控件。" +
				"所有演示都在真实读写文件/网络，日志会实时出现在「日志查看窗口」中。", 940));

			var grid = Ui.Grid(200);
			Ui.SetupGrid(grid, "标签页", "演示内容");
			Ui.AddRow(grid, "① 配置管理", "声明/读写/重置配置项、导入导出、内置配置管理窗口（树形分组 + 搜索）");
			Ui.AddRow(grid, "② 日志・加密解密", "四种日志级别、实时日志窗口、13 种主题、AES 加密落盘与解密、篡改检测");
			Ui.AddRow(grid, "③ 程序工具", "机器码、编译时间、文件 SHA256、随机串、路径转换、注册表自启动、DataFlowList 统计");
			Ui.AddRow(grid, "④ 网络", "GET/POST、图片下载、多线程下载器（带进度）、multipart 上传、内置 WebServer、钉钉机器人");
			Ui.AddRow(grid, "⑤ 自绘控件", "Switch / Led / Chart / ProgressBar / ProgressRing / Panels 六个控件实时演示");
			content.Controls.Add(grid);

			Ui.AddHeader(content, "快捷入口");
			Ui.Row(content,
				Ui.Btn("打开日志窗口", (s, e) => OpenLogWindow(), 150),
				Ui.Btn("打开配置窗口", (s, e) => OpenConfigWindow(), 150),
				Ui.Btn("打印一条测试日志", (s, e) => Program.Log.Println($"测试日志 {DateTime.Now:HH:mm:ss.fff}", "Info"), 170),
				Ui.Btn("打开工作目录", (s, e) => Tool.OpenTypeFile(Program.WorkDir), 150));

			Ui.AddHeader(content, "当前运行环境");
			var env = Ui.Grid(120);
			Ui.SetupGrid(env, "项目", "值");
			Ui.AddRow(env, "程序目录", Tool.GetProgramPath());
			Ui.AddRow(env, "工作目录", Program.WorkDir);
			Ui.AddRow(env, "配置文件", Program.ConfigPath);
			Ui.AddRow(env, "是否管理员", ProgTool.IsRunAsAdmin() ? "是" : "否");
			Ui.AddRow(env, "本机 IPv4", string.Join(" / ", Tool.GetLocalIPv4List()));
			content.Controls.Add(env);

			Ui.AddHint(content, "提示：按 F5 或使用工具栏可随时重新读取；配置文件为 AES 加密存储，直接用记事本打开是乱码。");

			return new TabPage("首页") { BackColor = SystemColors.Control, Controls = { host } };
		}

		private TabPage BuildAboutTab()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHeader(content, "库信息");
			var g1 = Ui.Grid(120);
			Ui.SetupGrid(g1, "项", "值");
			Ui.AddRow(g1, "项目名称", Program.DisplayName);
			Ui.AddRow(g1, "LibraryAuthor", Info.LibraryAuthor);
			Ui.AddRow(g1, "LibraryUrl", Info.LibraryUrl);
			Ui.AddRow(g1, "程序集版本", typeof(Configs).Assembly.GetName().Version.ToString());
			content.Controls.Add(g1);

			Ui.AddHeader(content, "本 Demo 演示的 API 速查");
			var g2 = Ui.Grid(300);
			Ui.SetupGrid(g2, "模块", "主要 API");
			Ui.AddRow(g2, "配置", "Configs.Add / Read / WriteValue / Load / Save / Reset / InputData / OutSave / ShowConfigManagForm");
			Ui.AddRow(g2, "日志", "Logs.Println / LogsSave / LogAddEvent / RedirectConsoleOut / ShowLogsForm / MaxLogPcs / AutoSavePath");
			Ui.AddRow(g2, "主题", "ConsoleLogTheme.GetHtmlHeader(ThemeType)（13 种主题）");
			Ui.AddRow(g2, "加密", "CryptoHelper.SaveEncrypted<T> / LoadDecrypted<T>");
			Ui.AddRow(g2, "工具", "Tool.GetProgramPath / GetRandomString / PathConversion / RunCmdCode / CalculateFileChecksum / ComputeSha256Hash / SetControlFillet / SafeInvoke / DataFlowList");
			Ui.AddRow(g2, "程序", "ProgTool.IsRunAsAdmin / RunAsAdmin / ProgramRestart / GenerateMachineCode / GetBuildTime / UninstallClickOnce");
			Ui.AddRow(g2, "网络", "HttpLink.GetTask / PostTask / GetImageFromUrl / GetClientIP、DownTool、UploadTool、WebServer、DingDingAPI");
			Ui.AddRow(g2, "控件", "Comp.Switch / Led / Chart / ProgressBar / ProgressRing / Panels");
			content.Controls.Add(g2);

			Ui.AddHeader(content, "已知限制（仍建议遵守）");
			Ui.AddHint(content,
				"· Configs 现在有三个 Save 重载：Save()（当前文件，自动沿用密钥）、Save(path)、Save(path, key)——不会再出现「路径被当成密钥」；\r\n" +
				"· 必须先 Add 全部配置项再 Load，否则文件里没有的项会在载入后消失；\r\n" +
				"· Logs.AutoSavePath 默认已指向程序目录下的 logs\\weikit_autosave.log，落盘失败会保留日志不清空；\r\n" +
				"· RedirectConsoleOut 现在可逆（返回原始输出流）并内置重入保护，事件里写 Console 不会再栈溢出；\r\n" +
				"· DownTool 需要服务端支持 HEAD 与 Range，不确定时请用 DownMode.SingleThread；\r\n" +
				"· WebServer 已加入路径穿越防护（越界返回 403）；LANS 模式监听 * 前缀仍需 URL ACL 或管理员权限。", 940);

			return new TabPage("关于") { BackColor = SystemColors.Control, Controls = { host } };
		}

		#endregion

		#region 状态栏与定时刷新

		private void BuildStatus()
		{
			status.Font = Ui.BaseFont;
			lbVersion.Text = $"{Program.DisplayName} (WeiKit.dll) {typeof(Configs).Assembly.GetName().Version}";
			lbVersion.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
			lbConfig.Spring = true;
			lbConfig.TextAlign = ContentAlignment.MiddleLeft;
			lbLogCount.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;

			status.Items.AddRange(new ToolStripItem[] { lbVersion, lbConfig, lbLogCount });
			Controls.Add(status);

			var timer = new Timer { Interval = 500 };
			timer.Tick += (s, e) =>
			{
				lbLogCount.Text = $"日志 {Program.Log.logs.Count} 条";
				lbConfig.Text = $" 配置文件：{Program.ConfigPath}（{(Program.Cfg.IsEffective ? "已载入" : "未载入")}）";
			};
			timer.Start();

			// 日志类型着色，让状态栏也能反映最新一条日志
			Program.Log.LogAddEvent += (s, log) =>
			{
				if (IsDisposed) return;
				Tool.SafeInvoke(this, () =>
				{
					switch (log.TypeText)
					{
						case "Error": lbLogCount.ForeColor = Color.Firebrick; break;
						case "Warning": lbLogCount.ForeColor = Color.DarkOrange; break;
						default: lbLogCount.ForeColor = SystemColors.ControlText; break;
					}
				});
			};
		}

		#endregion

		#region 公共操作

		private void OpenLogWindow()
		{
			// ShowLogsForm 内部会自动完成 new Logsinfo -> 赋 logs -> 显示，顺序不会错
			IntPtr h = Program.Log.ShowLogsForm(autoColorMode: true);
			Program.Log.Println($"日志窗口已打开，句柄 = 0x{h.ToInt64():X}", "Info");
		}

		private void OpenConfigWindow()
		{
			// 关闭配置窗口时用密钥落盘，避免使用窗口自带的保存按钮（会退化为明文）
			Program.Cfg.ConfigManagFormClosed += OnConfigFormClosed;
			Program.Cfg.ShowConfigManagForm(TopDisplay: TopMost);
		}

		private void OnConfigFormClosed(object sender, EventArgs e)
		{
			// 事件只订阅一次，回调后立即退订
			Program.Cfg.ConfigManagFormClosed -= OnConfigFormClosed;

			Program.Cfg.Save();
			Program.Log.Println("配置管理窗口已关闭，已用密钥保存配置。", "Info");
			TopMost = ReadBool("App.TopMost", false);
		}

		private void ToggleTopMost()
		{
			TopMost = !TopMost;
			Program.Cfg.WriteValue("App.TopMost", TopMost);
			Program.Cfg.Save();
			Program.Log.Println($"窗口置顶 = {TopMost}", "Info");
		}

		private void ExportLog(object sender, EventArgs e)
		{
			using (var dlg = new SaveFileDialog
			{
				Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt",
				FileName = $"weikit_demo_{DateTime.Now:yyyyMMdd_HHmmss}.log"
			})
			{
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				if (Program.Log.LogsSave(dlg.FileName))
					Ui.Info($"日志已导出：\r\n{dlg.FileName}\r\n共 {Program.Log.logs.Count} 条。");
				else
					Ui.Error("日志导出失败，请检查路径是否可写。");
			}
		}

		private static bool ReadBool(string name, bool fallback)
		{
			object v = Program.Cfg.Read(name);
			if (v == null) return fallback;
			bool b;
			return bool.TryParse(v.ToString(), out b) ? b : fallback;
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			Program.Log.Println("主窗体正在关闭，保存配置…", "Info");
			Program.Cfg.Save();
			base.OnFormClosing(e);
		}

		#endregion
	}
}
