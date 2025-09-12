namespace IndexerLib.Index
{
    using System;
    using System.Data.SQLite;
    using System.IO;

    /// <summary>
    /// Provides a simple SQLite-based mapping between string names and auto-incrementing IDs.
    /// Useful for assigning persistent IDs to paths or other unique strings.
    /// </summary>
    public class DocIdStore : IndexerBase, IDisposable
    {
        private readonly SQLiteConnection _connection; // Active SQLite connection
        readonly string _otzariaPath;

        /// <summary>
        /// Initializes the IdStore by ensuring the database and table exist.
        /// </summary>
        public DocIdStore(string otzariaPath = "C:\\")
        {
            if (!otzariaPath.EndsWith("\\"))
                otzariaPath += "\\";
            _otzariaPath = otzariaPath;

            // If the DB file doesn't exist, create it (and containing folder if needed)
            if (!File.Exists(DocIdStorePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DocIdStorePath));
                SQLiteConnection.CreateFile(DocIdStorePath);
            }

            // Open a connection to the SQLite database
            _connection = new SQLiteConnection($"Data Source={DocIdStorePath};Version=3;");
            _connection.Open();

            // Ensure required table exists
            EnsureTable();
        }

        /// <summary>
        /// Creates the IdStore table if it does not already exist.
        /// Table contains an auto-increment primary key (Id) and a unique Name field.
        /// </summary>
        private void EnsureTable()
        {
            string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS IdStore (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL
            );";

            using (var command = new SQLiteCommand(createTableQuery, _connection))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Adds a new name to the IdStore, or returns the existing ID if the name is already stored.
        /// </summary>
        /// <param name="name">The string to store.</param>
        /// <returns>The integer ID associated with the given name.</returns>
        public int Add(string path)
        {
            //ensure relative path
            path = path.Replace(_otzariaPath, "").Trim('\\');

            // Check if name already exists in the table
            int existingId = GetIdByPath(path);
            if (existingId != -1)
                return existingId;

            // Otherwise, insert new row with the given name
            string insertQuery = "INSERT INTO IdStore (Name) VALUES (@Name);";
            using (var command = new SQLiteCommand(insertQuery, _connection))
            {
                command.Parameters.AddWithValue("@Name", path);
                command.ExecuteNonQuery();
            }

            // Return the newly assigned auto-incremented ID
            return (int)_connection.LastInsertRowId;
        }

        /// <summary>
        /// Retrieves the ID for a given name if it exists.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The ID if found, otherwise -1.</returns>
        public int GetIdByPath(string path)
        {
            //ensure relative path
            path = path.Replace(_otzariaPath, "");

            string selectQuery = "SELECT Id FROM IdStore WHERE Name = @Name;";
            using (var command = new SQLiteCommand(selectQuery, _connection))
            {
                command.Parameters.AddWithValue("@Name", path);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return reader.GetInt32(0); // Return the ID column value
                }
            }
            return -1; // Not found
        }

        /// <summary>
        /// Retrieves the stored name for a given ID.
        /// </summary>
        /// <param name="id">The ID to search for.</param>
        /// <returns>The name if found, otherwise null.</returns>
        public string GetPathById(int id)
        {
            string selectQuery = "SELECT Name FROM IdStore WHERE Id = @Id;";
            using (var command = new SQLiteCommand(selectQuery, _connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return _otzariaPath + reader.GetString(0); // Return the Name column value appended to otzria path
                }
            }
            return null; // Not found
        }

        /// <summary>
        /// Properly disposes the SQLite connection.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
