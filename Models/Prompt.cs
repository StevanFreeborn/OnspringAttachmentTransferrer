using OnspringAttachmentTransferrer.Models;

namespace OnspringAttachmentTransferrer.Helpers;

public static class Prompt
{
  public static string GetSourceApiKey()
  {
    string apiKey = null;

    while (Context.IsValidKey(apiKey) is false)
    {
      Console.WriteLine("Please enter an api key for your source instance:");
      apiKey = Console.ReadLine();
    }

    return apiKey;
  }

  public static string GetTargetApiKey()
  {
    string apiKey = null;

    while (Context.IsValidKey(apiKey) is false)
    {
      Console.WriteLine("Please enter an api key for your target instance:");
      apiKey = Console.ReadLine();
    }

    return apiKey;
  }

  public static int GetSourceAppId()
  {
    var appId = 0;

    while (appId <= 0)
    {
      Console.WriteLine("Please enter the id for your source app:");
      var appIdInput = Console.ReadLine();

      if (Context.IsValidId(appIdInput, out int parsedId) is false)
      {
        continue;
      }

      appId = parsedId;
    }

    return appId;
  }

  public static int GetTargetAppId()
  {
    var appId = 0;

    while (appId <= 0)
    {
      Console.WriteLine("Please enter the id for your target app:");
      var appIdInput = Console.ReadLine();

      if (Context.IsValidId(appIdInput, out int parsedId) is false)
      {
        continue;
      }

      appId = parsedId;
    }

    return appId;
  }

  public static int GetSourceMatchFieldId()
  {
    var fieldId = 0;

    while (fieldId <= 0)
    {
      Console.WriteLine("Please enter the id for the field in the source whose value you want to match records on:");
      var fieldIdInput = Console.ReadLine();

      if (Context.IsValidId(fieldIdInput, out int parsedId) is false)
      {
        continue;
      }

      fieldId = parsedId;
    }

    return fieldId;
  }

  public static int GetTargetMatchFieldId()
  {
    var fieldId = 0;

    while (fieldId <= 0)
    {
      Console.Write("Please enter the id for the field in the target whose value you want to match records on:");
      var fieldIdInput = Console.ReadLine();

      if (Context.IsValidId(fieldIdInput, out int parsedId) is false)
      {
        continue;
      }

      fieldId = parsedId;
    }

    return fieldId;
  }

  public static Dictionary<int, int> GetAttachmentFieldMappings()
  {
    Dictionary<int, int> fieldMappings = null;

    while (fieldMappings.Count < 1)
    {
      Console.WriteLine("Please enter your attachment field id mappings (i.e. 0001|1000,0002|2000):");
      var fieldMappingInput = Console.ReadLine();

      if (Context.TryParseMappings(fieldMappingInput, out var mappings) is true)
      {
        fieldMappings = mappings;
      }
    }

    return fieldMappings;
  }
}