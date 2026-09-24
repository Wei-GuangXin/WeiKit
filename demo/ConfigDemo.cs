using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// ① 配置管理演示：声明、读写、重置、导入导出、加密落盘，以及内置配置管理窗口。
	/// </summary>
	internal class ConfigDemo
	{
		private DataGridView grid;
		private TextBox txtName;
		private TextBox txtValue;
		private Label lbState;
		private TextBox txtEventCount;   // 显示 ConfigChanged 触发次数
		private int eventCount;

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"配置项的内部名称用 . 分隔（如 App.TopMost），配置管理窗口会自动按层级建树。" +
				"本页所有写操作都会立即保存到加密配置文件。");

			// ---------- 当前配置总览 ----------
			Ui.AddHeader(content, "当前配置项");
			grid = Ui.Grid(220);
			Ui.SetupGrid(grid, "内部名称", "显示名称", "类型", "当前值", "默认值", "说明");
			content.Controls.Add(grid);

			lbState = new Label
			{
				AutoSize = true,
				ForeColor = Ui.Muted,
				Margin = new Padding(0, 0, 0, 9)
			};
			content.Controls.Add(lbState);

			Ui.Row(content,
				Ui.Btn("刷新", (s, e) => RefreshGrid(), 90),
				Ui.Btn("打开配置管理窗口", (s, e) => OpenWindow(), 170),
				Ui.Btn("SaveToDisk 保存", (s, e) => SaveAll(), 140),
				Ui.Btn("全部重置为默认值", (s, e) => ResetAll(), 165));

			// ---------- 单项读写 ----------
			var gWrite = Ui.Group(content, "读取 / 写入单项配置");
			Ui.Row(gWrite,
				Ui.Text("配置名：", 70),
				txtName = Ui.In("Demo.UserName", 240),
				Ui.Text("值：", 40),
				txtValue = Ui.In("张三", 220),
				Ui.Btn("读取", (s, e) => ReadOne(), 80),
				Ui.Btn("写入", (s, e) => WriteOne(), 80),
				Ui.Btn("该类型转换写入", (s, e) => WriteStr(), 130),
				Ui.Btn("重置该项", (s, e) => ResetOne(), 100));
			Ui.AddHint(gWrite,
				"读取返回 object（整型实际是 long）。「类型转换写入」调用 WriteStrValue，" +
				"已修复：解析失败会保留原值并返回 false，不会污染配置。数值项采用不区分区域性的解析。");

			// ---------- 导入导出 ----------
			var gIo = Ui.Group(content, "导入 / 导出");
			Ui.Row(gIo,
				Ui.Btn("导出为明文 JSON", (s, e) => ExportPlain(), 160),
				Ui.Btn("从明文 JSON 导入", (s, e) => ImportPlain(), 160),
				Ui.Btn("打开配置文件所在目录", (s, e) => Tool.OpenTypeFile(Program.WorkDir), 180));
			Ui.AddHint(gIo,
				"OutSave 永远写明文、不改动当前配置路径；InputData 按名称合并（同名整条覆盖、新名追加），" +
				"且不会触发 ConfigChanged、不会自动落盘。");
			Ui.Row(gIo,
				Ui.Text("配置变更事件触发次数：", 160),
				txtEventCount = Ui.Out("0", 80));
			Ui.AddHint(gIo, "上方的计数由 Configs.ConfigChanged 事件累加，可用来观察哪些操作会触发事件。");

			// 订阅配置变更事件（引用保存在字段里，避免被 GC）
			Program.Cfg.ConfigChanged += OnConfigChanged;

			RefreshGrid();
			return host;
		}

		private void OnConfigChanged(Configs configs)
		{
			eventCount++;
			RefreshGrid();
			txtEventCount.Text = eventCount.ToString();
			Ui.Log($"[配置事件] 第 {eventCount} 次触发 ConfigChanged，当前共 {configs.configs.Count} 项。", "Info");
		}

		private void RefreshGrid()
		{
			Ui.SetupGrid(grid, "内部名称", "显示名称", "类型", "当前值", "默认值", "说明");
			foreach (var c in Program.Cfg.configs)
			{
				Ui.AddRow(grid, c.Name, c.Text, c.Type.ToString(),
					c.Value?.ToString() ?? "(null)", c.DefaultValue?.ToString() ?? "(null)", c.Notes);
			}
			lbState.Text = $"配置文件：{Program.ConfigPath}\r\n" +
						   $"是否成功载入：{Program.Cfg.IsEffective}　是否加密载入：{Program.Cfg.IsEncrypted}　" +
						   $"密钥：\"{Program.ConfigSaveKey}\"　项数：{Program.Cfg.configs.Count}";
		}

		private void OpenWindow()
		{
			// 自行托管窗口关闭事件，避免依赖窗口自带的「保存」按钮
			Program.Cfg.ConfigManagFormClosed += OnWindowClosed;
			Program.Cfg.ShowConfigManagForm(true);
		}

		private void OnWindowClosed(object sender, EventArgs e)
		{
			Program.Cfg.ConfigManagFormClosed -= OnWindowClosed;
			Program.Cfg.Save();
			Ui.Log("配置管理窗口已关闭，配置已用密钥保存。", "Info");
			RefreshGrid();
		}

		private void SaveAll()
		{
			Program.Cfg.Save();
			var info = new FileInfo(Program.ConfigPath);
			Ui.Info($"已加密保存到：\r\n{Program.ConfigPath}\r\n文件大小：{info.Length} 字节\r\n\r\n" +
					"用记事本打开会是乱码 —— 这正是 CryptoHelper 的 AES 密文。");
			Ui.Log($"配置保存完成，文件 {info.Length} 字节。", "Info");
		}

		private void ResetAll()
		{
			if (!Confirm("确定要把所有配置项恢复为默认值吗？")) return;
			Program.Cfg.ResetAll();
			Program.Cfg.Save();
			Ui.Log("全部配置已重置为默认值并保存。", "Warning");
			RefreshGrid();
		}

		private void ReadOne()
		{
			if (!EnsureExists(txtName.Text)) return;
			var cfg = Program.Cfg.GetConfig(txtName.Text);
			object v = Program.Cfg.Read(txtName.Text);
			Ui.Info($"名称：{cfg.Name}\r\n显示名：{cfg.Text}\r\n声明类型：{cfg.Type}\r\n" +
					$"值的 CLR 类型：{v?.GetType().FullName ?? "(null)"}\r\n值：{v}");
			Ui.Log($"读取配置 {cfg.Name} = {v}（CLR 类型 {v?.GetType().Name}）", "Info");
		}

		private void WriteOne()
		{
			if (!EnsureExists(txtName.Text)) return;
			string name = txtName.Text;
			Configs.Config cfg = Program.Cfg.GetConfig(name);

			// WriteValue 不做类型校验，这里演示「按声明类型转换后写入」的稳妥做法
			try
			{
				object val;
				switch (cfg.Type)
				{
					case Configs.ValueType.Bool: val = bool.Parse(txtValue.Text); break;
					case Configs.ValueType.Long: val = long.Parse(txtValue.Text); break;
					case Configs.ValueType.Float: val = float.Parse(txtValue.Text); break;
					default: val = txtValue.Text; break;
				}
				bool ok = Program.Cfg.WriteValue(name, val);
				if (ok) Program.Cfg.Save();
				Ui.Log($"写入配置 {name} = {val} → {(ok ? "成功" : "失败")}", ok ? "Info" : "Warning");
			}
			catch (FormatException)
			{
				Ui.Error($"值 \"{txtValue.Text}\" 无法转换为 {cfg.Type} 类型。");
			}
		}

		private void WriteStr()
		{
			if (!EnsureExists(txtName.Text)) return;
			string name = txtName.Text;
			bool ok = Program.Cfg.WriteStrValue(name, txtValue.Text);
			Program.Cfg.Save();
			Configs.Config cfg = Program.Cfg.GetConfig(name);
			Ui.Log($"WriteStrValue(\"{name}\", \"{txtValue.Text}\") → {ok}，当前存储值 = {cfg.Value}", ok ? "Info" : "Warning");
			RefreshGrid();
		}

		private void ResetOne()
		{
			if (!EnsureExists(txtName.Text)) return;
			bool ok = Program.Cfg.Reset(txtName.Text);
			if (ok) Program.Cfg.Save();
			Ui.Log($"重置配置 {txtName.Text} → {ok}", ok ? "Info" : "Warning");
			RefreshGrid();
		}

		private void ExportPlain()
		{
			using (var dlg = new SaveFileDialog
			{
				Filter = "JSON 文件 (*.json)|*.json",
				FileName = "weikit_config_export.json",
				InitialDirectory = Program.WorkDir
			})
			{
				if (dlg.ShowDialog() != DialogResult.OK) return;
				Program.Cfg.OutSave(dlg.FileName);
				Ui.Info($"已导出明文配置（注意：这里不会加密）：\r\n{dlg.FileName}\r\n\r\n" +
						"内容为 Config 对象数组，可直接用记事本查看与编辑。");
				Ui.Log($"配置已明文导出到 {dlg.FileName}", "Warning");
			}
		}

		private void ImportPlain()
		{
			using (var dlg = new OpenFileDialog
			{
				Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
				InitialDirectory = Program.WorkDir
			})
			{
				if (dlg.ShowDialog() != DialogResult.OK) return;
				int before = Program.Cfg.configs.Count;
				Program.Cfg.InputData(dlg.FileName);
				int after = Program.Cfg.configs.Count;
				Program.Cfg.Save();
				RefreshGrid();
				Ui.Info($"导入完成：{before} 项 → {after} 项（同名覆盖、新名追加）。\r\n" +
						"已重新加密保存到当前配置文件。");
			}
		}

		private bool EnsureExists(string name)
		{
			if (Program.Cfg.IsHave(name)) return true;
			Ui.Error($"不存在名为 \"{name}\" 的配置项。\r\n可用的名称：" + string.Join("、", Program.Cfg.GetConfigNameList()));
			return false;
		}

		private static bool Confirm(string text)
			=> MessageBox.Show(text, "操作确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
	}
}
