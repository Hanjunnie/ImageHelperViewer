using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VisionAlgolismViewer.ViewModels;

namespace VisionAlgolismViewer.Converters
{
    public class EffectTypeToVisibilityConverter : IValueConverter
    {
        public string TargetEffect { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EffectType currentEffect && Enum.TryParse<EffectType>(TargetEffect, out var targetEffect))
            {
                return currentEffect == targetEffect ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
