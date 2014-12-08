using System;
using System.Diagnostics;
using System.IO;

namespace FFmpegSharp.Executor
{
    internal class Processor
    {
        internal static string FFmpeg(string @params, Action<int> onStart = null)
        {
            return Execute(true, @params, onStart);
        }

        internal static string FFprobe(string @params, Action<int> onStart = null)
        {
            return Execute(false, @params, onStart);
        }

        private static string Execute(bool userFFmpeg, string @params,Action<int> onStart = null)
        {
            Process p = null;
            try
            {
                using (p = new Process())
                {
                    var workdir = Path.GetDirectoryName(Config.Instance.FFmpegPath);

                    if (string.IsNullOrWhiteSpace(workdir))
                        throw new ApplicationException("work directory is null");

                    var exePath = userFFmpeg ? Config.Instance.FFmpegPath : Config.Instance.FFprobePath;

                    var info = new ProcessStartInfo(exePath)
                    {
                        Arguments = @params,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WorkingDirectory = workdir
                    };

                    p.StartInfo = info;
                    p.Start();

                    if (null != onStart)
                    {
                        onStart.Invoke(p.Id);
                    }

                    var message = string.Empty;

                    if (userFFmpeg)
                    {
                        while (!p.StandardError.EndOfStream)
                        {
                            message =p.StandardError.ReadLine();
                        }
                    }
                    else
                    {
                        message = p.StandardOutput.ReadToEnd();
                    }

                    p.WaitForExit();

                    return message;
                }
            }
            finally
            {
                if (null != p)
                {
                    p.Close();
                    p.Dispose();
                }
            }

        }
    }
}
