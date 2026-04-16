using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WotLK_TalentCalculator_3._3._5.Services;

namespace WotLK_TalentCalculator_3._3._5.ViewModels
{
    /// <summary>
    /// Holds bindable fields for the InfoBar.
    /// The class name is rendered using the class color, while the (t1/t2/t3) distribution 
    /// is rendered in white on the XAML side.
    /// </summary>
    public class TalentInfoViewModel : INotifyPropertyChanged
    {
        private int _pointsLeft = 71;
        private string _requiredLevel = "-";
        private string _className = "";
        private string _distributionText = "";
        private Brush _classBrush = Brushes.White;

        /// <summary>The numeric value in the "Points left: X" text.</summary>
        public int PointsLeft
        {
            get => _pointsLeft;
            private set { if (_pointsLeft == value) return; _pointsLeft = value; Notify(); }
        }

        /// <summary>Required character level (total points + 9).</summary>
        public string RequiredLevel
        {
            get => _requiredLevel;
            private set { if (_requiredLevel == value) return; _requiredLevel = value; Notify(); }
        }

        /// <summary>Class name (e.g., "Death Knight"). Used with <see cref="ClassBrush"/> for colored rendering.</summary>
        public string ClassName
        {
            get => _className;
            private set { if (_className == value) return; _className = value; Notify(); }
        }

        /// <summary>Distribution label such as "(14/0/57)".</summary>
        public string DistributionText
        {
            get => _distributionText;
            private set { if (_distributionText == value) return; _distributionText = value; Notify(); }
        }

        /// <summary>The color used to render ClassName (class-specific).</summary>
        public Brush ClassBrush
        {
            get => _classBrush;
            private set { if (_classBrush == value) return; _classBrush = value; Notify(); }
        }

        /// <summary>
        /// Updates all fields at once based on the points from the three trees.
        /// If classId is null or empty, name and brush are cleared.
        /// </summary>
        public void Update(string classId, string className, int t1, int t2, int t3)
        {
            int total = t1 + t2 + t3;
            PointsLeft = 71 - total;
            RequiredLevel = total == 0 ? "-" : (total + 9).ToString();
            ClassName = className ?? "";
            DistributionText = $"({t1}/{t2}/{t3})";
            ClassBrush = string.IsNullOrEmpty(classId) ? Brushes.White : ClassPalette.GetBrush(classId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}