using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CoreChess.Controls.Models;

namespace CoreChess.Controls;

public class Spinner : UserControl
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var iv = this.GetObservable(UserControl.IsVisibleProperty);
        iv.Subscribe(value =>
        {
            var stack = this.FindControl<StackPanel>("WaitSpinner");
            if (value)
                stack.Classes.Add("spinner");
            else
                stack.Classes.Remove("spinner");
        });
    }
}