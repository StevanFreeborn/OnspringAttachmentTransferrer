using Serilog;
using Onspring.API.SDK.Models;
using System.CommandLine;
using Serilog.Events;
using System.Diagnostics;
using OnspringAttachmentTransferrer.Models;

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
    
    var optionsBinder = new OptionsBinder(
      fileOption,
      logLevelOption,
      pageSizeOption,
      pageNumberOption,
      parallelOption
    );

    rootCommand.SetHandler(async (options) => 
    {
      await Run(options);
    }, optionsBinder);

    return await rootCommand.InvokeAsync(args);
  }

  static async Task<int> Run(Options options)
  {
    var logPath = LogFactory.GetLogPath();
    Log.Logger = LogFactory.CreateLogger(logPath, options.LogLevel);
    var context = Processor.GetContextFromFileOrUser(options.ConfigFile);

    if (context is null)
    {
      Log.Fatal("Unable to get context from file or user input.");
      return 1;
    }

    var processor = new Processor(context);

    if (await processor.ValidateMatchFields() is false)
    {
      Log.Fatal("Invalid match fields. Match fields should be of type text, date, number, auto number, or a formula with a non list output type.");
      return 2;
    }

    if (await processor.ValidateFlagFieldIdAndValues() is false)
    {
      Log.Fatal("Invalid flag field and/or values. The flag field should be a single select list field and should contain both your process and processed values.");
      return 3;
    }
    
    Log.Information("Onspring Attachment Transferrer Started");

    var stopWatch = new Stopwatch();
    var totalPages = 1;
    var pagingRequest = new PagingRequest(1, options.PageSize);
    var currentPage = pagingRequest.PageNumber;
    var maxRetries = 3;
    var retries = 0;

    do
    {
      Log.Information(
        "Fetching Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );

      var sourceRecords = await processor.GetAPageOfRecordsToBeProcessed(pagingRequest);

      if (sourceRecords is null)
      {
        Log.Warning(
        "Unable to fetch Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );
        retries++;
        continue;
      }

      if (sourceRecords.Items.Count == 0)
      {
        Log.Warning(
          "No records retrieved for Page {CurrentPage} of records for Source App {SourceApp}.",
          currentPage,
          context.SourceAppId
        );
      }

      totalPages = sourceRecords.TotalPages;

      Log.Information(
        "Begin processing Page {CurrentPage} of records for Source App {SourceApp}.",
        currentPage,
        context.SourceAppId
      );

      stopWatch.Start();

      if (options.ProcessInParallel is true)
      {
        await Parallel.ForEachAsync(sourceRecords.Items, async (sourceRecord, token) => 
        {
          await processor.TransferSourceRecordFilesToMatchingTargetRecord(sourceRecord, options.ProcessInParallel);
        });
      }
      else
      {
        foreach (var sourceRecord in sourceRecords.Items)
        {
          await processor.TransferSourceRecordFilesToMatchingTargetRecord(sourceRecord, options.ProcessInParallel);
        }
      }

      stopWatch.Stop();

      Log.Information(
        "Finished processing Page {CurrentPage} of records for Source App {SourceApp}. (in {ExecutionTime} seconds)",
        currentPage,
        context.SourceAppId,
        Math.Round(stopWatch.Elapsed.TotalSeconds, 0)
      );
      
      pagingRequest.PageNumber++;
      currentPage = pagingRequest.PageNumber;
      retries = 0;
    } while (
      currentPage <= totalPages && 
      (options.PageNumberLimit.HasValue is false || currentPage <= options.PageNumberLimit.Value) &&
      retries < maxRetries
    );

    Log.Information("Onspring Attachment Transferrer Finished");
    Log.Information("Find a log of the completed run here: {LogPath}", logPath);
    Log.CloseAndFlush();
    
    Console.WriteLine("Press any key to exit.");
    Console.ReadLine();

    return 0;
  }
}