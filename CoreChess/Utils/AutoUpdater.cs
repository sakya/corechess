using System;
using System.Collections.Generic;
using System.Text;
using Octokit;
using System.Threading.Tasks;
using System.Linq;

namespace CoreChess.Utils
{

    public class AutoUpdater
    {
        private GitHubClient m_Client = null;
        public AutoUpdater()
        {
            m_Client = new GitHubClient(new ProductHeaderValue("CoreChess"));
        }

        /// <summary>
        /// Check for an update (Windows only)
        /// </summary>
        /// <param name="owner">The window owner</param>
        /// <param name="manual">If set to true a message box is displayed if no update is available</param>
        /// <returns>True if an update has been downloaded and started</returns>
        public async Task<bool> CheckForUpdate(Avalonia.Controls.Window owner, bool manual = false)
        {
            IReadOnlyList<Release> releases = null;
            try {
                releases = await m_Client.Repository.Release.GetAll("sakya", "CoreChess");
            } catch (Exception) {
                return false;
            }

            var release = releases?.Count > 0 ? releases[0] : null;
            if (release != null && release.Assets?.Count > 0) {
                Version releaseVersion;
                if (Version.TryParse(release.TagName, out releaseVersion)) {
                    var currentVersion = new Version(App.Version);
                    if (releaseVersion > currentVersion) {
                        // Merge changelogs
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(release.Body);
                        foreach (var r in releases) {
                            if (r != release && Version.TryParse(r.TagName, out releaseVersion)) {
                                if (releaseVersion > currentVersion) {
                                    sb.AppendLine(r.Body);
                                } else {
                                    break;
                                }
                            }
                        }

                        // Wait that other dialogs are closed
                        while (owner.OwnedWindows?.Count > 0)
                            await Task.Delay(100);

                        var dlg = new Views.UpdateWindow(release, sb.ToString());
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
    }
}