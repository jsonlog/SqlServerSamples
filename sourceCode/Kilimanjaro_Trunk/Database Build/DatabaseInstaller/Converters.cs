using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!(value is bool))
                throw new ArgumentException("value must be a boolean", "value");
            if ((bool)value) return Visibility.Visible;
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!(value is Visibility))
                throw new ArgumentException("value must be a System.Windows.Visibility", "value");
            if ((Visibility)value == Visibility.Visible) return true;
            return false;
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!(value is bool))
                throw new ArgumentException("value must be a boolean", "value");
            if ((bool)value) return Visibility.Hidden;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!(value is Visibility))
                throw new ArgumentException("value must be a System.Windows.Visibility", "value");
            if ((Visibility)value == Visibility.Hidden) return true;
            return false;
        }
    }


    public class StringLengthToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (!(value is string))
                throw new ArgumentException("value must be a string", "value");
            if (((string)value).Length > 0)
                return Visibility.Visible;
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}
