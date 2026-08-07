namespace Ecng.Markdown;

/// <summary>
/// A node of the rendered document.
/// </summary>
/// <remarks>
/// The tree sits between the Markdig document and a UI toolkit. It exists so the decisions that are the same
/// for every host - what a resolved diagram reference means, whether a role-gated fragment is visible, which
/// address a video plays from - are made once and can be tested without a UI, leaving each host with nothing
/// but the mapping from a node to its own controls.
/// </remarks>
public abstract class MdNode
{
}
