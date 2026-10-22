using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WeiKit.Modms
{
	/// <summary>
	/// 模块（插件）生命周期管理器。
	/// 负责插件的载入、独立配置装配、事件挂接、初始化、主页切换与退出清理。
	/// </summary>
	public class ModuleManager
	{
		private readonly List<Module_Interface> _modules = new List<Module_Interface>();
		private readonly object _lock = new object();

		/// <summary>
		/// 宿主级配置对象。插件通过 <see cref="Module_Interface.GetConfigsNames"/>
		/// 声明关心的配置项，管理器会从宿主配置中拆分出对应的子配置注入给插件。
		/// </summary>
		public Configs HostConfigs { get; set; }

		/// <summary>插件载入后触发。</summary>
		public event EventHandler<ModuleEventArgs> ModuleLoaded;
		/// <summary>插件卸载后触发。</summary>
		public event EventHandler<ModuleEventArgs> ModuleUnloaded;
		/// <summary>插件的窗口句柄发生改变时转发给宿主。</summary>
		public event EventHandler<ModuleEventArgs> ModuleWindowsChanged;
		/// <summary>插件产生新日志时转发给宿主。</summary>
		public event EventHandler<ModuleEventArgs> ModuleLogsChanged;
		/// <summary>插件初始化完成时转发给宿主。</summary>
		public event EventHandler<ModuleEventArgs> ModuleInitDone;

		public ModuleManager()
		{
		}

		public ModuleManager(Configs hostConfigs)
		{
			HostConfigs = hostConfigs;
		}

		/// <summary>当前已载入的插件快照。</summary>
		public Module_Interface[] Modules
		{
			get { lock (_lock) return _modules.ToArray(); }
		}

		/// <summary>当前已载入的插件数量。</summary>
		public int Count
		{
			get { lock (_lock) return _modules.Count; }
		}

		/// <summary>载入一个插件实例（不立即初始化，需调用 <see cref="InitializeAll"/>）。</summary>
		public Module_Interface LoadModule(Module_Interface module)
		{
			if (module == null) throw new ArgumentNullException(nameof(module));

			lock (_lock)
			{
				if (_modules.Contains(module))
				{
					Console.WriteLine("插件已载入，忽略重复载入。");
					return module;
				}

				module.Configs = BuildModuleConfigs(module);
				Subscribe(module);
				_modules.Add(module);
			}

			ModuleLoaded?.Invoke(this, new ModuleEventArgs(module));
			return module;
		}

		/// <summary>通过类型实例化并载入一个插件。</summary>
		public T LoadModule<T>() where T : Module_Interface, new()
		{
			T module = new T();
			LoadModule(module);
			return module;
		}

		/// <summary>从指定程序集文件中载入所有实现了 <see cref="Module_Interface"/> 的插件类型。</summary>
		public int LoadModulesFromFile(string assemblyPath)
		{
			if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
			{
				Console.WriteLine($"载入插件失败：程序集文件不存在 -> {assemblyPath}");
				return 0;
			}

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"载入插件程序集失败 -> {assemblyPath}：{ex.Message}");
				return 0;
			}

			return LoadModulesFromAssembly(assembly);
		}

		/// <summary>从指定目录下所有匹配的程序集文件中载入插件。</summary>
		public int LoadModulesFromDirectory(string directory, string searchPattern = "*.dll")
		{
			if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
			{
				Console.WriteLine($"载入插件失败：目录不存在 -> {directory}");
				return 0;
			}

			int count = 0;
			foreach (string file in Directory.GetFiles(directory, searchPattern))
			{
				try
				{
					count += LoadModulesFromFile(file);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"从 {file} 载入插件失败：{ex.Message}");
				}
			}
			return count;
		}

		/// <summary>从已加载的程序集中查找并载入插件类型。</summary>
		public int LoadModulesFromAssembly(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));

			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(t => t != null).ToArray();
			}

			int count = 0;
			foreach (Type type in types)
			{
				if (type == null || type.IsAbstract || type.IsInterface) continue;
				if (!typeof(Module_Interface).IsAssignableFrom(type)) continue;

				Module_Interface module;
				try
				{
					module = (Module_Interface)Activator.CreateInstance(type);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"实例化插件 {type.FullName} 失败：{ex.Message}");
					continue;
				}

				LoadModule(module);
				count++;
			}

			return count;
		}

		/// <summary>初始化所有已载入的插件。</summary>
		public int InitializeAll()
		{
			Module_Interface[] snapshot = Modules;
			int count = 0;
			foreach (Module_Interface module in snapshot)
			{
				try
				{
					module.Module_Init();
					count++;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"插件 {module.GetType().FullName} 初始化异常：{ex.Message}");
				}
			}
			return count;
		}

		/// <summary>通知所有插件切换到主页面。</summary>
		public void GoToMainPageAll()
		{
			Module_Interface[] snapshot = Modules;
			foreach (Module_Interface module in snapshot)
			{
				try
				{
					module.GoToMainPage();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"插件 {module.GetType().FullName} 切换主页异常：{ex.Message}");
				}
			}
		}

		/// <summary>退出并卸载指定插件。</summary>
		public bool ExitModule(Module_Interface module)
		{
			if (module == null) return false;

			bool removed;
			lock (_lock) removed = _modules.Remove(module);
			if (!removed) return false;

			try
			{
				module.Module_Exit();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"插件 {module.GetType().FullName} 退出异常：{ex.Message}");
			}
			finally
			{
				Unsubscribe(module);
				ModuleUnloaded?.Invoke(this, new ModuleEventArgs(module));
			}

			return true;
		}

		/// <summary>退出并卸载所有插件。</summary>
		public int ExitAll()
		{
			Module_Interface[] snapshot = Modules;
			int count = 0;
			foreach (Module_Interface module in snapshot)
				if (ExitModule(module)) count++;
			return count;
		}

		/// <summary>为插件构建独立的配置子对象（从宿主配置中按名称拆分）。</summary>
		private Configs BuildModuleConfigs(Module_Interface module)
		{
			if (HostConfigs == null) return new Configs();

			string[] names = module.GetConfigsNames();
			if (names == null || names.Length == 0) return new Configs();

			return HostConfigs.SeparationConfigs(names);
		}

		/// <summary>把插件的配置变更写回宿主配置对象。</summary>
		private void PersistModuleConfigs(Module_Interface module)
		{
			if (HostConfigs == null || module.Configs == null) return;

			foreach (Configs.Config cfg in module.Configs.configs)
			{
				if (cfg == null || cfg.Name == null) continue;
				if (HostConfigs.IsHave(cfg.Name))
					HostConfigs.WriteValue(cfg.Name, cfg.Value);
			}
		}

		private void Subscribe(Module_Interface module)
		{
			module.WindowsChangeEvent += Module_WindowsChange;
			module.LogsChangeEvent += Module_LogsChange;
			module.ModuleExitEvent += Module_ModuleExit;
			module.ConfigsChangeEvent += Module_ConfigsChange;
			module.ModuleInitDoneEvent += Module_InitDone;
		}

		private void Unsubscribe(Module_Interface module)
		{
			module.WindowsChangeEvent -= Module_WindowsChange;
			module.LogsChangeEvent -= Module_LogsChange;
			module.ModuleExitEvent -= Module_ModuleExit;
			module.ConfigsChangeEvent -= Module_ConfigsChange;
			module.ModuleInitDoneEvent -= Module_InitDone;
		}

		private void Module_WindowsChange(object sender, EventArgs e)
		{
			ModuleWindowsChanged?.Invoke(this, new ModuleEventArgs(sender as Module_Interface));
		}

		private void Module_LogsChange(object sender, EventArgs e)
		{
			ModuleLogsChanged?.Invoke(this, new ModuleEventArgs(sender as Module_Interface));
		}

		private void Module_InitDone(object sender, EventArgs e)
		{
			ModuleInitDone?.Invoke(this, new ModuleEventArgs(sender as Module_Interface));
		}

		private void Module_ConfigsChange(object sender, EventArgs e)
		{
			Module_Interface module = sender as Module_Interface;
			if (module == null) return;
			PersistModuleConfigs(module);
		}

		private void Module_ModuleExit(object sender, EventArgs e)
		{
			Module_Interface module = sender as Module_Interface;
			if (module == null) return;

			bool removed;
			lock (_lock) removed = _modules.Remove(module);
			if (!removed) return;

			Unsubscribe(module);
			ModuleUnloaded?.Invoke(this, new ModuleEventArgs(module));
		}
	}

	/// <summary>
	/// 模块（插件）相关事件参数，携带触发事件的插件实例。
	/// </summary>
	public class ModuleEventArgs : EventArgs
	{
		/// <summary>触发事件的插件实例。</summary>
		public Module_Interface Module { get; private set; }

		public ModuleEventArgs(Module_Interface module)
		{
			Module = module;
		}
	}
}
