using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace WalkieDohi.Util
{   /// <summary>
    /// 사용법    Visibility="{Binding IsImage, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }

}
