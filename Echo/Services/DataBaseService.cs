using Echo.Models;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace Echo.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection _connection;

        public DatabaseService()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo", "Userdata",
                "note.db");

            var dbFolder = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }

            bool needInit = !File.Exists(dbPath);
            _connectionString = $"Data Source={dbPath};Version=3;";

            if (needInit)
            {
                InitializeDatabase();
            }
        }

        public static void CreateDatabaseFile()
        {
            // 构造数据库文件的完整路径
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo", "Userdata", "DataBase", "note.db");

            // 获取数据库文件夹路径
            string dbDirectory = Path.GetDirectoryName(dbPath);

            if (dbDirectory == null)
            {
                throw new Exception("Invalid database directory path.");
            }

            // 检查文件夹是否存在，如果不存在则创建
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            // 检查数据库文件是否已经存在，如果不存在则创建
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                Console.WriteLine($"Database file created at: {dbPath}");
            }
            else
            {
                Console.WriteLine($"Database file already exists at: {dbPath}");
            }
        }

        private void InitializeDatabase()
        {
            CreateDatabaseFile();
            SQLiteConnection.CreateFile(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo","Userdata","DataBase",
                "note.db"));

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    // 创建表
                    command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Words (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT, 
                Word            TEXT NOT NULL,
                Type            TEXT,
                SentenceId      INTEGER DEFAULT 0,
                SourceId        INTEGER DEFAULT 0,
                LanguageCode    TEXT,
                Status          INTEGER DEFAULT 0,
                IsDeleted       BOOLEAN DEFAULT 0,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(Word, LanguageCode),
                FOREIGN KEY(SentenceId) REFERENCES Sentences(Id),
                FOREIGN KEY(SourceId) REFERENCES Sources(Id)
            );

            CREATE TABLE IF NOT EXISTS Definitions (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId          INTEGER NOT NULL,
                Definition      TEXT NOT NULL,
                PartOfSpeech    TEXT,
                Tense           TEXT,
                IsDeleted       BOOLEAN DEFAULT 0,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(WordId) REFERENCES Words(Id),
                UNIQUE(WordId, Definition)
            );

            CREATE TABLE IF NOT EXISTS Phonetics (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId          INTEGER NOT NULL,
                Phonetic        TEXT NOT NULL,
                Accent          TEXT,
                AudioFilePath   TEXT,
                IsDeleted       BOOLEAN DEFAULT 0,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(WordId) REFERENCES Words(Id),
                UNIQUE(WordId, Phonetic)
            );

            CREATE TABLE IF NOT EXISTS Examples (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId          INTEGER NOT NULL,
                ExampleText     TEXT NOT NULL,
                Translation     TEXT,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                DefinitionID    INTEGER,
                FOREIGN KEY(DefinitionID) REFERENCES Definitions(Id),
                FOREIGN KEY(WordId) REFERENCES Words(Id)
            );

            CREATE TABLE IF NOT EXISTS Sentences (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Sentence        TEXT NOT NULL,
                Translation     TEXT,
                AudioFilePath   TEXT,
                LanguageCode    TEXT,
                Analysis        TEXT,
                IsDeleted       BOOLEAN DEFAULT 0,
                SourceId        INTEGER,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(SourceId) REFERENCES Sources(Id),
                UNIQUE(Sentence, LanguageCode)
            );

            CREATE TABLE IF NOT EXISTS Collections (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT NOT NULL,
                IsDeleted   BOOLEAN DEFAULT 0,
                CreateTime  DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime  DATETIME DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(Name)
            );

            CREATE TABLE IF NOT EXISTS WordSentenceCollectionLink (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId         INTEGER NOT NULL,
                CollectionId   INTEGER NOT NULL,
                SentenceId     INTEGER NOT NULL,
                CreateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(WordId) REFERENCES Words(Id),
                FOREIGN KEY(CollectionId) REFERENCES Collections(Id),
                FOREIGN KEY(SentenceId) REFERENCES Sentences(Id)
            );

            CREATE TABLE IF NOT EXISTS Sources (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                SourceFileName  TEXT,
                SourceFilePath  TEXT,
                SourceTimeStart TIME,
                SourceTimeEnd   TIME,
                CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                IsDeleted       BOOLEAN DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS SourceWordSentenceLink (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                WordId         INTEGER NOT NULL,
                SentenceId     INTEGER NOT NULL,
                SourceId       INTEGER NOT NULL,
                IsDeleted      BOOLEAN DEFAULT 0,
                CreateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(WordId) REFERENCES Words(Id),
                FOREIGN KEY(SentenceId) REFERENCES Sentences(Id),
                FOREIGN KEY(SourceId) REFERENCES Sources(Id),
                UNIQUE(WordId, SentenceId, SourceId)
            );

            CREATE INDEX IF NOT EXISTS idx_words_lang ON Words(LanguageCode);                    

            -- 触发器
            CREATE TRIGGER IF NOT EXISTS trg_update_words
            AFTER UPDATE ON Words
            FOR EACH ROW
            BEGIN
                UPDATE Words
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_definitions
            AFTER UPDATE ON Definitions
            FOR EACH ROW
            BEGIN
                UPDATE Definitions
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_phonetics
            AFTER UPDATE ON Phonetics
            FOR EACH ROW
            BEGIN
                UPDATE Phonetics
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_examples
            AFTER UPDATE ON Examples
            FOR EACH ROW
            BEGIN
                UPDATE Examples
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_sentences
            AFTER UPDATE ON Sentences
            FOR EACH ROW
            BEGIN
                UPDATE Sentences
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_collections
            AFTER UPDATE ON Collections
            FOR EACH ROW
            BEGIN
                UPDATE Collections
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_word_sentence_collection_link
            AFTER UPDATE ON WordSentenceCollectionLink
            FOR EACH ROW
            BEGIN
                UPDATE WordSentenceCollectionLink
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_sources
            AFTER UPDATE ON Sources
            FOR EACH ROW
            BEGIN
                UPDATE Sources
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_update_source_word_sentence_link
            AFTER UPDATE ON SourceWordSentenceLink
            FOR EACH ROW
            BEGIN
                UPDATE SourceWordSentenceLink
                SET UpdateTime = CURRENT_TIMESTAMP
                WHERE Id = OLD.Id;
            END;
        ";
                    command.ExecuteNonQuery();
                }
            }
        }

        private SQLiteConnection GetConnection()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public async Task SaveOrUpdateWordAsync(WordModel word)
        {
            if (word == null || string.IsNullOrWhiteSpace(word.Word))
                throw new ArgumentException("Invalid word model provided.");

            try
            {
                using var connection = GetConnection();
                using var transaction = connection.BeginTransaction();

                // 保存或更新 Words 表
                var wordId = await InsertOrUpdateWordAsync(connection, word);

                // 保存或更新 Definitions 表
                if (word.Definitions != null && word.Definitions.Any())
                {
                    await InsertOrUpdateDefinitionsAsync(connection, wordId, word.Definitions);
                }

                // 保存或更新 Phonetics 表
                if (word.Pronounciations != null && word.Pronounciations.Any())
                {
                    await InsertOrUpdatePhoneticsAsync(connection, wordId, word.Pronounciations);
                }

                // 保存或更新 Examples 表
                if (word.Senses != null && word.Senses.Any())
                {
                    await InsertOrUpdateExamplesAsync(connection, wordId, word.Senses);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving word: {ex.Message}");
                throw;
            }
        }

        private async Task<int> InsertOrUpdateWordAsync(SQLiteConnection connection, WordModel word)
        {
            var command = new SQLiteCommand(@"
        INSERT INTO Words (Word, Status, IsDeleted, CreateTime, UpdateTime)
        VALUES (@Word, @Status, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        ON CONFLICT(Word, LanguageCode)
        DO UPDATE SET 
            Status = @Status,
            UpdateTime = CURRENT_TIMESTAMP;

        SELECT Id FROM Words WHERE Word = @Word;
    ", connection);

            command.Parameters.AddWithValue("@Word", word.Word);
            command.Parameters.AddWithValue("@Status", word.IsFavorite ? 1 : 0);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result); // 返回 WordId
        }

        private async Task InsertOrUpdateDefinitionsAsync(
            SQLiteConnection connection,
            int wordId,
            Dictionary<string, string> definitions)
        {
            foreach (var definition in definitions)
            {
                var command = new SQLiteCommand(@"
            INSERT INTO Definitions (WordId, Definition, PartOfSpeech, IsDeleted, CreateTime, UpdateTime)
            VALUES (@WordId, @Definition, @PartOfSpeech, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT(WordId, Definition)
            DO UPDATE SET 
                PartOfSpeech = @PartOfSpeech,
                UpdateTime = CURRENT_TIMESTAMP;
        ", connection);

                command.Parameters.AddWithValue("@WordId", wordId);
                command.Parameters.AddWithValue("@Definition", definition.Value);
                command.Parameters.AddWithValue("@PartOfSpeech", definition.Key);

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertOrUpdatePhoneticsAsync(
            SQLiteConnection connection,
            int wordId,
            List<PronunciationModel> pronunciations)
        {
            foreach (var pronunciation in pronunciations)
            {
                var command = new SQLiteCommand(@"
            INSERT INTO Phonetics (WordId, Phonetic, Accent, AudioFilePath, IsDeleted, CreateTime, UpdateTime)
            VALUES (@WordId, @Phonetic, @Accent, @AudioFilePath, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT(WordId, Phonetic)
            DO UPDATE SET 
                Accent = @Accent,
                AudioFilePath = @AudioFilePath,
                UpdateTime = CURRENT_TIMESTAMP;
        ", connection);

                command.Parameters.AddWithValue("@WordId", wordId);
                command.Parameters.AddWithValue("@Phonetic", pronunciation.PhoneticSpelling);
                command.Parameters.AddWithValue("@Accent", pronunciation.Dialect);
                command.Parameters.AddWithValue("@AudioFilePath", pronunciation.AudioFile);

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertOrUpdateExamplesAsync(
            SQLiteConnection connection,
            int wordId,
            List<SenseModel> senses)
        {
            foreach (var sense in senses)
            {
                if (sense.Examples == null || !sense.Examples.Any())
                {
                    continue; // 跳过没有例句的 sense
                }
                foreach (var example in sense.Examples)
                {
                    var command = new SQLiteCommand(@"
                INSERT INTO Examples (WordId, ExampleText, Translation, CreateTime, UpdateTime)
                VALUES (@WordId, @ExampleText, @Translation, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT(WordId, ExampleText)
                DO UPDATE SET 
                    Translation = @Translation,
                    UpdateTime = CURRENT_TIMESTAMP;
            ", connection);

                    command.Parameters.AddWithValue("@WordId", wordId);
                    command.Parameters.AddWithValue("@ExampleText", example.Key);
                    command.Parameters.AddWithValue("@Translation", example.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}