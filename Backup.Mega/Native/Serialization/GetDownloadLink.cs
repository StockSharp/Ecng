namespace Ecng.Backup.Mega.Native.Serialization
{
  using Newtonsoft.Json;

  internal class GetDownloadLinkRequest : RequestBase
  {
    public GetDownloadLinkRequest(INode node)
      : base("l")
    {
      Id = node.Id;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }
  }
}
