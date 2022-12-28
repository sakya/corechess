using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CoreChess.Abstracts;
using CoreChess.Pages;

namespace CoreChess.Views;

public class MainWindow : Abstracts.BaseView
{
    private Grid m_Container;
    private Controls.TitleBar m_TitleBar;
    readonly List<BasePage> m_PageHistory = new();
    private readonly Dictionary<string, BasePage.PageState> m_PageStates = new();
    private bool m_ChangingPage;
    private WindowNotificationManager m_NotificationManager;

    #region classes
    public class TransitionSettings
    {
        public enum EnterTransitions
        {
            None,
            SlideLeft,
            SlideUp,
            FadeIn
        }

        public TransitionSettings(EnterTransitions transition, TimeSpan duration, Avalonia.Animation.Easings.Easing easing = null) {
            Type = transition;
            Duration = duration;
            Easing = easing ?? new Avalonia.Animation.Easings.LinearEasing();
        }

        public EnterTransitions Type { get; set; }
        public TimeSpan Duration { get; set; }
        public Avalonia.Animation.Easings.Easing Easing { get; set; }
    } // TransitionSettings
    #endregion

    public MainWindow()
    {
        InitializeComponent();
    }


    public MainWindow(string[] args)
    {
        InitializeComponent();

        WindowTitle = Title;
        Transition = new TransitionSettings(TransitionSettings.EnterTransitions.SlideLeft, TimeSpan.FromMilliseconds(250));
        BackKey = Key.Escape;

        m_Container = this.FindControl<Grid>("Container");
        m_TitleBar = this.FindControl<Controls.TitleBar>("TitleBar");

        Closing += OnWindowClosing;
        KeyDown += OnKeyDown;
    }


    #region public properties
    public TransitionSettings Transition { get; set; }
    public Key BackKey { get; set; }

    public string WindowTitle { get; set; }

    public BasePage CurrentPage
    {
        get
        {
            return m_Container?.Children.FirstOrDefault(c => c is BasePage) as BasePage;
        }
    }

    public bool CanNavigateBack => m_PageHistory.Count > 0;

    #endregion

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await Task.Delay(10);
        var mp = new MainPage();
        await NavigateTo(mp);
    }

    protected sealed override void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        if (App.Settings.RestoreWindowSizeAndPosition)
            RestoreWindowSizeAndPosition();

        m_NotificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3,
            Margin = OperatingSystem.IsWindows() ? new Thickness(0, 30, 0, 0) : new Thickness(0)
        };

        Transition = new TransitionSettings(TransitionSettings.EnterTransitions.SlideLeft, TimeSpan.FromMilliseconds(250));
        BackKey = Key.Escape;

        m_Container = this.FindControl<Grid>("Container");
        m_TitleBar = this.FindControl<Controls.TitleBar>("TitleBar");

        Closing += OnWindowClosing;
        KeyDown += OnKeyDown;

        base.InitializeComponent();
    }

    protected override void HandleWindowStateChanged(WindowState state)
    {
        CurrentPage?.HandleWindowStateChanged(state);
    }

    #region public operations
    /// <summary>
    /// Navigate to a new page
    /// </summary>
    /// <param name="page"></param>
    /// <returns>True on success</returns>
    public async Task<bool> NavigateTo(BasePage page)
    {
        if (m_ChangingPage)
            return false;

        var exitingPage = CurrentPage;
        if (exitingPage != null) {
            if (!await exitingPage.OnNavigatingFrom(BasePage.NavigationDirection.Forward))
                return false;
            m_PageHistory.Add(exitingPage);
        }

        await ChangePage(exitingPage, page, false);
        page.OnNavigatedTo(BasePage.NavigationDirection.Forward);

        return true;
    } // NavigateTo

    /// <summary>
    /// Navigate to the previous page
    /// </summary>
    /// <returns>True on success</returns>
    public async Task<bool> NavigateBack()
    {
        if (m_ChangingPage)
            return false;

        if (m_PageHistory.Count > 0) {
            var exitingPage = CurrentPage;
            if (exitingPage != null && await exitingPage.OnNavigatingFrom(BasePage.NavigationDirection.Backward) == false)
                return false;

            var enteringPage = m_PageHistory.Last();
            m_PageHistory.Remove(enteringPage);

            await ChangePage(exitingPage, enteringPage, true);
            exitingPage?.Dispose();
            RemovePageState(exitingPage);

            enteringPage.OnNavigatedTo(BasePage.NavigationDirection.Backward);
            return true;
        }
        return false;
    } // NavigateBack

    public void SavePageState(BasePage.PageState state) {
        m_PageStates[state.PageId] = state;
    } // SavePageState

    public BasePage.PageState LoadPageState<T>(BasePage page) where T : BasePage.PageState
    {
        BasePage.PageState res;
        if (m_PageStates.TryGetValue(page.Id, out res))
            return res as T;
        return null;
    } // LoadPageState

    public void RemovePageState(BasePage page) {
        if (page != null)
            m_PageStates.Remove(page.Id);
    } // RemovePageState

    public void ShowNotification(string title, string message)
    {
        m_NotificationManager.Show(new Notification(title, message));
    }

    #endregion

    private async Task ChangePage(BasePage exiting, BasePage entering, bool back)
    {
        if (entering == null)
            throw new ArgumentNullException(nameof(entering));

        m_ChangingPage = true;
        entering.Opacity = 0;
        m_Container!.Children.Add(entering);
        if (exiting == null) {
            entering.Opacity = 1;
            m_ChangingPage = false;
            return;
        }

        if (Transition == null || Transition.Type == TransitionSettings.EnterTransitions.None) {
            m_Container.Children.Remove(exiting);
            entering.Opacity = 1;
            m_ChangingPage = false;
            return;
        }

        exiting.IsHitTestVisible = false;
        entering.IsHitTestVisible = false;

        AvaloniaProperty property = null;
        var from = 0.0;
        var to = 0.0;
        switch (Transition.Type) {
            case TransitionSettings.EnterTransitions.SlideLeft:
                property = TranslateTransform.XProperty;
                from = 0.0;
                to = this.Bounds.Size.Width;

                exiting.RenderTransform = new TranslateTransform();
                entering.RenderTransform = new TranslateTransform() {
                    X = to
                };
                break;
            case TransitionSettings.EnterTransitions.SlideUp:
                property = TranslateTransform.YProperty;
                from = 0.0;
                to = this.Bounds.Size.Height;

                exiting.RenderTransform = new TranslateTransform();
                entering.RenderTransform = new TranslateTransform() {
                    Y = to
                };
                break;
            case TransitionSettings.EnterTransitions.FadeIn:
                property = UserControl.OpacityProperty;
                from = 1.0;
                to = 0.0;
                break;
        }

        // Exiting
        var exitAnim = new Animation()
        {
            Duration = Transition.Duration,
            Easing = Transition.Easing
        };

        var kf = new KeyFrame()
        {
            Cue = new Cue(0.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = property,
            Value = from
        });
        exitAnim.Children.Add(kf);

        kf = new KeyFrame()
        {
            Cue = new Cue(1.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = property,
            Value = back ? to : -to
        });
        exitAnim.Children.Add(kf);

        // Entering
        var enterAnim = new Animation()
        {
            Duration = Transition.Duration,
            Easing = Transition.Easing
        };

        kf = new KeyFrame()
        {
            Cue = new Cue(0.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = property,
            Value = back ? -to : to
        });
        enterAnim.Children.Add(kf);

        kf = new KeyFrame()
        {
            Cue = new Cue(1.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = property,
            Value = from
        });
        enterAnim.Children.Add(kf);

        if (Transition.Type == TransitionSettings.EnterTransitions.FadeIn) {
            await exitAnim.RunAsync(exiting, null);
            exiting.Opacity = 0;
            await enterAnim.RunAsync(entering, null);
            entering.Opacity = 1.0;
        } else {
            var tasks = new List<Task>();
            tasks.Add(exitAnim.RunAsync(exiting, null));
            tasks.Add(enterAnim.RunAsync(entering, null));
            entering.Opacity = 1;
            await Task.WhenAll(tasks);
        }

        entering.RenderTransform = null;
        exiting.RenderTransform = null;

        entering.IsHitTestVisible = true;
        m_Container.Children.Remove(exiting);
        entering.Focus();

        m_TitleBar!.CanGoBack = CanNavigateBack;
        m_ChangingPage = false;
    } // ChangePage

    private async void OnWindowClosing(object sender, CancelEventArgs args)
    {
        if (m_ChangingPage) {
            args.Cancel = true;
            return;
        }

        if (CurrentPage?.NavigateBackOnWindowClose == true && CanNavigateBack) {
            args.Cancel = true;
            await NavigateBack();
        }
    } // OnWindowClosing

    private async void OnKeyDown(object sender, KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (m_ChangingPage)
            return;

        if (BaseDialog.CurrentDialog != null) {
            BaseDialog.CurrentDialog.OnKeyDown(sender, e);
            if (e.Handled)
                return;
        } else if (CurrentPage != null) {
            CurrentPage.OnKeyDown(sender, e);
            if (e.Handled)
                return;
        }

        if (CurrentPage != null && CurrentPage.NavigateBackWithKeyboard && e.KeyModifiers == KeyModifiers.None && e.Key == BackKey) {
            e.Handled = true;
            await NavigateBack();
        }
    } // OnKeyDown
}