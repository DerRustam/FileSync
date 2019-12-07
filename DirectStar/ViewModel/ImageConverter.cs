using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows.Markup;

namespace DirectStar.ViewModel
{
    public class ImageConverter : MarkupExtension, IValueConverter
    {
        private static ImageConverter _instance;
        private Dictionary<string, BitmapImage> imgcache;
        static string[] imgFormat = { ".jpg", ".png", ".jpeg", ".svg", ".gif" };
        static string[] vidFormat = { ".mp4", ".webm", ".avi", ".wmv", ".flv" };
        static string[] musFormat = { ".wav", ".mp3", ".flac", ".aac", ".raw" };

        private void ConverterInit()
        {
            imgcache = new Dictionary<string, BitmapImage>();
            string[] imgNames = new string[]{"PictureIcon.png", "VideoIcon.png", "MusicIcon.png", "FileIcon.png" };
            for (int i = 0; i < imgNames.Length; ++i)
            {
                imgcache.Add(imgNames[i].Replace(".png", ""), InitImage(imgNames[i]));
            }
        }

        private BitmapImage InitImage(string name)
        {
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.DecodePixelHeight = 18;
            img.DecodePixelWidth = 18;
            img.UriSource = new Uri("pack://application:,,,/DirectStar;component/Resources/"+name);
            img.EndInit();
            return img;
        }

        public object Convert(
        object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = value.ToString();
            if (imgFormat.Any(x => name.ToLower().EndsWith(x)))
                return imgcache["PictureIcon"];
            if (vidFormat.Any(x => name.ToLower().EndsWith(x)))
                return imgcache["VideoIcon"];
            if (musFormat.Any(x => name.ToLower().EndsWith(x)))
                return imgcache["MusicIcon"];
            return imgcache["FileIcon"]; 
        }

        public static FileFormat GetFormat(string filename)
        {
            if (imgFormat.Any(x => filename.ToLower().EndsWith(x)))
                return FileFormat.Image;
            if (vidFormat.Any(x => filename.ToLower().EndsWith(x)))
                return FileFormat.Video;
            if (musFormat.Any(x => filename.ToLower().EndsWith(x)))
                return FileFormat.Music;
            return FileFormat.File;
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
           if (_instance == null)
            {
                _instance = new ImageConverter();
                _instance.ConverterInit();
            }
            return _instance;
        }
    }

    public enum FileFormat
    {
        Image,
        Music,
        Video,
        File
    }
}
