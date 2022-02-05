using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System.Reflection;

namespace CoreChess.Views
{
    public class AboutWindow : BaseView
    {
        public AboutWindow()
        {
            this.InitializeComponent();

            var txt = this.FindControl<TextBlock>("m_Title");
            txt.Text = $"CoreChess{System.Environment.NewLine}v.{Assembly.GetEntryAssembly().GetName().Version}";

            txt = this.FindControl<TextBlock>("m_Copyright");
            txt.Text = ((AssemblyCopyrightAttribute)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false)).Copyright;
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape || args.Key == Key.Enter)
                this.Close();
        }
    }
}
