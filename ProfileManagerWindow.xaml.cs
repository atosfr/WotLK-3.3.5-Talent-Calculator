using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Models;
using WotLK_TalentCalculator_3._3._5.Services;

namespace WotLK_TalentCalculator_3._3._5
{
    public partial class ProfileManagerWindow : Window
    {
        // ── Sınıf renkleri ───────────────────────────────────────────────────
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

        // ── Anlık build snapshot (constructor'da alındı) ──────────────────────
        private readonly Dictionary<string, string> _currentBuilds;
        private readonly Dictionary<string, List<string>> _currentMajorGlyphs;
        private readonly Dictionary<string, List<string>> _currentMinorGlyphs;
        private readonly string _currentClassId;
        private readonly string _currentClassName;
        private readonly string _currentDistribution;

        /// <summary>
        /// Kullanıcı "Yükle" butonuna bastığında set edilir.
        /// MainWindow ShowDialog() sonrasında bunu kontrol eder.
        /// </summary>
        public Profile LoadedProfile { get; private set; }

        // ── Constructor ──────────────────────────────────────────────────────
        public ProfileManagerWindow(
            Dictionary<string, string> currentBuilds,
            Dictionary<string, List<string>> currentMajorGlyphs,
            Dictionary<string, List<string>> currentMinorGlyphs,
            string currentClassId,
            string currentClassName,
            string currentDistribution)
        {
            InitializeComponent();

            _currentBuilds = currentBuilds;
            _currentMajorGlyphs = currentMajorGlyphs;
            _currentMinorGlyphs = currentMinorGlyphs;
            _currentClassId = currentClassId;
            _currentClassName = currentClassName;
            _currentDistribution = currentDistribution;

            // Aktif build bilgisini göster
            TxtActiveInfo.Text = $"{currentClassName}  {currentDistribution}";

            // Sınıf rengini uygula
            if (ClassColors.TryGetValue(currentClassId, out var color))
                TxtActiveInfo.Foreground = new SolidColorBrush(color);

            RebuildProfileList();
        }

        // ── Profil listesi ────────────────────────────────────────────────────
        private void RebuildProfileList()
        {
            ProfileListPanel.Children.Clear();
            var data = ProfileSerializer.Load();
            var profiles = data.Profiles;

            TxtCount.Text = $"  {profiles.Count} adet";

            if (profiles.Count == 0)
            {
                ProfileListPanel.Children.Add(new TextBlock
                {
                    Text = "Henüz kayıtlı profil yok.",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 153)),
                    FontSize = 12,
                    Margin = new Thickness(0, 24, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            // En yeni profil üstte
            for (int i = profiles.Count - 1; i >= 0; i--)
                ProfileListPanel.Children.Add(BuildProfileItem(profiles[i]));
        }

        private UIElement BuildProfileItem(Profile profile)
        {
            var classColor = ClassColors.TryGetValue(profile.ClassId, out var cc)
                ? new SolidColorBrush(cc)
                : Brushes.White;

            var outer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(19, 19, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 69)),
                BorderThickness = new Thickness(0, 0, 0, 1),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Renkli sol şerit
            var stripe = new Rectangle { Fill = classColor };
            grid.Children.Add(stripe);

            // İsim + detay
            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 9, 6, 9)
            };

            infoPanel.Children.Add(new TextBlock
            {
                Text = profile.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });

            var detail = new TextBlock { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
            detail.Inlines.Add(new Run(profile.ClassName ?? profile.ClassId) { Foreground = classColor });
            detail.Inlines.Add(new Run($"  {profile.Distribution}")
            { Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 153)) });

            infoPanel.Children.Add(detail);
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            // Yükle / Sil butonları
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var cap = profile;

            var loadBtn = MakeBtn("Yükle", Color.FromRgb(255, 209, 0));
            loadBtn.MouseLeftButtonUp += (_, _) =>
            {
                LoadedProfile = cap;
                DialogResult = true;
            };

            var delBtn = MakeBtn("✕", Color.FromRgb(204, 51, 51));
            delBtn.MouseLeftButtonUp += (_, _) =>
            {
                ProfileSerializer.DeleteProfile(cap.Id);
                RebuildProfileList();
            };

            btnRow.Children.Add(loadBtn);
            btnRow.Children.Add(new TextBlock { Width = 10 });
            btnRow.Children.Add(delBtn);
            Grid.SetColumn(btnRow, 2);
            grid.Children.Add(btnRow);

            // Hover efekti
            outer.MouseEnter += (_, _) =>
                outer.Background = new SolidColorBrush(Color.FromRgb(25, 25, 52));
            outer.MouseLeave += (_, _) =>
                outer.Background = new SolidColorBrush(Color.FromRgb(19, 19, 42));

            outer.Child = grid;
            return outer;
        }

        private static TextBlock MakeBtn(string text, Color color)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(color),
            };
            tb.MouseEnter += (s, _) => ((TextBlock)s).Opacity = 0.65;
            tb.MouseLeave += (s, _) => ((TextBlock)s).Opacity = 1.0;
            return tb;
        }

        // ── Kaydet ────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, MouseButtonEventArgs e) => TrySave();

        private void TxtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TrySave();
        }

        private void TrySave()
        {
            var name = TxtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                // TextBox sınırını kırmızıya çevir, 1 saniye sonra eski haline döndür
                TxtName.BorderBrush = new SolidColorBrush(Colors.IndianRed);
                var timer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, _) =>
                {
                    TxtName.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 69));
                    timer.Stop();
                };
                timer.Start();
                TxtName.Focus();
                return;
            }

            var profile = new Profile
            {
                Name = name,
                ClassId = _currentClassId,
                ClassName = _currentClassName,
                Distribution = _currentDistribution,
                Builds = _currentBuilds != null ? new Dictionary<string, string>(_currentBuilds) : new Dictionary<string, string>(),
                MajorGlyphs = new Dictionary<string, List<string>>(_currentMajorGlyphs),
                MinorGlyphs = new Dictionary<string, List<string>>(_currentMinorGlyphs),
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };

            ProfileSerializer.AddProfile(profile);
            TxtName.Clear();
            RebuildProfileList();
        }

        // ── Pencere yönetimi ──────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void BtnClose_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            DialogResult = false;
        }
    }
}