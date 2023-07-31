using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ChessLib;
using ChessLib.Engines;

namespace CoreChess.Controls;

public partial class PlayerControl : UserControl
{
    private Game.Colors? m_Color;
    private bool m_IsHuman = true;
    private bool m_SupportChess960 = true;

    public delegate void ColorChangeHandler(Game.Colors? color);
    public event ColorChangeHandler ColorChanged;

    public delegate void SupportChess960ChangeHandler(bool supported);
    public event SupportChess960ChangeHandler SupportChess960Changed;

    public PlayerControl()
    {
        InitializeComponent();

        m_White.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "wKnight.png"));
        m_Black.Source = new Bitmap(System.IO.Path.Combine(App.GetPiecesPath(App.Settings.PiecesSet), "bKnight.png"));

        m_Engines.ItemsSource = App.Settings.Engines.OrderBy(e => e.Name);
        m_Engines.SelectedIndex = 0;

        m_PlayerType.SelectedIndex = 0;
        m_PlayerNameStack.IsVisible = true;
        m_EngineStack.IsVisible = false;
        m_EngineEloStack.IsVisible = false;
        m_PersonalityStack.IsVisible = false;
        m_TheKingPersonalityStack.IsVisible = false;
        m_EngineElo.Value = 0;
    }

    public Game.Colors? Color
    {
        get => m_Color;
        set
        {
            switch (value) {
                case Game.Colors.White:
                    OnWhiteClick(null, null);
                    break;
                case Game.Colors.Black:
                    OnBlackClick(null, null);
                    break;
                default:
                    OnRandomClick(null, null);
                    break;
            }

            m_Color = value;
        }
    }

    public bool IsHuman
    {
        get => m_IsHuman;
        set
        {
            if (value == m_IsHuman)
                return;

            m_PlayerType.SelectedIndex = value ? 0 : 1;
            m_IsHuman = value;

            if (m_IsHuman) {
                m_PlayerNameStack.IsVisible = true;
                m_EngineStack.IsVisible = false;
                m_EngineEloStack.IsVisible = false;
                m_PersonalityStack.IsVisible = false;
                m_TheKingPersonalityStack.IsVisible = false;

                SupportChess960 = true;
            } else {
                m_PlayerNameStack.IsVisible = false;
                m_EngineStack.IsVisible = true;
                if (m_Engines.SelectedItem == null)
                    m_Engines.SelectedIndex = 0;
                OnEngineChanged(null, null);
            }
        }
    }

    public bool SupportChess960
    {
        get => m_SupportChess960;
        private set
        {
            if (value == m_SupportChess960)
                return;

            m_SupportChess960 = value;
            SupportChess960Changed?.Invoke(m_SupportChess960);
        }
    }

    public string PlayerName
    {
        get => m_PlayerName.Text;
        set
        {
            m_PlayerName.Text = value;
        }
    }

    public string EngineId
    {
        get => !IsHuman ? (m_Engines.SelectedItem as EngineBase)?.Id : null;
        set
        {
            var selectedEngine = App.Settings.GetEngine(value);
            m_Engines.SelectedItem = selectedEngine;
        }
    }

    public int? EngineElo
    {
        get => !IsHuman ? (int)m_EngineElo.Value : null;
        set
        {
            m_EngineElo.Value = value;
        }
    }

    public string EnginePersonality
    {
        get => !IsHuman ? m_Personality.SelectedItem as string : null;
        set
        {
            m_Personality.SelectedItem = value;
        }
    }

    public TheKing.Personality TheKingPersonality
    {
        get => !IsHuman ? m_TheKingPersonality.SelectedItem as TheKing.Personality : null;
        set
        {
            if (value == null) {
                m_TheKingPersonality.SelectedItem = null;
                return;
            }

            m_TheKingPersonality.SelectedItem = (m_TheKingPersonality.Items as IEnumerable<TheKing.Personality>).FirstOrDefault(p => p.Name == value.Name);
        }
    }

    private void OnWhiteClick(object sender, RoutedEventArgs e)
    {
        m_WhiteBtn.IsChecked = true;
        m_BlackBtn.IsChecked = false;
        m_RandomBtn.IsChecked = false;

        if (m_Color != Game.Colors.White) {
            m_Color = Game.Colors.White;
            ColorChanged?.Invoke(Game.Colors.White);
        }
    }

    private void OnBlackClick(object sender, RoutedEventArgs e)
    {
        m_BlackBtn.IsChecked = true;
        m_WhiteBtn.IsChecked = false;
        m_RandomBtn.IsChecked = false;

        if (m_Color != Game.Colors.Black) {
            m_Color = Game.Colors.Black;
            ColorChanged?.Invoke(Game.Colors.Black);
        }
    }

    private void OnRandomClick(object sender, RoutedEventArgs e)
    {
        m_RandomBtn.IsChecked = true;
        m_WhiteBtn.IsChecked = false;
        m_BlackBtn.IsChecked = false;
        if (m_Color != null) {
            m_Color = null;
            ColorChanged?.Invoke(null);
        }
    }

    private void OnPlayerTypeChanged(object sender, SelectionChangedEventArgs args)
    {
        IsHuman = m_PlayerType.SelectedIndex == 0;
        OnEngineChanged(null, null);
    }

    private void OnEngineChanged(object sender, SelectionChangedEventArgs args)
    {
        var engine = m_Engines.SelectedItem as EngineBase;
        if (engine == null || IsHuman)
            return;

        SupportChess960 = engine.SupportChess960();
        m_EngineEloStack.IsVisible = engine.CanSetElo();

        m_EngineElo.Maximum = engine.GetMaxElo();
        m_EngineElo.Minimum = engine.GetMinElo();
        m_EngineElo.Value = m_EngineElo.Maximum;

        // TheKing personalities
        if (engine is TheKing) {
            m_TheKingPersonalityStack.IsVisible = true;
            var opt = engine.GetOption(TheKing.PersonalitiesFolderOptionName);
            if (opt != null && !string.IsNullOrEmpty(opt.Value))
                m_TheKingPersonality.ItemsSource = TheKing.Personality.GetFromFolder(opt.Value).OrderByDescending(p => p.Elo);
        } else {
            m_TheKingPersonalityStack.IsVisible = false;

            if (engine is Uci) {
                // Dragon 2.6 personality
                var pOpt = engine.GetOption(Uci.PersonalityOptionNames);
                if (pOpt != null && pOpt.Type == "combo") {
                    m_PersonalityStack.IsVisible = true;
                    m_Personality.ItemsSource = pOpt.ValidValues;
                    m_Personality.SelectedItem = pOpt.Default;
                } else {
                    m_PersonalityStack.IsVisible = false;
                }
            } else {
                m_PersonalityStack.IsVisible = false;
            }
        }
    } // OnEngineChanged
}