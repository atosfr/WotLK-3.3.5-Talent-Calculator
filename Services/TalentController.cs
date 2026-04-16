using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WotLK_TalentCalculator_3._3._5.Controls;
using WotLK_TalentCalculator_3._3._5.Models;

namespace WotLK_TalentCalculator_3._3._5.Services
{
    /// <summary>
    /// Manages the state of talent trees: adding/removing ranks, validation,
    /// row lock control, arrow color updates, and resets.
    /// Initialize() is called with new tree data when the class changes.
    /// </summary>
    public class TalentController
    {
        public const int MaxPoints = 71;

        private List<IconControl>[] _treeIcons = new List<IconControl>[3];
        private Dictionary<string, List<Rectangle>>[] _arrowMaps = new Dictionary<string, List<Rectangle>>[3];
        private TextBlock[] _pointsLabels = new TextBlock[3];

        // ── Point cache ──────────────────────────────────────────────────────
        // Updated incrementally on every rank change to avoid redundant Sum() calls.
        private readonly int[] _treePoints = new int[3];
        private int _totalPoints;

        private bool _isLocked;

        public event System.Action Changed;

        public void Initialize(
            List<IconControl>[] treeIcons,
            Dictionary<string, List<Rectangle>>[] arrowMaps,
            TextBlock[] pointsLabels)
        {
            _treeIcons = treeIcons;
            _arrowMaps = arrowMaps;
            _pointsLabels = pointsLabels;

            for (int t = 0; t < 3; t++)
            {
                if (_treeIcons[t] == null) continue;
                int capturedTree = t;
                foreach (var icon in _treeIcons[t])
                    icon.RankChangeRequested += (s, inc) => HandleRankChange((IconControl)s, capturedTree, inc);
            }

            RecalculateCache();
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked == value) return;
                _isLocked = value;
                for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            }
        }

        public List<IconControl>[] TreeIcons => _treeIcons;

        public int TotalPoints() => _totalPoints;
        public int TreePoints(int treeIndex) => _treePoints[treeIndex];

        public void ApplyBuildString(string buildString)
        {
            var allIcons = new List<IconControl>();
            for (int t = 0; t < 3; t++)
                if (_treeIcons[t] != null) allIcons.AddRange(_treeIcons[t]);

            int len = System.Math.Min(buildString.Length, allIcons.Count);
            for (int i = 0; i < len; i++)
                if (int.TryParse(buildString[i].ToString(), out int rank))
                    allIcons[i].CurrentRank = System.Math.Min(rank, allIcons[i].MaxRank);

            RecalculateCache();
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            RefreshAllLabels();
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            if (_isLocked) return;

            bool any = false;
            for (int t = 0; t < 3; t++)
            {
                if (_treeIcons[t] == null) continue;
                bool changed = false;
                foreach (var icon in _treeIcons[t])
                    if (icon.CurrentRank > 0) { icon.CurrentRank = 0; changed = any = true; }
                if (changed && _pointsLabels[t] != null) _pointsLabels[t].Text = " (0)";
            }

            if (!any) return;

            RecalculateCache();
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            Changed?.Invoke();
        }

        public void ResetTree(int treeIndex)
        {
            if (_isLocked || _treeIcons[treeIndex] == null) return;

            bool changed = false;
            foreach (var icon in _treeIcons[treeIndex])
                if (icon.CurrentRank > 0) { icon.CurrentRank = 0; changed = true; }

            if (!changed) return;

            RecalculateCache();
            if (_pointsLabels[treeIndex] != null) _pointsLabels[treeIndex].Text = " (0)";

            // maxReached state might have changed — update all trees
            for (int t = 0; t < 3; t++) UpdateTreeStates(t);
            Changed?.Invoke();
        }

        // ────────────────────────────────────────────────────────────────────
        // Internal Logic
        // ────────────────────────────────────────────────────────────────────

        private void HandleRankChange(IconControl icon, int treeIndex, bool increase)
        {
            if (_isLocked) return;

            bool maxReachedBefore = _totalPoints >= MaxPoints;

            if (increase)
            {
                if (!icon.IsActive || icon.CurrentRank >= icon.MaxRank) return;
                if (_totalPoints >= MaxPoints) return;
                icon.CurrentRank++;
                _treePoints[treeIndex]++;
                _totalPoints++;
            }
            else
            {
                if (icon.CurrentRank == 0 || !CanRemove(icon, treeIndex)) return;
                icon.CurrentRank--;
                _treePoints[treeIndex]--;
                _totalPoints--;
            }

            bool maxReachedAfter = _totalPoints >= MaxPoints;
            bool boundaryChanged = maxReachedBefore != maxReachedAfter;

            CommitChange(treeIndex, boundaryChanged);
        }

        private bool CanRemove(IconControl clicked, int treeIndex)
        {
            var tree = _treeIcons[treeIndex];

            if (tree.Any(x => x.PrerequisiteTalent == clicked && x.CurrentRank > 0))
                return false;

            // When this talent is removed, are the unlock requirements for subsequent rows still met?
            // Checked using row-based cumulative totals.
            int cumulative = 0;
            int currentRow = -1;

            // Note: Since _treeIcons doesn't store talents in natural order, sum based on rows for each icon.
            // Given the small lists (~70 items), O(n²) complexity is tolerable; though caching could simplify this.
            foreach (var icon in tree)
            {
                int simRank = icon == clicked ? icon.CurrentRank - 1 : icon.CurrentRank;
                if (simRank <= 0 || icon.Row == 0) continue;

                int before = 0;
                foreach (var other in tree)
                    if (other.Row < icon.Row)
                        before += (other == clicked ? other.CurrentRank - 1 : other.CurrentRank);

                if (before < icon.Row * 5) return false;
            }
            return true;
        }

        private void CommitChange(int changedTree, bool boundaryChanged)
        {
            // Always update the affected tree
            UpdateTreeStates(changedTree);

            // If the 71-point limit (maxReached) is crossed or dropped below, update the other trees as well.
            if (boundaryChanged)
                for (int t = 0; t < 3; t++)
                    if (t != changedTree) UpdateTreeStates(t);

            if (_pointsLabels[changedTree] != null)
                _pointsLabels[changedTree].Text = $" ({_treePoints[changedTree]})";

            Changed?.Invoke();
        }

        private void UpdateTreeStates(int treeIndex)
        {
            var tree = _treeIcons[treeIndex];
            if (tree == null) return;

            int treeTotal = _treePoints[treeIndex];
            bool maxReached = _totalPoints >= MaxPoints;
            var arrowMap = _arrowMaps[treeIndex];

            foreach (var icon in tree)
            {
                bool rowMet = treeTotal >= icon.Row * 5;
                bool prereqMet = icon.PrerequisiteTalent == null ||
                                 icon.PrerequisiteTalent.CurrentRank == icon.PrerequisiteTalent.MaxRank;
                bool newActive = !((_isLocked || maxReached) && icon.CurrentRank == 0) && rowMet && prereqMet;

                // If IsActive hasn't changed, do nothing — skip arrow updates.
                if (icon.IsActive == newActive) continue;

                icon.IsActive = newActive;

                // Update arrows to the new color (only for changed icons)
                if (arrowMap == null) continue;
                var name = ((TalentEntry)icon.DataContext)?.name;
                if (name == null || !arrowMap.TryGetValue(name, out var arrows)) continue;
                foreach (var arrow in arrows)
                    SetArrowColor(arrow, newActive);
            }
        }

        private void RefreshAllLabels()
        {
            for (int t = 0; t < 3; t++)
                if (_pointsLabels[t] != null && _treeIcons[t] != null)
                    _pointsLabels[t].Text = $" ({_treePoints[t]})";
        }

        private void RecalculateCache()
        {
            _totalPoints = 0;
            for (int t = 0; t < 3; t++)
            {
                _treePoints[t] = _treeIcons[t]?.Sum(x => x.CurrentRank) ?? 0;
                _totalPoints += _treePoints[t];
            }
        }

        // Replacing the previous nested if-chain.
        private static void SetArrowColor(Rectangle arrow, bool active)
        {
            if (arrow.Fill is not ImageBrush brush) return;
            if (arrow.Tag is not ArrowPair pair) return;
            brush.ImageSource = active ? pair.Active : pair.Inactive;
        }
    }
}