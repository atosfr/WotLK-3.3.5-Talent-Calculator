using System.IO;
using System.Text.Json;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    public static class ProfileSerializer
    {
        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        public static ProfileData Load()
        {
            if (!File.Exists(AssetManager.ProfilesPath)) return new ProfileData();
            try
            {
                var json = File.ReadAllText(AssetManager.ProfilesPath);
                return JsonSerializer.Deserialize<ProfileData>(json, Opts) ?? new ProfileData();
            }
            catch { return new ProfileData(); }
        }

        public static void Save(ProfileData data)
            => File.WriteAllText(AssetManager.ProfilesPath,
               JsonSerializer.Serialize(data, Opts));

        public static void AddProfile(Profile profile)
        {
            var data = Load();
            data.Profiles.Add(profile);
            Save(data);
        }

        public static void DeleteProfile(string id)
        {
            var data = Load();
            data.Profiles.RemoveAll(p => p.Id == id);
            Save(data);
        }
    }
}