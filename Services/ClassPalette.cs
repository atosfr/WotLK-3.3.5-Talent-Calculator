using System.Windows.Media;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Official WoW class colors — single source of truth.
    /// Used by both the MainWindow info bar and the ProfileManagerWindow list.
    /// </summary>
    public static class ClassPalette
    {
        private static readonly Dictionary<string, Color> _colors = new()
        {
            { "deathknight", Color.FromRgb(196,  31,  59) },
            { "druid",       Color.FromRgb(255, 125,  10) },
            { "hunter",      Color.FromRgb(171, 212, 115) },
            { "mage",        Color.FromRgb(105, 204, 240) },
            { "paladin",     Color.FromRgb(245, 140, 186) },
            { "priest",      Color.FromRgb(255, 255, 255) },
            { "rogue",       Color.FromRgb(255, 245, 105) },
            { "shaman",      Color.FromRgb(  0, 112, 222) },
            { "warlock",     Color.FromRgb(148, 130, 201) },
            { "warrior",     Color.FromRgb(199, 156, 110) },
        };

        /// <summary>Returns the official WoW color for the given class ID. Defaults to white if unknown.</summary>
        public static Color GetColor(string classId)
            => _colors.TryGetValue(classId, out var c) ? c : Colors.White;

        /// <summary>Returns a SolidColorBrush — ready for UI bindings.</summary>
        public static SolidColorBrush GetBrush(string classId)
            => new(GetColor(classId));
    }
}