using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5
{
    public static class AssetManager
    {
        public static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

        // ── Dosya yolları ────────────────────────────────────────────────────
        public static string JsonPath => Path.Combine(BaseDir, "Talents.json");
        public static string SavePath => Path.Combine(BaseDir, "SavedBuild.json");
        public static string ProfilesPath => Path.Combine(BaseDir, "Profiles.json");
        public static string DescJsonPath => Path.Combine(BaseDir, "TalentDescriptions.json");

        // ── Bitmap cache ─────────────────────────────────────────────────────
        private static readonly Dictionary<string, BitmapImage> _cache = new();

        // ── Dinamik yol üreticileri ──────────────────────────────────────────
        public static string GetClassIconPath(string classId) =>
            Path.Combine(BaseDir, "Image", "Icons", "Classes", $"class_{classId}.jpg");

        public static string GetSpecIconPath(string classId, string fileName) =>
            Path.Combine(BaseDir, "Image", "Icons", "SpecIcons", classId, fileName);

        public static string GetBackgroundPath(string classId, string fileName) =>
            Path.Combine(BaseDir, "Image", "Backgrounds", classId, fileName);

        public static string GetSpriteSheetPath(string classId, string fileName) =>
            Path.Combine(BaseDir, "Image", "Talents", classId, fileName);

        private static string GetUtilPath(string fileName) =>
            Path.Combine(BaseDir, "Image", "Icons", "Utils", fileName);

        private static string GetArrowPath(string fileName) =>
            Path.Combine(BaseDir, "Image", "Icons", "Arrows", fileName);

        // ── Ortak görseller ──────────────────────────────────────────────────
        public static DescriptionsDb Descriptions { get; private set; }

        public static BitmapImage ArrowDown { get; private set; }
        public static BitmapImage ArrowLeftDown { get; private set; }
        public static BitmapImage ArrowRightDown { get; private set; }
        public static BitmapImage ArrowLeft { get; private set; }
        public static BitmapImage ArrowRight { get; private set; }

        public static BitmapImage ArrowDown2 { get; private set; }
        public static BitmapImage ArrowLeftDown2 { get; private set; }
        public static BitmapImage ArrowRightDown2 { get; private set; }
        public static BitmapImage ArrowLeft2 { get; private set; }
        public static BitmapImage ArrowRight2 { get; private set; }

        public static BitmapImage DeleteIcon { get; private set; }
        public static BitmapImage LockedIcon { get; private set; }
        public static BitmapImage UnlockedIcon { get; private set; }

        public static BitmapImage HiliteDefault { get; private set; }
        public static BitmapImage HiliteTalent { get; private set; }
        public static BitmapImage SmallIconHilite { get; private set; }
        public static BitmapImage IconFrame { get; private set; }
        public static BitmapImage IconSmallFrame { get; private set; }
        public static BitmapImage Bubble { get; private set; }

        public static CroppedBitmap BorderGray { get; private set; }
        public static CroppedBitmap BorderYellow { get; private set; }
        public static CroppedBitmap BorderGreen { get; private set; }

        public static void Initialize()
        {
            ArrowDown = LoadBitmap(GetArrowPath("down.png"));
            ArrowLeftDown = LoadBitmap(GetArrowPath("leftdown.png"));
            ArrowRightDown = LoadBitmap(GetArrowPath("rightdown.png"));
            ArrowLeft = LoadBitmap(GetArrowPath("left.png"));
            ArrowRight = LoadBitmap(GetArrowPath("right.png"));

            ArrowDown2 = LoadBitmap(GetArrowPath("down2.png"));
            ArrowLeftDown2 = LoadBitmap(GetArrowPath("leftdown2.png"));
            ArrowRightDown2 = LoadBitmap(GetArrowPath("rightdown2.png"));
            ArrowLeft2 = LoadBitmap(GetArrowPath("left2.png"));
            ArrowRight2 = LoadBitmap(GetArrowPath("right2.png"));

            DeleteIcon = LoadBitmap(GetUtilPath("delete.gif"));
            LockedIcon = LoadBitmap(GetUtilPath("locked.gif"));
            UnlockedIcon = LoadBitmap(GetUtilPath("unlocked.gif"));

            HiliteDefault = LoadBitmap(GetUtilPath("default-icon-hilite.png"));
            HiliteTalent = LoadBitmap(GetUtilPath("talent-icon-hilite.png"));
            SmallIconHilite = LoadBitmap(GetUtilPath("small-icon-hilite.png"));
            IconFrame = LoadBitmap(GetUtilPath("icon-medium-frame.png"));
            IconSmallFrame = LoadBitmap(GetUtilPath("icon-small-frame.png"));
            Bubble = LoadBitmap(GetUtilPath("bubble.png"));

            var borderSheet = LoadBitmap(GetUtilPath("border.gif"));
            if (borderSheet != null)
            {
                BorderGray = new CroppedBitmap(borderSheet, new Int32Rect(0, 0, 42, 42)); BorderGray.Freeze();
                BorderYellow = new CroppedBitmap(borderSheet, new Int32Rect(42, 0, 42, 42)); BorderYellow.Freeze();
                BorderGreen = new CroppedBitmap(borderSheet, new Int32Rect(84, 0, 42, 42)); BorderGreen.Freeze();
            }

            if (File.Exists(DescJsonPath))
            {
                var json = File.ReadAllText(DescJsonPath);
                Descriptions = JsonSerializer.Deserialize<DescriptionsDb>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }

        public static BitmapImage LoadBitmap(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            if (_cache.TryGetValue(path, out var cached)) return cached;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            _cache[path] = bmp;
            return bmp;
        }

        public static void ClearCache() => _cache.Clear();
    }
}