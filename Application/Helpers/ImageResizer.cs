
using ImageMagick;
using System.IO;

namespace Core.Application.Helpers
{
    public static class ImageResizer
    {
        public static Stream Resize(Stream stream, int width, int height, Stream returnStream)
        {
            stream.Position = 0;
            using MagickImage magick = new(stream);
            magick.Format = magick.Format;
            magick.Resize((uint)width, (uint)height);
            magick.Write(returnStream);

            return returnStream;
        }
    }
}
