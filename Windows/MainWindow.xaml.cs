using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;
using WotLK_TalentCalculator_3._3._5.Services;
using WotLK_TalentCalculator_3._3._5.ViewModels;
using IOPath = System.IO.Path;

namespace WotLK_TalentCalculator_3._3._5.Windows
{
    public partial class MainWindow : Window
    {
        // ── Data ────────────────────────────────────────────────────────────
        private TalentDb _db;
        private Dictionary<string, ClassGlyphs> _glyphDb = new();
        private ClassDef _selectedClass;

        // ── Saved builds ────────────────────────────────────────────────────
        private Dictionary<string, string> _savedBuilds = new();
        private Dictionary<string, List<string>> _savedMajorGlyphs = new();
        private Dictionary<string, List<string>> _savedMinorGlyphs = new();

        // ── UI state ────────────────────────────────────────────────────────
        private readonly List<IconControl> _classIcons = new();
        private readonly TextBlock[] _treePointsBlocks = new TextBlock[3];

        // Store event handlers bound to active class icons for unsubscription during class switching (prevents memory leaks)
        private readonly List<(IconControl Icon, MouseEventHandler Enter, MouseEventHandler Leave)> _iconHoverBindings = new();

        // ── Services ────────────────────────────────────────────────────────
        private readonly TalentController _talentController = new();
        private GlyphController _glyphController;
        private TooltipPresenter _tooltipPresenter;
        private readonly DispatcherTimer _saveTimer;

        public TalentInfoViewModel InfoVM { get; } = new();

        // ────────────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            AssetManager.Initialize();
            ImgResetAll.Source = AssetManager.DeleteIcon;
            ImgLock.Source = AssetManager.LockedIcon;

            InitializeGlyphSlots();
            InitializeAppIcon();
            InitializeServices();

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveTimer.Tick += (_, _) =>
            {
                _saveTimer.Stop();
                BuildSerializer.Save(_savedBuilds, _savedMajorGlyphs, _savedMinorGlyphs,
                                     _selectedClass?.id ?? "",
                                     GlyphsPanel.Visibility == Visibility.Visible);
            };

            LoadTalentData();
            LoadSaveData();
            BuildClassBar();
            SelectInitialClass();
            RestoreGlyphPanelState();
            RefreshProfileCount();

            _talentController.Changed += OnTalentChanged;

            // Defer Glyph JSON loading until after initial UI render to speed up startup
            Dispatcher.InvokeAsync(LoadGlyphData, DispatcherPriority.Background);
        }

        private void InitializeGlyphSlots()
        {
            BtnClearGlyphs.Source = AssetManager.DeleteIcon;
            string emptySlot = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "Utils", "inventoryslot_empty.jpg");
            MajorGlyph1.SpriteSheetPath = emptySlot;
            MajorGlyph2.SpriteSheetPath = emptySlot;
            MajorGlyph3.SpriteSheetPath = emptySlot;
            MinorGlyph1.SpriteSheetPath = emptySlot;
            MinorGlyph2.SpriteSheetPath = emptySlot;
            MinorGlyph3.SpriteSheetPath = emptySlot;
        }

        private void InitializeAppIcon()
        {
            string path = IOPath.Combine(AssetManager.BaseDir, "Image", "Icons", "SpecIcons", "Hunter", "hunter_marksmanship.gif");
            var bmp = AssetManager.LoadBitmap(path);
            if (bmp != null)
            {
                Icon = bmp;
                AppIconImage.Source = bmp;
            }
        }

        private void InitializeServices()
        {
            _glyphController = new GlyphController(
                majorIcons: new[] { MajorGlyph1, MajorGlyph2, MajorGlyph3 },
                minorIcons: new[] { MinorGlyph1, MinorGlyph2, MinorGlyph3 },
                majorTexts: new[] { TxtMajorGlyph1, TxtMajorGlyph2, TxtMajorGlyph3 },
                minorTexts: new[] { TxtMinorGlyph1, TxtMinorGlyph2, TxtMinorGlyph3 },
                dimBrush: (Brush)FindResource("TextDim"));

            _glyphController.Changed += ScheduleSave;

            _tooltipPresenter = new TooltipPresenter(TooltipPopup, TalentTooltip, GlyphTooltip);
        }

        // ────────────────────────────────────────────────────────────────────
        // Data Loading
        // ────────────────────────────────────────────────────────────────────
        private void LoadTalentData()
        {
            try
            {
                if (System.IO.File.Exists(AssetManager.JsonPath))
                {
                    _db = System.Text.Json.JsonSerializer.Deserialize<TalentDb>(
                        System.IO.File.ReadAllText(AssetManager.JsonPath),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load Talents.json:\n{ex.Message}", "Error");
            }
        }

        private void LoadGlyphData()
        {
            try
            {
                string glyphPath = IOPath.Combine(AssetManager.BaseDir, "Glyphs.json");
                if (!System.IO.File.Exists(glyphPath)) return;

                _glyphDb = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ClassGlyphs>>(
                    System.IO.File.ReadAllText(glyphPath),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                foreach (var kvp in _glyphDb)
                {
                    string cId = kvp.Key;
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

                // Glyph DB loaded — apply saved glyphs for the active class
                if (_selectedClass != null)
                    _glyphController.ApplyForClass(_selectedClass.id, _glyphDb, _savedMajorGlyphs, _savedMinorGlyphs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load Glyphs.json:\n{ex.Message}", "Error");
            }
        }

        private void LoadSaveData()
        {
            var saveData = BuildSerializer.Load();
            _savedBuilds = saveData.Builds ?? new Dictionary<string, string>();
            _savedMajorGlyphs = saveData.MajorGlyphs ?? new Dictionary<string, List<string>>();
            _savedMinorGlyphs = saveData.MinorGlyphs ?? new Dictionary<string, List<string>>();
        }

        private void SelectInitialClass()
        {
            if (_db?.classes?.Count > 0)
            {
                var saveData = BuildSerializer.Load();
                var target = _db.classes.FirstOrDefault(c => c.id == saveData.LastClassId) ?? _db.classes[0];
                SelectClass(target);
            }
        }

        private void RestoreGlyphPanelState()
        {
            var saveData = BuildSerializer.Load();
            if (saveData.GlyphPanelOpen)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                Width = 940;
            }
            else
            {
                GlyphsPanel.Visibility = Visibility.Collapsed;
                Width = 720;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_saveTimer.IsEnabled)
            {
                _saveTimer.Stop();
                BuildSerializer.Save(_savedBuilds, _savedMajorGlyphs, _savedMinorGlyphs,
                                     _selectedClass?.id ?? "",
                                     GlyphsPanel.Visibility == Visibility.Visible);
            }
            base.OnClosing(e);
        }

        // ────────────────────────────────────────────────────────────────────
        // Class Bar & Selection
        // ────────────────────────────────────────────────────────────────────
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

        private void SelectClass(ClassDef cls)
        {
            if (_selectedClass?.id == cls.id) return;

            // Clean up hover bindings of the previous class — memory leak prevention
            UnsubscribeOldIconHovers();

            _selectedClass = cls;
            for (int i = 0; i < _classIcons.Count; i++)
                _classIcons[i].IsSelected = (_db.classes[i].id == cls.id);

            var trees = cls.trees ?? new List<TreeDef>();
            var grids = new[] { Tree1Grid, Tree2Grid, Tree3Grid };
            var bgs = new[] { Tree1Bg, Tree2Bg, Tree3Bg };
            var footers = new[] { FooterTree1, FooterTree2, FooterTree3 };

            var treeIcons = new List<IconControl>[3];
            var arrowMaps = new Dictionary<string, List<System.Windows.Shapes.Rectangle>>[3];

            for (int t = 0; t < 3; t++)
            {
                grids[t].Children.Clear();
                grids[t].RowDefinitions.Clear();
                footers[t].Children.Clear();

                if (t >= trees.Count) { bgs[t].Source = null; continue; }

                var tree = trees[t];
                bgs[t].Source = AssetManager.LoadBitmap(AssetManager.GetBackgroundPath(cls.id, tree.background));

                var result = TalentTreeBuilder.Build(grids[t], tree,
                    AssetManager.GetSpriteSheetPath(cls.id, tree.spriteSheet));

                treeIcons[t] = result.Icons;
                arrowMaps[t] = result.ArrowMap;

                int tIdx = t;
                var localTree = tree;
                var localIcons = treeIcons;
                foreach (var icon in result.Icons)
                {
                    var enterHandler = new MouseEventHandler((s, _) =>
                        _tooltipPresenter.ShowTalent((IconControl)s, localTree, tIdx, cls.id, localIcons, _talentController.IsLocked));
                    var leaveHandler = new MouseEventHandler((_, _) => _tooltipPresenter.Hide());

                    icon.MouseEnter += enterHandler;
                    icon.MouseLeave += leaveHandler;

                    // Store for unsubscription
                    _iconHoverBindings.Add((icon, enterHandler, leaveHandler));
                }

                BuildFooterItem(footers[t], tree, t);
            }

            _talentController.Initialize(treeIcons, arrowMaps, _treePointsBlocks);

            if (_glyphDb.Count > 0)
                _glyphController.ApplyForClass(cls.id, _glyphDb, _savedMajorGlyphs, _savedMinorGlyphs);

            if (_savedBuilds.TryGetValue(cls.id, out var saved) && !string.IsNullOrEmpty(saved))
                _talentController.ApplyBuildString(saved);

            UpdateInfoBar();
            ScheduleSave();
        }

        private void UnsubscribeOldIconHovers()
        {
            foreach (var (icon, enter, leave) in _iconHoverBindings)
            {
                icon.MouseEnter -= enter;
                icon.MouseLeave -= leave;
            }
            _iconHoverBindings.Clear();
        }

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
            del.MouseLeftButtonUp += (_, _) => _talentController.ResetTree(treeIndex);
            panel.Children.Add(del);
        }

        // ────────────────────────────────────────────────────────────────────
        // Talent Changes
        // ────────────────────────────────────────────────────────────────────
        private void OnTalentChanged()
        {
            UpdateInfoBar();
            SnapshotAndSave();

            // Refresh the hovered icon's tooltip if it's currently open
            if (!_tooltipPresenter.IsOpen || _selectedClass == null) return;

            for (int t = 0; t < 3; t++)
            {
                if (_talentController.TreeIcons[t] == null) continue;
                var hovered = _talentController.TreeIcons[t].FirstOrDefault(x => x.IsMouseOver);
                if (hovered == null) continue;
                _tooltipPresenter.ShowTalent(hovered, _selectedClass.trees[t], t,
                    _selectedClass.id, _talentController.TreeIcons, _talentController.IsLocked);
                return;
            }
        }

        private void UpdateInfoBar()
        {
            if (_selectedClass == null)
            {
                InfoVM.Update("", "", 0, 0, 0);
                return;
            }
            InfoVM.Update(_selectedClass.id, _selectedClass.name,
                _talentController.TreePoints(0),
                _talentController.TreePoints(1),
                _talentController.TreePoints(2));
        }

        private void BtnResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_talentController.IsLocked) return;
            ClearAllGlyphSlots(saveAfter: false);
            _talentController.ResetAll();
        }

        private void BtnLock_Click(object sender, MouseButtonEventArgs e) => ToggleLock();

        private void ToggleLock()
        {
            _talentController.IsLocked = !_talentController.IsLocked;
            TxtLock.Text = _talentController.IsLocked ? "Unlock" : "Lock";
            ImgLock.Source = _talentController.IsLocked ? AssetManager.UnlockedIcon : AssetManager.LockedIcon;
        }

        // ────────────────────────────────────────────────────────────────────
        // Glyph UI
        // ────────────────────────────────────────────────────────────────────
        private void BtnGlyph_Click(object sender, MouseButtonEventArgs e)
        {
            if (GlyphsPanel.Visibility == Visibility.Collapsed)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                Width = 940;
            }
            else
            {
                GlyphsPanel.Visibility = Visibility.Collapsed;
                Width = 720;
            }
            ScheduleSave();
        }

        private void BtnClearGlyphs_MouseEnter(object sender, MouseEventArgs e) => BtnClearGlyphs.Opacity = 1.0;
        private void BtnClearGlyphs_MouseLeave(object sender, MouseEventArgs e) => BtnClearGlyphs.Opacity = 0.4;

        // UI click → locked control + save
        private void BtnClearGlyphs_Click(object sender, MouseButtonEventArgs e)
        {
            if (_talentController.IsLocked) return;
            ClearAllGlyphSlots(saveAfter: true);
        }

        private void ClearAllGlyphSlots(bool saveAfter)
        {
            _glyphController.ClearAll();
            if (saveAfter) SnapshotAndSave();
        }

        private void GlyphSlot_Click(object sender, MouseButtonEventArgs e)
        {
            if (_talentController.IsLocked) return;
            if (_selectedClass == null || sender is not Grid clickedGrid) return;

            string slotType = clickedGrid.Tag?.ToString();
            if (!_glyphDb.TryGetValue(_selectedClass.id, out var classGlyphs)) return;

            var fullList = slotType == "Major" ? classGlyphs.major : classGlyphs.minor;
            var equipped = GetEquippedInOtherSlots(clickedGrid, slotType);
            var filtered = fullList.Where(g => !equipped.Contains(g)).ToList();

            var dialog = new GlyphDialogWindow($"Select {slotType} Glyph", filtered) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            if (dialog.SelectedGlyph == null)
                _glyphController.ClearSlot(clickedGrid);
            else
                _glyphController.SetSlot(clickedGrid, dialog.SelectedGlyph);

            GlyphSlot_MouseEnter(clickedGrid, null);
            SnapshotAndSave();
        }

        private List<GlyphEntry> GetEquippedInOtherSlots(Grid clickedGrid, string slotType)
        {
            var equipped = new List<GlyphEntry>();
            if (clickedGrid.Parent is not StackPanel panel) return equipped;

            foreach (var child in panel.Children)
                if (child is Grid g && g != clickedGrid && g.Tag?.ToString() == slotType)
                    if (g.DataContext is GlyphEntry e) equipped.Add(e);

            return equipped;
        }

        private void GlyphSlot_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (_talentController.IsLocked) return;
            if (sender is not Grid clickedGrid) return;

            _glyphController.ClearSlot(clickedGrid);
            GlyphSlot_MouseEnter(clickedGrid, null);
            SnapshotAndSave();
        }

        private void GlyphSlot_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Grid hoveredGrid) return;
            string slotType = hoveredGrid.Tag?.ToString();
            var glyph = hoveredGrid.DataContext as GlyphEntry;
            _tooltipPresenter.ShowGlyph(glyph, slotType, _talentController.IsLocked);
        }

        private void GlyphSlot_MouseLeave(object sender, MouseEventArgs e) => _tooltipPresenter.Hide();

        // ────────────────────────────────────────────────────────────────────
        // Profiles
        // ────────────────────────────────────────────────────────────────────
        private void BtnProfiles_Click(object sender, MouseButtonEventArgs e)
        {
            if (_selectedClass == null) return;

            var singleBuild = new Dictionary<string, string>
            {
                [_selectedClass.id] = BuildSerializer.Snapshot(_talentController.TreeIcons)
            };
            var singleMajor = new Dictionary<string, List<string>>
            {
                [_selectedClass.id] = _glyphController.GetCurrent("Major")
            };
            var singleMinor = new Dictionary<string, List<string>>
            {
                [_selectedClass.id] = _glyphController.GetCurrent("Minor")
            };

            var win = new ProfileManagerWindow(
                currentBuilds: singleBuild,
                currentMajorGlyphs: singleMajor,
                currentMinorGlyphs: singleMinor,
                currentClassId: _selectedClass.id,
                currentClassName: _selectedClass.name,
                currentDistribution: GetDistributionString())
            { Owner = this };

            if (win.ShowDialog() == true && win.LoadedProfile != null)
                LoadProfile(win.LoadedProfile);

            RefreshProfileCount();
        }

        private void LoadProfile(Profile profile)
        {
            foreach (var kv in profile.Builds) _savedBuilds[kv.Key] = kv.Value;
            if (profile.MajorGlyphs != null)
                foreach (var kv in profile.MajorGlyphs) _savedMajorGlyphs[kv.Key] = kv.Value;
            if (profile.MinorGlyphs != null)
                foreach (var kv in profile.MinorGlyphs) _savedMinorGlyphs[kv.Key] = kv.Value;

            var cls = _db?.classes?.FirstOrDefault(c => c.id == profile.ClassId);
            if (cls == null) return;

            _selectedClass = null; // Bypass early return logic in SelectClass
            SelectClass(cls);

            SnapshotAndSave();
        }

        private void RefreshProfileCount()
        {
            var count = ProfileSerializer.Load().Profiles.Count;
            TxtProfileCount.Text = count > 0 ? $"({count})" : "";
        }

        private string GetDistributionString()
            => $"{_talentController.TreePoints(0)}/{_talentController.TreePoints(1)}/{_talentController.TreePoints(2)}";

        // ────────────────────────────────────────────────────────────────────
        // Import / Export
        // ────────────────────────────────────────────────────────────────────
        private void BtnImport_Click(object sender, MouseButtonEventArgs e)
        {
            var treeLengths = BuildTreeLengthsMap();
            var win = new ImportWindow(treeLengths) { Owner = this };
            if (win.ShowDialog() != true) return;

            string classId = win.ResultClassId;
            _savedBuilds[classId] = win.ResultBuildString;

            if (win.HasGlyphs && _glyphDb.TryGetValue(classId, out var cg))
            {
                var sortedMajor = cg.major.OrderBy(g => g.itemId).ToList();
                var sortedMinor = cg.minor.OrderBy(g => g.itemId).ToList();

                if (win.ResultMajorGlyphIndices != null)
                {
                    var names = new List<string>();
                    foreach (int idx in win.ResultMajorGlyphIndices)
                        if (idx < sortedMajor.Count) names.Add(sortedMajor[idx].name);
                    _savedMajorGlyphs[classId] = names;
                }

                if (win.ResultMinorGlyphIndices != null)
                {
                    var names = new List<string>();
                    foreach (int idx in win.ResultMinorGlyphIndices)
                        if (idx < sortedMinor.Count) names.Add(sortedMinor[idx].name);
                    _savedMinorGlyphs[classId] = names;
                }
            }

            var targetCls = _db?.classes?.FirstOrDefault(c => c.id == classId);
            if (targetCls == null) return;

            _selectedClass = null;
            SelectClass(targetCls);

            SnapshotAndSave();

            if (win.HasGlyphs && GlyphsPanel.Visibility == Visibility.Collapsed)
            {
                GlyphsPanel.Visibility = Visibility.Visible;
                Width = 940;
            }

            if (!_talentController.IsLocked) ToggleLock();
        }

        private Dictionary<string, int[]> BuildTreeLengthsMap()
        {
            var map = new Dictionary<string, int[]>();
            if (_db?.classes == null) return map;

            foreach (var cls in _db.classes)
            {
                var lens = new int[3];
                if (cls.trees != null)
                    for (int t = 0; t < Math.Min(3, cls.trees.Count); t++)
                        lens[t] = cls.trees[t].talents?.Count ?? 0;
                map[cls.id] = lens;
            }
            return map;
        }

        private void BtnExport_Click(object sender, MouseButtonEventArgs e)
        {
            if (_selectedClass == null) return;

            string summaryUrl = WotlkDbCodec.BuildSummaryUrl(_selectedClass.id, _talentController.TreeIcons);
            string shareLink = WotlkDbCodec.BuildFullLink(
                _selectedClass.id,
                _talentController.TreeIcons,
                _glyphDb,
                _glyphController.GetCurrent("Major"),
                _glyphController.GetCurrent("Minor"));

            new ExportWindow(summaryUrl, shareLink) { Owner = this }.ShowDialog();
        }

        // ────────────────────────────────────────────────────────────────────
        // Saving
        // ────────────────────────────────────────────────────────────────────
        private void SnapshotAndSave()
        {
            if (_selectedClass == null) return;
            _savedBuilds[_selectedClass.id] = BuildSerializer.Snapshot(_talentController.TreeIcons);
            _savedMajorGlyphs[_selectedClass.id] = _glyphController.GetCurrent("Major");
            _savedMinorGlyphs[_selectedClass.id] = _glyphController.GetCurrent("Minor");
            ScheduleSave();
        }

        private void ScheduleSave()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // ────────────────────────────────────────────────────────────────────
        // Title bar
        // ────────────────────────────────────────────────────────────────────
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
    }
}