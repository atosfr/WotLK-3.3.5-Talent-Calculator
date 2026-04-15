using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WotLK_TalentCalculator_3._3._5.Controls
{
    public partial class GlyphTooltipControl : UserControl
    {
        public GlyphTooltipControl()
        {
            InitializeComponent();
        }

        public void RefreshData(string name, string type, string desc, bool isEmpty, bool isLocked = false)
        {
            TxtType.Text = type + " Glyph";

            if (isEmpty)
            {
                TooltipBorder.MaxWidth = 200;
                TxtTitle.Text = "Empty";
                TxtTitle.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                TxtDesc.Visibility = Visibility.Collapsed;
                TxtDesc.Margin = new Thickness(0);
                TxtInstruction.TextWrapping = TextWrapping.Wrap;
                TxtInstruction.Text = isLocked ? "" : "Click to inscribe your spellbook";
                TxtInstruction.Foreground = new SolidColorBrush(Color.FromRgb(32, 255, 32));
                TxtInstruction.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                TooltipBorder.MaxWidth = 320;
                TxtTitle.Text = name;
                TxtTitle.Foreground = Brushes.White;
                TxtDesc.Text = desc;
                TxtDesc.Visibility = Visibility.Visible;
                TxtDesc.Margin = new Thickness(0, 0, 0, 6);
                TxtInstruction.Text = isLocked ? "" : "Right-click to remove";
                TxtInstruction.Foreground = new SolidColorBrush(Color.FromRgb(255, 32, 32));
                TxtInstruction.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}