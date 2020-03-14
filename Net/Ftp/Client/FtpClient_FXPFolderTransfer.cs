﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentFTP.Rules;
#if (CORE || NETFX)
using System.Threading;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {


		/// <summary>
		/// Transfer the specified directory from the source FTP Server onto the remote FTP Server using the FXP protocol.
		/// You will need to create a valid connection to your remote FTP Server before calling this method.
		/// In Update mode, we will only transfer missing files and preserve any extra files on the remote FTP Server. This is useful when you want to simply transfer missing files from an FTP directory.
		/// Currently Mirror mode is not implemented.
		/// Only transfers the files and folders matching all the rules provided, if any.
		/// All exceptions during transfer are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="sourceFolder">The full or relative path to the folder on the source FTP Server. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="remoteFolder">The full or relative path to destination folder on the remote FTP Server</param>
		/// <param name="mode">Only Update mode is currently implemented</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public List<FtpResult> TransferDirectory(string sourceFolder, FtpClient remoteClient, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

			if (sourceFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "sourceFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(TransferDirectory), new object[] { sourceFolder, remoteClient, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// cleanup the FTP paths
			sourceFolder = sourceFolder.GetFtpPath().EnsurePostfix("/");
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the source dir does not exist, fail fast
			if (!DirectoryExists(sourceFolder)) {
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!remoteClient.DirectoryExists(remoteFolder)) {
				remoteClient.CreateDirectory(remoteFolder);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = GetListing(sourceFolder, FtpListOption.Recursive).Where(x => x.Type == FtpFileSystemObjectType.Directory).Select(x => x.FullName).ToArray();

			// get all the already existing files
			var remoteListing = checkFileExistence ? remoteClient.GetListing(remoteFolder, FtpListOption.Recursive) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToTransfer(sourceFolder, remoteFolder, rules, results, dirListing);
			CreateSubDirectories(remoteClient, dirsToUpload);

			// get all the files in the local directory
			var fileListing = GetListing(sourceFolder, FtpListOption.Recursive).Where(x => x.Type == FtpFileSystemObjectType.File).Select(x => x.FullName).ToArray();

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToTransfer(sourceFolder, remoteFolder, rules, results, shouldExist, fileListing);
			TransferServerFiles(filesToUpload, remoteClient, existsMode, verifyOptions, progress, remoteListing);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			// DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}

#if ASYNC

		/// <summary>
		/// Transfer the specified directory from the source FTP Server onto the remote FTP Server asynchronously using the FXP protocol.
		/// You will need to create a valid connection to your remote FTP Server before calling this method.
		/// In Update mode, we will only transfer missing files and preserve any extra files on the remote FTP Server. This is useful when you want to simply transfer missing files from an FTP directory.
		/// Currently Mirror mode is not implemented.
		/// Only transfers the files and folders matching all the rules provided, if any.
		/// All exceptions during transfer are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="sourceFolder">The full or relative path to the folder on the source FTP Server. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="remoteFolder">The full or relative path to destination folder on the remote FTP Server</param>
		/// <param name="mode">Only Update mode is currently implemented</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> TransferDirectoryAsync(string sourceFolder, FtpClient remoteClient, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (sourceFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "sourceFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(TransferDirectoryAsync), new object[] { sourceFolder, remoteClient, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// cleanup the FTP paths
			sourceFolder = sourceFolder.GetFtpPath().EnsurePostfix("/");
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the source dir does not exist, fail fast
			if (!await DirectoryExistsAsync(sourceFolder, token)) {
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!await remoteClient.DirectoryExistsAsync(remoteFolder, token)) {
				await remoteClient.CreateDirectoryAsync(remoteFolder, token);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = (await GetListingAsync(sourceFolder, FtpListOption.Recursive, token)).Where(x => x.Type == FtpFileSystemObjectType.Directory).Select(x => x.FullName).ToArray();

			// get all the already existing files
			var remoteListing = checkFileExistence ? await remoteClient.GetListingAsync(remoteFolder, FtpListOption.Recursive, token) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToTransfer(sourceFolder, remoteFolder, rules, results, dirListing);
			await CreateSubDirectoriesAsync(remoteClient, dirsToUpload, token);

			// get all the files in the local directory
			var fileListing = (await GetListingAsync(sourceFolder, FtpListOption.Recursive, token)).Where(x => x.Type == FtpFileSystemObjectType.File).Select(x => x.FullName).ToArray();

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToTransfer(sourceFolder, remoteFolder, rules, results, shouldExist, fileListing);
			await TransferServerFilesAsync(filesToUpload, remoteClient, existsMode, verifyOptions, progress, remoteListing, token);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			// DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}
#endif

		private List<FtpResult> GetSubDirectoriesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, string[] dirListing) {

			var dirsToTransfer = new List<FtpResult>();

			foreach (var sourceFile in dirListing) {

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "").EnsurePostfix("/");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult {
					Type = FtpFileSystemObjectType.Directory,
					Size = 0,
					Name = sourceFile.GetFtpDirectoryName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile,
					IsDownload = false,
				};

				// record the folder
				results.Add(result);

				// skip transfering the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				dirsToTransfer.Add(result);
			}

			return dirsToTransfer;
		}

		private List<FtpResult> GetFilesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing) {

			var filesToTransfer = new List<FtpResult>();

			foreach (var sourceFile in fileListing) {

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult {
					Type = FtpFileSystemObjectType.File,
					Size = GetFileSize(sourceFile),
					Name = sourceFile.GetFtpFileName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile
				};

				// record the file
				results.Add(result);

				// skip transfering the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				// record that this file should exist
				shouldExist.Add(remoteFile.ToLowerInvariant(), true);

				// absorb errors
				filesToTransfer.Add(result);
			}

			return filesToTransfer;
		}

		private void TransferServerFiles(List<FtpResult> filesToTransfer, FtpClient remoteClient, FtpRemoteExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpListItem[] remoteListing) {

			LogFunc(nameof(TransferServerFiles), new object[] { filesToTransfer.Count + " files" });

			int r = -1;
			foreach (var result in filesToTransfer) {
				r++;

				// absorb errors
				try {

					// skip uploading if the file already exists on the server
					FtpRemoteExists existsModeToUse;
					if (!CanUploadFile(result, remoteListing, existsMode, out existsModeToUse)) {
						continue;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToTransfer.Count, r);

					// transfer the file
					var transferred = TransferFile(result.LocalPath, remoteClient, result.RemotePath, false, existsModeToUse, verifyOptions, progress, metaProgress);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogStatus(FtpTraceLevel.Warn, "File failed to transfer: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

#if ASYNC

		private async Task TransferServerFilesAsync(List<FtpResult> filesToTransfer, FtpClient remoteClient, FtpRemoteExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, FtpListItem[] remoteListing, CancellationToken token) {

			LogFunc(nameof(TransferServerFilesAsync), new object[] { filesToTransfer.Count + " files" });

			int r = -1;
			foreach (var result in filesToTransfer) {
				r++;

				// absorb errors
				try {

					// skip uploading if the file already exists on the server
					FtpRemoteExists existsModeToUse;
					if (!CanUploadFile(result, remoteListing, existsMode, out existsModeToUse)) {
						continue;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToTransfer.Count, r);

					// transfer the file
					var transferred = await TransferFileAsync(result.LocalPath, remoteClient, result.RemotePath, false, existsModeToUse, verifyOptions, progress, metaProgress, token);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogStatus(FtpTraceLevel.Warn, "File failed to transfer: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

#endif


	}
}