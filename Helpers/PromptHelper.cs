using Serilog;
using OnspringAttachmentTransferrer.Models;
using Microsoft.Extensions.Configuration;

namespace OnspringAttachmentTransferrer.Helpers;

public static class PromptHelper
{
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

    var sourceInstanceKey = GetSourceApiKey();
    var targetInstanceKey = GetTargetApiKey();
    var sourceAppId = GetSourceAppId();
    var targetAppId = GetTargetAppId();
    var sourceMatchField = GetSourceMatchFieldId();
    var targetMatchField = GetTargetMatchFieldId();
    var attachmentFieldMappings = GetAttachmentFieldMappings();

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

  private static string GetSourceApiKey()
  {
    string apiKey = null;

    while (Context.IsValidKey(apiKey) is true)
    {
      Console.Write("Please enter an api key for your source instance: ");
      apiKey = Console.ReadLine();
    }

    return apiKey;
  }

  private static string GetTargetApiKey()
  {
    string apiKey = null;

    while (Context.IsValidKey(apiKey) is false)
    {
      Console.Write("Please enter an api key for your target instance: ");
      apiKey = Console.ReadLine();
    }

    return apiKey;
  }

  private static int GetSourceAppId()
  {
    var appId = 0;

    while (appId <= 0)
    {
      Console.Write("Please enter the id for your source app: ");
      var appIdInput = Console.ReadLine();

      if (Context.IsValidId(appIdInput, out int parsedId) is false)
      {
        continue;
      }

      appId = parsedId;
    }

    return appId;
  }

  private static int GetTargetAppId()
  {
    var appId = 0;

    while (appId <= 0)
    {
      Console.Write("Please enter the id for your target app: ");
      var appIdInput = Console.ReadLine();

      if (Context.IsValidId(appIdInput, out int parsedId) is false)
      {
        continue;
      }

      appId = parsedId;
    }

    return appId;
  }

  private static int GetSourceMatchFieldId()
  {
    var fieldId = 0;

    while (fieldId <= 0)
    {
      Console.Write("Please enter the id for the field in the source whose value you want to match records on: ");
      var fieldIdInput = Console.ReadLine();

      if (Context.IsValidId(fieldIdInput, out int parsedId) is false)
      {
        continue;
      }

      fieldId = parsedId;
    }

    return fieldId;
  }

  private static int GetTargetMatchFieldId()
  {
    var fieldId = 0;

    while (fieldId <= 0)
    {
      Console.Write("Please enter the id for the field in the target whose value you want to match records on: ");
      var fieldIdInput = Console.ReadLine();

      if (Context.IsValidId(fieldIdInput, out int parsedId) is false)
      {
        continue;
      }

      fieldId = parsedId;
    }

    return fieldId;
  }

  private static Dictionary<int, int> GetAttachmentFieldMappings()
  {
    Dictionary<int, int> fieldMappings = null;

    while (fieldMappings.Count < 1)
    {
      Console.Write("Please enter your attachment field id mappings (i.e. 0001|1000,0002|2000): ");
      var fieldMappingInput = Console.ReadLine();

      if (Context.TryParseMappings(fieldMappingInput, out var mappings) is true)
      {
        fieldMappings = mappings;
      }
    }

    return fieldMappings;
  }
}