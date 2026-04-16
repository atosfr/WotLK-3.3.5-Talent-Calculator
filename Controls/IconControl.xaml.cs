using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WotLK_TalentCalculator_3._3._5.Services;

namespace WotLK_TalentCalculator_3._3._5.Controls
{
    public partial class IconControl : UserControl
    {
        private static readonly SolidColorBrush BrushRankGreen = new(Color.FromRgb(23, 253, 23));
        private static readonly SolidColorBrush BrushRankYellow = new(Color.FromRgb(231, 186, 0));

        private const int IconSize = 36;

        public IconControl PrerequisiteTalent { get; set; }

        // --- CROPPED BITMAP CACHE ---
        // Once SpriteSheetPath or IconIndex is set, these two are generated once.
        // As IsActive changes, RefreshIcon no longer performs cropping; it simply 
        // assigns the ready reference to PART_Icon.Source — zero allocation.
        private CroppedBitmap _cropActive;
        private CroppedBitmap _cropInactive;

        // --- DEPENDENCY PROPERTIES ---
        public static readonly DependencyProperty SpriteSheetPathProperty =
            DependencyProperty.Register(nameof(SpriteSheetPath), typeof(string), typeof(IconControl), new PropertyMetadata(null, OnIconSourceChanged));
        public static readonly DependencyProperty IconIndexProperty =
            DependencyProperty.Register(nameof(IconIndex), typeof(int), typeof(IconControl), new PropertyMetadata(0, OnIconSourceChanged));
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(IconControl), new PropertyMetadata(false, OnStateChanged));
        public static readonly DependencyProperty CurrentRankProperty =
            DependencyProperty.Register(nameof(CurrentRank), typeof(int), typeof(IconControl), new PropertyMetadata(0, OnStateChanged));
        public static readonly DependencyProperty MaxRankProperty =
            DependencyProperty.Register(nameof(MaxRank), typeof(int), typeof(IconControl), new PropertyMetadata(0, OnStateChanged));
        public static readonly DependencyProperty IsClassIconProperty =
            DependencyProperty.Register(nameof(IsClassIcon), typeof(bool), typeof(IconControl), new PropertyMetadata(false, OnStateChanged));
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(IconControl), new PropertyMetadata(false, OnStateChanged));
        public static readonly DependencyProperty RowProperty =
            DependencyProperty.Register(nameof(Row), typeof(int), typeof(IconControl), new PropertyMetadata(0));

        public string SpriteSheetPath { get => (string)GetValue(SpriteSheetPathProperty); set => SetValue(SpriteSheetPathProperty, value); }
        public int IconIndex { get => (int)GetValue(IconIndexProperty); set => SetValue(IconIndexProperty, value); }
        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
        public int CurrentRank { get => (int)GetValue(CurrentRankProperty); set => SetValue(CurrentRankProperty, value); }
        public int MaxRank { get => (int)GetValue(MaxRankProperty); set => SetValue(MaxRankProperty, value); }
        public bool IsClassIcon { get => (bool)GetValue(IsClassIconProperty); set => SetValue(IsClassIconProperty, value); }
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
        public int Row { get => (int)GetValue(RowProperty); set => SetValue(RowProperty, value); }

        public IconControl()
        {
            InitializeComponent();
            PART_Frame.Source = AssetManager.IconFrame;
            PART_BubbleImage.Source = AssetManager.Bubble;
        }

        // SpriteSheetPath or IconIndex change logic:
        // Regenerate crops when SpriteSheetPath or IconIndex changes, then refresh the screen.
        private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (IconControl)d;
            ctrl.BuildCrops();
            ctrl.RefreshAll();
        }

        // State change logic:
        // For other property changes, only refresh the UI (crops are already prepared).
        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((IconControl)d).RefreshAll();

        /// <summary>
        /// Generates active (row=0) and inactive (row=36) crops from the sprite sheet.
        /// This method is called only when SpriteSheetPath or IconIndex changes.
        /// We use prepared references instead of performing a bitmap crop every time IsActive changes.
        /// </summary>
        private void BuildCrops()
        {
            _cropActive = null;
            _cropInactive = null;

            if (string.IsNullOrWhiteSpace(SpriteSheetPath) || IsClassIcon) return;

            var sheet = AssetManager.LoadBitmap(SpriteSheetPath); // Load from cache, no disk I/O
            if (sheet == null) return;

            int col = IconIndex * IconSize;

            // Boundary check — prevents crash if JSON data is invalid
            if (col < 0 || col + IconSize > sheet.PixelWidth) return;

            // Active icon: top half of the sprite sheet (row = 0)
            if (IconSize <= sheet.PixelHeight)
            {
                _cropActive = new CroppedBitmap(sheet, new Int32Rect(col, 0, IconSize, IconSize));
                _cropActive.Freeze();
            }

            // Passive (darkened) icon: bottom half of the sprite sheet (row = IconSize)
            if (IconSize * 2 <= sheet.PixelHeight)
            {
                _cropInactive = new CroppedBitmap(sheet, new Int32Rect(col, IconSize, IconSize, IconSize));
                _cropInactive.Freeze();
            }
        }

        private void RefreshIcon()
        {
            if (IsClassIcon)
            {
                // Class icon is a single image — no crop needed, LoadBitmap returns from cache
                PART_Icon.Source = AssetManager.LoadBitmap(SpriteSheetPath);
                return;
            }

            // Assign ready crop reference — no new allocation
            PART_Icon.Source = IsActive ? _cropActive : _cropInactive;
        }

        private void RefreshAll()
        {
            RefreshIcon();

            PART_Frame.Visibility = IsClassIcon ? Visibility.Hidden : Visibility.Visible;

            if (IsClassIcon)
            {
                PART_Bubble.Visibility = Visibility.Hidden;
                PART_StateBorder.Source = IsSelected ? AssetManager.BorderYellow : AssetManager.BorderGray;
                PART_Highlight.Source = AssetManager.HiliteDefault;
            }
            else
            {
                bool isMaxed = MaxRank > 0 && CurrentRank >= MaxRank;
                PART_StateBorder.Source = !IsActive ? AssetManager.BorderGray
                                        : isMaxed ? AssetManager.BorderYellow
                                                    : AssetManager.BorderGreen;

                PART_RankText.Text = CurrentRank.ToString();
                PART_RankText.Foreground = isMaxed ? BrushRankYellow : BrushRankGreen;
                PART_Bubble.Visibility = IsActive ? Visibility.Visible : Visibility.Hidden;
                PART_Highlight.Source = IsActive ? AssetManager.HiliteTalent : AssetManager.HiliteDefault;
            }

            Cursor = Cursors.Hand;
            RefreshHighlight();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) => RefreshHighlight(true);
        private void OnMouseLeave(object sender, MouseEventArgs e) => RefreshHighlight(false);

        private void RefreshHighlight(bool? hovering = null)
        {
            bool over = hovering ?? IsMouseOver;
            bool keepHighlighted = IsClassIcon && IsSelected;
            PART_Highlight.Visibility = (over || keepHighlighted) ? Visibility.Visible : Visibility.Hidden;
        }

        public event EventHandler<bool> RankChangeRequested;

        private void OnLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (IsClassIcon) return;
            RankChangeRequested?.Invoke(this, true);
        }

        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (IsClassIcon || CurrentRank == 0) return;
            RankChangeRequested?.Invoke(this, false);
        }
    }
}