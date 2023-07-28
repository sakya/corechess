using Avalonia.Controls;
using Avalonia.Interactivity;
using ChessLib.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using CoreChess.Abstracts;
using CoreChess.Dialogs;

namespace CoreChess.Pages
{
    public partial class EnginesPage : BasePage
    {
        private readonly List<EngineBase> m_Engines;
        private bool m_IgnoreChanges;

        public EnginesPage()
        {
            this.InitializeComponent();

            NavigateBackWithKeyboard = false;
            PageTitle = Localizer.Localizer.Instance["WT_EnginesWindow"];
            if (!OperatingSystem.IsWindows()) {
                var items = m_EngineType.Items;
                items.RemoveAt(3);
            }

            m_Engines = new List<EngineBase>(App.Settings.Engines);
            m_List.Items = m_Engines.OrderBy(e => e.Name);
        }

        private void OnEngineOptionsClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button)?.DataContext as EngineBase;
            ShowEngineOptions(engine);
        }

        private void OnConfigureEngineClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button)?.DataContext as EngineBase;
            ShowEngineProperties(engine);
        }

        private void ShowEngineProperties(EngineBase engine, bool newEngine = false)
        {
            m_BtnEngineExePath.IsEnabled = !newEngine;
            m_BtnEngineWorkingDir.IsEnabled = !newEngine;

            m_EngineName.Text = engine.Name;
            m_EngineName.IsEnabled = !newEngine;

            m_IgnoreChanges = true;
            if (newEngine)
                m_EngineType.SelectedIndex = 0;
            else if (engine is TheKing)
                m_EngineType.SelectedIndex = 3;
            else if (engine is Uci)
                m_EngineType.SelectedIndex = 1;
            else if (engine is Cecp)
                m_EngineType.SelectedIndex = 2;
            m_EngineType.IsEnabled = newEngine;
            m_IgnoreChanges = false;

            m_EngineExePath.Text = engine.Command;
            m_EngineExePath.IsEnabled = !newEngine;
            m_EngineWorkingDir.Text = engine.WorkingDir;
            m_EngineWorkingDir.IsEnabled = !newEngine;
            m_EngineArguments.Text = engine.Arguments;
            m_EngineArguments.IsEnabled = !newEngine;

            if (engine is Uci) {
                m_EngineRegisterName.Text = ((Uci)engine).RegisterName;
                m_EngineRegisterName.IsEnabled = !newEngine;
                m_EngineRegisterCode.Text = ((Uci)engine).RegisterCode;
            } else {
                m_EngineRegisterName.IsEnabled = false;
                m_EngineRegisterCode.IsEnabled = false;
            }

            m_EngineProperties.DataContext = engine;
            m_List.IsVisible = false;
            m_EngineProperties.IsVisible = true;

            m_PropertiesScrollViewer.ScrollToHome();
        } // ShowEngineProperties

        private void OnEngineTypeChanged(object sender, SelectionChangedEventArgs args)
        {
            if (m_IgnoreChanges)
                return;

            EngineBase engine = null;
            var cmb = sender as ComboBox;
            if (cmb.SelectedIndex == 1)
                engine = new Uci(string.Empty, string.Empty);
            else if (cmb.SelectedIndex == 2)
                engine = new Cecp(string.Empty, string.Empty);
            else if (cmb.SelectedIndex == 3)
                engine = new TheKing(string.Empty, string.Empty);

            if (engine != null) {
                ShowEngineProperties(engine, false);
                m_EngineName.Focus();
            }
        }

        private void ShowEngineOptions(EngineBase engine)
        {
            m_List.IsVisible = false;
            m_EngineOptions.IsVisible = true;

            m_EngineOptionsControl.SetEngine(engine);
            m_OptionsScrollViewer.ScrollToHome();
        } // ShowEngineOptions

        private void OnResetEngineOptionsClick(object sender, RoutedEventArgs e)
        {
            m_EngineOptionsControl.ResetOptions();
        } // OnResetEngineClick

        private async void OnRemoveEngineClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button).DataContext as EngineBase;
            if (await MessageDialog.ShowConfirmMessage(App.MainWindow, Localizer.Localizer.Instance["Confirm"], string.Format(Localizer.Localizer.Instance["RemoveEngine"], engine.Name))) {
                m_Engines.Remove(engine);
                m_List.Items = m_Engines.OrderBy(e => e.Name);
            }
        } // OnRemoveEngineClick

        private void OnAddEngineClick(object sender, RoutedEventArgs e)
        {
            ShowEngineProperties(new Uci(string.Empty, string.Empty), true);
        } // OnAddEngineClick

        private async void OnCommandClick(object sender, RoutedEventArgs e)
        {
            var opts = new FilePickerOpenOptions()
            {
                AllowMultiple = false
            };
            if (OperatingSystem.IsWindows()) {
                opts.FileTypeFilter = new[]
                {
                    new FilePickerFileType("Executables")
                    {
                        Patterns = new []{ "*.exe" }
                    },
                };
            }

            var files = await MainWindow.StorageProvider.OpenFilePickerAsync(opts);
            if (files.Count > 0) {
                m_EngineExePath.Text = files[0].Path.AbsolutePath;
            }
        } // OnCommandClick

        private async void OnWorkingDirClick(object sender, RoutedEventArgs e)
        {
            var folders = await MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            });
            if (folders.Count > 0) {
                m_EngineWorkingDir.Text = folders[0].Path.AbsolutePath;
            }
        } // OnWorkingDirClick

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (m_EngineProperties.IsVisible) {
                EngineBase engine = m_EngineProperties.DataContext as EngineBase;
                engine.Name = m_EngineName.Text;
                engine.Command = m_EngineExePath.Text;
                engine.WorkingDir = m_EngineWorkingDir.Text;
                engine.Arguments = m_EngineArguments.Text;

                if (string.IsNullOrEmpty(engine.Command)) {
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["MissingEngineCommand"], MessageDialog.Icons.Error);
                    return;
                }

                if (!m_Engines.Contains(engine)) {
                    // New engine
                    try {
                        await engine.Start();
                        await engine.Stop();

                        m_Engines.Add(engine);
                        m_List.Items = m_Engines.OrderBy(e => e.Name);
                    } catch (Exception ex) {
                        await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorStartingEngine"], ex.Message), MessageDialog.Icons.Error);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(engine.Name)) {
                    await MessageDialog.ShowMessage(App.MainWindow, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["MissingEngineName"], MessageDialog.Icons.Error);
                    return;
                }

                m_EngineProperties.IsVisible = false;
                m_List.Items = null;
                m_List.Items = m_Engines.OrderBy(e => e.Name);
                m_List.IsVisible = true;
            } else if (m_EngineOptions.IsVisible) {
                m_EngineOptionsControl.ApplyOptions();
                m_EngineOptions.IsVisible = false;
                m_List.IsVisible = true;
            } else {
                App.Settings.Engines = m_Engines;
                App.Settings.Save(App.SettingsPath);
                await NavigateBack();
            }
        } // OnOkClick

        private async void OnCancelClick(object sender, RoutedEventArgs e)
        {
            if (m_EngineProperties.IsVisible) {
                m_EngineProperties.IsVisible = false;
                m_List.IsVisible = true;
            } else if (m_EngineOptions.IsVisible) {
                m_EngineOptions.IsVisible = false;
                m_List.IsVisible = true;
            } else
                await NavigateBack();
        } // OnCancelClick
    }
}