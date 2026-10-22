using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;

namespace WeiKit.Demo
{
	/// <summary>
	/// ⑧ API 服务器演示：用 WeiKit.HttpsApiServer 演示 Token 认证、内置用户、
	/// 登录接口与 ApiRequest.UserName 二级鉴权。
	/// </summary>
	internal class ApiServerDemo
	{
		private const string MasterToken = "demo-master-token";

		private HttpsApiServer server;
		private TextBox tbPort;
		private TextBox tbLog;
		private Label lbStatus;

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"演示 WeiKit.HttpsApiServer：Token 认证 + 内置用户 + 登录接口 + UserName 二级鉴权。" +
				"建议顺序：启动服务器 → 启用认证/用户/登录 → 注册示例 API → 运行完整自测。");

			Ui.AddHeader(content, "1. 启动 / 停止");
			var g1 = Ui.Group(content, "监听 HTTP（localhost）");
			Ui.Row(g1,
				Ui.Text("端口：", 50),
				tbPort = Ui.In("5000", 120),
				Ui.Btn("启动服务器", (s, e) => StartServer(), 120),
				Ui.Btn("停止服务器", (s, e) => StopServer(), 120));
			lbStatus = new Label { Text = "状态：未启动", AutoSize = true, ForeColor = Ui.Muted, Margin = new Padding(0, 4, 0, 4) };
			g1.Controls.Add(lbStatus);

			Ui.AddHeader(content, "2. 认证、用户与登录");
			var g2 = Ui.Group(content, "Token / User / Login");
			Ui.Row(g2,
				Ui.Btn("启用认证 + 用户 + 登录", (s, e) => SetupAuth(), 200),
				Ui.Btn("关闭认证", (s, e) => DisableAuth(), 110));
			Ui.AddHint(g2, $"Master Token 固定为：{MasterToken}。登录接口：GET/POST /api/login（u/p 或 username/password）。");

			Ui.AddHeader(content, "3. 注册示例 API");
			var g3 = Ui.Group(content, "接口：/health（免认证）、/api/whoami、/api/admin-only（二级鉴权）");
			Ui.Row(g3,
				Ui.Btn("注册示例 API", (s, e) => RegisterApis(), 130),
				Ui.Btn("列出已注册 API", (s, e) => ListApis(), 150));

			Ui.AddHeader(content, "4. 客户端自测");
			Ui.Row(content,
				Ui.Btn("运行完整自测", (s, e) => RunSelfTest(), 150));

			Ui.AddHeader(content, "输出");
			tbLog = Ui.Multi("", 920, 280);
			content.Controls.Add(tbLog);

			Append("就绪。请先启动服务器。");
			return host;
		}

		private void StartServer()
		{
			if (server != null && server.IsRunning) { Append("服务器已在运行。"); return; }

			int port;
			if (!int.TryParse(tbPort.Text.Trim(), out port)) port = 5000;

			server = new HttpsApiServer(8);
			server.RequestReceived += (s, e) => Append($"[收到] {e.Method} {e.Path}");
			server.RequestCompleted += (s, e) => Append($"[完成] {e.Method} {e.Path}");

			try
			{
				server.Open(new[] { $"http://localhost:{port}/" });
				string prefixes = string.Join(", ", server.Prefixes);
				lbStatus.Text = $"状态：运行中 {prefixes}";
				Append($"服务器已启动：{prefixes}");
			}
			catch (Exception ex)
			{
				Append($"启动失败：{ex.Message}");
				lbStatus.Text = "状态：启动失败";
			}
		}

		private void StopServer()
		{
			if (server == null || !server.IsRunning) { Append("服务器尚未启动。"); return; }
			server.Close();
			Append("服务器已停止。");
			lbStatus.Text = "状态：已停止";
		}

		private void SetupAuth()
		{
			if (!EnsureRunning()) return;

			server.EnableTokenAuth(MasterToken);
			server.AddUser("admin", "admin888");
			server.AddUser("alice", "123456");
			server.EnableBuiltInLogin("/api/login");
			server.AddAnonymousPath("/health");

			Append($"已启用认证（Master Token = {MasterToken}）、添加用户 admin/alice、登录接口 /api/login、白名单 /health。");
		}

		private void DisableAuth()
		{
			if (server == null) return;
			server.DisableTokenAuth();
			Append("已关闭 Token 认证（全部接口公开）。");
		}

		private void RegisterApis()
		{
			if (!EnsureRunning()) return;

			try
			{
				server.AddApi("/health", req => ApiResponse.Ok("{\"ok\":true}"), requireAuth: false);
				server.AddApi("/api/whoami", req =>
					ApiResponse.Ok($"{{\"user\":\"{req.UserName}\",\"isAuth\":{req.IsAuthenticated.ToString().ToLowerInvariant()}}}"));
				server.AddApi("/api/admin-only", req =>
				{
					// 二级鉴权：Master Token（已认证且无用户名）或 admin 用户放行，其余拒绝
					if (req.IsAuthenticated && string.IsNullOrEmpty(req.UserName))
						return ApiResponse.Ok("{\"msg\":\"Hi Master!\"}");
					if (req.UserName == "admin")
						return ApiResponse.Ok("{\"msg\":\"Hi Admin!\"}");
					return ApiResponse.Error(403, "Requires admin role");
				});
				Append("已注册 /health、/api/whoami、/api/admin-only。");
			}
			catch (Exception ex)
			{
				Append($"注册失败：{ex.Message}");
			}
		}

		private void ListApis()
		{
			if (server == null) { Append("请先启动服务器。"); return; }

			var list = server.GetRegisteredApis();
			Append("已注册 API：" + (list.Count == 0 ? "（无）" : string.Join("、", list)));
		}

		private async void RunSelfTest()
		{
			if (!EnsureRunning()) return;
			string baseUrl = server.Prefixes[0].TrimEnd('/');

			using (var client = new HttpClient())
			{
				try
				{
					string r1 = await GetWithToken(client, baseUrl + "/health", null);
					Append($"[自测①] GET /health（免认证） -> {r1}");

					string r2 = await GetWithToken(client, baseUrl + "/api/admin-only", null);
					Append($"[自测②] GET /api/admin-only（无 token） -> {r2}");

					string aliceToken = await Login(client, baseUrl, "alice", "123456");
					Append($"[自测③] alice 登录 -> {(string.IsNullOrEmpty(aliceToken) ? "失败" : "token=" + aliceToken)}");

					string r4 = await GetWithToken(client, baseUrl + "/api/admin-only", aliceToken);
					Append($"[自测④] alice 访问 admin-only -> {r4}");

					string adminToken = await Login(client, baseUrl, "admin", "admin888");
					Append($"[自测⑤] admin 登录 -> {(string.IsNullOrEmpty(adminToken) ? "失败" : "token=" + adminToken)}");

					string r5 = await GetWithToken(client, baseUrl + "/api/admin-only", adminToken);
					Append($"[自测⑤] admin 访问 admin-only -> {r5}");

					string r6 = await GetWithToken(client, baseUrl + "/api/admin-only", MasterToken);
					Append($"[自测⑥] master 访问 admin-only -> {r6}");
				}
				catch (Exception ex)
				{
					Append($"[自测] 出错：{ex.Message}");
				}
			}
		}

		private bool EnsureRunning()
		{
			if (server == null || !server.IsRunning) { Append("请先启动服务器。"); return false; }
			return true;
		}

		private async Task<string> Login(HttpClient client, string baseUrl, string user, string pass)
		{
			string url = $"{baseUrl}/api/login?u={Uri.EscapeDataString(user)}&p={Uri.EscapeDataString(pass)}";
			string json = await client.GetStringAsync(url);
			var m = Regex.Match(json, "\"token\"\\s*:\\s*\"([^\"]+)\"");
			return m.Success ? m.Groups[1].Value : string.Empty;
		}

		private async Task<string> GetWithToken(HttpClient client, string url, string token)
		{
			using (var req = new HttpRequestMessage(HttpMethod.Get, url))
			{
				if (!string.IsNullOrEmpty(token))
					req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

				using (var resp = await client.SendAsync(req))
				{
					string body = await resp.Content.ReadAsStringAsync();
					return $"{(int)resp.StatusCode} {body}";
				}
			}
		}

		private void Append(string text)
		{
			if (tbLog == null || tbLog.IsDisposed) return;
			if (tbLog.InvokeRequired)
				Tool.SafeInvoke(tbLog, () => AppendCore(text));
			else
				AppendCore(text);
		}

		private void AppendCore(string text)
		{
			if (tbLog == null || tbLog.IsDisposed) return;
			tbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\r\n");
		}
	}
}
