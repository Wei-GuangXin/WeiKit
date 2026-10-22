using System;
using System.Drawing;
using System.Windows.Forms;
using WeiKit;
using WeiKit.Modms;

namespace WeiKit.Demo
{
	/// <summary>
	/// 示例插件：演示如何继承 <see cref="Module_Base"/> 实现一个完整的 WeiKit 插件。
	/// 覆盖了配置读取、初始化 / 退出、主页切换、页面窗口以及日志 / 配置事件。
	/// 注意：为了让 <see cref="ModuleManager"/> 能通过反射实例化，本类必须是 public 且有无参构造。
	/// </summary>
	public class SamplePlugin : Module_Base
	{
		private Form _page;

		/// <summary>返回插件关心的宿主配置项名称列表。</summary>
		public override string[] GetConfigsNames()
		{
			return new[] { "Plugin.Greeting" };
		}

		/// <summary>初始化：读取注入的配置，构造页面窗口，并发出日志事件。</summary>
		public override void Module_Init()
		{
			Console.WriteLine($"[示例插件] 初始化完成，Plugin.Greeting = {ReadGreeting()}");

			_page = BuildPage();
			PageWindows = _page;

			OnLogsChange();       // 通知宿主：产生了新日志
			base.Module_Init();   // 触发 ModuleInitDoneEvent
		}

		/// <summary>退出：释放页面窗口等资源。</summary>
		public override void Module_Exit()
		{
			Console.WriteLine("[示例插件] 正在退出并释放资源…");
			if (_page != null && !_page.IsDisposed)
				_page.Dispose();
			_page = null;
			PageWindows = null;

			base.Module_Exit();   // 触发 ModuleExitEvent
		}

		/// <summary>宿主通知切换到主页面。</summary>
		public override void GoToMainPage()
		{
			Console.WriteLine("[示例插件] 收到「切换到主页面」通知。");
			OnLogsChange();
		}

		/// <summary>演示方法：执行一次工作。</summary>
		public void DoWork()
		{
			Console.WriteLine($"[示例插件] 执行了一次工作：{DateTime.Now:HH:mm:ss.fff}");
			OnLogsChange();
		}

		/// <summary>演示方法：修改配置并通知宿主回写。</summary>
		public void SetGreeting(string text)
		{
			if (Configs != null && Configs.WriteValue("Plugin.Greeting", text))
			{
				Console.WriteLine($"[示例插件] 配置已更新：Plugin.Greeting = {text}");
				OnConfigsChange();
			}
		}

		private string ReadGreeting()
		{
			if (Configs == null) return "（未注入配置）";
			object v = Configs.Read("Plugin.Greeting");
			return v == null ? "（无此配置项）" : v.ToString();
		}

		private Form BuildPage()
		{
			var form = new Form
			{
				Text = "示例插件页面",
				StartPosition = FormStartPosition.CenterScreen,
				ClientSize = new Size(380, 200),
				Font = new Font("Microsoft YaHei UI", 9F),
				ShowInTaskbar = false,
				MinimizeBox = false,
				MaximizeBox = false
			};

			form.Controls.Add(new Label
			{
				Text = "我是示例插件\r\n\r\n当前配置 Plugin.Greeting = " + ReadGreeting() +
					   "\r\n\r\n这个窗口由插件自身创建，宿主通过 PageWindows 属性显示或嵌入。",
				AutoSize = true,
				Location = new Point(24, 20),
				MaximumSize = new Size(330, 0)
			});

			return form;
		}
	}
}
