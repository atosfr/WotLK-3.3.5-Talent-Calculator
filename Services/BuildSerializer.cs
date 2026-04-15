using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using WotLK_TalentCalculator_3._3._5.Controls;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Disk üzerindeki kayıt yapısı.
    /// Her sınıfın build string'i ayrı tutulur; sınıflar arası geçişlerde
    /// önceki sınıfın puanları kaybolmaz.
    /// </summary>
    public class SaveData
    {
        public string LastClassId { get; set; } = "";
        /// <summary>classId → rank dizisi (örn. "deathknight" → "2305000...")</summary>
        public Dictionary<string, string> Builds { get; set; } = new();
        public Dictionary<string, List<string>> MajorGlyphs { get; set; } = new();
        public Dictionary<string, List<string>> MinorGlyphs { get; set; } = new();
        public bool GlyphPanelOpen { get; set; } = true;
    }

    public static class BuildSerializer
    {
        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        // ── Kaydetme ──────────────────────────────────────────────────────────

        /// <summary>
        /// Tüm sınıfların güncel build'lerini diske yazar.
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
                // Eğer içinde '0' haricinde bir sayı varsa talent verilmiş demektir
                bool hasTalents = kvp.Value.Any(c => c != '0');
                bool hasMajor = majorGlyphs.ContainsKey(kvp.Key) && majorGlyphs[kvp.Key].Count > 0;
                bool hasMinor = minorGlyphs.ContainsKey(kvp.Key) && minorGlyphs[kvp.Key].Count > 0;

                // Sadece dolu olanları VEYA en son açık olan sınıfı kaydet
                if (hasTalents || hasMajor || hasMinor || kvp.Key == lastClassId)
                {
                    payload.Builds[kvp.Key] = kvp.Value;
                    if (hasMajor) payload.MajorGlyphs[kvp.Key] = majorGlyphs[kvp.Key];
                    if (hasMinor) payload.MinorGlyphs[kvp.Key] = minorGlyphs[kvp.Key];
                }
            }

            File.WriteAllText(AssetManager.SavePath, JsonSerializer.Serialize(payload, Opts));
        }

        // ── Yükleme ───────────────────────────────────────────────────────────

        /// <summary>
        /// Diskteki kayıt dosyasını okur.
        /// Eski tek-sınıf formatını ( {ClassId, BuildString} ) otomatik olarak
        /// yeni formata ( {Builds: {...}} ) taşır.
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
            catch { /* Bozuk dosya — sıfır veriyle başla */ }

            return new SaveData();
        }

        // ── Yardımcı ──────────────────────────────────────────────────────────

        /// <summary>
        /// Mevcut üç ağacın rank'larını sıralı bir string'e çevirir.
        /// Sıra JSON'daki talent tanım sırasıyla örtüşür (TalentTreeBuilder garantisi).
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