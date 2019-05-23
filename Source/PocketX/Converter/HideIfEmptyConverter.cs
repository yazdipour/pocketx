using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PocketX.Converter
{
    public class HideIfEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value == null || value is string str && string.IsNullOrEmpty(str) || value is Array arr && arr.Length == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
