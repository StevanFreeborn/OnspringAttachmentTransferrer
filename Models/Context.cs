using Microsoft.Extensions.Configuration;
using Onspring.API.SDK.Enums;
using Onspring.API.SDK.Models;
using Serilog;

namespace OnspringAttachmentTransferrer.Models;

public class Context
{
  public Context(
    string sourceInstanceKey, 
    string targetInstanceKey, 
    int sourceAppId, 
    int targetAppId, 
    int sourceMatchField, 
    int targetMatchField, 
    Dictionary<int, int> attachmentFieldMappings,
    int flagFieldId,
    string processValue,
    string processedValue
  )
  {
    SourceInstanceKey = sourceInstanceKey;
    TargetInstanceKey = targetInstanceKey;
    SourceAppId = sourceAppId;
    TargetAppId = targetAppId;
    SourceMatchFieldId = sourceMatchField;
    TargetMatchFieldId = targetMatchField;
    AttachmentFieldMappings = attachmentFieldMappings;
    FlagFieldId = flagFieldId;
    ProcessValue = processValue;
    ProcssedValue = processedValue;
  }

  public string SourceInstanceKey { get; private set; }
  public string TargetInstanceKey { get; private set; }
  public int SourceAppId { get; private set; }
  public int TargetAppId { get; private set; }
  public int SourceMatchFieldId { get; private set; }
  public int TargetMatchFieldId { get; private set; }
  public Dictionary<int,int> AttachmentFieldMappings { get; private set; }
  public int FlagFieldId { get; private set; }
  public ListField FlagField { get; set; }
  public string ProcessValue { get; private set; }
  public Guid ProcessValueId { get; set; }
  public string ProcssedValue { get; private set; }
  public Guid ProcessedValueId { get; set; }
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
    targetFieldIds.Add(TargetMatchFieldId);
    return targetFieldIds;
  }

  private List<int> GetSourceFieldIds()
  {
    var sourceFieldIds = AttachmentFieldMappings.Keys.ToList();
    sourceFieldIds.Add(SourceMatchFieldId);
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
    var flagFieldId = configuration.GetSection("FlagFieldId").Value;
    var processValue = configuration.GetSection("ProcessValue").Value;
    var processedValue = configuration.GetSection("ProcessedValue").Value;

    if (
      IsNotNullOrWhiteSpace(sourceInstanceKey) is false ||
      IsNotNullOrWhiteSpace(targetInstanceKey) is false ||
      IsValidId(sourceAppId, out var parsedSourceAppId) is false ||
      IsValidId(targetAppId, out var parsedTargetAppId) is false ||
      IsValidId(sourceMatchField, out var parsedSourceMatchFieldId) is false ||
      IsValidId(targetMatchField, out var parsedTargetMatchFieldId) is false ||
      TryParseMappings(attachmentFieldMappings, out var fieldMappings) is false ||
      IsValidId(flagFieldId, out var parsedFlagFieldId) is false ||
      IsNotNullOrWhiteSpace(processValue) is false ||
      IsNotNullOrWhiteSpace(processedValue) is false
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
      fieldMappings,
      parsedFlagFieldId,
      processValue,
      processedValue
    );
    return true;
  }

  public static Boolean IsNotNullOrWhiteSpace(string value)
  {
    return String.IsNullOrWhiteSpace(value) is false;
  }

  public static Boolean IsValidId(string id, out int parsedId)
  {
    if (String.IsNullOrWhiteSpace(id) is true)
    {
      Log.Error("Id cannot be null or whitespace.");
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
      Log.Error("{Mappings} contains an invalid field mapping.", mappings);
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
        return false;
      }

      foreach (var fieldId in fieldPair)
      {
        if (int.TryParse(fieldId, out var result) is false)
        {
          return false;
        }
      }
    }

    return true;
  }
}