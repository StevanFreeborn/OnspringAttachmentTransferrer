using Onspring.API.SDK;
using Onspring.API.SDK.Models;
using Serilog;

namespace OnspringAttachmentTransferrer.Services;

public class OnspringService
{
  private readonly string baseUrl = "https://api.onspring.com/";

  public async Task<GetPagedRecordsResponse> GetAPageOfRecords(string apiKey, int appId, List<int> fieldIds, PagingRequest pagingRequest)
  {
    try
    {
      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var request = new GetRecordsByAppRequest
      {
        AppId = appId,
        FieldIds = fieldIds,
        PagingRequest = pagingRequest,
      };

      var response = await onspringClient.GetRecordsForAppAsync(request);

      if (response.IsSuccessful is false)
      {
        throw new ApplicationException($"Status Code: {response.StatusCode} - {response.Message}");
      }

      Log.Debug(
        "Successfully retrieved {CountOfRecords} record(s) for App {AppId}. (page {PageNumber} of {TotalPages})",
        response.Value.Items.Count,
        appId,
        response.Value.PageNumber,
        response.Value.TotalPages
      );
      return response.Value;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve records for App {AppId}. ({Message})",
        appId,
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

  public async Task<int?> GetMatchRecordId(string apiKey, int appId, int fieldId, string filterValue)
  {
    try
    {

      var onspringClient = new OnspringClient(baseUrl, apiKey);
      var request = new QueryRecordsRequest
      {
        AppId = appId,
        FieldIds = new List<int> { fieldId },
        Filter = $"{fieldId} eq '{filterValue}'",
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
        "Successfully retrieved match record {MatchRecordId} in App {MatchAppId}.",
        matchRecord.RecordId,
        matchRecord.AppId
      );
      return matchRecord.RecordId;
    }
    catch (Exception e)
    {
      var message = e.Message;
      Log.Error(
        "Failed to retrieve records to match against from App {AppId}. ({Message})",
        appId,
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
        "Successfully retrieved file info for file {FileId} in field {FieldId} for source record {RecordId}.",
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
        "Failed to retrieve file info for file {FileId} in field {FieldId} for source record {RecordId}. ({Message})",
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
        "Successfully retrieved file for file {FileId} in field {FieldId} for source record {RecordId}.",
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
        "Failed to retrieve file for file {FileId} in field {FieldId} for source record {RecordId}. ({Message})",
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
        "Successfully saved file named {FileName} with id {FileId} in field {FieldId} for match record {RecordId}.",
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
        "Failed to save file named {FileName} in field {FieldId} on match record {RecordId}. ({Message})",
        fileInfo.Name,
        fieldId,
        recordId,
        message
      );
    }

    return null;
  }
}