using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;


namespace DirectStar.ViewModel
{
    public class User_B_Converter : MarkupExtension, IValueConverter
    {
        private static User_B_Converter _instance;

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new User_B_Converter());
        }
    }
}
