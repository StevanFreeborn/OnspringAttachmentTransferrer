using OnspringAttachmentTransferrer.Models;

namespace OnspringAttachmentTransferrer.Helpers;

public static class Prompt
{
  public static string GetSourceApiKey()
  {
    return AskForValue("Please enter an api key for your source instance:");
  }

  public static string GetTargetApiKey()
  {
    return AskForValue("Please enter an api key for your target instance:");
  }

  public static int GetSourceAppId()
  {
    return AskForId("Please enter the id for your source app:");
  }

  public static int GetTargetAppId()
  {
    return AskForId("Please enter the id for your target app:");
  }

  public static int GetSourceMatchFieldId()
  {
    return AskForId("Please enter the id for the field in the source whose value you want to match records on:");
  }

  public static int GetTargetMatchFieldId()
  {
    return AskForId("Please enter the id for the field in the target whose value you want to match records on:");
  }

  public static Dictionary<int, int> GetAttachmentFieldMappings()
  {
    Dictionary<int, int> fieldMappings = new Dictionary<int, int>();

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

  public static int GetFlagFieldId()
  {
    return AskForId("Please enter the id for the field in the source whose value you want to use to determine which records to process:");
  }

  public static string GetProcessValue()
  {
    return AskForValue("Please enter the value that your source records should have in their flag field to be processed:");
  }

  public static string GetProcessedValue()
  {
    return AskForValue("Please enter the value that your source records should be updated with to indicate they have been processed:");
  }

  private static int AskForId(string message)
  {
    var id = 0;

    while (id <= 0)
    {
      Console.WriteLine(message);
      var idInput = Console.ReadLine();

      if (Context.IsValidId(idInput, out int parsedId) is false)
      {
        continue;
      }

      id = parsedId;
    }

    return id;
  }

  private static string AskForValue(string message)
  {
    string value = null;

    while (Context.IsNotNullOrWhiteSpace(value) is false)
    {
      Console.WriteLine(message);
      value = Console.ReadLine();
    }

    return value;
  }
}