using System;
using System.Reflection;
using System.Windows.Forms;
using WeiKit;
using WeiKit.Modms;

namespace WeiKit.Demo
{
	/// <summary>
	/// ⑦ 插件演示：用 <see cref="ModuleManager"/> 演示插件的载入 / 初始化 / 切换主页 / 卸载，
	/// 并附带一个继承 <see cref="Module_Base"/> 的示例插件。
	/// </summary>
	internal class ModuleDemo
	{
		private ModuleManager manager;
		private ListBox lbModules;
		private TextBox tbLog;
		private Label lbCount;

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"这一页演示 WeiKit.Modms 下的插件体系：插件实现 Module_Interface（通常继承 Module_Base），" +
				"由 ModuleManager 统一管理载入、配置注入、初始化、主页切换与卸载。" +
				"建议顺序：先点「载入示例插件」，再点「初始化全部」，然后可切主页 / 显示窗口 / 卸载。");

			manager = new ModuleManager(Program.Cfg);
			manager.ModuleLoaded += (s, e) => AppendLog($"[载入] {Describe(e.Module)}");
			manager.ModuleUnloaded += (s, e) => AppendLog($"[卸载] {Describe(e.Module)}");
			manager.ModuleInitDone += (s, e) => AppendLog($"[初始化完成] {Describe(e.Module)}");
			manager.ModuleLogsChanged += (s, e) => AppendLog($"[日志事件] {Describe(e.Module)}");
			manager.ModuleWindowsChanged += (s, e) => AppendLog($"[窗口句柄变更] {Describe(e.Module)}");

			EnsurePluginConfig();

			Ui.AddHeader(content, "操作");
			Ui.Row(content,
				Ui.Btn("载入示例插件", (s, e) => LoadSample(), 130),
				Ui.Btn("从当前程序集载入", (s, e) => LoadFromAssembly(), 160),
				Ui.Btn("初始化全部", (s, e) => InitAll(), 110),
				Ui.Btn("切换到主页", (s, e) => manager.GoToMainPageAll(), 110),
				Ui.Btn("显示插件窗口", (s, e) => ShowSelectedWindow(), 130),
				Ui.Btn("卸载选中", (s, e) => UnloadSelected(), 100),
				Ui.Btn("卸载全部", (s, e) => UnloadAll(), 100));

			Ui.AddHeader(content, "已载入插件");
			lbCount = Ui.Text("已载入插件数量：0");
			content.Controls.Add(lbCount);

			lbModules = new ListBox
			{
				Width = 640,
				Height = 120,
				IntegralHeight = false,
				Font = Ui.MonoFont,
				Margin = new Padding(0, 4, 0, 8)
			};
			content.Controls.Add(lbModules);

			Ui.AddHeader(content, "生命周期日志");
			tbLog = Ui.Multi("", 920, 200);
			content.Controls.Add(tbLog);

			RefreshList();
			return host;
		}

		private void EnsurePluginConfig()
		{
			if (!Program.Cfg.IsHave("Plugin.Greeting"))
			{
				Program.Cfg.Add("Plugin.Greeting", Configs.ValueType.String, "Hello WeiKit", "插件问候语", "示例插件读取的问候语配置");
				Ui.Log("已为示例插件注册配置项 Plugin.Greeting。", "Info");
			}
		}

		private void LoadSample()
		{
			var plugin = new SamplePlugin();
			manager.LoadModule(plugin);
			AppendLog($"[手动载入] {plugin.GetType().Name}（尚未初始化）");
			RefreshList();
		}

		private void LoadFromAssembly()
		{
			int n = manager.LoadModulesFromAssembly(Assembly.GetExecutingAssembly());
			AppendLog($"[程序集载入] 从当前程序集发现并载入 {n} 个插件类型。");
			RefreshList();
		}

		private void InitAll()
		{
			int n = manager.InitializeAll();
			AppendLog($"[初始化] 已初始化 {n} 个插件。");
			RefreshList();
		}

		private void ShowSelectedWindow()
		{
			Module_Interface m = SelectedModule();
			if (m == null) { Ui.Error("请先在上方列表选中一个已载入的插件。"); return; }

			Form page = m.PageWindows;
			if (page == null || page.IsDisposed) { Ui.Error("该插件没有可显示的页面窗口（可能尚未初始化）。"); return; }

			page.ShowDialog();
		}

		private void UnloadSelected()
		{
			Module_Interface m = SelectedModule();
			if (m == null) { Ui.Error("请先在上方列表选中一个已载入的插件。"); return; }

			manager.ExitModule(m);
			RefreshList();
		}

		private void UnloadAll()
		{
			int n = manager.ExitAll();
			AppendLog($"[卸载] 已卸载 {n} 个插件。");
			RefreshList();
		}

		private Module_Interface SelectedModule()
		{
			if (lbModules == null || lbModules.SelectedIndex < 0) return null;
			Module_Interface[] mods = manager.Modules;
			if (lbModules.SelectedIndex >= mods.Length) return null;
			return mods[lbModules.SelectedIndex];
		}

		private void RefreshList()
		{
			if (lbModules == null) return;

			lbModules.Items.Clear();
			foreach (var m in manager.Modules)
				lbModules.Items.Add(m.GetType().Name);

			lbCount.Text = $"已载入插件数量：{manager.Count}";
		}

		private void AppendLog(string text)
		{
			if (tbLog == null) return;
			tbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\r\n");
		}

		private static string Describe(Module_Interface m)
			=> m == null ? "(null)" : m.GetType().Name;
	}
}
