using System.Windows;
using System.Windows.Controls;

namespace WotLK_TalentCalculator_3._3._5.Controls
{
    public partial class TalentTooltipControl : UserControl
    {
        public TalentTooltipControl()
        {
            InitializeComponent();
        }

        public void RefreshData(
            string talentName,
            int currentRank,
            int maxRank,
            string[] descriptions,
            bool isActive,
            int requiredTreePoints,
            int currentTreePoints,
            string treeName,
            string attachedTalentName,
            int attachedRequiredRank,
            int attachedCurrentRank,
            string cost = null,
            string range = null,
            string castTime = null,
            string cooldown = null,
            bool isLocked = false)
        {
            TxtTitle.Text = talentName;
            TxtRank.Text = $"Rank {currentRank}/{maxRank}";

            // -- Reset Red Warnings --
            TxtRowWarning.Visibility = Visibility.Collapsed;
            TxtReqWarning.Visibility = Visibility.Collapsed;

            // -- Spell Details (Cost, Range, etc.) --
            bool hasMeta = false;

            bool hasCost = !string.IsNullOrWhiteSpace(cost);
            bool hasRange = !string.IsNullOrWhiteSpace(range);
            bool hasCast = !string.IsNullOrWhiteSpace(castTime);
            bool hasCd = !string.IsNullOrWhiteSpace(cooldown);

            // Row 1 Logic (Cost and Range)
            if (hasCost || hasRange)
            {
                hasMeta = true;
                GridLine1.Visibility = Visibility.Visible;

                if (hasCost && hasRange)
                {
                    TxtLine1Left.Text = cost;
                    TxtLine1Right.Text = range;
                    TxtLine1Right.Visibility = Visibility.Visible;
                }
                else if (hasCost)
                {
                    TxtLine1Left.Text = cost;
                    TxtLine1Right.Visibility = Visibility.Collapsed;
                }
                else // If only range exists, align to the left
                {
                    TxtLine1Left.Text = range;
                    TxtLine1Right.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                GridLine1.Visibility = Visibility.Collapsed;
            }

            // Row 2 Logic (Cast Time and Cooldown)
            if (hasCast || hasCd)
            {
                hasMeta = true;
                GridLine2.Visibility = Visibility.Visible;

                if (hasCast && hasCd)
                {
                    TxtLine2Left.Text = castTime;
                    TxtLine2Right.Text = cooldown;
                    TxtLine2Right.Visibility = Visibility.Visible;
                }
                else if (hasCast)
                {
                    TxtLine2Left.Text = castTime;
                    TxtLine2Right.Visibility = Visibility.Collapsed;
                }
                else // If only cooldown exists, align to the left
                {
                    TxtLine2Left.Text = cooldown;
                    TxtLine2Right.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                GridLine2.Visibility = Visibility.Collapsed;
            }

            GridSpellMeta.Visibility = hasMeta ? Visibility.Visible : Visibility.Collapsed;

            // -- Lock Controls (Red Warnings) --
            // 1. Insufficient tree points (Row)?
            if (currentTreePoints < requiredTreePoints)
            {
                TxtRowWarning.Text = $"Requires {requiredTreePoints} points in {treeName} Talents";
                TxtRowWarning.Visibility = Visibility.Visible;
            }

            // 2. Prerequisite (Attached) talent not met?
            if (!string.IsNullOrEmpty(attachedTalentName) && attachedTalentName != "none" && attachedCurrentRank < attachedRequiredRank)
            {
                string pText = attachedRequiredRank > 1 ? "points" : "point";
                TxtReqWarning.Text = $"Requires {attachedRequiredRank} {pText} in {attachedTalentName}";
                TxtReqWarning.Visibility = Visibility.Visible;
            }

            // -- Description Texts --
            if (currentRank == 0)
            {
                // If no points invested, show only Rank 1 description (if available)
                TxtCurrentDesc.Text = descriptions.Length > 0 ? descriptions[0] : "Açıklama bulunamadı.";
                TxtNextRankHeader.Visibility = Visibility.Collapsed;
                TxtNextDesc.Visibility = Visibility.Collapsed;
            }
            else
            {
                // If points invested, show current rank description
                TxtCurrentDesc.Text = descriptions.Length >= currentRank ? descriptions[currentRank - 1] : "";

                // Show next rank if it exists
                if (currentRank < maxRank && descriptions.Length > currentRank)
                {
                    TxtNextRankHeader.Visibility = Visibility.Visible;
                    TxtNextDesc.Visibility = Visibility.Visible;
                    TxtNextDesc.Text = descriptions[currentRank];
                }
                else
                {
                    // Max rank reached
                    TxtNextRankHeader.Visibility = Visibility.Collapsed;
                    TxtNextDesc.Visibility = Visibility.Collapsed;
                }
            }

            // -- Click Warnings (Green/Red) --
            // Only show these when locks are open (IsActive == true)
            if (isActive && !isLocked)
            {
                TxtClickLearn.Visibility = currentRank < maxRank ? Visibility.Visible : Visibility.Collapsed;
                TxtRightClickUnlearn.Visibility = currentRank > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                TxtClickLearn.Visibility = Visibility.Collapsed;
                TxtRightClickUnlearn.Visibility = Visibility.Collapsed;
            }
        }
    }
}