using System.IO;
using System.Text;
using System.Text.Json;
using WotLK_TalentCalculator_3._3._5.Controls;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Data structure for disk storage.
    /// Build strings for each class are stored separately; 
    /// talent points are preserved when switching between classes.
    /// </summary>
    public class SaveData
    {
        public string LastClassId { get; set; } = "";
        /// <summary>classId -> rank array (e.g., "deathknight" -> "2305000...")</summary>
        public Dictionary<string, string> Builds { get; set; } = new();
        public Dictionary<string, List<string>> MajorGlyphs { get; set; } = new();
        public Dictionary<string, List<string>> MinorGlyphs { get; set; } = new();
        public bool GlyphPanelOpen { get; set; } = true;
    }

    public static class BuildSerializer
    {
        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        // ── Saving ──────────────────────────────────────────────────────────

        /// <summary>
        /// Writes the current builds of all classes to disk.
        /// </summary>
        public static void Save(Dictionary<string, string> builds,
                                Dictionary<string, List<string>> majorGlyphs,
                                Dictionary<string, List<string>> minorGlyphs,
                                string lastClassId,
                                bool glyphPanelOpen = true)
        {
            var payload = new SaveData
            {
                LastClassId = lastClassId,
                GlyphPanelOpen = glyphPanelOpen
            };

            foreach (var kvp in builds)
            {
                // If it contains characters other than '0', talent points have been invested
                bool hasTalents = kvp.Value.Any(c => c != '0');
                bool hasMajor = majorGlyphs.ContainsKey(kvp.Key) && majorGlyphs[kvp.Key].Count > 0;
                bool hasMinor = minorGlyphs.ContainsKey(kvp.Key) && minorGlyphs[kvp.Key].Count > 0;

                // Save only classes with data OR the last active class
                if (hasTalents || hasMajor || hasMinor || kvp.Key == lastClassId)
                {
                    payload.Builds[kvp.Key] = kvp.Value;
                    if (hasMajor) payload.MajorGlyphs[kvp.Key] = majorGlyphs[kvp.Key];
                    if (hasMinor) payload.MinorGlyphs[kvp.Key] = minorGlyphs[kvp.Key];
                }
            }

            File.WriteAllText(AssetManager.SavePath, JsonSerializer.Serialize(payload, Opts));
        }

        // ── Loading ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the save file from disk.
        /// Automatically migrates the old single-class format to the new multi-build format.
        /// </summary>
        public static SaveData Load()
        {
            if (!File.Exists(AssetManager.SavePath)) return new SaveData();

            try
            {
                var json = File.ReadAllText(AssetManager.SavePath);
                if (string.IsNullOrWhiteSpace(json)) return new SaveData();

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Builds", out _))
                    return JsonSerializer.Deserialize<SaveData>(json, Opts) ?? new SaveData();
            }
            catch { /* Corrupt file — start with fresh data */ }

            return new SaveData();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Converts the current ranks of the three trees into a sequential string.
        /// The order matches the talent definition order in JSON (guaranteed by TalentTreeBuilder).
        /// </summary>
        public static string Snapshot(List<IconControl>[] treeIcons)
        {
            var sb = new StringBuilder();
            for (int t = 0; t < treeIcons.Length; t++)
                if (treeIcons[t] != null)
                    foreach (var icon in treeIcons[t])
                        sb.Append(icon.CurrentRank);
            return sb.ToString();
        }
    }
}