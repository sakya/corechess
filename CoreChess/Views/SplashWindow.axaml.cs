using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;
using System.Threading.Tasks;

namespace CoreChess.Views
{
    public class SplashWindow : BaseView
    {
        public SplashWindow()
        {
            this.InitializeComponent();

            var txt = this.FindControl<TextBlock>("m_Title");
            txt.Text = $"CoreChess v.{System.Reflection.Assembly.GetEntryAssembly().GetName().Version}";

            txt = this.FindControl<TextBlock>("m_Copyright");
            txt.Text = ((AssemblyCopyrightAttribute)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false)).Copyright;
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }
    }
}