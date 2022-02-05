using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.Interactivity;
using System.Collections;
namespace CoreChess.Controls
{
    public class CheckableMenuItem : MenuItem, IStyleable
    {
        public CheckableMenuItem()
        {
            InitializeComponent();
        } // CheckableMenuItem

        public static readonly DirectProperty<CheckableMenuItem, bool> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<CheckableMenuItem, bool>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v);

        Type IStyleable.StyleKey => typeof(MenuItem);
        private bool m_IsChecked = false;
        private string m_Group = string.Empty;

        public event EventHandler<RoutedEventArgs> IsCheckedChanged;

        public string Group { 
            get { return m_Group; }
            set {
                if (m_Group != value) {
                    m_Group = value;
                    SetIcon();
                }
            }
        }

        public bool IsChecked 
        {
            get { return m_IsChecked; }
            set {
                if (SetAndRaise(IsCheckedProperty, ref m_IsChecked, value)) {
                    SetIcon();
                    OnIsCheckedChanged();
                }
            }
        }

        private bool HasGroup 
        {
            get { return !string.IsNullOrEmpty(Group); }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            if (string.IsNullOrEmpty(Group)) {
                this.Icon = new Projektanker.Icons.Avalonia.Icon()
                {
                    Value = "far fa-square"
                };
            } else {
                this.Icon = new Projektanker.Icons.Avalonia.Icon();
            }
        }                        

        private void OnClicked(object sender, RoutedEventArgs args)
        {
            if (!HasGroup) {
                this.IsChecked = !this.IsChecked;                
            } else if (!this.IsChecked)
                this.IsChecked = true;
        } // OnClicked

        private void OnIsCheckedChanged()
        {
            if (HasGroup) {
                if (!this.IsChecked)
                    return;

                var pi = this.Parent as MenuItem;
                if (pi != null) {
                    foreach (var i in pi.Items) {
                        var mi = i as CheckableMenuItem;
                        if (mi != null && mi.Group == this.Group) {
                            mi.IsChecked = mi == this;                                
                        }
                    }
                }
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            } else {
                IsCheckedChanged?.Invoke(this, new RoutedEventArgs());
            }
        } // OnIsCheckedChanged

        private void SetIcon()
        {
            string icon = string.Empty;
            if (m_IsChecked)
                icon = HasGroup ? "far fa-dot-circle" : "fas fa-check-square";
            else
                icon = HasGroup ? "far fa-circle" : "far fa-square";

            if (this.Icon == null || (this.Icon as Projektanker.Icons.Avalonia.Icon).Value != icon) {
                this.Icon = new Projektanker.Icons.Avalonia.Icon()
                {
                    Value = icon
                };
            }
        } // SetIcon
    }    
}