using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FFmpegSharp.Codes;
using FFmpegSharp.Filters;
using FFmpegSharp.Media;

namespace FFmpegSharp.Executor
{
    public class Encoder : IExecutor
    {
        private readonly List<FilterBase> _filters;
        

        private string _inputPath;
        private string _outputPath;
        private MediaStream _source;
        private CodeBase _code;


        private Encoder()
        {
            _filters = new List<FilterBase>();
        }

        public Encoder WidthInput(string filePath)
        {
            _inputPath = filePath;
            return this;
        }

        public Encoder WithFilter(FilterBase filter)
        {
            _filters.Add(filter);
            return this;
        }

        public Encoder To<T>(string ouputPath) where T : CodeBase, new()
        {
            _outputPath = ouputPath;
            _code = new T();
            return this;
        }

        public static Encoder Create()
        {
            return new Encoder();
        }

        public void Execute()
        {
            var @params = BuildParams();

            Processor.FFmpeg(@params);
        }

        private string BuildParams()
        {
            Validate();

            _source = new MediaStream(_inputPath);

            var outdir = Path.GetDirectoryName(_outputPath);

            if (!string.IsNullOrWhiteSpace(outdir) && !Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }

            var builder = new StringBuilder();

            builder.Append(" -i");
            builder.Append(" ");
            builder.Append(_inputPath);

            foreach (var filter in _filters.OrderBy(x=>x.Rank))
            {
                filter.Source = _source;
                builder.Append(filter.Execute());
            }

            var dir = Path.GetDirectoryName(_outputPath);

            if(string.IsNullOrWhiteSpace(dir))
                throw new ApplicationException("output directory error.");

            var fileName = Path.GetFileNameWithoutExtension(_outputPath);

            if(string.IsNullOrWhiteSpace(fileName))
                throw new ApplicationException("output filename is null");

            builder.AppendFormat(" {0}\\{1}{2}", dir, fileName, _code.Extension);

            return builder.ToString();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_inputPath))
            {
                throw new ApplicationException("input file is null.");
            }

            if (string.IsNullOrWhiteSpace(_outputPath))
            {
                throw new ApplicationException("outout path is null");
            }
        }
    }
}