using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WotLK_TalentCalculator_3._3._5.ViewModels
{
    /// <summary>
    /// InfoBar'ın bağlanabilir (bindable) alanlarını tutar.
    /// XAML'daki RequiredLevel ve PointsLeft, kod tarafında
    /// <see cref="Update"/> çağrısıyla otomatik olarak yenilenir.
    ///
    /// InfoClassName'deki renkli Run'lar mixed-color gerektirdiğinden
    /// kod tarafında (MainWindow.UpdateInfoBar) güncellenir; bu ViewModel
    /// kapsamına girmez.
    /// </summary>
    public class TalentInfoViewModel : INotifyPropertyChanged
    {
        private int _pointsLeft = 71;
        private string _requiredLevel = "-";

        /// <summary>"Points left: X" metnindeki sayı.</summary>
        public int PointsLeft
        {
            get => _pointsLeft;
            set { if (_pointsLeft == value) return; _pointsLeft = value; Notify(); }
        }

        /// <summary>Gerekli karakter seviyesi (toplam puan + 9).</summary>
        public string RequiredLevel
        {
            get => _requiredLevel;
            set { if (_requiredLevel == value) return; _requiredLevel = value; Notify(); }
        }

        /// <summary>
        /// Üç ağacın güncel puanlarından tüm özellikleri tek seferde günceller.
        /// </summary>
        public void Update(int t1, int t2, int t3)
        {
            int total = t1 + t2 + t3;
            PointsLeft = 71 - total;
            RequiredLevel = total == 0 ? "-" : (total + 9).ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}