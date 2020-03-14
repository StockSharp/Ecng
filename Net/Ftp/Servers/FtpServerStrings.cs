﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.Servers {
	internal static class FtpServerStrings {

		#region File Exists

		/// <summary>
		/// Error messages returned by various servers when a file does not exist.
		/// Instead of throwing an error, we use these to detect and handle the file detection properly.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] fileNotFound = new[] {
			"can't find file",
			"can't check for file existence",
			"does not exist",
			"failed to open file",
			"not found",
			"no such file",
			"cannot find the file",
			"cannot find",
			"can't get file",
			"could not get file",
			"could not get file size",
			"cannot get file",
			"not a regular file",
			"file unavailable",
			"file is unavailable",
			"file not unavailable",
			"file is not available",
			"no files found",
			"no file found",
			"datei oder verzeichnis nicht gefunden",
			"can't find the path",
			"cannot find the path",
			"could not find the path",
		};

		#endregion

		#region File Size

		/// <summary>
		/// Error messages returned by various servers when a file size is not supported in ASCII mode.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] fileSizeNotInASCII = new[] {
			"not allowed in ascii",
			"size not allowed in ascii",
			"n'est pas autorisé en mode ascii"
		};

		#endregion

		#region File Transfer

		/// <summary>
		/// Error messages returned by various servers when a file transfer temporarily failed.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] unexpectedEOF = new[] {
			"unexpected eof for remote file",
			"received an unexpected eof",
			"unexpected eof"
		};

		#endregion

		#region Create Directory

		/// <summary>
		/// Error messages returned by various servers when a folder already exists.
		/// Instead of throwing an error, we use these to detect and handle the folder creation properly.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] folderExists = new[] {
			"exist on server",
			"exists on server",
			"file exist",
			"directory exist",
			"folder exist",
			"file already exist",
			"directory already exist",
			"folder already exist",
		};

		#endregion

	}
}
