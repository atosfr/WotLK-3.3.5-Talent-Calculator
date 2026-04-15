using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace WotLK_TalentCalculator_3._3._5
{
    public partial class ExportWindow : Window
    {
        public ExportWindow(string summaryUrl, string shareLink)
        {
            InitializeComponent();
            TxtExportUrl.Text = summaryUrl;
            TxtShareLink.Text = shareLink;
        }

        private async void BtnCopy_Click(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(TxtExportUrl.Text);
            TxtCopy.Text = "Copied!";
            await Task.Delay(1500);
            TxtCopy.Text = "Copy";
        }

        private async void BtnCopyLink_Click(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(TxtShareLink.Text);
            TxtCopyLink.Text = "Copied!";
            await Task.Delay(1500);
            TxtCopyLink.Text = "Copy";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}