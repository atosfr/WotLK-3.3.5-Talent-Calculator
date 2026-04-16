using System.Windows.Controls;
using System.Windows.Media;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;
using IOPath = System.IO.Path;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Manages the state of the 6 glyph slots (3 major + 3 minor) in the glyph panel.
    /// Reloaded via ApplyForClass when the class is changed.
    /// </summary>
    public class GlyphController
    {
        private readonly IconControl[] _majorIcons;
        private readonly IconControl[] _minorIcons;
        private readonly TextBlock[] _majorTexts;
        private readonly TextBlock[] _minorTexts;
        private readonly Brush _dimBrush;
        private readonly string _emptySlotPath;

        public event System.Action Changed;

        public GlyphController(
            IconControl[] majorIcons, IconControl[] minorIcons,
            TextBlock[] majorTexts, TextBlock[] minorTexts,
            Brush dimBrush)
        {
            _majorIcons = majorIcons;
            _minorIcons = minorIcons;
            _majorTexts = majorTexts;
            _minorTexts = minorTexts;
            _dimBrush = dimBrush;
            _emptySlotPath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Utils", "inventoryslot_empty.jpg");
        }

        /// <summary>Resets the slot to its empty state.</summary>
        public void ClearSlot(Grid grid)
        {
            var icon = grid.Children.OfType<IconControl>().FirstOrDefault();
            var text = grid.Children.OfType<TextBlock>().FirstOrDefault();
            if (icon != null) icon.SpriteSheetPath = _emptySlotPath;
            if (text != null)
            {
                text.Text = "Click to select";
                text.Foreground = _dimBrush;
            }
            grid.DataContext = null;
        }

        /// <summary>Clears all 6 slots.</summary>
        public void ClearAll()
        {
            foreach (var icon in _majorIcons)
                if (icon.Parent is Grid grid) ClearSlot(grid);
            foreach (var icon in _minorIcons)
                if (icon.Parent is Grid grid) ClearSlot(grid);
        }

        /// <summary>Places the selected glyph into the slot.</summary>
        public void SetSlot(Grid grid, GlyphEntry glyph)
        {
            grid.DataContext = glyph;
            var icon = grid.Children.OfType<IconControl>().FirstOrDefault();
            var text = grid.Children.OfType<TextBlock>().FirstOrDefault();

            if (icon != null) icon.SpriteSheetPath = glyph.MediumImagePath;
            if (text != null)
            {
                text.Text = glyph.DisplayName;
                text.Foreground = Brushes.White;
            }
        }

        /// <summary>Loads saved glyphs for the selected class.</summary>
        public void ApplyForClass(
            string classId,
            Dictionary<string, ClassGlyphs> glyphDb,
            Dictionary<string, List<string>> savedMajor,
            Dictionary<string, List<string>> savedMinor)
        {
            ClearAll();

            if (!glyphDb.TryGetValue(classId, out var classGlyphs)) return;

            if (savedMajor.TryGetValue(classId, out var majorList))
                for (int i = 0; i < System.Math.Min(majorList.Count, 3); i++)
                {
                    var g = classGlyphs.major.FirstOrDefault(x => x.name == majorList[i]);
                    if (g != null && _majorIcons[i].Parent is Grid grid)
                        SetSlot(grid, g);
                }

            if (savedMinor.TryGetValue(classId, out var minorList))
                for (int i = 0; i < System.Math.Min(minorList.Count, 3); i++)
                {
                    var g = classGlyphs.minor.FirstOrDefault(x => x.name == minorList[i]);
                    if (g != null && _minorIcons[i].Parent is Grid grid)
                        SetSlot(grid, g);
                }
        }

        /// <summary>Returns the names of currently equipped glyphs ("Major" or "Minor").</summary>
        public List<string> GetCurrent(string type)
        {
            var targets = type == "Major" ? _majorIcons : _minorIcons;
            var list = new List<string>();
            foreach (var icon in targets)
                if (icon.Parent is Grid g && g.DataContext is GlyphEntry e)
                    list.Add(e.name);
            return list;
        }
    }
}