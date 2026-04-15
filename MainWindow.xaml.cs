using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;
using WotLK_TalentCalculator_3._3._5.Services;
using WotLK_TalentCalculator_3._3._5.ViewModels;
using IOPath = System.IO.Path;

namespace WotLK_TalentCalculator_3._3._5
{
    public partial class MainWindow : Window
    {
        // ── Sınıf renkleri ──────────────────────────────────────────────────
        private static readonly Dictionary<string, Color> ClassColors = new()
        {
            { "deathknight", Color.FromRgb(196, 31,  59)  },
            { "druid",       Color.FromRgb(255, 125, 10)  },
            { "hunter",      Color.FromRgb(171, 212, 115) },
            { "mage",        Color.FromRgb(105, 204, 240) },
            { "paladin",     Color.FromRgb(245, 140, 186) },
            { "priest",      Color.FromRgb(255, 255, 255) },
            { "rogue",       Color.FromRgb(255, 245, 105) },
            { "shaman",      Color.FromRgb(0,   112, 222) },
            { "warlock",     Color.FromRgb(148, 130, 201) },
            { "warrior",     Color.FromRgb(199, 156, 110) },
        };

        // ── State ────────────────────────────────────────────────────────────
        private TalentDb _db;
        private ClassDef _selectedClass;
        private bool _isLocked;
        private Dictionary<string, ClassGlyphs> _glyphDb = new();

        private readonly List<IconControl> _classIcons = new();
        private readonly List<IconControl>[] _treeIcons = new List<IconControl>[3];
        private readonly Dictionary<string, List<Rectangle>>[] _arrowMaps = new Dictionary<string, List<Rectangle>>[3];
        private readonly TextBlock[] _treePointsBlocks = new TextBlock[3];

        // ── Kayıt ────────────────────────────────────────────────────────────
        private Dictionary<string, string> _savedBuilds = new();
        private Dictionary<string, List<string>> _savedMajorGlyphs = new();
        private Dictionary<string, List<string>> _savedMinorGlyphs = new();

        // ── Servisler & ViewModel ────────────────────────────────────────────
        private readonly DispatcherTimer _saveTimer;
        public TalentInfoViewModel InfoVM { get; } = new();

        // ── Constructor ──────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            AssetManager.Initialize();
            ImgResetAll.Source = AssetManager.DeleteIcon;
            ImgLock.Source = AssetManager.LockedIcon;

            // --- YENİ EKLENEN GLYPH İLK AYARLARI ---
            BtnClearGlyphs.Source = AssetManager.DeleteIcon;
            string emptySlotPath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Utils", "inventoryslot_empty.jpg");
            MajorGlyph1.SpriteSheetPath = emptySlotPath;
            MajorGlyph2.SpriteSheetPath = emptySlotPath;
            MajorGlyph3.SpriteSheetPath = emptySlotPath;
            MinorGlyph1.SpriteSheetPath = emptySlotPath;
            MinorGlyph2.SpriteSheetPath = emptySlotPath;
            MinorGlyph3.SpriteSheetPath = emptySlotPath;
            // ---------------------------------------

            string appIconPath = IOPath.Combine(
                AssetManager.BaseDir, "Image", "Icons", "SpecIcons", "Hunter", "hunter_marksmanship.gif");
            var appBmp = AssetManager.LoadBitmap(appIconPath);
            if (appBmp != null) { this.Icon = appBmp; AppIconImage.Source = appBmp; }

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); BuildSerializer.Save(_savedBuilds, _savedMajorGlyphs, _savedMinorGlyphs, _selectedClass?.id ?? "", GlyphsPanel.Visibility == Visibility.Visible); };

            LoadData();
            var saveData = BuildSerializer.Load();
            _savedBuilds = saveData.Builds ?? new Dictionary<string, string>();
            _savedMajorGlyphs = saveData.MajorGlyphs ?? new Dictionary<string, List<string>>();
            _savedMinorGlyphs = saveData.MinorGlyphs ?? new Dictionary<string, List<string>>();

            BuildClassBar();
            if (_db?.classes?.Count > 0)
            {
                var targetClass = _db.classes.FirstOrDefault(c => c.id == saveData.LastClassId) ?? _db.classes[0];
                SelectClass(targetClass);
            }

            // Glyph paneli durumunu yükle
            if (saveData.GlyphPanelOpen)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                this.Width = 940;
            }
            else
            {
                GlyphsPanel.Visibility = Visibility.Collapsed;
                this.Width = 720;
            }

            RefreshProfileCount();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_saveTimer.IsEnabled)
            {
                _saveTimer.Stop();
                BuildSerializer.Save(_savedBuilds, _savedMajorGlyphs, _savedMinorGlyphs, _selectedClass?.id ?? "", GlyphsPanel.Visibility == Visibility.Visible);
            }
            base.OnClosing(e);
        }

        // ── Veri ─────────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                // 1. Talent verilerini yükle (Mevcut kodun)
                if (File.Exists(AssetManager.JsonPath))
                {
                    _db = JsonSerializer.Deserialize<TalentDb>(
                        File.ReadAllText(AssetManager.JsonPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // 2. YENİ: Glyphs verilerini yükle
                string glyphPath = IOPath.Combine(AssetManager.BaseDir, "Glyphs.json"); // Dosya adını kendine göre ayarla
                if (File.Exists(glyphPath))
                {
                    _glyphDb = JsonSerializer.Deserialize<Dictionary<string, ClassGlyphs>>(
                        File.ReadAllText(glyphPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Her glyph için resim dosyasının tam yolunu önceden hesapla ve ImagePath'e yaz
                    foreach (var kvp in _glyphDb)
                    {
                        string cId = kvp.Key; // "deathknight", "druid" vb. (Windows dosya yollarında büyük/küçük harf duyarsızdır)

                        foreach (var g in kvp.Value.major)
                        {
                            g.ImagePath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Glyphs", "Small", cId, $"{g.icon}.jpg");
                            g.MediumImagePath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Glyphs", "Medium", cId, $"{g.icon}.jpg");
                        }

                        foreach (var g in kvp.Value.minor)
                        {
                            g.ImagePath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Glyphs", "Small", cId, $"{g.icon}.jpg");
                            g.MediumImagePath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Glyphs", "Medium", cId, $"{g.icon}.jpg");
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Talents.json yüklenemedi:\n{ex.Message}", "Hata"); }
        }

        // ── Sınıf bar ─────────────────────────────────────────────────────────
        private void BuildClassBar()
        {
            ClassBar.Children.Clear();
            _classIcons.Clear();
            if (_db?.classes == null) return;

            foreach (var cls in _db.classes)
            {
                var icon = new IconControl
                {
                    IsClassIcon = true,
                    IsActive = true,
                    SpriteSheetPath = AssetManager.GetClassIconPath(cls.id),
                    IconIndex = 0,
                    MaxRank = 1,
                    CurrentRank = 0,
                    Margin = new Thickness(0),
                };
                var captured = cls;
                icon.MouseLeftButtonUp += (_, _) => SelectClass(captured);
                ClassBar.Children.Add(icon);
                _classIcons.Add(icon);
            }
        }

        // ── Sınıf seçimi ──────────────────────────────────────────────────────
        private void SelectClass(ClassDef cls)
        {
            if (_selectedClass?.id == cls.id) return;

            _selectedClass = cls;
            for (int i = 0; i < _classIcons.Count; i++)
                _classIcons[i].IsSelected = (_db.classes[i].id == cls.id);

            var trees = cls.trees ?? new List<TreeDef>();
            var grids = new[] { Tree1Grid, Tree2Grid, Tree3Grid };
            var bgs = new[] { Tree1Bg, Tree2Bg, Tree3Bg };
            var footers = new[] { FooterTree1, FooterTree2, FooterTree3 };

            for (int t = 0; t < 3; t++)
            {
                grids[t].Children.Clear();
                grids[t].RowDefinitions.Clear();
                footers[t].Children.Clear();

                if (t >= trees.Count) { bgs[t].Source = null; continue; }

                var tree = trees[t];
                bgs[t].Source = AssetManager.LoadBitmap(
                    AssetManager.GetBackgroundPath(cls.id, tree.background));

                var result = TalentTreeBuilder.Build(grids[t], tree,
                                    AssetManager.GetSpriteSheetPath(cls.id, tree.spriteSheet));

                _treeIcons[t] = result.Icons;
                _arrowMaps[t] = result.ArrowMap;

                int tIdx = t;
                foreach (var icon in result.Icons)
                {
                    icon.RankChangeRequested += (s, inc) => HandleRankChange((IconControl)s, tIdx, inc);

                    // TOOLTIP BAĞLANTILARI
                    icon.MouseEnter += (s, _) => ShowTooltip((IconControl)s, tree, tIdx);
                    icon.MouseLeave += (_, _) => TooltipPopup.IsOpen = false;
                }

                BuildFooterItem(footers[t], tree, t);
            }

            if (_savedBuilds.TryGetValue(cls.id, out var saved) && !string.IsNullOrEmpty(saved))
                ApplyBuildString(saved);
            else
            {
                for (int t = 0; t < 3; t++) UpdateTreeStates(t);
                UpdateInfoBar();
            }

            ApplySavedGlyphs(cls.id);
            ScheduleSave();
        }

        // ── Build yükleme & kaydetme ──────────────────────────────────────────
        private void ApplyBuildString(string buildString)
        {
            var allIcons = new List<IconControl>();
            for (int t = 0; t < 3; t++)
                if (_treeIcons[t] != null)
                    allIcons.AddRange(_treeIcons[t]);

            int len = Math.Min(buildString.Length, allIcons.Count);
            for (int i = 0; i < len; i++)
                if (int.TryParse(buildString[i].ToString(), out int rank))
                    allIcons[i].CurrentRank = Math.Min(rank, allIcons[i].MaxRank);

            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            for (int t = 0; t < 3; t++)
                if (_treePointsBlocks[t] != null && _treeIcons[t] != null)
                    _treePointsBlocks[t].Text = $" ({_treeIcons[t].Sum(x => x.CurrentRank)})";
            UpdateInfoBar();
        }

        private void SnapshotAndSave()
        {
            if (_selectedClass == null) return;
            _savedBuilds[_selectedClass.id] = BuildSerializer.Snapshot(_treeIcons);
            _savedMajorGlyphs[_selectedClass.id] = GetCurrentGlyphs("Major");
            _savedMinorGlyphs[_selectedClass.id] = GetCurrentGlyphs("Minor");
            ScheduleSave();
        }

        // ── Profil yönetimi ───────────────────────────────────────────────────
        private void BtnProfiles_Click(object sender, MouseButtonEventArgs e)
        {
            var singleBuild = new Dictionary<string, string>();
            if (_treeIcons != null)
            {
                singleBuild[_selectedClass.id] = BuildSerializer.Snapshot(_treeIcons);
            }

            var singleMajor = new Dictionary<string, List<string>>();
            var currentMajorList = GetCurrentGlyphs("Major");
            if (currentMajorList != null)
            {
                singleMajor[_selectedClass.id] = currentMajorList;
            }

            var singleMinor = new Dictionary<string, List<string>>();
            var currentMinorList = GetCurrentGlyphs("Minor");
            if (currentMinorList != null)
            {
                singleMinor[_selectedClass.id] = currentMinorList;
            }

            var win = new ProfileManagerWindow(
                    currentBuilds: singleBuild,
                    currentMajorGlyphs: singleMajor,
                    currentMinorGlyphs: singleMinor,
                    currentClassId: _selectedClass?.id ?? "",
                    currentClassName: _selectedClass?.name ?? "",
                    currentDistribution: GetDistributionString()
            )
            { Owner = this };

            if (win.ShowDialog() == true && win.LoadedProfile != null)
                LoadProfile(win.LoadedProfile);

            RefreshProfileCount();
        }

        private void LoadProfile(Profile profile)
        {
            // Profildeki talentları al
            foreach (var kv in profile.Builds)
                _savedBuilds[kv.Key] = kv.Value;

            // Profildeki glifleri al
            if (profile.MajorGlyphs != null)
                foreach (var kv in profile.MajorGlyphs)
                    _savedMajorGlyphs[kv.Key] = kv.Value;

            if (profile.MinorGlyphs != null)
                foreach (var kv in profile.MinorGlyphs)
                    _savedMinorGlyphs[kv.Key] = kv.Value;

            // Sınıfa geçiş yap
            var cls = _db?.classes?.FirstOrDefault(c => c.id == profile.ClassId);
            if (cls == null) return;

            _selectedClass = null; // Aynı sınıftaysa bile ekranı zorla yenilemek için
            SelectClass(cls);

            // Glif görsellerini ekrana bas
            ApplySavedGlyphs(cls.id);

            // Yüklenen profili ana kayıt dosyasına (SavedBuild.json) işle
            SnapshotAndSave();
        }

        /// <summary>Toolbar'daki profil sayacını günceller.</summary>
        private void RefreshProfileCount()
        {
            var count = ProfileSerializer.Load().Profiles.Count;
            TxtProfileCount.Text = count > 0 ? $"({count})" : "";
        }

        private string GetDistributionString()
        {
            int t1 = _treeIcons[0]?.Sum(x => x.CurrentRank) ?? 0;
            int t2 = _treeIcons[1]?.Sum(x => x.CurrentRank) ?? 0;
            int t3 = _treeIcons[2]?.Sum(x => x.CurrentRank) ?? 0;
            return $"{t1}/{t2}/{t3}";
        }

        // ── Footer ────────────────────────────────────────────────────────────
        private void BuildFooterItem(StackPanel panel, TreeDef tree, int treeIndex)
        {
            var bmp = AssetManager.LoadBitmap(AssetManager.GetSpecIconPath(_selectedClass.id, tree.specIcon));
            if (bmp != null)
                panel.Children.Add(new Image
                {
                    Source = bmp,
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

            panel.Children.Add(new TextBlock
            {
                Text = tree.name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            });

            var pointsTb = new TextBlock
            {
                Text = " (0)",
                Foreground = (Brush)FindResource("TextDim"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            _treePointsBlocks[treeIndex] = pointsTb;
            panel.Children.Add(pointsTb);

            if (AssetManager.DeleteIcon == null) return;

            var del = new Image
            {
                Source = AssetManager.DeleteIcon,
                Stretch = Stretch.None,
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = Cursors.Hand,
                ToolTip = $"Reset {tree.name}",
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.3333
            };
            del.MouseEnter += (s, _) => ((Image)s).Opacity = 1.0;
            del.MouseLeave += (s, _) => ((Image)s).Opacity = 0.3333;
            del.MouseLeftButtonUp += (_, _) => ResetTree(treeIndex);
            panel.Children.Add(del);
        }

        // ── Rank değişimi ──────────────────────────────────────────────────────
        private void HandleRankChange(IconControl icon, int treeIndex, bool increase)
        {
            if (_isLocked) return;

            if (increase)
            {
                if (!icon.IsActive || icon.CurrentRank >= icon.MaxRank) return;
                if (TotalPoints() >= 71) return;
                icon.CurrentRank++;
            }
            else
            {
                if (icon.CurrentRank == 0 || !CanRemove(icon, treeIndex)) return;
                icon.CurrentRank--;
            }

            CommitChange(treeIndex);
        }

        private bool CanRemove(IconControl clicked, int treeIndex)
        {
            var tree = _treeIcons[treeIndex];
            if (tree.Any(x => x.PrerequisiteTalent == clicked && x.CurrentRank > 0))
                return false;

            foreach (var icon in tree)
            {
                int simRank = icon == clicked ? icon.CurrentRank - 1 : icon.CurrentRank;
                if (simRank <= 0 || icon.Row == 0) continue;

                int before = tree.Sum(other =>
                    other.Row < icon.Row
                        ? (other == clicked ? other.CurrentRank - 1 : other.CurrentRank)
                        : 0);

                if (before < icon.Row * 5) return false;
            }
            return true;
        }

        private void CommitChange(int treeIndex)
        {
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            _treePointsBlocks[treeIndex].Text = $" ({_treeIcons[treeIndex].Sum(x => x.CurrentRank)})";
            UpdateInfoBar();
            SnapshotAndSave();

            // _currentTalentIcons yerine _treeIcons içinde arıyoruz
            if (TooltipPopup.IsOpen)
            {
                for (int t = 0; t < 3; t++)
                {
                    if (_treeIcons[t] != null)
                    {
                        var mouseOverIcon = _treeIcons[t].FirstOrDefault(x => x.IsMouseOver);
                        if (mouseOverIcon != null)
                        {
                            ShowTooltip(mouseOverIcon, _selectedClass.trees[t], t);
                            break;
                        }
                    }
                }
            }
        }

        // ── Ağaç durumu & ok renkleri ──────────────────────────────────────────
        private void UpdateTreeStates(int treeIndex)
        {
            var tree = _treeIcons[treeIndex];
            if (tree == null) return;

            int treeTotal = tree.Sum(x => x.CurrentRank);
            bool isMaxPointsReached = TotalPoints() >= 71;
            foreach (var icon in tree)
            {
                bool rowMet = treeTotal >= icon.Row * 5;
                bool prereqMet = icon.PrerequisiteTalent == null ||
                                 icon.PrerequisiteTalent.CurrentRank == icon.PrerequisiteTalent.MaxRank;
                icon.IsActive = !((_isLocked || isMaxPointsReached) && icon.CurrentRank == 0) && rowMet && prereqMet;
            }

            var arrowMap = _arrowMaps[treeIndex];
            if (arrowMap == null) return;

            foreach (var icon in tree)
            {
                var name = ((TalentEntry)icon.DataContext)?.name;
                if (name == null || !arrowMap.TryGetValue(name, out var arrows)) continue;
                foreach (var arrow in arrows)
                    SetArrowColor(arrow, icon.IsActive);
            }
        }

        private static void SetArrowColor(Rectangle arrow, bool active)
        {
            if (arrow.Fill is not ImageBrush brush) return;
            var src = brush.ImageSource;
            brush.ImageSource =
                src == AssetManager.ArrowDown || src == AssetManager.ArrowDown2 ? (active ? AssetManager.ArrowDown2 : AssetManager.ArrowDown) :
                src == AssetManager.ArrowRight || src == AssetManager.ArrowRight2 ? (active ? AssetManager.ArrowRight2 : AssetManager.ArrowRight) :
                src == AssetManager.ArrowLeft || src == AssetManager.ArrowLeft2 ? (active ? AssetManager.ArrowLeft2 : AssetManager.ArrowLeft) :
                src == AssetManager.ArrowRightDown || src == AssetManager.ArrowRightDown2 ? (active ? AssetManager.ArrowRightDown2 : AssetManager.ArrowRightDown) :
                src == AssetManager.ArrowLeftDown || src == AssetManager.ArrowLeftDown2 ? (active ? AssetManager.ArrowLeftDown2 : AssetManager.ArrowLeftDown) :
                src;
        }

        // ── InfoBar ────────────────────────────────────────────────────────────
        private void UpdateInfoBar()
        {
            if (_selectedClass == null) return;

            int t1 = _treeIcons[0]?.Sum(x => x.CurrentRank) ?? 0;
            int t2 = _treeIcons[1]?.Sum(x => x.CurrentRank) ?? 0;
            int t3 = _treeIcons[2]?.Sum(x => x.CurrentRank) ?? 0;

            var color = ClassColors.TryGetValue(_selectedClass.id, out var c)
                ? new SolidColorBrush(c) : Brushes.White;

            InfoClassName.Inlines.Clear();
            InfoClassName.Inlines.Add(new Run(_selectedClass.name)
            { Foreground = color, FontWeight = FontWeights.Bold });
            InfoClassName.Inlines.Add(new Run($" ({t1}/{t2}/{t3})")
            { Foreground = Brushes.White });

            InfoVM.Update(t1, t2, t3);
        }

        // ── Reset ──────────────────────────────────────────────────────────────
        private void BtnResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            BtnClearGlyphs_Click(null, null);
            bool any = false;
            for (int t = 0; t < 3; t++)
            {
                if (_treeIcons[t] == null) continue;
                bool changed = false;
                foreach (var icon in _treeIcons[t])
                    if (icon.CurrentRank > 0) { icon.CurrentRank = 0; changed = any = true; }
                if (changed) { UpdateTreeStates(t); _treePointsBlocks[t].Text = " (0)"; }
            }
            if (any) { UpdateInfoBar(); SnapshotAndSave(); }
        }

        private void ResetTree(int treeIndex)
        {
            if (_isLocked || _treeIcons[treeIndex] == null) return;
            bool changed = false;
            foreach (var icon in _treeIcons[treeIndex])
                if (icon.CurrentRank > 0) { icon.CurrentRank = 0; changed = true; }
            if (!changed) return;
            UpdateTreeStates(treeIndex);
            _treePointsBlocks[treeIndex].Text = " (0)";
            UpdateInfoBar();
            SnapshotAndSave();
        }

        // ── Lock ───────────────────────────────────────────────────────────────
        private void BtnLock_Click(object sender, MouseButtonEventArgs e)
        {
            _isLocked = !_isLocked;
            TxtLock.Text = _isLocked ? "Unlock" : "Lock";
            ImgLock.Source = _isLocked ? AssetManager.UnlockedIcon : AssetManager.LockedIcon;
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
        }

        // ── Import / Export ────────────────────────────────────────────────────
        private void BtnImport_Click(object sender, MouseButtonEventArgs e)
        {
            // Tree uzunluklarini hesapla
            var treeLengths = new Dictionary<string, int[]>();
            if (_db?.classes != null)
            {
                foreach (var cls in _db.classes)
                {
                    var lens = new int[3];
                    if (cls.trees != null)
                        for (int t = 0; t < Math.Min(3, cls.trees.Count); t++)
                            lens[t] = cls.trees[t].talents?.Count ?? 0;
                    treeLengths[cls.id] = lens;
                }
            }

            var win = new ImportWindow(treeLengths) { Owner = this };
            if (win.ShowDialog() != true) return;

            string classId = win.ResultClassId;
            string buildString = win.ResultBuildString;

            // 1. Build kaydet
            _savedBuilds[classId] = buildString;

            // 2. Glif kaydet (varsa)
            if (win.HasGlyphs && _glyphDb.TryGetValue(classId, out var classGlyphs))
            {
                var sortedMajor = classGlyphs.major.OrderBy(g => g.itemId).ToList();
                var sortedMinor = classGlyphs.minor.OrderBy(g => g.itemId).ToList();

                if (win.ResultMajorGlyphIndices != null)
                {
                    var majorNames = new List<string>();
                    foreach (int idx in win.ResultMajorGlyphIndices)
                        if (idx < sortedMajor.Count)
                            majorNames.Add(sortedMajor[idx].name);
                    _savedMajorGlyphs[classId] = majorNames;
                }

                if (win.ResultMinorGlyphIndices != null)
                {
                    var minorNames = new List<string>();
                    foreach (int idx in win.ResultMinorGlyphIndices)
                        if (idx < sortedMinor.Count)
                            minorNames.Add(sortedMinor[idx].name);
                    _savedMinorGlyphs[classId] = minorNames;
                }
            }

            // 3. Sinifa gec ve ekrani guncelle
            var targetCls = _db?.classes?.FirstOrDefault(c => c.id == classId);
            if (targetCls == null) return;

            _selectedClass = null;
            SelectClass(targetCls);
            ApplySavedGlyphs(classId);
            SnapshotAndSave();

            // Glyph varsa paneli ac
            if (win.HasGlyphs && GlyphsPanel.Visibility == Visibility.Collapsed)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                this.Width = 940;
            }

            // 4. Kilitle
            if (!_isLocked)
                BtnLock_Click(null, null);
        }
        private void BtnExport_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedClass == null) return;

            // 1. Ağaçtaki güncel durumu string olarak al (Örn: "2035000...000")
            string rawBuild = BuildSerializer.Snapshot(_treeIcons);

            // 2. Sondan başlayarak sıfır (0) olmayan ilk karakterin indeksini bul
            int lastValidIndex = -1;
            for (int i = rawBuild.Length - 1; i >= 0; i--)
            {
                if (rawBuild[i] != '0')
                {
                    lastValidIndex = i;
                    break;
                }
            }

            // 3. Orijinal seriyi sıfır olmayan son karaktere kadar kırp. 
            // (Hiç puan verilmemişse boş string döner)
            string talValue = lastValidIndex >= 0 ? rawBuild.Substring(0, lastValidIndex + 1) : "";

            // 4. Sınıf ID'sini web sitesinin istediği Numeric ID'ye çevir
            string cid = GetWebClassId(_selectedClass.id);

            // 5. URL'yi oluştur ve Export penceresini aç
            string summaryUrl = $"https://wotlkdb.com/?talent#?cid={cid}&tal={talValue}";
            string shareLink = BuildWotlkDbLink();

            var exportWin = new ExportWindow(summaryUrl, shareLink) { Owner = this };
            exportWin.ShowDialog();
        }

        private string GetWebClassId(string classId)
        {
            // Web sitesinin Numeric ID eşleştirmeleri.
            return classId.ToLower() switch
            {
                "deathknight" => "6",
                "druid" => "11",
                "hunter" => "3",
                "mage" => "8",
                "paladin" => "2",
                "priest" => "5",
                "rogue" => "4",
                "shaman" => "7",
                "warlock" => "9",
                "warrior" => "1",
                _ => "6" // Fallback durumu
            };
        }

        // ── Glyphs Paneli Yönetimi ──────────────────────────────────────────────
        private void BtnGlyph_Click(object sender, MouseButtonEventArgs e)
        {
            if (GlyphsPanel.Visibility == Visibility.Collapsed)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                this.Width = 940; // Pencereyi Glyph paneli sığacak kadar genişlet
            }
            else
            {
                GlyphsPanel.Visibility = Visibility.Collapsed;
                this.Width = 720; // Pencereyi orijinal boyutuna döndür
            }
            ScheduleSave();
        }

        private void BtnClearGlyphs_MouseEnter(object sender, MouseEventArgs e) => BtnClearGlyphs.Opacity = 1.0;
        private void BtnClearGlyphs_MouseLeave(object sender, MouseEventArgs e) => BtnClearGlyphs.Opacity = 0.4;

        // Tüm Glifleri Temizleme (Çarpı Butonu)
        private void BtnClearGlyphs_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked && sender != null) return;
            ClearSingleGlyphSlot(MajorGlyph1, TxtMajorGlyph1);
            ClearSingleGlyphSlot(MajorGlyph2, TxtMajorGlyph2);
            ClearSingleGlyphSlot(MajorGlyph3, TxtMajorGlyph3);

            ClearSingleGlyphSlot(MinorGlyph1, TxtMinorGlyph1);
            ClearSingleGlyphSlot(MinorGlyph2, TxtMinorGlyph2);
            ClearSingleGlyphSlot(MinorGlyph3, TxtMinorGlyph3);

            if (sender != null) SnapshotAndSave();
        }

        private void ClearSingleGlyphSlot(IconControl icon, TextBlock text)
        {
            // İkonu boş slota çevir
            string emptySlotPath = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Utils", "inventoryslot_empty.jpg");
            icon.SpriteSheetPath = emptySlotPath;

            // Metni varsayılan hale getir
            text.Text = "Click to select";
            text.Foreground = (Brush)FindResource("TextDim");

            // Grid'in hafızasındaki (DataContext) glifi temizle
            if (icon.Parent is Grid parentGrid)
            {
                parentGrid.DataContext = null;
            }
        }

        private void GlyphSlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            if (_selectedClass == null || !(sender is Grid clickedGrid)) return;

            string slotType = clickedGrid.Tag?.ToString();
            if (!_glyphDb.TryGetValue(_selectedClass.id, out var classGlyphs)) return;

            var fullList = slotType == "Major" ? classGlyphs.major : classGlyphs.minor;

            // 1. Diğer slotlarda takılı olan glifleri tespit et
            var parentPanel = clickedGrid.Parent as StackPanel;
            var equippedInOtherSlots = new List<GlyphEntry>();

            if (parentPanel != null)
            {
                foreach (var child in parentPanel.Children)
                {
                    if (child is Grid g && g != clickedGrid && g.Tag?.ToString() == slotType)
                    {
                        if (g.DataContext is GlyphEntry entry)
                            equippedInOtherSlots.Add(entry);
                    }
                }
            }

            // 2. Diğer slotlarda takılı olanları diyalog listesinden filtrele (Aynı glifi engelle)
            var filteredList = fullList.Where(g => !equippedInOtherSlots.Contains(g)).ToList();

            GlyphDialogWindow dialog = new GlyphDialogWindow($"Select {slotType} Glyph", filteredList) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.SelectedGlyph == null)
                {
                    // None seçildi — slotu temizle
                    var iconCtrl = clickedGrid.Children.OfType<IconControl>().FirstOrDefault();
                    var textCtrl = clickedGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (iconCtrl != null && textCtrl != null)
                        ClearSingleGlyphSlot(iconCtrl, textCtrl);
                }
                else
                {
                    var selected = dialog.SelectedGlyph;
                    clickedGrid.DataContext = selected;
                    var iconCtrl = clickedGrid.Children.OfType<IconControl>().FirstOrDefault();
                    var textCtrl = clickedGrid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (iconCtrl != null) iconCtrl.SpriteSheetPath = selected.MediumImagePath;
                    if (textCtrl != null) { textCtrl.Text = selected.DisplayName; textCtrl.Foreground = Brushes.White; }
                    GlyphSlot_MouseEnter(clickedGrid, null);
                }
                SnapshotAndSave();
            }
        }

        // ── Sağ Tıkla Silme ──
        private void GlyphSlot_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked) return;
            if (sender is Grid clickedGrid)
            {
                var iconCtrl = clickedGrid.Children.OfType<IconControl>().FirstOrDefault();
                var textCtrl = clickedGrid.Children.OfType<TextBlock>().FirstOrDefault();

                if (iconCtrl != null && textCtrl != null)
                {
                    ClearSingleGlyphSlot(iconCtrl, textCtrl);
                    GlyphSlot_MouseEnter(clickedGrid, null); // Tooltip'i 'Empty' durumuna çevir
                    SnapshotAndSave();
                }
            }
        }

        // ── Hover Tooltip İşlemleri ──
        private void GlyphSlot_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid hoveredGrid)
            {
                string slotType = hoveredGrid.Tag?.ToString();
                var glyph = hoveredGrid.DataContext as GlyphEntry;

                if (glyph == null)
                    GlyphTooltip.RefreshData("", slotType, "", true, _isLocked);
                else
                    GlyphTooltip.RefreshData(glyph.name, slotType, glyph.description, false, _isLocked);

                TalentTooltip.Visibility = Visibility.Collapsed;
                GlyphTooltip.Visibility = Visibility.Visible;
                TooltipPopup.IsOpen = true;
            }
        }

        private void GlyphSlot_MouseLeave(object sender, MouseEventArgs e)
        {
            TooltipPopup.IsOpen = false;
        }

        // ── Yardımcılar ────────────────────────────────────────────────────────
        private int TotalPoints() =>
            _treeIcons.Where(t => t != null).Sum(t => t.Sum(x => x.CurrentRank));

        private void ScheduleSave() { _saveTimer.Stop(); _saveTimer.Start(); }

        private List<string> GetCurrentGlyphs(string type)
        {
            var list = new List<string>();
            var targets = type == "Major" ? new[] { MajorGlyph1, MajorGlyph2, MajorGlyph3 } : new[] { MinorGlyph1, MinorGlyph2, MinorGlyph3 };
            foreach (var icon in targets)
            {
                if (icon.Parent is Grid g && g.DataContext is GlyphEntry e)
                    list.Add(e.name);
            }
            return list;
        }

        private void ApplySavedGlyphs(string classId)
        {
            BtnClearGlyphs_Click(null, null); // Önce tüm slotları boşalt

            if (_glyphDb.TryGetValue(classId, out var classGlyphs))
            {
                if (_savedMajorGlyphs.TryGetValue(classId, out var majorList))
                {
                    var targets = new[] { MajorGlyph1, MajorGlyph2, MajorGlyph3 };
                    for (int i = 0; i < Math.Min(majorList.Count, 3); i++)
                    {
                        var g = classGlyphs.major.FirstOrDefault(x => x.name == majorList[i]);
                        if (g != null) SetGlyphSlot(targets[i], g);
                    }
                }

                if (_savedMinorGlyphs.TryGetValue(classId, out var minorList))
                {
                    var targets = new[] { MinorGlyph1, MinorGlyph2, MinorGlyph3 };
                    for (int i = 0; i < Math.Min(minorList.Count, 3); i++)
                    {
                        var g = classGlyphs.minor.FirstOrDefault(x => x.name == minorList[i]);
                        if (g != null) SetGlyphSlot(targets[i], g);
                    }
                }
            }
        }

        private void SetGlyphSlot(IconControl iconCtrl, GlyphEntry selected)
        {
            if (iconCtrl.Parent is Grid grid)
            {
                grid.DataContext = selected;
                iconCtrl.SpriteSheetPath = selected.MediumImagePath;
                var textCtrl = grid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textCtrl != null)
                {
                    textCtrl.Text = selected.DisplayName;
                    textCtrl.Foreground = Brushes.White;
                }
            }
        }

        // ── Tooltip ────────────────────────────────────────────────────────────
        private void ShowTooltip(IconControl icon, TreeDef tree, int treeIndex)
        {
            if (icon.IsClassIcon) return; // Sınıf ikonlarında tooltip yok

            var talent = icon.DataContext as TalentEntry;
            if (talent == null) return;

            // Açıklamaları bul
            string[] descriptions = Array.Empty<string>();
            string cost = null, range = null, castTime = null, cooldown = null;
            if (AssetManager.Descriptions != null &&
                AssetManager.Descriptions.TryGetValue(_selectedClass.id, out var classDescs) &&
                classDescs.TryGetValue(talent.name, out var tDesc) &&
                tDesc.ranks != null)
            {
                cost = tDesc.cost;
                range = tDesc.range;
                castTime = tDesc.castTime;
                cooldown = tDesc.cooldown;
                descriptions = tDesc.ranks.ToArray();
            }

            // Gerekli ağaç puanı (Satır * 5)
            int reqTreePoints = talent.row * 5;
            int currentTreePoints = _treeIcons[treeIndex].Sum(x => x.CurrentRank);

            // Bağlı (Attached) yetenek durumu
            string attachedName = talent.attached;
            int attachedReqRank = 0;
            int attachedCurRank = 0;

            if (!string.IsNullOrEmpty(attachedName) && attachedName != "none")
            {
                var reqIcon = _treeIcons[treeIndex].FirstOrDefault(x => ((TalentEntry)x.DataContext)?.name == attachedName);
                if (reqIcon != null)
                {
                    attachedReqRank = reqIcon.MaxRank;
                    attachedCurRank = reqIcon.CurrentRank;
                }
            }

            // UserControl'deki RefreshData metodunu çağırıp Popup'ı aç
            TalentTooltip.RefreshData(
                talentName: talent.name,
                currentRank: icon.CurrentRank,
                maxRank: icon.MaxRank,
                descriptions: descriptions,
                isActive: icon.IsActive,
                requiredTreePoints: reqTreePoints,
                currentTreePoints: currentTreePoints,
                treeName: tree.name,
                attachedTalentName: attachedName,
                attachedRequiredRank: attachedReqRank,
                attachedCurrentRank: attachedCurRank,
                cost: cost,
                range: range,
                castTime: castTime,
                cooldown: cooldown,
                isLocked: _isLocked
            );

            TalentTooltip.Visibility = Visibility.Visible;
            GlyphTooltip.Visibility = Visibility.Collapsed;
            TooltipPopup.IsOpen = true;
        }

        // ── Title bar ──────────────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
        private void BtnMinimize_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            WindowState = WindowState.Minimized;
        }
        private void BtnClose_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Close();
        }

        // wotlkdb link oluşturma
        private const string WotlkDbKey = "0zMcmVokRsaqbdrfwihuGINALpTjnyxtgevElBCDFHJKOPQSUWXYZ123456789";

        private static int GetWotlkDbClassIndex(string classId) => classId.ToLower() switch
        {
            "druid" => 0,
            "hunter" => 1,
            "mage" => 2,
            "paladin" => 3,
            "priest" => 4,
            "rogue" => 5,
            "shaman" => 6,
            "warlock" => 7,
            "warrior" => 8,
            "deathknight" => 9,
            _ => 0
        };

        private string BuildWotlkDbLink()
        {
            if (_selectedClass == null) return "";

            var sb = new StringBuilder();

            // 1. Sınıf karakteri
            int classIdx = GetWotlkDbClassIndex(_selectedClass.id);
            sb.Append(WotlkDbKey[classIdx * 3]);

            // 2. Talent encode
            for (int t = 0; t < 3; t++)
            {
                var icons = _treeIcons[t];
                if (icons == null || icons.Count == 0) { sb.Append('Z'); continue; }

                int treeLen = icons.Count;
                int i = 0;
                while (i < treeLen)
                {
                    // Kalan talentların hepsi 0 mı?
                    bool restZero = true;
                    for (int j = i; j < treeLen; j++)
                        if (icons[j].CurrentRank > 0) { restZero = false; break; }

                    if (restZero)
                    {
                        if (t < 2) sb.Append('Z'); // Son ağaçta gereksiz
                        break;
                    }

                    int r1 = icons[i].CurrentRank;
                    int r2 = (i + 1 < treeLen) ? icons[i + 1].CurrentRank : 0;
                    sb.Append(WotlkDbKey[r1 * 6 + r2]);
                    i += 2;
                }
            }

            // 3. Glyph encode
            string glyphPart = EncodeGlyphs();
            if (!string.IsNullOrEmpty(glyphPart))
                sb.Append(':').Append(glyphPart);

            return $"https://wotlkdb.com/?talent#{sb}";
        }

        private string EncodeGlyphs()
        {
            if (_selectedClass == null || !_glyphDb.TryGetValue(_selectedClass.id, out var classGlyphs))
                return "";

            var majorNames = GetCurrentGlyphs("Major");
            var minorNames = GetCurrentGlyphs("Minor");

            if (majorNames.Count == 0 && minorNames.Count == 0) return "";

            // itemId'ye göre sıralanmış listeler oluştur
            var sortedMajor = classGlyphs.major.OrderBy(g => g.itemId).ToList();
            var sortedMinor = classGlyphs.minor.OrderBy(g => g.itemId).ToList();

            var sb = new StringBuilder();

            foreach (var name in majorNames)
            {
                int idx = sortedMajor.FindIndex(g => g.name == name);
                if (idx >= 0 && idx < WotlkDbKey.Length)
                    sb.Append(WotlkDbKey[idx]);
            }

            if (majorNames.Count < 3 && minorNames.Count > 0)
                sb.Append('Z');

            foreach (var name in minorNames)
            {
                int idx = sortedMinor.FindIndex(g => g.name == name);
                if (idx >= 0 && idx < WotlkDbKey.Length)
                    sb.Append(WotlkDbKey[idx]);
            }

            return sb.ToString();
        }
    }
}