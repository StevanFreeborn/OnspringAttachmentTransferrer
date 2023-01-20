using Serilog;
using Onspring.API.SDK.Models;
using System.CommandLine;
using Serilog.Events;
using System.Diagnostics;

class Program
{
  static async Task<int> Main(string[] args)
  {
    var fileOption = new Option<string>(
      aliases: new string[] { "--config", "-c" },
      description: "The path to the file that specifies configuration for the transferrer."
    );

    var logLevelOption = new Option<LogEventLevel>(
      aliases: new string[] { "--log", "-l" },
      description: "Set the minimum level of event that will be logged to the console.",
      getDefaultValue: () => LogEventLevel.Information
    );

    var pageSizeOption = new Option<int>(
      aliases: new string[] { "--pageSize", "-ps" },
      description: "Set the size of each page of records processed.",
      getDefaultValue: () => 50
    );

    var pageNumberOption = new Option<int?>(
      aliases: new string[] { "--pageNumber", "-pn" },
      description: "Set a limit to the number of pages of records processed."
    );

    var parallelOption = new Option<bool>(
      aliases: new string[] { "--parallel", "-p" },
      description: "Process each record in parallel.",
      getDefaultValue: () => false
    );

    var rootCommand = new RootCommand("An app that will transfer attachments between two Onspring apps.");
    rootCommand.AddOption(fileOption);
    rootCommand.AddOption(logLevelOption);
    rootCommand.AddOption(pageSizeOption);
    rootCommand.AddOption(pageNumberOption);
    rootCommand.AddOption(parallelOption);
    rootCommand.SetHandler(async (filePath, logLevel, pageSize, pageNumber, isParallel) => 
    {
      await Run(filePath, logLevel, pageSize, pageNumber, isParallel);
    }, fileOption, logLevelOption, pageSizeOption, pageNumberOption, parallelOption);

    return await rootCommand.InvokeAsync(args);
  }

  static async Task<int> Run(string filePath, LogEventLevel logLevel, int pageSize, int? pageNumberLimit, bool isParallel)
  {
    var logPath = LogFactory.GetLogPath();
    Log.Logger = LogFactory.CreateLogger(logPath, logLevel);
    var context = Processor.GetContextFromFileOrUser(filePath);

    if (context is null)
    {
      Log.Fatal("Unable to get context from file or user input.");
      return 1;
    }

    var processor = new Processor(context);

    if (await processor.ValidateMatchFields() is false)
    {
      Log.Fatal("Invalid match fields.");
      return 2;
    }
    
    Log.Information("Onspring Attachment Transferrer Started");

    var stopWatch = new Stopwatch();
    var totalPages = 1;
    var pagingRequest = new PagingRequest(1, pageSize);
    var currentPage = pagingRequest.PageNumber;

    do
    {
      Log.Information(
        "Fetching Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );

      var sourceRecords = await processor.GetAPageOfRecords(pagingRequest);

      if (sourceRecords is null)
      {
        Log.Warning(
        "Unable to fetch Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );
        continue;
      }

      totalPages = sourceRecords.TotalPages;

      Log.Information(
        "Begin processing Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );

      stopWatch.Start();

      if (isParallel is true)
      {
        await Parallel.ForEachAsync(sourceRecords.Items, async (sourceRecord, token) => 
        {
          await processor.TransferSourceRecordFilesToMatchingTargetRecord(sourceRecord, isParallel);
        });
      }
      else
      {
        foreach (var sourceRecord in sourceRecords.Items)
        {
          await processor.TransferSourceRecordFilesToMatchingTargetRecord(sourceRecord, isParallel);
        }
      }

      stopWatch.Stop();

      Log.Information(
        "Finished processing Page {CurrentPage} of records for Source App {SourceApp}. ({ExecutionTime} seconds)",
        currentPage,
        context.SourceAppId,
        Math.Round(stopWatch.Elapsed.TotalSeconds, 0)
      );
      
      pagingRequest.PageNumber++;
      currentPage = pagingRequest.PageNumber;
    } while (currentPage <= totalPages && (pageNumberLimit.HasValue is false || currentPage < pageNumberLimit.Value));

    Log.Information("Onspring Attachment Transferrer Finished");
    Log.Information("Find a log of the completed run here: {LogPath}", logPath);
    Log.CloseAndFlush();
    
    Console.WriteLine("Press any key to exit.");
    Console.ReadLine();

    return 0;
  }
}