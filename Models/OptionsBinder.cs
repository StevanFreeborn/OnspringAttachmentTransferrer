using System.CommandLine;
using System.CommandLine.Binding;
using Serilog.Events;

namespace OnspringAttachmentTransferrer.Models;

public class OptionsBinder : BinderBase<Options>
{
  private readonly Option<string> _configFile;
  private readonly Option<LogEventLevel> _logEventLevel; 
  private readonly Option<int> _pageSize;
  private readonly Option<int?> _pageNumberLimit;
  private readonly Option<bool> _processInParallel;

  public OptionsBinder(
    Option<string> configFile, 
    Option<LogEventLevel> logEventLevel, 
    Option<int> pageSize, 
    Option<int?> pageNumberLimit, 
    Option<bool> processInParallel
  )
  {
    _configFile = configFile;
    _logEventLevel = logEventLevel;
    _pageSize = pageSize;
    _pageNumberLimit = pageNumberLimit;
    _processInParallel = processInParallel;
  }

  protected override Options GetBoundValue(BindingContext bindingContext)
  {
    return new Options
    {
      ConfigFile = bindingContext.ParseResult.GetValueForOption(_configFile),
      LogLevel = bindingContext.ParseResult.GetValueForOption(_logEventLevel),
      PageSize = bindingContext.ParseResult.GetValueForOption(_pageSize),
      PageNumberLimit = bindingContext.ParseResult.GetValueForOption(_pageNumberLimit),
      ProcessInParallel = bindingContext.ParseResult.GetValueForOption(_processInParallel),
    };
  }
}