using System.Drawing;
using System.Windows.Forms;

namespace WeiKit.Window
{
	partial class ConfigManag
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigManag));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripLabel2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
			this.statusLabel = new System.Windows.Forms.ToolStripLabel();
			this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
			this.导出配置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.导入配置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Rootpanel = new System.Windows.Forms.Panel();
			this.NumType = new System.Windows.Forms.Panel();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.BoolType = new System.Windows.Forms.Panel();
			this.switch1 = new WeiKit.Comp.Switch();
			this.StringType = new System.Windows.Forms.Panel();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.valueLabel = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.typeLabel = new System.Windows.Forms.Label();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.toolStrip1.SuspendLayout();
			this.Rootpanel.SuspendLayout();
			this.NumType.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			this.BoolType.SuspendLayout();
			this.StringType.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripButton3,
			this.toolStripLabel1,
			this.toolStripLabel2,
			this.toolStripButton1,
			this.toolStripButton2,
			this.toolStripButton4,
			this.statusLabel,
			this.toolStripSplitButton1});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Padding = new System.Windows.Forms.Padding(8, 5, 8, 5);
			this.toolStrip1.Size = new System.Drawing.Size(820, 58);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(36, 45);
			this.toolStripButton3.Text = "刷新";
			this.toolStripButton3.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripLabel1.Image")));
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(36, 45);
			this.toolStripLabel1.Text = "重载";
			this.toolStripLabel1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripLabel1.Click += new System.EventHandler(this.ToolStripLabel1_Click);
			// 
			// toolStripLabel2
			// 
			this.toolStripLabel2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripLabel2.Image")));
			this.toolStripLabel2.Name = "toolStripLabel2";
			this.toolStripLabel2.Size = new System.Drawing.Size(36, 45);
			this.toolStripLabel2.Text = "保存";
			this.toolStripLabel2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripLabel2.Click += new System.EventHandler(this.ToolStripLabel2_Click);
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(36, 45);
			this.toolStripButton1.Text = "重置";
			this.toolStripButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButton1.Click += new System.EventHandler(this.ToolStripButton1_Click);
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.Size = new System.Drawing.Size(60, 45);
			this.toolStripButton2.Text = "重置所有";
			this.toolStripButton2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButton2.Click += new System.EventHandler(this.ToolStripButton2_Click);
			// 
			// toolStripButton4
			// 
			this.toolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton4.Image")));
			this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton4.Name = "toolStripButton4";
			this.toolStripButton4.Size = new System.Drawing.Size(36, 45);
			this.toolStripButton4.Text = "退出";
			this.toolStripButton4.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
			this.toolStripButton4.Click += new System.EventHandler(this.ToolStripButton4_Click);
			// 
			// statusLabel
			// 
			this.statusLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(0, 45);
			// 
			// toolStripSplitButton1
			// 
			this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.导出配置ToolStripMenuItem,
			this.导入配置ToolStripMenuItem});
			this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
			this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripSplitButton1.Name = "toolStripSplitButton1";
			this.toolStripSplitButton1.Size = new System.Drawing.Size(16, 45);
			// 
			// 导出配置ToolStripMenuItem
			// 
			this.导出配置ToolStripMenuItem.Name = "导出配置ToolStripMenuItem";
			this.导出配置ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
			this.导出配置ToolStripMenuItem.Text = "导出配置";
			this.导出配置ToolStripMenuItem.Click += new System.EventHandler(this.导出配置ToolStripMenuItem_Click);
			// 
			// 导入配置ToolStripMenuItem
			// 
			this.导入配置ToolStripMenuItem.Name = "导入配置ToolStripMenuItem";
			this.导入配置ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
			this.导入配置ToolStripMenuItem.Text = "导入配置";
			this.导入配置ToolStripMenuItem.Click += new System.EventHandler(this.导入配置ToolStripMenuItem_Click);
			// 
			// Rootpanel
			// 
			this.Rootpanel.BackColor = System.Drawing.Color.White;
			this.Rootpanel.Controls.Add(this.NumType);
			this.Rootpanel.Controls.Add(this.BoolType);
			this.Rootpanel.Controls.Add(this.StringType);
			this.Rootpanel.Controls.Add(this.valueLabel);
			this.Rootpanel.Controls.Add(this.panel2);
			this.Rootpanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.Rootpanel.Location = new System.Drawing.Point(320, 58);
			this.Rootpanel.Name = "Rootpanel";
			this.Rootpanel.Padding = new System.Windows.Forms.Padding(12);
			this.Rootpanel.Size = new System.Drawing.Size(500, 446);
			this.Rootpanel.TabIndex = 2;
			// 
			// NumType
			// 
			this.NumType.Controls.Add(this.numericUpDown1);
			this.NumType.Dock = System.Windows.Forms.DockStyle.Top;
			this.NumType.Location = new System.Drawing.Point(12, 373);
			this.NumType.Name = "NumType";
			this.NumType.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
			this.NumType.Size = new System.Drawing.Size(476, 45);
			this.NumType.TabIndex = 4;
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.numericUpDown1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.numericUpDown1.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.numericUpDown1.Location = new System.Drawing.Point(0, 8);
			this.numericUpDown1.Maximum = new decimal(new int[] {
			-1,
			2147483647,
			0,
			0});
			this.numericUpDown1.Minimum = new decimal(new int[] {
			-1,
			2147483647,
			0,
			-2147483648});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(476, 27);
			this.numericUpDown1.TabIndex = 0;
			this.numericUpDown1.ValueChanged += new System.EventHandler(this.NumericUpDown1_ValueChanged);
			// 
			// BoolType
			// 
			this.BoolType.Controls.Add(this.switch1);
			this.BoolType.Dock = System.Windows.Forms.DockStyle.Top;
			this.BoolType.Location = new System.Drawing.Point(12, 335);
			this.BoolType.Name = "BoolType";
			this.BoolType.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
			this.BoolType.Size = new System.Drawing.Size(476, 38);
			this.BoolType.TabIndex = 3;
			// 
			// switch1
			// 
			this.switch1.Dock = System.Windows.Forms.DockStyle.Left;
			this.switch1.IsOpen = false;
			this.switch1.Location = new System.Drawing.Point(0, 8);
			this.switch1.Lock = false;
			this.switch1.Name = "switch1";
			this.switch1.Size = new System.Drawing.Size(80, 30);
			this.switch1.TabIndex = 0;
			this.switch1.IsOpenChange += new System.EventHandler<bool>(this.Switch1_IsOpenChange);
			// 
			// StringType
			// 
			this.StringType.Controls.Add(this.textBox1);
			this.StringType.Dock = System.Windows.Forms.DockStyle.Top;
			this.StringType.Location = new System.Drawing.Point(12, 228);
			this.StringType.Name = "StringType";
			this.StringType.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
			this.StringType.Size = new System.Drawing.Size(476, 107);
			this.StringType.TabIndex = 2;
			// 
			// textBox1
			// 
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.textBox1.Location = new System.Drawing.Point(0, 8);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(476, 99);
			this.textBox1.TabIndex = 0;
			this.textBox1.TextChanged += new System.EventHandler(this.TextBox1_TextChanged);
			// 
			// valueLabel
			// 
			this.valueLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this.valueLabel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.valueLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.valueLabel.Location = new System.Drawing.Point(12, 196);
			this.valueLabel.Name = "valueLabel";
			this.valueLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.valueLabel.Size = new System.Drawing.Size(476, 32);
			this.valueLabel.TabIndex = 1;
			this.valueLabel.Text = "配置值";
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.Color.Ivory;
			this.panel2.Controls.Add(this.typeLabel);
			this.panel2.Controls.Add(this.textBox2);
			this.panel2.Controls.Add(this.label6);
			this.panel2.Controls.Add(this.label4);
			this.panel2.Controls.Add(this.panel3);
			this.panel2.Controls.Add(this.label2);
			this.panel2.Controls.Add(this.label1);
			this.panel2.Controls.Add(this.label3);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(12, 12);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(476, 184);
			this.panel2.TabIndex = 0;
			// 
			// typeLabel
			// 
			this.typeLabel.BackColor = System.Drawing.Color.Ivory;
			this.typeLabel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.typeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			this.typeLabel.Location = new System.Drawing.Point(371, 21);
			this.typeLabel.Name = "typeLabel";
			this.typeLabel.Size = new System.Drawing.Size(90, 17);
			this.typeLabel.TabIndex = 7;
			this.typeLabel.Text = "类型:";
			this.typeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// textBox2
			// 
			this.textBox2.BackColor = System.Drawing.Color.Ivory;
			this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.textBox2.Location = new System.Drawing.Point(80, 85);
			this.textBox2.Multiline = true;
			this.textBox2.Name = "textBox2";
			this.textBox2.ReadOnly = true;
			this.textBox2.Size = new System.Drawing.Size(381, 87);
			this.textBox2.TabIndex = 6;
			this.textBox2.Text = "解释文本";
			// 
			// label6
			// 
			this.label6.AutoEllipsis = true;
			this.label6.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label6.Location = new System.Drawing.Point(80, 55);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(381, 17);
			this.label6.TabIndex = 4;
			this.label6.Text = "内部名称";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.label4.Location = new System.Drawing.Point(18, 83);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 17);
			this.label4.TabIndex = 3;
			this.label4.Text = "解释文本";
			// 
			// panel3
			// 
			this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
			this.panel3.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(4, 184);
			this.panel3.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.label2.Location = new System.Drawing.Point(18, 55);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(56, 17);
			this.label2.TabIndex = 1;
			this.label2.Text = "内部名称";
			// 
			// label1
			// 
			this.label1.AutoEllipsis = true;
			this.label1.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label1.Location = new System.Drawing.Point(17, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(444, 28);
			this.label1.TabIndex = 0;
			this.label1.Text = "配置名称";
			// 
			// label3
			// 
			this.label3.BackColor = System.Drawing.SystemColors.Highlight;
			this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.label3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label3.ForeColor = System.Drawing.Color.Transparent;
			this.label3.Location = new System.Drawing.Point(17, 14);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(28, 28);
			this.label3.TabIndex = 8;
			this.label3.Text = "?";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.label3.Click += new System.EventHandler(this.label3_Click);
			// 
			// treeView1
			// 
			this.treeView1.BackColor = System.Drawing.Color.White;
			this.treeView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.DrawMode = System.Windows.Forms.TreeViewDrawMode.Normal;
			this.treeView1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.treeView1.FullRowSelect = true;
			this.treeView1.HideSelection = false;
			this.treeView1.HotTracking = false;
			this.treeView1.ItemHeight = 32;
			this.treeView1.Location = new System.Drawing.Point(0, 58);
			this.treeView1.Name = "treeView1";
			this.treeView1.Scrollable = true;
			this.treeView1.ShowLines = true;
			this.treeView1.ShowPlusMinus = true;
			this.treeView1.ShowRootLines = true;
			this.treeView1.Size = new System.Drawing.Size(320, 446);
			this.treeView1.TabIndex = 3;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView1_AfterSelect);
			// 
			// ConfigManag
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(820, 504);
			this.ControlBox = false;
			this.Controls.Add(this.treeView1);
			this.Controls.Add(this.Rootpanel);
			this.Controls.Add(this.toolStrip1);
			this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigManag";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "配置管理器";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConfigManag_FormClosed);
			this.Load += new System.EventHandler(this.ConfigManag_Load);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.Rootpanel.ResumeLayout(false);
			this.NumType.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			this.BoolType.ResumeLayout(false);
			this.StringType.ResumeLayout(false);
			this.StringType.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#region Windows Form Designer generated code
		// 此区域代码由 Windows 窗体设计器生成
		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripLabel1;
		private System.Windows.Forms.ToolStripButton toolStripLabel2;
		private System.Windows.Forms.Panel Rootpanel;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel StringType;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Panel BoolType;
		private WeiKit.Comp.Switch switch1;
		private System.Windows.Forms.Panel NumType;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.Label valueLabel;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ToolStripButton toolStripButton4;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.Label typeLabel;
		private System.Windows.Forms.ToolStripLabel statusLabel;
		private ToolStripSplitButton toolStripSplitButton1;
		private ToolStripMenuItem 导出配置ToolStripMenuItem;
		private ToolStripMenuItem 导入配置ToolStripMenuItem;
		private Label label3;
	}
}