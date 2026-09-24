using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeiKit
{
	/// <summary>
	/// http协议基本连接方法
	/// </summary>
	public class HttpLink
	{
		/// <summary>
		/// 获取指定url的图片，失败返回null
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static async Task<Image> GetImageFromUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.Timeout = TimeSpan.FromSeconds(30);
					using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
					{
						if (response.IsSuccessStatusCode)
						{
							using (Stream stream = await response.Content.ReadAsStreamAsync())
							{
								return Image.FromStream(stream);
							}
						}
						return null;
					}
				}
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// 获取Http/s连接的客户端真实IP
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static string GetClientIP(HttpListenerRequest request)
		{
			if (request == null)
				return string.Empty;

			string ip = request.Headers["X-Forwarded-For"];
			if (string.IsNullOrEmpty(ip))
				ip = request.Headers["X-Real-IP"];
			if (string.IsNullOrEmpty(ip) && request.RemoteEndPoint != null)
				ip = request.RemoteEndPoint.Address.ToString();
			return ip ?? string.Empty;
		}

		/// <summary>
		/// 发送一个GET请求，如果请求失败则直接返回null
		/// </summary>
		/// <param name="relativeUrl">请求的URL</param>
		/// <returns>返回响应内容，失败时返回null</returns>
		public static string GetTask(string relativeUrl)
		{
			if (string.IsNullOrWhiteSpace(relativeUrl))
				return null;

			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = new Uri("https://www.example.com/");
				client.Timeout = TimeSpan.FromSeconds(30);
				try
				{
					HttpResponseMessage response = client.GetAsync(relativeUrl).Result;
					if (!response.IsSuccessStatusCode)
					{
						Console.WriteLine($"Request failed with status code: {response.StatusCode}");
						return null;
					}
					return response.Content.ReadAsStringAsync().Result;
				}
				catch (Exception e)
				{
					Console.WriteLine($"GET Request Error: {e.Message}");
					return null;
				}
			}
		}

		/// <summary>
		/// 发送一个POST请求并附加一个JSON参数
		/// </summary>
		/// <param name="Response"></param>
		/// <param name="JsonData"></param>
		/// <returns>返回响应内容</returns>
		public static string PostTask(string Response, string JsonData)
		{
			if (string.IsNullOrWhiteSpace(Response))
				return string.Empty;

			using (HttpClient client = new HttpClient())
			{
				try
				{
					client.Timeout = TimeSpan.FromSeconds(30);
					var content = new StringContent(JsonData ?? string.Empty, Encoding.UTF8, "application/json");
					HttpResponseMessage response = client.PostAsync(Response, content).Result;
					response.EnsureSuccessStatusCode();
					string responseBody = response.Content.ReadAsStringAsync().Result;
					Console.WriteLine(responseBody);
					return responseBody;
				}
				catch (Exception e)
				{
					Console.WriteLine($"POST Request Error: {e.Message}");
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// 下载工具，自动单/多线程下载，支持进度与状态查询
		/// </summary>
		public class DownTool : IDisposable
		{
			public enum DownMode
			{
				Auto,
				MultiThread,
				SingleThread,
			}

			/// <summary>
			/// 表示文件总大小，单位为字节
			/// </summary>
			public long TotalSize { get; private set; }

			/// <summary>
			/// 表示已下载大小，单位为字节
			/// </summary>
			public long DownloadedSize { get; private set; }

			/// <summary>
			/// 表示剩余下载大小，单位为字节
			/// </summary>
			public long RemainSize => TotalSize - DownloadedSize;

			/// <summary>
			/// 表示下载进度百分比
			/// </summary>
			public double DownloadProgress => TotalSize > 0 ? DownloadedSize * 100.0 / TotalSize : 0;

			/// <summary>
			/// 表示下载是否成功
			/// </summary>
			public bool IsSuccess { get; private set; }

			/// <summary>
			/// 表示是否正在下载
			/// </summary>
			public bool IsDownloading { get; private set; }

			/// <summary>
			/// 表示下载是否失败
			/// </summary>
			public bool IsFailed { get; private set; }

			/// <summary>
			/// 表示下载失败原因
			/// </summary>
			public string FailReason { get; private set; }

			/// <summary>
			/// 表示下载链接
			/// </summary>
			public string DownloadUrl { get; private set; }

			/// <summary>
			/// 表示下载保存路径
			/// </summary>
			public string DownFilePath { get; private set; }

			/// <summary>
			/// 表示下载模式
			/// </summary>
			public DownMode DownloadMode { get; private set; }

			/// <summary>
			/// 表示当前下载速度，单位为字节/秒
			/// </summary>
			public long Speed { get; private set; }

			/// <summary>
			/// 下载进度更新事件
			/// </summary>
			public event Action<double> ProgressChanged;

			/// <summary>
			/// 下载完成事件
			/// </summary>
			public event Action<bool, string> DownloadCompleted;

			private Thread downloadThread;
			private List<Thread> Threads = new List<Thread>();
			private Thread MonitorThread;
			private int numberOfThreads;
			private string tempSavePath;
			private long[] partDownloadedSize;
			private DateTime _lastSpeedTime = DateTime.MinValue;
			private long _lastSpeedDownloaded = 0;
			private readonly object _lockObj = new object();
			private bool _isDisposed = false;
			private CancellationTokenSource _cts;
			private bool _mergeCompleted = false;

			/// <summary>
			/// 获取下载文件的大小，失败返回0
			/// </summary>
			public long GetDownloadSize(string url)
			{
				if (string.IsNullOrWhiteSpace(url))
					return 0;

				try
				{
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
					request.Method = "HEAD";
					request.Timeout = 10000;
					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					{
						return response.ContentLength;
					}
				}
				catch
				{
					return 0;
				}
			}

			/// <summary>
			/// 开始下载文件
			/// </summary>
			public void StartDownload(string url, string savePath, DownMode mode = DownMode.Auto)
			{
				if (IsDownloading) return;

				if (string.IsNullOrWhiteSpace(url) || !(url.StartsWith("http://") || url.StartsWith("https://")))
				{
					IsFailed = true;
					IsDownloading = false;
					FailReason = $"无效的下载链接: {url}";
					DownloadCompleted?.Invoke(false, FailReason);
					return;
				}

				if (string.IsNullOrWhiteSpace(savePath))
				{
					IsFailed = true;
					IsDownloading = false;
					FailReason = "保存路径无效";
					DownloadCompleted?.Invoke(false, FailReason);
					return;
				}

				// 确保目录存在
				string directory = Path.GetDirectoryName(savePath);
				if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				lock (_lockObj)
				{
					IsDownloading = true;
					IsSuccess = false;
					IsFailed = false;
					FailReason = null;
					DownloadedSize = 0;
					Speed = 0;
					DownloadUrl = url;
					DownFilePath = savePath;
					DownloadMode = mode;
					_mergeCompleted = false;
					_cts = new CancellationTokenSource();
				}

				TotalSize = GetDownloadSize(url);
				if (TotalSize <= 0)
				{
					lock (_lockObj)
					{
						IsFailed = true;
						IsDownloading = false;
						FailReason = $"获取目标文件大小失败，链接无效或服务器不支持 HEAD 请求: {url}";
					}
					DownloadCompleted?.Invoke(false, FailReason);
					return;
				}

				downloadThread = new Thread(() => DoDownload(url, savePath, mode));
				downloadThread.IsBackground = true;
				downloadThread.Start();
			}

			private void DoDownload(string url, string savePath, DownMode mode)
			{
				try
				{
					lock (_lockObj)
					{
						if (mode == DownMode.Auto)
						{
							DownloadMode = (TotalSize > 100 * 1024 * 1024) ? DownMode.MultiThread : DownMode.SingleThread;
						}
						else
						{
							DownloadMode = mode;
						}
					}

					if (DownloadMode == DownMode.MultiThread && TotalSize > 1024 * 1024) // 至少1MB才使用多线程
					{
						StartMultiThreadDownload(savePath);
					}
					else
					{
						StartSingleThreadDownload(savePath);
					}
				}
				catch (Exception ex)
				{
					lock (_lockObj)
					{
						IsFailed = true;
						IsDownloading = false;
						FailReason = ex.Message;
					}
					DownloadCompleted?.Invoke(false, FailReason);
				}
			}

			private void StartSingleThreadDownload(string savePath)
			{
				Thread thread = new Thread(() =>
				{
					HttpWebRequest request = null;
					HttpWebResponse response = null;
					Stream responseStream = null;
					FileStream fileStream = null;
					try
					{
						lock (_lockObj)
						{
							_lastSpeedTime = DateTime.Now;
							_lastSpeedDownloaded = 0;
						}

						request = (HttpWebRequest)WebRequest.Create(DownloadUrl);
						request.Timeout = 30000;
						response = (HttpWebResponse)request.GetResponse();
						TotalSize = response.ContentLength;
						responseStream = response.GetResponseStream();

						// 使用FileShare.Read允许其他进程读取
						fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.Read);

						byte[] buffer = new byte[8192];
						int bytesRead;
						long totalRead = 0;

						while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
						{
							if (_cts?.IsCancellationRequested == true)
							{
								fileStream.Close();
								File.Delete(savePath);
								lock (_lockObj)
								{
									IsFailed = true;
									IsDownloading = false;
									FailReason = "下载已取消";
								}
								DownloadCompleted?.Invoke(false, FailReason);
								return;
							}

							fileStream.Write(buffer, 0, bytesRead);
							totalRead += bytesRead;

							lock (_lockObj)
							{
								DownloadedSize = totalRead;
								// 计算速度
								var now = DateTime.Now;
								var elapsed = (now - _lastSpeedTime).TotalSeconds;
								if (elapsed >= 1.0)
								{
									Speed = (long)((DownloadedSize - _lastSpeedDownloaded) / elapsed);
									_lastSpeedDownloaded = DownloadedSize;
									_lastSpeedTime = now;
								}
							}

							ProgressChanged?.Invoke(DownloadProgress);
						}

						lock (_lockObj)
						{
							Speed = 0;
							IsSuccess = true;
							IsDownloading = false;
							IsFailed = false;
						}
						DownloadCompleted?.Invoke(true, null);
					}
					catch (Exception ex)
					{
						lock (_lockObj)
						{
							IsFailed = true;
							IsDownloading = false;
							FailReason = $"HttpWebRequest异常: {ex.Message} | 链接: {DownloadUrl} | 路径: {savePath}";
						}
						DownloadCompleted?.Invoke(false, FailReason);
					}
					finally
					{
						responseStream?.Dispose();
						fileStream?.Dispose();
						response?.Dispose();
					}
				});
				thread.IsBackground = true;
				thread.Start();
			}

			private void StartMultiThreadDownload(string savePath)
			{
				// 计算线程数
				numberOfThreads = (int)Math.Min(TotalSize / (20 * 1024 * 1024), 12);
				numberOfThreads = Math.Max(numberOfThreads, 4);
				numberOfThreads = Math.Min(numberOfThreads, (int)Math.Ceiling(TotalSize / (1024.0 * 1024))); // 每文件至少1MB

				long partSize = TotalSize / numberOfThreads;
				long remainingBytes = TotalSize % numberOfThreads;
				Threads = new List<Thread>();
				tempSavePath = savePath;

				lock (_lockObj)
				{
					partDownloadedSize = new long[numberOfThreads];
					_lastSpeedTime = DateTime.Now;
					_lastSpeedDownloaded = 0;
				}

				for (int i = 0; i < numberOfThreads; i++)
				{
					long start = i * partSize;
					long end = (i == numberOfThreads - 1) ? (start + partSize + remainingBytes - 1) : (start + partSize - 1);
					int partNum = i;
					Threads.Add(new Thread(() => DownloadPart(start, end, partNum, savePath)));
					Threads[Threads.Count - 1].IsBackground = true;
					Threads[Threads.Count - 1].Start();
					Thread.Sleep(50);
				}

				MonitorThread = new Thread(() => _MonitorThread(savePath, numberOfThreads));
				MonitorThread.IsBackground = true;
				MonitorThread.Start();
			}

			private void _MonitorThread(string savePath, int threadpcs)
			{
				int StopThread = 0;
				try
				{
					while (!_mergeCompleted)
					{
						if (_cts?.IsCancellationRequested == true)
						{
							lock (_lockObj)
							{
								IsFailed = true;
								IsDownloading = false;
								FailReason = "下载已取消";
							}
							DownloadCompleted?.Invoke(false, FailReason);
							return;
						}

						lock (_lockObj)
						{
							if (partDownloadedSize != null)
							{
								long sum = 0;
								for (int i = 0; i < partDownloadedSize.Length; i++)
									sum += partDownloadedSize[i];
								DownloadedSize = Math.Min(sum, TotalSize);

								var now = DateTime.Now;
								var elapsed = (now - _lastSpeedTime).TotalSeconds;
								if (elapsed >= 1.0)
								{
									Speed = (long)((DownloadedSize - _lastSpeedDownloaded) / elapsed);
									_lastSpeedDownloaded = DownloadedSize;
									_lastSpeedTime = now;
								}
							}
						}
						ProgressChanged?.Invoke(DownloadProgress);

						Thread.Sleep(500);

						StopThread = 0;
						foreach (var s in Threads)
						{
							if (s.ThreadState == ThreadState.Stopped || s.ThreadState == ThreadState.Aborted)
								StopThread++;
						}

						if (StopThread == threadpcs)
						{
							try
							{
								MergeFiles(savePath, threadpcs);
								_mergeCompleted = true;

								lock (_lockObj)
								{
									DownloadedSize = TotalSize;
									Speed = 0;
									IsSuccess = true;
									IsDownloading = false;
									IsFailed = false;
								}
								ProgressChanged?.Invoke(100);
								DownloadCompleted?.Invoke(true, null);
							}
							catch (Exception ex)
							{
								lock (_lockObj)
								{
									IsSuccess = false;
									IsDownloading = false;
									IsFailed = true;
									FailReason = ex.Message;
								}
								DownloadCompleted?.Invoke(false, FailReason);
							}
							return;
						}
					}
				}
				catch (Exception ex)
				{
					lock (_lockObj)
					{
						IsFailed = true;
						IsDownloading = false;
						FailReason = ex.Message;
					}
					DownloadCompleted?.Invoke(false, FailReason);
				}
			}

			private void DownloadPart(long start, long end, int partNumber, string savePath)
			{
				int maxRetries = 3;
				int retryCount = 0;
				long partDownloaded = 0;

				while (retryCount <= maxRetries)
				{
					try
					{
						if (_cts?.IsCancellationRequested == true)
							return;

						HttpWebRequest request = (HttpWebRequest)WebRequest.Create(DownloadUrl);
						request.Timeout = 30000;
						request.ReadWriteTimeout = 30000;
						request.AddRange(start, end);

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						using (var stream = response.GetResponseStream())
						using (var fileStream = new FileStream($"{savePath}.part{partNumber}", FileMode.Create, FileAccess.Write, FileShare.Read))
						{
							byte[] buffer = new byte[8192];
							int bytesRead;
							partDownloaded = 0;

							while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
							{
								if (_cts?.IsCancellationRequested == true)
									return;

								fileStream.Write(buffer, 0, bytesRead);
								partDownloaded += bytesRead;
								lock (_lockObj)
								{
									if (partDownloadedSize != null && partNumber < partDownloadedSize.Length)
										partDownloadedSize[partNumber] = partDownloaded;
								}
							}
						}
						return;
					}
					catch
					{
						retryCount++;
						if (retryCount > maxRetries)
						{
							lock (_lockObj)
							{
								IsFailed = true;
								IsDownloading = false;
								FailReason = $"线程{partNumber}下载失败，已超过最大重试次数({maxRetries})";
							}
							DownloadCompleted?.Invoke(false, FailReason);
							return;
						}
						Thread.Sleep(1000 * retryCount);
					}
				}
			}

			private void MergeFiles(string savePath, int totalParts)
			{
				string tempDir = Path.GetDirectoryName(savePath);
				using (FileStream resultStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					for (int i = 0; i < totalParts; i++)
					{
						if (_cts?.IsCancellationRequested == true)
							throw new OperationCanceledException("合并已取消");

						string partFilePath = $"{savePath}.part{i}";
						if (!File.Exists(partFilePath))
						{
							throw new InvalidOperationException($"[错误] 部分文件丢失: {partFilePath}");
						}

						using (FileStream partStream = new FileStream(partFilePath, FileMode.Open, FileAccess.Read))
						{
							byte[] buffer = new byte[8192];
							int bytesRead;
							while ((bytesRead = partStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								resultStream.Write(buffer, 0, bytesRead);
							}
						}
						File.Delete(partFilePath);
					}
				}
			}

			/// <summary>
			/// 取消下载
			/// </summary>
			public void CancelDownload()
			{
				lock (_lockObj)
				{
					_cts?.Cancel();
				}
			}

			/// <summary>
			/// 释放资源
			/// </summary>
			public void Dispose()
			{
				if (!_isDisposed)
				{
					_isDisposed = true;
					_cts?.Cancel();
					_cts?.Dispose();
					_cts = null;

					// 清理临时文件
					try
					{
						if (!string.IsNullOrEmpty(DownFilePath))
						{
							for (int i = 0; i < 100; i++)
							{
								string partFile = $"{DownFilePath}.part{i}";
								if (File.Exists(partFile))
									File.Delete(partFile);
							}
						}
					}
					catch { }
				}
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// 上传工具，支持调用upload.php上传文件，支持进度与状态查询
		/// </summary>
		public class UploadTool : IDisposable
		{
			public long TotalSize { get; private set; }
			public long UploadedSize { get; private set; }
			public long RemainSize => TotalSize - UploadedSize;
			public double UploadProgress => TotalSize > 0 ? (UploadedSize * 100.0 / TotalSize) : 0;
			public bool IsSuccess { get; private set; }
			public bool IsUploading { get; private set; }
			public bool IsFailed { get; private set; }
			public string FailReason { get; private set; }
			public string ResponseText { get; private set; }

			public event Action<double> ProgressChanged;
			public event Action<bool, string> UploadCompleted;

			private Thread uploadThread;
			private readonly object _lockObj = new object();
			private bool _isDisposed = false;
			private CancellationTokenSource _cts;
			private const int BUFFER_SIZE = 8192;
			private const int TIMEOUT_SECONDS = 120;

			/// <summary>
			/// 开始上传文件
			/// </summary>
			public void UploadFile(string uploadUrl, string filePath, string formFileField = "file")
			{
				if (IsUploading) return;

				if (string.IsNullOrWhiteSpace(uploadUrl))
				{
					lock (_lockObj)
					{
						IsFailed = true;
						FailReason = "上传地址无效";
					}
					UploadCompleted?.Invoke(false, FailReason);
					return;
				}

				if (!File.Exists(filePath))
				{
					lock (_lockObj)
					{
						IsFailed = true;
						FailReason = "文件不存在";
					}
					UploadCompleted?.Invoke(false, FailReason);
					return;
				}

				lock (_lockObj)
				{
					IsUploading = true;
					IsSuccess = false;
					IsFailed = false;
					FailReason = null;
					ResponseText = null;
					UploadedSize = 0;
					TotalSize = new FileInfo(filePath).Length;
					_cts = new CancellationTokenSource();
				}

				uploadThread = new Thread(() => DoUpload(uploadUrl, filePath, formFileField));
				uploadThread.IsBackground = true;
				uploadThread.Start();
			}

			private void DoUpload(string uploadUrl, string filePath, string formFileField)
			{
				try
				{
					string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
					byte[] boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary + "\r\n");
					byte[] trailer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uploadUrl);
					request.Method = "POST";
					request.ContentType = "multipart/form-data; boundary=" + boundary;
					request.KeepAlive = true;
					request.Timeout = TIMEOUT_SECONDS * 1000;
					request.ReadWriteTimeout = TIMEOUT_SECONDS * 1000;

					using (Stream requestStream = request.GetRequestStream())
					{
						string header = $"Content-Disposition: form-data; name=\"{formFileField}\"; filename=\"{Path.GetFileName(filePath)}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
						byte[] headerBytes = Encoding.UTF8.GetBytes(header);

						requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
						requestStream.Write(headerBytes, 0, headerBytes.Length);

						using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
						{
							byte[] buffer = new byte[BUFFER_SIZE];
							int bytesRead;
							while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
							{
								if (_cts?.IsCancellationRequested == true)
								{
									throw new OperationCanceledException("上传已取消");
								}

								requestStream.Write(buffer, 0, bytesRead);

								lock (_lockObj)
								{
									UploadedSize += bytesRead;
								}
								ProgressChanged?.Invoke(UploadProgress);
							}
						}
						requestStream.Write(trailer, 0, trailer.Length);
						requestStream.Flush();
					}

					using (WebResponse response = request.GetResponse())
					using (Stream responseStream = response.GetResponseStream())
					using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
					{
						ResponseText = reader.ReadToEnd();
					}

					lock (_lockObj)
					{
						IsSuccess = true;
						IsUploading = false;
						IsFailed = false;
					}
					UploadCompleted?.Invoke(true, ResponseText);
				}
				catch (OperationCanceledException)
				{
					lock (_lockObj)
					{
						IsSuccess = false;
						IsUploading = false;
						IsFailed = true;
						FailReason = "上传已取消";
					}
					UploadCompleted?.Invoke(false, FailReason);
				}
				catch (Exception ex)
				{
					lock (_lockObj)
					{
						IsSuccess = false;
						IsUploading = false;
						IsFailed = true;
						FailReason = ex.Message;
					}
					UploadCompleted?.Invoke(false, FailReason);
				}
			}

			/// <summary>
			/// 取消上传
			/// </summary>
			public void CancelUpload()
			{
				lock (_lockObj)
				{
					_cts?.Cancel();
				}
			}

			/// <summary>
			/// 释放资源
			/// </summary>
			public void Dispose()
			{
				if (!_isDisposed)
				{
					_isDisposed = true;
					_cts?.Cancel();
					_cts?.Dispose();
					_cts = null;
				}
				GC.SuppressFinalize(this);
			}
		}
	}
}