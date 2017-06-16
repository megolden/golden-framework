namespace Golden
{
    using System.Drawing;
    using System.Windows;
    using System.Windows.Media;

    public static class Extensions
    {
        public static Image GetImage(this EmbeddedResourceManager manager, string resourceKey)
        {
            return Image.FromStream(manager.GetStream(resourceKey));
        }
        public static Icon GetIcon(this EmbeddedResourceManager manager, string resourceKey)
        {
            return manager.GetIcon(resourceKey, -1);
        }
        public static Icon GetIcon(this EmbeddedResourceManager manager, string resourceKey, int size)
        {
            return manager.GetIcon(resourceKey, size, size);
        }
        public static Icon GetIcon(this EmbeddedResourceManager manager, string resourceKey, int width, int height)
        {
            if (width == -1)
                return new Icon(manager.GetStream(resourceKey));
            else
                return new Icon(manager.GetStream(resourceKey), width, height);
        }
        //public static System.Windows.Media.ImageSource GetResourceAsImageSource(this GoldenLibrary.ResourceManagerBase manager, string resourceKey)
        //{
        //	var assemblyName = Assembly.GetAssembly(manager.GetType()).GetName().Name;
        //	var uri = new Uri(string.Concat(@"pack://application:,,,/", assemblyName, ";component/Resources/", resourceKey), UriKind.RelativeOrAbsolute);
        //	return new BitmapImage(uri);
        //}
        public static ImageSource GetImageSource(this EmbeddedResourceManager manager, string resourceKey)
        {
            var stream = manager.GetStream(resourceKey);
            return Win.Utility.WPFUtilities.StreamToImageSource(stream);
        }
        public static ImageSource GetIconImageSource(this EmbeddedResourceManager manager, string resourceKey)
        {
            return manager.GetIconImageSource(resourceKey, -1);
        }
        public static ImageSource GetIconImageSource(this EmbeddedResourceManager manager, string resourceKey, int size)
        {
            return manager.GetIconImageSource(resourceKey, size, size);
        }
        public static ImageSource GetIconImageSource(this EmbeddedResourceManager manager, string resourceKey, int width, int height)
        {
            using (var icon = manager.GetIcon(resourceKey, width, height))
            {
                return Win.Utility.WPFUtilities.ConvertIconToImageSource(icon);
            }
        }
        public static bool IsInDesignMode(this DependencyObject obj)
        {
            return Golden.Win.Utility.WPFUtilities.IsInDesignMode;
        }
    }
}

/*
namespace Golden.Win.Mvvm
{
	public static class ViewModelExtensions
	{
		public static TView ViewAs<TView>(this ViewModelBase viewModel) where TView : IView
		{
			return Golden.Utility.Utilities.Convert<TView>(viewModel.View);
		}
	}
}
*/
