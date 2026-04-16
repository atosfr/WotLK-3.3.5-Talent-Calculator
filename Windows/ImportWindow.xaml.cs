using System.Windows;
using System.Windows.Input;
using WotLK_TalentCalculator_3._3._5.Services;

namespace WotLK_TalentCalculator_3._3._5.Windows
{
    public partial class ImportWindow : Window
    {
        public string ResultClassId { get; private set; }
        public string ResultBuildString { get; private set; }
        public List<int> ResultMajorGlyphIndices { get; private set; }
        public List<int> ResultMinorGlyphIndices { get; private set; }
        public bool HasGlyphs { get; private set; }

        private readonly Dictionary<string, int[]> _treeLengths;

        public ImportWindow(Dictionary<string, int[]> treeLengths)
        {
            InitializeComponent();
            _treeLengths = treeLengths;
        }

        private void BtnPasteSummary_Click(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsText())
                TxtImportSummary.Text = Clipboard.GetText().Trim();
        }

        private void BtnPasteLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (Clipboard.ContainsText())
                TxtImportLink.Text = Clipboard.GetText().Trim();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = "";
            string link = TxtImportLink.Text.Trim();
            string summary = TxtImportSummary.Text.Trim();

            // Link first
            if (!string.IsNullOrEmpty(link))
            {
                var decoded = WotlkDbCodec.TryDecodeLink(link, _treeLengths);
                if (decoded.Error == null) { Apply(decoded); return; }
                TxtError.Text = decoded.Error;
            }

            // Summary
            if (!string.IsNullOrEmpty(summary))
            {
                var decoded = WotlkDbCodec.TryDecodeSummary(summary, _treeLengths);
                if (decoded.Error == null) { Apply(decoded); return; }
                TxtError.Text = decoded.Error;
            }

            if (string.IsNullOrEmpty(link) && string.IsNullOrEmpty(summary))
                TxtError.Text = "Paste a link or import string.";
        }

        private void Apply(WotlkDbCodec.DecodedBuild decoded)
        {
            ResultClassId = decoded.ClassId;
            ResultBuildString = decoded.BuildString;
            HasGlyphs = decoded.HasGlyphs;
            ResultMajorGlyphIndices = decoded.MajorGlyphIndices;
            ResultMinorGlyphIndices = decoded.MinorGlyphIndices;
            DialogResult = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}