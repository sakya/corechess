using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octokit;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CoreChess.Abstracts;

namespace CoreChess.Dialogs
{
    public partial class UpdateDialog : BaseDialog
    {
        private Release m_Release = null;
        private string m_Changelog = string.Empty;
        private CancellationTokenSource m_Cts;

        public UpdateDialog()
        {
            this.InitializeComponent();
        }

        public UpdateDialog(Release release, string changelog)
        {
            m_Release = release;
            m_Changelog = changelog;

            this.InitializeComponent();

            m_Version.Text = m_Release.TagName;
            m_Description.Text = m_Changelog;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            m_Cts = new CancellationTokenSource();
            DownloadAndInstall(m_Cts.Token);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        private async void DownloadAndInstall(CancellationToken token)
        {
            try {
                await DownloadAndInstallPrimitive(token);
            } catch (Exception ex) {
                m_Message.Text = $"{Localizer.Localizer.Instance["UpdateError"]} {ex.Message}";
            }
        } // DownloadAndInstall

        private async Task<bool> DownloadAndInstallPrimitive(CancellationToken token)
        {
            m_ProgressMessage.Text = $"{0.ToString("0", App.Settings.Culture)}%";

            m_VersionInfo.IsVisible = false;
            m_OkCancel.IsVisible = false;
            m_Download.IsVisible = true;

            var asset = m_Release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
            if (asset == null)
                throw new Exception("Asset not found");

            var fileName = Path.Combine(GetDownloadFolder(), asset.Name);
            if (File.Exists(fileName))
                File.Delete(fileName);

            double done = 0;
            using (var client = new HttpClient()) {
                using (Stream stream = await client.GetStreamAsync(asset.BrowserDownloadUrl)) {
                    using (Stream fs = new FileStream(fileName, System.IO.FileMode.CreateNew, FileAccess.Write)) {
                        while (done < asset.Size) {
                            if (token.IsCancellationRequested)
                                break;
                            var buffer = new byte[4096];
                            var chunk = await stream.ReadAsync(buffer, 0, buffer.Length);
                            await fs.WriteAsync(buffer, 0, chunk);

                            done += chunk;
                            var perc = done / (double)asset.Size * 100.0;
                            m_Progress.Value = perc;
                            m_ProgressMessage.Text = $"{perc.ToString("0", App.Settings.Culture)}%";
                        }
                    }
                }
            }

            if (!token.IsCancellationRequested) {
                System.Diagnostics.Process.Start(fileName);
                this.Close(true);
            } else {
                File.Delete(fileName);
            }
            return true;
        }

        private string GetDownloadFolder()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var folders = new List<string>()
            {
                Path.Combine(home, "Downloads"),
                Path.Combine(home, "downloads"),
                Path.Combine(home, "Download"),
                Path.Combine(home, "download"),
            };

            foreach (var f in folders) {
                if (Directory.Exists(f))
                    return f;
            }
            return home;
        } // GetDownloadFolder
    }
}