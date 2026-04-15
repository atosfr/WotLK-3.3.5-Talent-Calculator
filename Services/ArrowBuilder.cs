using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Yetenek ağacındaki okları canvas'a çizer.
    /// Dönen Dictionary (talentName → Rectangle listesi), UpdateTreeStates'in
    /// canvas'ı baştan taraması yerine O(1) lookup yapmasını sağlar. (FIX 7)
    /// </summary>
    public static class ArrowBuilder
    {
        private const int CellSize = 50;
        private const int Thick = 14;
        private const int HalfThick = Thick / 2;
        private const int Radius = 19;
        private const int Pierce = 6;

        /// <summary>
        /// Canvas'ı doldurur; önkoşul bağlantılarını ikonlara işler.
        /// Anahtar: hedef talent adı. Değer: o talente işaret eden tüm Rectangle'lar.
        /// </summary>
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

                // Önkoşul bağlantısını ikon nesnelerine aktar
                if (iconDict.TryGetValue(talent.name, out var childIcon) &&
                    iconDict.TryGetValue(talent.attached, out var reqIcon))
                    childIcon.PrerequisiteTalent = reqIcon;

                var arrows = new List<Rectangle>();

                int pX = parent.col * CellSize + CellSize / 2;
                int pY = parent.row * CellSize + CellSize / 2;
                int cX = talent.col * CellSize + CellSize / 2;
                int cY = talent.row * CellSize + CellSize / 2;

                if (parent.col == talent.col)
                    DrawStraightDown(canvas, arrows, talent.name, pX, pY, cY);
                else if (parent.row == talent.row)
                    DrawStraightHorizontal(canvas, arrows, talent.name, pX, pY, cX, talent.col > parent.col);
                else if (talent.col > parent.col)
                    DrawLShapeRightDown(canvas, arrows, talent.name, pX, pY, cX, cY);
                else
                    DrawLShapeLeftDown(canvas, arrows, talent.name, pX, pY, cX, cY);

                arrowMap[talent.name] = arrows;
            }

            return arrowMap;
        }

        // ── Düz aşağı ok ─────────────────────────────────────────
        private static void DrawStraightDown(
            Canvas canvas, List<Rectangle> arrows, string tag,
            int pX, int pY, int cY)
        {
            var r = Make(tag);
            r.Fill = Brush(AssetManager.ArrowDown, AlignmentX.Center, AlignmentY.Bottom);
            r.Width = Thick;
            r.Height = Math.Max(1, (cY - (Radius - Pierce)) - (pY + Radius));
            Place(r, pX - HalfThick, pY + Radius);
            canvas.Children.Add(r);
            arrows.Add(r);
        }

        // ── Yatay ok (sağ veya sol) ───────────────────────────────
        private static void DrawStraightHorizontal(
            Canvas canvas, List<Rectangle> arrows, string tag,
            int pX, int pY, int cX, bool goRight)
        {
            var r = Make(tag);
            r.Height = Thick;

            if (goRight)
            {
                r.Fill = Brush(AssetManager.ArrowRight, AlignmentX.Right, AlignmentY.Center);
                r.Width = Math.Max(1, (cX - (Radius - Pierce) - 2) - (pX + Radius + 2));
                Place(r, pX + Radius + 2, pY - HalfThick);
            }
            else
            {
                r.Fill = Brush(AssetManager.ArrowLeft, AlignmentX.Left, AlignmentY.Center);
                r.Width = Math.Max(1, (pX - Radius - 2) - (cX + (Radius - Pierce) + 2));
                Place(r, cX + (Radius - Pierce) + 2, pY - HalfThick);
            }

            canvas.Children.Add(r);
            arrows.Add(r);
        }

        // ── L şekli: sağ-aşağı ───────────────────────────────────
        private static void DrawLShapeRightDown(
            Canvas canvas, List<Rectangle> arrows, string tag,
            int pX, int pY, int cX, int cY)
        {
            double cornerH = AssetManager.ArrowRightDown.Height;
            double cornerTop = pY - cornerH / 2;

            // Yatay parça (köşe resmi)
            var corner = Make(tag, aliased: true);
            corner.Fill = Brush(AssetManager.ArrowRightDown, AlignmentX.Right, AlignmentY.Top);
            corner.Width = Math.Max(1, (cX + 7.5) - (pX + Radius + 2));
            corner.Height = cornerH;
            Place(corner, pX + Radius + 2, cornerTop);
            canvas.Children.Add(corner);
            arrows.Add(corner);

            // Düşey parça
            var vert = MakeVertical(tag, cornerTop, cornerH, cX, cY);
            canvas.Children.Add(vert);
            arrows.Add(vert);
        }

        // ── L şekli: sol-aşağı ───────────────────────────────────
        private static void DrawLShapeLeftDown(
            Canvas canvas, List<Rectangle> arrows, string tag,
            int pX, int pY, int cX, int cY)
        {
            double cornerH = AssetManager.ArrowLeftDown.Height;
            double cornerTop = pY - cornerH / 2;

            var corner = Make(tag, aliased: true);
            corner.Fill = Brush(AssetManager.ArrowLeftDown, AlignmentX.Left, AlignmentY.Top);
            corner.Width = Math.Max(1, (pX - Radius - 2) - (cX - 7.5));
            corner.Height = cornerH;
            Place(corner, cX - 7.5, cornerTop);
            canvas.Children.Add(corner);
            arrows.Add(corner);

            var vert = MakeVertical(tag, cornerTop, cornerH, cX, cY);
            canvas.Children.Add(vert);
            arrows.Add(vert);
        }

        // ── L şekillerindeki ortak düşey parça ───────────────────
        private static Rectangle MakeVertical(
            string tag, double cornerTop, double cornerH, int cX, int cY)
        {
            var vert = Make(tag, aliased: true);
            vert.Fill = Brush(AssetManager.ArrowDown, AlignmentX.Center, AlignmentY.Bottom);
            vert.Width = Thick;
            double vStartY = cornerTop + cornerH - Thick + 14;
            double vEndY = cY - (Radius - Pierce) - 1;
            vert.Height = Math.Max(1, vEndY - vStartY);
            Place(vert, cX - HalfThick, vStartY);
            return vert;
        }

        // ── Yardımcılar ──────────────────────────────────────────

        private static Rectangle Make(string tag, bool aliased = false)
        {
            var r = new Rectangle { Tag = tag };
            if (aliased) RenderOptions.SetEdgeMode(r, EdgeMode.Aliased);
            return r;
        }

        private static ImageBrush Brush(
            System.Windows.Media.ImageSource src,
            AlignmentX ax, AlignmentY ay)
            => new(src) { Stretch = Stretch.None, AlignmentX = ax, AlignmentY = ay };

        private static void Place(Rectangle r, double left, double top)
        {
            Canvas.SetLeft(r, left);
            Canvas.SetTop(r, top);
        }
    }
}