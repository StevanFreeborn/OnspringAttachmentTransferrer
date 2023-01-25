using Onspring.API.SDK;
using Onspring.API.SDK.Models;
using OnspringAttachmentTransferrer.Models;
using Serilog;

namespace OnspringAttachmentTransferrer.Services;

public class OnspringService
{
  private readonly string baseUrl = "https://api.onspring.com/";

  public async Task<GetPagedRecordsResponse> GetAPageOfRecordsToBeProcessed(Context context, PagingRequest pagingRequest)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, context.SourceInstanceKey);
      
      var request = new QueryRecordsRequest
      {
        AppId = context.SourceAppId,
        FieldIds = context.SourceFieldIds,
        Filter = $"{context.FlagField.Id} contains '{context.ProcessValueId}'",
      };

      var response = await onspringClient.QueryRecordsAsync(request, pagingRequest);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully retrieved {CountOfRecords} record(s) for Source App {SourceAppId}. (page {PageNumber} of {TotalPages})",
        response.Value.Items.Count,
        context.SourceAppId,
        response.Value.PageNumber,
        response.Value.TotalPages
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve records for Source App {SourceAppId}. ({Message})",
        context.SourceAppId,
        message
      );
    }

    return null;
  }

  public async Task<Field> GetField(string apiKey, int fieldId)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var response = await onspringClient.GetFieldAsync(fieldId);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully retrieved field {FieldId} for App {AppId}.",
        response.Value.Id,
        response.Value.AppId
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve field {FieldId}. ({Message})",
        fieldId,
        message
      );
    }

    return null;
  }

  public async Task<int?> GetMatchRecordId(Context context, string filterValue)
  {
    try
    {

      var onspringClient = new OnspringClient(baseUrl, context.TargetInstanceKey);
      var request = new QueryRecordsRequest
      {
        AppId = context.TargetAppId,
        FieldIds = new List<int> { context.TargetMatchFieldId },
        Filter = $"{context.TargetMatchFieldId} eq '{filterValue}'",
      };

      var response = await onspringClient.QueryRecordsAsync(request);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      if (response.Value.Items.Count is not 1)
      {
        return null;
      }

      var matchRecord = response.Value.Items[0];

      Log.Debug(
        "Successfully retrieved Match Record {TargetRecordId} in Target App {TargetAppId}.",
        matchRecord.RecordId,
        matchRecord.AppId
      );
      return matchRecord.RecordId;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve records to match against from Target App {TargetAppId}. ({Message})",
        context.TargetAppId,
        message
      );
    }

    return null;
  }

  public async Task<GetFileInfoResponse> GetFileInfo(string apiKey, int recordId, int fieldId, int fileId)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var response = await onspringClient.GetFileInfoAsync(recordId, fieldId, fileId);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully retrieved File Info for File {FileId} in Field {FieldId} for Source Record {SourceRecordId}.",
        fileId,
        fieldId,
        recordId
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve file info for File {FileId} in Field {FieldId} for Source Record {SourceRecordId}. ({Message})",
        fileId,
        fieldId,
        recordId,
        message
      );
    }

    return null;
  }

  public async Task<GetFileResponse> GetFile(string apiKey, int recordId, int fieldId, int fileId)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var response = await onspringClient.GetFileAsync(recordId, fieldId, fileId);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully retrieved file for File {FileId} in Field {FieldId} for Source Record {SourceRecordId}.",
        fileId,
        fieldId,
        recordId
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve file for File {FileId} in Field {FieldId} for Source Record {SourceRecordId}. ({Message})",
        fileId,
        fieldId,
        recordId,
        message
      );
    }

    return null;
  }

  public async Task<CreatedWithIdResponse<int>> AddSourceFileToMatchRecord(string apiKey, int? recordId, int fieldId, GetFileInfoResponse fileInfo, GetFileResponse file)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var request = new SaveFileRequest
      {
        RecordId = recordId.Value,
        FieldId = fieldId,
        FileName = fileInfo.Name,
        ContentType = file.ContentType,
        FileStream = file.Stream,
        Notes = fileInfo.Notes ?? "",
      };

      var response = await onspringClient.SaveFileAsync(request);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully saved file named {FileName} with id {FileId} in Field {FieldId} for Match Record {TargetRecordId}.",
        fileInfo.Name,
        response.Value.Id,
        fieldId,
        recordId
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to save file named {FileName} in Field {FieldId} on Match Record {TargetRecordId}. ({Message})",
        fileInfo.Name,
        fieldId,
        recordId,
        message
      );
    }

    return null;
  }

  public async Task<SaveRecordResponse> UpdateSourceRecordAsProcessed(Context context, ResultRecord sourceRecord)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, context.SourceInstanceKey);

      var request = new ResultRecord
      {
        AppId = context.SourceAppId,
        RecordId = sourceRecord.RecordId,
      };

      var flagFieldValue = new GuidFieldValue(context.FlagField.Id, context.ProcessedValueId);
      request.FieldData.Add(flagFieldValue);

      var response = await onspringClient.SaveRecordAsync(request);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully updated Source Record {SourceRecordId} in Source App {SourceAppId} as having been processed.",
        sourceRecord.RecordId,
        context.SourceAppId
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Warning(
        "Failed to update Source Record {SourceRecordId} in Source App {SourceAppId} as processed. ({Message})",
        sourceRecord.RecordId,
        sourceRecord.AppId
      );
    }

    return null;
  }
}