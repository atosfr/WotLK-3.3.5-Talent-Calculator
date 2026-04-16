using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WotLK_TalentCalculator_3._3._5.Services;

namespace WotLK_TalentCalculator_3._3._5.Controls
{
    public partial class SmallIconControl : UserControl
    {
        // DependencyProperty for XAML Binding
        public static readonly DependencyProperty IconPathProperty =
            DependencyProperty.Register("IconPath", typeof(string), typeof(SmallIconControl),
            new PropertyMetadata(null, OnIconPathChanged));

        public string IconPath
        {
            get { return (string)GetValue(IconPathProperty); }
            set { SetValue(IconPathProperty, value); }
        }

        private static void OnIconPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmallIconControl ctrl && e.NewValue is string path)
            {
                ctrl.PART_Icon.Source = AssetManager.LoadBitmap(path);
            }
        }

        public SmallIconControl()
        {
            InitializeComponent();

            if (AssetManager.IconSmallFrame != null) PART_Frame.Source = AssetManager.IconSmallFrame;
            if (AssetManager.SmallIconHilite != null) PART_Highlight.Source = AssetManager.SmallIconHilite;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) => PART_Highlight.Visibility = Visibility.Visible;
        private void OnMouseLeave(object sender, MouseEventArgs e) => PART_Highlight.Visibility = Visibility.Hidden;
    }
}