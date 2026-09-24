using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using WeiKit.Window;

namespace WeiKit
{
	public class Logs
	{

		/// <summary>
		/// 日志信息窗口实例
		/// </summary>
		private Logsinfo logsinfoform;
		/// <summary>
		/// 日志信息窗口句柄
		/// </summary>
		public IntPtr LogsFormHandle
		{
			get
			{
				return logsinfoform != null && !logsinfoform.IsDisposed ? logsinfoform.Handle : IntPtr.Zero;
			}
		}
		/// <summary>
		/// 显示日志信息窗口
		/// </summary>
		/// <param name="autoColorMode">自动识别日志关键字并着色</param>
		/// <returns>返回日志窗口的句柄</returns>
		public IntPtr ShowLogsForm(bool autoColorMode = true)
		{
			if (logsinfoform == null || logsinfoform.IsDisposed)
			{
				logsinfoform = new Logsinfo();
				logsinfoform.logs = this;
				logsinfoform.AutoColorMode = autoColorMode;
				logsinfoform.Show();
			}
			else
			{
				logsinfoform.BringToFront();
			}
			return logsinfoform.Handle;
		}

		/// <summary>
		/// 日志类型
		/// </summary>
		public class Log
		{
			/// <summary>
			/// 日志发生的时间
			/// </summary>
			public DateTime Time;
			/// <summary>
			/// 日志类型标识
			/// </summary>
			public string TypeText;
			/// <summary>
			/// 日志正文文本
			/// </summary>
			public string Text;
			/// <summary>
			/// 文本转换函数
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return $"<{Time.ToString("T")} | {TypeText}>{Text}";
			}
		}
		/// <summary>
		/// 日志最大记录条例数（0表示无限制）
		/// </summary>
		public int MaxLogPcs = 0;
		/// <summary>
		/// 达到MaxLogPcs后保存日志的路径。
		/// 默认指向程序目录下的 <c>logs\weikit_autosave.log</c>；写入前会自动创建所在目录。
		/// </summary>
		public string AutoSavePath = Path.Combine(Tool.GetProgramPath(), "logs", "weikit_autosave.log");
		/// <summary>
		/// 存储日志列表数据的动态数组
		/// </summary>
		public List<Log> logs { get; private set; } = new List<Log>();
		/// <summary>
		/// 打印一条日志时触发的事件
		/// </summary>
		public event EventHandler<Log> LogAddEvent;
		/// <summary>
		/// 手动保存日志
		/// </summary>
		/// <param name="path">保存路径（所在目录不存在时会自动创建）</param>
		/// <returns>是否保存成功</returns>
		public bool LogsSave(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				Console.WriteLine("保存日志失败：未指定文件路径。");
				return false;
			}
			try
			{
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				File.WriteAllText(path, ToString());
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine($"保存日志失败->{path}：{e.Message}");
				return false;
			}
		}
		/// <summary>
		/// 获取日志数据
		/// </summary>
		/// <returns></returns>
		public List<Log> GetLogList()
		{
			return logs;
		}
		/// <summary>
		/// 打印一条日志
		/// </summary>
		/// <param name="text">日志正文</param>
		/// <param name="typetext">类型文本，默认 "Info"</param>
		/// <remarks>
		/// 内置重入保护：若某条日志触发的 <see cref="LogAddEvent"/> 回调里又写 Console，
		/// 递归产生的日志会被直接丢弃，避免「写日志→触发事件→写控制台→又写日志」的无限递归。
		/// </remarks>
		public void Println(string text, string typetext = "Info")
		{
			if (_handlingLog) return;
			_handlingLog = true;
			try
			{
				PrintlnCore(text, typetext);
			}
			finally
			{
				_handlingLog = false;
			}
		}

		/// <summary>重入保护标志。递归调用（事件回调里再打印）会被 <see cref="Println"/> 丢弃。</summary>
		[ThreadStatic]
		private static bool _handlingLog;

		private void PrintlnCore(string text, string typetext)
		{
			//数量限制，自动保存功能
			if (MaxLogPcs > 0 && logs.Count >= MaxLogPcs)
			{
				if (LogsSave(Tool.PathConversion(AutoSavePath)))
				{
					logs.Clear();
					logs.Add(new Log { Time = DateTime.Now, Text = $"写出日志：{AutoSavePath}", TypeText = "AutoSave" });
				}
				else
				{
					// 落盘失败就保留日志，避免「写不出去还把内容清空」造成日志丢失
					logs.Add(new Log
					{
						Time = DateTime.Now,
						Text = $"自动保存失败，日志继续保留在内存中（上限 {MaxLogPcs} 条）",
						TypeText = "AutoSave"
					});
				}
			}
			logs.Add(new Log { Time = DateTime.Now, Text = text, TypeText = typetext });
			// 当日志被添加时触发事件
			try
			{
				LogAddEvent?.Invoke(this, logs[logs.Count - 1]);
			}
			catch (Exception ex)
			{
				//不做任何事
				//MessageBox.Show($"打印日志时遇到错误：{ex.Message}");
			}
		}
		/// <summary>
		/// 获取最新的日志信息
		/// </summary>
		/// <returns>最后一条日志；列表为空时返回 <c>null</c>（不再抛异常）</returns>
		public Log GetLastLog()
		{
			return logs.Count > 0 ? logs[logs.Count - 1] : null;
		}
		/// <summary>
		/// 获取最新的日志信息，String格式，没有时间和类型
		/// </summary>
		/// <returns>最后一条日志的正文；列表为空时返回 <see cref="string.Empty"/></returns>
		public string GetLastLogText()
		{
			return logs.Count > 0 ? logs[logs.Count - 1].Text : string.Empty;
		}
		/// <summary>
		/// 文本转换函数
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string str = string.Empty;
			foreach (var s in logs)
			{
				str = str + s.ToString() + '\r' + '\n';
			}
			return str;
		}

		/// <summary>
		/// 系统控制台输出重定向到日志列表（可逆）。
		/// </summary>
		/// <returns>重定向之前的原始 <see cref="TextWriter"/>，可传给 <see cref="RestoreConsoleOut"/> 恢复</returns>
		/// <remarks>
		/// 注意：重定向后 <c>Console.WriteLine</c> 会进入本日志实例。若在 <see cref="LogAddEvent"/>
		/// 回调里再次调用 <c>Console.WriteLine</c>，会形成「写日志→触发事件→写控制台→又写日志」的
		/// 无限递归。为此 <see cref="CustomTextWriter"/> 内置了重入保护：递归调用会被丢弃并打印一行警告。
		/// </remarks>
		public TextWriter RedirectConsoleOut()
		{
			TextWriter original = Console.Out;
			Console.SetOut(new CustomTextWriter(this));
			//显示库的关于信息
			Console.WriteLine($"WeiKit Framework | Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
			Console.WriteLine($"© Tiangong Technology Team & WeiGuangXin");
			return original;
		}

		/// <summary>
		/// 恢复 <see cref="Console"/> 的标准输出。
		/// </summary>
		/// <param name="original">
		/// <see cref="RedirectConsoleOut"/> 的返回值。传 <c>null</c> 时尝试恢复为
		/// <see cref="Console.Out"/> 的默认流（新建的 <see cref="StreamWriter"/>，写入标准输出）。
		/// </param>
		public static void RestoreConsoleOut(TextWriter original = null)
		{
			Console.SetOut(original ?? new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
		}

		class CustomTextWriter : TextWriter
		{
			private Logs logs;
			/// <summary>重入保护：为 true 时说明当前正在处理一条日志，递归调用必须丢弃</summary>
			[ThreadStatic]
			private static bool _reentered;

			public CustomTextWriter(Logs logs)
			{
				this.logs = logs;
			}
			public override void Write(char value)
			{
				MyCustomOutputFunction(value.ToString());
			}
			public override void Write(string value)
			{
				MyCustomOutputFunction(value);
			}
			public override void WriteLine(string value)
			{
				MyCustomOutputFunction(value);
			}
			public override Encoding Encoding => Encoding.UTF8;
			private void MyCustomOutputFunction(string output)
			{
				// 防止 LogAddEvent 回调里再写 Console 导致无限递归（栈溢出）
				if (_reentered) return;

				_reentered = true;
				try
				{
					logs.Println(output, "Console");
				}
				finally
				{
					_reentered = false;
				}
			}
		}
	}
}
