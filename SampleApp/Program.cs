using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FFmpegSharp.Codes;
using FFmpegSharp.Executor;
using FFmpegSharp.Filters;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDir =
                new FileInfo(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath));
            var appPath = currentDir.DirectoryName;

            if (string.IsNullOrWhiteSpace(appPath))
                throw new ApplicationException("app path not found.");

            var inputPath = Path.Combine(appPath, "bbt.mkv");
            var outputPath = Path.Combine(appPath, Guid.NewGuid().ToString());

            Console.WriteLine("start...");

            //Encoder.Create()
            //    .WidthInput(inputPath)
            //    .WithFilter(new X264Filter { Preset = X264Preset.Faster, ConstantQuantizer = 18 })
            //    .WithFilter(new ResizeFilter(980, 550))
            //    .To<Mp4>(outputPath)
            //    .Execute();

            Network.Create()
                .WithSource(inputPath)
                .WithDest("rtmp://192.168.10.12/live/stream")
                .WithFilter(new X264Filter{ConstantQuantizer = 20})
                .WithFilter(new ResizeFilter(980,500))
                .Push();

            Console.WriteLine("done.\npress any key to exit.");

            Console.ReadKey();
        }
    }
}
