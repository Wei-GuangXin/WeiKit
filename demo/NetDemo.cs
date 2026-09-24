using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;
// 说明：WeiKit.Comp 里也有同名控件，为避免与 WinForms 的 ProgressBar 冲突，
// 本文件统一显式使用 System.Windows.Forms.ProgressBar。
using WinProgressBar = System.Windows.Forms.ProgressBar;

namespace WeiKit.Demo
{
	/// <summary>
	/// ④ 网络演示：HTTP 基础请求、图片下载、多线程下载器、上传器、内置 WebServer、钉钉机器人。
	/// </summary>
	internal class NetDemo
	{
		// HTTP 基础
		private TextBox txtGetUrl;
		private TextBox txtOut;
		private PictureBox picImage;
		private Label lbImageState;

		// 下载
		private TextBox txtDownUrl;
		private TextBox txtDownPath;
		private ComboBox cboMode;
		private WinProgressBar barDown;
		private Label lbDownState;
		private HttpLink.DownTool downTool;

		// 上传
		private TextBox txtUpUrl;
		private TextBox txtUpFile;
		private WinProgressBar barUp;
		private Label lbUpState;
		private HttpLink.UploadTool upTool;

		// WebServer
		private WebServer server;
		private NumericUpDown numPort;
		private CheckBox chkLans;
		private TextBox txtServerPage;
		private Label lbServerState;

		// 钉钉
		private TextBox txtWebhook;
		private TextBox txtDingMsg;
		private Label lbDingState;

		/// <summary>本页的宿主面板（本类不是 Control，需要借它访问 Cursor / IsDisposed）</summary>
		private Control hostPanel;

		public Control Build()
		{
			FlowLayoutPanel content;
			hostPanel = Ui.ScrollHost(out content);
			var host = hostPanel;

			Ui.AddHint(content,
				"网络页所有演示都是真实请求。默认地址是 example.com（可改成任意可达地址）。" +
				"下载器需要服务端支持 HEAD 与 Range，不确定时请选「单线程」。");

			// ==================== HTTP 基础 ====================
			Ui.AddHeader(content, "HttpLink 基础请求");

			var gHttp = Ui.Group(content, "GET / POST / 图片");
			Ui.Row(gHttp,
				Ui.Text("URL：", 40),
				txtGetUrl = Ui.In(Convert.ToString(Program.Cfg.Read("Net.TestUrl")), 480),
				Ui.Btn("GET", (s, e) => DoGet(), 70),
				Ui.Btn("POST JSON", (s, e) => DoPost(), 100),
				Ui.Btn("下载图片", async (s, e) => await DoImage(), 100));
			txtOut = Ui.Multi("", 880, 140, true);
			gHttp.Controls.Add(txtOut);
			Ui.AddHint(gHttp,
				"GetTask 内部 BaseAddress 被硬编码为 example.com，所以必须传完整 URL；" +
				"PostTask 在非 2xx 或异常时返回空串，「成功但响应为空」也是空串，两者无法区分。");

			var gImg = Ui.Group(content, "GetImageFromUrl 结果");
			picImage = new PictureBox
			{
				Width = 320,
				Height = 200,
				BorderStyle = BorderStyle.FixedSingle,
				SizeMode = PictureBoxSizeMode.Zoom,
				BackColor = Color.White,
				Margin = new Padding(0, 4, 0, 6)
			};
			lbImageState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 2, 0, 6) };
			gImg.Controls.Add(picImage);
			gImg.Controls.Add(lbImageState);
			Ui.Row(gImg, Ui.Btn("清空图片", (s, e) =>
			{
				if (picImage.Image != null) { picImage.Image.Dispose(); picImage.Image = null; }
				lbImageState.Text = "已清空。";
			}, 100));

			// ==================== 下载器 ====================
			Ui.AddHeader(content, "DownTool 多线程下载器");

			var gDown = Ui.Group(content, "下载配置");
			Ui.Row(gDown,
				Ui.Text("URL：", 40),
				txtDownUrl = Ui.In("https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg", 480));
			Ui.Row(gDown,
				Ui.Text("保存到：", 60),
				txtDownPath = Ui.In(Path.Combine(Program.WorkDir, "downloaded.bin"), 380),
				Ui.Btn("选择…", (s, e) =>
				{
					using (var dlg = new SaveFileDialog { InitialDirectory = Program.WorkDir, FileName = "downloaded.bin" })
						if (dlg.ShowDialog() == DialogResult.OK) txtDownPath.Text = dlg.FileName;
				}, 80));
			Ui.Row(gDown,
				Ui.Text("模式：", 45),
				cboMode = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 12, 3) },
				Ui.Btn("开始下载", (s, e) => StartDownload(), 110),
				Ui.Btn("取消", (s, e) =>
				{
					downTool?.CancelDownload();
					Ui.Log("已请求取消下载。", "Warning");
				}, 80),
				Ui.Btn("仅探测文件大小", (s, e) => ProbeSize(), 140));
			cboMode.Items.AddRange(new object[] { "Auto（>100MB 自动多线程）", "MultiThread（强制多线程）", "SingleThread（推荐先用）" });
			cboMode.SelectedIndex = 2;

			barDown = new WinProgressBar { Width = 880, Height = 20, Margin = new Padding(0, 4, 0, 4), Maximum = 100 };
			gDown.Controls.Add(barDown);
			lbDownState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 0, 0, 6) };
			gDown.Controls.Add(lbDownState);
			Ui.AddHint(gDown,
				"进度与完成事件都在后台线程触发，Demo 用 Tool.SafeInvoke 回到 UI 线程；这正是库使用的跨线程更新方式。" +
				"对不确定的站点请先用「单线程」，多线程要求服务端正确支持 Range，否则合并结果会损坏。");

			// ==================== 上传器 ====================
			Ui.AddHeader(content, "UploadTool 上传器");

			var gUp = Ui.Group(content, "multipart/form-data 上传");
			Ui.Row(gUp,
				Ui.Text("上传地址：", 70),
				txtUpUrl = Ui.In("https://example.com/upload.php", 420));
			Ui.Row(gUp,
				Ui.Text("本地文件：", 70),
				txtUpFile = Ui.In(Path.Combine(Program.WorkDir, "downloaded.bin"), 380),
				Ui.Btn("选择…", (s, e) =>
				{
					using (var dlg = new OpenFileDialog { InitialDirectory = Program.WorkDir })
						if (dlg.ShowDialog() == DialogResult.OK) txtUpFile.Text = dlg.FileName;
				}, 80),
				Ui.Btn("开始上传", (s, e) => StartUpload(), 100));
			barUp = new WinProgressBar { Width = 880, Height = 20, Margin = new Padding(0, 4, 0, 4), Maximum = 100 };
			gUp.Controls.Add(barUp);
			lbUpState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 0, 0, 6) };
			gUp.Controls.Add(lbUpState);
			Ui.AddHint(gUp,
				"表单字段名固定为 \"file\"（UploadFile 第三个参数可改），需与服务端脚本一致；" +
				"超时 120 秒。Demo 默认地址是 example.com，会失败并显示失败原因 —— 这本身也是错误处理的演示。");

			// ==================== WebServer ====================
			Ui.AddHeader(content, "WebServer 内置服务");

			var gSrv = Ui.Group(content, "启动 / 停止");
			Ui.Row(gSrv,
				Ui.Text("端口：", 45),
				numPort = new NumericUpDown
				{
					Width = 90,
					Minimum = 1024,
					Maximum = 65535,
					Value = Math.Max(1024, Math.Min(65535, ReadPort())),
					Margin = new Padding(0, 4, 12, 3)
				},
				chkLans = new CheckBox { Text = "监听局域网（LANS，需要 URL ACL 或管理员权限）", AutoSize = true, Margin = new Padding(0, 7, 12, 3) },
				Ui.Btn("启动服务", (s, e) => StartServer(), 100),
				Ui.Btn("停止服务", (s, e) => StopServer(), 100),
				Ui.Btn("打开首页", (s, e) => OpenServerPage(), 100));
			Ui.Row(gSrv,
				Ui.Text("软页面内容：", 80),
				txtServerPage = Ui.In("", 520));
			Ui.Row(gSrv,
				Ui.Btn("更新软页面", (s, e) =>
				{
					if (server == null) { Ui.Info("请先启动服务。"); return; }
					server.WritePageData("/api/status", txtServerPage.Text);
					Ui.Log("已更新软页面 /api/status。", "Info");
				}, 120),
				Ui.Btn("读取软页面", (s, e) =>
				{
					if (server == null) { Ui.Info("请先启动服务。"); return; }
					Ui.Info("ReadPageData(\"/api/status\") = \r\n\r\n" + server.ReadPageData("/api/status"));
				}, 120),
				Ui.Btn("查看服务器日志条数", (s, e) =>
				{
					if (server == null) { Ui.Info("请先启动服务。"); return; }
					Ui.Info($"WebServer 自带独立 Logs 实例，当前 {server.logs.logs.Count} 条。\r\n" +
							"可以订阅 server.logs.LogAddEvent 把服务器日志并入主日志。");
				}, 170));
			lbServerState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 2, 0, 6) };
			gSrv.Controls.Add(lbServerState);
			Ui.AddHint(gSrv,
				"服务启动后会注册一个 /api/status 软页面（内存页面，优先于磁盘文件），根路径 / 会重定向到 /index.html。" +
				"默认只监听 localhost，不需要任何权限；勾选 LANS 会监听 * 前缀，失败时会提示拒绝访问。");

			// ==================== 钉钉 ====================
			Ui.AddHeader(content, "DingDingAPI 钉钉机器人");

			var gDing = Ui.Group(content, "发送 markdown 消息");
			Ui.Row(gDing,
				Ui.Text("Webhook：", 70),
				txtWebhook = Ui.In("https://oapi.dingtalk.com/robot/send?access_token=你的令牌", 520));
			Ui.Row(gDing,
				Ui.Text("内容：", 45),
				txtDingMsg = Ui.In("#### WeiKit Demo\\n- 测试消息\\n- 时间 " + DateTime.Now.ToString("HH:mm:ss"), 420),
				Ui.Btn("发送", async (s, e) => await SendDing(), 80));
			lbDingState = new Label { AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 2, 0, 6) };
			gDing.Controls.Add(lbDingState);
			Ui.AddHint(gDing,
				"消息类型固定为 markdown，且不支持 @ 指定人（at 段为空、isAtAll=false）。" +
				"内部每次调用都 new HttpClient 并设 5 秒超时，不适合高频调用。");

			UpdateServerState();
			return host;
		}

		private static int ReadPort()
		{
			object v = Program.Cfg.Read("Net.ServerPort");
			long p;
			return (v != null && long.TryParse(v.ToString(), out p)) ? (int)p : 5694;
		}

		#region HTTP 基础

		private void DoGet()
		{
			string url = txtGetUrl.Text.Trim();
			Ui.Log($"GET {url}", "Info");
			hostPanel.Cursor = Cursors.WaitCursor;
			try
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				string body = HttpLink.GetTask(url);
				sw.Stop();
				if (body == null)
				{
					txtOut.Text = $"GET 失败或返回 null：{url}\r\n" +
								  "（URL 为空、非 2xx、超时或网络异常都会返回 null，具体原因在库内部打到 Console，已被日志捕获）";
					Ui.Log("GET 返回 null。", "Warning");
				}
				else
				{
					txtOut.Text = $"GET {url}\r\n耗时 {sw.ElapsedMilliseconds} ms　长度 {body.Length} 字符\r\n" +
								  "────────────────────────────\r\n" +
								  (body.Length > 3000 ? body.Substring(0, 3000) + "\r\n…（已截断）" : body);
					Ui.Log($"GET 成功，{body.Length} 字符，耗时 {sw.ElapsedMilliseconds} ms。", "Info");
				}
			}
			catch (Exception ex)
			{
				txtOut.Text = "异常：" + ex;
				Ui.Log("GET 抛出异常：" + ex.Message, "Error");
			}
			finally { hostPanel.Cursor = Cursors.Default; }
		}

		private void DoPost()
		{
			string url = txtGetUrl.Text.Trim();
			string json = "{\"from\":\"WeiKit.Demo\",\"time\":\"" + DateTime.Now.ToString("s") + "\",\"value\":123}";
			txtOut.Text = "正在 POST …\r\n请求体：" + json;
			Ui.Log($"POST {url}\r\n{json}", "Info");
			hostPanel.Cursor = Cursors.WaitCursor;
			try
			{
				string resp = HttpLink.PostTask(url, json);
				txtOut.Text = $"POST {url}\r\n请求体：{json}\r\n\r\n响应长度 {resp?.Length ?? 0}\r\n" +
							  "────────────────────────────\r\n" +
							  (string.IsNullOrEmpty(resp)
								  ? "(空串：可能是服务端返回空，也可能是请求彻底失败 —— PostTask 无法区分)"
								  : resp);
				Ui.Log($"POST 返回 {resp?.Length ?? 0} 字符。", "Info");
			}
			catch (Exception ex)
			{
				txtOut.Text = "异常：" + ex;
				Ui.Log("POST 抛出异常：" + ex.Message, "Error");
			}
			finally { hostPanel.Cursor = Cursors.Default; }
		}

		private async Task DoImage()
		{
			// 一个稳定的公开图片地址（维基共享资源的示例图，支持 HTTPS 直链）
			string url = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/280px-PNG_transparency_demonstration_1.png";
			lbImageState.Text = "正在下载图片…";
			Ui.Log("GetImageFromUrl：" + url, "Info");
			try
			{
				Image img = await HttpLink.GetImageFromUrl(url);
				if (img == null)
				{
					lbImageState.Text = "下载失败或返回 null（网络不可达 / 非图片内容）。";
					Ui.Log("图片下载返回 null。", "Warning");
					return;
				}
				// 注意：返回的 Image 绑定在已释放的流上，必须复制一份再长期持有
				var copy = new Bitmap(img);
				img.Dispose();
				if (picImage.Image != null) picImage.Image.Dispose();
				picImage.Image = copy;
				lbImageState.Text = $"已下载 {copy.Width}×{copy.Height} 像素。\r\n" +
									"（GetImageFromUrl 返回的 Image 绑定在已释放的流上，Demo 已复制为 Bitmap 再显示）";
				Ui.Log($"图片下载成功：{copy.Width}×{copy.Height}", "Info");
			}
			catch (Exception ex)
			{
				lbImageState.Text = "异常：" + ex.Message;
				Ui.Log("图片下载异常：" + ex.Message, "Error");
			}
		}

		#endregion

		#region 下载

		private void ProbeSize()
		{
			string url = txtDownUrl.Text.Trim();
			Ui.Log("探测文件大小：" + url, "Info");
			hostPanel.Cursor = Cursors.WaitCursor;
			try
			{
				var probe = new HttpLink.DownTool();
				long size = probe.GetDownloadSize(url);
				probe.Dispose();
				if (size <= 0)
				{
					lbDownState.Text = $"探测失败：GetDownloadSize 返回 {size}。\r\n" +
									   "说明该地址不支持 HEAD 请求或不可达 —— 此时 StartDownload 会直接失败。";
					Ui.Log("GetDownloadSize 返回 0，该地址不能用于 DownTool。", "Warning");
				}
				else
				{
					lbDownState.Text = $"文件大小：{size:N0} 字节（{size / 1024.0:F1} KB）";
					Ui.Log($"探测成功：{size} 字节。", "Info");
				}
			}
			catch (Exception ex)
			{
				lbDownState.Text = "探测异常：" + ex.Message;
			}
			finally { hostPanel.Cursor = Cursors.Default; }
		}

		private void StartDownload()
		{
			if (downTool != null && downTool.IsDownloading)
			{
				Ui.Info("当前已有下载任务在进行中。");
				return;
			}

			// 上次的实例需要释放（它会清理 .partN 临时文件）
			downTool?.Dispose();

			string url = txtDownUrl.Text.Trim();
			string path = txtDownPath.Text.Trim();
			var mode = HttpLink.DownTool.DownMode.SingleThread;
			if (cboMode.SelectedIndex == 0) mode = HttpLink.DownTool.DownMode.Auto;
			if (cboMode.SelectedIndex == 1) mode = HttpLink.DownTool.DownMode.MultiThread;

			downTool = new HttpLink.DownTool();

			// 进度回调在后台线程，必须回到 UI 线程更新控件
			downTool.ProgressChanged += p => Tool.SafeInvoke(barDown, () =>
			{
				barDown.Value = Math.Min(100, Math.Max(0, (int)p));
				lbDownState.Text = $"进度 {p:F1}%　" +
								   $"{Fmt(downTool.DownloadedSize)} / {Fmt(downTool.TotalSize)}　" +
								   $"{Fmt(downTool.Speed)}/s　剩余 {Fmt(downTool.RemainSize)}";
			});

			downTool.DownloadCompleted += (ok, reason) => Tool.SafeInvoke(barDown, () =>
			{
				if (ok)
				{
					lbDownState.Text = $"下载完成：{Fmt(downTool.DownloadedSize)}　模式 {downTool.DownloadMode}\r\n" +
								   $"文件：{downTool.DownFilePath}";
					Ui.Log($"下载完成：{downTool.DownFilePath}（{downTool.DownloadedSize} 字节）", "Info");
				}
				else
				{
					lbDownState.Text = "下载失败：" + reason;
					Ui.Log("下载失败：" + reason, "Error");
				}
			});

			Ui.Log($"开始下载（{mode}）：{url} → {path}", "Info");
			barDown.Value = 0;
			lbDownState.Text = "正在获取文件大小（HEAD 请求）…";
			downTool.StartDownload(url, path, mode);
		}

		private static string Fmt(long bytes)
		{
			string[] units = { "B", "KB", "MB", "GB" };
			double v = bytes;
			int i = 0;
			while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
			return $"{v:0.##} {units[i]}";
		}

		#endregion

		#region 上传

		private void StartUpload()
		{
			if (upTool != null && upTool.IsUploading)
			{
				Ui.Info("当前已有上传任务在进行中。");
				return;
			}
			upTool?.Dispose();
			upTool = new HttpLink.UploadTool();

			upTool.ProgressChanged += p => Tool.SafeInvoke(barUp, () =>
			{
				barUp.Value = Math.Min(100, Math.Max(0, (int)p));
				lbUpState.Text = $"上传进度 {p:F1}%　{Fmt(upTool.UploadedSize)} / {Fmt(upTool.TotalSize)}";
			});

			upTool.UploadCompleted += (ok, text) => Tool.SafeInvoke(barUp, () =>
			{
				if (ok)
				{
					lbUpState.Text = "上传成功，服务端响应：\r\n" + Truncate(text, 300);
					Ui.Log($"上传成功，服务端响应 {text?.Length ?? 0} 字符。", "Info");
				}
				else
				{
					lbUpState.Text = "上传失败：" + text;
					Ui.Log("上传失败：" + text, "Error");
				}
			});

			barUp.Value = 0;
			lbUpState.Text = "正在上传…";
			Ui.Log($"开始上传：{txtUpFile.Text} → {txtUpUrl.Text}", "Info");
			upTool.UploadFile(txtUpUrl.Text.Trim(), txtUpFile.Text.Trim(), "file");
		}

		private static string Truncate(string s, int max)
			=> string.IsNullOrEmpty(s) ? "(空)" : (s.Length <= max ? s : s.Substring(0, max) + "…");

		#endregion

		#region WebServer

		private void StartServer()
		{
			if (server != null && server.ServerStarted)
			{
				Ui.Info("服务已在运行中：" + server.Prefix);
				return;
			}

			server = new WebServer();
			server.AccessEvent += (s, path) => Ui.Log($"[WebServer] 客户端请求：{path}", "Info");
			// 服务器自带的日志实例也可以并入主日志
			server.logs.LogAddEvent += (s, log) => Program.Log.Println("[服务] " + log.Text, log.TypeText);

			// 注册软页面：内存页面，优先于磁盘文件
			server.NewPage("/api/status", "{\"ok\":true,\"app\":\"WeiKit.Demo\"}");
			server.NewPage("/index.html",
				"<html><head><meta charset=\"utf-8\"><title>WeiKit Demo</title></head><body style=\"font-family:Microsoft YaHei\">" +
				"<h2>WeiKit 内置 WebServer 正在运行</h2>" +
				"<ul><li><a href=\"/index.html\">/index.html</a> —— 本页（软页面）</li>" +
				"<li><a href=\"/api/status\">/api/status</a> —— JSON 软页面</li>" +
				"<li>其它路径会映射到程序目录下的同名文件</li></ul>" +
				"<p>提示：可在左侧「软页面内容」输入框中修改 /api/status 的内容并实时生效。</p>" +
				"</body></html>");

			int port = (int)numPort.Value;
			bool lans = chkLans.Checked;
			Ui.Log($"启动 WebServer：端口 {port}，LANS={lans}，站点根目录=程序目录", "Info");

			server.ServerStart(port, "./", lans);

			// 把端口写回配置（演示配置与运行参数的联动）
			Program.Cfg.WriteValue("Net.ServerPort", (long)port);

			UpdateServerState();
			if (server.ServerStarted)
			{
				txtServerPage.Text = server.ReadPageData("/api/status");
				Ui.Info($"服务已启动。\r\n监听前缀：{server.Prefix}\r\n\r\n" +
						"在浏览器里访问该地址即可看到内置页面。");
			}
			else
			{
				Ui.Error("服务未能启动。\r\n\r\n最常见原因：LANS 模式监听了 * 前缀但缺少 URL ACL 权限。\r\n" +
						 "解决方式（管理员命令行）：\r\n" +
						 $"netsh http add urlacl url=http://*:{port}/ user=%USERNAME%\r\n\r\n" +
						 "或取消勾选 LANS 只监听 localhost。");
			}
		}

		private void StopServer()
		{
			if (server == null)
			{
				Ui.Info("服务尚未创建。");
				return;
			}
			server.StopServer();
			Ui.Log("已请求停止 WebServer（StopServer 现在会等待线程退出，超时 5 秒）。", "Warning");
			UpdateServerState();
		}

		private void OpenServerPage()
		{
			if (server == null || string.IsNullOrEmpty(server.Prefix))
			{
				Ui.Info("请先启动服务。");
				return;
			}
			Tool.RunExternalProg(server.Prefix + "index.html");
		}

		private void UpdateServerState()
		{
			if (server == null)
			{
				lbServerState.Text = "服务未创建。";
				return;
			}
			lbServerState.Text = $"运行中：{server.ServerStarted}　监听前缀：{server.Prefix ?? "(空)"}　" +
								 $"服务器日志：{server.logs.logs.Count} 条";
		}

		#endregion

		#region 钉钉

		private async Task SendDing()
		{
			string hook = txtWebhook.Text.Trim();
			string msg = txtDingMsg.Text.Replace("\\n", "\n");
			lbDingState.Text = "正在发送…";
			Ui.Log("DingDingAPI.SendDingDingMessageAsync 发送中…", "Info");
			try
			{
				bool ok = await DingDingAPI.SendDingDingMessageAsync(hook, msg, "WeiKit Demo");
				lbDingState.Text = ok ? "发送成功（errcode = 0）" : "发送失败（返回 false：令牌错误、网络异常或超时）";
				Ui.Log($"钉钉消息发送结果：{ok}", ok ? "Info" : "Warning");
			}
			catch (Exception ex)
			{
				lbDingState.Text = "异常：" + ex.Message;
				Ui.Log("钉钉发送异常：" + ex.Message, "Error");
			}
		}

		#endregion
	}
}
