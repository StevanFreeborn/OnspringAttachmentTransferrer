using Microsoft.Extensions.Configuration;
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
    AttachmentFieldMappings = attachmentFieldMappings;
  }

  public string SourceInstanceKey { get; set; }
  public string TargetInstanceKey { get; set; }
  public int SourceAppId { get; set; }
  public int TargetAppId { get; set; }
  public int SourceMatchField { get; set; }
  public int TargetMatchField { get; set; }
  public Dictionary<int,int> AttachmentFieldMappings { get; set; }

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
      Log.Error($"{id} is an invalid id.");
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
        Log.Error($"{fieldMapping} is an invalid field mapping.");
        return false;
      }

      foreach (var fieldId in fieldPair)
      {
        if (int.TryParse(fieldId, out var result) is false)
        {
          Log.Error($"{fieldMapping} is an invalid field mapping.");
          return false;
        }
      }
    }

    return true;
  }
}