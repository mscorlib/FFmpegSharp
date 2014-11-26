using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            //try 10 times
            var i = 0;
            do
            {
                try
                {
                    var infostr = GetStreamInfo(path);
                    LoadInfo(infostr);

                    i = 10;
                }
                catch (Exception)
                {
                    i += 1;
                }
                
            } while (i < 10);
            
            FilePath = path;

        }

        public VideoInfo VideoInfo { get; private set; }
        public AudioInfo AudioInfo { get; private set; }
        public string FilePath { get; set; }

        private static string GetStreamInfo(string path)
        {
            const string paramStr = " -v quiet -print_format json -hide_banner -show_format -show_streams -pretty {0}";
            var @param = string.Format(paramStr, path);

            var message = Processor.FFprobe(@param,
                id => Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    /*
                     * if rtmp is alive but no current stream output, 
                     * FFmpeg will  be in a wait state forerver.
                     * so after 1s, kill the process.
                     */

                    try
                    {
                        var p = Process.GetProcessById(id);//when the process was exited will throw a excetion. 
                        p.Kill();
                    }
                    catch (Exception)
                    {
                    }

                }));

            if (message.Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException("there some errors on ffprobe execute");
            }

            return message;
        }

        private void LoadInfo(string infostr)
        {
            var streams = JObject.Parse(infostr).SelectToken("streams", false).ToObject<List<StreamInfo>>();
            var mediaInfo = JObject.Parse(infostr).SelectToken("format").ToObject<StreamInfo>();

            if (null == streams)
            {
                throw new ApplicationException("no stream found in the source.");
            }

            var videoStream = streams.FirstOrDefault(x => x.Type.Equals("video"));

            if (null != videoStream)
            {
                VideoInfo = new VideoInfo
                {
                    CodecName = videoStream.CodecName,
                    Height = videoStream.Height,
                    Width = videoStream.Width,
                    Duration = videoStream.Duration
                };

                if (VideoInfo.Duration.Ticks < 1)
                {
                    VideoInfo.Duration = mediaInfo.Duration;
                }
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

                if (AudioInfo.Duration.Ticks < 1)
                {
                    AudioInfo.Duration = mediaInfo.Duration;
                }
            }

        }
    }
}