using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WeiKit.Client
{
	/// <summary>
	/// WeiKit Server 用户侧 API 客户端（独立源码，可直接拷入任意 .NET 8+ 工程）。
	/// 依赖：Newtonsoft.Json、System.Net.Http（.NET 8 已内置）。
	/// 与服务端契约严格对齐：{code,message,data} 响应信封 / Bearer Token 鉴权 / CamelCase 字段。
	/// </summary>
	public class WeiKitClient : IDisposable
	{
		// ==================== 公共 DTO ====================

		public class ApiEnvelope
		{
			public int code;
			public string message;
			public JToken data;
			public bool Ok => code == 0;
			public JObject DataObj => data as JObject;
			public JArray DataArr => data as JArray;
		}

		public class CaptchaInfo
		{
			public string captchaId;
			/// <summary>算式文本（文本模式返回；图片模式为 null）。</summary>
			public string expression;
			/// <summary>验证码图片（data:image/png;base64,...，仅 GetCaptchaImage 返回）。</summary>
			public string image;
			public int ttl;
		}

		public class UserInfo
		{
			public string id;
			public string username;
			public string nickname;
			public string email;
			public string phone;
			public string avatarKey;
			public bool banned;
			public int version;
			public DateTime? createdAt;
			public DateTime? updatedAt;
			public DateTime? lastLoginAt;
			public string lastLoginIp;
			public bool twoFactorEnabled;
			public string role;
		}

		public class SessionInfo
		{
			public string token;
			public string refreshToken;
			public string type;
			public string username;
			public string role;
			public int expires_in;
		}

		public class PluginBrief
		{
			public string id;
			public string name;
			public string description;
			public string author;
			public string category;
			public List<string> tags;
			public string latestVersion;
			public DateTime? updatedAt;
		}

		public class PluginFileItem
		{
			public string fileName;
			public string relPath;
			public long size;
			public string sha256;
			public string downloadUrl;
		}

		public class PluginVersionInfo
		{
			public string version;
			public string fileName;
			public long size;
			public string sha256;
			public string changelog;
			public bool isLatest;
			public DateTime? releasedAt;
			public List<PluginFileItem> files;
			public List<string> downloadUrls;
			public string downloadUrl;
		}

		public class PluginInfo
		{
			public string id;
			public string name;
			public string app;
			public string description;
			public string author;
			public string category;
			public string status;
			public List<string> tags;
			public List<PluginVersionInfo> versions;
			public string latestVersion;
			public DateTime? createdAt;
			public DateTime? updatedAt;
		}

		public class DataMeta
		{
			public string key;
			public string value;
			public string contentType;
			public long version;
			public DateTime? updatedAt;
		}

		/// <summary>本地登录态缓存（自动登录用），绑定机器码与 IP。</summary>
		public class LocalSession
		{
			/// <summary>缓存格式标识。</summary>
			public string format = "weikit-session";
			/// <summary>缓存格式版本。</summary>
			public int version = 1;
			/// <summary>用户名。</summary>
			public string username;
			/// <summary>用户 UID。</summary>
			public string userId;
			/// <summary>登录 Token。</summary>
			public string token;
			/// <summary>刷新 Token。</summary>
			public string refreshToken;
			/// <summary>绑定的机器码。</summary>
			public string machineCode;
			/// <summary>绑定的 IP。</summary>
			public string boundIp;
			/// <summary>缓存时间（UTC）。</summary>
			public DateTime savedAt;
			/// <summary>缓存的用户数据。</summary>
			public List<DataMeta> data;
		}

		public class SyncMetaItem
		{
			public string key;
			public long version;
			public DateTime? updatedAt;
			public long size;
			public string contentType;
		}

		public class SyncResult
		{
			public long DataVersion;
			public List<SyncMetaItem> Items;
			public List<DataMeta> Changed;
		}

		public class UpdateCheckResult
		{
			public bool hasUpdate;
			public bool forceUpdate;
			public string currentVersion;
			public string latestVersion;
			public string downloadUrl;
			public string notice;
		}

		public class AnnouncementInfo
		{
			public string id;
			public string title;
			public string content;
			/// <summary>公告图片地址（站内相对路径或 http(s) 链接，无需解析 Markdown）。</summary>
			public List<string> images;
			public bool pinned;
			public DateTime? createdAt;
			public DateTime? expiresAt;
		}

		public class NoticeResult
		{
			public string notice;
			public string forceMinVersion;
			public string downloadUrl;
			public List<AnnouncementInfo> announcements;
		}

		// ==================== 客户端状态 ====================

		/// <summary>API 服务器根地址（如 https://api.example.com 或 http://127.0.0.1:8080）。</summary>
		public string ApiServerUrl { get; private set; }

		/// <summary>可选 API Key（X-Api-Key 头）。</summary>
		public string ApiServerKey { get; private set; }

		/// <summary>当前登录用户名。</summary>
		public string Name { get; set; }

		/// <summary>当前 UID。</summary>
		public string UserId { get; set; } = string.Empty;

		/// <summary>API 令牌（Bearer）。</summary>
		public string Token { get; set; }

		/// <summary>刷新令牌。</summary>
		public string RefreshToken { get; set; }

		/// <summary>从本地缓存恢复的用户数据（ImportLocalData 成功后可用）。</summary>
		public List<DataMeta> CachedData { get; private set; }

		/// <summary>
		/// 服务端是否启用邮箱验证（/users/me 返回的 twoFactorEnabled）。
		/// 邮箱验证码登录、修改/重置密码的邮箱验证均依赖该开关。
		/// </summary>
		public bool ServerTwoFactorEnabled { get; private set; }

		/// <summary>登录是否有效（有 Token）。</summary>
		public bool IsEffective => !string.IsNullOrWhiteSpace(Token);

		/// <summary>最近一次失败的错误信息（Login 失败时可读取）。</summary>
		public string LastError => _lastError;
		private string _lastError;

		/// <summary>最后一次 GetCaptcha 拿到的 captchaId，供 Login 手动模式复用。</summary>
		private string _lastCaptchaId;

		private readonly HttpClient _http;
		private bool _disposed;

		/// <summary>构造客户端。</summary>
		public WeiKitClient(string apiServerUrl, string apiServerKey = "")
		{
			ApiServerUrl = (apiServerUrl ?? string.Empty).TrimEnd('/');
			ApiServerKey = apiServerKey ?? string.Empty;
			_http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
			_http.DefaultRequestHeaders.UserAgent.TryParseAdd("WeiKit-Client/1.0");
		}

		public void Dispose()
		{
			if (_disposed) return;
			_http?.Dispose();
			_disposed = true;
		}

		// ==================== 底层请求 ====================

		/// <summary>
		/// 发送请求，返回原始响应 JSON 字符串。
		/// 自动附加 Bearer Token / X-Api-Key / Content-Type。
		/// </summary>
		public string SendRequest(string endpoint, string method = "GET", string data = "")
		{
			return SendRequestAsync(endpoint, method, data).GetAwaiter().GetResult();
		}

		/// <summary>SendRequest 的异步版本。</summary>
		public async System.Threading.Tasks.Task<string> SendRequestAsync(
			string endpoint, string method = "GET", string data = "")
		{
			string url = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
				? endpoint
				: ApiServerUrl + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);

			using (var req = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url))
			{
				req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				if (!string.IsNullOrWhiteSpace(Token))
					req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
				if (!string.IsNullOrWhiteSpace(ApiServerKey))
					req.Headers.TryAddWithoutValidation("X-Api-Key", ApiServerKey);

				string m = method.ToUpperInvariant();
				if (m == "GET" || m == "DELETE")
				{
					if (!string.IsNullOrWhiteSpace(data))
					{
						url = BuildUrlWithQuery(url, data);
						req.RequestUri = new Uri(url);
					}
				}
				else
				{
					req.Content = new StringContent(data ?? string.Empty, Encoding.UTF8, "application/json");
				}

				try
				{
					using (var resp = await _http.SendAsync(req).ConfigureAwait(false))
					{
						return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
					}
				}
				catch (HttpRequestException ex)
				{
					throw new InvalidOperationException($"请求失败: {ex.Message}", ex);
				}
			}
		}

		/// <summary>发送请求并解析为 ApiEnvelope。</summary>
		public ApiEnvelope Request(string endpoint, string method = "GET", string data = "")
		{
			return ParseEnvelope(SendRequest(endpoint, method, data));
		}

		/// <summary>带 object 自动序列化为 JSON 的便捷调用。</summary>
		public ApiEnvelope RequestObject(string endpoint, string method, object data)
		{
			string json = data == null ? string.Empty : JsonConvert.SerializeObject(data);
			return Request(endpoint, method, json);
		}

		// ==================== 鉴权 / 公开接口 ====================

		/// <summary>获取算术验证码（文本模式，便于程序自动计算）。</summary>
		public CaptchaInfo GetCaptcha()
		{
			var env = Request("/users/captcha", "GET");
			if (!env.Ok) throw new InvalidOperationException("获取验证码失败: " + env.message);
			var info = env.DataObj?.ToObject<CaptchaInfo>();
			_lastCaptchaId = info?.captchaId;   // 缓存，供 Login 手动模式复用
			return info;
		}

		/// <summary>
		/// 获取服务端渲染的验证码图片（data:image/png;base64,...）。
		/// 图片模式不返回算式明文，适合展示给用户；captchaId 会缓存，供 Login/LoginByEmail 手动模式使用。
		/// </summary>
		public CaptchaInfo GetCaptchaImage()
		{
			var env = Request("/users/captcha?image=1", "GET");
			if (!env.Ok) throw new InvalidOperationException("获取验证码失败: " + env.message);
			var info = env.DataObj?.ToObject<CaptchaInfo>();
			_lastCaptchaId = info?.captchaId;
			return info;
		}

		/// <summary>
		/// 生成带登录态的用户中心 URL。
		/// 桌面程序登录后，可用此 URL 直接打开浏览器，用户中心会从 #token 读取令牌
		/// 并自动登录，无需再次输入账号密码（同机单点登录）。
		/// </summary>
		/// <param name="userCenterBaseUrl">用户中心根地址，如 https://file.xxx:60721/user</param>
		public string GetUserCenterUrl(string userCenterBaseUrl)
		{
			if (string.IsNullOrWhiteSpace(Token))
				throw new InvalidOperationException("尚未登录，无法生成免登录链接");
			string baseUrl = (userCenterBaseUrl ?? string.Empty).TrimEnd('/');
			return baseUrl + "/#token=" + Uri.EscapeDataString(Token);
		}

		/// <summary>
		/// 直接调用系统默认浏览器打开带登录态的用户中心。
		/// Windows 上用 Process.Start 启动浏览器；非 Windows 平台可改用 GetUserCenterUrl 自行打开。
		/// </summary>
		/// <param name="userCenterBaseUrl">用户中心根地址。</param>
		public void OpenUserCenterInBrowser(string userCenterBaseUrl)
		{
			string url;
			if (userCenterBaseUrl[userCenterBaseUrl.Length - 1] != '/')
				url = GetUserCenterUrl(userCenterBaseUrl + "/" + "user");
			else
				url = GetUserCenterUrl(userCenterBaseUrl + "user");
			var psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			};
			System.Diagnostics.Process.Start(psi);
		}

		/// <summary>
		/// 登录。返回 true 表示成功，Token/Name/UserId 自动填充。
		/// 失败时返回 false，错误信息通过 LastError 读取。
		/// <para>模式一（手动）：先调用 GetCaptcha() 拿到 expression 显示给用户，用户算好结果后传 verificationcode。</para>
		/// <para>模式二（自动）：verificationcode 留空，内部自动拉验证码并计算算式结果。</para>
		/// </summary>
		/// <param name="name">用户名。</param>
		/// <param name="password">密码。</param>
		/// <param name="verificationcode">验证码（算式结果）。留空则自动计算。</param>
		public bool Login(string name, string password, string verificationcode = "")
		{
			try
			{
				string captchaId = _lastCaptchaId;
				string answer = verificationcode;

				// 未提供验证码答案，或未提前调用 GetCaptcha：自动拉取并计算
				if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(captchaId))
				{
					var cap = GetCaptcha();
					captchaId = cap?.captchaId;
					answer = EvalExpression(cap?.expression)?.ToString();
				}

				var payload = new
				{
					username = name,
					password,
					captchaId = captchaId ?? string.Empty,
					captcha = answer
				};
				var env = RequestObject("/users/login", "POST", payload);
				_lastCaptchaId = null;
				if (!env.Ok)
				{
					_lastError = env.message;
					return false;
				}
				var sess = env.DataObj?["session"]?.ToObject<SessionInfo>();
				var user = env.DataObj?["user"]?.ToObject<UserInfo>();
				if (sess == null || user == null)
				{
					_lastError = "登录响应缺少 session/user";
					return false;
				}
				Token = sess.token;
				RefreshToken = sess.refreshToken;
				Name = user.username;
				UserId = user.id;
				_lastError = null;
				return true;
			}
			catch (Exception ex)
			{
				_lastError = ex.Message;
				return false;
			}
		}

		/// <summary>
		/// 邮箱验证码登录（服务端启用邮箱验证时可用）。返回 true 表示成功。
		/// 失败信息通过 <see cref="LastError"/> 读取。
		/// <para>verificationcode 为图形验证码答案；留空则自动拉取文本验证码并计算。</para>
		/// </summary>
		/// <param name="email">账号绑定邮箱。</param>
		/// <param name="emailCode">邮箱收到的验证码（先调用 SendEmailCode(email, "login")）。</param>
		/// <param name="verificationcode">图形验证码答案，留空自动计算。</param>
		public bool LoginByEmail(string email, string emailCode, string verificationcode = "")
		{
			try
			{
				string captchaId = _lastCaptchaId;
				string answer = verificationcode;

				if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(captchaId))
				{
					var cap = GetCaptcha();
					captchaId = cap?.captchaId;
					answer = EvalExpression(cap?.expression)?.ToString();
				}

				var payload = new
				{
					email,
					emailCode,
					captchaId = captchaId ?? string.Empty,
					captcha = answer
				};
				var env = RequestObject("/users/login/email", "POST", payload);
				_lastCaptchaId = null;
				if (!env.Ok)
				{
					_lastError = env.message;
					return false;
				}
				var sess = env.DataObj?["session"]?.ToObject<SessionInfo>();
				var user = env.DataObj?["user"]?.ToObject<UserInfo>();
				if (sess == null || user == null)
				{
					_lastError = "登录响应缺少 session/user";
					return false;
				}
				Token = sess.token;
				RefreshToken = sess.refreshToken;
				Name = user.username;
				UserId = user.id;
				_lastError = null;
				return true;
			}
			catch (Exception ex)
			{
				_lastError = ex.Message;
				return false;
			}
		}

		/// <summary>登出，清空 Token。</summary>
		public bool Logout()
		{
			try
			{
				var env = Request("/users/logout", "POST");
				Token = null;
				RefreshToken = null;
				return env.Ok;
			}
			catch
			{
				Token = null;
				RefreshToken = null;
				return false;
			}
		}

		/// <summary>刷新 Token。</summary>
		public bool Refresh()
		{
			if (string.IsNullOrWhiteSpace(RefreshToken)) return false;
			var env = RequestObject("/users/refresh", "POST", new { refreshToken = RefreshToken });
			if (!env.Ok) return false;
			var sess = env.DataObj?["session"]?.ToObject<SessionInfo>();
			if (sess == null) return false;
			Token = sess.token;
			RefreshToken = sess.refreshToken;
			return true;
		}

		/// <summary>
		/// 注册（服务端强制绑定邮箱）。需先 GetCaptcha 拿到 captchaId；
		/// 服务端启用邮箱验证时还需先 SendEmailCode(email, "register") 并传入 emailCode。
		/// 成功后自动填充 Token/Name/UserId。
		/// </summary>
		public (bool ok, string message) Register(string username, string password,
			string email = "", string phone = "", string nickname = "",
			string captchaId = "", string captcha = "", string emailCode = "")
		{
			if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
				return (false, "邮箱格式不正确（注册必须绑定邮箱）");
			var env = RequestObject("/users/newuser", "POST", new
			{
				username,
				password,
				email,
				phone,
				nickname,
				captchaId,
				captcha,
				emailCode
			});
			if (env.Ok)
			{
				var sess = env.DataObj?["session"]?.ToObject<SessionInfo>();
				var user = env.DataObj?["user"]?.ToObject<UserInfo>();
				if (sess != null) { Token = sess.token; RefreshToken = sess.refreshToken; }
				if (user != null) { Name = user.username; UserId = user.id; }
			}
			return (env.Ok, env.message);
		}

		/// <summary>重置密码。</summary>
		public (bool ok, string message) ResetPassword(string username, string newPassword,
			string captchaId, string captcha, string emailCode = "")
		{
			var env = RequestObject("/users/resetpassword", "POST",
				new { username, newPassword, captchaId, captcha, emailCode });
			return (env.Ok, env.message);
		}

		/// <summary>
		/// 发送邮箱验证码。purpose 取值：register（注册，始终可用）、login（邮箱登录）、
		/// reset（重置密码）、changepassword（修改密码）、deluser（注销账号）；
		/// 除 register 外均需服务端启用邮箱验证（2FA）。
		/// 返回服务端是否真正投递成功（SMTP 失败返回 false）。
		/// </summary>
		public bool SendEmailCode(string email, string purpose = "register")
		{
			var env = RequestObject("/users/sendcode", "POST", new { email, purpose });
			return env.Ok && env.DataObj?["sent"]?.Type == JTokenType.Boolean && env.DataObj["sent"].Value<bool>();
		}

		// ==================== 用户信息 ====================

		/// <summary>获取当前登录用户信息，并同步服务端邮箱验证开关（ServerTwoFactorEnabled）。</summary>
		public UserInfo GetMe()
		{
			var env = Request("/users/me", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			var user = env.DataObj?["user"]?.ToObject<UserInfo>();
			if (user != null) { Name = user.username; UserId = user.id; }
			var flag = env.DataObj?["twoFactorEnabled"];
			if (flag != null && flag.Type == JTokenType.Boolean)
				ServerTwoFactorEnabled = flag.Value<bool>();
			return user;
		}

		/// <summary>
		/// 修改密码：原密码与邮箱验证码二选一。
		/// 只传原密码时按原密码校验；只传邮箱验证码时按验证码校验；两者都传时任一通过即可。
		/// 成功后服务端会吊销全部会话，本地 Token 同时清空。
		/// </summary>
		/// <param name="oldPassword">原密码（可空，但需与 emailCode 至少填一项）。</param>
		/// <param name="newPassword">新密码（至少 8 位）。</param>
		/// <param name="emailCode">邮箱验证码（可选，先用 SendEmailCode(email, "changepassword") 获取）。</param>
		public (bool ok, string message) ChangePassword(string oldPassword, string newPassword, string emailCode = "")
		{
			if (string.IsNullOrWhiteSpace(oldPassword) && string.IsNullOrWhiteSpace(emailCode))
				return (false, "请提供原密码或邮箱验证码");
			var env = RequestObject("/users/changepassword", "POST", new { oldPassword, newPassword, emailCode });
			if (!env.Ok) return (false, env.message);
			Token = null; RefreshToken = null;
			return (true, env.message);
		}

		/// <summary>仅使用邮箱验证码修改密码（oldPassword 传空）。</summary>
		public (bool ok, string message) ChangePasswordByEmailCode(string emailCode, string newPassword)
		{
			return ChangePassword(string.Empty, newPassword, emailCode);
		}

		/// <summary>注销账号。</summary>
		public (bool ok, string message) DeleteAccount(string emailCode = "")
		{
			var env = RequestObject("/users/deluser", "POST", new { emailCode });
			if (env.Ok) { Token = null; RefreshToken = null; Name = null; UserId = null; }
			return (env.Ok, env.message);
		}

		// ==================== 数据托管 ====================

		/// <summary>读取一条数据。</summary>
		public DataMeta GetData(string key)
		{
			var env = RequestObject("/data/getdata", "POST", new { key });
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?.ToObject<DataMeta>();
		}

		/// <summary>写入数据，返回服务端版本号与字节数。</summary>
		public (long version, int size, string message) WriteData(string key, string value,
			string contentType = "text/plain")
		{
			var env = RequestObject("/data/writedata", "POST", new { key, value, contentType });
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return ((long)env.DataObj["version"], (int)env.DataObj["size"], env.message);
		}

		/// <summary>删除数据。</summary>
		public bool DeleteData(string key) => RequestObject("/data/deldata", "POST", new { key }).Ok;

		/// <summary>增量同步：返回全量元数据 + 变化项。knownVersions 传 null 时返回全量。</summary>
		public SyncResult Sync(Dictionary<string, long> knownVersions = null)
		{
			var env = RequestObject("/data/sync", "POST",
				new { known = knownVersions ?? new Dictionary<string, long>() });
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return new SyncResult
			{
				DataVersion = (long)env.DataObj["dataVersion"],
				Items = env.DataObj["items"]?.ToObject<List<SyncMetaItem>>() ?? new List<SyncMetaItem>(),
				Changed = env.DataObj["changed"]?.ToObject<List<DataMeta>>() ?? new List<DataMeta>()
			};
		}

		/// <summary>导出全部数据。</summary>
		public List<DataMeta> ExportAllData()
		{
			var env = Request("/data/export", "POST");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj["data"]?.ToObject<List<DataMeta>>() ?? new List<DataMeta>();
		}

		/// <summary>
		/// 获取当前用户头像的 data key（服务端 User.avatarKey，空值则默认 "avatar"）。
		/// </summary>
		public string GetAvatarKey()
		{
			var me = GetMe();
			return string.IsNullOrWhiteSpace(me?.avatarKey) ? "avatar" : me.avatarKey;
		}

		/// <summary>获取头像 base64 字符串；未设置头像时返回 null。</summary>
		public string GetAvatarBase64()
		{
			try
			{
				return GetData(GetAvatarKey())?.value;
			}
			catch
			{
				return null;   // 数据不存在（尚未设置头像）
			}
		}

		/// <summary>尝试把头像解码为字节数组；未设置时返回 false。</summary>
		public bool TryGetAvatarBytes(out byte[] bytes)
		{
			bytes = null;
			string b64 = GetAvatarBase64();
			if (string.IsNullOrWhiteSpace(b64)) return false;
			try
			{
				bytes = Convert.FromBase64String(b64);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>上传头像（base64 写入 data key "avatar"）。</summary>
		public (long version, int size, string message) SetAvatarBase64(string base64, string contentType = "image/png")
		{
			return WriteData("avatar", base64, contentType);
		}

		/// <summary>上传头像（字节数组）。</summary>
		public (long version, int size, string message) SetAvatarBytes(byte[] bytes, string contentType = "image/png")
		{
			return SetAvatarBase64(Convert.ToBase64String(bytes), contentType);
		}

		/// <summary>把本地图片文件设为头像。</summary>
		public (long version, int size, string message) SetAvatarFromFile(string imagePath, string contentType = "image/png")
		{
			return SetAvatarBytes(File.ReadAllBytes(imagePath), contentType);
		}

		// ==================== 本地登录态缓存（自动登录，绑定机器码+IP） ====================

		/// <summary>
		/// 验证当前 Token 是否仍然有效（调用 /users/me）。
		/// Token 为空或服务端返回非成功一律视为失效。
		/// </summary>
		public bool VerifyToken()
		{
			if (string.IsNullOrWhiteSpace(Token)) return false;
			try
			{
				var env = Request("/users/me", "GET");
				return env.Ok;
			}
			catch
			{
				return false;   // 网络异常/服务不可达也视为 Token 不可用，不向上抛
			}
		}

		/// <summary>
		/// 登录成功后，把当前登录态（Token/刷新Token/用户身份/用户数据）加密缓存到本地，
		/// 并绑定机器码与 IP，供下次运行自动登录（ImportLocalData）使用。
		/// </summary>
		/// <param name="filePath">本地缓存文件路径（建议 .wksession）。</param>
		/// <param name="password">加密密码。</param>
		public void ExportLocalData(string filePath, string password)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath 不能为空");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password 不能为空");
			if (string.IsNullOrWhiteSpace(Token)) throw new InvalidOperationException("尚未登录，无法缓存登录态");

			var session = new LocalSession
			{
				username = Name,
				userId = UserId,
				token = Token,
				refreshToken = RefreshToken,
				machineCode = GetMachineCode(),
				boundIp = GetCurrentIp(),
				savedAt = DateTime.UtcNow,
				data = ExportAllData()
			};

			byte[] plain = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(session, Formatting.None));
			byte[] blob = EncryptBackup(plain, password);

			string dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			File.WriteAllBytes(filePath, blob);
		}

		/// <summary>
		/// 从本地缓存恢复登录态（自动登录）。依次校验：机器码绑定、Token 有效性、IP 绑定，
		/// 任一不满足抛出 <see cref="InvalidOperationException"/>。
		/// </summary>
		/// <param name="filePath">本地缓存文件路径。</param>
		/// <param name="password">解密密码。</param>
		public void ImportLocalData(string filePath, string password)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath 不能为空");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password 不能为空");
			if (!File.Exists(filePath)) throw new FileNotFoundException("缓存文件不存在", filePath);

			var session = ReadSessionFile(filePath, password);

			// 机器码绑定校验（本地，无需网络）
			if (!string.Equals(session.machineCode, GetMachineCode(), StringComparison.Ordinal))
				throw new InvalidOperationException("缓存绑定于其它机器，无法使用");

			// 临时挂载 Token，以便校验 IP 与 Token 有效性
			Token = session.token;
			RefreshToken = session.refreshToken;

			string currentIp;
			try
			{
				currentIp = GetCurrentIp();
			}
			catch (Exception ex)
			{
				ClearAuth();
				throw new InvalidOperationException("Token 已失效，自动登录失败：" + ex.Message, ex);
			}

			// IP 绑定校验
			if (!string.Equals(session.boundIp ?? string.Empty, currentIp ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				ClearAuth();
				throw new InvalidOperationException("缓存绑定于其它 IP，无法使用");
			}

			Name = session.username;
			UserId = session.userId;
			CachedData = session.data ?? new List<DataMeta>();
			_lastError = null;
		}

		/// <summary>
		/// 导入的安全版本：任何失败（密码错误/机器码或 IP 不匹配/Token 失效/文件缺失）返回 false，
		/// 原因写入 <see cref="LastError"/>，不抛异常。
		/// </summary>
		/// <param name="filePath">本地缓存文件路径。</param>
		/// <param name="password">解密密码。</param>
		public bool TryImportLocalData(string filePath, string password)
		{
			try
			{
				ImportLocalData(filePath, password);
				return true;
			}
			catch (Exception ex)
			{
				ClearAuth();
				_lastError = ex.Message;
				return false;
			}
		}

		/// <summary>读取并解密缓存文件，返回反序列化后的登录态对象。</summary>
		private static LocalSession ReadSessionFile(string filePath, string password)
		{
			byte[] plain = DecryptBackup(File.ReadAllBytes(filePath), password);
			var session = JsonConvert.DeserializeObject<LocalSession>(Encoding.UTF8.GetString(plain));
			if (session == null || session.format != "weikit-session")
				throw new InvalidOperationException("缓存文件格式不正确");
			return session;
		}

		/// <summary>计算当前机器码（机器名/OS/处理器等组合的 SHA256 指纹，跨平台）。</summary>
		private static string GetMachineCode()
		{
			try
			{
				string s = Environment.MachineName
					+ "|" + Environment.OSVersion.VersionString
					+ "|" + Environment.ProcessorCount
					+ "|" + Environment.SystemDirectory;
				using (var sha = SHA256.Create())
				{
					byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
					return BitConverter.ToString(h).Replace("-", string.Empty).Substring(0, 32);
				}
			}
			catch { return "UNKNOWN"; }
		}

		/// <summary>通过服务端获取当前客户端 IP（依赖 /users/me 返回 ip；Token 无效会抛异常）。</summary>
		private string GetCurrentIp()
		{
			var env = Request("/users/me", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?["ip"]?.ToString() ?? string.Empty;
		}

		/// <summary>清空登录态（Token/身份/缓存数据）。</summary>
		private void ClearAuth()
		{
			Token = null;
			RefreshToken = null;
			Name = null;
			UserId = null;
			CachedData = null;
		}

		// ==================== 插件 ====================

		/// <summary>列出所有已发布插件。</summary>
		public List<PluginBrief> ListPlugins()
		{
			var env = Request("/plugin/getlist", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj["plugins"]?.ToObject<List<PluginBrief>>() ?? new List<PluginBrief>();
		}

		/// <summary>获取插件详情（含版本列表）。</summary>
		public PluginInfo GetPlugin(string id)
		{
			var env = Request("/plugin/getinfo?id=" + Uri.EscapeDataString(id), "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?["plugin"]?.ToObject<PluginInfo>();
		}

		/// <summary>获取插件版本列表。</summary>
		public List<PluginVersionInfo> GetPluginVersions(string pluginId)
		{
			var env = Request("/plugin/getverlist?id=" + Uri.EscapeDataString(pluginId), "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj["versions"]?.ToObject<List<PluginVersionInfo>>() ?? new List<PluginVersionInfo>();
		}

		/// <summary>获取插件最新版本下载信息。</summary>
		public PluginVersionInfo GetLatestDownload(string pluginId)
		{
			var env = Request("/plugin/getdownloadurl?id=" + Uri.EscapeDataString(pluginId), "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?.ToObject<PluginVersionInfo>();
		}

		/// <summary>获取插件指定版本下载信息。</summary>
		public PluginVersionInfo GetVersionDownload(string pluginId, string version)
		{
			string qs = $"?id={Uri.EscapeDataString(pluginId)}&version={Uri.EscapeDataString(version)}";
			var env = Request("/plugin/getverdownloadurl" + qs, "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?.ToObject<PluginVersionInfo>();
		}

		// ==================== 本体更新 ====================

		/// <summary>获取最新版本信息（GitHub 源）。</summary>
		public JObject GetLatestVersion()
		{
			var env = Request("/update/getlatver", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj;
		}

		/// <summary>获取全部版本列表（GitHub 源）。</summary>
		public JArray GetVersionList()
		{
			var env = Request("/update/getverlist", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataArr;
		}

		/// <summary>检查更新（含强制更新判断 / 公告合并）。</summary>
		public UpdateCheckResult CheckUpdate(string currentVersion)
		{
			var env = RequestObject("/update/check", "POST", new { version = currentVersion });
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?.ToObject<UpdateCheckResult>();
		}

		/// <summary>获取公告（服务端 + GitHub notice）。</summary>
		public NoticeResult GetNotice()
		{
			var env = Request("/update/getnotice", "GET");
			if (!env.Ok) throw new InvalidOperationException(env.message);
			return env.DataObj?.ToObject<NoticeResult>();
		}

		// ==================== 内部工具 ====================

		/// <summary>解析算术验证码表达式（如 "3 + 4" / "5 - 2" / "6 * 7"）。</summary>
		private static int? EvalExpression(string expr)
		{
			if (string.IsNullOrWhiteSpace(expr)) return null;
			var m = Regex.Match(expr, @"\s*(-?\d+)\s*([+\-*])\s*(-?\d+)\s*");
			if (!m.Success) return null;
			int a = int.Parse(m.Groups[1].Value);
			int b = int.Parse(m.Groups[3].Value);
			switch (m.Groups[2].Value)
			{
				case "+": return a + b;
				case "-": return a - b;
				case "*": return a * b;
				default: return null;
			}
		}

		private static ApiEnvelope ParseEnvelope(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return new ApiEnvelope { code = -1, message = "响应为空" };
			try
			{
				var obj = JObject.Parse(json);
				return new ApiEnvelope
				{
					code = (int)obj["code"],
					message = (string)obj["message"],
					data = obj["data"]
				};
			}
			catch
			{
				return new ApiEnvelope { code = -1, message = "响应非 JSON: " + json };
			}
		}

		private static string BuildUrlWithQuery(string url, string data)
		{
			if (string.IsNullOrWhiteSpace(data)) return url;

			JObject jo = null;
			try { jo = JObject.Parse(data); } catch { jo = null; }

			if (jo != null)
			{
				var pairs = new List<string>();
				foreach (var p in jo.Properties())
					pairs.Add(Uri.EscapeDataString(p.Name) + "=" +
							  Uri.EscapeDataString(p.Value?.ToString() ?? string.Empty));
				data = string.Join("&", pairs);
			}
			return url + (url.Contains("?") ? "&" : "?") + data.TrimStart('?');
		}

		/// <summary>备份文件魔数（8 字节）。</summary>
		private const string BackupMagic = "WKITBAK1";

		/// <summary>PBKDF2 迭代次数。</summary>
		private const int BackupIterations = 100000;

		/// <summary>
		/// 加密备份数据：PBKDF2(SHA256) 派生 64 字节密钥（前 32 字节 AES、后 32 字节 HMAC），
		/// AES-256-CBC 加密，Encrypt-then-MAC 保证完整性。
		/// 输出格式：magic(8) + salt(16) + iv(16) + mac(32) + cipher。
		/// </summary>
		private static byte[] EncryptBackup(byte[] plain, string password)
		{
			var salt = new byte[16];
			var iv = new byte[16];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
				rng.GetBytes(iv);
			}

			byte[] aesKey, macKey;
			DeriveKeys(password, salt, out aesKey, out macKey);

			byte[] cipher;
			using (var aes = Aes.Create())
			{
				aes.Key = aesKey;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				using (var enc = aes.CreateEncryptor())
					cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
			}

			byte[] macInput = new byte[iv.Length + cipher.Length];
			Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
			Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);

			byte[] mac;
			using (var hmac = new HMACSHA256(macKey))
				mac = hmac.ComputeHash(macInput);

			byte[] magic = Encoding.ASCII.GetBytes(BackupMagic);
			byte[] outBuf = new byte[8 + 16 + 16 + 32 + cipher.Length];
			Buffer.BlockCopy(magic, 0, outBuf, 0, 8);
			Buffer.BlockCopy(salt, 0, outBuf, 8, 16);
			Buffer.BlockCopy(iv, 0, outBuf, 24, 16);
			Buffer.BlockCopy(mac, 0, outBuf, 40, 32);
			Buffer.BlockCopy(cipher, 0, outBuf, 72, cipher.Length);
			return outBuf;
		}

		/// <summary>解密备份数据；密码错误或文件被篡改时抛 <see cref="InvalidOperationException"/>。</summary>
		private static byte[] DecryptBackup(byte[] blob, string password)
		{
			if (blob == null || blob.Length < 8 + 16 + 16 + 32)
				throw new InvalidOperationException("备份文件损坏或不完整");

			if (Encoding.ASCII.GetString(blob, 0, 8) != BackupMagic)
				throw new InvalidOperationException("备份文件格式不正确");

			var salt = new byte[16];
			var iv = new byte[16];
			var mac = new byte[32];
			int cipherLen = blob.Length - 72;
			var cipher = new byte[cipherLen];
			Buffer.BlockCopy(blob, 8, salt, 0, 16);
			Buffer.BlockCopy(blob, 24, iv, 0, 16);
			Buffer.BlockCopy(blob, 40, mac, 0, 32);
			Buffer.BlockCopy(blob, 72, cipher, 0, cipherLen);

			byte[] aesKey, macKey;
			DeriveKeys(password, salt, out aesKey, out macKey);

			byte[] macInput = new byte[iv.Length + cipher.Length];
			Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
			Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);

			byte[] actual;
			using (var hmac = new HMACSHA256(macKey))
				actual = hmac.ComputeHash(macInput);

			if (!FixedTimeEquals(actual, mac))
				throw new InvalidOperationException("密码错误或备份文件已被篡改");

			using (var aes = Aes.Create())
			{
				aes.Key = aesKey;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				using (var dec = aes.CreateDecryptor())
					return dec.TransformFinalBlock(cipher, 0, cipher.Length);
			}
		}

		/// <summary>PBKDF2(SHA256) 派生 AES / HMAC 密钥。</summary>
		private static void DeriveKeys(string password, byte[] salt, out byte[] aesKey, out byte[] macKey)
		{
			byte[] km;
			using (var kdf = new Rfc2898DeriveBytes(password, salt, BackupIterations, HashAlgorithmName.SHA256))
				km = kdf.GetBytes(64);
			aesKey = new byte[32];
			macKey = new byte[32];
			Buffer.BlockCopy(km, 0, aesKey, 0, 32);
			Buffer.BlockCopy(km, 32, macKey, 0, 32);
		}

		/// <summary>
		/// 恒定时间比较两个字节数组，避免时序攻击。
		/// .NET Framework 4.7.2 无 CryptographicOperations，故自行实现。
		/// </summary>
		private static bool FixedTimeEquals(byte[] a, byte[] b)
		{
			if (a == null || b == null) return false;
			if (a.Length != b.Length) return false;
			int diff = 0;
			for (int i = 0; i < a.Length; i++)
				diff |= a[i] ^ b[i];
			return diff == 0;
		}
	}
}
