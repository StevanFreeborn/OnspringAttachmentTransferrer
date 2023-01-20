using Serilog;
using Onspring.API.SDK.Models;
using OnspringAttachmentTransferrer.Services;
using System.CommandLine;
using Serilog.Events;

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

    var recordLimitOption = new Option<int?>(
      aliases: new string[] { "--recordLimit", "-r" },
      description: "Set a limit to the number of records that will be processed."
    );

    var rootCommand = new RootCommand("An app that will transfer attachments between two Onspring apps.");
    rootCommand.AddOption(fileOption);
    rootCommand.AddOption(logLevelOption);
    rootCommand.AddOption(recordLimitOption);
    rootCommand.SetHandler(async (filePath, logLevel, recordLimit) => 
    {
      await Run(filePath, logLevel, recordLimit);
    }, fileOption, logLevelOption, recordLimitOption);

    return await rootCommand.InvokeAsync(args);
  }

  static async Task<int> Run(string filePath, LogEventLevel logLevel, int? recordLimit)
  {
    var logPath = LogFactory.GetLogPath();
    Log.Logger = LogFactory.GetLogger(logPath, logLevel);
    var context = Processor.GetContextFromFileOrUser(filePath);

    if (context is null)
    {
      Log.Fatal("Unable to get context from file or user input.");
      return 1;
    }

    var onspringService = new OnspringService();
    var sourceMatchField = await onspringService.GetField(context.SourceInstanceKey, context.SourceMatchFieldId);
    var targetMatchField = await onspringService.GetField(context.TargetInstanceKey, context.TargetMatchFieldId);

    if (Processor.ValidateMatchFields(sourceMatchField, targetMatchField) is false)
    {
      Log.Fatal("Invalid match fields");
      return 2;
    }
    
    Log.Information("Onspring Attachment Transferrer Started");

    var totalPages = 1;
    var pagingRequest = new PagingRequest(1, 50);
    var currentPage = pagingRequest.PageNumber;
    var countOfProcessedRecords = 0;

    do
    {
      Log.Information(
        "Fetching Page {CurrentPage} of records for Source App {SourceApp}",
        currentPage,
        context.SourceAppId
      );

      var sourceRecords = await onspringService.GetAPageOfRecords(
        context.SourceInstanceKey, 
        context.SourceAppId, 
        context.SourceFieldIds, 
        pagingRequest
      );

      if (sourceRecords is null)
      {
        Log.Warning(
        "Unable to fetch Page {CurrentPage} of records for Source App {SourceApp}",
        currentPage,
        context.SourceAppId
      );
        continue;
      }

      totalPages = sourceRecords.TotalPages;

      Log.Information(
        "Begin processing Page {CurrentPage} of records for Source App {SourceApp}",
        currentPage,
        context.SourceAppId
      );

      foreach(var sourceRecord in sourceRecords.Items)
      {
        if (recordLimit.HasValue && countOfProcessedRecords >= recordLimit)
        {
          Log.Warning("Processed record limit of {RecordLimit} reached.", recordLimit.Value);
          goto End;
        }
        countOfProcessedRecords++;

        Log.Information(
          "Begin processing Source Record {RecordId} in Source App {AppId}", 
          sourceRecord.RecordId, 
          sourceRecord.AppId
        );

        var matchRecordValue = Processor.GetRecordFieldValue(sourceRecord, context.SourceMatchFieldId);
        
        if (matchRecordValue is null)
        {
          Log.Warning(
            "No identifier value found for Source Record {RecordId} in Source App {AppId}.", 
            sourceRecord.RecordId, 
            sourceRecord.AppId
          );
          continue;
        }

        var matchValueString = Processor.GetMatchValueAsString(matchRecordValue);
        
        var matchRecordId = await onspringService.GetMatchRecordId(
          context.TargetInstanceKey, 
          context.TargetAppId, 
          context.TargetMatchFieldId, 
          matchValueString
        );

        if (matchRecordId.HasValue is false)
        {
          Log.Warning(
            "No match record ({MatchValue}) could be found in Target App {TargetAppId} for Source Record {RecordId} in Source App {SourceAppId}.",
            matchValueString,
            context.TargetAppId,
            sourceRecord.RecordId,
            sourceRecord.AppId
          );
          continue;
        }

        foreach(var sourceAttachmentFieldId in context.SourceAttachmentFieldIds)
        {
          var attachmentFieldData = Processor.GetRecordFieldValue(sourceRecord, sourceAttachmentFieldId);

          if (attachmentFieldData is null)
          {
            Log.Warning(
              "No field data found in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
              sourceAttachmentFieldId,
              sourceRecord.RecordId,
              sourceRecord.AppId
            );
            continue;
          }

          var fileIds = Processor.GetFileIdsForInternalFilesFromAttachmentFieldData(attachmentFieldData);

          foreach(var fileId in fileIds)
          {
            var sourceFileInfo = await onspringService.GetFileInfo(
              context.SourceInstanceKey, 
              sourceRecord.RecordId, 
              sourceAttachmentFieldId,  
              fileId
            );
            
            if (sourceFileInfo is null)
            {
              Log.Warning(
                "No file info could be found for File {FileId} in Source Attachment Field {SourceAttachmentId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
                fileId,
                sourceAttachmentFieldId,
                sourceRecord.RecordId,
                sourceRecord.AppId
              );
              continue;
            }

            var sourceFile = await onspringService.GetFile(
              context.SourceInstanceKey, 
              sourceRecord.RecordId, 
              sourceAttachmentFieldId,  
              fileId
            );

            if (sourceFile is null)
            {
              Log.Warning(
                "No file could be found for File {FileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
                fileId,
                sourceAttachmentFieldId,
                sourceRecord.RecordId,
                sourceRecord.AppId
              );
              continue;
            }

            var targetAttachmentField = Processor.GetTargetAttachmentField(
              context.AttachmentFieldMappings, 
              sourceAttachmentFieldId
            );

            var saveFileResponse = await onspringService.AddSourceFileToMatchRecord(
              context.TargetInstanceKey, 
              matchRecordId, 
              targetAttachmentField, 
              sourceFileInfo, 
              sourceFile
            );

            if (saveFileResponse is null)
            {
              Log.Warning(
                "Source File {FileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId} could not be saved into Target Attachment Field {TargetAttachmentField} for Match Record {MatchRecordId} in Target App {TargetAppId}",
                fileId,
                sourceAttachmentFieldId,
                sourceRecord.RecordId,
                sourceRecord.AppId,
                targetAttachmentField,
                matchRecordId,
                context.TargetAppId
              );
              
              continue;
            }

            Log.Information(
              "Source File {FileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId} was successfully saved as File {TargetFileId} into Target Attachment Field {TargetAttachmentField} for Match Record {MatchRecordId} in Target App {TargetAppId}",
              fileId,
              sourceAttachmentFieldId,
              sourceRecord.RecordId,
              sourceRecord.AppId,
              saveFileResponse.Id,
              targetAttachmentField,
              matchRecordId,
              context.TargetAppId
            );
          }
        }

        Log.Information(
          "Finished processing Source Record {RecordId} in Source App {AppId}", 
          sourceRecord.RecordId, 
          sourceRecord.AppId
        );
      }

      Log.Information(
        "Finished processing Page {CurrentPage} of records for Source App {SourceApp}",
        currentPage,
        context.SourceAppId
      );
      
      pagingRequest.PageNumber++;
      currentPage = pagingRequest.PageNumber; 
    } while (currentPage <= totalPages);

    End:

    Log.Information("Onspring Attachment Transferrer Finished");
    Log.Information("Find a log of the completed run here: {LogPath}", logPath);
    Log.CloseAndFlush();
    Console.WriteLine("Press any key to close...");
    Console.ReadLine();

    return 0;
  }
}