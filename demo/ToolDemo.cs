using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// ③ 程序工具演示：ProgTool + Tool 的常用能力。
	/// </summary>
	internal class ToolDemo
	{
		private DataGridView gridInfo;
		private TextBox txtRandom;
		private NumericUpDown numRandomLen;
		private TextBox txtRandomChars;
		private TextBox txtHashOut;
		private TextBox txtCmdOut;
		private TextBox txtPathOut;
		private TextBox txtFlowOut;
		private TextBox txtStartupName;

		/// <summary>本页的宿主面板（本类不是 Control，需要借它判断 IsDisposed）</summary>
		private Control hostPanel;

		public Control Build()
		{
			FlowLayoutPanel content;
			hostPanel = Ui.ScrollHost(out content);
			var host = hostPanel;

			Ui.AddHint(content,
				"这一页演示 ProgTool（程序级：机器码、重启、提权、卸载）与 Tool（通用：随机串、哈希、命令行、路径转换、控件辅助）。" +
				"所有操作都会写入日志。");

			// ==================== 程序信息 ====================
			Ui.AddHeader(content, "程序信息（ProgTool）");
			gridInfo = Ui.Grid(170);
			Ui.SetupGrid(gridInfo, "项目", "值");
			content.Controls.Add(gridInfo);

			Ui.Row(content,
				Ui.Btn("刷新信息", (s, e) => RefreshInfo(), 100),
				Ui.Btn("复制机器码", (s, e) => CopyMachineCode(), 120),
				Ui.Btn("以管理员身份重启", (s, e) =>
				{
					if (ProgTool.IsRunAsAdmin())
					{
						Ui.Info("当前已是管理员权限，无需提权。");
						return;
					}
					if (MessageBox.Show("将以管理员权限重新启动本程序，当前进程会立即退出。继续吗？",
						"确认提权", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						ProgTool.RunAsAdmin();
				}, 160),
				Ui.Btn("重启程序", (s, e) =>
				{
					if (MessageBox.Show("将立即重启本程序（当前进程退出），继续吗？",
						"确认重启", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						ProgTool.ProgramRestart();
				}, 100),
				Ui.Btn("延迟 3 秒重启", (s, e) =>
				{
					if (MessageBox.Show("将在 3 秒后重启（期间程序会退出），继续吗？",
						"确认重启", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						ProgTool.ProgramRestartWithDelay(3000);
				}, 130));

			Ui.AddHint(content,
				"机器码由 CPU ID、主板序列号、硬盘序列号、网卡 MAC 组合后取 SHA256（64 位十六进制）。" +
				"硬件变更或虚拟机克隆都会导致机器码改变 —— 做授权绑定时必须留容错。");

			// ==================== 随机串与哈希 ====================
			Ui.AddHeader(content, "随机串与哈希（Tool）");

			var gRand = Ui.Group(content, "密码学安全随机串 GetRandomString");
			Ui.Row(gRand,
				Ui.Text("长度：", 45),
				numRandomLen = new NumericUpDown { Width = 70, Minimum = 1, Maximum = 256, Value = 24, Margin = new Padding(0, 4, 12, 3) },
				Ui.Text("附加字符集：", 80),
				txtRandomChars = Ui.In("!@#$%", 120),
				Ui.Btn("生成 3 个", (s, e) => GenRandom(), 95),
				txtRandom = Ui.Out("", 420));
			Ui.AddHint(gRand,
				"内部使用 RandomNumberGenerator + 拒绝采样，不是 System.Random，适合生成令牌/盐/临时口令。");

			var gHash = Ui.Group(content, "SHA256 校验");
			txtHashOut = Ui.Multi("", 880, 76, true);
			gHash.Controls.Add(txtHashOut);
			Ui.Row(gHash,
				Ui.Btn("计算本程序文件校验值", (s, e) => HashSelf(), 180),
				Ui.Btn("计算指定文件…", (s, e) => HashFile(), 130),
				Ui.Btn("计算字符串哈希", (s, e) => HashString(), 140),
				Ui.Btn("对比两个哈希是否相等", (s, e) => CompareHash(), 170));
			Ui.AddHint(gHash,
				"CalculateFileChecksum(路径) 与 ComputeSha256Hash(字符串) 都返回 64 位小写十六进制。");

			// ==================== 命令行与路径 ====================
			Ui.AddHeader(content, "命令行与路径");

			var gCmd = Ui.Group(content, "运行 CMD 命令（RunCmdCode）");
			Ui.Row(gCmd,
				Ui.Btn("ipconfig", (s, e) => RunCmd("ipconfig"), 90),
				Ui.Btn("ver", (s, e) => RunCmd("ver"), 70),
				Ui.Btn("systeminfo（较慢）", (s, e) => RunCmd("systeminfo"), 140),
				Ui.Btn("多行命令数组", (s, e) => RunCmdArray(), 130));
			txtCmdOut = Ui.Multi("", 880, 130, true);
			gCmd.Controls.Add(txtCmdOut);
			Ui.AddHint(gCmd,
				"RunCmdCode 是同步阻塞调用（内部 ReadToEnd + WaitForExit），Demo 放在后台线程执行以免界面卡死；" +
				"它不重定向 stderr，所以错误信息不会出现在返回值里。");

			var gPath = Ui.Group(content, "路径转换与文件夹创建（Tool）");
			Ui.Row(gPath,
				Ui.Btn("GetProgramPath()", (s, e) => PathDemo("program"), 150),
				Ui.Btn("PathConversion(\".\\\\A\\\\B\")", (s, e) => PathDemo("toabs"), 190),
				Ui.Btn("PathConversion(反算)", (s, e) => PathDemo("torel"), 150),
				Ui.Btn("AbsNewFolder 新建随机目录", (s, e) => PathDemo("newdir"), 190));
			txtPathOut = Ui.Multi("", 880, 96, true);
			gPath.Controls.Add(txtPathOut);

			// ==================== 注册表自启动 ====================
			Ui.AddHeader(content, "注册表开机自启动（Tool）");

			var gStart = Ui.Group(content, "HKCU\\...\\CurrentVersion\\Run");
			Ui.Row(gStart,
				Ui.Text("项名：", 45),
				txtStartupName = Ui.In("WeiKitDemo", 180),
				Ui.Btn("查询是否已启用", (s, e) =>
				{
					bool on = Tool.IsAutoStartEnabled(txtStartupName.Text);
					Ui.Log($"IsAutoStartEnabled(\"{txtStartupName.Text}\") = {on}", "Info");
					Ui.Info($"注册表自启动项 \"{txtStartupName.Text}\" 当前状态：{(on ? "已启用" : "未启用")}");
				}, 150),
				Ui.Btn("启用自启动", (s, e) =>
				{
					Tool.SetAutoStart(txtStartupName.Text, Application.ExecutablePath, true);
					Ui.Log($"已启用自启动：{txtStartupName.Text} → {Application.ExecutablePath}", "Warning");
					Ui.Info("已写入注册表自启动项。可用「查询是否已启用」确认。");
				}, 120),
				Ui.Btn("关闭自启动", (s, e) =>
				{
					Tool.SetAutoStart(txtStartupName.Text, Application.ExecutablePath, false);
					Ui.Log($"已移除自启动项：{txtStartupName.Text}", "Warning");
					Ui.Info("已删除注册表自启动项。");
				}, 120));
			Ui.AddHint(gStart,
				"写入的是当前用户（HKCU）分支，因此不需要管理员权限。演示结束后建议点「关闭自启动」恢复原状。");

			// ==================== DataFlowList ====================
			Ui.AddHeader(content, "DataFlowList 滑动窗口统计");

			var gFlow = Ui.Group(content, "定长数据流");
			Ui.Row(gFlow,
				Ui.Btn("加入 30 个随机数并统计", (s, e) => FlowDemo(), 190),
				Ui.Btn("加入一个越界元素(double)", (s, e) =>
				{
					// 已修复历史缺陷：旧版内部用 Cast<int>()/Cast<float>()，double 会抛异常；
					// 现在改为 Convert 转换，double/decimal/long 都能混用
					var f = new Tool.DataFlowList();
					f.SetFlowMaxLeng(5);
					f.FlowAdd(1.5d);
					try
					{
						float v = f.GetAverageValueF();
						Ui.Info("居然成功了，平均值 = " + v);
					}
					catch (Exception ex)
					{
						Ui.Info($"异常：{ex.GetType().Name}\r\n{ex.Message}");
					}
				}, 200));
			txtFlowOut = Ui.Multi("", 880, 110, true);
			gFlow.Controls.Add(txtFlowOut);

			// ==================== 杂项 ====================
			Ui.AddHeader(content, "其它");
			Ui.Row(content,
				Ui.Btn("打开程序目录", (s, e) => Tool.OpenTypeFile(Tool.GetProgramPath()), 130),
				Ui.Btn("打开工作目录", (s, e) => Tool.OpenTypeFile(Program.WorkDir), 130),
				Ui.Btn("区间映射 MapValue 演示", (s, e) => MapDemo(), 170),
				Ui.Btn("生成临时文件并自删除脚本", (s, e) => SelfDeleteDemo(), 200));
			Ui.AddHint(content,
				"「自删除脚本」会生成一个 %TEMP% 下的 .bat 并在主程序退出后删除指定文件，会弹出命令行窗口（这是设计如此）。");
			Ui.Row(content,
				Ui.Btn("残留自启动项一键清理", (s, e) =>
				{
					Tool.SetAutoStart("WeiKitDemo", Application.ExecutablePath, false);
					Ui.Log("已清理演示用的自启动项 WeiKitDemo。", "Info");
					Ui.Info("已清理 WeiKitDemo 自启动项。");
				}, 200));

			RefreshInfo();
			return host;
		}

		#region 程序信息

		private void RefreshInfo()
		{
			Ui.SetupGrid(gridInfo, "项目", "值");
			Ui.AddRow(gridInfo, "机器码 GenerateMachineCode()", ProgTool.GenerateMachineCode());
			Ui.AddRow(gridInfo, "编译时间 GetBuildTime()", ProgTool.GetBuildTime().ToString("yyyy-MM-dd HH:mm:ss"));
			Ui.AddRow(gridInfo, "可执行文件名 GetAppNameFromFile()", ProgTool.GetAppNameFromFile());
			Ui.AddRow(gridInfo, "是否管理员 IsRunAsAdmin()", ProgTool.IsRunAsAdmin().ToString());
			Ui.AddRow(gridInfo, "程序目录 GetProgramPath()", Tool.GetProgramPath());
			Ui.AddRow(gridInfo, "程序集版本", typeof(Configs).Assembly.GetName().Version.ToString());
			Ui.AddRow(gridInfo, "本机 IPv4 GetLocalIPv4List()", string.Join(" / ", Tool.GetLocalIPv4List()));
		}

		private void CopyMachineCode()
		{
			string code = ProgTool.GenerateMachineCode();
			try
			{
				Clipboard.SetText(code);
				Ui.Log("机器码已复制到剪贴板。", "Info");
			}
			catch (Exception ex)
			{
				Ui.Log("复制机器码失败：" + ex.Message, "Error");
			}
			Ui.Info("机器码（已尝试复制到剪贴板）：\r\n" + code);
		}

		#endregion

		#region 随机串与哈希

		private void GenRandom()
		{
			int len = (int)numRandomLen.Value;
			string extra = txtRandomChars.Text;
			string a = Tool.GetRandomString(len, extra);
			string b = Tool.GetRandomString(len, extra);
			string c = Tool.GetRandomString(len, extra);
			txtRandom.Text = $"{a}  |  {b}  |  {c}";
			Ui.Log($"GetRandomString({len}, \"{extra}\") → {a}", "Info");
		}

		private void HashSelf()
		{
			string path = Application.ExecutablePath;
			string hash = Tool.CalculateFileChecksum(path);
			txtHashOut.Text = $"文件：{path}\r\nSHA256：{hash}";
			Ui.Log($"文件校验值 {Path.GetFileName(path)} = {hash}", "Info");
		}

		private void HashFile()
		{
			using (var dlg = new OpenFileDialog { InitialDirectory = Program.WorkDir })
			{
				if (dlg.ShowDialog() != DialogResult.OK) return;
				try
				{
					string hash = Tool.CalculateFileChecksum(dlg.FileName);
					txtHashOut.Text = $"文件：{dlg.FileName}\r\n大小：{new FileInfo(dlg.FileName).Length} 字节\r\nSHA256：{hash}";
					Ui.Log($"文件校验值 {dlg.FileName} = {hash}", "Info");
				}
				catch (Exception ex)
				{
					Ui.Error("计算失败：" + ex.Message);
				}
			}
		}

		private void HashString()
		{
			string s = "WeiKit";
			string h = Tool.ComputeSha256Hash(s);
			txtHashOut.Text = $"字符串：{s}\r\nSHA256：{h}";
			Ui.Log($"字符串校验值 \"{s}\" = {h}", "Info");
		}

		private void CompareHash()
		{
			string a = Tool.ComputeSha256Hash("abc");
			string b = Tool.ComputeSha256Hash("ABC");     // 大小写不同 → 哈希不同
			Ui.Info($"ComputeSha256Hash(\"abc\") = {a.Substring(0, 24)}…\r\n" +
					$"ComputeSha256Hash(\"ABC\") = {b.Substring(0, 24)}…\r\n\r\n" +
					$"两者相等？{string.Equals(a, b, StringComparison.OrdinalIgnoreCase)}\r\n\r\n" +
					"实际用途：把服务器下发的 FileChecksum 与本地 CalculateFileChecksum 比对，" +
					"字符串比较必须用 OrdinalIgnoreCase（十六进制大小写可能不同）。");
			Ui.Log("已演示哈希比对（abc vs ABC）。", "Info");
		}

		#endregion

		#region 命令行与路径

		private void RunCmd(string cmd)
		{
			txtCmdOut.Text = $"正在执行 {cmd} …";
			Ui.Log($"执行 CMD：{cmd}", "Info");
			// 同步阻塞，放后台线程避免卡住 UI。
			// 注意：本类不是 Control，因此通过宿主面板 hostPanel 判断是否已释放。
			ToolDemo self = this;
			System.Threading.ThreadPool.QueueUserWorkItem(_ =>
			{
				string output;
				try { output = Tool.RunCmdCode(cmd); }
				catch (Exception ex) { output = "执行失败：" + ex.Message; }
				if (self.hostPanel.IsDisposed) return;
				Tool.SafeInvoke(self.txtCmdOut, () => self.txtCmdOut.Text = $"$ {cmd}\r\n" + output);
				Ui.Log($"CMD {cmd} 执行完成，输出 {output.Length} 字符。", "Info");
			});
		}

		private void RunCmdArray()
		{
			string[] codes = { "cd /d " + Tool.GetProgramPath().TrimEnd('\\'), "dir /b", "echo ---- 演示结束 ----" };
			txtCmdOut.Text = "正在执行多行命令…";
			Ui.Log("执行 CMD 命令数组。", "Info");
			ToolDemo self = this;
			System.Threading.ThreadPool.QueueUserWorkItem(_ =>
			{
				string output;
				try { output = Tool.RunCmdCode(codes); }
				catch (Exception ex) { output = "执行失败：" + ex.Message; }
				if (self.hostPanel.IsDisposed) return;
				Tool.SafeInvoke(self.txtCmdOut, () => self.txtCmdOut.Text = "$ " + string.Join(" && ", codes) + "\r\n" + output);
			});
		}

		private void PathDemo(string mode)
		{
			var sb = new System.Text.StringBuilder();
			switch (mode)
			{
				case "program":
					sb.AppendLine("Tool.GetProgramPath() = " + Tool.GetProgramPath());
					sb.AppendLine("（末尾自带反斜杠，可直接拼接文件名）");
					sb.AppendLine("注意区分 Environment.CurrentDirectory = " + Environment.CurrentDirectory);
					break;

				case "toabs":
					sb.AppendLine("输入：  .\\Data\\Config\\demo.json");
					sb.AppendLine("输出：  " + Tool.PathConversion(".\\Data\\Config\\demo.json"));
					sb.AppendLine("输入：  ./Data/Config/demo.json");
					sb.AppendLine("输出：  " + Tool.PathConversion("./Data/Config/demo.json"));
					sb.AppendLine("（把 .\\ 或 ./ 前缀替换为程序根目录）");
					break;

				case "torel":
					string abs = Path.Combine(Tool.GetProgramPath(), "Data", "Config", "demo.json");
					sb.AppendLine("输入：  " + abs);
					sb.AppendLine("输出：  " + Tool.PathConversion(abs));
					sb.AppendLine("（反算回相对路径）");
					break;

				case "newdir":
					string path = Tool.GetProgramPath();          // 必须传程序目录：传 "./" 会按工作目录解析
					string dir = Tool.AbsNewFolder(path, "Demo_");
					sb.AppendLine("在 " + path + " 下创建：");
					sb.AppendLine("  " + dir);
					sb.AppendLine("存在？" + Directory.Exists(dir));
					sb.AppendLine("（目录名 = 前缀 + 6 位随机串，绝不重名）");
					Ui.Log("已创建随机目录：" + dir, "Info");
					break;
			}
			txtPathOut.Text = sb.ToString();
		}

		#endregion

		#region DataFlowList

		private void FlowDemo()
		{
			var flow = new Tool.DataFlowList();
			flow.SetFlowMaxLeng(30);

			var rnd = new Random();
			var values = new int[30];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = rnd.Next(0, 100);
				flow.FlowAdd(values[i]);
			}

			object[] data = flow.GetDataArray();
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("写入的 30 个随机整数：");
			sb.AppendLine("  " + string.Join(", ", values));
			sb.AppendLine();
			sb.AppendLine("DataFlowList(上限 30) 统计结果：");
			sb.AppendLine($"  元素个数        = {data.Length}");
			sb.AppendLine($"  GetAverageValue()  = {flow.GetAverageValue()}     （int 平均值）");
			sb.AppendLine($"  GetAverageValueF() = {flow.GetAverageValueF():F2}  （float 平均值）");
			sb.AppendLine($"  GetMaxValue()      = {flow.GetMaxValue()}");
			sb.AppendLine($"  GetMinValue()      = {flow.GetMinValue()}");
			sb.AppendLine();
			sb.AppendLine("再写入 10 个值，观察滑动窗口（最早的数据被挤出）：");
			flow.SetFlowMaxLeng(20);
			object[] after = flow.GetDataArray();
			sb.AppendLine($"  SetFlowMaxLeng(20) 后元素个数 = {after.Length}");
			sb.AppendLine($"  当前内容 = {string.Join(", ", after.Select(o => o.ToString()))}");
			txtFlowOut.Text = sb.ToString();
			Ui.Log($"DataFlowList 统计完成：平均 {(int)flow.GetAverageValueF()}，最大 {flow.GetMaxValue()}。", "Info");
		}

		#endregion

		#region 其它

		private void MapDemo()
		{
			double v = 50;
			double mapped = Tool.MapValue(v, 0, 100, 0, 300);
			Ui.Info($"MapValue(value: {v}, xMin: 0, xMax: 100, yMin: 0, yMax: 300) = {mapped}\r\n\r\n" +
					"用途：把进度值映射成像素宽度。Comp.ProgressBar 内部就是这么算进度条宽度的，" +
					"因此 Progress 超出 [MinValue, MaxValue] 时宽度会溢出。");
		}

		private void SelfDeleteDemo()
		{
			string temp = Path.Combine(Path.GetTempPath(), "weikit_demo_temp_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".tmp");
			File.WriteAllText(temp, "这是一个演示用的临时文件。");
			Ui.Log("已创建临时文件：" + temp, "Info");

			if (MessageBox.Show(
				"将生成一个自删除批处理脚本，它在主程序退出后删除下面这个文件：\r\n\r\n" + temp +
				"\r\n\r\n脚本执行时会弹出一个命令行窗口（这是设计行为）。\r\n继续吗？",
				"确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				File.Delete(temp);
				return;
			}

			ProgTool.CreateSelfDeleteScript(new[] { temp, Application.ExecutablePath.Replace(".exe", ".pdb") });
			Ui.Info("脚本已启动。它会在本程序退出后删除临时文件（以及 pdb）。");
		}

		#endregion
	}
}
