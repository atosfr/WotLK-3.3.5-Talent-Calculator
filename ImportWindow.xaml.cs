using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace WotLK_TalentCalculator_3._3._5
{
    public partial class ImportWindow : Window
    {
        private const string WotlkDbKey = "0zMcmVokRsaqbdrfwihuGINALpTjnyxtgevElBCDFHJKOPQSUWXYZ123456789";

        private static readonly string[] ClassIds =
            { "druid", "hunter", "mage", "paladin", "priest", "rogue", "shaman", "warlock", "warrior", "deathknight" };

        private static readonly Dictionary<string, string> CidToClassId = new()
        {
            { "1", "warrior" }, { "2", "paladin" }, { "3", "hunter" },
            { "4", "rogue" },  { "5", "priest" },  { "6", "deathknight" },
            { "7", "shaman" }, { "8", "mage" },    { "9", "warlock" },
            { "11", "druid" }
        };

        // Sonuclar
        public string ResultClassId { get; private set; }
        public string ResultBuildString { get; private set; }
        public List<int> ResultMajorGlyphIndices { get; private set; }
        public List<int> ResultMinorGlyphIndices { get; private set; }
        public bool HasGlyphs { get; private set; }

        // Talent tree uzunluklari (JSON'daki talent sayilari)
        private readonly Dictionary<string, int[]> _treeLengths;

        public ImportWindow(Dictionary<string, int[]> treeLengths)
        {
            InitializeComponent();
            _treeLengths = treeLengths;
        }

        private void BtnPasteSummary_Click(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsText())
                TxtImportSummary.Text = Clipboard.GetText().Trim();
        }

        private void BtnPasteLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsText())
                TxtImportLink.Text = Clipboard.GetText().Trim();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = "";
            string link = TxtImportLink.Text.Trim();
            string summary = TxtImportSummary.Text.Trim();

            // Link oncelikli
            if (!string.IsNullOrEmpty(link))
            {
                if (TryParseLink(link)) { DialogResult = true; return; }
            }

            // Link bos veya basarisiz — summary dene
            if (!string.IsNullOrEmpty(summary))
            {
                if (TryParseSummary(summary)) { DialogResult = true; return; }
            }

            if (string.IsNullOrEmpty(link) && string.IsNullOrEmpty(summary))
                TxtError.Text = "Paste a link or import string.";
        }

        private bool TryParseSummary(string input)
        {
            // Format: ?cid=X&tal=DIGITS veya sadece cid=X&tal=DIGITS
            string classId = null;
            string talString = null;

            if (input.Contains("cid=") && input.Contains("tal="))
            {
                var parts = input.Split('&', '?').Where(s => s.Contains('=')).ToArray();
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "cid" && CidToClassId.TryGetValue(kv[1], out var cid))
                            classId = cid;
                        else if (kv[0] == "tal")
                            talString = kv[1];
                    }
                }
            }

            if (classId == null || string.IsNullOrEmpty(talString))
            {
                TxtError.Text = "Invalid format. Expected: ?cid=X&tal=...";
                return false;
            }

            // Talent string'i tam uzunluga tamamla
            ResultClassId = classId;
            ResultBuildString = PadBuildString(classId, talString);
            HasGlyphs = false;
            return true;
        }

        private bool TryParseLink(string url)
        {
            // Format: https://wotlkdb.com/?talent#ENCODED
            int hashIdx = url.IndexOf('#');
            if (hashIdx < 0 || hashIdx + 1 >= url.Length)
            {
                TxtError.Text = "Invalid link. Expected: .../?talent#...";
                return false;
            }

            string code = url.Substring(hashIdx + 1);

            // Glyph kismi
            string glyphPart = null;
            int colonIdx = code.IndexOf(':');
            if (colonIdx >= 0)
            {
                glyphPart = code.Substring(colonIdx + 1);
                code = code.Substring(0, colonIdx);
            }

            if (code.Length == 0)
            {
                TxtError.Text = "Empty talent code.";
                return false;
            }

            // 1. Sinif tespiti
            char firstChar = code[0];
            int keyPos = WotlkDbKey.IndexOf(firstChar);
            if (keyPos < 0)
            {
                TxtError.Text = "Unknown class character.";
                return false;
            }
            int classIndex = keyPos / 3;
            if (classIndex >= ClassIds.Length)
            {
                TxtError.Text = "Invalid class index.";
                return false;
            }
            string classId = ClassIds[classIndex];

            // 2. Talent decode
            if (!_treeLengths.TryGetValue(classId, out var lengths))
            {
                TxtError.Text = $"No tree data for {classId}.";
                return false;
            }

            var sb = new StringBuilder();
            int curTree = 0;
            int curTreeLen = 0;

            for (int x = 1; x < code.Length && curTree < 3; x++)
            {
                char c = code[x];
                if (c == 'Z')
                {
                    // Kalan tree sifirla
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
                    int cn = WotlkDbKey.IndexOf(c);
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

            // Kalan ağaçları sıfırla
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

            ResultClassId = classId;
            ResultBuildString = sb.ToString();

            // 3. Glyph decode
            if (!string.IsNullOrEmpty(glyphPart))
            {
                HasGlyphs = true;
                ResultMajorGlyphIndices = new List<int>();
                ResultMinorGlyphIndices = new List<int>();

                bool inMinor = false;
                int majorCount = 0;

                foreach (char gc in glyphPart)
                {
                    if (gc == 'Z')
                    {
                        inMinor = true;
                        continue;
                    }

                    int idx = WotlkDbKey.IndexOf(gc);
                    if (idx < 0) continue;

                    if (!inMinor && majorCount < 3)
                    {
                        ResultMajorGlyphIndices.Add(idx);
                        majorCount++;
                        if (majorCount >= 3) inMinor = true;
                    }
                    else
                    {
                        ResultMinorGlyphIndices.Add(idx);
                    }
                }
            }
            else
            {
                HasGlyphs = false;
            }

            return true;
        }

        private string PadBuildString(string classId, string tal)
        {
            if (!_treeLengths.TryGetValue(classId, out var lengths))
                return tal;

            int totalLen = lengths.Sum();
            if (tal.Length >= totalLen) return tal;
            return tal + new string('0', totalLen - tal.Length);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}