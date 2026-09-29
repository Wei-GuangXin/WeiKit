using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using WeiKit;
using Tool = WeiKit.Tool;
using WeiKit.Window;

namespace WeiKit.Demo
{
	/// <summary>
	/// WMessageBox 消息框演示：覆盖普通 / 动态 / 图片消息框以及全部输入询问框。
	/// </summary>
	internal class WMessageBoxDemo
	{
		// 动态消息框的引用，便于「更新文字」和「关闭」按钮操作
		private WMessageBoxBase dynamicBox;
		private int dynamicStep;

		public Control Build()
		{
			FlowLayoutPanel content;
			var host = Ui.ScrollHost(out content);

			Ui.AddHint(content,
				"WMessageBox 是 WeiKit 提供的自定义消息框，替代系统 MessageBox，" +
				"支持自定义图标、按钮组合、动态更新、图片预览、Toast 提示，以及文本/开关/数字/日期/时间/颜色/文件/路径等输入询问。" +
				"下面每个按钮都会真实弹出对应的消息框。");

			BuildNormalMessageBox(content);
			BuildDynamicMessageBox(content);
			BuildImageMessageBox(content);
			BuildToast(content);
			BuildAskText(content);
			BuildAskSwitch(content);
			BuildAskNumber(content);
			BuildAskDateTime(content);
			BuildAskColor(content);
			BuildAskFileFolderPath(content);

			Ui.AddHeader(content, "整体说明");
			Ui.AddHint(content,
				"· 所有 Show* 方法都返回 DialogResult，可据此判断用户点击了哪个按钮；\r\n" +
				"· 所有 Ask* 方法在用户点击「取消」时返回 null（值类型返回可空类型），点击「确定」返回输入值；\r\n" +
				"· ShowDynamic 返回窗体实例，需自行调用 UpdateMessage 更新文字、Close 关闭（不会自动关闭）；\r\n" +
				"· ShowToast 是非模态的 Toast，从屏幕边缘滑入、停留后自动滑出销毁；\r\n" +
				"· 图标使用库自带的 PNG 资源（信息/正确/注意/错误），与其它窗口风格一致。");

			Ui.Log("WMessageBox 演示页已就绪。", "Info");
			return host;
		}

		#region 1. 普通消息框

		private void BuildNormalMessageBox(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "1. 普通消息框");
			var g = Ui.Group(content, "Show / ShowInfo / ShowSuccess / ShowWarning / ShowError / ShowQuestion");

			Ui.Row(g,
				Ui.Btn("Show（信息图标+确定）", (s, e) => WMessageBox.Show("这是一条普通消息。"), 200),
				Ui.Btn("ShowInfo", (s, e) => WMessageBox.ShowInfo("操作已完成。"), 120),
				Ui.Btn("ShowSuccess", (s, e) => WMessageBox.ShowSuccess("保存成功！"), 120),
				Ui.Btn("ShowWarning", (s, e) => WMessageBox.ShowWarning("该操作不可撤销。"), 120),
				Ui.Btn("ShowError", (s, e) => WMessageBox.ShowError("连接失败：超时。"), 120));

			Ui.Row(g,
				Ui.Btn("ShowQuestion（是/否）", (s, e) =>
				{
					var r = WMessageBox.ShowQuestion("确定要删除这条记录吗？");
					Ui.Log($"ShowQuestion 返回：{r}", "Info");
				}, 170),
				Ui.Btn("ShowQuestionOKCancel", (s, e) =>
				{
					var r = WMessageBox.ShowQuestionOKCancel("应用更改？");
					Ui.Log($"ShowQuestionOKCancel 返回：{r}", "Info");
				}, 170),
				Ui.Btn("自定义图标+按钮", (s, e) =>
				{
					var r = WMessageBox.Show("自定义组合：警告图标 + 重试/取消", "自定义",
						WMessageBoxIcon.Warning, WMessageBoxButtons.RetryCancel);
					Ui.Log($"自定义消息框返回：{r}", "Info");
				}, 170));

			Ui.AddHint(g, "ShowQuestion 返回 DialogResult.Yes / No；ShowQuestionOKCancel 返回 OK / Cancel。");
		}

		#endregion

		#region 2. 动态消息框

		private void BuildDynamicMessageBox(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "2. 动态消息框（非模态，可实时更新）");
			var g = Ui.Group(content, "ShowDynamic → UpdateMessage → Close");

			Ui.Row(g,
				Ui.Btn("打开动态框", (s, e) =>
				{
					if (dynamicBox != null && !dynamicBox.IsDisposed)
					{
						Ui.Log("动态框已在显示，请先关闭。", "Warning");
						return;
					}
					dynamicStep = 0;
					dynamicBox = WMessageBox.ShowDynamic("正在处理", "第 1 步：读取文件…");
					Ui.Log("动态消息框已打开。", "Info");
				}, 120),
				Ui.Btn("更新文字（下一步）", (s, e) =>
				{
					if (dynamicBox == null || dynamicBox.IsDisposed)
					{
						Ui.Log("请先打开动态框。", "Warning");
						return;
					}
					dynamicStep++;
					string[] steps = { "第 1 步：读取文件…", "第 2 步：解析数据…", "第 3 步：写入结果…", "全部完成！" };
					dynamicBox.UpdateMessage(steps[Math.Min(dynamicStep, steps.Length - 1)]);
					Application.DoEvents();
					Ui.Log($"动态框文字已更新为第 {dynamicStep + 1} 步。", "Info");
				}, 160),
				Ui.Btn("关闭动态框", (s, e) =>
				{
					if (dynamicBox != null && !dynamicBox.IsDisposed)
					{
						dynamicBox.Close();
						Ui.Log("动态消息框已关闭。", "Info");
					}
					else
					{
						Ui.Log("动态框当前未打开。", "Warning");
					}
				}, 120));

			Ui.AddHint(g, "ShowDynamic 返回窗体实例，不会阻塞；需自行调用 UpdateMessage 更新文字、Close 关闭。");
		}

		#endregion

		#region 3. 图片消息框

		private void BuildImageMessageBox(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "3. 图片消息框");
			var g = Ui.Group(content, "ShowImage(Image) / ShowImage(path)");

			Ui.Row(g,
				Ui.Btn("用生成的 Bitmap 显示", (s, e) =>
				{
					using (var bmp = new Bitmap(320, 180))
					using (var g2 = Graphics.FromImage(bmp))
					{
						g2.Clear(Color.SteelBlue);
						using (var f = new Font("微软雅黑", 16F, FontStyle.Bold))
						using (var sb = new SolidBrush(Color.White))
						using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
						{
							g2.DrawString("这是一张运行时生成的图片", f, sb, bmp.Width / 2f, bmp.Height / 2f, sf);
						}
						// 克隆一份，因为 using 会释放 bmp，消息框需要持有图片
						Image copy = (Image)bmp.Clone();
						WMessageBox.ShowImage(copy, "图片预览", "下方是运行时生成的 Bitmap：");
						copy.Dispose();
					}
				}, 180),
				Ui.Btn("生成渐变图显示", (s, e) =>
				{
					using (var bmp = new Bitmap(400, 200))
					using (var g2 = Graphics.FromImage(bmp))
					{
						using (var brush = new LinearGradientBrush(
							new Rectangle(0, 0, bmp.Width, bmp.Height),
							Color.RoyalBlue, Color.Orchid, 45f))
						{
							g2.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
						}
						Image copy = (Image)bmp.Clone();
						WMessageBox.ShowImage(copy, "渐变图预览", "由 LinearGradientBrush 绘制：");
						copy.Dispose();
					}
				}, 150),
				Ui.Btn("从文件路径显示", (s, e) =>
				{
					string path = WMessageBox.AskFile("选择图片", "图片文件 (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|所有文件 (*.*)|*.*");
					if (!string.IsNullOrEmpty(path))
						WMessageBox.ShowImage(path, "文件图片预览");
				}, 150));

			Ui.AddHint(g, "图片会自动按比例缩放到 480x360 以内（不放大），可用 maxSize 参数调整。");
		}

		#endregion

		#region 4. Toast 提示框

		private void BuildToast(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "4. Toast 提示框（边缘滑入，定时自动销毁）");
			var g = Ui.Group(content, "ShowToast(message, title, icon, position, durationMs, owner) → WToast");

			Ui.Row(g,
				Ui.Btn("屏幕·右下角（默认）", (s, e) =>
				{
					WMessageBox.ShowToast("这是一条从屏幕右下角滑入的提示。");
					Ui.Log("ShowToast 已弹出（屏幕右下角）。", "Info");
				}, 170),
				Ui.Btn("屏幕·带标题", (s, e) =>
				{
					WMessageBox.ShowToast("配置文件已自动保存。", "操作成功", WMessageBoxIcon.Success);
					Ui.Log("ShowToast(带标题 + Success 图标) 已弹出。", "Info");
				}, 150),
				Ui.Btn("屏幕·左上角·警告", (s, e) =>
				{
					WMessageBox.ShowToast("磁盘剩余空间不足 10%。", "注意", WMessageBoxIcon.Warning, ToastPosition.TopLeft);
					Ui.Log("ShowToast(左上角 + Warning 图标) 已弹出。", "Info");
				}, 170));

			Ui.Row(g,
				Ui.Btn("屏幕·右上角·5 秒", (s, e) =>
				{
					WMessageBox.ShowToast("这条会停留 5 秒再滑出。", icon: WMessageBoxIcon.Info,
						position: ToastPosition.TopRight, durationMs: 5000);
					Ui.Log("ShowToast(右上角, 5000ms) 已弹出。", "Info");
				}, 160),
				Ui.Btn("屏幕·左下角·错误", (s, e) =>
				{
					WMessageBox.ShowToast("连接服务器超时。", "错误", WMessageBoxIcon.Error, ToastPosition.BottomLeft);
					Ui.Log("ShowToast(左下角 + Error 图标) 已弹出。", "Info");
				}, 170),
				Ui.Btn("连续弹三条", (s, e) =>
				{
					WMessageBox.ShowToast("第一条提示", icon: WMessageBoxIcon.Info, position: ToastPosition.BottomRight);
					WMessageBox.ShowToast("第二条提示", icon: WMessageBoxIcon.Success, position: ToastPosition.BottomRight, durationMs: 2000);
					WMessageBox.ShowToast("第三条提示", icon: WMessageBoxIcon.Warning, position: ToastPosition.BottomRight, durationMs: 4000);
					Ui.Log("已连续弹出三条 Toast（屏幕右下角）。", "Info");
				}, 150));

			Ui.Row(g,
				Ui.Btn("本窗口·右下角", (s, e) =>
				{
					var owner = content.FindForm();
					WMessageBox.ShowToast("在本窗口客户区右下角滑入。", icon: WMessageBoxIcon.Info,
						position: ToastPosition.BottomRight, owner: owner);
					Ui.Log("ShowToast(owner=本窗口, 右下角) 已弹出。", "Info");
				}, 170),
				Ui.Btn("本窗口·右上角", (s, e) =>
				{
					var owner = content.FindForm();
					WMessageBox.ShowToast("这条锚定在本窗口右上角。", "窗口内提示",
						WMessageBoxIcon.Success, ToastPosition.TopRight, owner: owner);
					Ui.Log("ShowToast(owner=本窗口, 右上角) 已弹出。", "Info");
				}, 170));

			Ui.AddHint(g,
				"Toast 是非模态的，不会阻塞调用方；从边缘滑入后停留 durationMs，再自动滑出销毁。" +
				"owner 传 null 时相对屏幕工作区定位，传某个窗体则相对该窗体客户区定位（适合窗口内提示）。" +
				"返回的 WToast 实例可在需要时调用 Close() 提前关闭。");
		}

		#endregion

		#region 5. 文本输入框

		private void BuildAskText(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "5. 询问文本输入框");
			var g = Ui.Group(content, "AskText(title, message, defaultValue, multiline) → string");

			Ui.Row(g,
				Ui.Btn("单行文本输入", (s, e) =>
				{
					string name = WMessageBox.AskText("用户信息", "请输入用户名：", "Tiangong");
					if (name != null)
						Ui.Log($"AskText 返回：\"{name}\"", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 150),
				Ui.Btn("多行文本输入", (s, e) =>
				{
					string desc = WMessageBox.AskText("描述", "请输入详细描述：", multiline: true);
					if (desc != null)
						Ui.Log($"AskText(multiline) 返回：{desc.Length} 个字符", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 150));
		}

		#endregion

		#region 6. 开关输入框

		private void BuildAskSwitch(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "6. 询问开关输入框（使用 WeiKit.Comp.Switch）");
			var g = Ui.Group(content, "AskSwitch(title, message, defaultValue) → bool?");

			Ui.Row(g,
				Ui.Btn("询问开关（默认 开）", (s, e) =>
				{
					bool? enabled = WMessageBox.AskSwitch("功能开关", "是否启用自动保存？", true);
					if (enabled.HasValue)
						Ui.Log($"AskSwitch 返回：{enabled.Value}", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 180),
				Ui.Btn("询问开关（默认 关）", (s, e) =>
				{
					bool? enabled = WMessageBox.AskSwitch("功能开关", "是否开启调试模式？", false);
					if (enabled.HasValue)
						Ui.Log($"AskSwitch 返回：{enabled.Value}", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 180));
		}

		#endregion

		#region 7. 数字输入框

		private void BuildAskNumber(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "7. 询问数字输入框");
			var g = Ui.Group(content, "AskNumber(title, message, defaultValue, min, max, decimalPlaces) → decimal?");

			Ui.Row(g,
				Ui.Btn("整数（端口号）", (s, e) =>
				{
					decimal? port = WMessageBox.AskNumber("网络设置", "请输入监听端口：",
						defaultValue: 5694, min: 1024, max: 65535, decimalPlaces: 0);
					if (port.HasValue)
						Ui.Log($"AskNumber(端口) 返回：{port.Value}", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 160),
				Ui.Btn("浮点数（超时秒数）", (s, e) =>
				{
					decimal? timeout = WMessageBox.AskNumber("网络设置", "超时时间（秒）：",
						defaultValue: 30.0m, min: 0, max: 3600, decimalPlaces: 1);
					if (timeout.HasValue)
						Ui.Log($"AskNumber(超时) 返回：{timeout.Value}", "Info");
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 170));
		}

		#endregion

		#region 8. 日期 / 时间 / 日期时间

		private void BuildAskDateTime(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "8. 询问日期 / 时间 / 日期时间输入框");
			var g = Ui.Group(content, "AskDate / AskTime / AskDateTime → DateTime?");

			Ui.Row(g,
				Ui.Btn("AskDate", (s, e) =>
				{
					DateTime? d = WMessageBox.AskDate("选择日期", "请选择生效日期：");
					Ui.Log(d.HasValue ? $"AskDate 返回：{d.Value:yyyy-MM-dd}" : "用户取消了输入。", "Info");
				}, 120),
				Ui.Btn("AskTime", (s, e) =>
				{
					DateTime? t = WMessageBox.AskTime("选择时间", "请选择提醒时间：");
					Ui.Log(t.HasValue ? $"AskTime 返回：{t.Value:HH:mm:ss}" : "用户取消了输入。", "Info");
				}, 120),
				Ui.Btn("AskDateTime", (s, e) =>
				{
					DateTime? dt = WMessageBox.AskDateTime("选择时间", "请选择执行时间：");
					Ui.Log(dt.HasValue ? $"AskDateTime 返回：{dt.Value:yyyy-MM-dd HH:mm:ss}" : "用户取消了输入。", "Info");
				}, 140));
		}

		#endregion

		#region 9. 颜色输入框

		private void BuildAskColor(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "9. 询问颜色输入框");
			var g = Ui.Group(content, "AskColor(title, defaultColor, allowFullOpen) → Color?");

			// 展示当前选中颜色的小方块
			var colorPanel = new Panel
			{
				Size = new Size(60, 30),
				BackColor = Color.SteelBlue,
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(0, 3, 8, 3)
			};

			Ui.Row(g,
				Ui.Btn("AskColor", (s, e) =>
				{
					Color? c = WMessageBox.AskColor("选择主题色", colorPanel.BackColor);
					if (c.HasValue)
					{
						colorPanel.BackColor = c.Value;
						Ui.Log($"AskColor 返回：{c.Value}", "Info");
					}
					else
						Ui.Log("用户取消了输入。", "Info");
				}, 120),
				colorPanel,
				Ui.Text("当前选中颜色", 120));

			Ui.AddHint(g, "颜色框使用系统 ColorDialog，支持自定义调色板。");
		}

		#endregion

		#region 10. 文件 / 文件夹 / 路径

		private void BuildAskFileFolderPath(FlowLayoutPanel content)
		{
			Ui.AddHeader(content, "10. 询问文件 / 文件夹 / 路径输入框");
			var g = Ui.Group(content, "AskFile / AskFolder / AskPath → string");

			Ui.Row(g,
				Ui.Btn("AskFile", (s, e) =>
				{
					string file = WMessageBox.AskFile("选择文件",
						"文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*");
					Ui.Log(file != null ? $"AskFile 返回：{file}" : "用户取消了选择。", "Info");
				}, 120),
				Ui.Btn("AskFolder", (s, e) =>
				{
					string folder = WMessageBox.AskFolder("选择文件夹");
					Ui.Log(folder != null ? $"AskFolder 返回：{folder}" : "用户取消了选择。", "Info");
				}, 130),
				Ui.Btn("AskPath（文件）", (s, e) =>
				{
					string path = WMessageBox.AskPath("导入", "请选择导入文件路径：",
						pickFolder: false, filter: "Excel 文件 (*.xlsx)|*.xlsx");
					Ui.Log(path != null ? $"AskPath(文件) 返回：{path}" : "用户取消了输入。", "Info");
				}, 140),
				Ui.Btn("AskPath（文件夹）", (s, e) =>
				{
					string path = WMessageBox.AskPath("工作目录", "请选择工作目录：", pickFolder: true);
					Ui.Log(path != null ? $"AskPath(文件夹) 返回：{path}" : "用户取消了输入。", "Info");
				}, 150));

			Ui.AddHint(g, "AskPath 提供「文本框 + 浏览按钮」，可手动输入或点浏览选择，pickFolder 决定浏览时选文件还是文件夹。");
		}

		#endregion
	}
}
