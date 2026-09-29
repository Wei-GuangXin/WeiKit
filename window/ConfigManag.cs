using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WeiKit.Window
{
	public partial class ConfigManag : Form
	{
		/// <summary>
		/// 配置列表
		/// </summary>
		private Configs Configs;

		/// <summary>
		/// 当前过滤后的配置列表
		/// </summary>
		private List<dynamic> FilteredConfigs = new List<dynamic>();

		/// <summary>
		/// 当前选中的配置项（叶子节点携带的配置）
		/// </summary>
		private Configs.Config SelectedConfig = null;

		/// <summary>
		/// 用于显示无配置文件时的信息
		/// </summary>
		private Label emptyLabel = new Label();

		public ConfigManag(Configs configs)
		{
			Configs = configs;
			InitializeComponent();
			SetupSearchBox();
			SetupEmptyLabel();
			ApplyAntiAliasedIcons();
		}

		/// <summary>
		/// 把工具栏图标用高质量双三次插值预缩放到 ImageScalingSize，
		/// 避免 ToolStrip 在 200×200 原图缩放到 24×24 时用默认低质量插值产生锯齿。
		/// </summary>
		private void ApplyAntiAliasedIcons()
		{
			Size target = toolStrip1.ImageScalingSize;
			if (target.Width <= 0 || target.Height <= 0) return;

			foreach (ToolStripItem item in toolStrip1.Items)
			{
				Image img = item.Image;
				if (img == null) continue;
				// 已是目标尺寸或更小则跳过，避免无谓放大
				if (img.Width == target.Width && img.Height == target.Height) continue;

				Bitmap scaled = new Bitmap(target.Width, target.Height);
				scaled.SetResolution(img.HorizontalResolution, img.VerticalResolution);
				using (Graphics g = Graphics.FromImage(scaled))
				{
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.SmoothingMode = SmoothingMode.HighQuality;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.CompositingQuality = CompositingQuality.HighQuality;
					g.DrawImage(img, new Rectangle(0, 0, target.Width, target.Height));
				}
				item.Image = scaled;
			}
		}

		private void SetupEmptyLabel()
		{
			emptyLabel.ForeColor = System.Drawing.Color.FromArgb(128, 128, 128);
			emptyLabel.Text = "暂无配置项";
			emptyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			emptyLabel.Dock = DockStyle.Fill;
			emptyLabel.Font = new Font("微软雅黑", 12F, FontStyle.Regular);
			emptyLabel.Visible = false;
			this.Controls.Add(emptyLabel);
		}

		private void SetupSearchBox()
		{
			// 搜索框容器
			ToolStripSeparator sep = new ToolStripSeparator();
			sep.Alignment = ToolStripItemAlignment.Right;
			toolStrip1.Items.Add(sep);

			ToolStripLabel searchLabel = new ToolStripLabel();
			searchLabel.Alignment = ToolStripItemAlignment.Right;
			searchLabel.Text = "🔍";
			searchLabel.Font = new Font("微软雅黑", 11F, FontStyle.Regular);
			searchLabel.ForeColor = Color.FromArgb(64, 64, 64);

			ToolStripTextBox searchBox = new ToolStripTextBox();
			searchBox.Alignment = ToolStripItemAlignment.Right;
			searchBox.Name = "searchBox";
			searchBox.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
			searchBox.Size = new Size(200, 28);
			searchBox.ToolTipText = "输入配置名称或显示名称进行搜索";
			searchBox.TextChanged += SearchBox_TextChanged;
			searchBox.KeyDown += SearchBox_KeyDown;


			ToolStripButton clearSearchBtn = new ToolStripButton();
			clearSearchBtn.Alignment = ToolStripItemAlignment.Right;
			clearSearchBtn.DisplayStyle = ToolStripItemDisplayStyle.Text;
			clearSearchBtn.Text = "✕";
			clearSearchBtn.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
			clearSearchBtn.ForeColor = Color.FromArgb(200, 50, 50);
			clearSearchBtn.ToolTipText = "清除搜索";
			clearSearchBtn.Click += (s, e) =>
			{
				searchBox.Text = "";
				searchBox.Focus();
			};
			toolStrip1.Items.Add(clearSearchBtn);
			toolStrip1.Items.Add(searchBox);
			toolStrip1.Items.Add(searchLabel);

			// 调整搜索框位置到右侧
			ToolStripSeparator sep2 = new ToolStripSeparator();
			sep2.Alignment = ToolStripItemAlignment.Right;
			toolStrip1.Items.Add(sep2);
		}

		/// <summary>
		/// 按分隔符（. - /）将配置内部名称拆分为层级片段
		/// </summary>
		private static string[] SplitNameToSegments(string name)
		{
			if (string.IsNullOrEmpty(name)) return new string[0];
			// 同时以 . - / 作为分隔符，连续的分隔符视为一个
			string[] segments = Regex.Split(name, @"[\.\-/]+")
				.Where(s => !string.IsNullOrEmpty(s))
				.ToArray();
			if (segments.Length == 0) segments = new string[] { name };
			return segments;
		}

		/// <summary>
		/// 从过滤后的配置列表构建树节点结构
		/// </summary>
		private void BuildTreeFromFilteredConfigs()
		{
			treeView1.BeginUpdate();
			treeView1.Nodes.Clear();

			if (FilteredConfigs.Count == 0)
			{
				treeView1.EndUpdate();
				return;
			}

			// 用于快速查找已存在的目录节点，Key为完整路径（用\0拼接）
			Dictionary<string, TreeNode> pathToNode = new Dictionary<string, TreeNode>();
			// 缓存一个加粗字体对象（用于目录节点）
			Font dirFont = new Font(treeView1.Font, FontStyle.Bold);
			Color dirColor = Color.FromArgb(80, 80, 80);

			foreach (var cfg in FilteredConfigs)
			{
				string name = cfg.Name ?? "";
				string text = cfg.Text ?? "";
				string[] segments = SplitNameToSegments(name);

				if (segments.Length == 0) continue;

				// 逐级查找或创建目录节点（除最后一段外都是目录）
				TreeNode parent = null;
				string currentPath = "";
				for (int i = 0; i < segments.Length - 1; i++)
				{
					string seg = segments[i];
					currentPath = currentPath + "\0" + seg;

					TreeNodeCollection parentCollection =
						parent == null ? treeView1.Nodes : parent.Nodes;

					TreeNode existing;
					if (pathToNode.TryGetValue(currentPath, out existing))
					{
						parent = existing;
						continue;
					}

					// 创建新目录节点
					TreeNode dirNode = new TreeNode(seg);
					dirNode.Tag = null; // 目录节点Tag为null，表示不是配置项
					dirNode.NodeFont = dirFont;
					dirNode.ForeColor = dirColor;
					pathToNode[currentPath] = dirNode;
					parentCollection.Add(dirNode);
					parent = dirNode;
				}

				// 最后一段是叶子节点，携带实际配置
				string displayLeafText = segments[segments.Length - 1];
				// 优先使用显示名称(Text)，若无则回退到名称的最后一段
				string leafText = string.IsNullOrEmpty(text) ? displayLeafText : text;

				TreeNode leafNode = new TreeNode(leafText);
				leafNode.Tag = cfg; // 叶子节点Tag保存实际配置对象

				TreeNodeCollection leafCollection =
					parent == null ? treeView1.Nodes : parent.Nodes;
				leafCollection.Add(leafNode);
			}

			// 默认展开所有节点
			treeView1.ExpandAll();

			treeView1.EndUpdate();
		}

		/// <summary>
		/// 枚举所有叶子节点
		/// </summary>
		private static IEnumerable<TreeNode> GetLeafNodes(TreeNodeCollection nodes)
		{
			foreach (TreeNode n in nodes)
			{
				if (n.Nodes.Count == 0) yield return n;
				foreach (var child in GetLeafNodes(n.Nodes)) yield return child;
			}
		}

		/// <summary>
		/// 展开所有包含匹配的节点，返回第一个匹配的叶子节点
		/// </summary>
		private TreeNode ExpandAndFindFirstLeaf()
		{
			TreeNode firstLeaf = null;
			foreach (TreeNode leaf in GetLeafNodes(treeView1.Nodes))
			{
				if (firstLeaf == null) firstLeaf = leaf;
				TreeNode p = leaf.Parent;
				while (p != null)
				{
					p.Expand();
					p = p.Parent;
				}
			}
			return firstLeaf;
		}

		private void SearchBox_TextChanged(object sender, EventArgs e)
		{
			var searchBox = sender as ToolStripTextBox;
			if (searchBox == null) return;

			string keyword = searchBox.Text.Trim();
			FilterAndRefreshList(keyword);
		}

		private void FilterAndRefreshList(string keyword)
		{
			if (Configs == null || Configs.configs.Count == 0)
			{
				ShowEmptyState("暂无配置项");
				return;
			}

			if (string.IsNullOrEmpty(keyword))
			{
				FilteredConfigs = Configs.configs.ToList<dynamic>();
			}
			else
			{
				var filtered = new List<dynamic>();
				foreach (var item in Configs.configs)
				{
					string name = item.Name ?? "";
					string text = item.Text ?? "";
					if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
						text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						filtered.Add(item);
					}
				}
				FilteredConfigs = filtered;
			}

			if (FilteredConfigs.Count > 0)
			{
				BuildTreeFromFilteredConfigs();

				// 选择第一个叶子节点（如有）
				TreeNode firstLeaf = ExpandAndFindFirstLeaf();
				if (firstLeaf != null)
				{
					treeView1.SelectedNode = firstLeaf;
					SelectedConfig = firstLeaf.Tag as Configs.Config;
					if (SelectedConfig != null) DisplayConfig(SelectedConfig);
				}

				emptyLabel.Visible = false;
				treeView1.Visible = true;
				Rootpanel.Visible = true;

				// 更新状态标签
				statusLabel.Text = $"共 {FilteredConfigs.Count} 项配置";
			}
			else
			{
				string msg = string.IsNullOrEmpty(keyword)
					? "暂无配置项"
					: $"未找到匹配 \"{keyword}\" 的配置";
				ShowEmptyState(msg);
			}
		}

		private void ShowEmptyState(string message)
		{
			treeView1.Visible = false;
			Rootpanel.Visible = false;
			emptyLabel.Text = message;
			emptyLabel.Visible = true;
			statusLabel.Text = "";
			SelectedConfig = null;
		}

		private void ConfigManag_Load(object sender, System.EventArgs e)
		{
			// 让TreeView支持双击展开/折叠（默认已有）
			treeView1.LabelEdit = false;

			if (Configs != null && Configs.configs.Count > 0)
			{
				FilteredConfigs = Configs.configs.ToList<dynamic>();
				BuildTreeFromFilteredConfigs();

				TreeNode firstLeaf = ExpandAndFindFirstLeaf();
				if (firstLeaf != null)
				{
					treeView1.SelectedNode = firstLeaf;
					SelectedConfig = firstLeaf.Tag as Configs.Config;
					if (SelectedConfig != null) DisplayConfig(SelectedConfig);
				}
				statusLabel.Text = $"共 {FilteredConfigs.Count} 项配置";
			}
			else
			{
				ShowEmptyState("暂无配置项");
			}
		}

		/// <summary>
		/// 树节点选中后事件：叶子节点显示配置详情；
		/// 目录节点由 +/- 按钮或双击触发展开/折叠（TreeView默认行为）
		/// </summary>
		private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var node = e.Node;
			if (node == null) return;

			var cfg = node.Tag as Configs.Config;
			if (cfg != null)
			{
				SelectedConfig = cfg;
				DisplayConfig(cfg);
			}
			// 注意：目录节点的展开/折叠交给 TreeView 内置的 +/- 按钮和双击处理
		}

		private void DisplayConfig(Configs.Config config)
		{
			// 基本信息
			label1.Text = config.Text;
			label6.Text = config.Name;
			textBox2.Text = config.Notes ?? "";

			if (config.HelpUrl == string.Empty)
			{
				label1.Left = 17;
			}
			else
			{
				label1.Left = 52;
				label3.Tag = config.HelpUrl;
			}

			// 显示配置类型标签
			string typeText = "";
			switch (config.Type)
			{
				case Configs.ValueType.String: typeText = "文本"; break;
				case Configs.ValueType.Bool: typeText = "开关"; break;
				case Configs.ValueType.Long: typeText = "整数"; break;
				case Configs.ValueType.Float: typeText = "浮点数"; break;
			}
			typeLabel.Text = $"类型: {typeText}";

			// 根据配置类型显示不同的输入控件
			switch (config.Type)
			{
				case Configs.ValueType.String:
					StringType.Visible = true;
					BoolType.Visible = false;
					NumType.Visible = false;
					break;
				case Configs.ValueType.Bool:
					StringType.Visible = false;
					BoolType.Visible = true;
					NumType.Visible = false;
					break;
				case Configs.ValueType.Long:
					StringType.Visible = false;
					BoolType.Visible = false;
					NumType.Visible = true;
					numericUpDown1.DecimalPlaces = 0;
					break;
				case Configs.ValueType.Float:
					StringType.Visible = false;
					BoolType.Visible = false;
					NumType.Visible = true;
					numericUpDown1.DecimalPlaces = 2;
					break;
			}

			// 读取并转换存储的值
			try
			{
				switch (config.Type)
				{
					case Configs.ValueType.String:
						textBox1.Text = config.Value?.ToString() ?? "";
						break;
					case Configs.ValueType.Bool:
						switch1.IsOpen = (bool)config.Value;
						break;
					case Configs.ValueType.Long:
						numericUpDown1.Value = System.Convert.ToDecimal(config.Value);
						break;
					case Configs.ValueType.Float:
						numericUpDown1.Value = System.Convert.ToDecimal(config.Value);
						break;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("读取配置数据时发生错误。\r\n" + ex.Message, "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void TextBox1_TextChanged(object sender, System.EventArgs e)
		{
			var config = SelectedConfig;
			if (config != null)
			{
				try
				{
					config.Value = textBox1.Text;
				}
				catch (Exception ex)
				{
					Console.WriteLine("应用更改时遇到错误：" + ex.ToString());
				}
			}
		}

		private void Switch1_IsOpenChange(object sender, bool e)
		{
			var config = SelectedConfig;
			if (config != null)
			{
				try
				{
					config.Value = switch1.IsOpen;
				}
				catch (Exception ex)
				{
					Console.WriteLine("应用更改时遇到错误：" + ex.ToString());
				}
			}
		}

		private void NumericUpDown1_ValueChanged(object sender, System.EventArgs e)
		{
			var config = SelectedConfig;
			if (config != null)
			{
				try
				{
					if (config.Type == Configs.ValueType.Long)
						config.Value = (long)numericUpDown1.Value;
					else if (config.Type == Configs.ValueType.Float)
						config.Value = (float)numericUpDown1.Value;
					else
						throw new Exception("配置项类型无效，无法应用更改。");
				}
				catch (Exception ex)
				{
					Console.WriteLine("应用更改时遇到错误：" + ex.ToString());
				}
			}
		}

		private void ToolStripLabel2_Click(object sender, EventArgs e)
		{
			// 保存按钮：沿用 Configs 记录的文件路径与密钥（无参 Save 会自动带上密钥），
			// 因此加密配置不会因为在这里保存而退化成明文。
			if (string.IsNullOrEmpty(Configs.ConfigFilePath))
			{
				MessageBox.Show("当前配置尚未与任何文件关联，无法保存。\r\n" +
					"请先用 Configs.Load(path, key) 或 Configs.Save(path, key) 指定配置文件。",
					"无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (!Configs.Save())
			{
				MessageBox.Show($"保存配置失败。\r\n\r\n目标文件：{Configs.ConfigFilePath}\r\n" +
					"详细原因已打印到控制台（可开启日志窗口查看）。",
					"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			MessageBox.Show($"配置已保存！\r\n\r\n文件：{Configs.ConfigFilePath}\r\n" +
				$"加密：{(Configs.IsEncrypted ? "是" : "否")}",
				"操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ToolStripLabel1_Click(object sender, EventArgs e)
		{
			try
			{
				if (MessageBox.Show("确定要重新加载整个配置文件吗？\r\n未保存的更改将会丢失。",
					"操作确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					// 必须带上原有密钥，否则加密配置会因「用空密钥载入」而失败
					if (!Configs.Load(Configs.ConfigFilePath, Configs.LastSaveKey))
					{
						MessageBox.Show("重新加载配置失败，详细原因请查看控制台输出。",
							"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					var searchBox = toolStrip1.Items.Find("searchBox", false).FirstOrDefault() as ToolStripTextBox;
					string keyword = searchBox?.Text.Trim() ?? "";
					FilterAndRefreshList(keyword);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("重新加载配置失败：\r\n" + ex.Message, "错误",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ToolStripButton1_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("确定要将此配置项重置为默认值吗？", "操作确认",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				var config = SelectedConfig;
				if (config != null)
				{
					try
					{
						if (!Configs.Reset(config.Name))
						{
							throw new Exception("无法重置配置项，可能是配置项名称无效。");
						}
						DisplayConfig(config);
					}
					catch (Exception ex)
					{
						MessageBox.Show("重置配置项失败：\r\n" + ex.Message, "错误",
							MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void ToolStripButton2_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("确定要将所有配置项重置为默认值吗？", "操作确认",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				try
				{
					Configs.ResetAll();
					var searchBox = toolStrip1.Items.Find("searchBox", false).FirstOrDefault() as ToolStripTextBox;
					string keyword = searchBox?.Text.Trim() ?? "";
					FilterAndRefreshList(keyword);
					MessageBox.Show("所有配置项已重置为默认值。", "操作成功",
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("重置所有配置项失败：\r\n" + ex.Message, "错误",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ConfigManag_FormClosed(object sender, FormClosedEventArgs e)
		{
			this.Dispose();
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			if (Configs != null && Configs.configs.Count > 0)
			{
				var searchBox = toolStrip1.Items.Find("searchBox", false).FirstOrDefault() as ToolStripTextBox;
				string keyword = searchBox?.Text.Trim() ?? "";
				FilterAndRefreshList(keyword);
			}
			else
			{
				ShowEmptyState("暂无配置项");
			}
		}

		private void ToolStripButton4_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("确定要关闭配置管理器吗？\r\n未保存的数据将会丢失。", "操作确认",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				this.Close();
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && FilteredConfigs.Count > 0)
			{
				treeView1.Focus();
				if (treeView1.SelectedNode == null)
				{
					TreeNode firstLeaf = ExpandAndFindFirstLeaf();
					if (firstLeaf != null) treeView1.SelectedNode = firstLeaf;
				}
			}
		}

		private void 导出配置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog fileDialog = new SaveFileDialog();
			fileDialog.Title = "请指定导出配置文件的目标位置：";
			fileDialog.DefaultExt = "配置文件 (*.inf)|*.inf|所有文件 (*.*)|*.*";
			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				if (!string.IsNullOrEmpty(fileDialog.FileName))
				{
					Configs.OutSave(fileDialog.FileName);
				}
			}
		}

		private void 导入配置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Title = "请指定导入配置文件的位置：";
			fileDialog.DefaultExt = "配置文件 (*.inf)|*.inf|所有文件 (*.*)|*.*";
			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				if (!string.IsNullOrEmpty(fileDialog.FileName))
				{
					if (File.Exists(fileDialog.FileName))
					{
						Configs.InputData(fileDialog.FileName);
					}
				}
			}
		}

		private void label3_Click(object sender, EventArgs e)
		{
			// 用浏览器打开存储在 label3.Tag 中的链接
			if (label3.Tag != null)
			{
				string url = label3.Tag.ToString();
				// 简单的 URL 有效性检查，防止恶意或空链接
				if (!string.IsNullOrWhiteSpace(url) &&
					(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
					 url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
				{
					try
					{
						System.Diagnostics.Process.Start(url);
					}
					catch (Exception ex)
					{
						// 如果打开失败，给出友好提示
						MessageBox.Show($"无法打开链接：{ex.Message}", "提示",
										MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
				else
				{
					MessageBox.Show("链接地址无效或格式不正确。", "提示",
									MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			else
			{
				MessageBox.Show("未设置链接地址。", "提示",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
	}
}
