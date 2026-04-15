namespace WotLK_TalentCalculator_3._3._5.Models
{
    public class TalentDb
    {
        public List<ClassDef> classes { get; set; }
    }

    public class ClassDef
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public List<TreeDef> trees { get; set; }
    }

    public class TreeDef
    {
        public string name { get; set; }
        public string specIcon { get; set; }
        public string background { get; set; }
        public string spriteSheet { get; set; }
        public List<TalentEntry> talents { get; set; }
    }

    public class TalentEntry
    {
        public string name { get; set; }
        public int row { get; set; }
        public int col { get; set; }
        public int icon { get; set; }
        public int maxRank { get; set; }
        public string attached { get; set; }
    }

    public class TalentDesc
    {
        public string cost { get; set; }
        public string range { get; set; }
        public string castTime { get; set; }
        public string cooldown { get; set; }
        public List<string> ranks { get; set; }
    }

    // Tüm sınıfların açıklamalarını tutan ana nesne
    // ClassId (örn: "deathknight") -> Talent Adı (örn: "Butchery") -> Açıklamalar
    public class DescriptionsDb : Dictionary<string, Dictionary<string, TalentDesc>>
    {
    }

    public class ClassGlyphs
    {
        public List<GlyphEntry> major { get; set; } = new();
        public List<GlyphEntry> minor { get; set; } = new();
    }

    public class GlyphEntry
    {
        public string name { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public int itemId { get; set; }

        // Uygulama içinde resmin tam yolunu tutacağımız yardımcı alan
        public string ImagePath { get; set; }
        public string MediumImagePath { get; set; }
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(name)) return "";

                if (name.StartsWith("Glyph of the ", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(13); // "Glyph of the " = 13 karakter
                }
                else if (name.StartsWith("Glyph of ", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(9); // "Glyph of " = 9 karakter
                }

                return name;
            }
        }
    }
}
