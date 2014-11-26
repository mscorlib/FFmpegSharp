using System.Drawing;
using FFmpegSharp.Utils;

namespace FFmpegSharp.Filters
{
    /// <summary>
    /// video resize filter
    /// </summary>
    public class ResizeFilter : FilterBase
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ResizeType ResizeType { get; private set; }


        public ResizeFilter(int width, int height, ResizeType type = ResizeType.Scale)
        {
            Name = "Resize";
            FilterType = FilterType.Video;
            Width = width;
            Height = height;
            ResizeType = type;

        }

        public override string ToString()
        {
            int destWidth;
            int destHeight;

            if (ResizeType == ResizeType.Scale)
            {
                var sourceSize = new Size(Source.VideoInfo.Width, Source.VideoInfo.Height);
                var destSize = SizeUtils.CalculateOutSize(sourceSize, Width, Height);
                destWidth = destSize.Width;
                destHeight = destSize.Height;
            }
            else
            {
                destWidth = Width;
                destHeight = Height;
            }

            return string.Concat(" -s ", destWidth, "x", destHeight);
        }
    }
}