using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Bir talent ağacının Grid içeriğini (ikonlar + ok canvas'ı) kurar.
    /// MainWindow, sonucu alır; event bağlantılarını ve footer'ı kendisi yönetir.
    /// </summary>
    public static class TalentTreeBuilder
    {
        private const int Rows = 11;
        private const int RowSize = 50;
        private const int Cols = 4;

        /// <param name="Icons">Oluşturulan IconControl listesi.</param>
        /// <param name="ArrowMap">talentName → o talente işaret eden Rectangle'lar (FIX 7 lookup).</param>
        public record TreeBuildResult(
            List<IconControl> Icons,
            Dictionary<string, List<Rectangle>> ArrowMap);

        public static TreeBuildResult Build(Grid grid, TreeDef tree, string spritePath)
        {
            // Satır tanımları
            for (int r = 0; r < Rows; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowSize) });

            var icons = new List<IconControl>();
            var iconDict = new Dictionary<string, IconControl>();

            // İkonları oluştur ve grid'e yerleştir
            foreach (var talent in tree.talents)
            {
                var icon = new IconControl
                {
                    SpriteSheetPath = spritePath,
                    IconIndex = talent.icon,
                    MaxRank = talent.maxRank,
                    CurrentRank = 0,
                    Row = talent.row,
                    IsClassIcon = false,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    DataContext = talent,
                };

                Grid.SetRow(icon, talent.row);
                Grid.SetColumn(icon, talent.col);
                grid.Children.Add(icon);

                icons.Add(icon);
                iconDict[talent.name] = icon;
            }

            // Ok canvas'ı — ikonların üstüne, hit-test'e dahil değil
            var arrowCanvas = new Canvas { IsHitTestVisible = false };
            Grid.SetRowSpan(arrowCanvas, Rows);
            Grid.SetColumnSpan(arrowCanvas, Cols);
            grid.Children.Add(arrowCanvas);

            // Okları çiz; önkoşul bağlantılarını da ArrowBuilder içinde kurar
            var arrowMap = ArrowBuilder.Build(arrowCanvas, tree.talents, iconDict);

            return new TreeBuildResult(icons, arrowMap);
        }
    }
}