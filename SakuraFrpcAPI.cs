/*
 针对SakuraFrpc平台的隧道认证方法
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WeiKit
{
	public class SakuraFrpcAPI
	{
		/// <summary>
		/// 自动化隧道访问认证
		/// </summary>
		/// <param name="link">要访问的链接</param>
		/// <param name="pw">访问认证密钥</param>
		//public static void SakuraFrpAPIAuth(string link, string pw)
		//{
		//    string[] SakuraFrpAPILink = { $"curl -X POST {link} -d \"pw={pw}\" -d  \"{link}\" -d \"persist=on\"" };
		//    Console.WriteLine($"正在进行SakuraFrpc访问认证：{link}");
		//    Console.WriteLine(Tool.RunCmdCode(SakuraFrpAPILink));
		//}
		/// <summary>
		/// 自动化隧道访问认证。
		/// </summary>
		/// <param name="link">要访问的链接</param>
		/// <param name="pw">访问认证密钥</param>
		/// <returns>是否成功发起并收到响应（响应内容与状态码会打印到控制台）</returns>
		/// <remarks>
		/// 旧版本会把 <see cref="ServicePointManager.SecurityProtocol"/> 全局改成
		/// <c>Tls12 | Tls11 | Tls</c>（启用已废弃的 TLS 1.0/1.1）且<b>不会恢复</b>，
		/// 这会降低整个进程的安全性。现在只在调用期间临时放宽，调用结束后恢复原值。
		/// </remarks>
		public static async Task<bool> SakuraFrpAPIAuth(string link, string pw)
		{
			if (string.IsNullOrWhiteSpace(link))
			{
				Console.WriteLine("SakuraFrpc 访问认证失败：链接为空。");
				return false;
			}

			Console.WriteLine("正在进行SakuraFrpc访问认证：" + link);

			// 临时放宽 TLS 版本以兼容老旧服务端，调用结束后必须还原
			SecurityProtocolType original = ServicePointManager.SecurityProtocol;
			try
			{
				// 只在原有基础上补充 TLS1.1/1.2，不强制启用 TLS1.0
				ServicePointManager.SecurityProtocol = original | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

				using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
				using (var content = new FormUrlEncodedContent(new[]
				{
					new KeyValuePair<string, string>("pw", pw),
					new KeyValuePair<string, string>("", link),
					new KeyValuePair<string, string>("persist", "on")
				}))
				{
					try
					{
						var response = await client.PostAsync(link, content).ConfigureAwait(false);
						string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
						Console.WriteLine($"认证响应: {result}");
						Console.WriteLine($"状态码: {response.StatusCode}");
						return response.IsSuccessStatusCode;
					}
					catch (Exception ex)
					{
						Console.WriteLine($"认证失败: {ex.Message}");
						return false;
					}
				}
			}
			finally
			{
				// 还原全局 TLS 设置，避免影响进程内其它网络请求
				ServicePointManager.SecurityProtocol = original;
			}
		}
	}
}
