using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using Stratum.Core.Entity;

namespace Stratum.Desktop.Services
{
    public static class DatabaseMigrator
    {
        private const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex;

        public static async Task<bool> IsPlainAsync(string path)
        {
            try
            {
                var connection = new SQLiteAsyncConnection(new SQLiteConnectionString(path, Flags, true));

                try
                {
                    await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master");
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task MigrateToEncryptedAsync(string path, string password)
        {
            var plain = new SQLiteAsyncConnection(new SQLiteConnectionString(path, Flags, true));
            List<Authenticator> authenticators;
            List<Category> categories;
            List<AuthenticatorCategory> bindings;
            List<CustomIcon> customIcons;
            List<IconPack> iconPacks;
            List<IconPackEntry> iconPackEntries;

            try
            {
                authenticators = await plain.Table<Authenticator>().ToListAsync();
                categories = await plain.Table<Category>().ToListAsync();
                bindings = await plain.Table<AuthenticatorCategory>().ToListAsync();
                customIcons = await plain.Table<CustomIcon>().ToListAsync();
                iconPacks = await plain.Table<IconPack>().ToListAsync();
                iconPackEntries = await plain.Table<IconPackEntry>().ToListAsync();
            }
            finally
            {
                await plain.CloseAsync();
            }

            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");

            var encrypted = new SQLiteAsyncConnection(new SQLiteConnectionString(path, Flags, true, password));
            await encrypted.EnableWriteAheadLoggingAsync();
            await encrypted.CreateTableAsync<Authenticator>();
            await encrypted.CreateTableAsync<Category>();
            await encrypted.CreateTableAsync<AuthenticatorCategory>();
            await encrypted.CreateTableAsync<CustomIcon>();
            await encrypted.CreateTableAsync<IconPack>();
            await encrypted.CreateTableAsync<IconPackEntry>();

            if (authenticators.Any()) await encrypted.InsertAllAsync(authenticators);
            if (categories.Any()) await encrypted.InsertAllAsync(categories);
            if (bindings.Any()) await encrypted.InsertAllAsync(bindings);
            if (customIcons.Any()) await encrypted.InsertAllAsync(customIcons);
            if (iconPacks.Any()) await encrypted.InsertAllAsync(iconPacks);
            if (iconPackEntries.Any()) await encrypted.InsertAllAsync(iconPackEntries);

            await encrypted.CloseAsync();
        }
    }
}
