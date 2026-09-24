using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace WeiKit
{
	/// <summary>
	/// 程序工具类
	/// </summary>
	public class ProgTool
	{
		/// <summary>
		/// 检查当前程序是否具有管理员权限
		/// </summary>
		/// <returns></returns>
		public static bool IsRunAsAdmin()
		{
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		///  以管理员权限重新启动程序
		/// </summary>
		public static void RunAsAdmin()
		{
			// 获取当前进程的可执行文件名（确保是.exe文件）
			string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

			ProcessStartInfo procInfo = new ProcessStartInfo();
			procInfo.UseShellExecute = true;
			procInfo.WorkingDirectory = Environment.CurrentDirectory;
			procInfo.FileName = exePath;  // 确保这是.exe文件路径
			procInfo.Verb = "runas";  // 这个动词表示需要提升权限
			try
			{
				Process.Start(procInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine("无法以管理权限重新启动。", ex.Message);
				return;
			}
			Environment.Exit(1);  // 关闭原始进程
		}
		/// <summary>
		/// 程序重启
		/// </summary>
		public static void ProgramRestart()
		{
			// 获取当前进程的可执行文件名（确保是.exe文件）
			string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			ProcessStartInfo procInfo = new ProcessStartInfo();
			procInfo.UseShellExecute = true;
			procInfo.WorkingDirectory = Environment.CurrentDirectory;
			procInfo.FileName = exePath;  // 确保这是.exe文件路径
			procInfo.Verb = "";
			try
			{
				Process.Start(procInfo);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to restart application with administrative privileges: {0}", ex.Message);
			}
			Environment.Exit(1);  // 关闭原始进程
		}

		/// <summary>
		/// 程序重启 - 先关闭当前程序，等待一段时间后再启动新程序
		/// </summary>
		/// <param name="delayMilliseconds">关闭后等待的时间（毫秒）</param>
		public static void ProgramRestartWithDelay(int delayMilliseconds = 2000)
		{
			try
			{
				// 获取当前进程的可执行文件名
				string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

				// 创建一个独立的启动进程
				System.Diagnostics.ProcessStartInfo procInfo = new System.Diagnostics.ProcessStartInfo
				{
					UseShellExecute = true,
					WorkingDirectory = Environment.CurrentDirectory,
					FileName = "cmd.exe",  // 使用cmd来执行延迟启动
					Arguments = $"/c timeout /t {delayMilliseconds / 1000} /nobreak && start \"\" \"{exePath}\"",
					WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
					CreateNoWindow = true
				};

				// 启动延迟重启进程
				System.Diagnostics.Process.Start(procInfo);

				Console.WriteLine($"程序将在关闭后等待 {delayMilliseconds / 1000} 秒重启...");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"准备重启失败: {ex.Message}");
			}

			// 立即关闭当前程序
			Environment.Exit(0);
		}


		/// <summary>
		/// 生成基于硬件信息的唯一机器码（仅适用于Windows平台）
		/// </summary>
		/// <returns>64位小写十六进制字符串形式的机器码</returns>
		public static string GenerateMachineCode()
		{
			string cpuId = string.Empty;
			string motherboardSerial = string.Empty;
			string hardDiskSerial = string.Empty;
			string macAddress = string.Empty;

			try
			{
				using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
				{
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
					{
						cpuId = obj["ProcessorId"]?.ToString();
						if (!string.IsNullOrEmpty(cpuId)) break;
					}
				}
			}
			catch { }

			try
			{
				using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
				{
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
					{
						motherboardSerial = obj["SerialNumber"]?.ToString();
						if (!string.IsNullOrEmpty(motherboardSerial)) break;
					}
				}
			}
			catch { }

			try
			{
				using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
				{
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
					{
						hardDiskSerial = obj["SerialNumber"]?.ToString();
						if (!string.IsNullOrEmpty(hardDiskSerial)) break;
					}
				}
			}
			catch { }

			try
			{
				using (var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=TRUE"))
				{
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
					{
						macAddress = obj["MACAddress"]?.ToString();
						if (!string.IsNullOrEmpty(macAddress)) break;
					}
				}
				macAddress = macAddress.Replace(":", "").Replace("-", "").Trim();
			}
			catch { }

			// 组合所有可用信息
			string rawString = $"{cpuId}_{motherboardSerial}_{hardDiskSerial}_{macAddress}";

			// 如果全部为空，则回退到机器名
			if (string.IsNullOrWhiteSpace(rawString.Replace("_", "")))
			{
				rawString = Environment.MachineName;
			}

			// 使用SHA256生成固定长度哈希
			using (var sha256 = SHA256.Create())
			{
				byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawString));
				StringBuilder sb = new StringBuilder();
				foreach (byte b in hashBytes)
				{
					sb.Append(b.ToString("x2"));
				}
				return sb.ToString(); // 返回64位小写十六进制字符串
			}
		}

		/// <summary>
		/// 获取当前程序的编译时间。
		/// 依次尝试：① 程序集 PE 头的链接器时间戳；② 文件最后写入时间；③ 文件创建时间。
		/// </summary>
		/// <returns>编译时间（本地时间）；全部方式失败时返回 <see cref="DateTime.MinValue"/></returns>
		/// <remarks>
		/// 启用「确定性构建」（本项目的 <c>Deterministic=true</c>）时，PE 时间戳可能被写成固定值或 0，
		/// 旧版本会因此返回 1961 年之类的错误时间。现在会校验时间戳合理性并在无效时自动回退。
		/// 若需要精确的版本信息，建议改用 <c>Assembly.GetName().Version</c>。
		/// </remarks>
		public static DateTime GetBuildTime()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string filePath = assembly.Location;

			// ① PE 头时间戳
			try
			{
				const int peHeaderOffset = 60;
				const int linkTimeStampOffset = 8;

				byte[] bytes = new byte[2048];
				using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					int read = file.Read(bytes, 0, bytes.Length);
					if (read < peHeaderOffset + 4) throw new InvalidDataException("文件头不完整。");
				}

				int headerOffset = BitConverter.ToInt32(bytes, peHeaderOffset);
				if (headerOffset > 0 && headerOffset + linkTimeStampOffset + 4 <= bytes.Length)
				{
					int timeStamp = BitConverter.ToInt32(bytes, headerOffset + linkTimeStampOffset);
					// 时间戳过小（早于 2000-01-01 UTC）视为无效
					if (timeStamp > 946684800)
					{
						DateTime t = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
							.AddSeconds(timeStamp)
							.ToLocalTime();
						if (t <= DateTime.Now.AddDays(1)) return t;
					}
				}
			}
			catch { /* 继续尝试其它来源 */ }

			// ② 文件最后写入时间
			try
			{
				DateTime t = File.GetLastWriteTime(filePath);
				if (t.Year > 2000 && t <= DateTime.Now.AddDays(1)) return t;
			}
			catch { }

			// ③ 文件创建时间
			try
			{
				DateTime t = File.GetCreationTime(filePath);
				if (t.Year > 2000 && t <= DateTime.Now.AddDays(1)) return t;
			}
			catch { }

			return DateTime.MinValue;
		}

		public static string GetAppNameFromFile()
		{
			return System.IO.Path.GetFileNameWithoutExtension(
				System.Windows.Forms.Application.ExecutablePath);
		}

		/// <summary>
		/// 卸载通过ClickOnce发布的程序（适用于Windows平台）。此方法会查找当前用户的注册表中与指定显示名称匹配的卸载信息，并执行卸载命令。请确保提供正确的应用程序显示名称，并注意卸载过程需要用户确认。此方法仅适用于通过ClickOnce发布的应用程序，其他类型的安装可能无法正确卸载。
		/// </summary>
		/// <param name="appDisplayName"></param>
		public static void UninstallClickOnce(string appDisplayName)
		{
			string uninstallString = null;
			string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
			using (RegistryKey uninstallRoot = Registry.CurrentUser.OpenSubKey(registryKey))
			{
				if (uninstallRoot != null)
				{
					foreach (string subKeyName in uninstallRoot.GetSubKeyNames())
					{
						using (RegistryKey subKey = uninstallRoot.OpenSubKey(subKeyName))
						{
							if (subKey != null)
							{
								object displayName = subKey.GetValue("DisplayName");
								if (displayName != null && displayName.ToString().Equals(appDisplayName))
								{
									uninstallString = subKey.GetValue("UninstallString")?.ToString();
									break;
								}
							}
						}
					}
				}
			}

			if (!string.IsNullOrEmpty(uninstallString))
			{
				// 1. 先退出当前应用程序
				System.Windows.Forms.Application.Exit();

				// 2. 启动卸载进程
				System.Diagnostics.Process.Start("cmd.exe", $"/c {uninstallString}");

				// 注意：此时卸载对话框仍会出现，需要用户点击确认。
			}
		}

		/// <summary>
		/// 创建一个自删除的批处理脚本，用于在主程序退出后删除被占用的文件。此方法会生成一个临时批处理文件，持续检查指定的文件是否仍被占用，并在主程序退出后尝试删除这些文件。批处理脚本会显示一个窗口，提示用户等待文件解除占用，并在成功删除所有文件后自动清理自身。请确保提供正确的文件路径，并注意此方法仅适用于Windows平台。
		/// </summary>
		/// <param name="lockedFiles"></param>
		public static void CreateSelfDeleteScript(string[] lockedFiles)
		{
			try
			{
				// 创建一个临时批处理文件
				string scriptPath = Path.Combine(Path.GetTempPath(), $"cleanup_{DateTime.Now:yyyyMMddHHmmss}.bat");

				// 构建批处理内容
				StringBuilder script = new StringBuilder();
				script.AppendLine("@echo off");
				script.AppendLine("title 文件清理程序");
				script.AppendLine("echo 正在等待文件解除占用...");
				script.AppendLine("echo 请不要关闭此窗口");
				script.AppendLine("echo.");

				// 添加要删除的文件列表
				for (int i = 0; i < lockedFiles.Length; i++)
				{
					script.AppendLine($"echo {i + 1}. 等待删除: {Path.GetFileName(lockedFiles[i])}");
				}
				script.AppendLine("echo.");

				// 主循环：持续检查并尝试删除文件
				script.AppendLine(":LOOP");
				script.AppendLine("set DELETED_COUNT=0");
				script.AppendLine("set TOTAL_COUNT=0");

				// 为每个文件添加删除尝试
				for (int i = 0; i < lockedFiles.Length; i++)
				{
					string file = lockedFiles[i];
					string fileQuoted = $"\"{file}\"";

					script.AppendLine($@"
if exist {fileQuoted} (
    set /a TOTAL_COUNT+=1
    del /f /q {fileQuoted} 2>nul
    if not exist {fileQuoted} (
        set /a DELETED_COUNT+=1
        echo [成功] 已删除: {Path.GetFileName(file)}
    ) else (
        echo [等待] 文件被占用: {Path.GetFileName(file)}
    )
)");
				}

				// 检查是否所有文件都已删除
				script.AppendLine(@"
echo.
if %DELETED_COUNT% EQU %TOTAL_COUNT% (
    if %TOTAL_COUNT% GTR 0 (
        echo 所有文件已成功删除！
    ) else (
        echo 没有找到需要删除的文件。
    )
    goto CLEANUP
)");

				// 等待主程序退出
				script.AppendLine(@"
echo 还有文件被占用，等待主程序退出...
echo 请关闭主程序后继续...
echo.
timeout /t 3 /nobreak >nul
goto LOOP");

				// 清理自身
				script.AppendLine(@"
:CLEANUP
echo.
echo 正在清理临时文件...
timeout /t 2 /nobreak >nul");

				// 删除自身（使用特殊的自删除技巧）
				script.AppendLine($@"
del /f /q ""{scriptPath}"" 2>nul
if exist ""{scriptPath}"" (
    echo 正在创建自删除任务...
    echo @echo off > ""%temp%\final_cleanup.bat""
    echo timeout /t 1 /nobreak >nul >> ""%temp%\final_cleanup.bat""
    echo del /f /q ""{scriptPath}"" >> ""%temp%\final_cleanup.bat""
    echo del /f /q ""%temp%\final_cleanup.bat"" >> ""%temp%\final_cleanup.bat""
    start /b """" ""%temp%\final_cleanup.bat""
)
exit");

				// 写入批处理文件
				File.WriteAllText(scriptPath, script.ToString(), Encoding.Default);

				// 运行批处理文件
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = scriptPath,
					UseShellExecute = true,
					WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"创建清理脚本失败: {ex.Message}");
			}
		}

	}
}
