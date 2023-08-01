using System.Reflection;

namespace CoreChess.Views
{
    public partial class SplashWindow : Abstracts.BaseView
    {
        public SplashWindow()
        {
            this.InitializeComponent();
            m_Title.Text = $"CoreChess v.{Assembly.GetEntryAssembly()?.GetName().Version}";
            m_Copyright.Text = ((AssemblyCopyrightAttribute)System.Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false))?.Copyright;
        }
    }
}