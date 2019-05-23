using System;
using Windows.UI.Xaml.Data;

namespace PocketX.Converter
{
    public class ArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            string.Join(parameter is string spl ? spl : " ", value);

        public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
    }
}
