using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeiKit.Window;

namespace WeiKit
{
	/// <summary>
	/// WeiKit 自定义消息框的静态入口。
	/// 提供普通消息框、动态消息框、图片消息框，以及一系列输入询问框
	/// （文本 / 开关 / 数字 / 日期 / 时间 / 日期时间 / 颜色 / 文件 / 文件夹 / 路径）。
	/// </summary>
	public static class WMessageBox
	{
		#region 普通消息框

		/// <summary>
		/// 显示一条普通消息框（信息图标 + 确定按钮）。
		/// </summary>
		/// <param name="message">消息文本</param>
		/// <param name="title">标题（默认与库名一致）</param>
		/// <returns>用户点击的按钮结果</returns>
		public static DialogResult Show(string message, string title = null)
		{
			return Show(message, title ?? Info.LibraryName, WMessageBoxIcon.Info, WMessageBoxButtons.OK);
		}

		/// <summary>
		/// 显示一条消息框，可自定义图标与按钮组合。
		/// </summary>
		public static DialogResult Show(string message, string title, WMessageBoxIcon icon, WMessageBoxButtons buttons)
		{
			using (WMessageBoxBase box = new WMessageBoxBase())
			{
				box.Configure(title ?? Info.LibraryName, message ?? string.Empty, icon, buttons);
				return box.ShowDialog();
			}
		}

		/// <summary>
		/// 信息提示（蓝色 i 图标）。
		/// </summary>
		public static DialogResult ShowInfo(string message, string title = "提示")
		{
			return Show(message, title, WMessageBoxIcon.Info, WMessageBoxButtons.OK);
		}

		/// <summary>
		/// 成功提示（绿色对勾图标）。
		/// </summary>
		public static DialogResult ShowSuccess(string message, string title = "成功")
		{
			return Show(message, title, WMessageBoxIcon.Success, WMessageBoxButtons.OK);
		}

		/// <summary>
		/// 警告提示（橙色感叹号图标）。
		/// </summary>
		public static DialogResult ShowWarning(string message, string title = "警告")
		{
			return Show(message, title, WMessageBoxIcon.Warning, WMessageBoxButtons.OK);
		}

		/// <summary>
		/// 错误提示（红色叉号图标）。
		/// </summary>
		public static DialogResult ShowError(string message, string title = "错误")
		{
			return Show(message, title, WMessageBoxIcon.Error, WMessageBoxButtons.OK);
		}

		/// <summary>
		/// 询问确认（蓝色问号图标 + 是/否按钮）。
		/// </summary>
		/// <returns>点击「是」返回 <see cref="DialogResult.Yes"/>，否则 <see cref="DialogResult.No"/>。</returns>
		public static DialogResult ShowQuestion(string message, string title = "请确认")
		{
			return Show(message, title, WMessageBoxIcon.Question, WMessageBoxButtons.YesNo);
		}

		/// <summary>
		/// 询问确认（蓝色问号图标 + 确定/取消按钮）。
		/// </summary>
		public static DialogResult ShowQuestionOKCancel(string message, string title = "请确认")
		{
			return Show(message, title, WMessageBoxIcon.Question, WMessageBoxButtons.OKCancel);
		}

		#endregion

		#region 动态消息框

		/// <summary>
		/// 显示一个非模态的动态消息框，返回窗体实例以便后续更新内容或关闭。
		/// 常用于展示处理进度、实时日志等：
		/// <code>
		/// var box = WMessageBox.ShowDynamic("处理中", "正在执行第 1 步…");
		/// // …执行第 1 步…
		/// box.UpdateMessage("正在执行第 2 步…");
		/// // …执行第 2 步…
		/// box.Close();
		/// </code>
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="message">初始消息文本</param>
		/// <param name="icon">图标（默认信息）</param>
		/// <returns>动态消息框窗体，可调用 <see cref="WMessageBoxBase.UpdateMessage"/> 与 <see cref="Form.Close"/>。</returns>
		public static WMessageBoxBase ShowDynamic(string title, string message, WMessageBoxIcon icon = WMessageBoxIcon.Info)
		{
			WMessageBoxBase box = new WMessageBoxBase();
			box.Configure(title ?? Info.LibraryName, message ?? string.Empty, icon, WMessageBoxButtons.OK);
			box.Show();
			Application.DoEvents();
			return box;
		}

		#endregion

		#region 图片消息框

		/// <summary>
		/// 显示一张图片的消息框。
		/// </summary>
		/// <param name="image">要显示的图片</param>
		/// <param name="title">标题</param>
		/// <param name="message">图片上方的说明文字（可选）</param>
		/// <param name="maxSize">图片最大显示尺寸（默认 480x360）</param>
		public static DialogResult ShowImage(Image image, string title = "图片预览", string message = null, Size? maxSize = null)
		{
			if (image == null)
				return ShowWarning("图片为空。", title);

			Size max = maxSize ?? new Size(480, 360);
			using (WMessageBoxBase box = new WMessageBoxBase())
			{
				box.Configure(title, message ?? string.Empty, WMessageBoxIcon.None, WMessageBoxButtons.OK);

				// 按比例缩放图片
				Size scaled = ScaleImageSize(image.Size, max);
				PictureBox pic = new PictureBox
				{
					Image = image,
					SizeMode = PictureBoxSizeMode.Zoom,
					Size = scaled,
					Location = new Point(0, 0),
					BorderStyle = BorderStyle.FixedSingle
				};
				box.ContentPanel.Controls.Add(pic);
				box.ContentPanel.Width = scaled.Width;
				box.LayoutContent();
				return box.ShowDialog();
			}
		}

		/// <summary>
		/// 从文件路径加载并显示图片消息框。
		/// </summary>
		public static DialogResult ShowImage(string imagePath, string title = "图片预览", string message = null, Size? maxSize = null)
		{
			if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
				return ShowWarning($"图片文件不存在：\r\n{imagePath}", title);

			try
			{
				using (Image img = Image.FromFile(imagePath))
				{
					return ShowImage(img, title, message, maxSize);
				}
			}
			catch (Exception ex)
			{
				return ShowError($"加载图片失败：\r\n{ex.Message}", title);
			}
		}

		private static Size ScaleImageSize(Size original, Size max)
		{
			if (original.Width <= 0 || original.Height <= 0) return max;
			double ratio = Math.Min((double)max.Width / original.Width, (double)max.Height / original.Height);
			if (ratio > 1) ratio = 1; // 不放大
			return new Size((int)(original.Width * ratio), (int)(original.Height * ratio));
		}

		#endregion

		#region 输入询问框通用逻辑

		/// <summary>
		/// 创建一个带确定/取消按钮的询问框，内容区由 <paramref name="buildContent"/> 填充。
		/// 返回值为「确定」时才视为有效输入。
		/// </summary>
		private static DialogResult AskCore(string title, string message, Action<Panel> buildContent,
			WMessageBoxIcon icon = WMessageBoxIcon.Question)
		{
			using (WMessageBoxBase box = new WMessageBoxBase())
			{
				box.Configure(title, message ?? string.Empty, icon, WMessageBoxButtons.OKCancel);
				buildContent?.Invoke(box.ContentPanel);
				box.LayoutContent();
				return box.ShowDialog();
			}
		}

		#endregion

		#region 询问文本输入框

		/// <summary>
		/// 弹出文本输入框。
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="message">提示文字</param>
		/// <param name="defaultValue">默认文本</param>
		/// <param name="multiline">是否允许多行</param>
		/// <returns>用户输入的文本；点击取消返回 <c>null</c>。</returns>
		public static string AskText(string title, string message, string defaultValue = "", bool multiline = false)
		{
			string result = defaultValue ?? string.Empty;
			TextBox tb = null;
			DialogResult dr = AskCore(title, message, panel =>
			{
				tb = new TextBox
				{
					Text = result,
					Multiline = multiline,
					Size = multiline ? new Size(360, 80) : new Size(360, 28),
					Location = new Point(0, 0),
					Font = new Font("微软雅黑", 10F),
					BorderStyle = BorderStyle.FixedSingle
				};
				panel.Controls.Add(tb);
			});

			return dr == DialogResult.OK ? (tb?.Text ?? result) : null;
		}

		#endregion

		#region 询问开关输入框

		/// <summary>
		/// 弹出开关（布尔）输入框，使用 WeiKit 自带的 <see cref="Comp.Switch"/> 控件。
		/// </summary>
		/// <returns>开关状态；点击取消返回 <c>null</c>。</returns>
		public static bool? AskSwitch(string title, string message, bool defaultValue = false)
		{
			bool? result = null;
			AskCore(title, message, panel =>
			{
				Comp.Switch sw = new Comp.Switch
				{
					IsOpen = defaultValue,
					Location = new Point(0, 0),
					Size = new Size(80, 30)
				};
				sw.IsOpenChange += (s, e) => result = e;
				panel.Controls.Add(sw);
				result = defaultValue;
			});
			return result;
		}

		#endregion

		#region 询问数字输入框

		/// <summary>
		/// 弹出数字输入框。
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="message">提示文字</param>
		/// <param name="defaultValue">默认值</param>
		/// <param name="min">最小值</param>
		/// <param name="max">最大值</param>
		/// <param name="decimalPlaces">小数位数（0 表示整数）</param>
		/// <returns>用户输入的数字；点击取消返回 <c>null</c>。</returns>
		public static decimal? AskNumber(string title, string message, decimal defaultValue = 0,
			decimal min = decimal.MinValue, decimal max = decimal.MaxValue, int decimalPlaces = 0)
		{
			decimal? result = null;
			AskCore(title, message, panel =>
			{
				NumericUpDown num = new NumericUpDown
				{
					Minimum = min,
					Maximum = max,
					Value = Math.Max(min, Math.Min(max, defaultValue)),
					DecimalPlaces = decimalPlaces,
					Size = new Size(360, 28),
					Location = new Point(0, 0),
					Font = new Font("微软雅黑", 10F),
					BorderStyle = BorderStyle.FixedSingle
				};
				num.ValueChanged += (s, e) => result = num.Value;
				panel.Controls.Add(num);
				result = num.Value;
			});
			return result;
		}

		#endregion

		#region 询问日期 / 时间 / 日期时间输入框

		/// <summary>
		/// 弹出日期输入框。
		/// </summary>
		/// <returns>用户选择的日期（时间部分为 00:00:00）；点击取消返回 <c>null</c>。</returns>
		public static DateTime? AskDate(string title, string message, DateTime? defaultValue = null)
		{
			return AskDateTimePicker(title, message, defaultValue ?? DateTime.Now.Date,
				DateTimePickerFormat.Short);
		}

		/// <summary>
		/// 弹出时间输入框。
		/// </summary>
		/// <returns>用户选择的时间（日期部分为今天）；点击取消返回 <c>null</c>。</returns>
		public static DateTime? AskTime(string title, string message, DateTime? defaultValue = null)
		{
			return AskDateTimePicker(title, message, defaultValue ?? DateTime.Now,
				DateTimePickerFormat.Time);
		}

		/// <summary>
		/// 弹出日期时间输入框。
		/// </summary>
		/// <returns>用户选择的日期时间；点击取消返回 <c>null</c>。</returns>
		public static DateTime? AskDateTime(string title, string message, DateTime? defaultValue = null)
		{
			return AskDateTimePicker(title, message, defaultValue ?? DateTime.Now,
				DateTimePickerFormat.Long);
		}

		private static DateTime? AskDateTimePicker(string title, string message, DateTime defaultValue,
			DateTimePickerFormat format)
		{
			DateTime? result = null;
			AskCore(title, message, panel =>
			{
				DateTimePicker dtp = new DateTimePicker
				{
					Format = format,
					Value = defaultValue,
					Size = new Size(360, 28),
					Location = new Point(0, 0),
					Font = new Font("微软雅黑", 10F)
				};
				dtp.ValueChanged += (s, e) => result = dtp.Value;
				panel.Controls.Add(dtp);
				result = dtp.Value;
			});
			return result;
		}

		#endregion

		#region 询问颜色输入框

		/// <summary>
		/// 弹出颜色选择框（使用系统 <see cref="ColorDialog"/>）。
		/// </summary>
		/// <param name="defaultColor">默认颜色</param>
		/// <param name="allowFullOpen">是否允许自定义颜色</param>
		/// <returns>用户选择的颜色；点击取消返回 <c>null</c>。</returns>
		public static Color? AskColor(string title = "选择颜色", Color defaultColor = default(Color),
			bool allowFullOpen = true)
		{
			using (ColorDialog dlg = new ColorDialog())
			{
				dlg.Color = defaultColor;
				dlg.AllowFullOpen = allowFullOpen;
				dlg.FullOpen = allowFullOpen;
				if (dlg.ShowDialog() == DialogResult.OK)
					return dlg.Color;
				return null;
			}
		}

		#endregion

		#region 询问文件 / 文件夹 / 路径输入框

		/// <summary>
		/// 弹出文件选择框（使用系统 <see cref="OpenFileDialog"/>）。
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="filter">文件过滤器，例如 "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"</param>
		/// <param name="initialDirectory">初始目录</param>
		/// <returns>用户选择的文件完整路径；点击取消返回 <c>null</c>。</returns>
		public static string AskFile(string title = "选择文件", string filter = "所有文件 (*.*)|*.*",
			string initialDirectory = null)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.Title = title;
				dlg.Filter = filter;
				if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
					dlg.InitialDirectory = initialDirectory;
				if (dlg.ShowDialog() == DialogResult.OK)
					return dlg.FileName;
				return null;
			}
		}

		/// <summary>
		/// 弹出文件夹选择框（使用系统 <see cref="FolderBrowserDialog"/>）。
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="selectedPath">初始选中路径</param>
		/// <param name="showNewFolderButton">是否显示「新建文件夹」按钮</param>
		/// <returns>用户选择的文件夹路径；点击取消返回 <c>null</c>。</returns>
		public static string AskFolder(string title = "选择文件夹", string selectedPath = null,
			bool showNewFolderButton = true)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				dlg.Description = title;
				dlg.ShowNewFolderButton = showNewFolderButton;
				if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
					dlg.SelectedPath = selectedPath;
				if (dlg.ShowDialog() == DialogResult.OK)
					return dlg.SelectedPath;
				return null;
			}
		}

		/// <summary>
		/// 弹出路径输入框：提供一个文本框 + 「浏览…」按钮，浏览时可选择文件或文件夹。
		/// </summary>
		/// <param name="title">标题</param>
		/// <param name="message">提示文字</param>
		/// <param name="defaultValue">默认路径</param>
		/// <param name="pickFolder">为 true 时浏览选择文件夹，否则选择文件</param>
		/// <param name="filter">文件过滤器（仅 pickFolder=false 时生效）</param>
		/// <returns>用户输入或选择的路径；点击取消返回 <c>null</c>。</returns>
		public static string AskPath(string title, string message, string defaultValue = "",
			bool pickFolder = false, string filter = "所有文件 (*.*)|*.*")
		{
			string result = defaultValue ?? string.Empty;
			TextBox tb = null;

			DialogResult dr = AskCore(title, message, panel =>
			{
				tb = new TextBox
				{
					Text = result,
					Size = new Size(260, 28),
					Location = new Point(0, 0),
					Font = new Font("微软雅黑", 10F),
					BorderStyle = BorderStyle.FixedSingle
				};

				Button browse = new Button
				{
					Text = "浏览…",
					Size = new Size(80, 28),
					Location = new Point(270, -1),
					FlatStyle = FlatStyle.Flat,
					Font = new Font("微软雅黑", 9F),
					Cursor = Cursors.Hand
				};
				browse.FlatAppearance.BorderSize = 1;
				browse.Click += (s, e) =>
				{
					string picked = pickFolder
						? AskFolder("选择文件夹", tb.Text)
						: AskFile("选择文件", filter, Path.GetDirectoryName(tb.Text));
					if (!string.IsNullOrEmpty(picked))
						tb.Text = picked;
				};

				panel.Controls.Add(tb);
				panel.Controls.Add(browse);
			});

			return dr == DialogResult.OK ? (tb?.Text ?? result) : null;
		}

		#endregion

		#region Toast 提示框

		/// <summary>
		/// 显示一个从边缘滑入、停留后自动滑出并销毁的小提示框（Toast）。
		/// 非模态，不阻塞调用方。
		/// </summary>
		/// <param name="message">消息正文</param>
		/// <param name="title">可选标题（加粗显示在正文上方）</param>
		/// <param name="icon">图标类型</param>
		/// <param name="position">出现的边缘位置，默认右下角</param>
		/// <param name="durationMs">停留时长（毫秒，不含滑入滑出动画），默认 3000</param>
		/// <param name="owner">宿主窗口。传 null 时相对屏幕工作区定位；传某个窗体则相对该窗体的客户区定位。</param>
		/// <returns>Toast 窗体实例；一般无需保存，若要提前关闭可调用其 <see cref="Form.Close"/>。</returns>
		public static WToast ShowToast(string message, string title = null,
			WMessageBoxIcon icon = WMessageBoxIcon.Info,
			ToastPosition position = ToastPosition.BottomRight,
			int durationMs = 3000,
			IWin32Window owner = null)
		{
			var toast = new WToast(message, title, icon, position, durationMs, owner);
			if (owner != null)
				toast.Show(owner);
			else
				toast.Show();
			return toast;
		}

		#endregion
	}
}
