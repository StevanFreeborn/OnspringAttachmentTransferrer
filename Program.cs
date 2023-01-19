using Serilog;
using Onspring.API.SDK.Models;
using OnspringAttachmentTransferrer.Services;

class Program
{
  static async Task<int> Main(string[] args)
  {
    var logPath = LogFactory.GetLogPath();
    Log.Logger = LogFactory.GetLogger(logPath);
    var context = Processor.GetContextFromFileOrUser(args[0]);

    if (context is null)
    {
      Log.Error("Unable to get context from file or user input.");
      return 1;
    }

    Log.Information("Onspring Attachment Transferrer Started");
    var onspringService = new OnspringService();
    var sourceMatchField = await onspringService.GetField(context.SourceInstanceKey, context.SourceMatchFieldId);
    var targetMatchField = await onspringService.GetField(context.TargetInstanceKey, context.TargetMatchFieldId);

    if (Processor.ValidateMatchFields(sourceMatchField, targetMatchField) is false)
    {
      Log.Error("Invalid match fields");
      return 2;
    }

    var totalPages = 1;
    var pagingRequest = new PagingRequest(1, 1);
    var currentPage = pagingRequest.PageNumber;

    do
    {
      Log.Information(
        "Fetching page {CurrentPage} of records for {SourceApp}",
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
        Log.Information(
        "Unable to fetch page {CurrentPage} of records for {SourceApp}",
        currentPage,
        context.SourceAppId
      );
        continue;
      }

      totalPages = sourceRecords.TotalPages;

      Log.Information(
        "Begin processing page {CurrentPage} of records for {SourceApp}",
        currentPage,
        context.SourceAppId
      );

      foreach(var sourceRecord in sourceRecords.Items)
      {
        Log.Information(
          "Begin processing source record {RecordId} in app {AppId}", 
          sourceRecord.RecordId, 
          sourceRecord.AppId
        );

        var matchRecordValue = Processor.GetRecordFieldValue(sourceRecord, context.SourceMatchFieldId);
        
        if (matchRecordValue is null)
        {
          Log.Information(
            "No identifier value found for source record {RecordId} in {AppId}.", 
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
          Log.Information(
            "No match record could be found in App {TargetAppId} for {RecordId} in {SourceAppId}.",
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
            Log.Information(
              "No field data found in field {SourceAttachmentFieldId} for source record {SourceRecordId} in {SourceAppId}.",
              sourceAttachmentFieldId,
              sourceRecord.RecordId,
              sourceRecord.AppId
            );
            continue;
          }

          var fileIds = Processor.GetFileIdsFromAttachmentFieldData(attachmentFieldData);

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
              Log.Information(
                "No file info could be found for file {FileId} in field {SourceAttachmentId} for source record {SourceRecordId} in {SourceAppId}.",
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
              Log.Information(
                "No file could be found for file {FileId} in field {SourceAttachmentFieldId} for source record {SourceRecordId} in {SourceAppId}.",
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
              Log.Information(
                "Source file {FileId} in {SourceAttachmentFieldId} for source record {SourceRecordId} in {SourceAppId} could not be saved into field {TargetAttachmentField} for match record {MatchRecordId} in app {TargetAppId}",
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
              "Source file {FileId} in {SourceAttachmentFieldId} for source record {SourceRecordId} in {SourceAppId} was successfully saved as file {TargetFileId} into field {TargetAttachmentField} for match record {MatchRecordId} in app {TargetAppId}",
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
          "Finished processing source record {RecordId} in app {AppId}", 
          sourceRecord.RecordId, 
          sourceRecord.AppId
        );
      }

      Log.Information(
        "Finished processing page {CurrentPage} of records for {SourceApp}",
        currentPage,
        context.SourceAppId
      );
      
      pagingRequest.PageNumber++;
      currentPage = pagingRequest.PageNumber; 
    } while (currentPage <= totalPages);

    Log.Information("Onspring Bulk Attachment Transferrer Finished");
    Log.Information("Find a completed log here: {LogPath}", logPath);
    Log.CloseAndFlush();
    Console.WriteLine("Press any key to close...");
    Console.ReadLine();

    return 0;
  }
}