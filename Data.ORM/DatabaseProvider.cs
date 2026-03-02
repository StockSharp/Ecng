namespace Ecng.Data;

public abstract class DatabaseProvider(DbProviderFactory factory, SqlRenderer renderer, string name)
{
	protected DatabaseProvider(DbProviderFactory factory, SqlRenderer renderer)
		: this(factory, renderer, renderer.GetType().Name.Remove("Renderer"))
	{
	}

	public string Name { get; } = name;
	public DbProviderFactory Factory { get; } = factory ?? throw new ArgumentNullException(nameof(factory));
	public SqlRenderer Renderer { get; } = renderer ?? throw new ArgumentNullException(nameof(renderer));

	public async ValueTask<DbConnection> CreateConnectionAsync(string connectionString, CancellationToken cancellationToken)
	{
		var connection = Factory.CreateConnection();

		if (connection == null)
			throw new InvalidOperationException();

		connection.ConnectionString = connectionString;
		await connection.OpenAsync(cancellationToken);
		return connection;
	}

	public DbCommand CreateCommand(string text, CommandType type)
	{
		var command = Factory.CreateCommand();

		if (command == null)
			throw new InvalidOperationException();

		command.CommandText = text;
		command.CommandType = type;

		return command;
	}

	public DbParameter CreateParameter(string name, DbType type)
	{
		var param = Factory.CreateParameter();

		if (param == null)
			throw new InvalidOperationException();

		param.ParameterName = name;
		param.DbType = type;

		return param;
	}

	protected internal abstract void DeriveParameters(DbCommand command);
}