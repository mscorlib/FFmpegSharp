using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpegSharp.Codes;
using FFmpegSharp.Filters;
using FFmpegSharp.Media;

namespace FFmpegSharp.Executor
{
    public class Network
    {
        private readonly List<FilterBase> _filters;

        private string _source;
        private string _dest;
        private TargetType _sourceType;
        private TargetType _destType;

        public Network()
        {
            _sourceType = TargetType.Default;
            _destType = TargetType.Default;
            _filters = new List<FilterBase>();
        }

        public Network WithFilter(FilterBase filter)
        {
            _filters.Add(filter);
            return this;
        }

        public Network WithSource(string source)
        {
            _sourceType = GetTargetType(source);
            _source = source;

            return this;
        }

        public Network WithDest(string dest)
        {
            _destType = GetTargetType(dest);
            _dest = dest;

            return this;
        }

        public static Network Create()
        {
            return  new Network();
        }

        /// <summary>
        /// push a stream to rtmp server
        /// </summary>
        public void Push()
        {
            Validate();

            if (_destType != TargetType.Live)
            {
                throw new ApplicationException("source type must be 'RtmpType.Live' when Push a stream to rtmp server.");
            }

            var @params = GetParams();

            Processor.FFmpeg(@params);
        }

        /// <summary>
        /// pull a stream from rtmp server
        /// </summary>
        public void Pull()
        {
            Validate();

            if (!TestRtmpServer(_source))
                throw new ApplicationException("rtmp server sent error.");

            if (_sourceType != TargetType.Live)
            {
                throw new ApplicationException("source type must be a rtmp server.");
            }

            var @params = GetParams();

            Processor.FFmpeg(@params);
        }

        private void Validate()
        {
            if (_sourceType == TargetType.Default)
                throw new ApplicationException("source error.please input a source.");

            if (_destType == TargetType.Default)
                throw new ApplicationException("dest error.please input a dest.");

            var supportFilters = new[] { "Resize", "Segment", "X264", "AudioRate", "AudioBitrate" };

            if (_filters.Any(x => !supportFilters.Contains(x.Name)))
            {
                throw new ApplicationException(string.Format("filter not support.only support:{0}",
                    supportFilters.Aggregate(string.Empty, (current, filter) => current + (filter + ",")).TrimEnd(new[] { ',' })));
            }
        }

        private static TargetType GetTargetType(string target)
        {
            if (target.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase))
            {
                return TargetType.Live;
            }

            if (File.Exists(target))
            {
                return TargetType.File;
            }

            throw new ApplicationException("source error.unknown source.");
        }
        

        private static CodeBase GuessCode(string filePath)
        {
            var codes = new Dictionary<string, CodeBase>
            {
                {"mp3", new Mp3()},
                {"mp4", new Mp4()}, 
                {"m4a", new M4A()}, 
                {"flv", new Flv()}
            };

            var ext = Path.GetExtension(filePath);

            if (string.IsNullOrEmpty(ext))
                throw new ApplicationException(string.Format("can't guess the code from Path :'{0}'", filePath));

            var key = ext.TrimStart(new[] { '.' });

            if (codes.Keys.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                return codes[key];
            }

            throw new ApplicationException(string.Format("not support file extension :{0}", key.ToLower()));
        }

        private string GetParams()
        {
            var builder = new StringBuilder();

            if (_sourceType == TargetType.File)
            {
                builder.Append(" -re -i ");
                builder.Append(_source);
            }
            
            if(_sourceType == TargetType.Live)
            {
                builder.Append(" -i");

                builder.AppendFormat(
                    -1 < _source.IndexOf("live=1", StringComparison.OrdinalIgnoreCase) ? "\" {0} live=1\"" : "\" {0}\"",
                    _source);
            }

            if (_sourceType == TargetType.File)
            {
                if (_filters.Any(x => x.Name.Equals("Segment", StringComparison.OrdinalIgnoreCase)))
                {
                    var filter = _filters.First(x => x.Name.Equals("Segment", StringComparison.OrdinalIgnoreCase));
                    _filters.Remove(filter);
                }
            }
            

            if (!_filters.Any(x => x.Name.Equals("x264", StringComparison.OrdinalIgnoreCase)))
            {
                builder.Append(" -vcodec copy");
            }

            if (_destType == TargetType.Live)
            {
                if (!_filters.Any(x => x.Name.Equals("AudioRate", StringComparison.OrdinalIgnoreCase)))
                {
                    _filters.Add(new AudioRatelFilter(44100));
                }

                if (!_filters.Any(x => x.Name.Equals("AudioBitrate", StringComparison.OrdinalIgnoreCase)))
                {
                    _filters.Add(new AudioBitrateFilter(128));
                }
            }

            var sourcefile = new MediaStream(_source);

            foreach (var filter in _filters.OrderBy(x => x.Rank))
            {
                filter.Source = sourcefile;
                builder.Append(filter.Execute());
            }

            if (_destType == TargetType.File)
            {
                var dir = Path.GetDirectoryName(_dest);

                if (string.IsNullOrWhiteSpace(dir))
                    throw new ApplicationException("output directory error.");

                var fileName = Path.GetFileNameWithoutExtension(_dest);

                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ApplicationException("output filename is null");

                var code = GuessCode(_dest);

                if (!_filters.Any(x => x.Name.Equals("Segment", StringComparison.OrdinalIgnoreCase)))
                {
                    // out%d.mp4
                    builder.AppendFormat(" {0}{1}%d{2}", dir, fileName, code.Extension);
                }
            }

            if (_destType == TargetType.Live)
            {
                builder.Append(" -f flv ");
                builder.Append(_dest);
            }

            return builder.ToString();
        }

        private bool TestRtmpServer(string server)
        {
            var val = false;

            var @params = string.Format(
                -1 < _source.IndexOf("live=1", StringComparison.OrdinalIgnoreCase) ? "\" -i {0} live=1\"" : "\" -i {0}\"",
                server);

            //try 10 times
            var i = 0;
            do
            {
                try
                {
                    var message = Processor.FFprobe(@params,
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
                                var p = Process.GetProcessById(id);
                                    //when the process was exited will throw a excetion. 
                                p.Kill();
                            }
                            catch (Exception)
                            {
                            }

                        }));
                    
                    if (message.Equals("error", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ApplicationException("error");
                    }

                    val = true;
                    i = 10;
                }
                catch (Exception)
                {
                    i += 1;
                }

            } while (i < 10);

            return val;
        }
    }
}