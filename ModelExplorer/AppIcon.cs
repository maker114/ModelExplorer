using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModelExplorer
{
    public static class AppIcon
    {
        private static ImageSource _windowIcon;

        public static ImageSource WindowIcon
        {
            get
            {
                if (_windowIcon == null)
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "ModelExplorer.Resources.app.ico");
                    if (stream != null)
                    {
                        using (stream)
                        {
                            _windowIcon = BitmapFrame.Create(
                                stream,
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                        }
                    }
                }
                return _windowIcon;
            }
        }
    }
}
