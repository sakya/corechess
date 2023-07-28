using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChessLib;
using ChessLib.Engines;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CoreChess.Controls
{
    public partial class GameAnalyzeGraph : UserControl
    {
        Canvas m_GraphCanvas = null;
        Border m_Line = null;
        Border m_Marker = null;
        double m_ImageRatio = 0;
        int? m_MouseDownIndex = null;
        int ? m_SelectedResultIndex = null;

        CancellationTokenSource m_CancellationTokenSource = null;
        List<EngineBase.AnalyzeResult> m_Results = null;

        #region events
        public class MouseEventArgs : EventArgs
        {
            public MouseEventArgs(int? index)
            {
                Index = index;
            }
            public int? Index { get; set; }
        }
        public delegate void MouseOnResultHandler(object sender, MouseEventArgs e);
        public event MouseOnResultHandler MouseOnResult;
        public event MouseOnResultHandler MouseClickOnResult;
        public event EventHandler AnalyzeCompleted;
        #endregion

        public GameAnalyzeGraph()
        {
            this.InitializeComponent();

            m_ProgressMessage.Text = $"{0.ToString("0.0", App.Settings.Culture)}%";

            this.PropertyChanged += OnControlPropertyChanged;
        }

        public Game Game { get; set; }
        public bool IsInteractive { get; set; }

        public void Clear()
        {
            m_Results = null;
            m_Canvas.Children.Clear();
            m_Canvas.Height = double.NaN;
            m_Progress.Value = 0;
            m_ProgressMessage.Text = $"{0.ToString("0.0", App.Settings.Culture)}%";
            m_ProgressGrid.IsVisible = false;
            m_Analyze.IsVisible = true;
            m_Line = null;
        } // Clear

        public void SetResults(List<ChessLib.Engines.EngineBase.AnalyzeResult> results)
        {
            m_Analyze.IsVisible = false;
            Draw(results);
            m_ProgressGrid.IsVisible = false;
        } // SetResults

        public async void Analyze(int depth)
        {
            m_Analyze.IsVisible = false;
            m_ProgressGrid.IsVisible = true;

            m_SelectedResultIndex = null;
            bool completed = false;
            if (Game != null) {
                m_CancellationTokenSource = new CancellationTokenSource();
                var results = await Game.Analyze(
                    App.Settings.GameAnalysisEngine.Copy(),
                    depth,
                    (idx, count) => {
                        m_Progress.Maximum = count;
                        m_Progress.Value = idx;

                        m_ProgressMessage.Text = $"{((double)idx / (double)count * 100.0).ToString("0.0", App.Settings.Culture)}%";
                    },
                    m_CancellationTokenSource.Token);

                if (!m_CancellationTokenSource.Token.IsCancellationRequested) {
                    completed = true;
                    Draw(results);
                    m_ProgressGrid.IsVisible = false;
                }

                m_CancellationTokenSource.Dispose();
                m_CancellationTokenSource = null;
            }

            if (completed)
                AnalyzeCompleted?.Invoke(this, new EventArgs());
        }

        public async Task<bool> Abort()
        {
            if (m_CancellationTokenSource != null) {
                m_CancellationTokenSource.Cancel();
                while (m_CancellationTokenSource != null)
                    await Task.Delay(10);
            }

            return true;
        }

        public void RemoveMarker()
        {
            if (m_Marker != null)
                m_Marker.IsVisible = false;
        }

        public void AddMarker(int resultIndex)
        {
            if (m_Marker != null && m_Results != null) {
                double step = m_Canvas.Bounds.Width / (m_Results.Count - 1);
                double left = step * resultIndex;
                if (resultIndex >= m_Results.Count - 1)
                    left = m_Canvas.Bounds.Width - 2;
                Canvas.SetLeft(m_Marker, left);
                m_Marker.IsVisible = true;
                m_SelectedResultIndex = resultIndex;
            }
        } // AddMarker

        private void OnControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Bounds") {
                if (m_Results != null) {
                    m_GraphCanvas.Width = m_Canvas.Bounds.Width;
                    m_GraphCanvas.Height = m_Canvas.Bounds.Width / m_ImageRatio;
                    m_Canvas.Height = m_GraphCanvas.Height;
                    if (m_Line != null)
                        m_Line.Height = m_GraphCanvas.Height;
                    if (m_Marker != null)
                        m_Marker.Height = m_GraphCanvas.Height;

                    if (m_SelectedResultIndex.HasValue)
                        AddMarker(m_SelectedResultIndex.Value);
                }
            }
        } // OnControlPropertyChanged

        private void OnAnalyzeClick(object sender, RoutedEventArgs args)
        {
            Analyze(App.Settings.GameAnalysisEngine.GetDefaultAnalyzeDepth());
        } // OnAnalyzeClick

        private void OnMouseMoved(object sender, Avalonia.Input.PointerEventArgs args)
        {
            if (m_Line != null) {
                Avalonia.Point relPos = args.GetPosition(m_Canvas);

                int resultIdx = GetMouseOverIndex(args);
                double step = m_Canvas.Bounds.Width / (m_Results.Count - 1);
                m_Line.IsVisible = true;
                double left = (int)(relPos.X / step) * step + step;
                if (resultIdx >= m_Results.Count - 1)
                    left = m_Canvas.Bounds.Width - 2;
                Canvas.SetLeft(m_Line, left);
                Canvas.SetTop(m_Line, 0);

                MouseOnResult?.Invoke(this, new MouseEventArgs(resultIdx));
            }
        } // OnMouseMoved

        private void OnMouseEnter(object sender, Avalonia.Input.PointerEventArgs args)
        {
            if (m_Line != null)
                OnMouseMoved(sender, args);
        } // OnMouseEnter

        private void OnMouseLeave(object sender, Avalonia.Input.PointerEventArgs args)
        {
            if (m_Line != null)
                m_Line.IsVisible = false;
            MouseOnResult?.Invoke(this, new MouseEventArgs(null));
        } // OnMouseLeave

        private void OnMouseDown(object sender, Avalonia.Input.PointerPressedEventArgs args)
        {
            if (IsInteractive && args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                m_MouseDownIndex = GetMouseOverIndex(args);
        } // OnMouseDown

        private void OnMouseUp(object sender, Avalonia.Input.PointerReleasedEventArgs args)
        {
            if (IsInteractive && args.InitialPressMouseButton == Avalonia.Input.MouseButton.Left) {
                if (m_MouseDownIndex == GetMouseOverIndex(args)) {
                    MouseClickOnResult?.Invoke(this, new MouseEventArgs(m_MouseDownIndex));
                    m_SelectedResultIndex = m_MouseDownIndex;
                }
                m_MouseDownIndex = null;
            }
        } // OnMouseUp

        private void Draw(List<EngineBase.AnalyzeResult> results)
        {
            if (results == null || results.Count <= 1)
                return;

            // CECP gives a value of 9000+ for "mate in" positions
            foreach (var a in results){
                if (a.Score.CentiPawns > 9000)
                    a.Score.CentiPawns = 3000;
                else if (a.Score.CentiPawns < -9000)
                    a.Score.CentiPawns = -3000;
            }

            int max = results.Max(r => Math.Abs(r.Score.CentiPawns));
            int step = (int)Math.Round(1000.0 / ((double)results.Count - 1));
            var bitmap = new WriteableBitmap(new PixelSize(step * (results.Count - 1), 250), new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Opaque);

            using (var fb = bitmap.Lock()) {
                int centerY = bitmap.PixelSize.Height / 2;
                // Draw the middle line
                DrawLine(fb, centerY, 0, centerY, bitmap.PixelSize.Width, centerY);

                var x = 0;
                Avalonia.Point lastPoint = new Avalonia.Point(0, centerY);
                foreach (var r in results) {
                    int value = r.Score.CentiPawns;
                    if (r.Score.CentiPawns == 0 && r.Score.MateIn != 0) {
                        if (r.Color == Game.Colors.White) {
                            if (r.Score.MateIn > 0)
                                value = results.Where(r => r.Color == Game.Colors.White).Max(r => r.Score.CentiPawns);
                            else {
                                value = results.Where(r => r.Color == Game.Colors.Black).Max(r => r.Score.CentiPawns);
                                value *= -1;
                            }
                        } else {
                            if (r.Score.MateIn > 0)
                                value = results.Where(r => r.Color == Game.Colors.Black).Max(r => r.Score.CentiPawns);
                            else {
                                value = results.Where(r => r.Color == Game.Colors.White).Max(r => r.Score.CentiPawns);
                                value *= -1;
                            }
                        }
                    }
                    if (r.Color != Game.Settings.HumanPlayerColor)
                        value *= -1;

                    int y = centerY - (value * centerY / max);
                    if (y < 0)
                        y = 0;
                    else if (y >= 250)
                        y = 249;

                    DrawLine(fb, centerY, (int)lastPoint.X, (int)lastPoint.Y, x, y);
                    lastPoint = new Avalonia.Point(x, y);
                    x += step;
                }
            }

            m_ImageRatio = (double)bitmap.PixelSize.Width / (double)bitmap.PixelSize.Height;
            m_GraphCanvas = new Canvas()
            {
                Width = m_Canvas.Bounds.Width,
                Height = m_Canvas.Bounds.Width / m_ImageRatio,
                Background = new ImageBrush(bitmap),
                ZIndex = 1
            };

            Canvas.SetLeft(m_GraphCanvas, 0);
            Canvas.SetTop(m_GraphCanvas, 0);
            m_Canvas.Height = m_GraphCanvas.Height;
            m_Canvas.Children.Add(m_GraphCanvas);

            if (IsInteractive) {
                m_Line = new Border()
                {
                    BorderThickness = new Thickness(1),
                    Height = m_GraphCanvas.Height,
                    Width = 2,
                    ZIndex = 2,
                    IsVisible = false,
                };
                m_Line.Classes.Add("MoveOver");
                m_Canvas.Children.Add(m_Line);

                m_Marker = new Border()
                {
                    BorderThickness = new Thickness(1),
                    Height = m_GraphCanvas.Height,
                    Width = 2,
                    ZIndex = 3,
                    IsVisible = false,
                };
                m_Marker.Classes.Add("MoveMarker");
                m_Canvas.Children.Add(m_Marker);
            }

            m_Results = results;
        } // Draw

        private void DrawLine(Avalonia.Platform.ILockedFramebuffer fb, int centerY, int fromX, int fromY, int toX, int toY)
        {
            Avalonia.Media.Color color = Avalonia.Media.Colors.GhostWhite;
            if (fromX == toX) {
                int from = Math.Min(fromY, toY);
                int to = Math.Max(fromY, toY);
                for (int y = from; y <= to; y++) {
                    if (y == centerY)
                        color = ((Avalonia.Media.SolidColorBrush)m_Progress.Foreground).Color;
                    else if (y < centerY)
                        color = Avalonia.Media.Colors.GhostWhite;
                    else
                        color = Avalonia.Media.Colors.DarkGray;
                    var pixel = new byte[4] { color.R, color.G, color.B, color.A };

                    IntPtr point = new IntPtr(fb.Address.ToInt64() + fb.RowBytes * y + fromX * 4);
                    Marshal.Copy(pixel, 0, point, pixel.Length);
                }
            } else {
                double m = (double)(fromY - toY) / (double)(fromX - toX);

                int from = Math.Min(fromX, toX);
                int to = Math.Max(fromX, toX);
                for (int x = from; x < to; x++) {
                    int y = (int)(m * (x - fromX) + fromY);
                    DrawLine(fb, centerY, x, centerY, x, y);
                }
            }
        } // DrawLine

        private int GetMouseOverIndex(Avalonia.Input.PointerEventArgs args)
        {
            Avalonia.Point relPos = args.GetPosition(m_Canvas);
            double step = m_Canvas.Bounds.Width / (m_Results.Count - 1);
            var res = (int)(relPos.X / step) + 1;
            if (res < 0)
                res = 0;
            else if (res >= m_Results.Count)
                res = m_Results.Count - 1;
            return res;
        } // GetMouseOverIndex
    }
}