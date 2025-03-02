namespace Ecng.Interop;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

using Windows.Win32;

using Ecng.Common;

// https://stackoverflow.com/a/30687230/8029915

/// <summary>
/// Provides methods to modify access rights for the Windows window station and desktop.
/// </summary>
public static class WindowsGrandAccess
{
	private class Token(GenericSecurity wsSecurity, WindowsGrandAccess.GenericSecurity dsSecurity, int? oldWindowStationMaskm, int? oldDesktopMask) : Disposable
	{
		private readonly GenericSecurity _wsSecurity = wsSecurity ?? throw new ArgumentNullException(nameof(wsSecurity));
		private readonly GenericSecurity _dsSecurity = dsSecurity ?? throw new ArgumentNullException(nameof(dsSecurity));
		private readonly int? _oldWindowStationMaskm = oldWindowStationMaskm;
		private readonly int? _oldDesktopMask = oldDesktopMask;

		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			if (_oldWindowStationMaskm is not null)
				_wsSecurity.RestAccessMask(_oldWindowStationMaskm.Value, _windowStationAllAccess);

			if (_oldDesktopMask is not null)
				_dsSecurity.RestAccessMask(_oldDesktopMask.Value, _desktopRightsAllAccess);
		}
	}

	private const int _desktopRightsAllAccess = 0x000f01ff;
	private const int _windowStationAllAccess = 0x000f037f;

	/// <summary>
	/// Grants full access rights to the current process window station and thread desktop for the specified user.
	/// </summary>
	/// <param name="username">The user account to which access rights are to be granted.</param>
	/// <returns>
	/// An <see cref="IDisposable"/> token that, when disposed, restores the original access rights.
	/// </returns>
	public static IDisposable GrantAccessToWindowStationAndDesktop(string username)
	{
		var wsHandle = new NoopSafeHandle(PInvoke.GetProcessWindowStation());
		var dsHandle = new NoopSafeHandle(PInvoke.GetThreadDesktop(PInvoke.GetCurrentThreadId()));

		var account = new NTAccount(username);

		var (oldWindowStationMask, wsSecurity) = ReadAccessMask(account, wsHandle, _windowStationAllAccess);
		var (oldDesktopMask, dsSecurity) = ReadAccessMask(account, dsHandle, _desktopRightsAllAccess);

		wsSecurity.SetGrandAccess(_windowStationAllAccess);
		dsSecurity.SetGrandAccess(_desktopRightsAllAccess);

		return new Token(wsSecurity, dsSecurity, oldWindowStationMask, oldDesktopMask);
	}

	private static (int? mask, GenericSecurity security) ReadAccessMask(NTAccount account, SafeHandle handle, int accessMask)
	{
		var security = new GenericSecurity(account, false, ResourceType.WindowObject, handle, AccessControlSections.Access);

		var rules = security.GetAccessRules();

		var username = account.Value;
		//if (!username.Contains("\\"))
		//	username = $"{Environment.MachineName}\\{username}";

		var userResult = rules.Cast<GenericAccessRule>().SingleOrDefault(r => r.IdentityReference.Value.EqualsIgnoreCase(username) && accessMask == r.PublicAccessMask);
		if (userResult is null)
		{
			security.AddGrandAccess(accessMask);
			userResult = rules.Cast<GenericAccessRule>().SingleOrDefault(r => r.IdentityReference.Value.EqualsIgnoreCase(username));
			
			if (userResult != null)
				return (userResult.PublicAccessMask, security);
		}
		else
			return (userResult.PublicAccessMask, security);

		return (null, security);
	}

	private class GenericAccessRule : AccessRule
	{
		public GenericAccessRule(
			IdentityReference identity, int accessMask, bool isInherited,
			InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
			AccessControlType type)
			: base(identity, accessMask, isInherited,
			inheritanceFlags, propagationFlags, type)
		{
		}

		public GenericAccessRule(IdentityReference identity, int accessMask, AccessControlType type)
			: base(identity, accessMask, false, InheritanceFlags.None, PropagationFlags.None, type)
		{
		}

		public int PublicAccessMask => AccessMask;
	}

	private class GenericSecurity(NTAccount account, bool isContainer, ResourceType resType, SafeHandle handle, AccessControlSections sectionsRequested) : NativeObjectSecurity(isContainer, resType, handle, sectionsRequested)
	{
		private readonly NTAccount _account = account ?? throw new ArgumentNullException(nameof(account));
		private readonly SafeHandle _handle = handle ?? throw new ArgumentNullException(nameof(handle));

		private void Persist() => Persist(_handle, AccessControlSections.Access);

		public AuthorizationRuleCollection GetAccessRules()
			=> GetAccessRules(true, false, typeof(NTAccount));

		public void AddGrandAccess(int accessMask)
		{
			AddAccessRule(new GenericAccessRule(_account, accessMask, AccessControlType.Allow));
			Persist();
		}

		public void SetGrandAccess(int accessMask)
		{
			SetAccessRule(new GenericAccessRule(_account, accessMask, AccessControlType.Allow));
			Persist();
		}

		public void RemoveGrantAccess(int accessMask)
		{
			RemoveAccessRule(new GenericAccessRule(_account, accessMask, AccessControlType.Allow));
			Persist();
		}

		public void RestAccessMask(int? oldAccessMask, int fullAccessMask)
		{
			if (oldAccessMask == null)
				RemoveGrantAccess(fullAccessMask);
			else if (oldAccessMask != fullAccessMask)
				SetGrandAccess(oldAccessMask.Value);
		}

		#region NativeObjectSecurity Abstract Method Overrides

		public override Type AccessRightType => throw new NotSupportedException();
		public override Type AuditRuleType => typeof(AuditRule);
		public override Type AccessRuleType => typeof(AccessRule);

		public override AccessRule AccessRuleFactory(
			IdentityReference identityReference,
			int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
			PropagationFlags propagationFlags, AccessControlType type)
		{
			return new GenericAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
		}

		public override AuditRule AuditRuleFactory(
			IdentityReference identityReference,
			int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
			PropagationFlags propagationFlags, AuditFlags flags)
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	private class NoopSafeHandle(IntPtr handle) : SafeHandle(handle, false)
	{
		public override bool IsInvalid => false;

		// Handles returned by GetProcessWindowStation and GetThreadDesktop
		// should not be closed
		protected override bool ReleaseHandle() => true;
	}
}