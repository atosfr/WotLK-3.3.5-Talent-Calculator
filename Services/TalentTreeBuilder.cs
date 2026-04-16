using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Sets up the Grid content for a talent tree (icons + arrow canvas).
    /// MainWindow receives the result and manages event bindings and the footer itself.
    /// </summary>
    public static class TalentTreeBuilder
    {
        private const int Rows = 11;
        private const int RowSize = 50;
        private const int Cols = 4;

        /// <param name="Icons">The list of created IconControl objects.</param>
        /// <param name="ArrowMap">talentName -> Rectangles pointing to that talent (O(1) lookup).</param>
        public record TreeBuildResult(
            List<IconControl> Icons,
            Dictionary<string, List<Rectangle>> ArrowMap);

        public static TreeBuildResult Build(Grid grid, TreeDef tree, string spritePath)
        {
            // Row definitions
            for (int r = 0; r < Rows; r++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowSize) });

            var icons = new List<IconControl>();
            var iconDict = new Dictionary<string, IconControl>();

            // Create icons and place them in the grid
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

            // Arrow canvas — sits on top of icons, excluded from hit-testing
            var arrowCanvas = new Canvas { IsHitTestVisible = false };
            Grid.SetRowSpan(arrowCanvas, Rows);
            Grid.SetColumnSpan(arrowCanvas, Cols);
            grid.Children.Add(arrowCanvas);

            // Draw arrows; also establishes prerequisite links within ArrowBuilder
            var arrowMap = ArrowBuilder.Build(arrowCanvas, tree.talents, iconDict);

            return new TreeBuildResult(icons, arrowMap);
        }
    }
}