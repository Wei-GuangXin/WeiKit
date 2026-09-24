using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeiKit.Comp
{
    public partial class Panels : Panel
    {
        private int _cornerRadius = 10; // 圆角半径，默认为10
        [Browsable(true)]
        [Category("外观")]
        [Description("控件的圆角大小")]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set
            {
                if (value >= 0)
                {
                    _cornerRadius = value;
                    Invalidate(); // 触发重绘
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Tool.SetControlFillet(this, _cornerRadius);
        }
    }
}
