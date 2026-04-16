using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Holds a pair of active/inactive bitmaps for an arrow.
    /// Assigned to Rectangle.Tag; TalentController.SetArrowColor accesses the correct bitmap 
    /// via a single property access (no 5-level ImageSource comparison).
    /// </summary>
    public sealed class ArrowPair
    {
        public BitmapImage Active { get; }
        public BitmapImage Inactive { get; }
        public ArrowPair(BitmapImage active, BitmapImage inactive)
        {
            Active = active;
            Inactive = inactive;
        }
    }

    /// <summary>
    /// Draws talent tree arrows on the canvas.
    /// The returned Dictionary (talentName -> list of Rectangles) allows UpdateTreeStates 
    /// to perform O(1) lookups.
    /// </summary>
    public static class ArrowBuilder
    {
        private const int CellSize = 50;
        private const int Thick = 14;
        private const int HalfThick = Thick / 2;
        private const int Radius = 19;
        private const int Pierce = 6;

        public static Dictionary<string, List<Rectangle>> Build(
            Canvas canvas,
            IReadOnlyList<TalentEntry> talents,
            IReadOnlyDictionary<string, IconControl> iconDict)
        {
            var arrowMap = new Dictionary<string, List<Rectangle>>();

            foreach (var talent in talents)
            {
                if (string.IsNullOrWhiteSpace(talent.attached) || talent.attached == "none") continue;

                var parent = talents.FirstOrDefault(x => x.name == talent.attached);
                if (parent == null) continue;

                if (iconDict.TryGetValue(talent.name, out var childIcon) &&
                    iconDict.TryGetValue(talent.attached, out var reqIcon))
                    childIcon.PrerequisiteTalent = reqIcon;

                var arrows = new List<Rectangle>();

                int pX = parent.col * CellSize + CellSize / 2;
                int pY = parent.row * CellSize + CellSize / 2;
                int cX = talent.col * CellSize + CellSize / 2;
                int cY = talent.row * CellSize + CellSize / 2;

                if (parent.col == talent.col)
                    DrawStraightDown(canvas, arrows, pX, pY, cY);
                else if (parent.row == talent.row)
                    DrawStraightHorizontal(canvas, arrows, pX, pY, cX, talent.col > parent.col);
                else if (talent.col > parent.col)
                    DrawLShapeRightDown(canvas, arrows, pX, pY, cX, cY);
                else
                    DrawLShapeLeftDown(canvas, arrows, pX, pY, cX, cY);

                arrowMap[talent.name] = arrows;
            }

            return arrowMap;
        }

        // ── Direction pairs (defined in one place) ───────────────
        private static ArrowPair PairDown => new(AssetManager.ArrowDown2, AssetManager.ArrowDown);
        private static ArrowPair PairLeft => new(AssetManager.ArrowLeft2, AssetManager.ArrowLeft);
        private static ArrowPair PairRight => new(AssetManager.ArrowRight2, AssetManager.ArrowRight);
        private static ArrowPair PairLeftDown => new(AssetManager.ArrowLeftDown2, AssetManager.ArrowLeftDown);
        private static ArrowPair PairRightDown => new(AssetManager.ArrowRightDown2, AssetManager.ArrowRightDown);

        // ── Straight down arrow ──────────────────────────────────
        private static void DrawStraightDown(Canvas canvas, List<Rectangle> arrows, int pX, int pY, int cY)
        {
            var r = Make(PairDown);
            r.Fill = Brush(AssetManager.ArrowDown, AlignmentX.Center, AlignmentY.Bottom);
            r.Width = Thick;
            r.Height = System.Math.Max(1, (cY - (Radius - Pierce)) - (pY + Radius));
            Place(r, pX - HalfThick, pY + Radius);
            canvas.Children.Add(r);
            arrows.Add(r);
        }

        // ── Horizontal arrow (right or left) ─────────────────────
        private static void DrawStraightHorizontal(Canvas canvas, List<Rectangle> arrows, int pX, int pY, int cX, bool goRight)
        {
            var r = Make(goRight ? PairRight : PairLeft);
            r.Height = Thick;

            if (goRight)
            {
                r.Fill = Brush(AssetManager.ArrowRight, AlignmentX.Right, AlignmentY.Center);
                r.Width = System.Math.Max(1, (cX - (Radius - Pierce) - 2) - (pX + Radius + 2));
                Place(r, pX + Radius + 2, pY - HalfThick);
            }
            else
            {
                r.Fill = Brush(AssetManager.ArrowLeft, AlignmentX.Left, AlignmentY.Center);
                r.Width = System.Math.Max(1, (pX - Radius - 2) - (cX + (Radius - Pierce) + 2));
                Place(r, cX + (Radius - Pierce) + 2, pY - HalfThick);
            }

            canvas.Children.Add(r);
            arrows.Add(r);
        }

        // ── L shape: right-down ──────────────────────────────────
        private static void DrawLShapeRightDown(Canvas canvas, List<Rectangle> arrows, int pX, int pY, int cX, int cY)
        {
            double cornerH = AssetManager.ArrowRightDown.Height;
            double cornerTop = pY - cornerH / 2;

            var corner = Make(PairRightDown, aliased: true);
            corner.Fill = Brush(AssetManager.ArrowRightDown, AlignmentX.Right, AlignmentY.Top);
            corner.Width = System.Math.Max(1, (cX + 7.5) - (pX + Radius + 2));
            corner.Height = cornerH;
            Place(corner, pX + Radius + 2, cornerTop);
            canvas.Children.Add(corner);
            arrows.Add(corner);

            var vert = MakeVertical(cornerTop, cornerH, cX, cY);
            canvas.Children.Add(vert);
            arrows.Add(vert);
        }

        // ── L shape: left-down ────────────────────────────────────
        private static void DrawLShapeLeftDown(Canvas canvas, List<Rectangle> arrows, int pX, int pY, int cX, int cY)
        {
            double cornerH = AssetManager.ArrowLeftDown.Height;
            double cornerTop = pY - cornerH / 2;

            var corner = Make(PairLeftDown, aliased: true);
            corner.Fill = Brush(AssetManager.ArrowLeftDown, AlignmentX.Left, AlignmentY.Top);
            corner.Width = System.Math.Max(1, (pX - Radius - 2) - (cX - 7.5));
            corner.Height = cornerH;
            Place(corner, cX - 7.5, cornerTop);
            canvas.Children.Add(corner);
            arrows.Add(corner);

            var vert = MakeVertical(cornerTop, cornerH, cX, cY);
            canvas.Children.Add(vert);
            arrows.Add(vert);
        }

        // ── Shared vertical part in L shapes ──────────────────────
        private static Rectangle MakeVertical(double cornerTop, double cornerH, int cX, int cY)
        {
            var vert = Make(PairDown, aliased: true);
            vert.Fill = Brush(AssetManager.ArrowDown, AlignmentX.Center, AlignmentY.Bottom);
            vert.Width = Thick;
            double vStartY = cornerTop + cornerH - Thick + 14;
            double vEndY = cY - (Radius - Pierce) - 1;
            vert.Height = System.Math.Max(1, vEndY - vStartY);
            Place(vert, cX - HalfThick, vStartY);
            return vert;
        }

        // ── Helpers ───────────────────────────────────────────────
        private static Rectangle Make(ArrowPair pair, bool aliased = false)
        {
            var r = new Rectangle { Tag = pair };
            if (aliased) RenderOptions.SetEdgeMode(r, EdgeMode.Aliased);
            return r;
        }

        private static ImageBrush Brush(ImageSource src, AlignmentX ax, AlignmentY ay)
            => new(src) { Stretch = Stretch.None, AlignmentX = ax, AlignmentY = ay };

        private static void Place(Rectangle r, double left, double top)
        {
            Canvas.SetLeft(r, left);
            Canvas.SetTop(r, top);
        }
    }
}