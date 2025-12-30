namespace Ecng.IO.Compression;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Disposable enumerable of ZIP entries. Disposing this object releases the underlying ZipArchive.
/// The consumer must ensure all returned entry streams are no longer needed before disposing this wrapper.
/// </summary>
public class ZipEntries : Disposable, IEnumerable<(string name, Stream body)>
{
	private readonly ZipArchive _zip;
	private readonly IEnumerable<ZipArchiveEntry> _entries;

	internal ZipEntries(ZipArchive zip, Func<string, bool> filter)
	{
		if (filter is null)
			throw new ArgumentNullException(nameof(filter));

		_zip = zip ?? throw new ArgumentNullException(nameof(zip));
		_entries = zip.Entries.Where(e => filter(e.Name));
	}

	IEnumerator<(string, Stream)> IEnumerable<(string name, Stream body)>.GetEnumerator()
	{
		foreach (var entry in _entries)
			yield return (entry.FullName, entry.Open());
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		=> ((IEnumerable<(string, Stream)>)this).GetEnumerator();

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		_zip.Dispose();
	}
}
