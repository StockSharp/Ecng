namespace Ecng.Tests.Serialization;

using System.Security.Cryptography;

using Ecng.Serialization;

[TestClass]
[DoNotParallelize]
public class SecureStringEncryptorTests : BaseTestClass
{
	private sealed class TaggedEncryptor : ISecureStringEncryptor
	{
		private readonly string _tag;

		public TaggedEncryptor(string tag)
		{
			_tag = tag.ThrowIfEmpty(nameof(tag));
		}

		public int EncryptCalls { get; private set; }
		public int DecryptCalls { get; private set; }

		public byte[] Encrypt(SecureString value)
		{
			EncryptCalls++;

			return value is null ? null : $"{_tag}:{value.UnSecure()}".UTF8();
		}

		public SecureString Decrypt(byte[] cipher)
		{
			DecryptCalls++;

			if (cipher is null)
				return null;

			var value = cipher.UTF8();
			var prefix = $"{_tag}:";

			if (!value.StartsWith(prefix, StringComparison.Ordinal))
				throw new CryptographicException("The payload was encrypted by another encryptor.");

			return value.Substring(prefix.Length).Secure();
		}
	}

	private sealed class SecureStringHolder : IPersistable
	{
		public SecureString Secret { get; set; }

		void IPersistable.Load(SettingsStorage storage)
		{
			Secret = storage.GetValue<SecureString>(nameof(Secret));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.Set(nameof(Secret), Secret);
		}
	}

	private async Task<MemoryStream> SerializeAsync<T>(JsonSerializer<T> serializer, T value)
	{
		var stream = new MemoryStream();
		await serializer.SerializeAsync(value, stream, CancellationToken);

		stream.Length.AssertGreater(0L);
		stream.Position = 0;

		return stream;
	}

	[TestMethod]
	public void SecureStringHelper_NestedScopes_OverrideAndRestoreEncryptor()
	{
		var original = SecureStringHelper.Encryptor;
		var global = new TaggedEncryptor("global");
		var outer = new TaggedEncryptor("outer");
		var inner = new TaggedEncryptor("inner");

		try
		{
			SecureStringHelper.Encryptor = global;

			using (new Scope<ISecureStringEncryptor>(outer, false))
			{
				SecureStringHelper.Encrypt("outer-1".Secure());

				using (new Scope<ISecureStringEncryptor>(inner, false))
					SecureStringHelper.Encrypt("inner".Secure());

				SecureStringHelper.Encrypt("outer-2".Secure());
			}

			SecureStringHelper.Encrypt("global".Secure());

			global.EncryptCalls.AssertEqual(1);
			outer.EncryptCalls.AssertEqual(2);
			inner.EncryptCalls.AssertEqual(1);
		}
		finally
		{
			SecureStringHelper.Encryptor = original;
		}
	}

	[TestMethod]
	public async Task JsonSerializer_LocalEncryptor_RoundTripsSecureString()
	{
		const string plain = "SuperSecret123!";
		var encryptor = new TaggedEncryptor("serializer");
		var serializer = new JsonSerializer<SecureString> { SecureStringEncryptor = encryptor };

		await using var stream = await SerializeAsync(serializer, plain.Secure());
		stream.ToArray().UTF8().Contains(plain).AssertFalse();

		var actual = await serializer.DeserializeAsync(stream, CancellationToken);

		actual.UnSecure().AssertEqual(plain);
		encryptor.EncryptCalls.AssertEqual(1);
		encryptor.DecryptCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task JsonSerializer_DifferentLocalEncryptor_ThrowsOnDeserialize()
	{
		const string plain = "SuperSecret123!";
		var writerEncryptor = new TaggedEncryptor("writer");
		var readerEncryptor = new TaggedEncryptor("reader");

		var writer = new JsonSerializer<SecureString> { SecureStringEncryptor = writerEncryptor };
		var reader = new JsonSerializer<SecureString> { SecureStringEncryptor = readerEncryptor };

		await using var stream = await SerializeAsync(writer, plain.Secure());

		await ThrowsAsync<CryptographicException>(async () => await reader.DeserializeAsync(stream, CancellationToken));

		writerEncryptor.EncryptCalls.AssertEqual(1);
		readerEncryptor.DecryptCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task JsonSerializer_LocalEncryptor_DoesNotChangeGlobalEncryptor()
	{
		var original = SecureStringHelper.Encryptor;
		var global = new TaggedEncryptor("global");
		var local = new TaggedEncryptor("local");

		try
		{
			SecureStringHelper.Encryptor = global;

			var serializer = new JsonSerializer<SecureString> { SecureStringEncryptor = local };

			await using var stream = await SerializeAsync(serializer, "inside".Secure());
			await serializer.DeserializeAsync(stream, CancellationToken);

			local.EncryptCalls.AssertEqual(1);
			local.DecryptCalls.AssertEqual(1);
			global.EncryptCalls.AssertEqual(0);
			global.DecryptCalls.AssertEqual(0);

			SecureStringHelper.Encrypt("outside".Secure());
			global.EncryptCalls.AssertEqual(1);
		}
		finally
		{
			SecureStringHelper.Encryptor = original;
		}
	}

	[TestMethod]
	public async Task JsonSerializer_LocalEncryptor_IsUsedByPersistableSettingsStorage()
	{
		const string plain = "persistable-secret";
		var encryptor = new TaggedEncryptor("persistable");
		var serializer = new JsonSerializer<SecureStringHolder> { SecureStringEncryptor = encryptor };
		var holder = new SecureStringHolder { Secret = plain.Secure() };

		await using var stream = await SerializeAsync(serializer, holder);
		var json = stream.ToArray().UTF8();

		json.Contains(nameof(SecureStringHolder.Secret)).AssertTrue();
		json.Contains(plain).AssertFalse();

		var actual = await serializer.DeserializeAsync(stream, CancellationToken);

		actual.Secret.UnSecure().AssertEqual(plain);
		encryptor.EncryptCalls.AssertEqual(1);
		encryptor.DecryptCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task JsonSerializer_PersistableSettingsStorage_DifferentLocalEncryptorThrows()
	{
		var writer = new JsonSerializer<SecureStringHolder>
		{
			SecureStringEncryptor = new TaggedEncryptor("writer")
		};
		var readerEncryptor = new TaggedEncryptor("reader");
		var reader = new JsonSerializer<SecureStringHolder>
		{
			SecureStringEncryptor = readerEncryptor
		};

		await using var stream = await SerializeAsync(writer, new SecureStringHolder { Secret = "persistable-secret".Secure() });

		await ThrowsAsync<CryptographicException>(async () => await reader.DeserializeAsync(stream, CancellationToken));

		readerEncryptor.DecryptCalls.AssertEqual(1);
	}
}
