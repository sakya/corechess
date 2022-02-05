﻿namespace CoreChess.Localizer
{
    using System;
    using Avalonia.Data;
    using Avalonia.Markup.Xaml;
    using Avalonia.Markup.Xaml.MarkupExtensions;
    
    public class LocalizeExtension : MarkupExtension
    {
        public LocalizeExtension(string key)
        {
            this.Key = key;
        }

        public string Key { get; set; }

        public string Context { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var keyToUse = Key;
            if (!string.IsNullOrWhiteSpace(Context))
                keyToUse = $"{Context}/{Key}";

            var binding = new ReflectionBindingExtension($"[{keyToUse}]")
            {
                Mode = BindingMode.OneWay,
                Source = Localizer.Instance,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
