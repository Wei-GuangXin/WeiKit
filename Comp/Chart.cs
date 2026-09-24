using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WeiKit.Comp
{
    public partial class Chart : UserControl
    {
        public Chart()
        {
            InitializeComponent();
        }

        private float[] _value = new float[] { 56, 60, 40, 20, 0, 40, 10, 10, 22, 68 };

        [Category("Behavior")]
        [Browsable(true)]
        [Description("曲线数据")]
        public Array Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value != null && value.GetType().GetElementType() == typeof(int))
                {
                    // 如果传入的是 int[]，则转换为 float[]
                    _value = Array.ConvertAll((int[])value, item => (float)item);
                }
                else if (value != null && value.GetType().GetElementType() == typeof(float))
                {
                    // 如果传入的是 float[]，直接赋值
                    _value = (float[])value;
                }
                else
                {
                    throw new ArgumentException("Value must be either an int[] or a float[].");
                }
                panel.Invalidate(); // 触发重绘
            }
        }
		// 标题文本
		private string _Text = "曲线图";
        [Category("Behavior")]
        [Browsable(true)]
        [Description("标题文本")]
        public override string Text
        {
            get
            {
                return _Text;
            }
            set
            {
                _Text = value;
                panel.Invalidate();
            }
        }
		// 是否显示标题文本
		private bool _IsDisplayNumber = true;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("标题文本")]
        public bool IsDisplayNumber
        {
            get
            {
                return _IsDisplayNumber;
            }
            set
            {
                _IsDisplayNumber = value;
                panel.Invalidate();
            }
        }
		// 是否显示标题栏
		private bool _TitleBar = true;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("是否显示标题栏")]
        public bool TitleBar
        {
            get
            {
                return _TitleBar;
            }
            set
            {
                _TitleBar = value;
                panel.Invalidate();
            }
        }
		// 曲线颜色
		private Color _LineColor = Color.DarkGreen;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("曲线颜色")]
        public Color LineColor
        {
            get
            {
                return _LineColor;
            }
            set
            {
                _LineColor = value;
                panel.Invalidate();
            }
        }
		// 曲线背景颜色
		private Color _LineBackgroundColor = Color.LightGreen;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("曲线背景颜色")]
        public Color LineBackgroundColor
        {
            get
            {
                return _LineBackgroundColor;
            }
            set
            {
                _LineBackgroundColor = value;
                panel.Invalidate();
            }
        }
		// 曲线宽度
		private float _LineWide = 2f;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("曲线宽度")]
        public float LineWide
        {
            get
            {
                return _LineWide;
            }
            set
            {
                _LineWide = value;
                panel.Invalidate();
            }
        }
		// 张力参数
		private float _Tension = 0.5f;
        [Category("Behavior")]
        [Browsable(true)]
        [Description("张力参数（tension），范围是0到1，值越大曲线越紧绷")]
        public float Tension
        {
            get
            {
                return _Tension;
            }
            set
            {
                _Tension = value;
                panel.Invalidate();
            }
        }
		// 重绘事件
		private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            label.Visible = _TitleBar;
            label.Text = _Text;
            Graphics g = e.Graphics;
            // 抗锯齿
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 获取Panel的宽度和高度
            float panelWidth = panel.ClientSize.Width;
            float panelHeight = panel.ClientSize.Height;

            // 空数据直接返回：旧版本会在下面 points[points.Length - 1] 处索引越界
            if (_value == null || _value.Length == 0) return;

            // 找到x和y的最大值，用于计算缩放比例
            int maxX = _value.Length;
            float maxY = _value.Cast<float>().Max();
            maxY += maxY * 0.1f; // 增加10%的余量

            if (maxY < 100)
                maxY = 100;

            // X轴数组生成
            int[] x = new int[maxX];
            for (int i = 0; i < maxX; i++)
                x[i] = i;

            // 如果最大值为0，则避免除以0的情况
            if (maxX == 0 || maxY == 0) return;

            // 创建一个PointF数组存储转换后的坐标点
            PointF[] points = new PointF[x.Length];
            for (int q = 0; q < x.Length; q++)
            {
                // 将数据映射到Panel的坐标系中（只有一个点时避免除以 0）
                float pointX = maxX > 1 ? (x[q] / (float)(maxX - 1)) * panelWidth : panelWidth / 2f;
                float pointY = panelHeight - (_value[q] / maxY) * panelHeight; // 翻转Y轴方向
                points[q] = new PointF(pointX, pointY);
            }

            List<PointF> _points = new List<PointF>();
            _points.Add(new PointF(-5, panelHeight + 10));
            _points.Add(new PointF(0, points[0].Y));
            _points.AddRange(points);
            _points.Add(new PointF(panelWidth, points[points.Length - 1].Y));
            _points.Add(new PointF(panelWidth + 5, panelHeight + 10));
            g.FillClosedCurve(new SolidBrush(_LineBackgroundColor), _points.ToArray());
            // 使用DrawCurve方法绘制平滑曲线
            if (points.Length > 1)
            {
                Pen pen = new Pen(_LineColor, _LineWide);
                g.DrawCurve(pen, points, _Tension);
            }

            if (_IsDisplayNumber)
            {
                Pen pen = new Pen(Color.Gray, 1);
                g.DrawLine(pen, 0, points[points.Length - 1].Y, panelWidth, points[points.Length - 1].Y);
                g.DrawString($"Value:{_value[_value.Length - 1]}", new Font("等线", 10, FontStyle.Bold), new SolidBrush(Color.Gray), new PointF(0, points[points.Length - 1].Y - 12));
            }
        }
    }
}
