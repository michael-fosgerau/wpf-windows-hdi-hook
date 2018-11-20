using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfApplication1
{
    public class ToolBar : System.Windows.Controls.ToolBar
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var overflowPanel = base.GetTemplateChild("PART_ToolBarOverflowPanel") as ToolBarOverflowPanel;
            if (overflowPanel != null)
            {
                overflowPanel.Background = OverflowPanelBackground ?? Background;
                overflowPanel.Margin = new Thickness(0);
            }
            var overflowButton = GetTemplateChild("OverflowButton") as ToggleButton;
            if (overflowButton != null)
            {
                overflowButton.Background = OverflowPanelBackground ?? Background;
            }
        }

        public Brush OverflowPanelBackground
        {
            get;
            set;
        }
    }
}
