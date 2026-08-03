using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Stratum.Desktop.Services
{
    [DBusInterface("org.freedesktop.Secret.Service")]
    public interface ISecretService : IDBusObject
    {
        Task<(object result, ObjectPath output)> OpenSessionAsync(string algorithm, object input);
        Task<(ObjectPath[] unlocked, ObjectPath[] locked)> SearchItemsAsync(IDictionary<string, string> attributes);
    }

    [DBusInterface("org.freedesktop.Secret.Collection")]
    public interface ISecretCollection : IDBusObject
    {
        Task<(ObjectPath item, ObjectPath prompt)> CreateItemAsync(IDictionary<string, object> properties,
            (ObjectPath session, byte[] parameters, byte[] value, string contentType) secret, bool replace);
    }

    [DBusInterface("org.freedesktop.Secret.Item")]
    public interface ISecretItem : IDBusObject
    {
        Task<(ObjectPath session, byte[] parameters, byte[] value, string contentType)> GetSecretAsync(ObjectPath session);
        Task DeleteAsync();
    }

    public class SecretServiceStore
    {
        private const string ServiceName = "org.freedesktop.secrets";
        private const string ServicePath = "/org/freedesktop/secrets";
        private const string CollectionPath = "/org/freedesktop/secrets/collection/kdewallet";
        private const string Label = "Stratum master key";
        private const string AttributeKey = "stratum-key";

        private static readonly Lazy<Connection> SharedConnection = new(() =>
        {
            var connection = Connection.Session;
            connection.ConnectAsync().GetAwaiter().GetResult();
            return connection;
        });

        private static Connection CurrentConnection => SharedConnection.Value;

        public bool IsAvailable()
        {
            try
            {
                return CurrentConnection.IsServiceActiveAsync(ServiceName).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        public byte[] GetOrCreateKey(int length)
        {
            try
            {
                var result = Task.Run(async () =>
                {
                    var service = CurrentConnection.CreateProxy<ISecretService>(ServiceName, ServicePath);
                    var (_, sessionPath) = await service.OpenSessionAsync("plain", new Dictionary<string, object>());

                    var attributes = new Dictionary<string, string> { { AttributeKey, "1" } };
                    var (found, _) = await service.SearchItemsAsync(attributes);

                    if (found.Length > 0)
                    {
                        var item = CurrentConnection.CreateProxy<ISecretItem>(ServiceName, found[0]);
                        var (_, _, value, _) = await item.GetSecretAsync(sessionPath);

                        if (value.Length == length)
                        {
                            return value;
                        }
                    }

                    var newKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);
                    var properties = new Dictionary<string, object>
                    {
                        { "org.freedesktop.Secret.Item.Label", Label },
                        { "org.freedesktop.Secret.Item.Attributes", attributes }
                    };
                    var secret = (sessionPath, Array.Empty<byte>(), newKey, "application/octet-stream");
                    var collection = CurrentConnection.CreateProxy<ISecretCollection>(ServiceName, CollectionPath);
                    await collection.CreateItemAsync(properties, secret, true);
                    return newKey;
                });

                return result.GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }

        // TEMP: test-only cleanup of the keystore test items, not used by the app
        public void DeleteTestItems()
        {
            try
            {
                var service = CurrentConnection.CreateProxy<ISecretService>(ServiceName, ServicePath);
                var attributes = new Dictionary<string, string> { { AttributeKey, "1" } };
                var (found, _) = service.SearchItemsAsync(attributes).GetAwaiter().GetResult();

                foreach (var itemPath in found)
                {
                    var item = CurrentConnection.CreateProxy<ISecretItem>(ServiceName, itemPath);
                    item.DeleteAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
            }
        }
    }
}
