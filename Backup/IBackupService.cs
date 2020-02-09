#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Algo
File: IBackupService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace Ecng.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;

	/// <summary>
	/// The interface describing online data storage service.
	/// </summary>
	public interface IBackupService : IDisposable
	{
		/// <summary>
		/// Is publishing feature available.
		/// </summary>
		bool CanPublish { get; }

		/// <summary>
		/// Find files by the specified criteria.
		/// </summary>
		/// <param name="parent">Parent element. Can be null.</param>
		/// <param name="criteria">Criteria.</param>
		/// <returns>File list.</returns>
		IEnumerable<BackupEntry> Find(BackupEntry parent, string criteria);

		/// <summary>
		/// List of files.
		/// </summary>
		/// <param name="parent">Parent element. Can be null.</param>
		/// <returns>File list.</returns>
		IEnumerable<BackupEntry> GetChilds(BackupEntry parent);

		/// <summary>
		/// Fill file info.
		/// </summary>
		/// <param name="entry">Element.</param>
		void FillInfo(BackupEntry entry);

		/// <summary>
		/// Delete file from the service.
		/// </summary>
		/// <param name="entry">Element.</param>
		void Delete(BackupEntry entry);

		/// <summary>
		/// Save file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		CancellationTokenSource Download(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress);

		/// <summary>
		/// Upload file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream">The stream of the open file into which data from the service will be downloaded.</param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		CancellationTokenSource Upload(BackupEntry entry, Stream stream, Action<int> progress);

		/// <summary>
		/// Get public url for the specified element.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <returns>Public url.</returns>
		string Publish(BackupEntry entry);

		/// <summary>
		/// Remove public url for the specified element.
		/// </summary>
		/// <param name="entry">Element.</param>
		void UnPublish(BackupEntry entry);
	}
}