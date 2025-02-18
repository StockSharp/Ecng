namespace Ecng.Backup.Mega.Native
{
  using System.Collections.Generic;

  interface IAccountInformation
  {
    long TotalQuota { get; }

    long UsedQuota { get; }

    IEnumerable<IStorageMetrics> Metrics { get; }
  }

  interface IStorageMetrics
  {
    string NodeId { get; }

    long BytesUsed { get; }

    long FilesCount { get; }

    long FoldersCount { get; }
  }
}
