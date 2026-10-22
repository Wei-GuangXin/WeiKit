using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WeiKit
{
	/// <summary>
	/// HTTPS API 服务器类
	/// 使用事件队列处理客户端请求
	/// 支持多 Token 认证（Header / QueryString 多位置传值）
	/// 内置用户管理 + 可自定义路径登录接口 + 二级鉴权 UserName
	/// </summary>
	public class HttpsApiServer : IDisposable
	{
		#region 常量 & 私有字段

		/// <summary>密码加盐哈希的固定盐（避免彩虹表；同库不同部署可改）</summary>
		private const string USER_SALT = "HttpsApiServer::User::v1::";

		private HttpListener _listener;
		private CancellationTokenSource _cancellationTokenSource;
		private Task _listenerTask;
		private readonly ConcurrentQueue<ApiRequestContext> _requestQueue;
		private readonly ConcurrentDictionary<string, ApiHandler> _apiHandlers;
		private bool _isRunning;
		private readonly int _maxConcurrentRequests;
		private int _activeRequests;
		private readonly SemaphoreSlim _requestSemaphore;

		// ----- Token 认证相关 -----
		private bool _tokenAuthEnabled;
		/// <summary>Key: token; Value: 绑定的用户名（空串 = master token，不绑定特定用户）</summary>
		private readonly ConcurrentDictionary<string, string> _tokens;
		/// <summary>路径白名单（Key 规范化 / 开头，忽略大小写）</summary>
		private readonly ConcurrentDictionary<string, byte> _anonymousPaths;

		// ----- 内置用户系统（新增）-----
		/// <summary>Key: 用户名（忽略大小写） Value: 加盐 SHA256 哈希后的密码</summary>
		private readonly ConcurrentDictionary<string, string> _users;

		#endregion

		#region 公共属性

		/// <summary>
		/// 服务器是否正在运行
		/// </summary>
		public bool IsRunning => _isRunning;

		/// <summary>
		/// 服务器监听的前缀列表
		/// </summary>
		public IReadOnlyList<string> Prefixes { get; private set; }

		/// <summary>
		/// 是否启用了 Token 认证
		/// </summary>
		public bool TokenAuthEnabled => _tokenAuthEnabled;

		/// <summary>
		/// 请求处理事件（用于日志或监控）
		/// </summary>
		public event EventHandler<ApiRequestEventArgs> RequestReceived;
		public event EventHandler<ApiRequestEventArgs> RequestCompleted;

		#endregion

		#region 构造函数

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="maxConcurrentRequests">最大并发请求数</param>
		public HttpsApiServer(int maxConcurrentRequests = 10)
		{
			_maxConcurrentRequests = maxConcurrentRequests;
			_requestQueue = new ConcurrentQueue<ApiRequestContext>();
			_apiHandlers = new ConcurrentDictionary<string, ApiHandler>(StringComparer.OrdinalIgnoreCase);
			_requestSemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
			_isRunning = false;

			// Token 相关初始化（现在 Value 存 userName 绑定，空串 = master token）
			_tokenAuthEnabled = false;
			_tokens = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			_anonymousPaths = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

			// 新增：用户存储（忽略用户名大小写）
			_users = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		#endregion

		#region Token 认证 - 公共方法（易用性设计）

		/// <summary>
		/// 启用 Token 认证（一键开关，极致易用）。
		/// 若未传入任何 token，将自动生成一个 32 字符的随机 token 并打印到控制台（master token，无用户绑定）。
		/// </summary>
		/// <param name="initialTokens">初始有效的 master token 列表（可选）</param>
		public void EnableTokenAuth(params string[] initialTokens)
		{
			_tokenAuthEnabled = true;

			if (initialTokens != null && initialTokens.Length > 0)
			{
				foreach (var t in initialTokens)
				{
					if (!string.IsNullOrWhiteSpace(t))
						AddToken(t.Trim(), userName: string.Empty);   // master token
				}
			}

			// 如果没有 token，自动生成一个，方便零配置使用
			if (_tokens.IsEmpty)
			{
				string auto = GenerateRandomToken();
				AddToken(auto, userName: string.Empty);
				Console.WriteLine($"[TokenAuth] 已启用 Token 认证，自动生成 Master Token: {auto}");
			}
			else
			{
				Console.WriteLine($"[TokenAuth] 已启用 Token 认证，当前有效 Token 数: {_tokens.Count}，用户数: {_users.Count}");
			}
		}

		/// <summary>
		/// 关闭 Token 认证（所有接口变为公开访问）
		/// </summary>
		public void DisableTokenAuth()
		{
			_tokenAuthEnabled = false;
			Console.WriteLine("[TokenAuth] 已关闭 Token 认证");
		}

		/// <summary>
		/// 添加一个 master Token（无用户绑定）。
		/// 若要绑定到指定用户，请使用带 userName 的重载。
		/// </summary>
		public void AddToken(string token) => AddToken(token, string.Empty);

		/// <summary>
		/// 添加一个 Token，并绑定到指定用户（userName 为空 = master token）。
		/// 业务层可通过 ApiRequest.UserName 读取身份做二级鉴权。
		/// </summary>
		public void AddToken(string token, string userName)
		{
			if (string.IsNullOrWhiteSpace(token))
				throw new ArgumentException("Token 不能为空", nameof(token));
			_tokens.TryAdd(token.Trim(), userName != null ? userName.Trim() : string.Empty);
		}

		/// <summary>
		/// 删除一个 Token（同时会解除用户绑定）。
		/// </summary>
		public bool RemoveToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) return false;
			return _tokens.TryRemove(token.Trim(), out _);
		}

		/// <summary>
		/// 清空所有 Token（除白名单接口外全部无法访问）
		/// </summary>
		public void ClearTokens()
		{
			_tokens.Clear();
		}

		/// <summary>
		/// 获取当前所有有效 Token（仅 key 列表；要绑定名需另行查询）
		/// </summary>
		public IReadOnlyList<string> GetTokens() => _tokens.Keys.ToList();

		/// <summary>
		/// 添加免认证路径（白名单）。例：/api/public, /health
		/// </summary>
		public void AddAnonymousPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("路径不能为空", nameof(path));
			string normalized = path.Trim();
			if (!normalized.StartsWith("/")) normalized = "/" + normalized;
			_anonymousPaths.TryAdd(normalized, 0);
		}

		/// <summary>
		/// 从白名单移除路径
		/// </summary>
		public bool RemoveAnonymousPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) return false;
			string normalized = path.Trim();
			if (!normalized.StartsWith("/")) normalized = "/" + normalized;
			return _anonymousPaths.TryRemove(normalized, out _);
		}

		/// <summary>
		/// 获取当前所有白名单路径
		/// </summary>
		public IReadOnlyList<string> GetAnonymousPaths() => _anonymousPaths.Keys.ToList();

		#endregion

		#region 内置用户管理（新增）

		/// <summary>
		/// 添加一个可登录的内置用户。密码将自动加盐 SHA256 后存储。
		/// </summary>
		/// <param name="userName">用户名（忽略大小写）</param>
		/// <param name="plainPassword">明文密码</param>
		public void AddUser(string userName, string plainPassword)
		{
			if (string.IsNullOrWhiteSpace(userName))
				throw new ArgumentException("用户名不能为空", nameof(userName));
			if (string.IsNullOrWhiteSpace(plainPassword))
				throw new ArgumentException("密码不能为空", nameof(plainPassword));

			string user = userName.Trim();
			string hash = HashPassword(user, plainPassword);
			if (!_users.TryAdd(user, hash))
			{
				// 用户已存在 → 允许重置密码（方便交互式 adduser 重复调）
				_users[user] = hash;
			}
		}

		/// <summary>
		/// 删除用户，同时吊销该用户所有已颁发的 Token（安全起见，避免已删除用户仍能访问）。
		/// </summary>
		public bool RemoveUser(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName)) return false;
			string user = userName.Trim();
			bool removed = _users.TryRemove(user, out _);

			if (removed)
			{
				// 吊销该用户所有已颁发的 token（用移除空项的方式）
				foreach (var kv in _tokens.ToArray())
				{
					if (string.Equals(kv.Value, user, StringComparison.Ordinal))
						_tokens.TryRemove(kv.Key, out _);
				}
			}

			return removed;
		}

		/// <summary>
		/// 获取当前所有已注册用户名列表
		/// </summary>
		public IReadOnlyList<string> GetUserNames() => _users.Keys.ToList();

		#endregion

		#region 内置登录接口（新增）

		/// <summary>
		/// 一键注册内置登录接口。路径可自定义，默认 "/api/login"。
		/// 支持 4 种参数接收方式（按优先级）：
		///   1. QueryString  ?u=xxx&amp;p=xxx
		///   2. QueryString  ?username=xxx&amp;password=xxx
		///   3. JSON Body POST {"u":"","p":""}
		///   4. JSON Body POST {"username":"","password":""}
		/// 成功返回 200: {"success":true,"token":"xxx","userName":"xxx","expires_in":86400}
		/// 失败返回 401: {"error":"Invalid username or password"}
		/// </summary>
		/// <param name="path">登录接口路径，默认 /api/login。会自动规范化以 '/' 开头。</param>
		public void EnableBuiltInLogin(string path = "/api/login")
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("登录路径不能为空", nameof(path));

			// 规范化路径
			string loginPath = path.Trim();
			if (!loginPath.StartsWith("/")) loginPath = "/" + loginPath;

			// 注册处理器（requireAuth: false，登录接口必须免认证）
			AddApi(loginPath, (req) => BuiltInLoginHandler(req), requireAuth: false);

			Console.WriteLine($"[Login] 已注册内置登录接口: {loginPath}  (可用 QueryString 或 JSON Body 传 username/password)");
		}

		/// <summary>
		/// 内置登录处理器：校验用户 + 颁发绑定到该用户的 Token
		/// </summary>
		private ApiResponse BuiltInLoginHandler(ApiRequest req)
		{
			// 1. 从 QueryString 取参数（支持两组命名）
			string u = req.QueryString["u"];
			string p = req.QueryString["p"];
			if (string.IsNullOrWhiteSpace(u)) u = req.QueryString["username"];
			if (string.IsNullOrWhiteSpace(p)) p = req.QueryString["password"];

			// 2. QueryString 拿不到 → 尝试 JSON Body
			if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
			{
				if (!string.IsNullOrEmpty(req.Body))
				{
					try
					{
						JObject root = JObject.Parse(req.Body);
						if (string.IsNullOrWhiteSpace(u))
							u = GetJsonString(root, "u") ?? GetJsonString(root, "username");
						if (string.IsNullOrWhiteSpace(p))
							p = GetJsonString(root, "p") ?? GetJsonString(root, "password");
					}
					catch
					{
						// JSON 解析失败 → 忽略，仍按已有 u/p 往下走（会返回 401）
					}
				}
			}

			// 3. 基础校验
			if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
				return ApiResponse.Error(401, "Missing username or password");

			u = u.Trim();

			// 4. 查用户表 + 校验密码哈希
			if (!_users.TryGetValue(u, out var storedHash) || storedHash != HashPassword(u, p))
				return ApiResponse.Error(401, "Invalid username or password");

			// 5. 单点登录：颁发新 Token 前，先吊销该用户所有已颁发的旧 Token
			//    确保同一账户"新登录踢掉旧登录"，避免多端并存（与 RemoveUser 安全策略一致）
			foreach (var kv in _tokens.ToArray())
			{
				if (string.Equals(kv.Value, u, StringComparison.Ordinal))
					_tokens.TryRemove(kv.Key, out _);
			}

			// 6. 颁发绑定到该用户的全新 Token（前缀 "u_"，方便识别）
			string token = "u_" + GenerateRandomToken();
			AddToken(token, u);

			// 7. 成功响应
			return ApiResponse.Ok(
				$"{{\"success\":true,\"token\":\"{token}\",\"userName\":\"{u}\",\"expires_in\":86400}}");
		}

		#endregion

		#region 公共方法 - 服务器控制 & API 注册

		/// <summary>
		/// 启动服务器
		/// </summary>
		/// <param name="prefixes">监听的前缀列表，例如：https://*:8443/</param>
		/// <param name="certificate">SSL证书（如果使用HTTPS）</param>
		public void Open(IEnumerable<string> prefixes, X509Certificate2 certificate = null)
		{
			if (_isRunning)
			{
				throw new InvalidOperationException("服务器已经在运行中");
			}

			if (prefixes == null)
			{
				throw new ArgumentNullException(nameof(prefixes));
			}

			_listener = new HttpListener();
			Prefixes = new List<string>(prefixes);

			foreach (var prefix in Prefixes)
			{
				_listener.Prefixes.Add(prefix);
			}

			// 配置SSL证书（如果提供）
			if (certificate != null && Prefixes.Any(p => p.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
			{
				foreach (var prefix in Prefixes.Where(p => p.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
				{
					var uri = new Uri(prefix);
					if (uri.Port != 443)
					{
						// netsh http add sslcert 是 Windows http.sys 专用命令；
						// Linux/macOS 下 HttpListener 用托管栈，要么前置反代做 TLS 卸载，
						// 要么通过系统/运行时机制加载证书，无需也不能执行 netsh。
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						{
							ConfigureSslCertificate(uri.Port, certificate);
						}
						else
						{
							Console.WriteLine($"[SSL] 非Windows平台({RuntimeInformation.OSDescription})，跳过 netsh SSL 证书绑定。"
								+ " 请使用 Nginx/Caddy 前置反代做 TLS 卸载，或通过 ASP.NET Core Kestrel/运行时机制加载证书。");
						}
					}
				}
			}

			_cancellationTokenSource = new CancellationTokenSource();
			_listener.Start();
			_isRunning = true;

			// 启动监听任务
			_listenerTask = Task.Run(() => ListenLoop(_cancellationTokenSource.Token));

			// 启动请求处理任务
			Task.Run(() => ProcessQueueLoop(_cancellationTokenSource.Token));

			Console.WriteLine($"服务器已启动，监听: {string.Join(", ", Prefixes)}");
		}

		/// <summary>
		/// 关闭服务器
		/// </summary>
		public void Close()
		{
			if (!_isRunning)
			{
				return;
			}

			_isRunning = false;
			_cancellationTokenSource?.Cancel();

			if (_listener != null && _listener.IsListening)
			{
				_listener.Stop();
				_listener.Close();
			}

			// 等待任务完成
			try
			{
				if (_listenerTask != null)
				{
					_listenerTask.Wait(TimeSpan.FromSeconds(5));
				}
			}
			catch (AggregateException)
			{
				// 忽略取消异常
			}

			_requestSemaphore.Dispose();
			Console.WriteLine("服务器已停止");
		}

		/// <summary>
		/// 添加API处理器（同步）
		/// </summary>
		/// <param name="apiPath">API路径，例如：/api/test</param>
		/// <param name="handler">处理函数</param>
		/// <param name="requireAuth">是否需要Token认证（默认true，若全局未启用认证则忽略）</param>
		public void AddApi(string apiPath, Func<ApiRequest, ApiResponse> handler, bool requireAuth = true)
		{
			if (string.IsNullOrWhiteSpace(apiPath))
			{
				throw new ArgumentException("API路径不能为空", nameof(apiPath));
			}

			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			var apiHandler = new ApiHandler
			{
				Path = apiPath,
				Handler = handler,
				AddedTime = DateTime.UtcNow,
				RequireAuth = requireAuth
			};

			if (!_apiHandlers.TryAdd(apiPath, apiHandler))
			{
				throw new InvalidOperationException($"API '{apiPath}' 已经存在");
			}

			Console.WriteLine($"API已添加: {apiPath}" + (requireAuth ? "" : " (AllowAnonymous)"));
		}

		/// <summary>
		/// 添加异步API处理器
		/// </summary>
		/// <param name="apiPath">API路径</param>
		/// <param name="handler">异步处理函数</param>
		/// <param name="requireAuth">是否需要Token认证（默认true）</param>
		public void AddApi(string apiPath, Func<ApiRequest, Task<ApiResponse>> handler, bool requireAuth = true)
		{
			if (string.IsNullOrWhiteSpace(apiPath))
			{
				throw new ArgumentException("API路径不能为空", nameof(apiPath));
			}

			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			var apiHandler = new ApiHandler
			{
				Path = apiPath,
				AsyncHandler = handler,
				AddedTime = DateTime.UtcNow,
				IsAsync = true,
				RequireAuth = requireAuth
			};

			if (!_apiHandlers.TryAdd(apiPath, apiHandler))
			{
				throw new InvalidOperationException($"API '{apiPath}' 已经存在");
			}

			Console.WriteLine($"异步API已添加: {apiPath}" + (requireAuth ? "" : " (AllowAnonymous)"));
		}

		/// <summary>
		/// 删除API处理器
		/// </summary>
		/// <param name="apiPath">API路径</param>
		/// <returns>是否成功删除</returns>
		public bool DelApi(string apiPath)
		{
			if (string.IsNullOrWhiteSpace(apiPath))
			{
				throw new ArgumentException("API路径不能为空", nameof(apiPath));
			}

			var result = _apiHandlers.TryRemove(apiPath, out _);
			if (result)
			{
				Console.WriteLine($"API已删除: {apiPath}");
			}
			return result;
		}

		/// <summary>
		/// 获取所有已注册的API
		/// </summary>
		/// <returns>API路径列表</returns>
		public IReadOnlyList<string> GetRegisteredApis()
		{
			return _apiHandlers.Keys.ToList();
		}

		#endregion

		#region 私有方法 - 请求处理 & Token 认证（含 UserName 绑定）

		/// <summary>
		/// 监听循环
		/// </summary>
		private async Task ListenLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
			{
				try
				{
					var context = await _listener.GetContextAsync();

					// 将请求加入队列
					var requestContext = new ApiRequestContext
					{
						Context = context,
						RequestTime = DateTime.UtcNow,
						Id = Guid.NewGuid().ToString()
					};

					_requestQueue.Enqueue(requestContext);

					// 触发请求接收事件
					RequestReceived?.Invoke(this, new ApiRequestEventArgs
					{
						RequestId = requestContext.Id,
						Path = context.Request.Url?.AbsolutePath,
						Method = context.Request.HttpMethod,
						Time = requestContext.RequestTime
					});

					Console.WriteLine($"请求已入队: {context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");
				}
				catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
				{
					// 正常关闭
					break;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"监听错误: {ex.Message}");
					await Task.Delay(100, cancellationToken);
				}
			}
		}

		/// <summary>
		/// 处理队列循环
		/// </summary>
		private async Task ProcessQueueLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_requestQueue.TryDequeue(out var requestContext))
				{
					// 使用信号量控制并发
					await _requestSemaphore.WaitAsync(cancellationToken);

					// 处理请求
					_ = Task.Run(async () =>
					{
						try
						{
							await ProcessRequest(requestContext);
						}
						finally
						{
							_requestSemaphore.Release();
							Interlocked.Decrement(ref _activeRequests);
						}
					}, cancellationToken);

					Interlocked.Increment(ref _activeRequests);
				}
				else
				{
					// 队列为空，短暂等待
					await Task.Delay(10, cancellationToken);
				}
			}
		}

		/// <summary>
		/// 处理单个请求（含 Token 认证 + UserName 绑定）
		/// </summary>
		private async Task ProcessRequest(ApiRequestContext requestContext)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				var request = requestContext.Context.Request;
				var response = requestContext.Context.Response;
				var requestPath = request.Url?.AbsolutePath;

				// ---- 1. 查找 API 处理器 ----
				if (!_apiHandlers.TryGetValue(requestPath, out var handler))
				{
					await SendErrorResponse(response, 404, "API not found");
					return;
				}

				// ---- 2. Token 认证（若全局启用）----
				string extractedToken = null;
				bool isAuthenticated = false;
				string boundUserName = string.Empty;     // 新增：从 _tokens[token] 读出绑定用户名

				if (_tokenAuthEnabled)
				{
					bool pathInWhitelist = requestPath != null && _anonymousPaths.ContainsKey(requestPath);
					bool apiAllowsAnonymous = handler.RequireAuth == false;

					if (!pathInWhitelist && !apiAllowsAnonymous)
					{
						extractedToken = TryExtractToken(request);
						if (string.IsNullOrWhiteSpace(extractedToken) ||
							!_tokens.TryGetValue(extractedToken, out boundUserName))
						{
							// 认证失败 → 401（含友好提示）
							await SendErrorResponse(response, 401,
								string.IsNullOrWhiteSpace(extractedToken)
									? "Authentication required. Provide token via 'Authorization: Bearer <token>', 'X-Api-Key' header, or '?token=' query string."
									: "Invalid or expired token.");
							return;
						}
						isAuthenticated = true;
					}
					else
					{
						// 免认证接口：如带了合法 token，也解析身份（方便登录态识别）
						extractedToken = TryExtractToken(request);
						if (!string.IsNullOrWhiteSpace(extractedToken) &&
							_tokens.TryGetValue(extractedToken, out boundUserName))
						{
							isAuthenticated = true;
						}
						else
						{
							boundUserName = string.Empty;
						}
					}
				}

				// ---- 3. 构建 API 请求对象（含 UserName）----
				var apiRequest = new ApiRequest
				{
					Id = requestContext.Id,
					Path = requestPath,
					Method = request.HttpMethod,
					Headers = request.Headers,
					QueryString = request.QueryString,
					RemoteEndPoint = request.RemoteEndPoint?.ToString(),
					Body = await ReadRequestBody(request),
					// Token 认证相关
					IsAuthenticated = isAuthenticated,
					Token = extractedToken ?? string.Empty,
					// 新增：用户名（空串 = master token 或 未认证）
					UserName = boundUserName ?? string.Empty
				};

				// ---- 4. 执行处理器 ----
				ApiResponse apiResponse;
				if (handler.IsAsync && handler.AsyncHandler != null)
				{
					apiResponse = await handler.AsyncHandler(apiRequest);
				}
				else if (handler.Handler != null)
				{
					apiResponse = await Task.Run(() => handler.Handler(apiRequest));
				}
				else
				{
					apiResponse = ApiResponse.Error(500, "Handler not configured");
				}

				await SendApiResponse(response, apiResponse);

				sw.Stop();
				Console.WriteLine($"请求处理完成: {requestPath}, 耗时: {sw.ElapsedMilliseconds}ms"
					+ $", 认证: {(isAuthenticated ? "Y" : "-")}"
					+ (isAuthenticated && !string.IsNullOrEmpty(boundUserName) ? $" 用户:{boundUserName}" : ""));
			}
			catch (Exception ex)
			{
				sw.Stop();
				Console.WriteLine($"请求处理错误: {ex.Message}");

				try
				{
					await SendErrorResponse(requestContext.Context.Response, 500, $"Internal server error: {ex.Message}");
				}
				catch
				{
					// 忽略响应发送错误
				}
			}
			finally
			{
				// 触发请求完成事件
				RequestCompleted?.Invoke(this, new ApiRequestEventArgs
				{
					RequestId = requestContext.Id,
					Path = requestContext.Context.Request.Url?.AbsolutePath,
					Method = requestContext.Context.Request.HttpMethod,
					Time = DateTime.UtcNow
				});
			}
		}

		/// <summary>
		/// 从请求中提取 Token（多位置，按优先级）
		/// </summary>
		private static string TryExtractToken(HttpListenerRequest request)
		{
			// 1. Authorization Header: "Bearer xxx" or plain "xxx"
			string auth = request.Headers["Authorization"];
			if (!string.IsNullOrWhiteSpace(auth))
			{
				auth = auth.Trim();
				if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
				{
					string t = auth.Substring(7).Trim();
					if (!string.IsNullOrEmpty(t)) return t;
				}
				else
				{
					return auth;
				}
			}

			// 2. X-Api-Key Header
			string apiKey = request.Headers["X-Api-Key"];
			if (!string.IsNullOrWhiteSpace(apiKey))
				return apiKey.Trim();

			// 3. QueryString ?token=
			string qsToken = request.QueryString["token"];
			if (!string.IsNullOrWhiteSpace(qsToken))
				return qsToken.Trim();

			return null;
		}

		#endregion

		#region 私有方法 - 工具

		/// <summary>读取请求体</summary>
		private async Task<string> ReadRequestBody(HttpListenerRequest request)
		{
			if (!request.HasEntityBody) return null;
			using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
				return await reader.ReadToEndAsync();
		}

		/// <summary>发送API响应</summary>
		private async Task SendApiResponse(HttpListenerResponse response, ApiResponse apiResponse)
		{
			response.StatusCode = apiResponse.StatusCode;
			response.ContentType = apiResponse.ContentType ?? "application/json";

			if (apiResponse.Headers != null)
			{
				foreach (var header in apiResponse.Headers)
					response.Headers[header.Key] = header.Value;
			}

			var data = Encoding.UTF8.GetBytes(apiResponse.Body ?? string.Empty);
			response.ContentLength64 = data.Length;
			await response.OutputStream.WriteAsync(data, 0, data.Length);
			response.OutputStream.Close();
		}

		/// <summary>发送错误响应</summary>
		private async Task SendErrorResponse(HttpListenerResponse response, int statusCode, string message)
		{
			var errorResponse = new ApiResponse
			{
				StatusCode = statusCode,
				Body = $"{{\"error\":\"{EscapeJson(message)}\"}}",
				ContentType = "application/json"
			};
			await SendApiResponse(response, errorResponse);
		}

		/// <summary>
		/// 配置SSL证书（仅Windows：通过netsh http add sslcert绑定到http.sys，需管理员权限）。
		/// 非Windows平台：已在Open()入口处跳过调用；此处额外做防御：再检查平台 + try-catch，
		/// 避免直接被外部调用时因netsh不存在抛出未处理异常。
		/// </summary>
		private void ConfigureSslCertificate(int port, X509Certificate2 certificate)
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Console.WriteLine($"[SSL] ConfigureSslCertificate 在非Windows平台({RuntimeInformation.OSDescription})"
					+ " 不执行（无 netsh 命令）。请使用 Nginx/Caddy 前置反代做 TLS 卸载。");
				return;
			}

			var processInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "netsh",
				Arguments = $"http add sslcert ipport=0.0.0.0:{port} certhash={certificate.Thumbprint} appid={{{Guid.NewGuid()}}}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			try
			{
				var process = System.Diagnostics.Process.Start(processInfo);
				if (process != null)
				{
					process.WaitForExit();
					if (process.ExitCode != 0)
					{
						string err = process.StandardError.ReadToEnd();
						Console.WriteLine($"[SSL] SSL证书配置(netsh)失败，ExitCode={process.ExitCode}。请以管理员权限运行。{Environment.NewLine}  {err.Trim()}");
					}
					else
					{
						Console.WriteLine($"[SSL] 已用 netsh 绑定 SSL 证书到端口 {port} (thumbprint={certificate.Thumbprint})");
					}
				}
			}
			catch (Exception ex)
			{
				// 典型情况：netsh 不存在、权限不足、启动进程失败。避免服务器启动被阻断。
				Console.WriteLine($"[SSL] 执行 netsh 出现异常：{ex.Message}。SSL 证书绑定未生效。"
					+ " 请手动绑定或改用 Nginx/Caddy 做 TLS 卸载。");
			}
		}

		/// <summary>生成 32 字符的随机 Token（128-bit 熵）</summary>
		private static string GenerateRandomToken()
		{
			byte[] buf = new byte[16];
			using (var rng = RandomNumberGenerator.Create())
				rng.GetBytes(buf);
			return string.Concat(buf.Select(b => b.ToString("x2")));
		}

		/// <summary>JSON 字符串转义</summary>
		private static string EscapeJson(string s)
		{
			if (string.IsNullOrEmpty(s)) return s;
			var sb = new StringBuilder(s.Length);
			foreach (char c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < ' ') sb.Append($"\\u{(int)c:x4}");
						else sb.Append(c);
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// 密码加盐哈希（SHA256）。salt 包含用户名，避免同密码不同用户产生相同哈希。
		/// </summary>
		private static string HashPassword(string userName, string plainPassword)
		{
			string combined = USER_SALT + "|" + userName.Trim().ToLowerInvariant() + "|" + plainPassword;
			using (var sha = SHA256.Create())
			{
				byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
				return string.Concat(bytes.Select(b => b.ToString("x2")));
			}
		}

		/// <summary>
		/// 从 JObject 尝试获取指定字符串字段的值（精确匹配优先，其次忽略大小写）。
		/// </summary>
		private static string GetJsonString(JObject root, string key)
		{
			if (root == null) return null;

			// 精确匹配优先
			if (root.TryGetValue(key, StringComparison.Ordinal, out var exact) && exact.Type == JTokenType.String)
				return exact.ToString();

			// 忽略大小写
			if (root.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var ci) && ci.Type == JTokenType.String)
				return ci.ToString();

			return null;
		}

		#endregion

		#region IDisposable实现

		public void Dispose()
		{
			Close();
			_requestSemaphore?.Dispose();
			_cancellationTokenSource?.Dispose();
		}

		#endregion
	}

	#region 辅助类

	/// <summary>
	/// API请求上下文
	/// </summary>
	public class ApiRequestContext
	{
		public string Id { get; set; } = string.Empty;
		public HttpListenerContext Context { get; set; }
		public DateTime RequestTime { get; set; }
	}

	/// <summary>
	/// API处理器
	/// </summary>
	public class ApiHandler
	{
		public string Path { get; set; } = string.Empty;
		public Func<ApiRequest, ApiResponse> Handler { get; set; }
		public Func<ApiRequest, Task<ApiResponse>> AsyncHandler { get; set; }
		public DateTime AddedTime { get; set; }
		public bool IsAsync { get; set; }
		/// <summary>该接口是否需要Token认证（默认 true）</summary>
		public bool RequireAuth { get; set; } = true;
	}

	/// <summary>
	/// API请求对象
	/// </summary>
	public class ApiRequest
	{
		public string Id { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public string Method { get; set; } = string.Empty;
		public System.Collections.Specialized.NameValueCollection Headers { get; set; }
		public System.Collections.Specialized.NameValueCollection QueryString { get; set; }
		public string RemoteEndPoint { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;

		/// <summary>请求是否通过 Token 认证（全局认证未开启时恒为 false）</summary>
		public bool IsAuthenticated { get; set; }

		/// <summary>请求携带的原始 Token（未携带或认证失败返回空字符串）</summary>
		public string Token { get; set; } = string.Empty;

		/// <summary>
		/// 认证用户名。当请求携带的是登录颁发的"绑定用户的Token"时为此用户名；
		/// Master Token（全局配置的）或未认证时为 <see cref="string.Empty"/>。
		/// 业务层可用来实现二级操作鉴权：
		/// <code>
		/// if (req.UserName == "admin") 允许高权限操作；
		/// else if (string.IsNullOrEmpty(req.UserName) &amp;&amp; req.IsAuthenticated) Master 通行；
		/// else 拒绝；
		/// </code>
		/// </summary>
		public string UserName { get; set; } = string.Empty;
	}

	/// <summary>
	/// API响应对象
	/// </summary>
	public class ApiResponse
	{
		public int StatusCode { get; set; } = 200;
		public string Body { get; set; } = string.Empty;
		public string ContentType { get; set; } = "application/json";
		public Dictionary<string, string> Headers { get; set; }

		public static ApiResponse Ok(string body = null)
		{
			return new ApiResponse { StatusCode = 200, Body = body ?? string.Empty };
		}

		public static ApiResponse Error(int statusCode, string message)
		{
			return new ApiResponse
			{
				StatusCode = statusCode,
				Body = $"{{\"error\":\"{message}\"}}"
			};
		}
	}

	/// <summary>
	/// API请求事件参数
	/// </summary>
	public class ApiRequestEventArgs : EventArgs
	{
		public string RequestId { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public string Method { get; set; } = string.Empty;
		public DateTime Time { get; set; }
	}

	#endregion
}
