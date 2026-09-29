using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeiKit.Window
{
	public partial class Logsinfo : Form
	{
		/// <summary>
		/// 自动识别日志关键字并着色
		/// </summary>
		public bool AutoColorMode = true;
		/// <summary>
		/// WeiKit日志对象
		/// </summary>
		public Logs logs;
		/// <summary>
		/// 控制台日志主题名（对应主题目录中不含扩展名的文件名）。
		/// 默认从持久化存储恢复，无记录时为 LightClean。
		/// </summary>
		public string ThemeName { get; set; } = ConsoleLogTheme.LoadCurrentTheme();

		// 敏感信息屏蔽相关字段
		private bool _sensitiveInfoEnabled = false;
		private readonly string[] _sensitivePatterns = new string[]
		{
			// API密钥（各种格式）
			@"key=[A-Za-z0-9]+",
			@"api[_-]?key[=\s:]+[A-Za-z0-9]+",
			@"token[=\s:]+[A-Za-z0-9]+",
			@"secret[=\s:]+[A-Za-z0-9]+",
			@"password[=\s:]+[A-Za-z0-9]+",
			@"pw=[A-Za-z0-9]+",
			
			// URL中的敏感参数
			@"\?[^""\s]*?(key|token|secret|password|pw)=[^&\s""]+",
			
			// 手机号（中国）
			@"1[3-9]\d{9}",
			
			// 邮箱
			@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
			
			// IP地址
			@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
			
			// 身份证号
			@"\d{17}[\dXx]",
			
			// 银行卡号
			@"\d{16,19}",
			
			// 文件路径（Windows路径）
			@"[A-Za-z]:\\[^<>:""]*",
			
			// 用户目录路径
			@"C:\\Users\\[^\\]+",
			
			// 程序路径（可能包含用户名）
			@"\\[^\\]+\\AppData\\",
			@"\\[^\\]+\\Documents\\",
			
			// 域名中的随机子域名
			@"[a-z0-9]{10,}\.[a-z]+\.[a-z]+",
			
			// 长随机字符串（可能是session或ID）
			@"[A-Za-z0-9]{20,}"
		};

		// 敏感信息替换字符
		private const string SensitiveReplacement = "#InfoBlock#";

		public Logsinfo()
		{
			InitializeComponent();
		}

		private const string codeb = "</body></html>";

		/// <summary>
		/// 屏蔽日志中的敏感信息
		/// </summary>
		private string MaskSensitiveInfo(string text)
		{
			if (!_sensitiveInfoEnabled || string.IsNullOrEmpty(text))
				return text;

			string maskedText = text;

			// 先处理URL参数中的敏感信息
			maskedText = MaskUrlParameters(maskedText);

			// 然后处理其他敏感模式
			foreach (string pattern in _sensitivePatterns)
			{
				try
				{
					maskedText = Regex.Replace(
						maskedText,
						pattern,
						SensitiveReplacement,
						RegexOptions.IgnoreCase
					);
				}
				catch (ArgumentException)
				{
					// 忽略无效的正则表达式
					continue;
				}
			}

			return maskedText;
		}

		/// <summary>
		/// 屏蔽URL中的敏感参数
		/// </summary>
		private string MaskUrlParameters(string text)
		{
			// 匹配URL中的参数部分
			string urlPattern = @"(https?://[^\s""'<>]+)";

			return Regex.Replace(text, urlPattern, match =>
			{
				string url = match.Value;
				// 如果URL包含敏感参数，替换它们
				if (url.Contains("key=") || url.Contains("token=") ||
					url.Contains("secret=") || url.Contains("password=") ||
					url.Contains("pw="))
				{
					// 替换URL中的参数值为屏蔽标记
					url = Regex.Replace(url, @"([?&])(key|token|secret|password|pw)=[^&\s""']+",
						"$1$2=" + SensitiveReplacement, RegexOptions.IgnoreCase);
				}
				return url;
			}, RegexOptions.IgnoreCase);
		}

		/// <summary>
		/// 增强的日志着色和敏感信息处理
		/// </summary>
		private string LogToHtml(WeiKit.Logs.Log log)
		{
			string Color = "t";
			if (log.TypeText != "Console")
				switch (log.TypeText)
				{
					case "信息":
						Color = "i";
						break;
					case "警告":
						Color = "w";
						break;
					case "错误":
						Color = "e";
						break;
					case "info":
						Color = "i";
						break;
					case "warning":
						Color = "w";
						break;
					case "error":
						Color = "e";
						break;
					case "Info":
						Color = "i";
						break;
					case "Warning":
						Color = "w";
						break;
					case "Error":
						Color = "e";
						break;
					default:
						Color = "t";
						break;
				}
			else if (AutoColorMode)
			{
				if (log.Text.Contains("error") || log.Text.Contains("错误") || log.Text.Contains("故障") || log.Text.Contains("失败"))
				{
					Color = "e";
				}
				else if (log.Text.Contains("warning") || log.Text.Contains("警告") || log.Text.Contains("异常"))
				{
					Color = "w";
				}
				else if (log.Text.Contains("info") || log.Text.Contains("信息") || log.Text.Contains("提示") || log.Text.Contains("成功") || log.Text.Contains("完成"))
				{
					Color = "i";
				}
				else
				{
					Color = "t";
				}
			}
			else Color = "t";

			// 获取日志文本
			string logText = log.Text;

			// 先进行敏感信息屏蔽（在HTML转义之前）
			if (_sensitiveInfoEnabled)
			{
				logText = MaskSensitiveInfo(logText);
			}

			// 然后再进行HTML转义
			logText = logText.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r\n", "<br>");

			// 最后，如果需要高亮显示屏蔽标记，在转义后添加HTML标签
			if (_sensitiveInfoEnabled && logText.Contains(SensitiveReplacement))
			{
				// 注意：这里替换的是已经转义后的文本，SensitiveReplacement不包含HTML特殊字符，所以安全
				string highlightedReplacement = "<span style='background-color: #ffeb3b; color: #000; padding: 0 2px; border-radius: 3px;'>" + SensitiveReplacement + "</span>";
				logText = logText.Replace(SensitiveReplacement, highlightedReplacement);
			}

			string htmlcode = "<div class=\"" + Color + "\"><b>[" + log.Time.ToString("yyyy-MM-dd HH:mm:ss") + " | " + log.TypeText + "]</b> " + logText + "</div>";
			return htmlcode;
		}

		private void Logsinfo_Load(object sender, EventArgs e)
		{

			pictureBox1.Image = Properties.Resources.信息_info;

			if (logs == null)
			{
				panel3.Visible = true;
			}
			else
			{
				string code = "";
				foreach (var log in logs.logs)
				{
					code = code + LogToHtml(log);
				}

				logs.LogAddEvent += Logs_LogAddEvent;
				async Task InitializeWebView()
				{
					try
					{
						await webView21.EnsureCoreWebView2Async(null);
						webView21.NavigateToString(ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb);
						panel3.Visible = false;

						// 从主题目录加载主题列表，并选中持久化的当前主题
						comboBox1.Items.Clear();
						foreach (var theme in ConsoleLogTheme.GetThemeNames())
						{
							comboBox1.Items.Add(theme);
						}
						int idx = comboBox1.Items.IndexOf(ThemeName);
						comboBox1.SelectedIndex = idx >= 0 ? idx : (comboBox1.Items.Count > 0 ? 0 : -1);
						if (comboBox1.SelectedIndex >= 0)
							ThemeName = comboBox1.SelectedItem.ToString();
					}
					catch (Exception ex)
					{
						panel3.Visible = true;
					}
				}
				InitializeWebView();

				// MaxLogPcs 是库的公开字段，可取任意 int；而 NumericUpDown 默认 Maximum 只有 100，
				// 直接赋值会在 MaxLogPcs > 100 时抛 ArgumentOutOfRangeException。
				// 这里先把控件上限放宽到 int.MaxValue，再同步当前值。
				numericUpDown1.Maximum = int.MaxValue;
				numericUpDown1.Value = logs.MaxLogPcs;

				Logsinfo_SizeChanged(null, null);

				// 初始化敏感信息屏蔽开关的ToolTip
				ToolTip toolTip = new ToolTip();
				toolTip.SetToolTip(switch4, "启用后，日志中的API密钥、密码、手机号、邮箱、IP地址、身份证号、银行卡号、文件路径等敏感信息将被屏蔽显示");
			}
		}

		/// <summary>
		/// 触发一次日志内容重绘。修改 <see cref="ThemeName"/>、<see cref="AutoColorMode"/>
		/// 或从外部批量改动日志后调用即可让窗口按新设置刷新。
		/// （库内部的 LogAddEvent 也走同一路径刷新。）
		/// </summary>
		public void RefreshContent()
		{
			Logs_LogAddEvent(null, null);
		}

		private void Logs_LogAddEvent(object sender, Logs.Log e)
		{
			string code = "";
			foreach (var log in logs.logs)
			{
				code = code + LogToHtml(log);
			}
			Tool.SafeInvoke(this, () =>
			{
				if (webView21 != null && webView21.Visible)
			{
				webView21.NavigateToString(ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb);
				//判断是否有焦点
				if (!webView21.Focused)
					//没有焦点则自动滚动到最底部
					webView21.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, document.body.scrollHeight);");
			}
			//兼容模式
			if (webBrowser1 != null && webBrowser1.Visible)
			{
				webBrowser1.DocumentText = ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb;
			}
			});
		}

		private void Button1_Click(object sender, EventArgs e)
		{
			//为用户打开问题报告链接
			Tool.RunExternalProg("https://alidocs.dingtalk.com/notable/share/form/v013BMqYaLD37mEqwZL_AfRittZ_7A465AG");
		}

		private void Button2_Click(object sender, EventArgs e)
		{
			//尝试使用兼容控件显示
			webView21.Visible = false;
			webBrowser1.Visible = true;
			panel3.Visible = false;
			string code = "";
			foreach (var log in logs.logs)
			{
				code = code + LogToHtml(log);
			}
			webBrowser1.DocumentText = ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb;
		}

		private bool MenuVisible = false;
		private void Logsinfo_SizeChanged(object sender, EventArgs e)
		{
			panel1.Top = 0;
			panel1.Height = this.ClientSize.Height;
			panel4.Top = 0;
			panel4.Left = this.ClientSize.Width - panel4.Width;

			if (MenuVisible)
			{
				panel1.Left = this.ClientSize.Width - panel1.Width;
			}
			else
			{
				panel1.Left = this.ClientSize.Width + 10;
			}
		}

		private void Panel4_Click(object sender, EventArgs e)
		{
			if (!MenuVisible)
			{
				Tool.ControlMove(panel1, new System.Drawing.Point(this.ClientSize.Width - panel1.Width, 0), 20, 200);
				MenuVisible = true;
				panel4.Visible = false;
			}
		}

		private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox1.SelectedItem == null) return;
			//更新主题并持久化
			ThemeName = comboBox1.SelectedItem.ToString();
			ConsoleLogTheme.SaveCurrentTheme(ThemeName);
			RefreshContent();
		}

		private void Label4_Click_1(object sender, EventArgs e)
		{
			if (MenuVisible)
			{
				//panel1移出窗口外
				MenuVisible = false;
				Tool.ControlMove(panel1, new System.Drawing.Point(this.ClientSize.Width + 10, 0), 20, 200);
				panel4.Visible = true;
			}
		}

		private void WebView21_Click(object sender, EventArgs e)
		{
			if (MenuVisible)
			{
				//panel1移出窗口外
				MenuVisible = false;
				Tool.ControlMove(panel1, new System.Drawing.Point(this.ClientSize.Width + 10, 0), 20, 200);
				panel4.Visible = true;
			}
		}

		private void Logsinfo_FormClosed(object sender, FormClosedEventArgs e)
		{
			//解绑日志事件
			if (logs != null)
				logs.LogAddEvent -= Logs_LogAddEvent;
		}

		private void Switch1_IsOpenChange(object sender, bool e)
		{
			AutoColorMode = switch1.IsOpen;
			RefreshContent();
		}

		private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (logs != null)
				logs.MaxLogPcs = ((int)numericUpDown1.Value);
		}

		private void Switch2_IsOpenChange(object sender, bool e)
		{
			this.TopMost = switch2.IsOpen;
		}

		private void Switch3_IsOpenChange(object sender, bool e)
		{
			if (switch3.IsOpen)
			{
				if (MessageBox.Show("真的要启用开发指令吗？您可能无需使用这些功能。", "操作确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					panel10.Visible = true;
				}
				else
				{
					panel10.Visible = false;
					switch3.IsOpen = false;
				}
			}
			else
			{
				panel10.Visible = false;
			}

		}

		private void ComboBox2_TextUpdate(object sender, EventArgs e)
		{
			string[] commands = new string[] { "OPEN_PROGRAM_ROOT", "UNINSTALLER" };
			comboBox2.Items.Clear();
			comboBox2.SelectedIndex = -1;
			// 搜索（不区分大小写）用户输入的文本在命令列表中，并将匹配的命令添加到下拉菜单中
			foreach (string command in commands)
			{
				if (comboBox2.Text != string.Empty)
					if (command.StartsWith(comboBox2.Text, StringComparison.OrdinalIgnoreCase))
					{
						comboBox2.Items.Add(command);
					}
			}
			//显示下拉菜单
			comboBox2.DroppedDown = true;
			comboBox2.SelectionStart = comboBox2.Text.Length;
		}

		private void Button3_Click(object sender, EventArgs e)
		{
			switch (comboBox2.Text)
			{
				case "OPEN_PROGRAM_ROOT":
					System.Diagnostics.Process.Start("explorer.exe", Tool.GetProgramPath());
					break;
				case "UNINSTALLER":
					// 直接调用卸载方法
					ProgTool.UninstallClickOnce(ProgTool.GetAppNameFromFile());
					break;
				default:
					Console.WriteLine($"输入了无效的命令：{comboBox2.Text}");
					break;
			}
		}

		private void ComboBox2_KeyPress(object sender, KeyPressEventArgs e)
		{
			//当用户按下回车键时，触发按钮点击事件
			if (e.KeyChar == (char)Keys.Enter)
			{
				Button3_Click(null, null);
				e.Handled = true; // 阻止系统发出"ding"声
			}
		}

		private void Label10_Click(object sender, EventArgs e)
		{
			Panel4_Click(sender, e);
		}

		/// <summary>
		/// 敏感信息屏蔽开关事件
		/// </summary>
		private void Switch4_IsOpenChange(object sender, bool e)
		{
			// 敏感信息屏蔽-功能开关
			_sensitiveInfoEnabled = e;

			// 当开关状态改变时，刷新当前显示的日志
			if (logs != null && logs.logs != null)
			{
				string code = "";
				foreach (var log in logs.logs)
				{
					code = code + LogToHtml(log);
				}

				Tool.SafeInvoke(this, () =>
				{
					if (webView21 != null && webView21.Visible)
				{
					webView21.NavigateToString(ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb);
					if (!webView21.Focused)
						webView21.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, document.body.scrollHeight);");
				}
				if (webBrowser1 != null && webBrowser1.Visible)
				{
					webBrowser1.DocumentText = ConsoleLogTheme.GetHtmlHeader(ThemeName) + code + codeb;
				}
				});
			}
		}
	}
}