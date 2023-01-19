using Microsoft.Extensions.Configuration;
using Onspring.API.SDK.Enums;
using Onspring.API.SDK.Models;
using OnspringAttachmentTransferrer.Helpers;
using OnspringAttachmentTransferrer.Models;
using Serilog;

public static class Processor
{
  public static int GetTargetAttachmentField(Dictionary<int, int> mappings, int sourceAttachmentFieldId)
  {
    return mappings.GetValueOrDefault(sourceAttachmentFieldId);
  }

  public static List<int> GetFileIdsFromAttachmentFieldData(RecordFieldValue attachmentFieldData)
  {
    switch (attachmentFieldData.Type)
    {
      case ResultValueType.FileList:
        return attachmentFieldData.AsFileList();
      case ResultValueType.AttachmentList:
      default:
        return attachmentFieldData
        .AsAttachmentList()
        .Select(attachment => attachment.FileId)
        .ToList();
    }
  }

  public static string GetMatchValueAsString(RecordFieldValue value)
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

  public static RecordFieldValue GetRecordFieldValue(ResultRecord record, int fieldId)
  {
    return record
    .FieldData
    .FirstOrDefault(fd => fd.FieldId == fieldId);
  }
  public static Boolean ValidateMatchFields(Field sourceMatchField, Field targetMatchField)
  {
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

    var sourceInstanceKey = PromptHelper.GetSourceApiKey();
    var targetInstanceKey = PromptHelper.GetTargetApiKey();
    var sourceAppId = PromptHelper.GetSourceAppId();
    var targetAppId = PromptHelper.GetTargetAppId();
    var sourceMatchField = PromptHelper.GetSourceMatchFieldId();
    var targetMatchField = PromptHelper.GetTargetMatchFieldId();
    var attachmentFieldMappings = PromptHelper.GetAttachmentFieldMappings();

    return new Context(
      sourceInstanceKey,
      targetInstanceKey,
      sourceAppId,
      targetAppId,
      sourceMatchField,
      targetMatchField,
      attachmentFieldMappings
    );
  }
}