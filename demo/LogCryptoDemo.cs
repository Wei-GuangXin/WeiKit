using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using WeiKit;
using WeiKit.Window;      // Logsinfo 日志查看窗口
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// ② 日志 + 加密解密演示。
	/// </summary>
	internal class LogCryptoDemo
	{
		private NumericUpDown numMaxLog;
		private TextBox txtAutoSave;
		private ComboBox cboTheme;
		private TextBox txtPlain;
		private TextBox txtCipherInfo;
		private Label lbCryptoState;
		private Logsinfo logWindow;      // 由 Demo 自己 new 出来的日志窗口引用

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"库的日志是「内存列表 + 事件 + 可选窗口」三段式；页面左边所有操作都会真实写入日志，" +
				"点「打开日志窗口」即可实时观察。加密部分直接操作磁盘文件，可看到 AES 密文与篡改报错。");

			// ==================== 日志 ====================
			Ui.AddHeader(content, "日志 Logs");

			var gLog = Ui.Group(content, "打印日志");
			Ui.Row(gLog,
				Ui.Btn("Info", (s, e) => Print("Info"), 70),
				Ui.Btn("Warning", (s, e) => Print("Warning"), 85),
				Ui.Btn("Error", (s, e) => Print("Error"), 70),
				Ui.Btn("自定义类型", (s, e) => Print("MyModule"), 110),
				Ui.Btn("模拟库内部 Console 输出", (s, e) =>
				{
					// 走 RedirectConsoleOut 接管后的 Console，会以 "Console" 类型进日志
					Console.WriteLine($"[模拟库输出] 当前时间 {DateTime.Now:HH:mm:ss.fff}");
				}, 200));
			Ui.AddHint(gLog,
				"前四个按钮调用 Logs.Println(text, typeText)。最后一个走 Console.WriteLine —— " +
				"因为 Program 启动时调用了 RedirectConsoleOut()，它会以 \"Console\" 类型出现在日志里（这正是库内其他模块的输出路径）。");

			var gLimit = Ui.Group(content, "数量上限与自动落盘");
			Ui.Row(gLimit,
				Ui.Text("MaxLogPcs：", 80),
				numMaxLog = new NumericUpDown
				{
					Width = 90,
					Minimum = 0,
					Maximum = 100000,
					Value = Program.Log.MaxLogPcs,
					Margin = new Padding(0, 4, 12, 3)
				},
				Ui.Btn("应用上限", (s, e) =>
				{
					Program.Log.MaxLogPcs = (int)numMaxLog.Value;
					Ui.Log($"MaxLogPcs = {Program.Log.MaxLogPcs}（0 表示不限制）", "Info");
				}, 100),
				Ui.Text("达到上限后自动写到：", 150),
				txtAutoSave = Ui.In(Program.Log.AutoSavePath, 300),
				Ui.Btn("应用路径", (s, e) =>
				{
					Program.Log.AutoSavePath = txtAutoSave.Text;
					Ui.Log($"AutoSavePath = {Program.Log.AutoSavePath}", "Info");
				}, 100));
			Ui.AddHint(gLimit,
				"把上限设为 10，然后连点几次「Info」，日志到达 10 条时会自动整份落盘并清空，最后留下一条 AutoSave 记录。" +
				"默认值已改为程序目录下的 logs\\weikit_autosave.log；写入前会自动创建目录，落盘失败也会保留日志不清空。");

			var gView = Ui.Group(content, "查看窗口与导出");
			cboTheme = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 10, 3) };
			foreach (var t in Enum.GetValues(typeof(ConsoleLogTheme.ThemeType))) cboTheme.Items.Add(t);
			cboTheme.SelectedIndex = 2;   // LightClean

			Ui.Row(gView,
				Ui.Btn("打开日志窗口（库托管）", (s, e) => OpenLibraryWindow(), 180),
				Ui.Btn("自行创建并指定主题", (s, e) => OpenOwnWindow(), 175),
				Ui.Text("主题：", 45),
				cboTheme,
				Ui.Btn("切换 ThemeType", (s, e) => ApplyTheme(), 130));
			Ui.Row(gView,
				Ui.Btn("导出日志文件", (s, e) => ExportLog(), 140),
				Ui.Btn("清空日志列表", (s, e) =>
				{
					Program.Log.logs.Clear();
					Ui.Log("日志列表已清空（logs 是 get; private set; 属性，内容可变）。", "Warning");
				}, 150),
				Ui.Btn("取最后一条日志", (s, e) => LastLog(), 150));
			Ui.AddHint(gView,
				"「库托管」用 Logs.ShowLogsForm()，它会替你按正确顺序赋值 logs、AutoColorMode 再显示；" +
				"「自行创建」演示 new Logsinfo() 时必须先给 logs 赋值再 Show()。ThemeType 必须在 Show 之前设置才生效。");

			// ==================== 加密解密 ====================
			Ui.AddHeader(content, "加密解密 CryptoHelper");

			var gEnc = Ui.Group(content, "AES 加密对象落盘");
			txtPlain = Ui.Multi(
				"这是一段将被加密保存的明文。" + Environment.NewLine +
				"用户名：Tiangong" + Environment.NewLine +
				"机器码：" + ProgTool.GenerateMachineCode().Substring(0, 16) + "…",
				620, 110, false);
			gEnc.Controls.Add(txtPlain);

			Ui.Row(gEnc,
				Ui.Btn("加密保存", (s, e) => Encrypt(), 110),
				Ui.Btn("解密读回", (s, e) => Decrypt(), 110),
				Ui.Btn("篡改 1 字节后解密", (s, e) => Tamper(), 150),
				Ui.Btn("用错误密码解密", (s, e) => WrongPassword(), 150),
				Ui.Btn("显示文件十六进制", (s, e) => ShowHex(), 150));

			txtCipherInfo = Ui.Multi("", 620, 110, true);
			gEnc.Controls.Add(txtCipherInfo);

			lbCryptoState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 2, 0, 8) };
			gEnc.Controls.Add(lbCryptoState);

			Ui.AddHint(gEnc,
				"CryptoHelper.SaveEncrypted<T> 的落盘格式固定为：[32 字节随机盐] + [AES-CBC 密文]，" +
				"密钥由 Rfc2898DeriveBytes(password, salt, 10000, SHA256) 派生。" +
				"篡改任意一个字节都会在解密时因填充校验失败抛 CryptographicException —— Demo 会捕获并展示异常类型。");

			UpdateCryptoState();
			return host;
		}

		#region 日志演示

		private void Print(string type)
		{
			string text = string.Format("[{0}] 演示日志，序号 {1}，时间 {2:HH:mm:ss.fff}",
				type, Program.Log.logs.Count + 1, DateTime.Now);
			Program.Log.Println(text, type);
		}

		private void OpenLibraryWindow()
		{
			IntPtr h = Program.Log.ShowLogsForm(autoColorMode: true);
			Ui.Log($"Logs.ShowLogsForm() 返回窗口句柄 0x{h.ToInt64():X}", "Info");
		}

		private void OpenOwnWindow()
		{
			// 演示正确顺序：先赋 logs / AutoColorMode / ThemeType，最后 Show
			if (logWindow != null && !logWindow.IsDisposed)
			{
				logWindow.BringToFront();
				Ui.Log("自定义日志窗口已存在，已前置。", "Info");
				return;
			}

			logWindow = new Logsinfo
			{
				logs = Program.Log,
				AutoColorMode = true,
				ThemeType = (ConsoleLogTheme.ThemeType)cboTheme.SelectedItem
			};
			logWindow.FormClosed += (s, e) => Ui.Log("自定义日志窗口已关闭。", "Info");
			logWindow.Show();
			Ui.Log($"已自行创建日志窗口，主题 {logWindow.ThemeType}。", "Info");
		}

		private void ApplyTheme()
		{
			if (logWindow == null || logWindow.IsDisposed)
			{
				Ui.Info("请先点「自行创建并指定主题」打开窗口，ThemeType 只对自建窗口生效。\r\n" +
						"（库托管的窗口请在窗口右上角菜单里切换主题。）");
				return;
			}
			logWindow.ThemeType = (ConsoleLogTheme.ThemeType)cboTheme.SelectedItem;
			logWindow.RefreshContent();                 // 详见窗口新增的公开刷新方法
			Ui.Log($"日志窗口主题已切换为 {logWindow.ThemeType}。", "Info");
		}

		private void ExportLog()
		{
			using (var dlg = new SaveFileDialog
			{
				Filter = "日志文件 (*.log)|*.log",
				FileName = $"weikit_log_{DateTime.Now:yyyyMMdd_HHmmss}.log",
				InitialDirectory = Program.WorkDir
			})
			{
				if (dlg.ShowDialog() != DialogResult.OK) return;
				bool ok = Program.Log.LogsSave(dlg.FileName);
				Ui.Log($"LogsSave → {ok}，共 {Program.Log.logs.Count} 条，文件 {dlg.FileName}", ok ? "Info" : "Error");
				if (ok) Ui.Info($"已导出 {Program.Log.logs.Count} 条日志到：\r\n{dlg.FileName}");
			}
		}

		private void LastLog()
		{
			if (Program.Log.logs.Count == 0)
			{
				// 已修复：GetLastLog 空列表返回 null 而不是抛异常
				Ui.Info("日志列表为空 —— GetLastLog() 现在返回 null、GetLastLogText() 返回空串，\r\n" +
						"不会再抛 ArgumentOutOfRangeException。");
				return;
			}
			Logs.Log last = Program.Log.GetLastLog();
			Ui.Info($"GetLastLog()：\r\n时间：{last.Time:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
					$"类型：{last.TypeText}\r\n正文：{last.Text}\r\n\r\n" +
					$"ToString() = {last}\r\nGetLastLogText() = {Program.Log.GetLastLogText()}");
		}

		#endregion

		#region 加密演示

		/// <summary>会被序列化后加密保存的数据结构</summary>
		private class Secret
		{
			public string Text { get; set; }
			public string UserName { get; set; }
			public DateTime SavedAt { get; set; }
			public string MachineCode { get; set; }
		}

		private void Encrypt()
		{
			var secret = new Secret
			{
				Text = txtPlain.Text,
				UserName = Convert.ToString(Program.Cfg.Read("Demo.UserName")),
				SavedAt = DateTime.Now,
				MachineCode = ProgTool.GenerateMachineCode()
			};

			try
			{
				CryptoHelper.SaveEncrypted(Program.SecretPath, secret, Program.ConfigSaveKey);
				Ui.Log($"已加密保存到 {Program.SecretPath}", "Info");
				UpdateCryptoState();
			}
			catch (Exception ex)
			{
				Ui.Error("加密保存失败：" + ex.Message);
				Ui.Log("加密保存失败：" + ex.Message, "Error");
			}
		}

		private void Decrypt()
		{
			if (!File.Exists(Program.SecretPath))
			{
				Ui.Info("还没有加密文件，请先点「加密保存」。");
				return;
			}
			try
			{
				Secret s = CryptoHelper.LoadDecrypted<Secret>(Program.SecretPath, Program.ConfigSaveKey);
				txtPlain.Text = s.Text;
				txtCipherInfo.Text =
					"解密成功 ✔" + Environment.NewLine +
					"UserName = " + s.UserName + Environment.NewLine +
					"SavedAt = " + s.SavedAt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
					"MachineCode = " + s.MachineCode;
				Ui.Log("LoadDecrypted<Secret> 解密成功。", "Info");
			}
			catch (CryptographicException ex)
			{
				txtCipherInfo.Text = "解密失败：CryptographicException" + Environment.NewLine + ex.Message;
				Ui.Log("解密失败（密码错误或数据被篡改）：" + ex.Message, "Error");
			}
			catch (Exception ex)
			{
				txtCipherInfo.Text = "解密失败：" + ex.GetType().Name + Environment.NewLine + ex.Message;
				Ui.Log("解密失败：" + ex.Message, "Error");
			}
			UpdateCryptoState();
		}

		private void Tamper()
		{
			if (!File.Exists(Program.SecretPath))
			{
				Ui.Info("还没有加密文件，请先点「加密保存」。");
				return;
			}
			byte[] data = File.ReadAllBytes(Program.SecretPath);
			if (data.Length < 40)
			{
				Ui.Error("文件过小，无法演示篡改。");
				return;
			}
			int at = data.Length / 2;
			byte old = data[at];
			data[at] ^= 0xFF;                       // 翻转中间某个字节
			File.WriteAllBytes(Program.SecretPath, data);

			Ui.Log($"已篡改文件第 {at} 字节：0x{old:X2} → 0x{data[at]:X2}，现在尝试解密…", "Warning");
			Decrypt();                               // 预期抛 CryptographicException
			Ui.Info($"已把第 {at} 字节从 0x{old:X2} 改成 0x{data[at]:X2}（AES-CBC 无完整性校验，\r\n" +
					"因此靠 PKCS7 填充校验才抛出 CryptographicException）。\r\n\r\n" +
					"想恢复请重新点「加密保存」。");
		}

		private void WrongPassword()
		{
			if (!File.Exists(Program.SecretPath))
			{
				Ui.Info("还没有加密文件，请先点「加密保存」。");
				return;
			}
			try
			{
				CryptoHelper.LoadDecrypted<Secret>(Program.SecretPath, "错误的密码");
				Ui.Log("用错误密码居然解密成功了？这不应该发生。", "Error");
			}
			catch (CryptographicException ex)
			{
				txtCipherInfo.Text = "预期内的失败：CryptographicException" + Environment.NewLine + ex.Message;
				Ui.Log("用错误密码解密 → CryptographicException（符合预期）。", "Info");
			}
			catch (Exception ex)
			{
				txtCipherInfo.Text = "失败：" + ex.GetType().Name + Environment.NewLine + ex.Message;
				Ui.Log("用错误密码解密 → " + ex.GetType().Name, "Info");
			}
		}

		private void ShowHex()
		{
			if (!File.Exists(Program.SecretPath))
			{
				Ui.Info("还没有加密文件，请先点「加密保存」。");
				return;
			}
			byte[] data = File.ReadAllBytes(Program.SecretPath);
			var sb = new StringBuilder();
			sb.AppendLine($"文件：{Program.SecretPath}");
			sb.AppendLine($"总长度：{data.Length} 字节 = [32 字节盐] + [{data.Length - 32} 字节密文]");
			sb.AppendLine();
			sb.AppendLine("前 32 字节（随机盐，每次保存都不同）：");
			sb.AppendLine(Hex(data, 0, Math.Min(32, data.Length)));
			sb.AppendLine();
			sb.AppendLine("其后的密文前 64 字节（无规律可循）：");
			sb.AppendLine(Hex(data, 32, Math.Min(96, data.Length)));
			txtCipherInfo.Text = sb.ToString();
			Ui.Log($"已读取 {data.Length} 字节密文并显示十六进制。", "Info");
		}

		private static string Hex(byte[] data, int offset, int count)
		{
			var sb = new StringBuilder();
			for (int i = offset; i < offset + count; i++)
			{
				sb.Append(data[i].ToString("X2"));
				sb.Append((i - offset + 1) % 16 == 0 ? Environment.NewLine : " ");
			}
			return sb.ToString();
		}

		private void UpdateCryptoState()
		{
			if (!File.Exists(Program.SecretPath))
			{
				lbCryptoState.Text = $"加密文件：{Program.SecretPath}（尚未创建）";
				return;
			}
			var fi = new FileInfo(Program.SecretPath);
			lbCryptoState.Text = $"加密文件：{Program.SecretPath}\r\n" +
								 $"大小 {fi.Length} 字节　最后写入 {fi.LastWriteTime:HH:mm:ss}　" +
								 $"SHA256 {Tool.CalculateFileChecksum(Program.SecretPath).Substring(0, 16)}…";
		}

		#endregion
	}
}
