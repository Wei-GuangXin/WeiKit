using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeiKit
{
	/// <summary>
	/// 基础Web服务页面框架
	/// </summary>
	public class WebServer
	{
		/// <summary>
		/// 服务端线程实例
		/// </summary>
		public Thread SeverThread;
		public string Prefix;//服务目前监听目标
		/// <summary>
		/// HttpListener 实例。<b>延迟创建</b>：某些受限环境（如部分沙箱/容器）在
		/// <c>new HttpListener()</c> 时就会抛 <c>PlatformNotSupportedException</c>，
		/// 延迟创建可以让「只用软页面、不启动监听」的用法在任何环境下都能工作。
		/// </summary>
		private HttpListener listener;
		List<Page> pages = new List<Page>();//软页面数据
		public Logs logs = new Logs();//服务器日志
		/// <summary>
		/// 服务线程退出标志
		/// </summary>
		private volatile bool ThreadExitEn = false;

		private int Port = 5694;
		/// <summary>
		/// 表示服务器是否已启动
		/// </summary>
		public bool ServerStarted
		{
			get
			{
				return SeverThread != null && SeverThread.IsAlive;
			}
		}
		/// <summary>
		/// 表示有客户端访问了服务器
		/// </summary>
		public event EventHandler<string> AccessEvent;
		/// <summary>
		/// 服务器站点根目录（绝对路径）。由 <see cref="ServerStart"/> 的 Rootpath 参数解析而来。
		/// </summary>
		public string RootPath { get; private set; } = string.Empty;
		/// <summary>
		/// 404页面数据
		/// </summary>
		private const string _404Page = "<!DOCTYPE html> <html lang=\"zh-CN\"> <head> <meta charset=\"UTF-8\"> <title>页面未找到 - 404 错误</title> <style> /* 基本重置 */ * { margin: 0; padding: 0; box-sizing: border-box; } body, html { height: 100%; font-family: Arial, sans-serif; background-color: #f4f4f4; display: flex; justify-content: center; align-items: center; } .not-found-container { text-align: center; padding: 20px; background: white; border-radius: 8px; box-shadow: 0 0 15px rgba(0,0,0,.1); } .not-found-container h1 { color: #333; font-size: 2em; margin-bottom: 20px; } .not-found-container p { color: #666; font-size: 1.2em; margin-bottom: 30px; } .home-link { display: inline-block; padding: 10px 20px; background-color: #007bff; color: white !important; text-decoration: none; border-radius: 5px; font-size: 1em; transition: background .3s ease; } .home-link:hover { background-color: #0056b3; } </style> </head> <body> <div class=\"not-found-container\"> <h1>无法访问</h1> <p>您正在试图请求访问一个不存在的页面“{PageName}”，服务器无法找到对应的资源。</p> <a href=\"/index.html\" class=\"home-link\">返回首页</a> </div> </body> </html>";
		/// <summary>
		/// 启动服务器
		/// </summary>
		/// <param name="prefix">指定要监听的链接</param>
		/// <param name="Rootpath">指定服务器根目录挂载路径</param>
		/// <param name="LANS">指定是否监听局域网内其它设备的请求</param>
		public void ServerStart(int port = 5694, string Rootpath = "./", bool LANS = false)
		{
			string str = $"http://localhost:{port}/";
			Port = port;

			// 先解析站点根目录：这样即使后面监听启动失败（端口占用/权限不足），
			// RootPath 也已经是正确值，路径穿越校验依然有效。
			RootPath = string.IsNullOrEmpty(Rootpath)
				? AppDomain.CurrentDomain.BaseDirectory
				: Rootpath.Replace("./", AppDomain.CurrentDomain.BaseDirectory);

			if (SeverThread != null && SeverThread.IsAlive)
			{
				logs.Println($"WEBServer已经启动，无法重复启动。","警告");
				return;
			}

			// 延迟创建：只有真正启动监听时才需要 HttpListener
			try
			{
				listener = new HttpListener();
			}
			catch (Exception e)
			{
				logs.Println($"[WebServer] 无法创建 HttpListener：{e.Message}", "错误");
				logs.Println("[WebServer] 该环境不支持 HTTP 监听（常见于受限沙箱/容器），服务器未启动。", "警告");
				return;
			}

			logs.Println($"WEBServer正在启动。");
			logs.Println($"已将WEBServer站点根目录挂载到-->{RootPath}");
			if (LANS)
			{
				string[] strings = Tool.GetLocalIPv4List();
				string strr = str.Replace("localhost", "*");
				listener.Prefixes.Add(strr);
				if (strings != null && strings.Length > 0) strr = str.Replace("localhost", strings[0]);
				Prefix = strr;
				logs.Println($"注意：当前模式需要在获得管理员权限后执行，如出现错误请检查。");
				logs.Println($"监听地址-->{Prefix}");
			}
			else
			{
				listener.Prefixes.Add(str);
				Prefix = str;
				logs.Println($"监听地址-->{str}");
			}
			try
			{
				listener.Start();
			}
			catch (Exception e)
			{
				logs.Println($"[WebServer] 服务器错误：{e.Message}","错误");
				return;
			}
			SeverThread = new Thread(_SeverThread);
			SeverThread.Name = "程序内置WEBServer子线程";
			SeverThread.IsBackground = true;//设置为后台线程
			SeverThread.Start();
			logs.Println($"[WebServer] 请使用<{Prefix}>来连接WEBServer。");
		}
		/// <summary>
		/// 停止服务器
		/// </summary>
		public void StopServer()
		{
			if (SeverThread == null) return;
			logs.Println("[WebServer] 正在尝试结束服务线程。");
			//关闭线程运行使能
			ThreadExitEn = true;
			try
			{
				listener?.Stop();
			}
			catch { }
			// 发起一个本地请求，唤醒阻塞中的 GetContext()
			try
			{
				string shutdownUrl = $"http://localhost:{Port}/__shutdown_signal__";
				using (var client = new HttpClient())
				{
					// 使用 CancelAfter 或独立 Task 避免卡死
					var cts = new CancellationTokenSource(3000); // 最多等3秒
					var task = client.GetStringAsync(shutdownUrl);
					if (Task.WaitAny(new[] { task }, cts.Token) >= 0)
					{
						logs.Println("[WebServer] 成功发送关闭关闭请求。");
					}
				}
			}
			catch
			{
				// 忽略所有网络异常（比如服务已经停止）
			}

			logs.Println("[WebServer] 正在等待服务线程退出...");

			// 真正等待线程结束：旧版本这里被注释掉了，导致 StopServer 返回后
			// ServerStarted 仍可能为 true、端口也还没释放。
			try
			{
				if (!SeverThread.Join(5000))
				{
					logs.Println("[WebServer] 服务线程响应超时（5 秒内未退出），端口可能仍被占用。", "Warning");
				}
				else
				{
					logs.Println("[WebServer] 服务线程已退出。");
					SeverThread = null;
				}
			}
			catch (ThreadStateException)
			{
				// 线程尚未启动过，忽略
			}
		}
		private void _SeverThread()
		{
			try
			{
				logs.Println($"[WebServer] 服务线程启动完成。");
				while (!ThreadExitEn)
				{
					try
					{
						HttpListenerContext context = listener.GetContext();
						if (ThreadExitEn) break;//如果退出标志被置位则跳出循环
						HttpListenerRequest request = context.Request;
						HttpListenerResponse response = context.Response;
						string UrlText = request.RawUrl;
						if (UrlText == "/") UrlText = "/index.html";
						string PrintData = ReadPageData(UrlText);
						if (PrintData == null)
						{
							string filePath = null;
							bool forbidden = false;
							try
							{
								string requestedPath = GetUrlText(context);
								// 安全校验：路径穿越（..\..\）会解析到根目录之外，直接拒绝
								if (!IsPathInsideRoot(requestedPath))
								{
									forbidden = true;
									logs.Println($"[WebServer] 安全拒绝：请求路径越出站点根目录（{UrlText}）", "Warning");
									WriteSimpleResponse(response, 403, "<h1>403 Forbidden</h1><p>请求路径不被允许。</p>");
									AccessEvent?.Invoke(this, UrlText);
								}
								else if (File.Exists(requestedPath))
								{
									filePath = requestedPath;
								}
							}
							catch (Exception ex)
							{
								logs.Println($"[WebServer] 解析请求路径失败：{ex.Message}", "Warning");
							}

							if (!forbidden && filePath != null)
							{
								logs.Println($"[WebServer] 客户端请求（{filePath}）");
								byte[] buffer = File.ReadAllBytes(filePath);

								response.ContentLength64 = buffer.Length;
								string extension = Path.GetExtension(filePath).ToLower().TrimStart('.');
								string contentType = "text/html; charset=utf-8";

								switch (extension)
								{
									case "html": contentType = "text/html; charset=utf-8"; break;
									case "css": contentType = "text/css; charset=utf-8"; break;
									case "js": contentType = "application/javascript; charset=utf-8"; break;
									case "png": contentType = "image/png"; break;
									case "jpg":
									case "jpeg": contentType = "image/jpeg"; break;
									case "gif": contentType = "image/gif"; break;
									default: contentType = "application/octet-stream"; break;
								}

								response.ContentType = contentType;
								using (Stream output = response.OutputStream)
								{
									output.Write(buffer, 0, buffer.Length);
								}
								AccessEvent?.Invoke(this, filePath);
							}
							else if (!forbidden)
							{
								PrintData = _404Page;
								byte[] buffer = Encoding.UTF8.GetBytes(PrintData.Replace("{PageName}", UrlText));
								response.ContentLength64 = buffer.Length;
								response.ContentType = "text/html; charset=utf-8";
								using (Stream output = response.OutputStream)
								{
									output.Write(buffer, 0, buffer.Length);
								}
								logs.Println($"[WebServer] Error : Resources {UrlText} Does Not Exist.", "Warning");
								AccessEvent?.Invoke(this, UrlText);
							}
						}
						else
						{
							byte[] buffer = Encoding.UTF8.GetBytes(PrintData);
							response.ContentLength64 = buffer.Length;
							response.ContentType = "text/html; charset=utf-8";
							using (Stream output = response.OutputStream)
							{
								output.Write(buffer, 0, buffer.Length);
							}
							AccessEvent?.Invoke(this, GetUrlText(context));
						}
					}
					catch (HttpListenerException ex) when (ex.ErrorCode == 995 || ex.ErrorCode == 1236)
					{
						// 错误码 995: IO operation aborted (通常由 Stop() 引起)
						// 表示 listener 已被 Stop()，正常退出
						if (ThreadExitEn)
						{
							logs.Println("[WebServer] 监听器已停止。");
							break;
						}
						else
						{
							logs.Println($"[WebServer] 意外的监听器异常: {ex.Message}", "警告");
						}
					}
					catch (ObjectDisposedException)
					{
						logs.Println("[WebServer] 监听器已被释放。");
						break;
					}
					catch (Exception e)
					{
						logs.Println($"[WebServer] 服务器异常：{e.Message}", "警告");
					}
				}
			}
			catch (Exception ex)
			{
				logs.Println($"[WebServer] 服务线程意外终止: {ex.Message}\n{ex.StackTrace}", "错误");
			}
			finally
			{
				logs.Println("[WebServer] 服务已关闭。");
				ThreadExitEn = false; // 复位标志
			}
			return;
		}

		//服务软页面
		public class Page
		{
			public string PageName;
			public string PageData;
		}
		/// <summary>
		/// 添加一个软页面，重复添加会覆写原有页面的数据。
		/// </summary>
		/// <param name="PageName">页面的名称</param>
		/// <param name="PageData">页面数据</param>
		public void NewPage(string PageName, string PageData = "")
		{
			bool newen = true;
			//遍历搜索并覆写软页面数据
			foreach (Page page in pages)
			{
				if (page.PageName == PageName)
				{
					page.PageData = PageData;
					newen = false;
				}
			}
			//没有搜索到就新建一个
			if (newen)
			{
				pages.Add(new Page { PageName = PageName, PageData = PageData });
				logs.Println($"[WebServer] 创建<{PageName}>软页面完成。");
			}
		}
		/// <summary>
		/// 设置指定软页面的数据
		/// </summary>
		/// <param name="PageName">页面名称</param>
		/// <param name="PageData">页面数据</param>
		/// <returns>返回是否成功</returns>
		public bool WritePageData(string PageName, string PageData)
		{
			foreach (Page page in pages)
			{
				if (page.PageName == PageName)
				{
					page.PageData = PageData;
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// 获取指定的软页面数据
		/// </summary>
		/// <param name="PageName">页面名称</param>
		/// <returns>返回页面数据</returns>
		public string ReadPageData(string PageName)
		{
			foreach (Page page in pages)
			{
				if (page.PageName == PageName)
				{
					return page.PageData;
				}
			}
			return null;
		}
		/// <summary>
		/// 将请求 URL 转换为站点根目录下的实际文件路径（自动 URL 解码、去掉查询字符串）。
		/// </summary>
		/// <param name="context">HTTP 请求上下文</param>
		/// <returns>拼出的文件路径（未做根目录越界校验，请配合 <see cref="IsPathInsideRoot"/> 使用）</returns>
		private string GetUrlText(HttpListenerContext context)
		{
			string raw = context.Request.RawUrl ?? "/";
			int q = raw.IndexOf('?');
			if (q >= 0) raw = raw.Substring(0, q);       // 去掉查询字符串，避免 "?a=1" 混进文件名

			string decoded = WebUtility.UrlDecode(raw);
			return Path.GetFullPath(RootPath + decoded.Replace('/', '\\').TrimStart('\\'));
		}

		/// <summary>
		/// 判断给定路径是否位于站点根目录内，用于阻止 <c>..\..\</c> 之类的路径穿越。
		/// </summary>
		/// <param name="path">要检查的路径</param>
		/// <returns>在根目录内返回 true</returns>
		public bool IsPathInsideRoot(string path)
		{
			return IsPathInside(Path.GetFullPath(RootPath), path);
		}

		/// <summary>
		/// 判断目标路径是否位于指定根目录内（纯函数，不依赖 HttpListener 实例）。
		/// </summary>
		/// <param name="rootPath">站点根目录（绝对路径）</param>
		/// <param name="targetPath">要检查的路径</param>
		/// <returns>在根目录内返回 true；路径非法或越界返回 false</returns>
		public static bool IsPathInside(string rootPath, string targetPath)
		{
			if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(targetPath)) return false;
			try
			{
				string root = Path.GetFullPath(rootPath);
				if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
					root += Path.DirectorySeparatorChar;

				string full = Path.GetFullPath(targetPath);
				return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;   // 非法字符等一律视为不允许
			}
		}

		/// <summary>
		/// 写入一个简单的纯文本/HTML 响应（用于 403 等安全响应）。
		/// </summary>
		private static void WriteSimpleResponse(HttpListenerResponse response, int statusCode, string body)
		{
			try
			{
				byte[] buffer = Encoding.UTF8.GetBytes(body ?? string.Empty);
				response.StatusCode = statusCode;
				response.ContentLength64 = buffer.Length;
				response.ContentType = "text/html; charset=utf-8";
				using (Stream output = response.OutputStream)
				{
					output.Write(buffer, 0, buffer.Length);
				}
			}
			catch { /* 响应可能已被客户端断开，忽略 */ }
		}
	}
}
