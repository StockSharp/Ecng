﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	public class FtpFxpSession : IDisposable {
		/// <summary>
		/// A connection to the FTP server where the file or folder is currently stored
		/// </summary>
		public FtpClient SourceServer;

		/// <summary>
		/// A connection to the destination FTP server where you want to create the file or folder
		/// </summary>
		public FtpClient TargetServer;

		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Closes an FXP connection by disconnecting and disposing off the FTP clients that are
		/// cloned for this FXP connection. Manually created FTP clients are untouched.
		/// </summary>
		public void Dispose() {
			if (IsDisposed) {
				return;
			}
			
			if (SourceServer != null) {
				SourceServer.AutoDispose();
				SourceServer = null;
			}
			if (TargetServer != null) {
				TargetServer.AutoDispose();
				TargetServer = null;
			}

			IsDisposed = true;
			GC.SuppressFinalize(this);

		}
	}
}