using System;
using System.Windows.Forms;

namespace WeiKit.Modms
{
	public interface Module_Interface
	{
		/*
		WeiKit 模块（插件）接口定义 
		 */
		//通过WindowsAPI窗口嵌入机制接口
		IntPtr WindowsHwnd { get; set; }
		event EventHandler WindowsChangeEvent;//用来告诉宿主插件的窗口句柄已经改变，宿主需要重新嵌入新插件窗口

		//通过窗口实例嵌入机制接口
		Form PageWindows { get; set; }
		//功能事件/属性
		event EventHandler LogsChangeEvent;//用来告诉宿主插件产生了新的日志，宿主可以进行一些日志处理工作
		event EventHandler ModuleExitEvent;//用来告诉宿主插件已经退出，宿主可以进行一些清理工作
		event EventHandler ConfigsChangeEvent;//用来告诉宿主插件的配置数据已经改变，宿主可以进行一些保存工作
		event EventHandler ModuleInitDoneEvent;//用来告诉宿主插件的初始化已经完成，宿主可以进行一些后续工作
		/// <summary>
		/// 插件的配置属性对象，插件可以通过该属性对象获取和设置插件的配置数据
		/// 这个属性的持久化由宿主的插件载入逻辑管理。
		/// </summary>
		Configs Configs { get; set; }
		//公开方法
		/// <summary>
		/// 插件的初始化逻辑
		/// </summary>
		void Module_Init();
		/// <summary>
		/// 插件的退出逻辑
		/// </summary>
		void Module_Exit();
		/// <summary>
		/// 宿主通知插件切换到主页面，插件需要自行处理切换逻辑
		/// </summary>
		void GoToMainPage();
		/// <summary>
		/// 宿主会获取当前插件的配置项名列表，并从主配置文件中读取对应的配置数据，传递给插件的Configs属性对象
		/// 请注意，如果主配置文件中没有对应的配置项，Configs属性也不会有对应的配置数据，插件需要自行处理这种情况。
		/// </summary>
		/// <returns></returns>
		string[] GetConfigsNames();
	}

	/// <summary>
	/// 模块（插件）基类：提供 <see cref="Module_Interface"/> 的默认实现，
	/// 并为各事件提供受保护的触发辅助方法，插件继承后只需重写关心的成员即可。
	/// </summary>
	public abstract class Module_Base : Module_Interface
	{
		/// <summary>通过 Windows API 窗口句柄嵌入宿主。默认值为 <see cref="IntPtr.Zero"/>。</summary>
		public virtual IntPtr WindowsHwnd { get; set; }

		/// <summary>通过窗口实例嵌入宿主。默认值为 null。</summary>
		public virtual Form PageWindows { get; set; }

		/// <summary>插件的配置对象，由宿主的插件载入逻辑注入。</summary>
		public virtual Configs Configs { get; set; }

		/// <summary>窗口句柄改变事件。</summary>
		public event EventHandler WindowsChangeEvent;
		/// <summary>产生新日志事件。</summary>
		public event EventHandler LogsChangeEvent;
		/// <summary>插件退出事件。</summary>
		public event EventHandler ModuleExitEvent;
		/// <summary>配置数据改变事件。</summary>
		public event EventHandler ConfigsChangeEvent;
		/// <summary>插件初始化完成事件。</summary>
		public event EventHandler ModuleInitDoneEvent;

		/// <summary>插件初始化。默认实现直接触发 <see cref="ModuleInitDoneEvent"/>。</summary>
		public virtual void Module_Init() { OnModuleInitDone(); }

		/// <summary>插件退出。默认实现触发 <see cref="ModuleExitEvent"/>。</summary>
		public virtual void Module_Exit() { OnModuleExit(); }

		/// <summary>切换到主页面。默认无操作，需要时由插件重写。</summary>
		public virtual void GoToMainPage() { }

		/// <summary>返回插件关心的配置项名称列表。默认返回 null，表示不读取宿主配置。</summary>
		public virtual string[] GetConfigsNames() { return null; }

		/// <summary>触发窗口句柄改变事件。</summary>
		protected void OnWindowsChange() { WindowsChangeEvent?.Invoke(this, EventArgs.Empty); }
		/// <summary>触发新日志事件。</summary>
		protected void OnLogsChange() { LogsChangeEvent?.Invoke(this, EventArgs.Empty); }
		/// <summary>触发插件退出事件。</summary>
		protected void OnModuleExit() { ModuleExitEvent?.Invoke(this, EventArgs.Empty); }
		/// <summary>触发配置数据改变事件。</summary>
		protected void OnConfigsChange() { ConfigsChangeEvent?.Invoke(this, EventArgs.Empty); }
		/// <summary>触发插件初始化完成事件。</summary>
		protected void OnModuleInitDone() { ModuleInitDoneEvent?.Invoke(this, EventArgs.Empty); }
	}
}
