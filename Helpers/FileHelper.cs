namespace OnspringAttachmentTransferrer.Helpers;

public static class FileHelper
{
  public static string GetOutputDirectory()
  {
    var currentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
    var outputDirectory = $"{DateTime.Now.ToString("yyyyMMddHHmm")}-output";
    return Path.Combine(currentDirectory, outputDirectory); ;
  }
}