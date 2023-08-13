using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CoreChess.Views;

namespace CoreChess.Controls;

public partial class OpeningBookComboBox : UserControl
{
    public OpeningBookComboBox()
    {
        InitializeComponent();

        m_OpeningBookType.SelectedIndex = 0;
    }

    public string Value
    {
        get => m_OpeningBook.Text;
        set
        {
            m_OpeningBook.Text = value;
            if (string.IsNullOrEmpty(value))
                m_OpeningBookType.SelectedIndex = 0;
            else if (value == Settings.InternalOpeningBook)
                m_OpeningBookType.SelectedIndex = 1;
            else
                m_OpeningBookType.SelectedIndex = 2;
        }
    }

    private void OnOpeningBookTypeChanged(object sender, SelectionChangedEventArgs args)
    {
        var cb = sender as ComboBox;
        m_OpeningBook.Text = cb.SelectedIndex switch
        {
            0 => string.Empty,
            1 => Settings.InternalOpeningBook,
            2 when m_OpeningBook.Text == Settings.InternalOpeningBook => string.Empty,
            _ => m_OpeningBook.Text
        };
        m_OpeningBookCustom.IsVisible = cb.SelectedIndex == 2;
    } // OnOpeningBookTypeChanged

    private async void OnOpeningBookClick(object sender, RoutedEventArgs e)
    {
        var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            FileTypeFilter = new []
            {
                new FilePickerFileType("Polyglot opening book")
                {
                    Patterns = new []{ "*.bin" }
                },
                new FilePickerFileType("Arena opening book")
                {
                    Patterns = new []{ "*.abk" }
                },
                new FilePickerFileType("Chessmaster opening book")
                {
                    Patterns = new []{ "*.obk" }
                },
            }
        });
        if (files.Count > 0) {
            m_OpeningBook.Text = files[0].Path.AbsolutePath;
        }
    } // OnOpeningBookClick
}