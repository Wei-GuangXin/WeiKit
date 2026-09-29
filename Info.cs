using System;

namespace WeiKit
{
	/// <summary>
	/// 库信息
	/// </summary>
	public class Info
	{
		/// <summary>
		/// 库名称
		/// </summary>
		public const string LibraryName = "WeiKit Library";
		/// <summary>
		/// 主页网址
		/// </summary>
		public const string LibraryUrl = "https://www-home.rsie3cp43.nyat.app:37071/";
		/// <summary>
		/// 库作者
		/// </summary>
		public const string LibraryAuthor = "Wei Guang Xin";

		private const bool Isrelease = true;

		private static string _version;

		/// <summary>
		/// 库版本
		/// 格式如下：
		/// 发行版：20260929RLSxxxx
		/// 开发版：20260929DEVxxxx
		/// 解释：
		/// 前8位为日期，9~11位为版本类型&分隔符，后面的序列号代表同一天内的版本迭代次数。
		/// 未显式赋值时自动生成：日期取程序集编译时间，版本类型由 Isrelease 决定（RLS / DEV），
		/// 序列号取编译时刻的时分秒（HHmmss）；编译时间获取失败时显示「未知版本」。
		/// </summary>
		public static string Version
		{
			get
			{
				if (string.IsNullOrEmpty(_version))
					_version = BuildVersion();
				return _version;
			}

			set
			{
				_version = value;
			}
		}

		/// <summary>
		/// 自动生成版本号：yyyyMMdd + 版本类型（RLS/DEV） + 序列号（HHmmss）。
		/// 编译时间获取失败时返回「未知版本」，不回退到当前时间。
		/// </summary>
		private static string BuildVersion()
		{
			DateTime buildTime = ProgTool.GetBuildTime();
			string type = Isrelease ? "RLS" : "DEV";
			if (buildTime == DateTime.MinValue)
				return "未知版本" + type;
			return buildTime.ToString("yyyyMMdd") + type + buildTime.ToString("HHmmss");
		}
	}
}
