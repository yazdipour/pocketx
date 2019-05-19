using System;
using Windows.UI.Xaml.Data;

namespace PocketX.Converter
{
    public class BooleanToCustomStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(parameter is string parameterString) || string.IsNullOrEmpty(parameterString))
                return parameter;
            var parameters = parameterString.Split('|');
            return (bool)value ? parameters[0] : parameters[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => parameter;
    }
}
