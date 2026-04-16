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

namespace WotLK_TalentCalculator_3._3._5.Windows
{
    public partial class ProfileManagerWindow : Window
    {
        private readonly Dictionary<string, string> _currentBuilds;
        private readonly Dictionary<string, List<string>> _currentMajorGlyphs;
        private readonly Dictionary<string, List<string>> _currentMinorGlyphs;
        private readonly string _currentClassId;
        private readonly string _currentClassName;
        private readonly string _currentDistribution;

        public Profile LoadedProfile { get; private set; }

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

            TxtActiveInfo.Text = $"{currentClassName}  {currentDistribution}";
            TxtActiveInfo.Foreground = ClassPalette.GetBrush(currentClassId);

            RebuildProfileList();
        }

        private void RebuildProfileList()
        {
            ProfileListPanel.Children.Clear();
            var profiles = ProfileSerializer.Load().Profiles;

            TxtCount.Text = $"  {profiles.Count} items";

            if (profiles.Count == 0)
            {
                ProfileListPanel.Children.Add(new TextBlock
                {
                    Text = "No saved profiles yet.",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 153)),
                    FontSize = 12,
                    Margin = new Thickness(0, 24, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            for (int i = profiles.Count - 1; i >= 0; i--)
                ProfileListPanel.Children.Add(BuildProfileItem(profiles[i]));
        }

        private UIElement BuildProfileItem(Profile profile)
        {
            var classColor = ClassPalette.GetBrush(profile.ClassId);

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

            var stripe = new Rectangle { Fill = classColor };
            grid.Children.Add(stripe);

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

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var cap = profile;

            var loadBtn = MakeBtn("Load", Color.FromRgb(255, 209, 0));
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

            outer.MouseEnter += (_, _) => outer.Background = new SolidColorBrush(Color.FromRgb(25, 25, 52));
            outer.MouseLeave += (_, _) => outer.Background = new SolidColorBrush(Color.FromRgb(19, 19, 42));

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
                TxtName.BorderBrush = new SolidColorBrush(Colors.IndianRed);
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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