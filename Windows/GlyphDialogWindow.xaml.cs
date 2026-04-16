using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Windows
{
    public partial class GlyphDialogWindow : Window
    {
        public GlyphEntry SelectedGlyph { get; private set; }
        private readonly List<GlyphEntry> _allGlyphs;

        public GlyphDialogWindow(string title, List<GlyphEntry> glyphs)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            _allGlyphs = glyphs;
            BuildList(glyphs);
            TxtSearch.Focus();
        }

        private void BuildList(List<GlyphEntry> glyphs)
        {
            GlyphListPanel.Children.Clear();
            foreach (var glyph in glyphs.OrderBy(g => g.name))
                GlyphListPanel.Children.Add(BuildGlyphRow(glyph));
        }

        private UIElement BuildGlyphRow(GlyphEntry glyph)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 13, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 69)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 7, 10, 7),
                Cursor = Cursors.Hand,
            };

            border.MouseEnter += (s, _) =>
                ((Border)s).Background = new SolidColorBrush(Color.FromRgb(26, 26, 48));
            border.MouseLeave += (s, _) =>
                ((Border)s).Background = new SolidColorBrush(Color.FromRgb(13, 13, 26));

            var cap = glyph;
            border.MouseLeftButtonUp += (_, _) =>
            {
                SelectedGlyph = cap;
                DialogResult = true;
                Close();
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new SmallIconControl
            {
                IconPath = glyph.ImagePath,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 10, 0)
            };
            grid.Children.Add(icon);

            var textPanel = new StackPanel();
            Grid.SetColumn(textPanel, 1);

            textPanel.Children.Add(new TextBlock
            {
                Text = glyph.DisplayName,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            });

            textPanel.Children.Add(new TextBlock
            {
                Text = glyph.description,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 209, 0)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            });

            grid.Children.Add(textPanel);
            border.Child = grid;
            return border;
        }

        // ── Search filter ──
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = TxtSearch.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(query))
            {
                BuildList(_allGlyphs);
                return;
            }

            var filtered = _allGlyphs.Where(g =>
                g.name.ToLowerInvariant().Contains(query) ||
                g.description.ToLowerInvariant().Contains(query)
            ).ToList();

            BuildList(filtered);
        }

        // ── "None" option ──
        private void BtnNone_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedGlyph = null;
            DialogResult = true;
            Close();
        }

        private void BtnClose_Click2(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}