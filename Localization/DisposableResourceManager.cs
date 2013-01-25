namespace Ecng.Localization
{
    #region Using Directives

	using System;
	using System.Resources;

	using Ecng.Common;

	#endregion

    class DisposableResourceManager : Disposable
    {
        private readonly ResourceManager _manager;

        public DisposableResourceManager(Type resourseSource)
        {
            _manager = new ResourceManager(resourseSource);
        }

		public string GetString(string name)
		{
			return _manager.GetString(name);
		}

		public object GetObject(string name)
		{
			return _manager.GetObject(name);
		}

		protected override void DisposeManaged()
		{
			_manager.ReleaseAllResources();
		}
    }
}