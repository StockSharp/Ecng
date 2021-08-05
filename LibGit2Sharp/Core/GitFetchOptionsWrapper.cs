﻿using System;

namespace LibGit2Sharp.Core
{
    /// <summary> 
    /// Git fetch options wrapper. Disposable wrapper for GitFetchOptions 
    /// </summary>
    internal class GitFetchOptionsWrapper : IDisposable
    {
        public GitFetchOptionsWrapper() : this(new GitFetchOptions()) { }

        public GitFetchOptionsWrapper(GitFetchOptions fetchOptions)
        {
            this.Options = fetchOptions;
        }

        public GitFetchOptions Options { get; private set; }

        #region IDisposable
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            this.Options.CustomHeaders.Dispose();
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
