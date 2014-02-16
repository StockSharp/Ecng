namespace Ecng.Web
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Text.RegularExpressions;
	using System.Web.Security;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Security;
	using Ecng.Serialization;

	public enum SecurityErrorTypes
	{
		InvalidPassword,
		InvalidOldPassword,
		InvalidPasswordAnswer,
		InvalidName,
		UserLockedOut,
		UserNotApproved
	}

	[Ignore(FieldName = "_EventHandler")]
	[Ignore(FieldName = "_name")]
	[Ignore(FieldName = "_Initialized")]
	[Ignore(FieldName = "_Description")]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.Both)]
	public abstract class BaseMembershipProvider : MembershipProvider
	{
		[Ignore]
		private readonly object _lock = new object();

		[Ignore]
		private readonly Dictionary<IWebUser, Tuple<DateTime, int>> _passwordAttempts = new Dictionary<IWebUser, Tuple<DateTime, int>>();

		public override void Initialize(string name, NameValueCollection config)
		{
			this.Initialize(config);
			base.Initialize(name, config);
		}

		//public const UniqueStates DefaultUniqueStateValue = UniqueStates.NameAndEmail;
		public const bool DefaultRequiresUniqueNameValue = true;
		public const bool DefaultEnablePasswordResetValue = true;
		public const bool DefaultEnablePasswordRetrievalValue = false;
		public const bool DefaultRequiresQuestionAndAnswerValue = true;
		public const bool DefaultRequiresUniqueEmailValue = true;
		public const int DefaultMaxInvalidPasswordAttemptsValue = 5;
		public const int DefaultMinRequiredNonAlphanumericCharactersValue = 1;
		public const int DefaultMinRequiredPasswordLengthValue = 7;
		public const int DefaultPasswordAttemptWindowValue = 0;
		public const MembershipPasswordFormat DefaultPasswordFormatValue = MembershipPasswordFormat.Hashed;
		public const string DefaultPasswordStrengthRegularExpressionValue = "";
		public const int DefaultMinRequiredUserLengthValue = 4;
		public const int DefaultMaxRequiredUserLengthValue = 12;

		//#region UniqueState

		//[DefaultValue(DefaultUniqueStateValue)]
		//private UniqueStates _uniqueState = DefaultUniqueStateValue;

		//public UniqueStates UniqueState
		//{
		//    get { return _uniqueState; }
		//    set { _uniqueState = value; }
		//}

		//#endregion

		[DefaultValue(DefaultRequiresUniqueNameValue)]
		[Field("requiresUniqueName")]
		private readonly bool _requiresUniqueName = DefaultRequiresUniqueNameValue;

		public bool RequiresUniqueName
		{
			get { return _requiresUniqueName; }
		}

		[DefaultValue(DefaultMinRequiredUserLengthValue)]
		[Field("minRequiredUserLength")]
		private readonly int _minRequiredUserLength = DefaultMinRequiredUserLengthValue;

		public int MinRequiredUserLength
		{
			get { return _minRequiredUserLength; }
		}

		[DefaultValue(DefaultMaxRequiredUserLengthValue)]
		[Field("maxRequiredUserLength")]
		private readonly int _maxRequiredUserLength = DefaultMaxRequiredUserLengthValue;

		public int MaxRequiredUserLength
		{
			get { return _maxRequiredUserLength; }
		}

		#region MembershipProvider Members

		[ApplicationDefaultValue]
		[Field("applicationName")]
		private string _applicationName;

		public override string ApplicationName
		{
			get { return _applicationName; }
			set { _applicationName = value; }
		}

		[DefaultValue(DefaultEnablePasswordResetValue)]
		[Field("enablePasswordReset")]
		private readonly bool _enablePasswordReset = DefaultEnablePasswordResetValue;
		
		public override bool EnablePasswordReset
		{
			get { return _enablePasswordReset; }
		}

		[DefaultValue(DefaultEnablePasswordRetrievalValue)]
		[Field("enablePasswordRetrieval")]
		private readonly bool _enablePasswordRetrieval = DefaultEnablePasswordRetrievalValue;

		public override bool EnablePasswordRetrieval
		{
			get { return _enablePasswordRetrieval; }
		}

		[DefaultValue(DefaultRequiresQuestionAndAnswerValue)]
		[Field("requiresQuestionAndAnswer")]
		private readonly bool _requiresQuestionAndAnswer = DefaultRequiresQuestionAndAnswerValue;

		public override bool RequiresQuestionAndAnswer
		{
			get { return _requiresQuestionAndAnswer; }
		}

		[DefaultValue(DefaultRequiresUniqueEmailValue)]
		[Field("requiresUniqueEmail")]
		private readonly bool _requiresUniqueEmail = DefaultRequiresUniqueEmailValue;

		public override bool RequiresUniqueEmail
		{
			get { return _requiresUniqueEmail; }
		}

		[DefaultValue(DefaultMaxInvalidPasswordAttemptsValue)]
		[Field("maxInvalidPasswordAttempts")]
		private readonly int _maxInvalidPasswordAttempts = DefaultMaxInvalidPasswordAttemptsValue;

		public override int MaxInvalidPasswordAttempts
		{
			get { return _maxInvalidPasswordAttempts; }
		}

		[DefaultValue(DefaultMinRequiredNonAlphanumericCharactersValue)]
		[Field("minRequiredNonAlphanumericCharacters")]
		private readonly int _minRequiredNonAlphanumericCharacters = DefaultMinRequiredNonAlphanumericCharactersValue;

		public override int MinRequiredNonAlphanumericCharacters
		{
			get { return _minRequiredNonAlphanumericCharacters; }
		}

		[DefaultValue(DefaultMinRequiredPasswordLengthValue)]
		[Field("minRequiredPasswordLength")]
		private readonly int _minRequiredPasswordLength = DefaultMinRequiredPasswordLengthValue;

		public override int MinRequiredPasswordLength
		{
			get { return _minRequiredPasswordLength; }
		}

		[DefaultValue(DefaultPasswordAttemptWindowValue)]
		[Field("passwordAttemptWindow")]
		private readonly int _passwordAttemptWindow = DefaultPasswordAttemptWindowValue;

		public override int PasswordAttemptWindow
		{
			get { return _passwordAttemptWindow; }
		}

		[DefaultValue(DefaultPasswordFormatValue)]
		[Field("passwordFormat")]
		private readonly MembershipPasswordFormat _passwordFormat = DefaultPasswordFormatValue;

		public override MembershipPasswordFormat PasswordFormat
		{
			get { return _passwordFormat; }
		}

		[DefaultValue(DefaultPasswordStrengthRegularExpressionValue)]
		[Field("passwordStrengthRegularExpression")]
		private readonly string _passwordStrengthRegularExpression = DefaultPasswordStrengthRegularExpressionValue;

		public override string PasswordStrengthRegularExpression
		{
			get { return _passwordStrengthRegularExpression; }
		}

		[DefaultValue(Secret.DefaultSaltSize)]
		[Field("saltSize")]
		private readonly int _saltSize = Secret.DefaultSaltSize;

		public int SaltSize
		{
			get { return _saltSize; }
		}

		private readonly Lazy<CryptoAlgorithm> _cryptoAlgorithm = new Lazy<CryptoAlgorithm>(() => CryptoAlgorithm.Create(CryptoAlgorithm.GetAlgo(Membership.HashAlgorithmType)));

		public virtual CryptoAlgorithm CryptoAlgorithm
		{
			get
			{
				return _cryptoAlgorithm.Value;
			}
		}

		public override MembershipUser CreateUser(string userName, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
		{
			status = MembershipCreateStatus.Success;

			if (RequiresUniqueName)
			{
				if (userName.IsEmpty() || !new Range<int>(MinRequiredUserLength, MaxRequiredUserLength).Contains(userName.Length))
					status = MembershipCreateStatus.InvalidUserName;
				else if (Users.GetByName(userName) != null)
					status = MembershipCreateStatus.DuplicateUserName;
			}

			if (status != MembershipCreateStatus.Success)
				return null;

			if (RequiresUniqueEmail)
			{
				if (email.IsEmpty())
					status = MembershipCreateStatus.InvalidEmail;
				else if (Users.GetByEmail(email) != null)
					status = MembershipCreateStatus.DuplicateEmail;
			}

			if (status != MembershipCreateStatus.Success)
				return null;

			if (password.IsEmpty() || password.Length < MinRequiredPasswordLength)
				status = MembershipCreateStatus.InvalidPassword;
			else
			{
				//int count = password.Length - password.Count(char.IsLetterOrDigit);

				if (password.Count(char.IsLetterOrDigit) < MinRequiredNonAlphanumericCharacters)
					status = MembershipCreateStatus.InvalidPassword;
				else if (!PasswordStrengthRegularExpression.IsEmpty() && !Regex.IsMatch(password, PasswordStrengthRegularExpression))
					status = MembershipCreateStatus.InvalidPassword;
				else
				{
					var e = new ValidatePasswordEventArgs(userName, password, true);
					base.OnValidatingPassword(e);

					if (e.Cancel)
						status = MembershipCreateStatus.InvalidPassword;	
				}
			}

			if (status != MembershipCreateStatus.Success)
				return null;

			if (RequiresQuestionAndAnswer && passwordQuestion.IsEmpty())
				status = MembershipCreateStatus.InvalidQuestion;

			if (status != MembershipCreateStatus.Success)
				return null;

			if (RequiresQuestionAndAnswer && passwordAnswer.IsEmpty())
				status = MembershipCreateStatus.InvalidAnswer;

			if (status != MembershipCreateStatus.Success)
				return null;

			return ConvertToMembershipUser(CreateUser(userName, CreateSecret(password), email, passwordQuestion, RequiresQuestionAndAnswer ? CreateSecret(passwordAnswer) : new Secret(), isApproved, providerUserKey));
		}

		public Secret CreateSecret(string plainText)
		{
			return CreateSecret(plainText, CryptoHelper.GenerateSalt(SaltSize));
		}

		protected virtual Secret CreateSecret(string plainText, byte[] salt)
		{
			return new Secret(plainText.To<byte[]>(), salt, CryptoAlgorithm);
		}

		public override string GetUserNameByEmail(string email)
		{
			var user = Users.GetByEmail(email);

			if (user == null)
				return null;

			return user.Name;
		}

		public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
		{
			//if (providerUserKey == null)
			//    throw new ArgumentNullException("providerUserKey");

			var user = Users.GetByKey(providerUserKey);

			if (user == null)
				throw new ArgumentException("User not founded.", "providerUserKey");

			if (userIsOnline)
			{
				user.LastActivityDate = DateTime.Now;
				UpdateUser(user);
			}

			return ConvertToMembershipUser(user);
		}

		public override MembershipUser GetUser(string userName, bool userIsOnline)
		{
			if (userName.IsEmpty())
				return null;

			var user = Users.GetByName(userName);

			if (user == null)
			{
				//SecurityError(userName, SecurityErrorTypes.InvalidName);
				return null;
			}

			if (userIsOnline)
			{
				user.LastActivityDate = DateTime.Now;
				UpdateUser(user);
			}

			return ConvertToMembershipUser(user);
		}

		public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
		{
			var users = new MembershipUserCollection();

			foreach (var user in GetUserRange(pageIndex, pageSize, out totalRecords))
				users.Add(ConvertToMembershipUser(user));

			return users;
		}

		public override MembershipUserCollection FindUsersByName(string userNameToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			var users = new MembershipUserCollection();

			foreach (var user in GetUserRangeByName(userNameToMatch, pageIndex, pageSize, out totalRecords))
				users.Add(ConvertToMembershipUser(user));

			return users;
		}

		public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			var users = new MembershipUserCollection();

			foreach (var user in GetUserRangeByEmail(emailToMatch, pageIndex, pageSize, out totalRecords))
				users.Add(ConvertToMembershipUser(user));

			return users;
		}

		public override string GetPassword(string userName, string answer)
		{
			if (!EnablePasswordRetrieval)
				throw new NotSupportedException();

			var user = Users.GetByName(userName);

			if (user == null)
			{
				SecurityError(userName, SecurityErrorTypes.InvalidName);
				throw new ArgumentException("User {0} not founded.".Put(userName), "userName");
			}

			if (!IsAnswerValid(user, answer))
			{
				ValidatePasswordAttemts(user);
				SecurityError(userName, SecurityErrorTypes.InvalidPasswordAnswer);
				throw new MembershipPasswordException("Answer for user {0} is incorrect.".Put(userName));
			}
			
			return user.Password.ToString();
		}

		public override string ResetPassword(string userName, string answer)
		{
			if (!EnablePasswordReset)
				throw new NotSupportedException();

			var user = Users.GetByName(userName);

			if (user == null)
			{
				SecurityError(userName, SecurityErrorTypes.InvalidName);
				throw new ArgumentException("User {0} not founded.".Put(userName), "userName");
			}

			if (!user.IsApproved)
			{
				SecurityError(userName, SecurityErrorTypes.UserNotApproved);
				throw new MembershipPasswordException("User {0} not approved.".Put(userName));
			}

			if (!IsAnswerValid(user, answer))
			{
				ValidatePasswordAttemts(user);
				SecurityError(userName, SecurityErrorTypes.InvalidPasswordAnswer);
				throw new MembershipPasswordException("Answer for user {0} is incorrect.".Put(userName));
			}

			var newPassword = Membership.GeneratePassword(MinRequiredPasswordLength, MinRequiredNonAlphanumericCharacters);

			user.Password = CreateSecret(newPassword);
			user.LastPasswordChangedDate = DateTime.Now;

			UpdateUser(user);
			return newPassword;
		}

		public override bool ChangePassword(string userName, string oldPassword, string newPassword)
		{
			var user = Users.GetByName(userName);

			if (user == null)
			{
				SecurityError(userName, SecurityErrorTypes.InvalidName);
				return false;
			}

			if (!user.IsApproved)
			{
				SecurityError(userName, SecurityErrorTypes.UserNotApproved);
				return false;
			}

			if (!IsPasswordValid(user, oldPassword))
			{
				ValidatePasswordAttemts(user);
				SecurityError(userName, SecurityErrorTypes.InvalidOldPassword);
				return false;
			}

			ChangePassword(user, newPassword);
			return true;
		}

		internal void ChangePassword(IWebUser user, string password)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			user.Password = CreateSecret(password);
			user.LastPasswordChangedDate = DateTime.Now;

			UpdateUser(user);
		}

		public override bool ChangePasswordQuestionAndAnswer(string userName, string password, string newPasswordQuestion, string newPasswordAnswer)
		{
			var user = Users.GetByName(userName);

			if (user == null)
				return false;

			if (!user.IsApproved)
			{
				SecurityError(userName, SecurityErrorTypes.UserNotApproved);
				return false;
			}

			if (!IsPasswordValid(user, password))
			{
				ValidatePasswordAttemts(user);
				SecurityError(userName, SecurityErrorTypes.InvalidPasswordAnswer);
				return false;
			}

			user.PasswordQuestion = newPasswordQuestion;
			user.PasswordAnswer = CreateSecret(newPasswordAnswer);
			user.LastPasswordChangedDate = DateTime.Now;

			UpdateUser(user);
			return true;
		}

		public override bool UnlockUser(string userName)
		{
			var user = Users.GetByName(userName);

			if (user == null)
				return false;

			user.IsLockedOut = false;
			UpdateUser(user);
			return true;
		}

		public override bool DeleteUser(string name, bool deleteAllRelatedData)
		{
			var user = Users.GetByName(name);

			if (user == null)
				return false;
			
			Users.Remove(user);
			return true;
		}

		public override void UpdateUser(MembershipUser membershipUser)
		{
			if (membershipUser == null)
				throw new ArgumentNullException("membershipUser");

			var user = Users.GetByName(membershipUser.UserName);

			if (user == null)
				throw new ArgumentException("membershipUser");

			user.LastLoginDate = membershipUser.LastLoginDate;
			user.LastActivityDate = membershipUser.LastActivityDate;
			user.IsApproved = membershipUser.IsApproved;

			UpdateUser(user);
		}

		public override bool ValidateUser(string userName, string password)
		{
			var type = password.IsEmpty() ? SecurityErrorTypes.InvalidPassword : ValidateUserInternal(userName, password);

			if (type == null)
				return true;

			SecurityError(userName, (SecurityErrorTypes)type);
			return false;
		}

		#endregion

		internal SecurityErrorTypes? ValidateUserInternal(string userName, string password)
		{
			var user = Users.GetByName(userName);

			if (user == null)
				return SecurityErrorTypes.InvalidName;

			if (user.IsLockedOut)
				return SecurityErrorTypes.UserLockedOut;

			if (!user.IsApproved)
				return SecurityErrorTypes.UserNotApproved;

			if (!IsPasswordValid(user, password))
			{
				ValidatePasswordAttemts(user);
				return SecurityErrorTypes.InvalidPassword;
			}

			user.LastLoginDate = DateTime.Now;
			UpdateUser(user);
			return null;
		}

		protected abstract IWebUser CreateUser(string userName, Secret password, string email, string passwordQuestion, Secret passwordAnswer, bool isApproved, object providerUserKey);
		//protected abstract bool IsNameUnique(string userName);
		protected abstract IEnumerable<IWebUser> GetUserRange(int pageIndex, int pageSize, out int totalRecords);
		protected abstract IEnumerable<IWebUser> GetUserRangeByName(string userNameToMatch, int pageIndex, int pageSize, out int totalRecords);
		protected abstract IEnumerable<IWebUser> GetUserRangeByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords);
		protected abstract void UpdateUser(IWebUser user);

		protected internal abstract IWebRoleCollection Roles { get; }
		protected internal abstract IWebUserCollection Users { get; }

		protected virtual void SecurityError(string userName, SecurityErrorTypes type)
		{
			SecurityError(new UnauthorizedAccessException("{0}{1}{2}".Put(userName, Environment.NewLine, type)));
		}

		protected abstract void SecurityError(UnauthorizedAccessException ex);

		private bool IsAnswerValid(IWebUser user, string answer)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			return !RequiresQuestionAndAnswer || user.PasswordAnswer.Equals(CreateSecret(answer, user.PasswordAnswer.Salt));
		}

		private bool IsPasswordValid(IWebUser user, string password)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			return user.Password.Equals(CreateSecret(password, user.Password.Salt));
		}

		private MembershipUser ConvertToMembershipUser(IWebUser user)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			return new MembershipUser(base.Name, user.Name, user.Key, user.Email, RequiresQuestionAndAnswer ? user.PasswordQuestion : string.Empty, user.Description, user.IsApproved, user.IsLockedOut, user.CreationDate, user.LastLoginDate, user.LastActivityDate, user.LastPasswordChangedDate, user.LastLockOutDate);
		}

		private void ValidatePasswordAttemts(IWebUser user)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			Tuple<DateTime, int> passwordAttemts;

			lock (_lock)
			{
				var count = 1;

				if (_passwordAttempts.TryGetValue(user, out passwordAttemts))
				{
					if ((DateTime.Now - passwordAttemts.Item1) < TimeSpan.FromMinutes(PasswordAttemptWindow))
						count = passwordAttemts.Item2 + 1;
				}

				passwordAttemts = new Tuple<DateTime, int>(DateTime.Now, count);

				_passwordAttempts[user] = passwordAttemts;
			}

			if (passwordAttemts.Item2 >= MaxInvalidPasswordAttempts)
			{
				user.IsLockedOut = true;
				user.LastLockOutDate = DateTime.Now;
				UpdateUser(user);
			}
		}
	}
}