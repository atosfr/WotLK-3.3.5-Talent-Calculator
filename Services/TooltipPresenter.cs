using System.Windows;
using System.Windows.Controls.Primitives;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Displays tooltips for talents and glyphs.
    /// Receives MainWindow's own tooltip UI references (Popup, TalentTooltipControl, GlyphTooltipControl)
    /// in the constructor.
    /// </summary>
    public class TooltipPresenter
    {
        private readonly Popup _popup;
        private readonly TalentTooltipControl _talentTooltip;
        private readonly GlyphTooltipControl _glyphTooltip;

        public TooltipPresenter(Popup popup, TalentTooltipControl talent, GlyphTooltipControl glyph)
        {
            _popup = popup;
            _talentTooltip = talent;
            _glyphTooltip = glyph;
        }

        public void ShowTalent(
            IconControl icon, TreeDef tree, int treeIndex,
            string classId, System.Collections.Generic.List<IconControl>[] treeIcons,
            bool isLocked)
        {
            if (icon.IsClassIcon) return;
            if (icon.DataContext is not TalentEntry talent) return;

            // Find descriptions
            string[] descriptions = System.Array.Empty<string>();
            string cost = null, range = null, castTime = null, cooldown = null;

            if (AssetManager.Descriptions != null &&
                AssetManager.Descriptions.TryGetValue(classId, out var classDescs) &&
                classDescs.TryGetValue(talent.name, out var tDesc) &&
                tDesc.ranks != null)
            {
                cost = tDesc.cost;
                range = tDesc.range;
                castTime = tDesc.castTime;
                cooldown = tDesc.cooldown;
                descriptions = tDesc.ranks.ToArray();
            }

            int reqTreePoints = talent.row * 5;
            int currentTreePoints = treeIcons[treeIndex].Sum(x => x.CurrentRank);

            // Dependency (Attached) talent
            string attachedName = talent.attached;
            int attachedReqRank = 0;
            int attachedCurRank = 0;

            if (!string.IsNullOrEmpty(attachedName) && attachedName != "none")
            {
                var reqIcon = treeIcons[treeIndex].FirstOrDefault(
                    x => ((TalentEntry)x.DataContext)?.name == attachedName);
                if (reqIcon != null)
                {
                    attachedReqRank = reqIcon.MaxRank;
                    attachedCurRank = reqIcon.CurrentRank;
                }
            }

            _talentTooltip.RefreshData(
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
                isLocked: isLocked);

            _talentTooltip.Visibility = Visibility.Visible;
            _glyphTooltip.Visibility = Visibility.Collapsed;
            _popup.IsOpen = true;
        }

        public void ShowGlyph(GlyphEntry glyph, string slotType, bool isLocked)
        {
            if (glyph == null)
                _glyphTooltip.RefreshData("", slotType, "", true, isLocked);
            else
                _glyphTooltip.RefreshData(glyph.name, slotType, glyph.description, false, isLocked);

            _talentTooltip.Visibility = Visibility.Collapsed;
            _glyphTooltip.Visibility = Visibility.Visible;
            _popup.IsOpen = true;
        }

        public void Hide() => _popup.IsOpen = false;

        public bool IsOpen => _popup.IsOpen;
    }
}