FFmpegSharp is a fluent api encapsulation of ffmpeg with C#

======

sample:

======

var currentDir =
new FileInfo(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath));


var appPath = currentDir.DirectoryName;

if (string.IsNullOrWhiteSpace(appPath))throw new ApplicationException("app path not found.");


var inputPath = Path.Combine(appPath, "test.mov");
var outputPath = Path.Combine(appPath, Guid.NewGuid().ToString());

Encoder.Create()
.WidthInput(inputPath)
.WithFilter(new X264Filter {Preset = X264Preset.Faster, ConstantQuantizer = 18})
.To<Mp4>(outputPath)
.Execute();



======
if you want build this project,
please donwload ffmpeg lib first.

for x32 build with:
http://ffmpeg.zeranoe.com/builds/win32/shared/ffmpeg-20141117-git-3f07dd6-win32-shared.7z

for x64 build withd:
http://ffmpeg.zeranoe.com/builds/win64/shared/ffmpeg-20141117-git-3f07dd6-win64-shared.7z

after extract the files, copy the contents of the 'bin' folder to the path '/external/ffmpeg/x32(or x64)/'