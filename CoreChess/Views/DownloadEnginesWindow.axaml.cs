using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ChessLib;
using ChessLib.Engines;
using CoreChess.Localizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreChess.Views
{
    public class DownloadEnginesWindow : BaseView
    {
        public DownloadEnginesWindow()
        {
            this.InitializeComponent();
        }

        protected override void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            base.InitializeComponent();
        }
    }
}