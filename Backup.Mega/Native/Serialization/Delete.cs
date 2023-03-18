namespace Ecng.Backup.Mega.Native.Serialization
{
  using Newtonsoft.Json;

  internal class DeleteRequest : RequestBase
  {
    public DeleteRequest(INode node)
      : base("d")
    {
      Node = node.Id;
    }

    [JsonProperty("n")]
    public string Node { get; private set; }
  }
}
