using System;
using System.Globalization;
using System.Windows.Data;

namespace QuanLySCL.GUI.Converters
{
    public class DateEqualsToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;

            if (values[0] is not DateTime left) return false;
            if (values[1] is not DateTime right) return false;

            return left.Date == right.Date;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

