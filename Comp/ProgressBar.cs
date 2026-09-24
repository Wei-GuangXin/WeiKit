using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WeiKit.Comp
{
    public partial class ProgressBar : UserControl
    {
        /// <summary>
        /// 进度条的值
        /// </summary>
        public int _progress = 62;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("进度条的值")]
        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                ComDisplay();
            }
        }
        /// <summary>
        /// 进度条的最大值
        /// </summary>
        public int _MaxValue = 100;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("进度条的最大值")]
        public int MaxValue
        {
            get
            {
                return _MaxValue;
            }
            set
            {
                _MaxValue = value;
                ComDisplay();
            }
        }
        /// <summary>
        /// 进度条的最小值
        /// </summary>
        public int _MinValue = 0;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("进度条的最小值")]
        public int MinValue
        {
            get
            {
                return _MinValue;
            }
            set
            {
                _MinValue = value;
                ComDisplay();
            }
        }
        /// <summary>
        /// 进度条颜色
        /// </summary>
        public Color _BarColor = Color.Blue;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("进度条颜色")]
        public Color BarColor
        {
            get
            {
                return _BarColor;
            }
            set
            {
                _BarColor = value;
                ComDisplay();
            }
        }

        /// <summary>
        /// 进度条背景颜色
        /// </summary>
        public Color _BarBackColor = Color.White;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("进度条背景颜色")]
        public Color BarBackColor
        {
            get
            {
                return _BarBackColor;
            }
            set
            {
                _BarBackColor = value;
                ComDisplay();
            }
        }

        public void ComDisplay()
        {
            // 把进度值钳制在 [MinValue, MaxValue] 内再映射，避免 Progress 越界导致
            // panel2.Width 出现负数或超出控件宽度（表现为进度条消失/溢出）。
            int min = Math.Min(_MinValue, _MaxValue);
            int max = Math.Max(_MinValue, _MaxValue);
            int value = _progress;
            if (value < min) value = min;
            if (value > max) value = max;

            int width = (int)Tool.MapValue(value, _MinValue, _MaxValue, 0, this.Width);
            if (width < 0) width = 0;
            if (width > this.Width) width = this.Width;

            panel2.Width = width;
            panel2.BackColor = _BarColor;
            panel1.BackColor = _BarBackColor;
        }
        public ProgressBar()
        {
            InitializeComponent();
            ComDisplay();
        }

        private void ProgressBar_Load(object sender, System.EventArgs e)
        {
            ComDisplay();
        }

        private void ProgressBar_Paint(object sender, PaintEventArgs e)
        {
            ComDisplay();
        }
    }
}
