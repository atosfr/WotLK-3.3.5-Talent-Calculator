using System.Text;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Encodes and decodes the wotlkdb.com talent link format.
    /// Format: https://wotlkdb.com/?talent#{classChar}{tree1}Z{tree2}Z{tree3}:{glyphs}
    /// 
    /// Also provides helpers for the wotlkdb.com summary format (?cid=X&tal=DIGITS).
    /// </summary>
    public static class WotlkDbCodec
    {
        // ── Key Table ────────────────────────────────────────────────────
        private const string Key = "0zMcmVokRsaqbdrfwihuGINALpTjnyxtgevElBCDFHJKOPQSUWXYZ123456789";

        // -- Class Mappings --
        // Class order in the wotlkdb.com talent link (index / 3 of the first character)
        private static readonly string[] LinkClassOrder =
        {
            "druid", "hunter", "mage", "paladin", "priest",
            "rogue", "shaman", "warlock", "warrior", "deathknight"
        };

        // Mapping for cid=X in the wotlkdb.com summary URL
        private static readonly Dictionary<string, string> SummaryCidToClass = new()
        {
            { "1", "warrior"     }, { "2", "paladin" }, { "3", "hunter"     },
            { "4", "rogue"       }, { "5", "priest"  }, { "6", "deathknight" },
            { "7", "shaman"      }, { "8", "mage"    }, { "9", "warlock"    },
            { "11", "druid"      }
        };

        private static readonly Dictionary<string, string> ClassToSummaryCid = new()
        {
            { "warrior",     "1" }, { "paladin", "2" }, { "hunter",      "3" },
            { "rogue",       "4" }, { "priest",  "5" }, { "deathknight", "6" },
            { "shaman",      "7" }, { "mage",    "8" }, { "warlock",     "9" },
            { "druid",       "11" }
        };

        // ────────────────────────────────────────────────────────────────────
        // Export / Encode
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Generates a wotlkdb.com summary URL from a Class ID (talents only).</summary>
        public static string BuildSummaryUrl(string classId, List<IconControl>[] treeIcons)
        {
            string cid = ClassToSummaryCid.TryGetValue(classId, out var c) ? c : "6";
            string raw = BuildSerializer.Snapshot(treeIcons);

            // Trim trailing zeros
            int lastIdx = -1;
            for (int i = raw.Length - 1; i >= 0; i--)
                if (raw[i] != '0') { lastIdx = i; break; }

            string tal = lastIdx >= 0 ? raw[..(lastIdx + 1)] : "";
            return $"https://wotlkdb.com/?talent#?cid={cid}&tal={tal}";
        }

        /// <summary>Generates an encoded link (talent + glyph, full format).</summary>
        public static string BuildFullLink(
            string classId,
            List<IconControl>[] treeIcons,
            Dictionary<string, ClassGlyphs> glyphDb,
            List<string> majorGlyphNames,
            List<string> minorGlyphNames)
        {
            if (string.IsNullOrEmpty(classId)) return "";

            var sb = new StringBuilder();

            // 1. Class character
            int classIdx = GetLinkClassIndex(classId);
            sb.Append(Key[classIdx * 3]);

            // 2. Talent encoding
            for (int t = 0; t < 3; t++)
            {
                var icons = treeIcons[t];
                if (icons == null || icons.Count == 0)
                {
                    sb.Append('Z');
                    continue;
                }

                EncodeTree(sb, icons, isLastTree: t == 2);
            }

            // 3. Glyph encoding
            if (glyphDb != null && glyphDb.TryGetValue(classId, out var cg))
            {
                string glyphPart = EncodeGlyphs(cg, majorGlyphNames, minorGlyphNames);
                if (!string.IsNullOrEmpty(glyphPart))
                    sb.Append(':').Append(glyphPart);
            }

            return $"https://wotlkdb.com/?talent#{sb}";
        }

        private static void EncodeTree(StringBuilder sb, List<IconControl> icons, bool isLastTree)
        {
            int len = icons.Count;
            int i = 0;
            while (i < len)
            {
                // Check if all remaining talents are zero
                bool restZero = true;
                for (int j = i; j < len; j++)
                    if (icons[j].CurrentRank > 0) { restZero = false; break; }

                if (restZero)
                {
                    if (!isLastTree) sb.Append('Z');
                    return;
                }

                int r1 = icons[i].CurrentRank;
                int r2 = (i + 1 < len) ? icons[i + 1].CurrentRank : 0;
                sb.Append(Key[r1 * 6 + r2]);
                i += 2;
            }
        }

        private static string EncodeGlyphs(ClassGlyphs cg, List<string> majorNames, List<string> minorNames)
        {
            majorNames ??= new List<string>();
            minorNames ??= new List<string>();

            if (majorNames.Count == 0 && minorNames.Count == 0) return "";

            var sortedMajor = cg.major.OrderBy(g => g.itemId).ToList();
            var sortedMinor = cg.minor.OrderBy(g => g.itemId).ToList();

            var sb = new StringBuilder();

            foreach (var name in majorNames)
            {
                int idx = sortedMajor.FindIndex(g => g.name == name);
                if (idx >= 0 && idx < Key.Length) sb.Append(Key[idx]);
            }

            if (majorNames.Count < 3 && minorNames.Count > 0)
                sb.Append('Z');

            foreach (var name in minorNames)
            {
                int idx = sortedMinor.FindIndex(g => g.name == name);
                if (idx >= 0 && idx < Key.Length) sb.Append(Key[idx]);
            }

            return sb.ToString();
        }

        // ────────────────────────────────────────────────────────────────────
        // Import / Decode
        // ────────────────────────────────────────────────────────────────────

        public class DecodedBuild
        {
            public string ClassId { get; set; }
            public string BuildString { get; set; }
            public bool HasGlyphs { get; set; }
            public List<int> MajorGlyphIndices { get; set; }
            public List<int> MinorGlyphIndices { get; set; }
            public string Error { get; set; }
        }

        /// <summary>Decodes the wotlkdb.com summary (?cid=X&tal=...) format.</summary>
        public static DecodedBuild TryDecodeSummary(string input, Dictionary<string, int[]> treeLengths)
        {
            if (!input.Contains("cid=") || !input.Contains("tal="))
                return new DecodedBuild { Error = "Invalid format. Expected: ?cid=X&tal=..." };

            string classId = null, talString = null;
            var parts = input.Split('&', '?').Where(s => s.Contains('=')).ToArray();
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "cid" && SummaryCidToClass.TryGetValue(kv[1], out var c)) classId = c;
                else if (kv[0] == "tal") talString = kv[1];
            }

            if (classId == null || string.IsNullOrEmpty(talString))
                return new DecodedBuild { Error = "Invalid format. Expected: ?cid=X&tal=..." };

            return new DecodedBuild
            {
                ClassId = classId,
                BuildString = PadBuildString(classId, talString, treeLengths),
                HasGlyphs = false
            };
        }

        /// <summary>Decodes a full wotlkdb.com talent link.</summary>
        public static DecodedBuild TryDecodeLink(string url, Dictionary<string, int[]> treeLengths)
        {
            int hashIdx = url.IndexOf('#');
            if (hashIdx < 0 || hashIdx + 1 >= url.Length)
                return new DecodedBuild { Error = "Invalid link. Expected: .../?talent#..." };

            string code = url[(hashIdx + 1)..];

            // Glyph section
            string glyphPart = null;
            int colonIdx = code.IndexOf(':');
            if (colonIdx >= 0)
            {
                glyphPart = code[(colonIdx + 1)..];
                code = code[..colonIdx];
            }

            if (code.Length == 0)
                return new DecodedBuild { Error = "Empty talent code." };

            // 1. Class identification
            int keyPos = Key.IndexOf(code[0]);
            if (keyPos < 0)
                return new DecodedBuild { Error = "Unknown class character." };

            int classIndex = keyPos / 3;
            if (classIndex >= LinkClassOrder.Length)
                return new DecodedBuild { Error = "Invalid class index." };

            string classId = LinkClassOrder[classIndex];

            if (!treeLengths.TryGetValue(classId, out var lengths))
                return new DecodedBuild { Error = $"No tree data for {classId}." };

            // 2. Talent decoding
            var sb = new StringBuilder();
            int curTree = 0, curTreeLen = 0;

            for (int x = 1; x < code.Length && curTree < 3; x++)
            {
                char c = code[x];
                if (c == 'Z')
                {
                    while (curTreeLen < lengths[curTree])
                    {
                        sb.Append('0');
                        curTreeLen++;
                    }
                    curTreeLen = 0;
                    curTree++;
                }
                else
                {
                    int cn = Key.IndexOf(c);
                    if (cn < 0) continue;

                    int r1 = cn / 6;
                    int r2 = cn % 6;

                    sb.Append(r1);
                    curTreeLen++;

                    if (curTreeLen < lengths[curTree])
                    {
                        sb.Append(r2);
                        curTreeLen++;
                    }
                }

                if (curTreeLen >= lengths[curTree])
                {
                    curTreeLen = 0;
                    curTree++;
                }
            }

            // Pad remaining trees with zeros
            while (curTree < 3)
            {
                while (curTreeLen < lengths[curTree])
                {
                    sb.Append('0');
                    curTreeLen++;
                }
                curTreeLen = 0;
                curTree++;
            }

            var result = new DecodedBuild
            {
                ClassId = classId,
                BuildString = sb.ToString()
            };

            // 3. Glyph decoding
            if (!string.IsNullOrEmpty(glyphPart))
            {
                result.HasGlyphs = true;
                result.MajorGlyphIndices = new List<int>();
                result.MinorGlyphIndices = new List<int>();

                bool inMinor = false;
                int majorCount = 0;

                foreach (char gc in glyphPart)
                {
                    if (gc == 'Z') { inMinor = true; continue; }

                    int idx = Key.IndexOf(gc);
                    if (idx < 0) continue;

                    if (!inMinor && majorCount < 3)
                    {
                        result.MajorGlyphIndices.Add(idx);
                        majorCount++;
                        if (majorCount >= 3) inMinor = true;
                    }
                    else
                    {
                        result.MinorGlyphIndices.Add(idx);
                    }
                }
            }

            return result;
        }

        // ────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────

        private static int GetLinkClassIndex(string classId)
        {
            for (int i = 0; i < LinkClassOrder.Length; i++)
                if (LinkClassOrder[i] == classId) return i;
            return 0;
        }

        private static string PadBuildString(string classId, string tal, Dictionary<string, int[]> treeLengths)
        {
            if (!treeLengths.TryGetValue(classId, out var lengths)) return tal;
            int totalLen = lengths.Sum();
            return tal.Length >= totalLen ? tal : tal + new string('0', totalLen - tal.Length);
        }
    }
}