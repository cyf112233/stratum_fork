using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;

namespace Stratum.Desktop.Services
{
    public static class QrScanner
    {
        public static string Decode(byte[] imageBytes)
        {
            try
            {
                using var bitmap = SKBitmap.Decode(imageBytes);

                if (bitmap == null)
                {
                    return null;
                }

                using var bgra = bitmap.Copy(SKColorType.Bgra8888);
                var pixelBytes = new byte[bgra.Width * bgra.Height * 4];
                Marshal.Copy(bgra.GetPixels(), pixelBytes, 0, pixelBytes.Length);

                var source = new RGBLuminanceSource(pixelBytes, bgra.Width, bgra.Height,
                    RGBLuminanceSource.BitmapFormat.BGRA32);
                var reader = new BarcodeReaderGeneric { AutoRotate = true };
                var result = reader.Decode(source);
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }
    }
}
