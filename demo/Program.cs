using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WeiKit;

namespace WeiKit.Demo
{
	/// <summary>
	/// 程序入口。演示 WeiKit 推荐的初始化顺序：
	/// 接管控制台输出 -> 声明并载入配置 -> 挂接配置变更事件 -> 启动主窗体。
	/// </summary>
	internal static class Program
	{
		/// <summary>全局日志实例（整个演示程序共用一份）</summary>
		public static readonly Logs Log = new Logs();

		/// <summary>全局配置实例</summary>
		public static readonly Configs Cfg = new Configs();

		/// <summary>配置文件的加密密钥。改成空字符串即为明文保存。</summary>
		public const string ConfigSaveKey = "weikit-demo-key-2026";

		/// <summary>
		/// 演示程序展示用的库名称。
		/// 注意：库自身的 <see cref="WeiKit.Info.LibraryName"/> 常量值仍是 "WeiKit Library"
		/// （属于库的公开 API，未改动），这里只影响界面上显示的名字。
		/// </summary>
		public const string DisplayName = "WeiKit-light";

		/// <summary>演示用工作目录（配置、加密文件、下载文件都放在这里）</summary>
		public static string WorkDir
		{
			get
			{
				string dir = Path.Combine(Tool.GetProgramPath(), "DemoData");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return dir;
			}
		}

		/// <summary>配置文件完整路径</summary>
		public static string ConfigPath => Path.Combine(WorkDir, "demo.config");

		/// <summary>加密演示文件完整路径</summary>
		public static string SecretPath => Path.Combine(WorkDir, "secret.dat");

		[STAThread]
		private static void Main(string[] args)
		{
			// ① 先抓住「真正的控制台输出流」。
			// RedirectConsoleOut 之后 Console.Out 就指向日志列表了，
			// 若此时再在 LogAddEvent 里调用 Console.WriteLine，会形成
			// 写日志 → 触发事件 → 写控制台 → 又写日志 的无限递归（栈溢出）。
			TextWriter realConsole = Console.Out;

			// ② 让所有 Console.WriteLine（库内部大量使用）都进入日志系统
			Log.RedirectConsoleOut();

			// 日志数量上限与自动落盘目录。注意：这里必须给到「文件名」，不能只给目录
			Log.MaxLogPcs = 500;
			Log.AutoSavePath = Path.Combine(WorkDir, "auto_saved.log");

			Log.Println($"=== {Info.LibraryName} 演示程序启动 ===", "Info");
			Log.Println($"程序目录：{Tool.GetProgramPath()}", "Info");
			Log.Println($"工作目录：{WorkDir}", "Info");
			Log.Println($"程序集版本：{typeof(Configs).Assembly.GetName().Version}", "Info");

			InitConfig();

			// 无界面自检模式：WeiKit.Demo.exe /smoke
			if (args != null && args.Length > 0 &&
				(string.Equals(args[0], "/smoke", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(args[0], "-smoke", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(args[0], "--smoke", StringComparison.OrdinalIgnoreCase)))
			{
				realConsole.WriteLine("WeiKit 自检模式：日志输出将实时回显到控制台。");
				// 关键：必须写 realConsole，不能写 Console（否则递归栈溢出）
				Log.LogAddEvent += (s, log) => realConsole.WriteLine("  # " + log);
				Environment.ExitCode = SmokeTest.Run(realConsole);
				return;
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}

		/// <summary>
		/// 声明配置项并载入。关键顺序：先 Add 出全部默认项，再 Load 用文件覆盖值。
		/// 反过来做的话，文件里没有的配置项会在 Load 后消失。
		/// </summary>
		private static void InitConfig()
		{
			// 内部名称用 . 分隔，配置管理窗口会自动按层级建树
			Cfg.Add("App.TopMost", Configs.ValueType.Bool, false, "窗口置顶", "主窗口是否总在最前");
			Cfg.Add("App.Theme", Configs.ValueType.String, "LightClean", "日志主题", "日志查看窗口的配色主题");
			Cfg.Add("App.AutoSaveLog", Configs.ValueType.Bool, true, "自动保存日志", "日志达到上限后自动落盘");

			Cfg.Add("Net.ServerPort", Configs.ValueType.Long, 5694L, "内置服务端口", "WebServer 的监听端口，范围 1024-65535");
			Cfg.Add("Net.TimeoutSeconds", Configs.ValueType.Float, 30.0f, "网络超时(秒)", "HTTP 请求的超时时间");
			Cfg.Add("Net.TestUrl", Configs.ValueType.String, "https://www.example.com/", "测试地址", "网络页测试用地址");

			Cfg.Add("Demo.UserName", Configs.ValueType.String, "Tiangong", "演示用户名", "用于展示字符串配置的读写");
			Cfg.Add("Demo.RunCount", Configs.ValueType.Long, 0L, "启动次数", "程序每启动一次自动 +1");

			// 载入（文件不存在时会返回 false，这是正常情况：写出默认配置即可）
			bool loaded = Cfg.Load(ConfigPath, ConfigSaveKey);
			if (loaded)
			{
				Log.Println($"配置载入成功：{Cfg.ConfigFilePath}（共 {Cfg.configs.Count} 项，加密={Cfg.IsEncrypted}）", "Info");
			}
			else
			{
				Log.Println("未找到配置文件，正在写出默认配置…", "Warning");

				// ⚠ 这里是本库最容易踩的坑：
				//   Save(string SaveKey = "")            ← 单参数调用会命中这个重载
				//   Save(string path, string SaveKey = "")
				// Load 失败时 Configs 还没记录 ConfigFilePath，此时写 Save(ConfigSaveKey) 没问题（因为传的是密钥），
				// 但若写成 Save(ConfigPath) 就会被当成「密钥」，又因路径为 null 而静默不写文件。
				// 结论：想写到指定路径，必须显式给两个实参。
				Cfg.Save(ConfigPath, ConfigSaveKey);

				Log.Println(File.Exists(ConfigPath)
					? $"默认配置已写出：{ConfigPath}"
					: $"配置写出失败（{ConfigPath} 不可写？）", File.Exists(ConfigPath) ? "Info" : "Error");
			}

			// 启动次数累加，顺便演示 WriteValue + Save
			long runCount = 0;
			object raw = Cfg.Read("Demo.RunCount");
			if (raw != null) long.TryParse(raw.ToString(), out runCount);
			Cfg.WriteValue("Demo.RunCount", runCount + 1);
			Cfg.Save();
			Log.Println($"这是第 {runCount + 1} 次启动演示程序。", "Info");

			// 配置变更统一在这里响应
			Cfg.ConfigChanged += configs =>
			{
				Log.Println("[配置变更] 配置数据已更新。", "Info");
			};
		}
	}
}
