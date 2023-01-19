using Onspring.API.SDK;

namespace OnspringAttachmentTransferrer.Services;

public class OnspringService
{
  private readonly string baseUrl = "https://api.onspring.com/";
  public readonly OnspringClient _client;

  public OnspringService(string apiKey)
  {
    _client = new OnspringClient(baseUrl, apiKey);
  }
}