using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeiKit.Comp
{
    public partial class Switch : UserControl
    {
        /// <summary>
        /// 开关状态更改事件。<b>仅在用户点击开关时触发</b>；用代码给 <see cref="IsOpen"/> 赋值不会触发。
        /// 需要在代码赋值时也通知订阅者，请调用 <see cref="SetIsOpen"/> 并传入 raiseEvent=true。
        /// </summary>
        public event EventHandler<bool> IsOpenChange;
        protected virtual void OnValueChange(bool e)
        {
            IsOpenChange?.Invoke(this, e);
        }

        /// <summary>
        /// 以编程方式设置开关状态。
        /// </summary>
        /// <param name="isOpen">目标状态</param>
        /// <param name="raiseEvent">是否同时触发 <see cref="IsOpenChange"/>（默认 false，保持原有行为）</param>
        public void SetIsOpen(bool isOpen, bool raiseEvent = false)
        {
            IsOpen = isOpen;
            if (raiseEvent) OnValueChange(_IsOpen);
        }

        [Browsable(true)]
        [Category("功能")]
        [Description("开关是否是开启状态")]
        public bool IsOpen
        {
            get
            {
                return _IsOpen;
            }
            set
            {
                _IsOpen = value;
                this.Invalidate();
            }
        }
        bool _IsOpen = false;
        [Browsable(true)]
        [Category("功能")]
        [Description("是否是锁定状态，锁定状态用户不能更改开关的状态。")]
        public bool Lock
        {
            get
            {
                return _Lock;
            }
            set
            {
                _Lock = value;
                this.Invalidate();
            }
        }
        bool _Lock = false;
        public Switch()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;//抗锯齿

            Rectangle rect = ClientRectangle;
            rect.Inflate(-2, -2); // 内边距

            if (_IsOpen)
            {
                SolidBrush blueBrush = new SolidBrush(Color.LimeGreen);
                Rectangle ellipseRect = new Rectangle(0, 0, rect.Height, rect.Height);
                g.FillEllipse(blueBrush, ellipseRect);
                ellipseRect = new Rectangle(rect.Width - rect.Height, 0, rect.Height, rect.Height);
                g.FillEllipse(blueBrush, ellipseRect);
                ellipseRect = new Rectangle(rect.Height / 2, 0, rect.Width - rect.Height, rect.Height);
                g.FillRectangle(blueBrush, ellipseRect);
                Pen blackPen = new Pen(Color.Gray, 2);
                blueBrush = new SolidBrush(Color.White);
                ellipseRect = new Rectangle(rect.Width - rect.Height, 0, rect.Height - 1, rect.Height - 1);
                g.FillEllipse(blueBrush, ellipseRect);
                g.DrawEllipse(blackPen, ellipseRect);
            }
            else
            {
                SolidBrush blueBrush = new SolidBrush(Color.Gray);
                Rectangle ellipseRect = new Rectangle(0, 0, rect.Height, rect.Height);
                g.FillEllipse(blueBrush, ellipseRect);
                ellipseRect = new Rectangle(rect.Width - rect.Height, 0, rect.Height, rect.Height);
                g.FillEllipse(blueBrush, ellipseRect);
                ellipseRect = new Rectangle(rect.Height / 2, 0, rect.Width - rect.Height, rect.Height);
                g.FillRectangle(blueBrush, ellipseRect);
                Pen blackPen = new Pen(Color.Gray, 2);
                blueBrush = new SolidBrush(Color.White);
                ellipseRect = new Rectangle(0, 0, rect.Height - 1, rect.Height - 1);
                g.FillEllipse(blueBrush, ellipseRect);
                g.DrawEllipse(blackPen, ellipseRect);
            }
        }

        private void Switch_Click(object sender, System.EventArgs e)
        {
            if (!_Lock) IsOpen = !IsOpen;
            OnValueChange(_IsOpen);
        }
    }
}
