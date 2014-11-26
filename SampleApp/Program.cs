using System;
using System.IO;
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

            var inputPath = Path.Combine(appPath, "test.mov");
            var outputPath = Path.Combine(appPath, Guid.NewGuid().ToString());

            Console.WriteLine("start encoding...");

            Encoder.Create()
                .WidthInput(inputPath)
                .WithFilter(new X264Filter { Preset = X264Preset.Faster, ConstantQuantizer = 18 })
                .WithFilter(new ResizeFilter(980, 550))
                .To<Mp4>(outputPath)
                .Execute();

            //after execute completed,will create a mp4 file in '/bin/debug'.

            Console.WriteLine("done.\npress any key to exit.");

            Console.ReadKey();
        }
    }
}
