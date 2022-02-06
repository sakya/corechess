using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib.Engines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreChess.Views
{
    public class EnginesWindow : BaseView
    {
        Controls.ItemsList m_List = null;
        Grid m_EngineProperties = null;
        Grid m_EngineOptions = null;
        List<EngineBase> m_Engines = null;
        bool m_IgnoreChanges = false;

        public EnginesWindow()
        {
            this.InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();

            if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
                var cb = this.FindControl<ComboBox>("m_EngineType");
                var items = cb.Items as Avalonia.Collections.AvaloniaList<object>;
                items.RemoveAt(3);
            }

            m_Engines = new List<EngineBase>(App.Settings.Engines);
            m_List = this.FindControl<Controls.ItemsList>("m_List");
            m_List.Items = m_Engines.OrderBy(e => e.Name);

            m_EngineProperties = this.FindControl<Grid>("m_EngineProperties");
            m_EngineOptions = this.FindControl<Grid>("m_EngineOptions");
        }

        private void OnEngineOptionsClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button).DataContext as EngineBase;
            ShowEngineOptions(engine);
        }

        private void OnConfigureEngineClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button).DataContext as EngineBase;
            ShowEngineProperties(engine);
        }

        private void ShowEngineProperties(EngineBase engine, bool newEngine = false)
        {
            var btn = this.FindControl<Button>("m_BtnEngineExePath");
            btn.IsEnabled = !newEngine;
            btn = this.FindControl<Button>("m_BtnEngineWorkingDir");
            btn.IsEnabled = !newEngine;

            var txt = this.FindControl<TextBox>("m_EngineName");
            txt.Text = engine.Name;
            txt.IsEnabled = !newEngine;

            m_IgnoreChanges = true;
            var cmb = this.FindControl<ComboBox>("m_EngineType");
            if (newEngine)
                cmb.SelectedIndex = 0;
            else if (engine is TheKing)
                cmb.SelectedIndex = 3;
            else if (engine is Uci)
                cmb.SelectedIndex = 1;
            else if (engine is Cecp)
                cmb.SelectedIndex = 2;
            cmb.IsEnabled = newEngine;
            m_IgnoreChanges = false;

            txt = this.FindControl<TextBox>("m_EngineExePath");
            txt.Text = engine.Command;
            txt.IsEnabled = !newEngine;
            txt = this.FindControl<TextBox>("m_EngineWorkingDir");
            txt.Text = engine.WorkingDir;
            txt.IsEnabled = !newEngine;
            txt = this.FindControl<TextBox>("m_EngineArguments");
            txt.Text = engine.Arguments;
            txt.IsEnabled = !newEngine;

            if (engine is Uci) {
                txt = this.FindControl<TextBox>("m_EngineRegisterName");
                txt.Text = ((Uci)engine).RegisterName;
                txt.IsEnabled = !newEngine;
                txt = this.FindControl<TextBox>("m_EngineRegisterCode");
                txt.Text = ((Uci)engine).RegisterCode;
            } else {
                this.FindControl<TextBox>("m_EngineRegisterName").IsEnabled = false;
                this.FindControl<TextBox>("m_EngineRegisterCode").IsEnabled = false;
            }

            m_EngineProperties.DataContext = engine;
            m_List.IsVisible = false;
            m_EngineProperties.IsVisible = true;

            this.FindControl<ScrollViewer>("m_PropertiesScrollViewer").ScrollToHome();
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
                this.FindControl<TextBox>("m_EngineName").Focus();
            }
        }

        private void ShowEngineOptions(EngineBase engine)
        {
            m_List.IsVisible = false;
            m_EngineOptions.IsVisible = true;

            this.FindControl<Controls.EngineOptions>("m_EngineOptionsControl").SetEngine(engine);
            this.FindControl<ScrollViewer>("m_OptionsScrollViewer").ScrollToHome();
        } // ShowEngineOptions

        private void OnResetEngineOptionsClick(object sender, RoutedEventArgs e)
        {
            var opt = this.FindControl<Controls.EngineOptions>("m_EngineOptionsControl");
            opt.ResetOptions();
        } // OnResetEngineClick

        private async void OnRemoveEngineClick(object sender, RoutedEventArgs e)
        {
            EngineBase engine = (sender as Button).DataContext as EngineBase;
            if (await MessageWindow.ShowConfirmMessage(this, Localizer.Localizer.Instance["Confirm"], string.Format(Localizer.Localizer.Instance["RemoveEngine"], engine.Name))) {
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
            var dlg = new OpenFileDialog();
            dlg.AllowMultiple = false;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                dlg.Filters = new List<FileDialogFilter>()
                {
                    new FileDialogFilter(){ Extensions = new List<string>() {"exe" }, Name = "Executables"},
                };
            }
            string[] files = await dlg.ShowAsync(this);
            if (files?.Length > 0) {
                var txt = this.FindControl<TextBox>("m_EngineExePath");
                txt.Text = files[0];
            }
        } // OnCommandClick

        private async void OnWorkingDirClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog();
            string path = await dlg.ShowAsync(this);
            if (!string.IsNullOrEmpty(path)) {
                var txt = this.FindControl<TextBox>("m_EngineWorkingDir");
                txt.Text = path;
            }
        } // OnWorkingDirClick

        private async void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (m_EngineProperties.IsVisible) {
                EngineBase engine = m_EngineProperties.DataContext as EngineBase;
                var txt = this.FindControl<TextBox>("m_EngineName");
                engine.Name = txt.Text;
                txt = this.FindControl<TextBox>("m_EngineExePath");
                engine.Command = txt.Text;

                txt = this.FindControl<TextBox>("m_EngineWorkingDir");
                engine.WorkingDir = txt.Text;

                txt = this.FindControl<TextBox>("m_EngineArguments");
                engine.Arguments = txt.Text;

                if (string.IsNullOrEmpty(engine.Command)) {
                    await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["MissingEngineCommand"], MessageWindow.Icons.Error);
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
                        await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], string.Format(Localizer.Localizer.Instance["ErrorStartingEngine"], ex.Message), MessageWindow.Icons.Error);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(engine.Name)) {
                    await MessageWindow.ShowMessage(this, Localizer.Localizer.Instance["Error"], Localizer.Localizer.Instance["MissingEngineName"], MessageWindow.Icons.Error);
                    return;
                }

                m_EngineProperties.IsVisible = false;
                m_List.Items = null;
                m_List.Items = m_Engines.OrderBy(e => e.Name);
                m_List.IsVisible = true;
            } else if (m_EngineOptions.IsVisible) {
                this.FindControl<Controls.EngineOptions>("m_EngineOptionsControl").ApplyOptions();
                m_EngineOptions.IsVisible = false;
                m_List.IsVisible = true;
            } else {
                App.Settings.Engines = m_Engines;
                App.Settings.Save(App.SettingsPath);
                this.Close();
            }
        } // OnOkClick

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            if (m_EngineProperties.IsVisible) {
                m_EngineProperties.IsVisible = false;
                m_List.IsVisible = true;
            } else if (m_EngineOptions.IsVisible) {
                m_EngineOptions.IsVisible = false;
                m_List.IsVisible = true;
            } else
                this.Close();
        } // OnCancelClick
    }
}