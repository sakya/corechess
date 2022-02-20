using System;
using System.Collections.Generic;
using System.Text;
using Octokit;
using System.Threading.Tasks;

namespace CoreChess.Utils
{

    public class AutoUpdater
    {
        private GitHubClient m_Client = null;
        public AutoUpdater()
        {
            m_Client = new GitHubClient(new ProductHeaderValue("CoreChess"));
        }

        public async Task<bool> CheckForUpdate(Avalonia.Controls.Window owner, bool manual = false)
        {
            var release = await GetLatestRelease();
            if (release != null && release.Assets?.Count > 0) {
                Version rv;
                if (Version.TryParse(release.TagName, out rv)) {
                    var cr = new Version(App.Version);
                    if (rv > cr) {
                        var dlg = new Views.UpdateWindow(release);
                        if (await dlg.ShowDialog<bool>(owner)) {
                            return true;
                        }
                    } else if (manual) {
                        await Views.MessageWindow.ShowMessage(owner, Localizer.Localizer.Instance["Info"], Localizer.Localizer.Instance["NoUpdateAvailable"], Views.MessageWindow.Icons.Info);
                    }
                }
            }
            return false;
        } // CheckForUpdate

        public async Task<Release> GetLatestRelease()
        {
            try {
                var releases = await m_Client.Repository.Release.GetAll("sakya", "CoreChess");
                if (releases?.Count > 0)
                    return releases[0];
            } catch {}
            return null;
        } // GetLatestRelease
    }
}