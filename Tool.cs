using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;


namespace WeiKit
{
	/// <summary>
	/// 工具集合
	/// </summary>
	public static class Tool
	{

		[System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
		public static extern System.IntPtr CreateRoundRectRgn(
			int nLeftRect,
			int nTopRect,
			int nRightRect,
			int nBottomRect,
			int nWidthEllipse,
			int nHeightEllipse
		);
		/// <summary>
		/// 设置开机自启动
		/// </summary>
		/// <param name="appName"></param>
		/// <param name="appPath"></param>
		/// <param name="isAutoStart"></param>
		public static void SetAutoStart(string appName, string appPath, bool isAutoStart = true)
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

			if (isAutoStart)
			{
				// 添加启动项
				registryKey.SetValue(appName, $"\"{appPath}\"");
			}
			else
			{
				// 删除启动项
				if (registryKey.GetValue(appName) != null)
				{
					registryKey.DeleteValue(appName);
				}
			}
			registryKey.Close();
		}

		/// <summary>
		/// 检查是否已设置自启动
		/// </summary>
		/// <param name="appName"></param>
		/// <returns></returns>
		public static bool IsAutoStartEnabled(string appName)
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);

			bool exists = registryKey?.GetValue(appName) != null;
			registryKey?.Close();
			return exists;
		}

		/// <summary>
		/// 获取程序根目录。
		/// </summary>
		/// <returns></returns>
		public static string GetProgramPath()
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}
		///// <summary>
		///// 获取指定长度的随机字符串。
		///// </summary>
		///// <param name="length">要获取字符串的长度，默认为8</param>
		///// <param name="AddedChar">额外的字符数据</param>
		///// <returns>随机字符串</returns>
		//public static string GetRandomString(int length = 8, string AddedChar = "")
		//{
		//	Random random = new Random();
		//	string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" + AddedChar;
		//	return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
		//}

		/// <summary>
		/// 获取指定长度的随机字符串
		/// </summary>
		/// <param name="length">要获取字符串的长度，默认为8</param>
		/// <param name="AddedChar">额外的字符数据</param>
		/// <returns>随机字符串</returns>
		public static string GetRandomString(int length = 8, string AddedChar = "")
		{
			string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" + AddedChar;
			int charCount = chars.Length;
			byte[] data = new byte[length * 4]; // 多取一些字节以减少重试概率
			char[] result = new char[length];
			using (var rng = RandomNumberGenerator.Create())
			{
				int index = 0;
				while (index < length)
				{
					rng.GetBytes(data);
					for (int i = 0; i < data.Length && index < length; i += 4)
					{
						// 使用 32 位整数生成一个安全的随机索引
						uint rand = BitConverter.ToUInt32(data, i);
						// 拒绝大于 charCount 的最大倍数的值，保证均匀分布
						long max = (long)charCount * (uint.MaxValue / charCount);
						if (rand < max)
						{
							// 修改此行，显式将 rand % charCount 转换为 int 类型
							result[index++] = chars[(int)(rand % charCount)];
						}
					}
				}
			}
			return new string(result);
		}

		/// <summary>
		/// 通过系统文件资源管理器打开某个文件或目录。
		/// </summary>
		/// <param name="Path">文件路径</param>
		/// <returns>是否成功执行</returns>
		public static bool OpenTypeFile(string Path)
		{
			Process re = Process.Start("explorer.exe", Path);
			if (re != null)
				return true;
			else
				return false;
		}
		/// <summary>
		/// 区间等比映射。
		/// </summary>
		/// <param name="value">要映射的值</param>
		/// <param name="xMin">源区间最小值</param>
		/// <param name="xMax">源区间最大值</param>
		/// <param name="yMin">目标区间最小值</param>
		/// <param name="yMax">目标区间最大值</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static double MapValue(double value, double xMin, double xMax, double yMin, double yMax)
		{
			if (xMax == xMin)
				return 0;
			return ((value - xMin) / (xMax - xMin)) * (yMax - yMin) + yMin;
		}
		/// <summary>
		/// 运行Cmd代码。
		/// </summary>
		/// <param name="Codes">存储需要执行命令的数组</param>
		/// <returns>返回控制台输出</returns>
		public static string RunCmdCode(string[] Codes)
		{
			return RunCmdCore(Codes ?? new string[0]);
		}

		public static string RunCmdCode(string Code)
		{
			return RunCmdCore(new[] { Code ?? string.Empty });
		}

		/// <summary>
		/// 运行 cmd 命令的实际实现：同时捕获 stdout 与 stderr，避免错误信息丢失。
		/// 注意：本方法是同步阻塞的，不要在 UI 线程直接调用长耗时命令。
		/// </summary>
		private static string RunCmdCore(string[] codes)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,   // 旧版没有重定向 stderr，错误信息会丢失
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (Process process = new Process())
			{
				process.StartInfo = startInfo;
				process.Start();

				// 异步读取 stderr，避免大量错误输出把管道写满导致双方互相等待（死锁）
				StringBuilder errorText = new StringBuilder();
				process.ErrorDataReceived += (s, e) =>
				{
					if (e.Data != null) errorText.AppendLine(e.Data);
				};
				process.BeginErrorReadLine();

				using (StreamWriter sw = process.StandardInput)
				{
					if (sw.BaseStream.CanWrite)
					{
						foreach (var s in codes)
							sw.WriteLine(s);
						sw.WriteLine("exit");  // 执行完命令后退出 CMD
					}
				}

				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				string err = errorText.ToString();
				if (err.Length > 0)
					output = output + (output.Length > 0 && !output.EndsWith("\n") ? "\r\n" : "") + err;
				return output;
			}
		}
		/// <summary>
		/// 运行外部程序。
		/// </summary>
		/// <param name="Path">程序路径</param>
		/// <param name="Parameter">附加参数（可选）</param>
		/// <returns>是否成功执行</returns>
		public static bool RunExternalProg(string Path, string Parameter = "")
		{
			Process re = Process.Start(Path, Parameter);
			if (re != null)
				return true;
			else
				return false;
		}
		/// <summary>
		/// 在指定目录中创建一个绝对不会与现存目录重名的目录。
		/// </summary>
		/// <param name="path">指定要创建目录的目录</param>
		/// <param name="prefix">目录前缀</param>
		/// <returns>新目录的路径</returns>
		/// <summary>
		/// 在指定目录中创建一个绝对不会与现存目录重名的目录。
		/// </summary>
		/// <param name="path">指定要创建目录的目录；默认程序根目录。</param>
		/// <param name="prefix">目录前缀</param>
		/// <returns>新目录的完整路径</returns>
		public static string AbsNewFolder(string path = null, string prefix = "")
		{
			// 旧版默认值是 "./"，会按「当前工作目录」解析而不是程序目录，容易建错地方。
			// 现在默认使用程序根目录，语义明确。
			if (string.IsNullOrWhiteSpace(path))
				path = GetProgramPath();

			string newDirName;
			do
			{
				string randomString = Path.GetRandomFileName().Replace(".", "").Substring(0, 6);
				newDirName = Path.Combine(path, prefix + randomString);
			} while (Directory.Exists(newDirName)); // 如果文件夹已存在，则重新生成
			Directory.CreateDirectory(newDirName);
			return newDirName;
		}
		/// <summary>
		/// 绝对相对路径转换
		/// </summary>
		/// <param name="path">绝对或相对路径</param>
		/// <returns>转换后的绝对或相对路径</returns>
		public static string PathConversion(string path)
		{
			string str = path;
			if (str.Contains(@".\"))
				return str.Replace(@".\", GetProgramPath());
			if (str.Contains(@"./"))
				return str.Replace(@"./", GetProgramPath());
			string ProgramPath = GetProgramPath();
			if (str.Contains(ProgramPath))
				return str.Replace(ProgramPath, @".\"); ;
			return path;
		}
		/// <summary>
		/// 绝对相对链接转换：把 <c>.\</c> 或 <c>./</c> 前缀替换为域名，反向则把域名替换回 <c>./</c>。
		/// </summary>
		/// <param name="link">绝对或相对链接</param>
		/// <param name="domainname">站点域名，例如 <c>https://example.com/</c></param>
		/// <returns>转换后的链接</returns>
		public static string LinkConversion(string link, string domainname)
		{
			if (string.IsNullOrEmpty(link)) return link;
			if (string.IsNullOrEmpty(domainname)) return link;

			// 相对 → 绝对：同时支持反斜杠与正斜杠写法
			if (link.Contains(@".\"))
				return link.Replace(@".\", domainname);
			if (link.Contains("./"))
				return link.Replace("./", domainname);

			// 绝对 → 相对
			string normalizedDomain = domainname;
			if (link.StartsWith(domainname, StringComparison.OrdinalIgnoreCase))
				return "./" + link.Substring(domainname.Length).TrimStart('/');
			if (!string.IsNullOrEmpty(normalizedDomain) && link.Contains(normalizedDomain))
				return link.Replace(normalizedDomain, "./");
			return link;
		}

		/// <summary>
		/// 通用的窗口控件委托更新方法（<b>同步</b>）。
		/// 使用方法：
		/// label1.SafeInvoke(() =&gt; label1.Text = newText);
		/// </summary>
		/// <param name="control">目标控件；为 null 或已释放时直接忽略</param>
		/// <param name="action">要在 UI 线程执行的操作</param>
		/// <remarks>
		/// 本方法内部使用阻塞式的 <see cref="Control.Invoke(Delegate)"/>。
		/// 如果在「持有锁」的情况下由后台线程调用，且 UI 线程正在等待同一把锁，就会死锁。
		/// 这种场景请改用 <see cref="SafeBeginInvoke"/>（异步、不阻塞调用方）。
		/// </remarks>
		public static void SafeInvoke(this Control control, Action action)
		{
			if (control == null || action == null || control.IsDisposed) return;

			if (control.InvokeRequired)
			{
				control.Invoke(action);
			}
			else
			{
				action();
			}
		}

		/// <summary>
		/// 通用的窗口控件委托更新方法（<b>异步</b>，不阻塞调用线程）。
		/// 适合在后台线程、尤其是不确定 UI 线程是否空闲时使用，可避免同步 Invoke 引起的死锁。
		/// </summary>
		/// <param name="control">目标控件；为 null 或已释放时直接忽略</param>
		/// <param name="action">要在 UI 线程执行的操作</param>
		public static void SafeBeginInvoke(this Control control, Action action)
		{
			if (control == null || action == null || control.IsDisposed) return;

			if (control.InvokeRequired)
			{
				try
				{
					control.BeginInvoke(action);
				}
				catch (ObjectDisposedException) { /* 控件在投递后瞬间被释放，忽略 */ }
				catch (InvalidOperationException) { /* 句柄未创建或正在销毁，忽略 */ }
			}
			else
			{
				action();
			}
		}

		/// <summary>
		/// 获取本机Ip地址列表
		/// </summary>
		/// <returns></returns>
		public static string[] GetLocalIPv4List()
		{
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			string[] strings = new string[host.AddressList.Length];
			List<string> strs = new List<string>();
			//过滤出IPv4的地址
			foreach (IPAddress ip in host.AddressList)
				if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					strs.Add(ip.ToString());
			return strs.ToArray();
		}
		/// <summary>
		/// 使一个控件从原来的位置平滑移动至指定位置。
		/// </summary>
		/// <param name="control">目标控件</param>
		/// <param name="newpoint">指定位置</param>
		/// <param name="framenum">移动帧数量</param>
		/// <param name="frametimer">播放所有帧用时（ms）</param>
		public static void ControlMove(this Control control, Point newpoint, int framenum = 20, int frametimer = 200)
		{
			if (control == null || control.IsDisposed || !control.Visible) return;
			Point startPoint = control.Location; // 获取控件当前起点位置
			Point targetPoint = newpoint; // 目标位置
			int totalSteps = Math.Max(framenum, 1); // 确保帧数至少为1
			double stepX = (targetPoint.X - startPoint.X) / totalSteps; // 每帧在X轴上的增量
			double stepY = (targetPoint.Y - startPoint.Y) / totalSteps; // 每帧在Y轴上的增量
			Timer timer = new Timer(); // 创建一个计时器
			int currentStep = 0;
			// 间隔至少 1ms，避免 frametimer 很小时 Interval 变成 0（0 会被当作 1ms，但语义不清）
			timer.Interval = Math.Max(1, frametimer / totalSteps);
			timer.Tick += (sender, args) =>
			{
				// 控件已释放就立刻停止并释放定时器，避免访问已释放对象抛异常 + 定时器泄漏
				if (control.IsDisposed)
				{
					timer.Stop();
					timer.Dispose();
					return;
				}

				currentStep++;
				if (currentStep > totalSteps)
				{
					control.Location = targetPoint; // 最后一步直接设置为目标位置
					timer.Stop();
					timer.Dispose();
				}
				else
				{
					// 计算当前位置
					int newX = (int)(startPoint.X + stepX * currentStep);
					int newY = (int)(startPoint.Y + stepY * currentStep);
					control.Location = new Point(newX, newY);
				}
			};
			// 控件销毁时一并释放定时器，避免动画中断后定时器一直活着
			control.Disposed += (sender, args) =>
			{
				timer.Stop();
				timer.Dispose();
			};
			timer.Start(); // 启动计时器
		}

		//控件圆角支持
		[DllImport("GDI32.dll")]
		private static extern IntPtr CreateRoundRectRgn(IntPtr x1, IntPtr y1, IntPtr x2, IntPtr y2, IntPtr w, IntPtr h);
		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
		[DllImport("GDI32.dll")]
		private static extern IntPtr DeleteObject(IntPtr hObject);
		/// <summary>
		/// 为一个控件设置圆角区域。
		/// </summary>
		/// <param name="control">目标控件</param>
		/// <param name="radius">圆角半径（会翻倍作为椭圆直径，并自动限制不超过控件宽高）</param>
		public static void SetControlFillet(Control control, int radius)
		{
			if (control == null || control.IsDisposed) return;
			if (!control.IsHandleCreated) return;      // 句柄还没建立时设置区域没有意义
			if (control.Width <= 0 || control.Height <= 0) return;

			//半径转换为直径
			radius = radius * 2;
			//大小限制
			if (radius > control.Width)
				radius = control.Width;
			if (radius > control.Height)
				radius = control.Height;
			if (radius < 0) radius = 0;

			//调用API绘制
			IntPtr s = CreateRoundRectRgn((IntPtr)0, (IntPtr)0, (IntPtr)control.Width + 1, (IntPtr)control.Height + 1, (IntPtr)radius, (IntPtr)radius);
			if (s == IntPtr.Zero) return;

			// SetWindowRgn 成功后系统接管该区域的所有权，不能再 DeleteObject（否则句柄泄漏/二次释放）；
			// 只有在设置失败时才需要自己释放。
			if (SetWindowRgn(control.Handle, s, true) == IntPtr.Zero)
				DeleteObject(s);
		}

		public class DataFlowList
		{
			/// <summary>
			/// 数据流列表最大允许长度
			/// </summary>
			private int maxLeng = 60;
			/// <summary>
			/// 数据流列表数据
			/// </summary>
			private List<object> lists = new List<object>();

			/// <summary>
			/// 设置数据流列表最大允许长度
			/// </summary>
			/// <param name="leng"></param>
			/// <exception cref="Exception"></exception>
			public void SetFlowMaxLeng(int leng)
			{
				if (leng <= 0)
					throw new Exception("参数“leng”不接受小于或等于0的值。");
				//设置长度变量
				maxLeng = leng;
				//删除原来可能多出目标长度的旧数据
				if (lists.Count > maxLeng)
					lists.RemoveRange(0, lists.Count - maxLeng);
			}

			/// <summary>
			/// 向数据流中添加数据
			/// </summary>
			/// <param name="data">要添加的数据</param>
			public void FlowAdd(object data)
			{
				if (lists.Count == maxLeng)
					lists.RemoveAt(0);

				lists.Add(data);
			}
			/// <summary>
			/// 清除数据流列表中的所有数据
			/// </summary>
			public void FlowClear()
			{
				lists = new List<object>();
			}
			/// <summary>
			/// 获取数组数据，数据长度为0将会返回null。
			/// </summary>
			/// <returns></returns>
			public object[] GetDataArray()
			{
				if (lists.Count == 0)
					return null;
				return lists.ToArray();
			}
			/// <summary>
			/// 把元素统一转换为 float。任何 <see cref="IConvertible"/>（int/long/float/double/decimal/byte…）都可以。
			/// </summary>
			/// <exception cref="InvalidCastException">元素不是可转换为数值的类型时抛出（字符串也算）</exception>
			private float[] ToFloats()
			{
				var result = new float[lists.Count];
				for (int i = 0; i < lists.Count; i++)
				{
					object item = lists[i];
					if (!(item is IConvertible) || item is string || item is char || item is bool || item is DateTime)
						throw new InvalidCastException(
							$"第 {i} 个元素（{item?.GetType().Name ?? "null"}）不是可转换的数值类型。");
					try
					{
						result[i] = Convert.ToSingle(item, System.Globalization.CultureInfo.InvariantCulture);
					}
					catch (Exception e)
					{
						throw new InvalidCastException($"第 {i} 个元素（{item.GetType().Name}）无法转换为数值：{e.Message}");
					}
				}
				return result;
			}

			/// <summary>
			/// 把元素统一转换为 int。
			/// </summary>
			/// <exception cref="InvalidCastException">元素不是可转换为数值的类型时抛出（字符串也算）</exception>
			private int[] ToInts()
			{
				var result = new int[lists.Count];
				for (int i = 0; i < lists.Count; i++)
				{
					object item = lists[i];
					if (!(item is IConvertible) || item is string || item is char || item is bool || item is DateTime)
						throw new InvalidCastException(
							$"第 {i} 个元素（{item?.GetType().Name ?? "null"}）不是可转换的数值类型。");
					try
					{
						result[i] = Convert.ToInt32(item, System.Globalization.CultureInfo.InvariantCulture);
					}
					catch (Exception e)
					{
						throw new InvalidCastException($"第 {i} 个元素（{item.GetType().Name}）无法转换为整数：{e.Message}");
					}
				}
				return result;
			}

			/// <summary>
			/// 获取数据平均值（float）。列表为空时返回 0。
			/// </summary>
			/// <returns></returns>
			public float GetAverageValueF()
			{
				if (lists == null || lists.Count == 0)
					return 0.0f;
				return ToFloats().Average();
			}
			/// <summary>
			/// 获取数据平均值（int，四舍五入）。
			/// </summary>
			/// <returns></returns>
			public int GetAverageValue()
			{
				if (lists == null || lists.Count == 0)
					throw new InvalidOperationException("列表为空，无法计算平均值。");
				return (int)Math.Round(ToFloats().Average());
			}

			/// <summary>
			/// 获取数据最大值（float）。
			/// </summary>
			/// <returns></returns>
			public float GetMaxValueF()
			{
				if (lists == null || lists.Count == 0)
					throw new InvalidOperationException("列表为空，无法获取最大值。");
				return ToFloats().Max();
			}
			/// <summary>
			/// 获取数据最大值（int）。
			/// </summary>
			/// <returns></returns>
			public int GetMaxValue()
			{
				if (lists == null || lists.Count == 0)
					throw new InvalidOperationException("列表为空，无法获取最大值。");
				return ToInts().Max();
			}

			/// <summary>
			/// 获取数据最小值（float）。
			/// </summary>
			/// <returns></returns>
			public float GetMinValueF()
			{
				if (lists == null || lists.Count == 0)
					throw new InvalidOperationException("列表为空，无法获取最小值。");
				return ToFloats().Min();
			}

			/// <summary>
			/// 获取数据最小值（int）。
			/// </summary>
			/// <returns></returns>
			public int GetMinValue()
			{
				if (lists == null || lists.Count == 0)
					throw new InvalidOperationException("列表为空，无法获取最小值。");
				return ToInts().Min();
			}
		}
		/// <summary>
		/// 计算文件的SHA256校验值
		/// </summary>
		/// <param name="filePath">文件路径</param>
		/// <returns>SHA256校验值</returns>
		public static string CalculateFileChecksum(string filePath)
		{
			using (var sha256 = SHA256.Create())
			{
				using (var stream = File.OpenRead(filePath))
				{
					byte[] hashBytes = sha256.ComputeHash(stream);
					StringBuilder sb = new StringBuilder();
					foreach (byte b in hashBytes)
					{
						sb.Append(b.ToString("x2"));
					}
					return sb.ToString();
				}
			}
		}

		/// <summary>
		/// 判断指定控件是否在窗体可视区域内（考虑滚动和父容器裁剪）
		/// </summary>
		public static bool IsControlVisibleOnForm(Control control)
		{
			if (control == null || !control.Visible) return false;

			Form form = control.FindForm();
			if (form == null) return false;

			// 获取控件相对于屏幕的位置
			Rectangle screenRect = control.RectangleToScreen(control.ClientRectangle);

			// 获取窗体客户区在屏幕上的位置
			Rectangle formClientArea = form.RectangleToScreen(form.ClientRectangle);

			// 检查控件是否与窗体客户区有交集
			return screenRect.IntersectsWith(formClientArea);
		}

		/// <summary>
		/// 计算 SHA256 哈希并返回十六进制字符串
		/// </summary>
		/// <param name="rawData"></param>
		/// <returns></returns>
		public static string ComputeSha256Hash(string rawData)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
				string storedHash = BitConverter.ToString(bytes).Replace("-", "");
				return storedHash.ToLower();
			}
		}

	}
}
