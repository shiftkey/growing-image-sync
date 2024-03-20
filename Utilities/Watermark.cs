using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Grow
{
    public static class Watermark
    {
        //
        // Apply some text to the bottom-right of the image
        // 
        // The firstRow parameter is there to shift the text up (so it will render above the second row)
        //
        // Based upon https://github.com/SixLabors/Samples/blob/main/ImageSharp/DrawWaterMarkOnImage/Program.cs#L28
        public static IImageProcessingContext ApplyToImage(IImageProcessingContext processingContext,
            Font font,
            string text,
            Color color,
            float padding,
            bool firstRow = false)
        {
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            float targetMinHeight = imgSize.Height - (padding * 3); // Must be with in a margin width of the target height

            // Now we are working in 2 dimensions at once and can't just scale because it will cause the text to
            // reflow we need to just try multiple times
            var scaledFont = font;
            FontRectangle s = new(0, 0, float.MaxValue, float.MaxValue);

            float scaleFactor = (scaledFont.Size / 2); // Every time we change direction we half this size
            int trapCount = (int)scaledFont.Size * 2;
            if (trapCount < 10)
            {
                trapCount = 10;
            }

            bool isTooSmall = false;

            while ((s.Height > targetHeight || s.Height < targetMinHeight) && trapCount > 0)
            {
                if (s.Height > targetHeight)
                {
                    if (isTooSmall)
                    {
                        scaleFactor /= 2;
                    }

                    scaledFont = new Font(scaledFont, scaledFont.Size - scaleFactor);
                    isTooSmall = false;
                }

                if (s.Height < targetMinHeight)
                {
                    if (!isTooSmall)
                    {
                        scaleFactor /= 2;
                    }
                    scaledFont = new Font(scaledFont, scaledFont.Size + scaleFactor);
                    isTooSmall = true;
                }
                trapCount--;

                s = TextMeasurer.MeasureSize(text, new TextOptions(scaledFont)
                {
                    WrappingLength = targetWidth
                });

            }

            var center = new PointF(imgSize.Width, imgSize.Height);

            center.X -= padding;
            center.Y -= padding;

            if (firstRow)
            {
                center.Y -= 100;
            }

            var textOptions = new RichTextOptions(scaledFont)
            {
                Origin = center,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = targetWidth
            };
            return processingContext.DrawText(textOptions, text, color);
        }
    }
}
