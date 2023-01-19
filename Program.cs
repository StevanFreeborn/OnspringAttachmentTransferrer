using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using OnspringAttachmentTransferrer.Helpers;

class Program
{
  static int Main(string[] args)
  {
    var outputDirectory = FileHelper.GetOutputDirectory();
    var logPath = Path.Combine(outputDirectory, "log.json");

    Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(new RenderedCompactJsonFormatter(), logPath)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

    var context = PromptHelper.GetContextFromFileOrUser(args[0]);

    if (context is null)
    {
      return 1;
    }

    Log.Information("Onspring Attachment Transferrer Started");

    Log.Information("Onspring Bulk Attachment Transferrer Finished");
    Log.CloseAndFlush();
    Console.WriteLine("Presss any key to close...");
    Console.ReadLine();

    return 0;
  }
}