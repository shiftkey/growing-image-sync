using SixLabors.Fonts;

namespace Grow
{
    public static class FontLoader
    {
        public static Font? Setup()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceStream("Grow.assets.NotoSansMono-Regular.ttf");

            if (resource == null)
            {
                return null;
            }

            FontCollection collection = new();
            var family = collection.Add(resource);
            return family.CreateFont(14, FontStyle.Regular);
        }
    }
}
