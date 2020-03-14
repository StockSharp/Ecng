using System;

namespace FluentFTP {
	/// <summary>
	/// Class to report FTP file transfer progress during upload or download of files
	/// </summary>
	public class FtpProgress {
		/// <summary>
		/// A value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public double Progress { get; set; }

		/// <summary>
		/// A value representing the current Transfer Speed in Bytes per seconds.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public double TransferSpeed { get; set; }

		/// <summary>
		/// A value representing the calculated 'Estimated time of arrival'.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public TimeSpan ETA { get; set; }

		/// <summary>
		/// Stores the absolute remote path of the the current file being transfered.
		/// </summary>
		public string RemotePath { get; set; }

		/// <summary>
		/// Stores the absolute local path of the the current file being transfered.
		/// </summary>
		public string LocalPath { get; set; }

		/// <summary>
		/// Stores the index of the the file in the listing.
		/// Only used when transfering multiple files or an entire directory.
		/// </summary>
		public int FileIndex { get; set; }

		/// <summary>
		/// Stores the total count of the files to be transfered.
		/// Only used when transfering multiple files or an entire directory.
		/// </summary>
		public int FileCount { get; set; }

		/// <summary>
		/// Create a new FtpProgress object for meta progress info.
		/// </summary>
		public FtpProgress(int fileCount, int fileIndex) {
			FileCount = fileCount;
			FileIndex = fileIndex;
		}

		/// <summary>
		/// Create a new FtpProgress object for individual file transfer progress.
		/// </summary>
		public FtpProgress(double progress, double transferspeed, TimeSpan remainingtime, string localPath, string remotePath, FtpProgress metaProgress) {

			// progress of individual file transfer
			Progress = progress;
			TransferSpeed = transferspeed;
			ETA = remainingtime;
			LocalPath = localPath;
			RemotePath = remotePath;

			// progress of the entire task
			if (metaProgress != null) {
				FileCount = metaProgress.FileCount;
				FileIndex = metaProgress.FileIndex;
			}
		}
		
		/// <summary>
		/// Convert Transfer Speed (bytes per second) in human readable format
		/// </summary>
		public string TransferSpeedToString() {
			var value = TransferSpeed > 0 ? TransferSpeed / 1024 : 0; //get KB/s

			if (value < 1024) {
				return Math.Round(value, 2).ToString() + " KB/s";
			}
			else {
				value = value / 1024;
				return Math.Round(value, 2).ToString() + " MB/s";
			}
		}


		/// <summary>
		/// Create a new FtpProgress object for a file transfer and calculate the ETA, Percentage and Transfer Speed.
		/// </summary>
		public static FtpProgress Generate(long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			// default values to send
			double progressValue = -1;
			double transferSpeed = 0;
			var estimatedRemaingTime = TimeSpan.Zero;

			// catch any divide-by-zero errors
			try {

				// calculate raw transferSpeed (bytes per second)
				transferSpeed = bytesProcessed / elapsedtime.TotalSeconds;

				// If fileSize < 0 the below computations make no sense 
				if (fileSize > 0) {

					// calculate % based on file length vs file offset
					// send a value between 0-100 indicating percentage complete
					progressValue = (double)position / (double)fileSize * 100;

					//calculate remaining time			
					estimatedRemaingTime = TimeSpan.FromSeconds((fileSize - position) / transferSpeed);
				}
			}
			catch (Exception) {
			}

			// suppress invalid values and send -1 instead
			if (double.IsNaN(progressValue) && double.IsInfinity(progressValue)) {
				progressValue = -1;
			}
			if (double.IsNaN(transferSpeed) && double.IsInfinity(transferSpeed)) {
				transferSpeed = 0;
			}

			var p = new FtpProgress(progressValue, transferSpeed, estimatedRemaingTime, localPath, remotePath, metaProgress);
			return p;
		}

	}
}