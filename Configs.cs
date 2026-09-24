using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using WeiKit.Window;

namespace WeiKit
{
	/// <summary>
	/// 配置列表类
	/// </summary>
	public class Configs
	{
		/// <summary>
		/// 配置文件路径
		/// </summary>
		public string ConfigFilePath { get; private set; }
		/// <summary>
		/// 是有效的？
		/// </summary>
		public bool IsEffective { get; private set; }
		/// <summary>
		/// 是否是加密的
		/// </summary>
		public bool IsEncrypted { get; private set; }
		/// <summary>
		/// 最近一次载入/保存使用的加密密钥（空字符串表示明文）。
		/// 由 <see cref="Load(string,string)"/> 与 <see cref="Save(string,string)"/> 自动记录，
		/// 便于无参 <see cref="Save()"/> 沿用同一密钥，避免「窗口保存导致加密配置退化为明文」。
		/// </summary>
		public string LastSaveKey { get; private set; } = string.Empty;
		/// <summary>
		/// 配置值类型
		/// </summary>
		public enum ValueType
		{
			Bool,
			Long,
			Float,
			String
		}
		/// <summary>
		/// 配置数据更新事件
		/// </summary>
		public event ConfigsChangedEventHandler ConfigChanged;
		public delegate void ConfigsChangedEventHandler(Configs configs);
		/// <summary>
		/// 配置管理窗口引用
		/// </summary>
		private ConfigManag ConfigManagForm;

		public event EventHandler ConfigManagFormClosed;
		/// <summary>
		/// 配置管理窗口关闭事件处理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConfigManagForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
		{
			ConfigManagFormClosed?.Invoke(this, EventArgs.Empty);
			ConfigManagForm.FormClosed -= ConfigManagForm_FormClosed;
		}
		/// <summary>
		/// 配置类型
		/// </summary>
		public class Config
		{
			/// <summary>
			/// 名称
			/// </summary>
			public string Name;
			/// <summary>
			/// 显示文本
			/// </summary>
			public string Text;
			/// <summary>
			/// 备注以及解释信息
			/// </summary>
			public string Notes;
			/// <summary>
			/// 数据类型
			/// </summary>
			public ValueType Type;
			/// <summary>
			/// 数据值
			/// </summary>
			public object Value;
			/// <summary>
			/// 数据默认值
			/// </summary>
			public object DefaultValue;
			/// <summary>
			/// 关于配置的帮助链接
			/// </summary>
			public string HelpUrl = string.Empty;
		}
		/// <summary>
		/// 配置数据列表
		/// </summary>
		public List<Config> configs = new List<Config>();
		/// <summary>
		/// 获取配置项数据，没有找到则返回null。
		/// </summary>
		/// <param name="name">配置项目名称</param>
		/// <returns>配置项数据</returns>
		public Config GetConfig(string name)
		{
			if (configs == null)
				return null;
			foreach (var s in configs)
				if (s.Name == name)
					return s;
			return null;
		}
		/// <summary>
		/// 获取配置集合中所有项目的内部名称
		/// </summary>
		/// <returns></returns>
		public string[] GetConfigNameList()
		{
			if (configs != null && configs.Count > 0)
			{
				string[] strs = new string[configs.Count];
				for (int i = 0; i < configs.Count; i++)
				{
					strs[i] = configs[i].Name;
				}
				return strs;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 返回配置数据
		/// </summary>
		/// <param name="name">配置项目名称</param>
		/// <returns>配置数据</returns>
		public object Read(string name)
		{
			foreach (var s in configs)
				if (s.Name == name)
					return s.Value;
			Console.WriteLine($"未找到配置<{name}>数据");
			return null;
		}
		/// <summary>
		/// 写入配置数据
		/// </summary>
		/// <param name="name"></param>
		/// <param name="data"></param>
		/// <returns>是否成功</returns>
		public bool WriteValue(string name, object value)
		{
			Config config = GetConfig(name);
			if (config != null)
			{
				config.Value = value;
				ConfigChanged?.Invoke(this);// 触发配置更改事件
				return true;
			}
			Console.WriteLine($"未找到配置<{name}>数据，写入操作失败。");
			return false;
		}
		/// <summary>
		/// 写入配置数据（自动转换为Object）
		/// </summary>
		/// <param name="name">配置项目内部名称</param>
		/// <param name="value">String数据</param>
		/// <returns>是否成功转换并存储</returns>
		/// <remarks>
		/// 转换失败时会保持原有值不变（不会污染配置），并返回 false。
		/// </remarks>
		public bool WriteStrValue(string name, string value)
		{
			Config config = GetConfig(name);
			if (config != null)
			{
				object y_value = StringToObject(name, value);
				if (y_value == null)
				{
					Console.WriteLine($"写入配置<{name}>失败，原值保持不变。");
					return false;
				}
				config.Value = y_value;
				ConfigChanged?.Invoke(this);// 触发配置更改事件
				return true;
			}
			Console.WriteLine($"未找到配置<{name}>数据，写入操作失败。");
			return false;
		}
		/// <summary>
		/// 将String转换为配置项目的数据类型相应Object数据
		/// </summary>
		/// <param name="name">配置项目内部名称</param>
		/// <param name="value">String数据</param>
		/// <returns>转换后的数据；转换失败或找不到配置项时返回 null</returns>
		private object StringToObject(string name, string value)
		{
			Config config = GetConfig(name);
			object t_value;
			if (config != null)
			{
				try
				{
					switch (config.Type)
					{
						case ValueType.String:
							t_value = value;
							return t_value;
						case ValueType.Long:
							// 允许 "3" 与 "3.0" 两种写法，避免因小数点被判定为失败
							t_value = long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
							return t_value;
						case ValueType.Bool:
							t_value = bool.Parse(value);
							return t_value;
						case ValueType.Float:
							t_value = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
							return t_value;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"转换失败：配置<{name}>的值“{value}”无法转换为 {config.Type} 类型（{e.Message}）。");
					return null;
				}
			}
			Console.WriteLine($"转换失败：没有找到名为<{name}>的配置项目");
			return null;
		}

		/// <summary>
		/// 新建配置条例
		/// </summary>
		/// <param name="name">条例名称</param>
		/// <param name="type">数据类型</param>
		/// <param name="value">数据值</param>
		/// <param name="text">显示名称</param>
		/// <param name="notes">解释信息</param>
		/// <param name="defaultvalue">默认值</param>
		/// <returns></returns>
		public bool Add(string name, ValueType type = ValueType.String, object value = null, string text = "", string notes = "暂无解释信息。", object defaultvalue = null, string heplurl = "")
		{
			//检查是否存在同名项目
			if (GetConfig(name) != null)
			{
				Console.WriteLine($"新建配置数据失败，存在同名配置<{name}>数据。");
				return false;
			}
			if (text == "")
				text = name;
			if (defaultvalue == null) defaultvalue = value;
			//新建项目
			configs.Add(new Config
			{
				Name = name,
				Type = type,
				Value = value,
				Text = text,
				Notes = notes,
				DefaultValue = defaultvalue,
				HelpUrl = heplurl
			});
			ConfigChanged?.Invoke(this);// 触发配置更改事件
			return true;
		}
		/// <summary>
		/// 删除配置条例
		/// </summary>
		/// <param name="name">条例名称</param>
		/// <returns>是否成功</returns>
		public bool Remove(string name)
		{
			Config config = GetConfig(name);
			if (config != null)
			{
				bool en = configs.Remove(config);
				if (en) ConfigChanged?.Invoke(this);// 触发配置更改事件
				if (en)
					Console.WriteLine($"成功删除配置<{name}>数据");
				else
					Console.WriteLine($"删除配置<{name}>数据失败，未知原因。");
				return en;
			}
			Console.WriteLine($"未找到配置<{name}>数据，删除配置数据失败。");
			return false;
		}
		/// <summary>
		/// 重置配置数据
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool Reset(string name)
		{
			Config config = GetConfig(name);
			if (config != null)
			{
				config.Value = config.DefaultValue;
				ConfigChanged?.Invoke(this);// 触发配置更改事件
				Console.WriteLine($"重置配置<{name}>数据成功。");
				return true;
			}
			Console.WriteLine($"未找到配置<{name}>数据，重置配置数据失败。");
			return false;
		}
		/// <summary>
		/// 重置所有配置数据
		/// </summary>
		public void ResetAll()
		{
			foreach (var s in configs)
				s.Value = s.DefaultValue;
			ConfigChanged?.Invoke(this);// 触发配置更改事件
			Console.WriteLine($"配置数据已设置为默认值。");
		}

		public bool Load(string path, string SaveKey = "")
		{
			Console.WriteLine($"正在载入配置数据->{path}");

			if (!File.Exists(path))
			{
				IsEffective = false;
				Console.WriteLine($"载入配置数据失败：文件不存在。");
				return false;
			}

			ConfigFilePath = path;
			List<Config> loadedConfigs = null;
			bool shouldBeEncrypted = false;

			try
			{
				if (SaveKey != "")
				{
					shouldBeEncrypted = true;

					// 先尝试无加密载入（兼容旧明文配置）
					try
					{
						loadedConfigs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(path));
						if (loadedConfigs != null && loadedConfigs.Count > 0)
						{
							// 明文文件，转换为加密格式
							configs = loadedConfigs;
							IsEncrypted = true;
							IsEffective = true;
							Console.WriteLine($"检测到明文配置文件，将自动加密保存。");
							Save(path, SaveKey);        // 注意：必须两个实参，单参数会被当成路径
							LastSaveKey = SaveKey;
							ConfigChanged?.Invoke(this);
							return true;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"尝试无加密载入失败：{ex.Message}，准备进行解密载入...");
					}

					// 解密载入
					try
					{
						string jsondata = CryptoHelper.LoadDecrypted<string>(path, SaveKey);
						loadedConfigs = JsonConvert.DeserializeObject<List<Config>>(jsondata);

						if (loadedConfigs == null || loadedConfigs.Count == 0)
						{
							Console.WriteLine($"解密后的配置数据为空或无效。");
							IsEffective = false;
							return false;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"解密载入失败：{ex.Message}");
						IsEffective = false;
						return false;
					}
				}
				else
				{
					// 无密钥，直接读取明文
					shouldBeEncrypted = false;
					loadedConfigs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(path));

					if (loadedConfigs == null || loadedConfigs.Count == 0)
					{
						Console.WriteLine($"明文配置数据为空或无效。");
						IsEffective = false;
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"载入配置数据时发生异常：{ex.Message}");
				IsEffective = false;
				return false;
			}

			// 成功加载
			configs = loadedConfigs;
			IsEffective = true;
			IsEncrypted = shouldBeEncrypted;
			LastSaveKey = SaveKey ?? string.Empty;       // 记录密钥（SaveKey 是参数），供后续无参 Save 沿用
			ConfigChanged?.Invoke(this);
			Console.WriteLine($"载入配置数据成功。");
			return true;
		}

		/// <summary>
		/// 保存配置数据到当前配置文件（<see cref="ConfigFilePath"/>，即最近一次 Load 或 Save 的路径），明文保存。
		/// </summary>
		/// <returns>是否保存成功。从未载入/保存过路径时返回 false。</returns>
		/// <remarks>
		/// 旧版本签名是 <c>Save(string SaveKey = "")</c>，与 <c>Save(string path, string SaveKey = "")</c>
		/// 组成重载时，单参数调用 <c>Save(x)</c> 会被解析到「密钥」那个重载，
		/// 导致 <c>Save(路径)</c> 静默不写任何文件。现已改为无参重载，分别用
		/// <see cref="Save(string)"/>（按路径明文）与 <see cref="Save(string,string)"/>（按路径+密钥）。
		/// </remarks>
		public bool Save()
		{
			// 沿用最近一次的密钥：Load 时给了密钥就继续加密保存，不会悄悄退化成明文
			return SaveCore(ConfigFilePath, LastSaveKey);
		}

		/// <summary>
		/// 保存配置数据到指定路径（明文）。
		/// </summary>
		/// <param name="path">要写入的文件路径。</param>
		/// <returns>是否保存成功。</returns>
		public bool Save(string path)
		{
			return SaveCore(path, "");
		}

		/// <summary>
		/// 保存配置数据到指定路径，可选加密。
		/// </summary>
		/// <param name="path">要写入的文件路径。</param>
		/// <param name="saveKey">加密密钥；留空则明文保存。</param>
		/// <returns>是否保存成功。</returns>
		public bool Save(string path, string saveKey)
		{
			return SaveCore(path, saveKey);
		}

		/// <summary>
		/// 保存的实际实现。写盘失败不再静默：会把原因打印到控制台并返回 false。
		/// </summary>
		private bool SaveCore(string path, string saveKey)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				Console.WriteLine("配置数据保存失败：未指定文件路径（也从未成功载入过配置）。");
				return false;
			}

			try
			{
				// 目录不存在时自动创建，避免「路径正确却写不进去」
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				string jsondata = JsonConvert.SerializeObject(configs);
				if (!string.IsNullOrEmpty(saveKey))
				{
					CryptoHelper.SaveEncrypted(path, jsondata, saveKey);
					Console.WriteLine($"配置数据加密保存完成->{path}");
				}
				else
				{
					File.WriteAllText(path, jsondata);
					Console.WriteLine($"配置数据保存完成->{path}");
				}

				ConfigFilePath = path;
				LastSaveKey = saveKey ?? string.Empty;   // 记录密钥，供后续无参 Save 沿用
				IsEncrypted = !string.IsNullOrEmpty(saveKey);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine($"配置数据保存失败->{path}：{e.Message}");
				return false;
			}
		}

		/// <summary>
		/// 导出配置数据到文件（永远明文，不修改当前配置路径）。
		/// </summary>
		/// <param name="path">要存储到的文件路径</param>
		/// <returns>是否导出成功</returns>
		public bool OutSave(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				Console.WriteLine("配置数据导出失败：未指定文件路径。");
				return false;
			}
			try
			{
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				File.WriteAllText(path, JsonConvert.SerializeObject(configs));
				Console.WriteLine($"配置数据导出完成->{path}");
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine($"配置数据导出失败->{path}：{e.Message}");
				return false;
			}
		}

		/// <summary>
		/// 从另一个配置文件导入配置数据。
		/// 按 <see cref="Config.Name"/> 合并：同名项整条覆盖，新名称项追加。
		/// </summary>
		/// <param name="path">要导入的配置文件路径（明文或加密均可，明文可直接读）</param>
		/// <returns>是否成功导入（导入成功后有新增或覆盖的配置项）</returns>
		/// <remarks>
		/// 导入不会自动保存，也不会触发 <see cref="ConfigChanged"/>，需要落盘请自行调用 Save。
		/// </remarks>
		public bool InputData(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				Console.WriteLine("导入配置文件失败：参数为空。");
				return false;
			}
			if (!File.Exists(path))
			{
				Console.WriteLine("导入配置文件失败：文件不存在。");
				return false;
			}

			Configs configs_temp = new Configs();
			if (!configs_temp.Load(path))
			{
				Console.WriteLine("导入配置文件失败：载入失败。");
				return false;
			}

			Console.WriteLine($"成功打开配置文件，共 {configs_temp.configs.Count} 条配置。");
			foreach (var incoming in configs_temp.configs)
			{
				int index = GetConfigIndex(incoming.Name);
				if (index >= 0)
					configs[index] = incoming;    // 同名覆盖
				else
					configs.Add(incoming);         // 新名称追加
			}
			Console.WriteLine($"导入配置文件 {path} 完成，当前共 {configs.Count} 条配置。");

			ConfigChanged?.Invoke(this);
			return true;
		}

		/// <summary>
		/// 显示配置管理窗口
		/// </summary>
		/// <returns>窗口句柄</returns>
		public IntPtr ShowConfigManagForm(bool TopDisplay = false)
		{
			if (ConfigManagForm == null || ConfigManagForm.IsDisposed)
			{
				ConfigManagForm = new ConfigManag(this);
				ConfigManagForm.Show();
				ConfigManagForm.FormClosed += ConfigManagForm_FormClosed;
				ConfigManagForm.TopMost = TopDisplay;
			}
			else
			{
				if (ConfigManagForm.WindowState == System.Windows.Forms.FormWindowState.Minimized)
					ConfigManagForm.WindowState = System.Windows.Forms.FormWindowState.Normal;
				ConfigManagForm.TopMost = TopDisplay;
				ConfigManagForm.BringToFront();
			}
			return ConfigManagForm.Handle;
		}

		/// <summary>
		/// 是否存在指定名称的配置项目
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool IsHave(string name)
		{
			foreach (var s in configs)
				if (s.Name == name)
					return true;
			return false;
		}

		/// <summary>
		/// 获取配置项目的索引位置，未找到则返回-1。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public int GetConfigIndex(string name)
		{
			for (int i = 0; i < configs.Count; i++)
			{
				if (configs[i].Name == name)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// 复制本实例中指定的配置项到另一个配置对象（同名覆盖，否则追加）。
		/// </summary>
		/// <param name="name">要复制的配置项内部名称</param>
		/// <param name="Toconfigs">目标配置对象。<b>不能为 null</b>，否则抛 <see cref="ArgumentNullException"/>。</param>
		/// <returns>是否复制成功（源中没有该项目时返回 false）</returns>
		public bool Copy(string name, Configs Toconfigs)
		{
			if (Toconfigs == null)
				throw new ArgumentNullException(nameof(Toconfigs), "目标配置对象不能为空。");

			Config source = GetConfig(name);
			if (source == null)
			{
				Console.WriteLine($"复制配置失败：未找到名为<{name}>的配置项目。");
				return false;
			}

			int i = Toconfigs.GetConfigIndex(name);
			if (i != -1)
			{
				// 存在同名配置，整条覆盖（避免残留旧字段）
				Toconfigs.configs[i] = new Config
				{
					Name = source.Name,
					Notes = source.Notes,
					Text = source.Text,
					Type = source.Type,
					Value = source.Value,
					DefaultValue = source.DefaultValue,
					HelpUrl = source.HelpUrl
				};
			}
			else
			{
				// 没有同名配置，直接复制过去
				Toconfigs.Add(name, source.Type, source.Value, source.Text, source.Notes, source.DefaultValue, source.HelpUrl);
			}
			return true;
		}

		/// <summary>
		/// 复制本实例的全部配置项到另一个配置对象。
		/// </summary>
		/// <param name="Toconfigs">目标配置对象，不能为 null。</param>
		/// <returns>成功复制的配置项数量</returns>
		public int CopyAll(Configs Toconfigs)
		{
			if (Toconfigs == null)
				throw new ArgumentNullException(nameof(Toconfigs), "目标配置对象不能为空。");

			int count = 0;
			foreach (var s in configs)
				if (Copy(s.Name, Toconfigs)) count++;
			return count;
		}

	}
}
