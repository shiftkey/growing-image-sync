using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Grow
{
    public static class Watermark
    {
        static readonly float KnownGoodFontSize = 101.5f;

        public static IImageProcessingContext ApplyTimestamp(IImageProcessingContext processingContext,
            Font font,
            string firstRow,
            string secondRow,
            Color color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);

            // hard-coded for now to avoid re-computing each time when we have a known range
            // of characters in use (and plenty of space to draw into the image)
            var scaledFont = new Font(font, KnownGoodFontSize);

            var firstRowCenter = new PointF(imgSize.Width, imgSize.Height);
            firstRowCenter.X -= padding;

            // offset the center by the height of the font, with some padding
            // so we can draw this line "above" the bottom-right corner
            firstRowCenter.Y -= padding + KnownGoodFontSize;

            var firstRowTextOptions = new RichTextOptions(scaledFont)
            {
                Origin = firstRowCenter,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = targetWidth
            };

            var secondRowCenter = firstRowCenter;
            // reset this number for the second row so it's back in the
            // expected bottom-right corner
            secondRowCenter.Y += KnownGoodFontSize;

            var secondRowTextOptions = new RichTextOptions(scaledFont)
            {
                Origin = secondRowCenter,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = targetWidth
            };
            return processingContext
                .DrawText(firstRowTextOptions, firstRow, color)
                .DrawText(secondRowTextOptions, secondRow, color);
        }
    }
}
