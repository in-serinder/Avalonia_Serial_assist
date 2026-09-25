using Avalonia.Data.Converters;
using Avalonia.Media;
using System;


namespace Avalonia_Serial_assist.ViewModels;

public class DirectionToColorConverter: IValueConverter

{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var direction = value as string;
        if (direction == "RX")
            return Brushes.Blue;
        else if (direction == "TX")
            return Brushes.Green;
        return Brushes.Gray; // 默认颜色
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}