namespace Ecng.Tests.Backup;

using Ecng.Backup;

[TestClass]
public class BackupEntryTests : BaseTestClass
{
	[TestMethod]
	public void GetFullPath_SimpleEntry_ReturnsName()
	{
		var entry = new BackupEntry { Name = "test.txt" };

		var path = entry.GetFullPath();

		path.AssertEqual("test.txt");
	}

	[TestMethod]
	public void GetFullPath_WithParent_ReturnsFullPath()
	{
		var parent = new BackupEntry { Name = "folder" };
		var entry = new BackupEntry { Name = "test.txt", Parent = parent };

		var path = entry.GetFullPath();

		path.AssertEqual("folder/test.txt");
	}

	[TestMethod]
	public void GetFullPath_WithNestedParents_ReturnsFullPath()
	{
		var grandParent = new BackupEntry { Name = "root" };
		var parent = new BackupEntry { Name = "folder", Parent = grandParent };
		var entry = new BackupEntry { Name = "test.txt", Parent = parent };

		var path = entry.GetFullPath();

		path.AssertEqual("root/folder/test.txt");
	}

	[TestMethod]
	public void GetFullPath_EmptyName_ThrowsInvalidOperationException()
	{
		var entry = new BackupEntry { Name = "" };

		Throws<InvalidOperationException>(() => entry.GetFullPath());
	}

	[TestMethod]
	public void GetFullPath_NullName_ThrowsInvalidOperationException()
	{
		var entry = new BackupEntry { Name = null };

		Throws<InvalidOperationException>(() => entry.GetFullPath());
	}

	[TestMethod]
	public void GetFullPath_CircularReference_ThrowsInvalidOperationException()
	{
		var entry1 = new BackupEntry { Name = "entry1" };
		var entry2 = new BackupEntry { Name = "entry2" };
		entry1.Parent = entry2;
		entry2.Parent = entry1;

		Throws<InvalidOperationException>(() => entry1.GetFullPath());
	}

	[TestMethod]
	public void GetFullPath_SelfReference_ThrowsInvalidOperationException()
	{
		var entry = new BackupEntry { Name = "self" };
		entry.Parent = entry;

		Throws<InvalidOperationException>(() => entry.GetFullPath());
	}
}
