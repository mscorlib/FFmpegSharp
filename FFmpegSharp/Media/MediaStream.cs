using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpegSharp.Executor;
using Newtonsoft.Json.Linq;

namespace FFmpegSharp.Media
{
    public class MediaStream
    {
        public MediaStream(string path)
        {
            if (!File.Exists(path))
            {
                throw new ApplicationException(string.Format("file not found in the path: {0} .", path));
            }

            var infostr = GetStreamInfo(path);

            var streams = JObject.Parse(infostr).SelectToken("streams", false).ToObject<List<StreamInfo>>();

            var videoStream = streams.FirstOrDefault(x => x.Type.Equals("video"));

            if (null != videoStream)
            {
                VideoInfo = new VideoInfo
                {
                    CodecName =  videoStream.CodecName,
                    Height = videoStream.Height,
                    Width = videoStream.Width,
                    Duration = videoStream.Duration
                };
            }

            var audioStream = streams.FirstOrDefault(x => x.Type.Equals("audio"));

            if (null != audioStream)
            {
                AudioInfo = new AudioInfo
                {
                    CodecName = audioStream.CodecName,
                    Channels = audioStream.Channels,
                    Duration = audioStream.Duration,
                };
            }

        }

        public VideoInfo VideoInfo { get; private set; }
        public AudioInfo AudioInfo { get; private set; }

        private string GetStreamInfo(string path)
        {
            const string paramStr = " -v quiet -print_format json -hide_banner -show_format -show_streams -pretty {0}";
            var @param = string.Format(paramStr, path);

            var message =  Processor.FFprobe(@param);

            if (message.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("there some errors on ffprobe execute");
            }

            return message;
        }
    }
}