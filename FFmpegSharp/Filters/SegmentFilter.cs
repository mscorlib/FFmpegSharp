using System;
using FFmpegSharp.Media;

namespace FFmpegSharp.Filters
{
    public class SegmentFilter : FilterBase
    {
        public int SegmentTime { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segmentTime"></param>
        public SegmentFilter(int segmentTime)
        {
            Name = "Segment";
            FilterType = FilterType.Video;
            Rank = 5;
            SegmentTime = segmentTime;
        }

        public override string ToString()
        {
            return string.Format(" -f segment -segment_time {0} -reset_timestamps 1", SegmentTime);
        }
    }
}