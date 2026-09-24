using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeiKit
{
	/// <summary>
	/// 钉钉自定义机器人消息发送。
	/// </summary>
	public static class DingDingAPI
	{
		/// <summary>
		/// 共享的 HttpClient。HttpClient 设计为可重用，反复 new 会逐步耗尽 socket（TIME_WAIT）。
		/// </summary>
		private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

		/// <summary>
		/// 发送 markdown 消息给钉钉自定义机器人。
		/// </summary>
		/// <param name="webhookUrl">钉钉自定义机器人 Webhook 地址</param>
		/// <param name="message">消息正文（markdown 格式）</param>
		/// <param name="titles">消息标题</param>
		/// <returns>是否发送成功（服务端 errcode == 0）</returns>
		/// <remarks>
		/// 消息类型固定为 markdown，且不支持 @ 指定人（at 段为空、isAtAll=false）。
		/// 失败原因会打印到控制台（配合 Logs.RedirectConsoleOut 可进入日志）。
		/// </remarks>
		public static async Task<bool> SendDingDingMessageAsync(string webhookUrl, string message, string titles = "Message")
		{
			if (string.IsNullOrWhiteSpace(webhookUrl))
			{
				Console.WriteLine("发送钉钉消息失败：Webhook 地址为空。");
				return false;
			}

			try
			{
				var content = new
				{
					msgtype = "markdown",
					markdown = new
					{
						title = titles,
						text = message
					},
					at = new
					{
						atMobiles = new string[0],
						atUserIds = new string[0],
						isAtAll = false
					}
				};

				var jsonContent = JsonConvert.SerializeObject(content);
				using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
				using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
				{
					HttpResponseMessage response = await client.PostAsync(webhookUrl, httpContent, cts.Token).ConfigureAwait(false);

					if (response.IsSuccessStatusCode)
					{
						string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
						dynamic result = JsonConvert.DeserializeObject(responseBody);
						// 根据钉钉API文档，errcode为0表示成功
						return result != null && result.errcode == 0;
					}

					Console.WriteLine($"发送钉钉消息失败：HTTP {(int)response.StatusCode} {response.StatusCode}");
					return false;
				}
			}
			catch (TaskCanceledException)
			{
				Console.WriteLine("发送钉钉消息超时（5 秒）。");
				return false;
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("发送钉钉消息已取消。");
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"发送钉钉消息发生错误：{ex.Message}");
				return false;
			}
		}
	}
}
