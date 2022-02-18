using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreChess.Utils
{
    public static  class Animations
    {
        public static async Task<bool> FadeOutControl(Control ctrl, TimeSpan? duration = null)
        {
            Animation fadeOut = new Animation()
            {
                Duration = duration ?? TimeSpan.FromMilliseconds(250),
                Easing = new Avalonia.Animation.Easings.LinearEasing()
            };

            var kf = new KeyFrame()
            {
                Cue = new Cue(0.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = UserControl.OpacityProperty,
                Value = 1.0
            });
            fadeOut.Children.Add(kf);

            kf = new KeyFrame()
            {
                Cue = new Cue(1.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = UserControl.OpacityProperty,
                Value = 0.0
            });
            fadeOut.Children.Add(kf);
            await fadeOut.RunAsync(ctrl, null);
            ctrl.Opacity = 0;

            return true;
        } // FadeOutControl

        public static async Task<bool> FadeInControl(Control ctrl, TimeSpan? duration = null)
        {
            Animation fadeIn = new Animation()
            {
                Duration = duration ?? TimeSpan.FromMilliseconds(250),
                Easing = new Avalonia.Animation.Easings.LinearEasing()
            };

            var kf = new KeyFrame()
            {
                Cue = new Cue(0.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = UserControl.OpacityProperty,
                Value = 0.0
            });
            fadeIn.Children.Add(kf);

            kf = new KeyFrame()
            {
                Cue = new Cue(1.0)
            };
            kf.Setters.Add(new Setter()
            {
                Property = UserControl.OpacityProperty,
                Value = 1.0
            });
            fadeIn.Children.Add(kf);
            await fadeIn.RunAsync(ctrl, null);
            ctrl.Opacity = 1;

            return true;
        } // FadeInControl

    }
}
