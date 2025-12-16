namespace Ecng.Backup.Mega.Native;

using System.Text.Json.Serialization;

internal abstract class MegaRequest
{
	[JsonPropertyName("a")]
	public string Action { get; protected set; }

	// Request id echoed by server in some cases; required by some commands.
	[JsonPropertyName("i")]
	public string RequestId { get; set; }
}

internal sealed class PreLoginRequest : MegaRequest
{
	public PreLoginRequest()
	{
		Action = "us0";
	}

	[JsonPropertyName("user")]
	public string User { get; set; }
}

internal sealed class PreLoginResponse
{
	[JsonPropertyName("v")]
	public int Version { get; set; }
}

internal sealed class LoginRequest : MegaRequest
{
	public LoginRequest()
	{
		Action = "us";
	}

	[JsonPropertyName("user")]
	public string User { get; set; }

	[JsonPropertyName("uh")]
	public string PasswordHash { get; set; }
}

internal sealed class LoginResponse
{
	[JsonPropertyName("csid")]
	public string SessionId { get; set; }

	[JsonPropertyName("privk")]
	public string PrivateKey { get; set; }

	[JsonPropertyName("k")]
	public string MasterKey { get; set; }
}

internal sealed class LogoutRequest : MegaRequest
{
	public LogoutRequest()
	{
		Action = "sml";
	}
}

internal sealed class GetNodesRequest : MegaRequest
{
	public GetNodesRequest()
	{
		Action = "f";
		C = 1;
	}

	[JsonPropertyName("c")]
	public int C { get; set; }
}

internal sealed class DeleteRequest : MegaRequest
{
	public DeleteRequest()
	{
		Action = "d";
	}

	[JsonPropertyName("n")]
	public string NodeId { get; set; }
}

internal sealed class UploadUrlRequest : MegaRequest
{
	public UploadUrlRequest()
	{
		Action = "u";
	}

	[JsonPropertyName("s")]
	public long Size { get; set; }
}

internal sealed class UploadUrlResponse
{
	[JsonPropertyName("p")]
	public string Url { get; set; }
}

internal sealed class DownloadUrlRequest : MegaRequest
{
	public DownloadUrlRequest()
	{
		Action = "g";
		G = 1;
	}

	[JsonPropertyName("g")]
	public int G { get; set; }

	[JsonPropertyName("n")]
	public string Id { get; set; }

	// For public links.
	[JsonPropertyName("p")]
	public string PublicHandle { get; set; }
}

/// <summary>
/// Direct download URL response.
/// </summary>
public sealed class DownloadUrlResponse
{
	/// <summary>
	/// Direct download URL.
	/// </summary>
	[JsonPropertyName("g")]
	public string Url { get; set; }

	/// <summary>
	/// Size in bytes.
	/// </summary>
	[JsonPropertyName("s")]
	public long Size { get; set; }
}

internal sealed class CreateNodeRequest : MegaRequest
{
	public CreateNodeRequest()
	{
		Action = "p";
	}

	[JsonPropertyName("t")]
	public string ParentId { get; set; }

	[JsonPropertyName("n")]
	public CreateNodeData[] Nodes { get; set; }
}

internal sealed class CreateNodeData
{
	[JsonPropertyName("h")]
	public string CompletionHandle { get; set; }

	[JsonPropertyName("t")]
	public int Type { get; set; }

	[JsonPropertyName("a")]
	public string Attributes { get; set; }

	[JsonPropertyName("k")]
	public string Key { get; set; }
}

internal sealed class SharedKey
{
	[JsonPropertyName("h")]
	public string Id { get; set; }

	[JsonPropertyName("k")]
	public string Key { get; set; }
}

internal sealed class ExportLinkRequest : MegaRequest
{
	public ExportLinkRequest()
	{
		Action = "l";
	}

	[JsonPropertyName("n")]
	public string NodeId { get; set; }

	// d=1 disables export link
	[JsonPropertyName("d")]
	public int? Disable { get; set; }
}

internal sealed class ExportLinkResponse
{
	[JsonPropertyName("ph")]
	public string PublicHandle { get; set; }
}
