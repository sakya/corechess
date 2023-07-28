using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using CoreChess.Controls.Models;

namespace CoreChess.Controls;

public partial class Spinner : UserControl
{
    public static readonly DirectProperty<Spinner, string> MessageProperty =
        AvaloniaProperty.RegisterDirect<Spinner, string>(
            nameof(Message),
            o => o.Message,
            (o, v) => o.Message = v);

    private readonly SpinnerModel m_Model = new();
    private string m_Message;

    public Spinner()
    {
        DataContext = m_Model;
        InitializeComponent();

        var iv = this.GetObservable(UserControl.IsVisibleProperty);
        iv.Subscribe(new AnonymousObserver<bool>(value =>
        {
            if (value)
                WaitSpinner.Classes.Add("spinner");
            else
                WaitSpinner.Classes.Remove("spinner");
        }));
    }

    public string Message
    {
        get => m_Message;
        set
        {
            if (SetAndRaise(MessageProperty, ref m_Message, value)) {
                m_Model.Message = value;
            }
        }
    }
}