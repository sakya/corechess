using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CoreChess.Controls.Models;

namespace CoreChess.Controls;

public class Spinner : UserControl
{
    public static readonly DirectProperty<Spinner, string?> MessageProperty =
        AvaloniaProperty.RegisterDirect<Spinner, string?>(
            nameof(Message),
            o => o.Message,
            (o, v) => o.Message = v);

    private readonly SpinnerModel _model = new();
    private string? _message;

    public Spinner()
    {
        DataContext = _model;
        InitializeComponent();
    }

    public string? Message
    {
        get => _message;
        set
        {
            if (SetAndRaise(MessageProperty, ref _message, value)) {
                _model.Message = value;
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