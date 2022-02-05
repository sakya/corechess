using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace CoreChess.Controls
{
    public class ItemsList : UserControl
    {
        private IEnumerable<object> m_Items;
        private bool m_Selectable = false;
        private ContentControl m_SelectedControl;
        private object m_SelectedItem;

        private IDataTemplate m_ItemTemplate;
        private ItemsRepeater m_ItemsRepeater;

        public static readonly DirectProperty<ItemsList, IEnumerable<object>> ItemsProperty =
                AvaloniaProperty.RegisterDirect<ItemsList, IEnumerable<object>>(
                    nameof(Items),
                    o => o.Items,
                    (o, v) => o.Items = v);

        public static readonly DirectProperty<ItemsList, bool> SelectableProperty =
                AvaloniaProperty.RegisterDirect<ItemsList, bool>(
                    nameof(Selectable),
                    o => o.Selectable,
                    (o, v) => o.Selectable = v);

        public static readonly DirectProperty<ItemsList, IDataTemplate> ItemTemplateProperty =
                AvaloniaProperty.RegisterDirect<ItemsList, IDataTemplate>(
                    nameof(ItemTemplate),
                    o => o.ItemTemplate,
                    (o, v) => o.ItemTemplate = v);

        public static readonly DirectProperty<ItemsList, object> SelectedItemProperty =
                AvaloniaProperty.RegisterDirect<ItemsList, object>(
                    nameof(SelectedItem),
                    o => o.SelectedItem,
                    (o, v) => o.SelectedItem = v);

        public ItemsList()
        {
            InitializeComponent();

            this.FindControl<ScrollViewer>("m_ScrollViewer").DataContext = this;
        }

        public IEnumerable<object> Items
        {
            get { return m_Items; }
            set
            {
                if (SetAndRaise(ItemsProperty, ref m_Items, value)) {
                    m_ItemsRepeater.Items = m_Items;
                }
            }
        }

        public IDataTemplate ItemTemplate
        {
            get { return m_ItemTemplate; }
            set
            {
                if (SetAndRaise(ItemTemplateProperty, ref m_ItemTemplate, value)) {
                }
            }
        }

        public bool Selectable
        {
            get { return m_Selectable; }
            set
            {
                if (SetAndRaise(SelectableProperty, ref m_Selectable, value)) {
                    if (!Selectable)
                        SelectedItem = null;
                }
            }
        }
        public object SelectedItem
        {
            get { return m_SelectedItem; }
            set
            {
                if (SetAndRaise(SelectedItemProperty, ref m_SelectedItem, value)) {

                }
            }
        }

        private void OnElementPrepared(object sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            var ctrl = args.Element as ContentControl;
            SetItemBackground(ctrl);
        }

        private void OnMouseEnter(object sender, Avalonia.Input.PointerEventArgs args)
        {
            var ctrl = sender as ContentControl;
            if (ctrl.DataContext != SelectedItem)
                ctrl.Background = (SolidColorBrush)this.FindResource("SystemControlHighlightListLowBrush");
        }

        private void OnMouseLeave(object sender, Avalonia.Input.PointerEventArgs args)
        {
            var ctrl = sender as ContentControl;
            if (ctrl.DataContext != SelectedItem)
                ctrl.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void OnItemTapped(object sender, RoutedEventArgs e)
        {
            if (!Selectable)
                return;

            var ctrl = sender as ContentControl;
            SelectedItem = ctrl.DataContext;
            if (m_SelectedControl != null)
                SetItemBackground(m_SelectedControl);
            m_SelectedControl = ctrl;
            SetItemBackground(ctrl);
        }

        private void SetItemBackground(ContentControl ctrl)
        {
            if (ctrl.DataContext == SelectedItem) {
                ctrl.Background = (SolidColorBrush)this.FindResource("SystemControlHighlightListAccentLowBrush");
            } else {
                ctrl.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            m_ItemsRepeater = this.FindControl<ItemsRepeater>("m_ItemsRepeater");
        }
    }
}
