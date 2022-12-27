using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace CoreChess.Abstracts;

public abstract class BaseDialog : UserControl, IDisposable
{
    public static BaseDialog CurrentDialog = null;
    private bool m_Closed;
    private object m_Result;

    protected BaseDialog()
    {
        Animated = true;
        CloseOnBackdropClick = true;
        Margin = new Thickness(5);
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Center;

        var c = App.GetStyleColor("SystemAltHighColor");
        if (c != null)
            Background = new SolidColorBrush(c.Value);
        CornerRadius = new CornerRadius(10);
        BorderThickness = new Thickness(0.5);
        c = App.GetStyleColor("SystemBaseHighColor");
        if (c != null)
            BorderBrush = new SolidColorBrush(c.Value);
    }

    public bool Animated { get; set; }
    public bool CloseOnBackdropClick { get; set; }

    public virtual void Dispose()
    {
    }

    public virtual Task Show(Window owner)
    {
        return Show<object>(owner);
    }

    protected virtual void Opened()
    {

    }

    public virtual async Task<T> Show<T>(Window owner)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));

        var container = owner.FindControl<Grid>("Container");
        if (container == null)
            throw new Exception("Container not found");

        m_Closed = false;
        owner.Closing += OwnerOnClosing;

        // Create the backdrop
        var border = new Border()
        {
            Background = new SolidColorBrush(new Color(255, 0, 0, 0)),
            Opacity = 0.8
        };
        border.Tapped += (s, a) =>
        {
            if (CloseOnBackdropClick)
                Close();
        };
        container.Children.Add(border);
        var bAnim = AnimateBackdrop(border, 0, 0.8);

        // Animate entrance
        container.Children.Add(this);
        if (Animated)
            await Animate(container.Bounds.Height, 0.0);
        await bAnim;
        Focus();
        Opened();

        CurrentDialog = this;
        while (!m_Closed)
            await Task.Delay(100);        

        // Animate exit
        bAnim = AnimateBackdrop(border, 0.8, 0);
        if (Animated)
            await Animate(0.0, container.Bounds.Height);
        await bAnim;

        container.Children.Remove(this);
        container.Children.Remove(border);
        owner.Closing -= OwnerOnClosing;
        CurrentDialog = null;

        return (T)m_Result;
    }

    public virtual void Close()
    {
        Close(null);
    }

    public virtual void Close(object result)
    {
        m_Result = result;
        m_Closed = true;
    }

    #region private operations
    private void OwnerOnClosing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Close(null);
    }

    private async Task Animate(double from, double to)
    {
        IsHitTestVisible = false;

        RenderTransform = new TranslateTransform() {
            Y = from
        };

        var animation = new Animation()
        {
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = new LinearEasing()
        };

        var kf = new KeyFrame()
        {
            Cue = new Cue(0.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = TranslateTransform.YProperty,
            Value = from
        });
        animation.Children.Add(kf);

        kf = new KeyFrame()
        {
            Cue = new Cue(1.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = TranslateTransform.YProperty,
            Value = to
        });
        animation.Children.Add(kf);

        await animation.RunAsync(this, null);
        RenderTransform = new TranslateTransform() {
            Y = to
        };
        IsHitTestVisible = true;
    }

    private async Task AnimateBackdrop(Border backdrop, double from, double to)
    {
        backdrop.Opacity = from;
        var animation = new Animation()
        {
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = new LinearEasing()
        };

        var kf = new KeyFrame()
        {
            Cue = new Cue(0.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = OpacityProperty,
            Value = from
        });
        animation.Children.Add(kf);

        kf = new KeyFrame()
        {
            Cue = new Cue(1.0)
        };
        kf.Setters.Add(new Setter()
        {
            Property = OpacityProperty,
            Value = to
        });
        animation.Children.Add(kf);

        await animation.RunAsync(backdrop, null);
        backdrop.Opacity = to;
    }
    #endregion
}