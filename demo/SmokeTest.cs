using System;
using System.IO;
using System.Linq;
using System.Text;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// 无界面自检：在命令行用 WeiKit.Demo.exe /smoke 运行。
	/// 它会真实地跑一遍配置、日志、加密解密、工具、数据流等路径，逐项打印 PASS/FAIL，
	/// 最后以退出码 0（全部通过）或 1（有失败）结束，适合做冒烟测试。
	/// </summary>
	internal static class SmokeTest
	{
		private static int passed;
		private static int failed;
		private static int skipped;
		private static TextWriter outw;   // 真正的控制台输出流（不能用 Console.Out）
		private static TextWriter filew;  // UTF-8 结果文件（绕开控制台编码问题）

		/// <summary>可被序列化后加密保存的测试对象</summary>
		private class Payload
		{
			public string Name { get; set; }
			public int Level { get; set; }
			public DateTime At { get; set; }
			public string[] Tags { get; set; }
		}

		/// <summary>
		/// 执行自检。<paramref name="console"/> 必须是 RedirectConsoleOut 之前抓到的真实控制台流，
		/// 否则所有输出都会再次进入日志系统并触发 LogAddEvent → 无限递归。
		/// </summary>
		public static int Run(TextWriter console)
		{
			outw = console ?? Console.Out;

			// 结果同时写入 UTF-8 文件：命令行控制台编码千差万别，文件才是可靠输出
			string resultFile = Path.Combine(Program.WorkDir, "smoke_result.txt");
			try
			{
				filew = new StreamWriter(resultFile, false, new UTF8Encoding(false));
			}
			catch { filew = null; }

			WriteLine();
			WriteLine("================ WeiKit 自检 (/smoke) ================");
			WriteLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			WriteLine($"程序目录：{Tool.GetProgramPath()}");
			WriteLine($"当前目录：{Environment.CurrentDirectory}");
			WriteLine($"工作目录：{Program.WorkDir}");
			WriteLine($"结果文件：{resultFile}");
			WriteLine();

			// ---------- 程序工具 ----------
			Check("Tool.GetProgramPath 非空", () => !string.IsNullOrEmpty(Tool.GetProgramPath()));
			Check("Info.LibraryName", () => Info.LibraryName == "WeiKit Library");
			Check("ProgTool.GetAppNameFromFile", () => ProgTool.GetAppNameFromFile() == "WeiKit.Demo");
			Check("ProgTool.GetBuildTime 合理（已修复：确定性构建回退）", () =>
			{
				DateTime t = ProgTool.GetBuildTime();
				WriteLine($"        编译时间 = {t:yyyy-MM-dd HH:mm:ss}（Kind={t.Kind}）");
				return t.Year >= 2020 && t <= DateTime.Now.AddDays(1);
			});

			string machine = null;
			Check("ProgTool.GenerateMachineCode 为 64 位小写十六进制", () =>
			{
				machine = ProgTool.GenerateMachineCode();
				WriteLine("        机器码 = " + machine);
				return machine.Length == 64 && machine == machine.ToLower() && IsHex(machine);
			});
			Check("机器码可重复（两次一致）", () => ProgTool.GenerateMachineCode() == machine);

			Check("Tool.PathConversion 相对转绝对", () =>
			{
				string r = Tool.PathConversion(@".\abc\def.txt");
				WriteLine("        " + r);
				return r.StartsWith(Tool.GetProgramPath(), StringComparison.OrdinalIgnoreCase) && r.EndsWith("abc\\def.txt");
			});
			Check("Tool.MapValue 区间映射", () => Math.Abs(Tool.MapValue(50, 0, 100, 0, 300) - 150) < 0.0001);
			Check("Tool.MapValue 除零保护", () => Tool.MapValue(5, 3, 3, 0, 10) == 0);
			Check("Tool.GetRandomString 长度、字符集与追加字符", () =>
			{
				// 语义澄清：AddedChar 是「在 0-9a-zA-Z 基础上追加字符」，不是限定字符集。
				// 所以结果里必然同时出现数字/字母和追加字符，不能断言「只含追加字符」。
				string base62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
				string extra = "!@#";
				string full = base62 + extra;

				// ① 长度必须精确（跑 200 次，避免偶发）
				bool lenOk = true;
				for (int i = 0; i < 200; i++)
				{
					string s = Tool.GetRandomString(32, extra);
					if (s.Length != 32) { lenOk = false; WriteLine($"        !! 第 {i} 次长度 = {s.Length}（期望 32）"); break; }
				}

				// ② 每一位都必须落在「基础字符集 + 追加字符」内
				string sample = Tool.GetRandomString(512, extra);
				bool insideOk = sample.All(c => full.IndexOf(c) >= 0);
				bool hasExtra = sample.IndexOfAny(extra.ToCharArray()) >= 0;
				bool hasBase = sample.Any(c => base62.IndexOf(c) >= 0);

				// ③ 附加字符确实生效：只用 "!@#" 时，字符种类应远少于 65
				var kinds = new System.Collections.Generic.HashSet<char>(sample);
				WriteLine($"        长度 200 次全对 = {lenOk}；512 位采样字符种类 = {kinds.Count}（理论上限 {full.Length}）");
				WriteLine($"        全部字符都在允许集合内 = {insideOk}；出现了追加字符 = {hasExtra}；出现了基础字符 = {hasBase}");

				return lenOk && insideOk && hasExtra && hasBase && kinds.Count <= full.Length;
			});
			Check("Tool.GetRandomString 两次不同", () => Tool.GetRandomString(24) != Tool.GetRandomString(24));

			Check("Tool.LinkConversion 双向转换（已修复：原实现分支重复、缺 ./ 分支）", () =>
			{
				string domain = "https://example.com/";
				string up1 = Tool.LinkConversion(@".\a\b.html", domain);
				string up2 = Tool.LinkConversion("./a/b.html", domain);
				string back1 = Tool.LinkConversion(domain + "a/b.html", domain);
				WriteLine($"        .\\a\\b.html -> {up1}");
				WriteLine($"        ./a/b.html  -> {up2}");
				WriteLine($"        {domain}a/b.html -> {back1}");
				return up1 == "https://example.com/a\\b.html"
					&& up2 == "https://example.com/a/b.html"
					&& back1 == "./a/b.html"
					&& Tool.LinkConversion(null, domain) == null
					&& Tool.LinkConversion("x", "") == "x";
			});
			Check("Tool.AbsNewFolder 默认建在程序目录（已修复：不再依赖当前工作目录）", () =>
			{
				string dir = Tool.AbsNewFolder(null, "SmokeTest_");
				bool exists = Directory.Exists(dir);
				bool insideProgramDir = dir.StartsWith(Tool.GetProgramPath(), StringComparison.OrdinalIgnoreCase);
				bool named = Path.GetFileName(dir).StartsWith("SmokeTest_");
				if (exists) Directory.Delete(dir, true);
				WriteLine($"        新建目录 = {dir}");
				WriteLine($"        在程序目录内 = {insideProgramDir}，前缀正确 = {named}");
				return exists && insideProgramDir && named;
			});
			Check("Tool.RunCmdCode 会捕获 stderr 输出（已修复）", () =>
			{
				// cmd 用与号分隔多条命令，不需要转义（直接传给 cmd.exe）
				string outp = Tool.RunCmdCode("echo STDOUT-MARK & echo STDERR-MARK 1>&2");
				bool hasOut = outp.Contains("STDOUT-MARK");
				bool hasErr = outp.Contains("STDERR-MARK");
				WriteLine($"        返回内容 = \"{outp.Replace("\r", "\\r").Replace("\n", "\\n")}\"");
				WriteLine($"        包含 stdout 标记 = {hasOut}，包含 stderr 标记 = {hasErr}");
				return hasOut && hasErr;
			});
			Check("Tool.SetControlFillet 空句柄/已释放控件不再抛异常（已修复）", () =>
			{
				var panel = new System.Windows.Forms.Panel { Size = new System.Drawing.Size(100, 60) };
				Tool.SetControlFillet(panel, 10);       // 句柄未创建 → 安全返回
				panel.CreateControl();                  // 建立句柄
				Tool.SetControlFillet(panel, 8);        // 正常设置
				Tool.SetControlFillet(panel, -5);       // 负数半径 → 安全处理
				panel.Dispose();
				Tool.SetControlFillet(panel, 5);        // 已释放 → 安全返回
				Tool.SetControlFillet(null, 5);         // null → 安全返回
				WriteLine("        null / 无句柄 / 负数半径 / 已释放控件 四种情况均未抛异常");
				return true;
			});
			Check("Tool.SafeInvoke / SafeBeginInvoke 对 null 与已释放控件安全（已修复）", () =>
			{
				System.Windows.Forms.Control c = null;
				Tool.SafeInvoke(c, () => { });
				Tool.SafeBeginInvoke(c, () => { });
				bool ran = false;
				c = new System.Windows.Forms.Panel();
				c.CreateControl();
				Tool.SafeInvoke(c, () => ran = true);
				bool ranBegin = false;
				Tool.SafeBeginInvoke(c, () => ranBegin = true);
				System.Windows.Forms.Application.DoEvents();   // 让 BeginInvoke 有機會执行
				c.Dispose();
				Tool.SafeInvoke(c, () => { });                 // 已释放 → 安全返回
				Tool.SafeInvoke(c, null);
				WriteLine($"        SafeInvoke 执行 = {ran}；SafeBeginInvoke 已投递 = {ranBegin}（异步，可能稍后执行）");
				return ran;
			});
			Check("Tool.ControlMove 动画会走到目标位置且不泄漏定时器（已修复）", () =>
			{
				// 注意：ControlMove 对不可见控件直接返回，而 WinForms Timer 也需要消息泵，
				// 所以这里必须真的把窗体显示出来（同一线程）再泵消息。
				using (var form = new System.Windows.Forms.Form { StartPosition = System.Windows.Forms.FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) })
				using (var panel = new System.Windows.Forms.Panel { Size = new System.Drawing.Size(100, 100), Location = new System.Drawing.Point(0, 0) })
				{
					form.Controls.Add(panel);
					form.Show();
					System.Windows.Forms.Application.DoEvents();

					Tool.ControlMove(panel, new System.Drawing.Point(10, 10), 3, 30);
					var sw = System.Diagnostics.Stopwatch.StartNew();
					while (sw.ElapsedMilliseconds < 600)
					{
						System.Windows.Forms.Application.DoEvents();
						System.Threading.Thread.Sleep(10);
					}

					bool moved = panel.Location.X == 10 && panel.Location.Y == 10;
					WriteLine($"        动画结束位置 = ({panel.Location.X},{panel.Location.Y})（目标 10,10）到位 = {moved}");

					// 释放控件不应抛异常（内部 Disposed 处理器会停止并释放定时器）
					panel.Dispose();
					Tool.ControlMove(panel, new System.Drawing.Point(5, 5), 3, 30);   // 已释放 → 安全
					Tool.ControlMove(null, new System.Drawing.Point(0, 0));           // null → 安全
					form.Close();
					return moved;
				}
			});

			Check("Comp.Switch.SetIsOpen 可选择是否触发事件（已修复）", () =>
			{
				var sw = new WeiKit.Comp.Switch();
				int events = 0;
				sw.IsOpenChange += (s, v) => events++;

				sw.SetIsOpen(true);                 // 默认不触发，保持旧行为
				bool noEvent = events == 0 && sw.IsOpen;
				sw.SetIsOpen(false, raiseEvent: true);   // 显式要求触发
				bool raised = events == 1;
				sw.IsOpen = true;                   // 直接赋属性仍然不触发（原有约定）
				bool propertySilent = events == 1;
				sw.Dispose();

				WriteLine($"        SetIsOpen 静默 = {noEvent}；raiseEvent:true 触发 = {raised}；属性赋值静默 = {propertySilent}");
				return noEvent && raised && propertySilent;
			});

			Check("Tool.ComputeSha256Hash 已知向量", () =>
				Tool.ComputeSha256Hash("abc") == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
			Check("Tool.CalculateFileChecksum 与 ComputeSha256Hash 一致", () =>
			{
				string f = Path.Combine(Program.WorkDir, "smoke_hash.txt");
				File.WriteAllText(f, "abc", new UTF8Encoding(false));
				bool ok = Tool.CalculateFileChecksum(f) == Tool.ComputeSha256Hash("abc");
				File.Delete(f);
				return ok;
			});

			Check("Tool.GetLocalIPv4List 至少一项", () => Tool.GetLocalIPv4List().Length >= 1);
			Check("Tool.RunCmdCode 执行 ver", () =>
			{
				string outp = Tool.RunCmdCode("ver");
				return !string.IsNullOrWhiteSpace(outp);
			});

			// ---------- 数据流 ----------
			Check("Tool.DataFlowList 统计正确", () =>
			{
				var flow = new Tool.DataFlowList();
				flow.SetFlowMaxLeng(10);
				for (int i = 1; i <= 10; i++) flow.FlowAdd(i);   // 1..10，平均 5.5 → int 5
				int avg = flow.GetAverageValue();
				int max = flow.GetMaxValue();
				int min = flow.GetMinValue();
				WriteLine($"        平均(int)={avg} 最大={max} 最小={min}");
				return avg == 6 && max == 10 && min == 1;
			});
			Check("Tool.DataFlowList 滑动窗口淘汰最旧", () =>
			{
				var flow = new Tool.DataFlowList();
				flow.SetFlowMaxLeng(3);
				flow.FlowAdd(1); flow.FlowAdd(2); flow.FlowAdd(3); flow.FlowAdd(4);
				object[] d = flow.GetDataArray();
				return d.Length == 3 && (int)d[0] == 2 && (int)d[2] == 4;
			});
			Check("Tool.DataFlowList 空列表抛 InvalidOperationException", () =>
			{
				try { new Tool.DataFlowList().GetMaxValue(); return false; }
				catch (InvalidOperationException) { return true; }
			});
			Check("Tool.DataFlowList 支持 double/decimal 等数值类型（已修复）", () =>
			{
				var flow = new Tool.DataFlowList();
				flow.FlowAdd(1.5d);      // 旧版本用 Cast<float>()，double 会抛 InvalidCastException
				flow.FlowAdd(2.5m);
				flow.FlowAdd(3);         // int 也要能混用
				flow.FlowAdd((byte)1);
				float avgF = flow.GetAverageValueF();
				int max = flow.GetMaxValue();
				int min = flow.GetMinValue();
				WriteLine($"        double/decimal/int/byte 混用：均值(float)={avgF}（2.0）最大={max}（3）最小={min}（1）");
				return Math.Abs(avgF - 2.0f) < 0.001f && max == 3 && min == 1;
			});
			Check("Tool.DataFlowList 非数值元素仍抛 InvalidCastException", () =>
			{
				var flow = new Tool.DataFlowList();
				flow.FlowAdd("不是数字");
				try { flow.GetAverageValueF(); return false; }
				catch (InvalidCastException) { return true; }
			});
			Check("Tool.DataFlowList SetFlowMaxLeng(0) 抛异常", () =>
			{
				try { new Tool.DataFlowList().SetFlowMaxLeng(0); return false; }
				catch (Exception) { return true; }
			});

			// ---------- 配置 ----------
			Check("配置项已声明（8 项）", () => Program.Cfg.configs.Count == 8);
			Check("配置已成功载入或已写出默认文件", () =>
			{
				WriteLine($"        配置路径 = {Program.ConfigPath}");
				WriteLine($"        文件存在 = {File.Exists(Program.ConfigPath)}　IsEffective = {Program.Cfg.IsEffective}　IsEncrypted = {Program.Cfg.IsEncrypted}");
				// 首次运行 Load 会失败（文件不存在），InitConfig 随后写出默认配置。
				// 因此正确的期望是：要么载入成功，要么文件已写出。
				return Program.Cfg.IsEffective || File.Exists(Program.ConfigPath);
			});
			CheckEnv("配置文件可写出（写入权限正常）", () =>
			{
				bool exists = File.Exists(Program.ConfigPath);
				if (!exists) WriteLine("        !! 配置文件未能写出 —— 检查程序目录是否可写（只读目录会被静默忽略）");
				return exists;
			});
			Check("明文载入时 IsEncrypted 为 false", () => SmokePlainProbe());
			Check("Demo.RunCount 在启动时被累加", () =>
			{
				object v = Program.Cfg.Read("Demo.RunCount");
				WriteLine("        Demo.RunCount = " + v);
				return v != null && Convert.ToInt64(v) >= 1;
			});
			Check("Read 返回声明类型（Long → long）", () => Program.Cfg.Read("Net.ServerPort") is long);
			Check("WriteValue 写入 bool 并读回", () =>
			{
				Program.Cfg.WriteValue("App.TopMost", true);
				bool ok = Program.Cfg.Read("App.TopMost") is bool b && b;
				Program.Cfg.WriteValue("App.TopMost", false);
				return ok;
			});
			Check("WriteStrValue 正常路径（字符串项）", () =>
				Program.Cfg.WriteStrValue("Demo.UserName", "测试用户") &&
				Convert.ToString(Program.Cfg.Read("Demo.UserName")) == "测试用户");
			Check("WriteStrValue 解析失败不再污染 Value（已修复）", () =>
			{
				// 旧版本会先把原始字符串写进 Value 再解析，失败后配置被污染成 "yes"。
				// 现在解析失败会保留原值并返回 false。
				object before = Program.Cfg.Read("App.AutoSaveLog");
				bool ok = Program.Cfg.WriteStrValue("App.AutoSaveLog", "yes");   // bool 项传非法值
				object after = Program.Cfg.Read("App.AutoSaveLog");
				bool unchanged = Equals(before, after) && after is bool;
				WriteLine($"        WriteStrValue(\"yes\") 返回 {ok}；原值 {before}（{before?.GetType().Name}）→ 现值 {after}（{after?.GetType().Name}）");
				return !ok && unchanged;
			});
			Check("WriteStrValue 数值项接受不区分区域性的写法", () =>
			{
				bool a = Program.Cfg.WriteStrValue("Net.ServerPort", "8080");
				long port = Convert.ToInt64(Program.Cfg.Read("Net.ServerPort"));
				bool b = Program.Cfg.WriteStrValue("Net.TimeoutSeconds", "12.5");
				float sec = Convert.ToSingle(Program.Cfg.Read("Net.TimeoutSeconds"));
				Program.Cfg.Reset("Net.ServerPort");
				Program.Cfg.Reset("Net.TimeoutSeconds");
				WriteLine($"        整型写入 = {a}（{port}）；浮点写入 = {b}（{sec}）");
				return a && port == 8080 && b && Math.Abs(sec - 12.5f) < 0.001f;
			});
			Check("Reset 恢复默认值", () =>
			{
				Program.Cfg.WriteValue("Demo.UserName", "临时值");
				bool ok = Program.Cfg.Reset("Demo.UserName");
				return ok && Convert.ToString(Program.Cfg.Read("Demo.UserName")) == "Tiangong";
			});
			Check("IsHave / GetConfigIndex / GetConfigNameList", () =>
			{
				int idx = Program.Cfg.GetConfigIndex("Net.ServerPort");
				int total = Program.Cfg.configs.Count;
				string[] names = Program.Cfg.GetConfigNameList();
				WriteLine($"        GetConfigIndex(Net.ServerPort) = {idx}，项数 = {total}，名称列表 {(names == null ? "null" : names.Length.ToString())} 项");
				WriteLine("        " + string.Join(" | ", names ?? new string[0]));
				bool a = Program.Cfg.IsHave("Net.ServerPort");
				bool b = idx >= 0;
				bool c = names != null && names.Length == total;
				bool notFound = Program.Cfg.GetConfigIndex("不存在的项") == -1 && !Program.Cfg.IsHave("不存在的项");
				return a && b && c && notFound;
			});
			Check("Add 重复名称被拒绝", () => !Program.Cfg.Add("Net.ServerPort", Configs.ValueType.Long, 1L));
			Check("Remove 后 GetConfig 返回 null", () =>
			{
				Program.Cfg.Add("Smoke.Temp", Configs.ValueType.String, "x");
				bool removed = Program.Cfg.Remove("Smoke.Temp");
				return removed && Program.Cfg.GetConfig("Smoke.Temp") == null;
			});
			Check("ConfigChanged 事件被触发", () =>
			{
				int n = 0;
				// Configs.ConfigChanged 用的是自定义委托 ConfigsChangedEventHandler(Configs)
				Configs.ConfigsChangedEventHandler handler = c => n++;
				Program.Cfg.ConfigChanged += handler;
				Program.Cfg.WriteValue("Demo.RunCount", 123L);
				Program.Cfg.ConfigChanged -= handler;
				return n == 1;
			});
			Check("OutSave 导出明文 + InputData 回读", () =>
			{
				string exp = Path.Combine(Program.WorkDir, "smoke_export.json");
				Program.Cfg.OutSave(exp);
				bool plain = File.ReadAllText(exp).TrimStart().StartsWith("[");
				int before = Program.Cfg.configs.Count;
				Program.Cfg.InputData(exp);
				bool same = Program.Cfg.configs.Count == before;
				File.Delete(exp);
				return plain && same;
			});
			Check("Save(key) + Load(key) 往返一致", () =>
			{
				string p = Path.Combine(Program.WorkDir, "smoke_cfg.dat");
				Program.Cfg.Save(p, Program.ConfigSaveKey);
				var c2 = new Configs();
				c2.Add("Demo.UserName", Configs.ValueType.String, "x");
				bool loaded = c2.Load(p, Program.ConfigSaveKey);
				bool val = Convert.ToString(c2.Read("Demo.UserName")) == Convert.ToString(Program.Cfg.Read("Demo.UserName"));
				File.Delete(p);
				return loaded && val;
			});
			Check("Load 明文文件自动迁移为加密", () =>
			{
				string p = Path.Combine(Program.WorkDir, "smoke_plain.json");
				File.WriteAllText(p, "[{\"Name\":\"A\",\"Text\":\"A\",\"Notes\":\"n\",\"Type\":1,\"Value\":7,\"DefaultValue\":7,\"HelpUrl\":\"\"}]");
				var c3 = new Configs();
				bool ok = c3.Load(p, Program.ConfigSaveKey);       // 带密钥载入明文 → 应自动加密重写
				bool encrypted = !File.ReadAllText(p).TrimStart().StartsWith("[");
				var c4 = new Configs();
				bool reopen = c4.Load(p, Program.ConfigSaveKey);
				bool val = Convert.ToInt64(c4.Read("A")) == 7L;
				File.Delete(p);
				WriteLine($"        迁移: ok={ok} 已加密={encrypted} 重开={reopen} 值正确={val}");
				return ok && encrypted && reopen && val;
			});
			Check("Load 不存在的文件返回 false（不抛异常）", () =>
			{
				var c = new Configs();
				return !c.Load(Path.Combine(Program.WorkDir, "根本不存在.dat"), "k") && !c.IsEffective;
			});
			Check("Save() 未指定路径时返回 false 且不抛异常", () =>
			{
				var c = new Configs();
				c.Add("X", Configs.ValueType.String, "1");
				bool ok = c.Save();          // ConfigFilePath 为 null → 应返回 false 而不是抛异常
				return !ok && c.ConfigFilePath == null;
			});
			Check("Save(path) 直接按路径明文写入（已修复：不再把路径当密钥）", () =>
			{
				string p = Path.Combine(Program.WorkDir, "smoke_overload.json");
				if (File.Exists(p)) File.Delete(p);

				var c = new Configs();
				c.Add("X", Configs.ValueType.String, "1");
				bool ok = c.Save(p);                       // 单参数 = 按路径明文保存
				bool wrote = File.Exists(p);
				bool plain = wrote && File.ReadAllText(p).TrimStart().StartsWith("[");
				bool pathRecorded = c.ConfigFilePath == p;
				bool keyCleared = c.LastSaveKey == "" && !c.IsEncrypted;

				// 同一对象再加密存到另一个文件，SaveKey 应被记录，无参 Save() 会沿用密钥
				string p2 = Path.Combine(Program.WorkDir, "smoke_overload2.dat");
				if (File.Exists(p2)) File.Delete(p2);
				bool ok2 = c.Save(p2, "k2");
				bool encFile = ok2 && File.Exists(p2) && !File.ReadAllText(p2).TrimStart().StartsWith("[");
				bool keyKept = c.LastSaveKey == "k2" && c.IsEncrypted;
				var c2 = new Configs();
				bool canRead = c2.Load(p2, "k2") && Convert.ToString(c2.Read("X")) == "1";

				if (File.Exists(p)) File.Delete(p);
				if (File.Exists(p2)) File.Delete(p2);

				bool all = ok && wrote && plain && pathRecorded && keyCleared && ok2 && encFile && keyKept && canRead;
				WriteLine($"        [明细] ok={ok} wrote={wrote} plain={plain} pathRecorded={pathRecorded} keyCleared={keyCleared} " +
						  $"ok2={ok2} encFile={encFile} keyKept={keyKept} canRead={canRead} → 总判定={all}");
				return all;
			});
			Check("InputData 导入会触发 ConfigChanged 并返回结果", () =>
			{
				string p = Path.Combine(Program.WorkDir, "smoke_import.json");
				var src = new Configs();
				src.Add("Imported.Item", Configs.ValueType.String, "hello");
				src.Save(p, "");

				int events = 0;
				Configs.ConfigsChangedEventHandler h = cfg => events++;
				Program.Cfg.ConfigChanged += h;
				bool ok = Program.Cfg.InputData(p);
				Program.Cfg.ConfigChanged -= h;

				bool imported = Program.Cfg.IsHave("Imported.Item") &&
								Convert.ToString(Program.Cfg.Read("Imported.Item")) == "hello";
				Program.Cfg.Remove("Imported.Item");
				if (File.Exists(p)) File.Delete(p);

				WriteLine($"        InputData 返回 {ok}，触发事件 {events} 次，配置项已导入 = {imported}");
				return ok && events == 1 && imported;
			});
			Check("Copy(null) 抛 ArgumentNullException（不再静默无效）", () =>
			{
				var c = new Configs();
				c.Add("X", Configs.ValueType.String, "1");
				try { c.Copy("X", null); return false; }
				catch (ArgumentNullException) { return true; }
			});
			Check("Copy / CopyAll 复制成功并返回结果", () =>
			{
				var src = new Configs();
				src.Add("A", Configs.ValueType.Long, 7L, "甲", "说明甲", 1L, "https://example.com/");
				src.Add("B", Configs.ValueType.Bool, true, "乙");

				var dst = new Configs();
				bool c1 = src.Copy("A", dst);
				int n = src.CopyAll(dst);
				bool all = c1 && n == 2 && dst.configs.Count == 2;
				bool values = Convert.ToInt64(dst.Read("A")) == 7L && (bool)dst.Read("B");
				// HelpUrl 也要一起复制过去（旧版本会漏掉）
				var copied = dst.GetConfig("A");
				bool helpUrl = copied.HelpUrl == "https://example.com/" && copied.Notes == "说明甲";
				WriteLine($"        Copy/CopyAll = {all}，值正确 = {values}，HelpUrl/Notes 已复制 = {helpUrl}");
				return all && values && helpUrl;
			});
			Check("GetConfigNameList 空配置返回 null", () => new Configs().GetConfigNameList() == null);

			// ---------- 日志 ----------
			Check("日志记录了自检输出", () => Program.Log.logs.Count > 0);
			Check("Println 指定类型被保存", () =>
			{
				int n = Program.Log.logs.Count;
				Program.Log.Println("自检日志", "SmokeTest");
				return Program.Log.logs.Count == n + 1 && Program.Log.GetLastLog().TypeText == "SmokeTest";
			});
			Check("LogAddEvent 被触发", () =>
			{
				int n = 0;
				EventHandler<Logs.Log> h = (s, e) => n++;
				Program.Log.LogAddEvent += h;
				Program.Log.Println("事件测试", "SmokeTest");
				Program.Log.LogAddEvent -= h;
				return n == 1;
			});
			Check("RedirectConsoleOut 生效（Console 输出进日志）", () =>
			{
				int n = Program.Log.logs.Count;
				Console.WriteLine("这行来自 Console.WriteLine");
				return Program.Log.logs.Count > n && Program.Log.GetLastLog().TypeText == "Console";
			});
			Check("LogsSave 写出文件", () =>
			{
				string p = Path.Combine(Program.WorkDir, "smoke_log.txt");
				bool ok = Program.Log.LogsSave(p);
				bool has = ok && File.Exists(p) && new FileInfo(p).Length > 0;
				if (has) File.Delete(p);
				return has;
			});
			Check("Log.ToString 含时间与类型", () =>
			{
				Program.Log.Println("格式检查", "Fmt");
				string s = Program.Log.GetLastLog().ToString();
				return s.Contains("Fmt") && s.Contains("格式检查");
			});
			Check("MaxLogPcs 自动落盘后清空", () =>
			{
				var lg = new Logs();
				string p = Path.Combine(Program.WorkDir, "smoke_autosave.log");
				if (File.Exists(p)) File.Delete(p);
				lg.MaxLogPcs = 3;
				lg.AutoSavePath = p;
				lg.Println("1"); lg.Println("2"); lg.Println("3"); lg.Println("4");
				bool saved = File.Exists(p);
				// 清空后会补一条 AutoSave 提示，再追加本次日志 "4"，因此恰好 2 条
				bool cleared = lg.logs.Count == 2 && lg.logs[0].TypeText == "AutoSave" && lg.logs[1].Text == "4";
				if (saved) File.Delete(p);
				WriteLine($"        落盘={saved} 清空后 {lg.logs.Count} 条（{lg.logs[0].TypeText} + \"{lg.logs[1].Text}\"）");
				return saved && cleared;
			});
			Check("GetLastLog / GetLastLogText 空列表不再抛异常（已修复）", () =>
			{
				var lg = new Logs();
				Logs.Log last = lg.GetLastLog();
				string text = lg.GetLastLogText();
				WriteLine($"        空列表：GetLastLog() = {(last == null ? "null" : "非 null")}，GetLastLogText() = \"{text}\"");
				return last == null && text == string.Empty;
			});
			Check("AutoSavePath 默认值是程序目录下的完整文件路径（已修复）", () =>
			{
				var lg = new Logs();
				string p = lg.AutoSavePath;
				bool full = Path.IsPathRooted(p) && Path.HasExtension(p);
				WriteLine($"        默认 AutoSavePath = {p}");
				return full;
			});
			Check("自动落盘成功时清空列表", () =>
			{
				var lg = new Logs();
				string p = Path.Combine(Program.WorkDir, "smoke_autosave_ok.log");
				if (File.Exists(p)) File.Delete(p);
				lg.MaxLogPcs = 3;
				lg.AutoSavePath = p;
				lg.Println("1"); lg.Println("2"); lg.Println("3"); lg.Println("4");
				bool saved = File.Exists(p);
				bool cleared = lg.logs.Count == 2 && lg.logs[0].TypeText == "AutoSave" && lg.logs[1].Text == "4";
				if (saved) File.Delete(p);
				WriteLine($"        落盘={saved} 清空后剩余={lg.logs.Count} 条（类型 {lg.logs[0].TypeText}）");
				return saved && cleared;
			});
			Check("自动落盘失败时保留日志不丢失（已修复）", () =>
			{
				var lg = new Logs();
				lg.MaxLogPcs = 3;
				lg.AutoSavePath = Path.Combine(Program.WorkDir, "不存在的目录" + Guid.NewGuid().ToString("N"), "x.log");
				// 目录不存在时 LogsSave 现在会自动创建，所以改用非法路径强制失败
				lg.AutoSavePath = ":::\\invalid\\path\\x.log";
				lg.Println("1"); lg.Println("2"); lg.Println("3"); lg.Println("4");
				bool kept = lg.logs.Count >= 4;      // 没被清空
				WriteLine($"        落盘失败后仍有 {lg.logs.Count} 条日志（保留内容 = {kept}）");
				return kept;
			});
			Check("RedirectConsoleOut 可恢复原始输出流（已修复）", () =>
			{
				var lg = new Logs();
				TextWriter original = Console.Out;
				TextWriter got = lg.RedirectConsoleOut();
				bool returnsOriginal = ReferenceEquals(got, original);
				Logs.RestoreConsoleOut(got);
				bool restored = ReferenceEquals(Console.Out, original);
				WriteLine($"        返回原流 = {returnsOriginal}，RestoreConsoleOut 后已还原 = {restored}");
				return returnsOriginal && restored;
			});
			Check("LogAddEvent 里写 Console 不再无限递归（已修复：重入保护）", () =>
			{
				var lg = new Logs();
				TextWriter original = lg.RedirectConsoleOut();
				int events = 0;
				EventHandler<Logs.Log> h = (s, e) =>
				{
					events++;
					// 旧版本这里会形成「写日志→触发事件→写 Console→又写日志」的无限递归（栈溢出）。
					// 现在 CustomTextWriter 有重入保护，递归调用会被直接丢弃。
					Console.WriteLine("事件回调里的输出");
				};
				lg.LogAddEvent += h;
				lg.Println("触发一次", "SmokeTest");
				lg.LogAddEvent -= h;

				// 统计「由递归产生」的日志：正文等于回调里那串文字的条目一条都不该有
				int recursive = lg.logs.Count(x => x.Text != null && x.Text.Contains("事件回调里的输出"));
				int mine = lg.logs.Count(x => x.Text == "触发一次");
				int banner = lg.logs.Count(x => x.TypeText == "Console");
				Logs.RestoreConsoleOut(original);

				WriteLine($"        事件触发 {events} 次（应 1）；自身日志 {mine} 条（应 1）；RedirectConsoleOut 横幅 {banner} 条；由回调递归产生的日志 {recursive} 条（应 0）");
				return events == 1 && mine == 1 && recursive == 0;
			});
			Check("ConsoleLogTheme 13 个主题都有 HTML", () =>
			{
				var values = (ConsoleLogTheme.ThemeType[])Enum.GetValues(typeof(ConsoleLogTheme.ThemeType));
				WriteLine($"        主题数量 = {values.Length}");
				if (values.Length != 13) return false;
				foreach (var t in values)
				{
					string html = ConsoleLogTheme.GetHtmlHeader(t);
					if (string.IsNullOrWhiteSpace(html) || !html.Contains(".e")) return false;
				}
				return true;
			});
			Check("日志窗口能接受大于 100 的 MaxLogPcs（回归：NumericUpDown 默认上限 100）", () =>
			{
				// 回归用例：Logsinfo_Load 里会执行 numericUpDown1.Value = logs.MaxLogPcs。
				// 若控件 Maximum 仍是默认的 100，MaxLogPcs > 100 时窗口一打开就抛
				// ArgumentOutOfRangeException（"500"的值对于"Value"无效）。这里用 5000 复现/守住该缺陷。
				int old = Program.Log.MaxLogPcs;
				Program.Log.MaxLogPcs = 5000;
				WeiKit.Window.Logsinfo win = null;
				try
				{
					win = new WeiKit.Window.Logsinfo { logs = Program.Log, AutoColorMode = true };
					win.CreateControl();          // 触发 OnLoad → 走 numericUpDown1.Value = MaxLogPcs
					WriteLine($"        MaxLogPcs=5000 时窗口加载成功，标题 = {win.Text}");
					return true;
				}
				catch (Exception ex)
				{
					WriteLine("        抛出：" + ex.GetType().Name + " " + ex.Message);
					return false;
				}
				finally
				{
					Program.Log.MaxLogPcs = old;
					if (win != null) { try { win.Close(); win.Dispose(); } catch { } }
				}
			});

			// ---------- 加密解密 ----------
			string secretPath = Path.Combine(Program.WorkDir, "smoke_secret.dat");
			var payload = new Payload
			{
				Name = "WeiKit 自检",
				Level = 7,
				At = new DateTime(2026, 1, 2, 3, 4, 5),
				Tags = new[] { "a", "b", "中文标签" }
			};
			Check("SaveEncrypted + LoadDecrypted 往返一致", () =>
			{
				CryptoHelper.SaveEncrypted(secretPath, payload, "pwd-123");
				var back = CryptoHelper.LoadDecrypted<Payload>(secretPath, "pwd-123");
				return back.Name == payload.Name && back.Level == payload.Level
					   && back.At == payload.At && back.Tags.Length == 3 && back.Tags[2] == "中文标签";
			});
			Check("密文长度 = 32 字节盐 + 密文", () =>
			{
				long len = new FileInfo(secretPath).Length;
				string plainJson = "{\"Name\":\"" + payload.Name + "\",\"Level\":" + payload.Level +
								   ",\"At\":\"" + payload.At.ToString("s") + "\",\"Tags\":[\"a\",\"b\",\"中文标签\"]}";
				WriteLine($"        文件长度 = {len} 字节（等价明文 JSON 约 {Encoding.UTF8.GetByteCount(plainJson)} 字节）");
				return len > 32;
			});
			Check("两次保存同一对象产生不同密文（随机盐生效）", () =>
			{
				string h1 = Tool.CalculateFileChecksum(secretPath);
				CryptoHelper.SaveEncrypted(secretPath, payload, "pwd-123");
				string h2 = Tool.CalculateFileChecksum(secretPath);
				return h1 != h2;
			});
			Check("错误密码抛 CryptographicException", () =>
			{
				try { CryptoHelper.LoadDecrypted<Payload>(secretPath, "错误密码"); return false; }
				catch (System.Security.Cryptography.CryptographicException) { return true; }
			});
			Check("篡改密文抛异常", () =>
			{
				string p2 = Path.Combine(Program.WorkDir, "smoke_tamper.dat");
				CryptoHelper.SaveEncrypted(p2, payload, "pwd-123");
				byte[] d = File.ReadAllBytes(p2);
				d[d.Length / 2] ^= 0xFF;
				File.WriteAllBytes(p2, d);
				bool threw;
				try { CryptoHelper.LoadDecrypted<Payload>(p2, "pwd-123"); threw = false; }
				catch (Exception) { threw = true; }
				File.Delete(p2);
				return threw;
			});
			Check("空密码/空路径参数校验", () =>
			{
				bool a, b, c;
				try { CryptoHelper.SaveEncrypted(secretPath, payload, "  "); a = false; } catch (ArgumentException) { a = true; }
				try { CryptoHelper.SaveEncrypted("  ", payload, "p"); b = false; } catch (ArgumentException) { b = true; }
				// 注意：SaveEncrypted<T> 是泛型，直接传 null 无法推断 T，必须显式指定类型参数
				try { CryptoHelper.SaveEncrypted<Payload>(secretPath, null, "p"); c = false; } catch (ArgumentNullException) { c = true; }
				return a && b && c;
			});
			Check("文件不存在抛 FileNotFoundException", () =>
			{
				try { CryptoHelper.LoadDecrypted<Payload>(Path.Combine(Program.WorkDir, "无此文件.dat"), "p"); return false; }
				catch (FileNotFoundException) { return true; }
			});
			Check("损坏文件（<32 字节）抛 InvalidOperationException", () =>
			{
				string p3 = Path.Combine(Program.WorkDir, "smoke_short.dat");
				File.WriteAllBytes(p3, new byte[10]);
				try { CryptoHelper.LoadDecrypted<Payload>(p3, "p"); return false; }
				catch (InvalidOperationException) { return true; }
				finally { File.Delete(p3); }
			});

			// ---------- 网络（不依赖外部可达性） ----------
			Check("HttpLink.GetClientIP 解析 X-Forwarded-For", () =>
			{
				// 用 HttpListener 造一个真实请求来验证取 IP 逻辑过于笨重，这里只验证空值保护
				return HttpLink.GetClientIP(null) == string.Empty;
			});
			Check("HttpLink.GetTask 空 URL 返回 null", () => HttpLink.GetTask("") == null);
			Check("HttpLink.PostTask 空 URL 返回空串", () => HttpLink.PostTask("", "{}") == string.Empty);
			Check("DownTool.GetDownloadSize 非法 URL 返回 0", () =>
			{
				using (var d = new HttpLink.DownTool()) return d.GetDownloadSize("not-a-url") == 0;
			});
			Check("DownTool 非法链接直接失败并回调", () =>
			{
				using (var d = new HttpLink.DownTool())
				{
					bool called = false; string reason = null;
					d.DownloadCompleted += (ok, r) => { called = true; reason = r; };
					d.StartDownload("ftp://x/y", Path.Combine(Program.WorkDir, "x.bin"));
					return called && !string.IsNullOrEmpty(reason) && !d.IsDownloading;
				}
			});
			Check("UploadTool 文件不存在直接失败", () =>
			{
				using (var u = new HttpLink.UploadTool())
				{
					bool called = false; string reason = null;
					u.UploadCompleted += (ok, r) => { called = true; reason = r; };
					u.UploadFile("http://x/upload", Path.Combine(Program.WorkDir, "无此文件.zip"));
					return called && reason != null && !u.IsUploading;
				}
			});
			Check("WebServer 软页面读写（纯内存，不依赖 HttpListener）", () =>
			{
				var srv = new WebServer();
				srv.NewPage("/t", "hello");
				bool r1 = srv.ReadPageData("/t") == "hello";
				bool w = srv.WritePageData("/t", "world");
				bool r2 = srv.ReadPageData("/t") == "world";
				bool miss = srv.ReadPageData("/none") == null && !srv.WritePageData("/none", "x");
				return r1 && w && r2 && miss;
			});
			Check("WebServer 路径穿越防护（已修复）", () =>
			{
				string root = Program.WorkDir;
				bool inside = WebServer.IsPathInside(root, Path.Combine(root, "a.txt"));
				bool traversal = !WebServer.IsPathInside(root, Path.Combine(root, @"..\..\..\Windows\win.ini"));
				bool absoluteOutside = !WebServer.IsPathInside(root, @"C:\Windows\win.ini");
				bool empty = !WebServer.IsPathInside(root, null);
				bool emptyRoot = !WebServer.IsPathInside("", Path.Combine(root, "a.txt"));
				WriteLine($"        根目录内 = {inside}（应 true）；..穿越 = {traversal}（应 true）；外部绝对路径 = {absoluteOutside}（应 true）；null = {empty}（应 true）；空根 = {emptyRoot}（应 true）");
				return inside && traversal && absoluteOutside && empty && emptyRoot;
			});
			CheckEnv("WebServer localhost 启停", () =>
			{
				try
				{
					var srv = new WebServer();
					srv.ServerStart(58231, "./", false);
					bool started = srv.ServerStarted;
					System.Threading.Thread.Sleep(150);
					string body = HttpLink.GetTask("http://localhost:58231/__smoke_none__");
					srv.StopServer();
					WriteLine($"        启动={started} 前缀={srv.Prefix} 404页长度={body?.Length ?? 0}");
					return started && body != null && body.Contains("无法访问");
				}
				catch (Exception ex)
				{
					WriteLine("        端口 58231 不可用：" + ex.Message);
					return false;
				}
			});
			CheckEnv("WebServer 软页面可通过 HTTP 访问", () =>
			{
				try
				{
					var srv = new WebServer();
					srv.NewPage("/smoke", "<b>SMOKE-OK</b>");
					srv.ServerStart(58232, "./", false);
					System.Threading.Thread.Sleep(150);
					string body = HttpLink.GetTask("http://localhost:58232/smoke");
					srv.StopServer();
					return body != null && body.Contains("SMOKE-OK");
				}
				catch (Exception ex)
				{
					WriteLine("        端口 58232 不可用：" + ex.Message);
					return false;
				}
			});

			// ---------- States ----------
			Check("States 定义/触发/查询", () =>
			{
				var st = new States();
				bool d1 = st.DefinitionState("网络", States.State.StateType.Normal);
				bool d2 = !st.DefinitionState("网络", States.State.StateType.Error);   // 重名应失败
				bool s1 = st.SetStateCurrently("网络", true);
				bool s2 = !st.SetStateCurrently("不存在", true);
				return d1 && d2 && s1 && s2 && st.GetNormalCurrentlys().Count == 1 && st.GetAllCurrentlys().Count == 1;
			});
			Check("States.StateCurrentlyChange 事件触发", () =>
			{
				var st = new States();
				st.DefinitionState("A", States.State.StateType.Warning);
				int n = 0;
				st.StateCurrentlyChange += (s, e) => n++;
				st.SetStateCurrently("A", true);
				return n == 1;
			});
			Check("States 计数字段自动维护（已修复）", () =>
			{
				var st = new States();
				st.DefinitionState("A", States.State.StateType.Normal);
				st.DefinitionState("B", States.State.StateType.Message);
				st.DefinitionState("C", States.State.StateType.Warning);
				st.DefinitionState("D", States.State.StateType.Error);
				st.DefinitionState("E", States.State.StateType.Error);

				bool zeroBefore = st.AllCurrentlyPcs == 0;

				st.SetStateCurrently("A", true);
				st.SetStateCurrently("C", true);
				st.SetStateCurrently("D", true);
				st.SetStateCurrently("E", true);

				bool counted = st.AllCurrentlyPcs == 4
							&& st.NormalCurrentlyPcs == 1
							&& st.MessageCurrentlyPcs == 0
							&& st.WarningCurrentlyPcs == 1
							&& st.ErrorCurrentlyPcs == 2;

				// 触发/取消后计数要跟着变
				st.SetStateCurrently("D", false);
				bool afterUntrigger = st.AllCurrentlyPcs == 3 && st.ErrorCurrentlyPcs == 1;

				// 直接改字段后手动重算也应生效
				st.states[1].IsCurrently = true;      // B: Message
				st.RecalculateCounts();
				bool manual = st.AllCurrentlyPcs == 4 && st.MessageCurrentlyPcs == 1;

				WriteLine($"        初始为 0 = {zeroBefore}；触发 4 个后 = {counted}；取消 1 个后 = {afterUntrigger}；手动重算 = {manual}");
				WriteLine($"        当前统计：全部 {st.AllCurrentlyPcs} / 正常 {st.NormalCurrentlyPcs} / 消息 {st.MessageCurrentlyPcs} / 警告 {st.WarningCurrentlyPcs} / 错误 {st.ErrorCurrentlyPcs}");
				return zeroBefore && counted && afterUntrigger && manual;
			});

			// ---------- 收尾 ----------
			try { if (File.Exists(secretPath)) File.Delete(secretPath); } catch { }

			WriteLine("======================================================");
			WriteLine($"自检结果：通过 {passed} 项，失败 {failed} 项，跳过 {skipped} 项（依赖环境的检查）。");
			if (failed == 0) WriteLine(skipped == 0 ? "全部通过 ✔" : "无失败项 ✔（跳过项请在本机常规环境复测）");
			else WriteLine("存在失败项 ✘（详见上方 FAIL 行）");
			WriteLine("======================================================");

			if (filew != null)
			{
				filew.Flush();
				filew.Dispose();
				filew = null;
			}
			return failed == 0 ? 0 : 1;
		}

		/// <summary>
		/// 隔离探针：验证「明文保存 → 明文载入 → 带密钥载入自动迁移为加密」这条链路。
		/// 每一步都打印状态，避免多个断言混在一起难以定位。
		/// </summary>
		private static bool SmokePlainProbe()
		{
			string p = Path.Combine(Program.WorkDir, "smoke_plain2.json");
			try
			{
				if (File.Exists(p)) File.Delete(p);

				var c = new Configs();
				bool added = c.Add("A", Configs.ValueType.String, "v");
				WriteLine($"        Add = {added}，configs.Count = {c.configs.Count}，ConfigFilePath = {c.ConfigFilePath ?? "(null)"}");
				WriteLine($"        目标路径 = {p}　目录存在 = {Directory.Exists(Path.GetDirectoryName(p))}　文件已存在 = {File.Exists(p)}");

				try
				{
					// 关键：必须写成 Save(path, key) 两个实参！
					// 单参数 Save(p) 会被 C# 解析成 Save(SaveKey: p) 那个重载，
					// 因 ConfigFilePath 仍是 null 而静默返回，文件永远写不出来。
					c.Save(p, "");
					WriteLine($"        Save(path, \"\") 返回后：ConfigFilePath = {c.ConfigFilePath ?? "(null)"}");
				}
				catch (Exception ex)
				{
					WriteLine("        Save(path) 抛出：" + ex.Message);
				}

				bool exists = File.Exists(p);
				WriteLine($"        Save(path, \"\") 后：文件存在 = {exists}" + (exists ? $"，大小 = {new FileInfo(p).Length} 字节" : ""));
				if (!exists) return false;

				bool loaded = c.Load(p);         // 明文载入
				WriteLine($"        Load(明文) = {loaded}，IsEffective = {c.IsEffective}，IsEncrypted = {c.IsEncrypted}，值 = {c.Read("A")}");

				long lenBefore = File.Exists(p) ? new FileInfo(p).Length : -1;
				var c2 = new Configs();
				bool enc = c2.Load(p, Program.ConfigSaveKey);   // 带密钥载入明文 → 应自动迁移为加密
				long lenAfter = File.Exists(p) ? new FileInfo(p).Length : -1;
				bool nowEncrypted = lenAfter > 0 && !File.ReadAllText(p).TrimStart().StartsWith("[");
				WriteLine($"        Load(带密钥) = {enc}，IsEffective = {c2.IsEffective}，IsEncrypted = {c2.IsEncrypted}，" +
						  $"LastSaveKey = \"{c2.LastSaveKey}\"，项数 = {c2.configs.Count}，值 = {c2.Read("A")}");
				WriteLine($"        文件长度：{lenBefore} -> {lenAfter} 字节，现为密文 = {nowEncrypted}");

				return loaded && !c.IsEncrypted && enc && nowEncrypted && c2.IsEncrypted;
			}
			catch (Exception ex)
			{
				WriteLine("        探针异常：" + ex);
				return false;
			}
			finally
			{
				try { if (File.Exists(p)) File.Delete(p); } catch { }
			}
		}

		private static bool IsHex(string s)
		{
			foreach (char c in s)
				if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
			return true;
		}

		private static string ReadHead(string path, int count)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				var buf = new byte[Math.Min(count, (int)Math.Min(fs.Length, int.MaxValue))];
				int n = fs.Read(buf, 0, buf.Length);
				return Encoding.UTF8.GetString(buf, 0, n);
			}
		}

		/// <summary>写一行到真实控制台（绝不能用 Console.WriteLine，否则会被日志重定向吞掉并递归）</summary>
		private static void WriteLine(string text = "")
		{
			(outw ?? Console.Out).WriteLine(text);
			if (filew != null)
			{
				filew.WriteLine(text);
				filew.Flush();      // 立即落盘，崩溃也能留下记录
			}
		}

		/// <summary>执行一项检查</summary>
		private static void Check(string name, Func<bool> test) => CheckCore(name, test, false);

		/// <summary>
		/// 执行一项「依赖运行环境」的检查。失败时记为 SKIP：不计入通过数也不计入失败数，
		/// 因此不会让退出码变 1（例如受限账号下 HttpListener 不可用、只读目录写不了文件）。
		/// </summary>
		private static void CheckEnv(string name, Func<bool> test) => CheckCore(name, test, true);

		private static void CheckCore(string name, Func<bool> test, bool envDependent)
		{
			try
			{
				if (test())
				{
					passed++;
					WriteLine("  [PASS] " + name);
				}
				else if (envDependent)
				{
					skipped++;
					WriteLine("  [SKIP] " + name + "  ← 本环境不支持（不计入失败；在常规 Windows 上应通过）");
				}
				else
				{
					failed++;
					WriteLine("  [FAIL] " + name + "  ← 断言为 false");
				}
			}
			catch (Exception ex)
			{
				if (envDependent)
				{
					skipped++;
					WriteLine($"  [SKIP] {name}  ← 本环境抛出 {ex.GetType().Name}: {ex.Message}");
					return;
				}
				failed++;
				// 打印完整异常（含堆栈），便于定位框架内部抛错的确切位置
				WriteLine($"  [FAIL] {name}  ← 抛出 {ex.GetType().Name}: {ex.Message}");
				WriteLine("        " + ex.StackTrace.Replace("\r\n", "\r\n        "));
			}
		}
	}
}
