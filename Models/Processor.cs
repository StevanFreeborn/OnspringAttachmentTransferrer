using Microsoft.Extensions.Configuration;
using Onspring.API.SDK.Enums;
using Onspring.API.SDK.Models;
using OnspringAttachmentTransferrer.Helpers;
using OnspringAttachmentTransferrer.Models;
using OnspringAttachmentTransferrer.Services;
using Serilog;

public class Processor
{
  private readonly Context _context;
  private readonly OnspringService _onspringService;

  public Processor(Context context)
  {
    _context = context;
    _onspringService = new OnspringService();
  }

  public async Task<GetPagedRecordsResponse> GetAPageOfRecordsToBeProcessed(PagingRequest pagingRequest)
  {
    return await _onspringService.GetAPageOfRecordsToBeProcessed(_context, pagingRequest);
  }

  public async Task TransferSourceRecordFilesToMatchingTargetRecord(ResultRecord sourceRecord, bool isParallel)
  {
    Log.Information(
          "Begin processing Source Record {SourceRecordId} in Source App {SourceAppId}.",
          sourceRecord.RecordId,
          sourceRecord.AppId
        );

    var matchRecordValue = GetRecordFieldValue(sourceRecord, _context.SourceMatchFieldId);

    if (matchRecordValue is null)
    {
      Log.Warning(
        "No identifier value found for Source Record {SourceRecordId} in Source App {SourceAppId}.",
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
      return;
    }

    var matchValueString = GetMatchValueAsString(matchRecordValue);

    var matchRecordId = await _onspringService.GetMatchRecordId(_context, matchValueString);

    if (matchRecordId.HasValue is false)
    {
      Log.Warning(
        "No match record ({MatchValue}) could be found in Target App {TargetAppId} for Source Record {RecordId} in Source App {SourceAppId}.",
        matchValueString,
        _context.TargetAppId,
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
      return;
    }

    if (isParallel is true)
    {
      await Parallel.ForEachAsync(_context.SourceAttachmentFieldIds, async (sourceAttachmentFieldId, token) =>
      {
        await ProcessSourceAttachmentFieldId(sourceRecord, sourceAttachmentFieldId, matchRecordId, isParallel);
      });
    }
    else
    {
      foreach (var sourceAttachmentFieldId in _context.SourceAttachmentFieldIds)
      {
        await ProcessSourceAttachmentFieldId(sourceRecord, sourceAttachmentFieldId, matchRecordId, isParallel);
      }
    }

    var updateResponse = await _onspringService.UpdateSourceRecordAsProcessed(_context, sourceRecord);

    if (updateResponse is null)
    {
      Log.Warning(
        "Failed to update Source Record {SourceRecordId} in Source App {SourceAppId} as processed.",
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
    }

    Log.Information(
      "Finished processing Source Record {SourceRecordId} in Source App {SourceAppId}.",
      sourceRecord.RecordId,
      sourceRecord.AppId
    );
  }

  public async Task<Boolean> ValidateMatchFields()
  {
    var sourceMatchField = await _onspringService.GetField(_context.SourceInstanceKey, _context.SourceMatchFieldId);
    var targetMatchField = await _onspringService.GetField(_context.TargetInstanceKey, _context.TargetMatchFieldId);

    if (
      sourceMatchField is null ||
      Context.IsValidMatchFieldType(sourceMatchField) is false ||
      targetMatchField is null ||
      Context.IsValidMatchFieldType(targetMatchField) is false
    )
    {
      return false;
    }

    return true;
  }

  public static Context GetContextFromFileOrUser(string filePath)
  {
    if (filePath is not null)
    {

      var configuration = new ConfigurationBuilder()
      .AddJsonFile(filePath, optional: true, reloadOnChange: true)
      .Build();

      if (Context.TryParseConfigToContext(configuration, out var contextFromConfig) is false)
      {
        Log.Error("Configuration is not valid. Please correct and try again.");
        return null;
      }

      return contextFromConfig;
    }

    var sourceInstanceKey = Prompt.GetSourceApiKey();
    var targetInstanceKey = Prompt.GetTargetApiKey();
    var sourceAppId = Prompt.GetSourceAppId();
    var targetAppId = Prompt.GetTargetAppId();
    var sourceMatchField = Prompt.GetSourceMatchFieldId();
    var targetMatchField = Prompt.GetTargetMatchFieldId();
    var attachmentFieldMappings = Prompt.GetAttachmentFieldMappings();
    var flagFieldId = Prompt.GetFlagFieldId();
    var processValue = Prompt.GetProcessValue();
    var processedValue = Prompt.GetProcessedValue();

    return new Context(
      sourceInstanceKey,
      targetInstanceKey,
      sourceAppId,
      targetAppId,
      sourceMatchField,
      targetMatchField,
      attachmentFieldMappings,
      flagFieldId,
      processValue,
      processedValue
    );
  }

  private async Task ProcessSourceAttachmentFieldId(ResultRecord sourceRecord, int sourceAttachmentFieldId, int? matchRecordId, bool isParallel)
  {
    var attachmentFieldData = GetRecordFieldValue(sourceRecord, sourceAttachmentFieldId);

      if (attachmentFieldData is null)
      {
        Log.Warning(
          "No field data found in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
          sourceAttachmentFieldId,
          sourceRecord.RecordId,
          sourceRecord.AppId
        );
        return;
      }

      var fileIds = GetFileIdsForInternalFilesFromAttachmentFieldData(attachmentFieldData);

      if (isParallel is true)
      {
        await Parallel.ForEachAsync(fileIds, async (fileId, toke) =>
        {
          await ProcessSourceFileId(sourceRecord, sourceAttachmentFieldId, matchRecordId, fileId);
        });
      }
      else
      {
        foreach (var fileId in fileIds)
        {
          await ProcessSourceFileId(sourceRecord, sourceAttachmentFieldId, matchRecordId, fileId);
        }
      }
  }

  private async Task ProcessSourceFileId(ResultRecord sourceRecord, int sourceAttachmentFieldId, int? matchRecordId, int fileId)
  {
    var sourceFileInfo = await _onspringService.GetFileInfo(
      _context.SourceInstanceKey,
      sourceRecord.RecordId,
      sourceAttachmentFieldId,
      fileId
    );

    if (sourceFileInfo is null)
    {
      Log.Warning(
        "No file info could be found for File {SourceFileId} in Source Attachment Field {SourceAttachmentId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
        fileId,
        sourceAttachmentFieldId,
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
      return;
    }

    var sourceFile = await _onspringService.GetFile(
      _context.SourceInstanceKey,
      sourceRecord.RecordId,
      sourceAttachmentFieldId,
      fileId
    );

    if (sourceFile is null)
    {
      Log.Warning(
        "No file could be found for File {SourceFileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId}.",
        fileId,
        sourceAttachmentFieldId,
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
      return;
    }

    var targetAttachmentField = GetTargetAttachmentField(
      _context.AttachmentFieldMappings,
      sourceAttachmentFieldId
    );

    var saveFileResponse = await _onspringService.AddSourceFileToMatchRecord(
      _context.TargetInstanceKey,
      matchRecordId,
      targetAttachmentField,
      sourceFileInfo,
      sourceFile
    );

    if (saveFileResponse is null)
    {
      Log.Warning(
        "Source File {SourceFileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId} could not be saved into Target Attachment Field {TargetAttachmentField} for Match Record {MatchRecordId} in Target App {TargetAppId}.",
        fileId,
        sourceAttachmentFieldId,
        sourceRecord.RecordId,
        sourceRecord.AppId,
        targetAttachmentField,
        matchRecordId,
        _context.TargetAppId
      );
      return;
    }

    Log.Information(
      "Source File {SourceFileId} in Source Attachment Field {SourceAttachmentFieldId} for Source Record {SourceRecordId} in Source App {SourceAppId} was successfully saved as File {TargetFileId} into Target Attachment Field {TargetAttachmentField} for Match Record {MatchRecordId} in Target App {TargetAppId}.",
      fileId,
      sourceAttachmentFieldId,
      sourceRecord.RecordId,
      sourceRecord.AppId,
      saveFileResponse.Id,
      targetAttachmentField,
      matchRecordId,
      _context.TargetAppId
    );
  }

  private int GetTargetAttachmentField(Dictionary<int, int> mappings, int sourceAttachmentFieldId)
  {
    return mappings.GetValueOrDefault(sourceAttachmentFieldId);
  }

  private List<int> GetFileIdsForInternalFilesFromAttachmentFieldData(RecordFieldValue attachmentFieldData)
  {
    switch (attachmentFieldData.Type)
    {
      case ResultValueType.FileList:
        return attachmentFieldData.AsFileList();
      case ResultValueType.AttachmentList:
      default:
        return attachmentFieldData
        .AsAttachmentList()
        .Where(attachment => attachment.StorageLocation is FileStorageSite.Internal)
        .Select(attachment => attachment.FileId)
        .ToList();
    }
  }

  private string GetMatchValueAsString(RecordFieldValue value)
  {
    switch (value.Type)
    {
      case ResultValueType.String:
        return value.AsString();
      case ResultValueType.Integer:
        return $"{value.AsNullableInteger()}";
      case ResultValueType.Decimal:
        return $"{value.AsNullableDecimal()}";
      case ResultValueType.Date:
        return $"{value.AsNullableDateTime()}";
      case ResultValueType.TimeSpan:
        var data = value.AsTimeSpanData();
        return $"Quantity: {data.Quantity}, Increment: {data.Increment}, Recurrence: {data.Recurrence}, EndByDate: {data.EndByDate}, EndAfterOccurrences: {data.EndAfterOccurrences}";
      case ResultValueType.Guid:
        return $"{value.AsNullableGuid()}";
      case ResultValueType.StringList:
        return string.Join(", ", value.AsStringList());
      case ResultValueType.IntegerList:
        return string.Join(", ", value.AsIntegerList());
      case ResultValueType.GuidList:
        return string.Join(", ", value.AsGuidList());
      case ResultValueType.AttachmentList:
        var attachmentFiles = value.AsAttachmentList().Select(f => $"FileId: {f.FileId}, FileName: {f.FileName}, Notes: {f.Notes}");
        return string.Join(", ", attachmentFiles);
      case ResultValueType.ScoringGroupList:
        var scoringGroups = value.AsScoringGroupList().Select(g => $"ListValueId: {g.ListValueId}, Name: {g.Name}, Score: {g.Score}, MaximumScore: {g.MaximumScore}");
        return string.Join(", ", scoringGroups);
      default:
        return $"Unsupported ResultValueType: {value.Type}";
    }
  }

  private RecordFieldValue GetRecordFieldValue(ResultRecord record, int fieldId)
  {
    return record
    .FieldData
    .FirstOrDefault(fd => fd.FieldId == fieldId);
  }

  public async Task<bool> ValidateFlagFieldIdAndValues()
  {
    var flagField = await _onspringService.GetField(_context.SourceInstanceKey, _context.FlagFieldId);

    if (flagField is null || flagField.Type is not FieldType.List)
    {
      return false;
    }

    var listField = flagField as ListField;

    if (listField.Multiplicity is Multiplicity.MultiSelect)
    {
      return false;
    }

    var processListValue = listField
    .Values
    .FirstOrDefault(
      value => value.Name == _context.ProcessValue || 
      (Guid.TryParse(_context.ProcessValue, out var result) && value.Id == result)
    );

    var processedListValue = listField
    .Values
    .FirstOrDefault(
      value => value.Name == _context.ProcssedValue || 
      (Guid.TryParse(_context.ProcssedValue, out var result) && value.Id == result)
    );

    if (processListValue is null || processedListValue is null)
    {
      return false;
    }

    _context.FlagField = listField;
    _context.ProcessValueId = processListValue.Id;
    _context.ProcessedValueId = processedListValue.Id;
    return true;
  }
}