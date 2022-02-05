using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib.Engines;
using System.Collections.Generic;
using System.Linq;
using AvaloniaColorPicker;
using Avalonia.Media;
using System.Threading.Tasks;

namespace CoreChess.Views
{
    public class ColorPickerWindow : BaseView, IColorPickerWindow
    {
        private bool Result = false;

        public Color Color
        {
            get => this.FindControl<ColorPicker>("ColorPicker").Color;
            set
            {
                this.FindControl<ColorPicker>("ColorPicker").Color = value;
            }
        }

        public Color? PreviousColor
        {
            get => this.FindControl<ColorPicker>("ColorPicker").PreviousColor;
            set
            {
                this.FindControl<ColorPicker>("ColorPicker").PreviousColor = value;
            }
        }

        public ColorPickerWindow()
        {
            this.InitializeComponent();
        }

        public ColorPickerWindow(Color? previousColor) : this()
        {
            this.PreviousColor = previousColor;

            if (previousColor != null)
                this.Color = previousColor.Value;
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        public new async Task<Color?> ShowDialog(Window parent)
        {
            await base.ShowDialog(parent);

            if (this.Result)
                return this.Color;
            else
                return null;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.Result = true;
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            this.Close();
        }
    }
}