using System.Data.SQLite;
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
                "Echo","Data",
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

        private void InitializeDatabase()
        {
            SQLiteConnection.CreateFile(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo","Data",
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

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}