using System.IO;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using SQLite;

namespace Stratum.Desktop.Persistence
{
    public class Database
    {
        private const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex;

        private readonly string _path;
        private SQLiteAsyncConnection _connection;

        public Database(string path)
        {
            _path = path;
        }

        public async Task OpenAsync(string password = null)
        {
            var directory = Path.GetDirectoryName(_path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var connectionString = password == null
                ? new SQLiteConnectionString(_path, Flags, true)
                : new SQLiteConnectionString(_path, Flags, true, password);

            _connection = new SQLiteAsyncConnection(connectionString);
            await _connection.EnableWriteAheadLoggingAsync();
            await _connection.CreateTableAsync<Authenticator>();
            await _connection.CreateTableAsync<Category>();
            await _connection.CreateTableAsync<AuthenticatorCategory>();
            await _connection.CreateTableAsync<CustomIcon>();
            await _connection.CreateTableAsync<IconPack>();
            await _connection.CreateTableAsync<IconPackEntry>();
        }

        public Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            return Task.FromResult(_connection);
        }
    }
}
