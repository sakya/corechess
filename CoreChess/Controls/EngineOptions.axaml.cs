using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ChessLib.Engines;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Platform.Storage;
using CoreChess.Views;

namespace CoreChess.Controls
{
    public partial class EngineOptions : UserControl
    {
        private EngineBase m_Engine = null;

        public EngineOptions()
        {
            this.InitializeComponent();
        }

        public EngineBase Engine
        {
            get { return m_Engine; }
        }

        public void SetEngine(EngineBase engine)
        {
            m_Engine = engine;

            var grid = this.FindControl<Grid>("m_Container");
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            HashSet<string> hiddenOptions = new HashSet<string>()
            {
                Uci.Chess960OptionName,
                Uci.AnalyzeModeOptionName
            };

            // Render options
            foreach (var o in engine.Options.OrderBy(opt => opt.Name)) {
                if (!hiddenOptions.Contains(o.Name)) {
                    if (o.Type == "check") {
                        AddCheck(grid, o);
                    } else if (o.Type == "spin") {
                        AddSpin(grid, o);
                    } else if (o.Type == "combo") {
                        AddCombo(grid, o);
                    } else if (o.Type == "button") {
                    } else if (o.Type == "string") {
                        AddString(grid, o);
                    } else if (o.Type == "path") {
                        AddPath(grid, o);
                    }
                }
            }

            var sv = this.Parent as ScrollViewer;
            if (sv != null)
                sv.Offset = new Vector(0 , 0);
        } // SetEngine

        /// <summary>
        /// Reset options to default values
        /// </summary>
        public void ResetOptions()
        {
            if (m_Engine != null) {
                var grid = this.FindControl<Grid>("m_Container");
                foreach (var c in grid.Children) {
                    var subGrid = c as Grid;
                    if (subGrid != null) {
                        foreach (var sc in subGrid.Children) {
                            var ctrl = sc as Control;
                            if (ctrl.Tag != null) {
                                string name = ctrl.Tag as string;
                                if (!string.IsNullOrEmpty(name)) {
                                    var opt = m_Engine.GetOption(name);
                                    if (opt != null)
                                        SetControlValue(ctrl, opt.Default);
                                }
                            }
                        }
                    }
                }
            }
        } // ResetOptions

        /// <summary>
        /// Apply options values to Engine.Options
        /// </summary>
        public void ApplyOptions()
        {
            if (m_Engine != null) {
                var grid = this.FindControl<Grid>("m_Container");
                foreach (var c in grid.Children) {
                    var subGrid = c as Grid;
                    if (subGrid != null) {
                        foreach (var sc in subGrid.Children) {
                            var ctrl = sc as Control;
                            if (ctrl.Tag != null) {
                                string value = string.Empty;
                                if (ctrl is ToggleSwitch)
                                    value = ((ToggleSwitch)ctrl).IsChecked.Value ? "true" : "false";
                                else if (ctrl is NumericUpDown)
                                    value = ((double)((NumericUpDown)ctrl).Value).ToString(CultureInfo.InvariantCulture);
                                else if (ctrl is ComboBox)
                                    value = ((ComboBox)ctrl).SelectedItem?.ToString();
                                else if (ctrl is TextBox)
                                    value = ((TextBox)ctrl).Text;

                                string name = ctrl.Tag as string;
                                if (!string.IsNullOrEmpty(name)) {
                                    var opt = m_Engine.GetOption(name);
                                    if (opt != null)
                                        opt.Value = value;
                                }
                            }
                        }
                    }
                }
            }
        } // ApplyOptions

        public void SetIsEnabled(bool enabled)
        {
            var grid = this.FindControl<Grid>("m_Container");
            foreach (var c in grid.Children) {
                var subGrid = c as Grid;
                if (subGrid != null) {
                    foreach (var sc in subGrid.Children) {
                        var ctrl = sc as Control;
                        ctrl.IsEnabled = enabled;
                    }
                }
            }
        } // SetIsEnabled

        #region private operations
        private void SetControlValue(Control ctrl, string value)
        {
            if (ctrl is ToggleSwitch)
                ((ToggleSwitch)ctrl).IsChecked = value == "true";
            else if (ctrl is NumericUpDown)
                ((NumericUpDown)ctrl).Value = int.Parse(value);
            else if (ctrl is ComboBox)
                ((ComboBox)ctrl).SelectedItem = value;
            else if (ctrl is TextBox)
                ((TextBox)ctrl).Text = value;
        } // SetControlValue

        private void AddCheck(Grid container, EngineBase.Option opt, bool isEnabled = true)
        {
            container.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock()
            {
                Text = opt.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(lbl);

            var ctrl = new ToggleSwitch()
            {
                Tag = opt.Name,
                IsChecked = opt.Value == "true",
                IsEnabled = isEnabled,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            grid.Children.Add(ctrl);

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(ctrl, 1);

            Grid.SetRow(grid, container.RowDefinitions.Count - 1);
            container.Children.Add(grid);
        } // AddCheck

        private void AddSpin(Grid container, EngineBase.Option opt)
        {
            container.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock()
            {
                Text = opt.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(lbl);

            var ctrl = new NumericUpDown()
            {
                Tag = opt.Name,
                Minimum = int.Parse(opt.Min),
                Maximum = int.Parse(opt.Max),
                Value = int.Parse(opt.Value),
            };
            grid.Children.Add(ctrl);

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(ctrl, 1);

            Grid.SetRow(grid, container.RowDefinitions.Count - 1);
            container.Children.Add(grid);
        } // AddSpin

        private void AddCombo(Grid container, EngineBase.Option opt)
        {
            container.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock()
            {
                Text = opt.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(lbl);

            var ctrl = new ComboBox()
            {
                Tag = opt.Name,
                ItemsSource = opt.ValidValues,
            };
            ctrl.SelectedItem = opt.ValidValues.FirstOrDefault(v => v == opt.Value);

            grid.Children.Add(ctrl);

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(ctrl, 1);

            Grid.SetRow(grid, container.RowDefinitions.Count - 1);
            container.Children.Add(grid);
        } // AddCombo

        private void AddString(Grid container, EngineBase.Option opt)
        {
            container.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock()
            {
                Text = opt.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(lbl);

            var ctrl = new TextBox()
            {
                Tag = opt.Name,
                Text = opt.Value
            };
            grid.Children.Add(ctrl);

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(ctrl, 1);

            Grid.SetRow(grid, container.RowDefinitions.Count - 1);
            container.Children.Add(grid);
        } // AddString

        private void AddPath(Grid container, EngineBase.Option opt)
        {
            container.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            var lbl = new TextBlock()
            {
                Text = opt.Name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(lbl);

            var ctrl = new TextBox()
            {
                Tag = opt.Name,
                Text = opt.Value,
                Margin = new Thickness(0,5,0,5),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(ctrl);

            var btn = new Button()
            {
                Content = new Projektanker.Icons.Avalonia.Icon() { Value = "fas fa-folder-open" },
                Padding = new Thickness(12),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            grid.Children.Add(btn);
            btn.Click += async (s, args) =>
            {
                var folders = await ((Window)this.VisualRoot).StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions()
                    {
                        AllowMultiple = false
                    });
                if (folders.Count > 0)
                    ctrl.Text = folders[0].Path.AbsolutePath;
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(ctrl, 1);
            Grid.SetColumn(btn, 2);

            Grid.SetRow(grid, container.RowDefinitions.Count - 1);
            container.Children.Add(grid);
        } // AddPath
        #endregion
    }
}
