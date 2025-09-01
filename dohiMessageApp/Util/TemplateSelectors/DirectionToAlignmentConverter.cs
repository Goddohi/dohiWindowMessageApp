using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WalkieDohi.Packet.Messages.Entity;

namespace WalkieDohi.Util.TemplateSelectors
{
    public class DirectionToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value: MessageDirection (Send/Receive)
            return (MessageDirection)value == MessageDirection.Send
                ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class DirectionToBubbleBrushConverter : IValueConverter
    {
        public Brush SendBrush { get; set; } = (Brush)new BrushConverter().ConvertFromString("#3B82F6");
        public Brush ReceiveBrush { get; set; } = (Brush)new BrushConverter().ConvertFromString("#F1F5F9");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (MessageDirection)value == MessageDirection.Send ? SendBrush : ReceiveBrush;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class DirectionToForegroundConverter : IValueConverter
    {
        public Brush SendForeground { get; set; } = Brushes.White;
        public Brush ReceiveForeground { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (MessageDirection)value == MessageDirection.Send ? SendForeground : ReceiveForeground;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class DirectionToCornerRadiusConverter : IValueConverter
    {
        // Send(오른쪽) 꼬리: 8,8,0,8 / Receive(왼쪽) 꼬리: 8,8,8,0
        public CornerRadius SendRadius { get; set; } = new CornerRadius(8, 8, 0, 8);
        public CornerRadius ReceiveRadius { get; set; } = new CornerRadius(8, 8, 8, 0);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (MessageDirection)value == MessageDirection.Send ? SendRadius : ReceiveRadius;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class DirectionToCornerNameViewConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (MessageDirection)value == MessageDirection.Send ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { throw new NotImplementedException(); }
    }
}
