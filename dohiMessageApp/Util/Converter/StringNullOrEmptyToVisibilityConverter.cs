using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace WalkieDohi.Util
{
    /// <summary>
    /// StringNullOrEmptyToVisibilityConverter  : 입력된 값이 없으면 text를 보여주는 곳에 사용 
    /// 
    /// 사용처
    /// FriendMainListView.xmal
    /// Visibility="{Binding Text, ElementName=FriendSearchBox, Converter={StaticResource StringNullOrEmptyToVisibilityConverter}}"/>
    /// </summary>
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
