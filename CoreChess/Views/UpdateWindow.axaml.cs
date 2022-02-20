using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octokit;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace CoreChess.Views
{
    public class UpdateWindow : BaseView
    {
        private Release m_Release = null;
        private CancellationTokenSource m_Cts;

        public UpdateWindow()
        {
            this.InitializeComponent();
        }

        public UpdateWindow(Release release)
        {
            m_Release = release;
            this.InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();

            var tb = this.FindControl<TextBlock>("m_Version");
            tb.Text = m_Release.TagName;
            tb = this.FindControl<TextBlock>("m_Description");
            tb.Text = m_Release.Body;

            this.Closing += (s, e) =>
            {
                if (m_Cts != null)
                    m_Cts.Cancel();
            };
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
            var asset = m_Release.Assets[0];
            var progress = this.FindControl<ProgressBar>("m_Progress");
            var progressMessage = this.FindControl<TextBlock>("m_ProgressMessage");
            progressMessage.Text = $"{0.ToString("0", App.Settings.Culture)}%";

            var fileName = Path.Combine(GetDownloadFolder(), asset.Name);
            if (File.Exists(fileName))
                File.Delete(fileName);

            this.FindControl<StackPanel>("m_VersionInfo").IsVisible = false;
            this.FindControl<Controls.OkCancelButtons>("m_OkCancel").IsVisible = false;
            this.FindControl<StackPanel>("m_Download").IsVisible = true;

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
                            progress.Value = perc;
                            progressMessage.Text = $"{perc.ToString("0", App.Settings.Culture)}%";
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
        } // DownloadAndInstall

        private string GetDownloadFolder()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
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