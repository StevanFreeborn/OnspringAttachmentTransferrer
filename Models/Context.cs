using Microsoft.Extensions.Configuration;
using Onspring.API.SDK.Enums;
using Onspring.API.SDK.Models;
using Serilog;

namespace OnspringAttachmentTransferrer.Models;

public class Context
{
  public Context(string sourceInstanceKey, string targetInstanceKey, int sourceAppId, int targetAppId, int sourceMatchField, int targetMatchField, Dictionary<int, int> attachmentFieldMappings)
  {
    SourceInstanceKey = sourceInstanceKey;
    TargetInstanceKey = targetInstanceKey;
    SourceAppId = sourceAppId;
    TargetAppId = targetAppId;
    SourceMatchField = sourceMatchField;
    TargetMatchField = targetMatchField;
    AttachmentFieldMappings = attachmentFieldMappings;
  }

  public string SourceInstanceKey { get; set; }
  public string TargetInstanceKey { get; set; }
  public int SourceAppId { get; set; }
  public int TargetAppId { get; set; }
  public int SourceMatchField { get; set; }
  public int TargetMatchField { get; set; }
  public Dictionary<int,int> AttachmentFieldMappings { get; set; }
  public List<int> SourceAttachmentFieldIds => GetSourceAttachmentFields();
  public List<int> SourceFieldIds => GetSourceFieldIds();
  public List<int> TargetFieldIds => GetTargetFieldIds();

  private List<int> GetSourceAttachmentFields()
  {
    return AttachmentFieldMappings.Keys.ToList();
  }

  private List<int> GetTargetFieldIds()
  {
    var targetFieldIds = AttachmentFieldMappings.Values.ToList();
    targetFieldIds.Add(TargetMatchField);
    return targetFieldIds;
  }

  private List<int> GetSourceFieldIds()
  {
    var sourceFieldIds = AttachmentFieldMappings.Keys.ToList();
    sourceFieldIds.Add(SourceMatchField);
    return sourceFieldIds;
  }

  public static Boolean IsValidMatchFieldType(Field field)
  {
    var isSupportedField = field.Type is 
    FieldType.Text or 
    FieldType.AutoNumber or 
    FieldType.Date or 
    FieldType.Number or 
    FieldType.Formula;

    if (isSupportedField is false)
    {
      return false;
    }

    if (field.Type is FieldType.Formula)
    {
      var formulaField = field as FormulaField;
      return formulaField.OutputType is not FormulaOutputType.List;
    }

    return true;
  }

  public static Boolean TryParseConfigToContext(IConfigurationRoot configuration, out Context context)
  {
    var sourceInstanceKey = configuration.GetSection("SourceInstanceKey").Value;
    var targetInstanceKey = configuration.GetSection("TargetInstanceKey").Value;
    var sourceAppId = configuration.GetSection("SourceAppId").Value;
    var targetAppId = configuration.GetSection("TargetAppId").Value;
    var sourceMatchField = configuration.GetSection("SourceMatchField").Value;
    var targetMatchField = configuration.GetSection("TargetMatchField").Value;
    var attachmentFieldMappings = configuration.GetSection("AttachmentFieldMappings").Value;

    if (
      IsValidKey(sourceInstanceKey) is false ||
      IsValidKey(targetInstanceKey) is false ||
      IsValidId(sourceAppId, out var parsedSourceAppId) is false ||
      IsValidId(targetAppId, out var parsedTargetAppId) is false ||
      IsValidId(sourceMatchField, out var parsedSourceMatchFieldId) is false ||
      IsValidId(targetMatchField, out var parsedTargetMatchFieldId) is false ||
      TryParseMappings(attachmentFieldMappings, out var fieldMappings) is false
    )
    {
      context = null;
      return false;
    }

    context = new Context(
      sourceInstanceKey, 
      targetInstanceKey, 
      parsedSourceAppId, 
      parsedTargetAppId, 
      parsedSourceMatchFieldId, 
      parsedTargetMatchFieldId, 
      fieldMappings
    );
    return true;
  }

  public static Boolean IsValidKey(string key)
  {
    return String.IsNullOrWhiteSpace(key) is false;
  }

  public static Boolean IsValidId(string id, out int parsedId)
  {
    if (String.IsNullOrWhiteSpace(id) is true)
    {
      parsedId = 0;
      return false;
    }

    if (int.TryParse(id, out int result) is false)
    {
      Log.Error("{Id} is an invalid id.", id);
      parsedId = 0;
      return false;
    }

    parsedId = result;
    return true;
  }

  public static Boolean TryParseMappings(string mappings, out Dictionary<int,int> fieldMappings)
  {
    fieldMappings = new Dictionary<int, int>();

    if (AreValidFieldMappings(mappings) is false)
    {
      return false;
    }

    var fieldPairs = mappings.Split(",", StringSplitOptions.TrimEntries).ToList();

    foreach (var pair in fieldPairs)
    {
      var ids = pair
      .Split("|", StringSplitOptions.TrimEntries)
      .Select(id => int.Parse(id))
      .ToList();

      var sourceId = ids[0];
      var targetId = ids[1];

      fieldMappings.Add(sourceId, targetId);
    }

    return true;
  }

  private static Boolean AreValidFieldMappings(string mappings)
  {
    if (String.IsNullOrWhiteSpace(mappings) is true)
    {
      return false;
    }

    var fieldMappingsList = mappings
    .Split(",", StringSplitOptions.TrimEntries)
    .ToList();

    foreach(var fieldMapping in fieldMappingsList)
    {
      var fieldPair = fieldMapping.Split("|").ToList();

      if (fieldPair.Count != 2)
      {
        Log.Error("{FieldMapping} is an invalid field mapping.", fieldMapping);
        return false;
      }

      foreach (var fieldId in fieldPair)
      {
        if (int.TryParse(fieldId, out var result) is false)
        {
          Log.Error("{FieldMapping} is an invalid field mapping.", fieldMapping);
          return false;
        }
      }
    }

    return true;
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
}