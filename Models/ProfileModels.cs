using System;
using System.Collections.Generic;

namespace WotLK_TalentCalculator_3._3._5.Models
{
    /// <summary>
    /// Tek bir isimlendirilmiş build kaydı.
    /// Hangi sınıfa ait olduğunu (ClassId/ClassName) ve
    /// tüm sınıfların o andaki rank dizilerini (Builds) saklar.
    /// </summary>
    public class Profile
    {
        /// <summary>Benzersiz kimlik — silme ve güncelleme için kullanılır.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Kullanıcının verdiği ad (örn. "Preg Pala").</summary>
        public string Name { get; set; } = "";

        /// <summary>Profil kaydedildiğinde aktif olan sınıfın id'si (örn. "paladin").</summary>
        public string ClassId { get; set; } = "";

        /// <summary>Görüntüleme adı (örn. "Paladin").</summary>
        public string ClassName { get; set; } = "";

        /// <summary>Spec dağılımı (örn. "0/28/43").</summary>
        public string Distribution { get; set; } = "0/0/0";

        /// <summary>
        /// classId → rank dizisi.
        /// Profil kaydedildiğinde ziyaret edilen tüm sınıfları içerir.
        /// </summary>
        public Dictionary<string, string> Builds { get; set; } = new();

        public Dictionary<string, List<string>> MajorGlyphs { get; set; } = new();
        public Dictionary<string, List<string>> MinorGlyphs { get; set; } = new();

        /// <summary>Kayıt tarihi/saati (gösterim amaçlı).</summary>
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>Profiles.json'ın kök nesnesi.</summary>
    public class ProfileData
    {
        public List<Profile> Profiles { get; set; } = new();
    }
}