namespace Golden.Win.Utility
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public static class WPFUtilities
	{
        private static readonly bool _IsInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());

		public static bool IsInDesignMode
        {
            get { return _IsInDesignMode; }
        }

		[DllImport("Gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		public static Icon ConvertImageToIcon(Image image)
		{
			var bmp = image as Bitmap;
			if (bmp == null) bmp = new Bitmap(image);
			var hBmp = bmp.GetHicon();
			var icon = Icon.FromHandle(hBmp);
			DeleteObject(hBmp);
			return icon;
		}
		public static BitmapSource ConvertImageToImageSource(Image image)
		{
			var bmp = image as Bitmap;
			if (bmp == null) bmp = new Bitmap(image);
			var hBmp = bmp.GetHbitmap();
			var bmpSrc = Imaging.CreateBitmapSourceFromHBitmap(
				hBmp,
				IntPtr.Zero,
				new Int32Rect(0, 0, bmp.Width, bmp.Height),
				BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
			DeleteObject(hBmp);
			return bmpSrc;
		}
		public static BitmapSource ConvertIconToImageSource(Icon icon)
		{
			var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				new Int32Rect(0, 0, icon.Width, icon.Height),
				BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
			return bmpSrc;
		}
		public static BitmapSource StreamToImageSource(Stream stream)
		{
			var bmpSource = new BitmapImage();
			bmpSource.BeginInit();
			bmpSource.StreamSource = stream;
			bmpSource.EndInit();
			return bmpSource;
		}
		public static void SetInputLanguage(CultureInfo cultureInfo)
		{
			Thread.CurrentThread.CurrentCulture = cultureInfo;
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
			System.Windows.Forms.Application.CurrentInputLanguage = System.Windows.Forms.InputLanguage.FromCulture(cultureInfo);
		}
		public static void SetPersianInputLanguage()
		{
			SetInputLanguage(CultureInfo.GetCultureInfo("FA-IR"));
		}
		public static void SetEnglishInputLanguage()
		{
			SetInputLanguage(CultureInfo.GetCultureInfo("EN-US"));
		}
		public static string GetDefaultPrinterName()
		{
			return (new System.Drawing.Printing.PrinterSettings()).PrinterName;
		}
        public static IEnumerable<DependencyObject> GetVisualTreeParents(DependencyObject obj)
        {
            if (obj == null)
                yield break;
            var parent = VisualTreeHelper.GetParent(obj);
            while(parent != null)
            {
                yield return parent;
                parent = VisualTreeHelper.GetParent(obj);
            }
            yield break;
        }
        public static IEnumerable<DependencyObject> GetVisualTreeChildren(DependencyObject obj)
        {
            if (obj == null)
                yield break;
            var childCount = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                yield return child;
                foreach (var ch in GetVisualTreeChildren(child))
                    yield return ch;
            }
        }
    }
}
