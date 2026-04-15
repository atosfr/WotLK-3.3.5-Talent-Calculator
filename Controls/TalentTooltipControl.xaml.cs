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
            int requiredTreePoints, // Satır kilidini açmak için o ağaçta gereken toplam puan (örn. 10)
            int currentTreePoints,  // O ağaçta şu an harcanmış toplam puan
            string treeName,        // Ağaç adı (örn. Blood)
            string attachedTalentName, // Varsa önkoşul talent adı
            int attachedRequiredRank,  // Varsa önkoşul talent için gereken rank (genelde MaxRank'idir)
            int attachedCurrentRank,    // Varsa önkoşul talentin şu anki rankı
            string cost = null,
            string range = null,
            string castTime = null,
            string cooldown = null,
            bool isLocked = false)
        {
            TxtTitle.Text = talentName;
            TxtRank.Text = $"Rank {currentRank}/{maxRank}";

            // -- Kırmızı Uyarıları Sıfırla --
            TxtRowWarning.Visibility = Visibility.Collapsed;
            TxtReqWarning.Visibility = Visibility.Collapsed;

            // -- Büyü Detayları (Cost, Range, vb.) --
            bool hasMeta = false;

            bool hasCost = !string.IsNullOrWhiteSpace(cost);
            bool hasRange = !string.IsNullOrWhiteSpace(range);
            bool hasCast = !string.IsNullOrWhiteSpace(castTime);
            bool hasCd = !string.IsNullOrWhiteSpace(cooldown);

            // 1. Satır Mantığı (Cost ve Range)
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
                else // Sadece menzil varsa sola yapıştır
                {
                    TxtLine1Left.Text = range;
                    TxtLine1Right.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                GridLine1.Visibility = Visibility.Collapsed;
            }

            // 2. Satır Mantığı (Cast Time ve Cooldown)
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
                else // Sadece cooldown varsa sola yapıştır
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

            // -- Kilit Kontrolleri (Kırmızı Yazılar) --
            // 1. Ağaç puanı (Satır) yetmiyor mu?
            if (currentTreePoints < requiredTreePoints)
            {
                TxtRowWarning.Text = $"Requires {requiredTreePoints} points in {treeName} Talents";
                TxtRowWarning.Visibility = Visibility.Visible;
            }

            // 2. Önkoşul (Attached) yetenek sağlanmamış mı?
            if (!string.IsNullOrEmpty(attachedTalentName) && attachedTalentName != "none" && attachedCurrentRank < attachedRequiredRank)
            {
                string pText = attachedRequiredRank > 1 ? "points" : "point";
                TxtReqWarning.Text = $"Requires {attachedRequiredRank} {pText} in {attachedTalentName}";
                TxtReqWarning.Visibility = Visibility.Visible;
            }

            // -- Açıklama Metinleri --
            if (currentRank == 0)
            {
                // Hiç puan verilmediyse sadece Rank 1 açıklaması (Varsa)
                TxtCurrentDesc.Text = descriptions.Length > 0 ? descriptions[0] : "Açıklama bulunamadı.";
                TxtNextRankHeader.Visibility = Visibility.Collapsed;
                TxtNextDesc.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Puan verildiyse mevcut rank açıklaması
                TxtCurrentDesc.Text = descriptions.Length >= currentRank ? descriptions[currentRank - 1] : "";

                // Sonraki rank varsa göster
                if (currentRank < maxRank && descriptions.Length > currentRank)
                {
                    TxtNextRankHeader.Visibility = Visibility.Visible;
                    TxtNextDesc.Visibility = Visibility.Visible;
                    TxtNextDesc.Text = descriptions[currentRank];
                }
                else
                {
                    // Max rank'a ulaşıldı
                    TxtNextRankHeader.Visibility = Visibility.Collapsed;
                    TxtNextDesc.Visibility = Visibility.Collapsed;
                }
            }

            // -- Tıklama Uyarıları (Yeşil/Kırmızı) --
            // Sadece kilitler açıkken (IsActive == true) bunları göster
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