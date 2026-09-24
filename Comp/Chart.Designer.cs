namespace WeiKit.Comp
{
    partial class Chart
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
			this.label = new System.Windows.Forms.Label();
			this.panel = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// label
			// 
			this.label.BackColor = System.Drawing.SystemColors.Highlight;
			this.label.Dock = System.Windows.Forms.DockStyle.Top;
			this.label.Font = new System.Drawing.Font("等线", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.label.ForeColor = System.Drawing.SystemColors.HighlightText;
			this.label.Location = new System.Drawing.Point(0, 0);
			this.label.Name = "label";
			this.label.Size = new System.Drawing.Size(183, 17);
			this.label.TabIndex = 0;
			this.label.Text = "text";
			this.label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel
			// 
			this.panel.AutoScroll = true;
			this.panel.BackColor = System.Drawing.Color.Transparent;
			this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel.Location = new System.Drawing.Point(0, 17);
			this.panel.Margin = new System.Windows.Forms.Padding(5);
			this.panel.Name = "panel";
			this.panel.Size = new System.Drawing.Size(183, 82);
			this.panel.TabIndex = 1;
			this.panel.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel1_Paint);
			// 
			// Chart
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.panel);
			this.Controls.Add(this.label);
			this.DoubleBuffered = true;
			this.Name = "Chart";
			this.Size = new System.Drawing.Size(183, 99);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Panel panel;
    }
}
