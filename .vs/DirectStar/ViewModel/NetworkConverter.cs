using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Data;

namespace DirectStar.ViewModel 
{
    class NetworkConverter : MarkupExtension, IValueConverter
    {
        private static NetworkConverter _instance;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] bit_rate = new string[] { "KB / s", "MB / s" };
            var spd = System.Convert.ToInt64(value) / 8;
            if (spd > 1000000) return (spd / 1000000).ToString() + " MB / s";
            if (spd > 1000) return (spd / 1000).ToString() + " KB / s";
            return spd.ToString() + " B / s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new NetworkConverter());
        }
    }
}
