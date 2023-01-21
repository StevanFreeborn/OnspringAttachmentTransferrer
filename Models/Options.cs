using Serilog.Events;

namespace OnspringAttachmentTransferrer.Models;

public class Options
{
  public string ConfigFile { get; set; }
  public LogEventLevel LogLevel { get; set; }
  public int PageSize { get; set; }
  public int? PageNumberLimit { get; set; }
  public bool ProcessInParallel { get; set; }
}