using Echo.Models;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Transactions;
using System.Windows;

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
                "Echo", "Data", "Database", "note.db");

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

        //create path if not exists
        public static void CreateDatabaseFile()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo", "Data", "Database", "note.db");

            string dbDirectory = Path.GetDirectoryName(dbPath);

            if (dbDirectory == null)
            {
                throw new Exception("Invalid database directory path.");
            }

            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

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
                "Echo", "Data", "Database", "note.db"));

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    //Status 0:未学习 1:已学习 2:已掌握
                    //Type 0:单词 1:短语 2:句子
                    //PartOfSpeech 0:名词 1:动词 2:形容词 3:副词 4:代词 5:介词 6:连词 7:数词 8:感叹词 9:冠词 10:助词 11:缩略词 12:缩写词 13:短语 14:句子
                    //Tense 0:一般现在时 1:一般过去时 2:一般将来时 3:现在进行时 4:过去进行时 5:将来进行时 6:过去完成时 7:将来完成时 8:现在完成进行时 9:过去完成进行时 10:将来完成进行时 11:一般过去完成时 12:一般将来完成时 13:现在完成时 14:过去完成时 15:将来完成时 16:一般将来完成时 17:一般过去完成时 18:现在完成进行时 19:过去完成进行时 20:将来完成进行时 21:一般将来完成进行时 22:一般过去完成进行时 23:现在完成时 24:过去完成时 25:将来完成时 26:一般将来完成时 27:一般过去完成时 28:现在完成进行时 29:过去完成进行时 30:将来完成进行时 31:一般将来完成进行时 32:一般过去完成进行时 33:现在完成时 34:过去完成时 35:将来完成时 36:一般将来完成时 37:一般过去完成时 38:现在完成进行时 39:过去完成进行时 40:将来完成进行时 41:一般将来完成进行时 42:一般过去完成进行时 43:现在完成时 44:过去完成时 45:将来完成时 46:一般将来完成时 47:一般过去完成时 48:现在完成进行时 49:过去完成进行时 50:将来完成进行时 51:一般将来完成进行时 52:一般过去完成进行时 53:现在完成时 54:过去完成时 55:将来完成时 56:一般将来完成时 57:一般过去完成时 58:现在
                    //SourceLanguageCode 

                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Words (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT, 
                        Word            TEXT NOT NULL,
                        Type            TEXT,
                        SentenceId      INTEGER DEFAULT 0,
                        SourceId        INTEGER DEFAULT 0,
                        SourceLanguageCode    TEXT NOT NULL,
                        TargetLanguageCode    TEXT NOT NULL,
                        Infections      TEXT,
                        Status          INTEGER DEFAULT 0,
                        IsDeleted       BOOLEAN DEFAULT 0,
                        CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(Word, SourceLanguageCode),
                        FOREIGN KEY(SentenceId) REFERENCES Sentences(Id),
                        FOREIGN KEY(SourceId) REFERENCES Sources(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Definitions (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        WordId          INTEGER NOT NULL,
                        Definition      TEXT NOT NULL,
                        ExplanationLanguageCode    TEXT,
                        PartOfSpeech    TEXT,
                        Tense           TEXT,
                        IsDeleted       BOOLEAN DEFAULT 0,
                        CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(WordId) REFERENCES Words(Id)
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
                        FOREIGN KEY(WordId) REFERENCES Words(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Examples (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        WordId          INTEGER NOT NULL,
                        DefinitionId    INTEGER,
                        ExampleText     TEXT NOT NULL,
                        Translation     TEXT,
                        CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(DefinitionId) REFERENCES Definitions(Id),
                        FOREIGN KEY(WordId) REFERENCES Words(Id)
                    );

                    CREATE TABLE IF NOT EXISTS Sentences (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        Sentence        TEXT NOT NULL,
                        Translation     TEXT,
                        AudioFilePath   TEXT,
                        SourceLanguageCode    TEXT,
                        TargetLanguageCode    TEXT,
                        Analysis        TEXT,
                        IsDeleted       BOOLEAN DEFAULT 0,
                        SourceId        INTEGER,
                        CreateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(SourceId) REFERENCES Sources(Id),
                        UNIQUE(Sentence, SourceLanguageCode)
                    );

                    CREATE TABLE IF NOT EXISTS Collections (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name        TEXT NOT NULL,
                        SourceFileName TEXT,
                        IsDeleted   BOOLEAN DEFAULT 0,
                        CreateTime  DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime  DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(Name)
                    );

                    CREATE TABLE IF NOT EXISTS WordSentenceCollectionLink (
                        Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                        WordId         INTEGER ,
                        CollectionId   INTEGER NOT NULL,
                        SentenceId     INTEGER ,
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
                        WordId         INTEGER ,
                        SentenceId     INTEGER ,
                        SourceId       INTEGER NOT NULL,
                        IsDeleted      BOOLEAN DEFAULT 0,
                        CreateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdateTime     DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(WordId) REFERENCES Words(Id),
                        FOREIGN KEY(SentenceId) REFERENCES Sentences(Id),
                        FOREIGN KEY(SourceId) REFERENCES Sources(Id),
                        UNIQUE(WordId, SentenceId, SourceId)
                    );

                    CREATE INDEX IF NOT EXISTS idx_words_lang ON Words(SourceLanguageCode);                    

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

            var connection = new SQLiteConnection(_connectionString);
             connection.Open();

            return connection;
        }

        public async Task<int> SaveOrUpdateWordAsync(WordModel word)
        {
            if (word == null || string.IsNullOrWhiteSpace(word.Word))
            //throw new ArgumentException("Invalid word model provided.");
            {
                MessageBox.Show("Invalid word");
                return 0;
            }
            try
            {
                using var connection = GetConnection();
                using var transaction = connection.BeginTransaction();

                // 保存或更新 Sources 表
                var sourceId = await InsertOrUpdateSourceAsync(connection, word);

                // 保存或更新 Words 表
                var wordId = await InsertOrUpdateWordAsync(connection, word);
                //MessageBox.Show($"wordId:{wordId}");

                // 保存或更新 SourceWordSentenceLink 表
                await InsertOrUpdateSourceWordSentenceLink(connection, wordId, sourceId);

                //Debug.WriteLine($"wordId:{wordId}");
                // 保存或更新 Definitions 表
                if (word.Definitions != null && word.Definitions.Any())
                {
                    //Debug.WriteLine($"wordId:{Definitions}");
                    await InsertOrUpdateDefinitionsAsync(connection, wordId, word);
                }

                // 保存或更新 Phonetics 表
                if (word.Pronounciations != null && word.Pronounciations.Any())
                {
                    await InsertOrUpdatePhoneticsAsync(connection, wordId, word.Pronounciations);
                }


                transaction.Commit();
                return wordId;
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Error saving word: {ex.Message}");
                throw;
            }
        }

        private async Task InsertOrUpdateSourceWordSentenceLink(SQLiteConnection connection, int wordId, int sourceId)
        {
            var command = new SQLiteCommand(@"
            INSERT INTO SourceWordSentenceLink (WordId,SourceId)
            VALUES (@WordId,@SourceId );
    ", connection);

            command.Parameters.AddWithValue("@WordId", wordId);
            command.Parameters.AddWithValue("@SourceId", sourceId);

            var result = await command.ExecuteScalarAsync();
            //return Convert.ToInt32(result);
        }

        private async Task<int> InsertOrUpdateSourceAsync(SQLiteConnection connection, WordModel word)
        {
            var command = new SQLiteCommand(@"
            INSERT INTO Sources (SourceFileName, SourceFilePath,SourceTimeStart,SourceTimeEnd)
            VALUES (@SourceFileName, @SourceFilePath, @SourceTimeStart,@SourceTimeEnd );
            SELECT last_insert_rowid();
            ", connection);

            command.Parameters.AddWithValue("@SourceFileName", word.SourceFileName);
            command.Parameters.AddWithValue("@SourceFilePath", word.SourceFilePath);
            command.Parameters.AddWithValue("@SourceTimeStart", word.SourceStartTime);
            command.Parameters.AddWithValue("@SourceTimeEnd", word.SourceEndTime);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result); 
        }

        private async Task InsertOrUpdateDefinitionsAsync(
            SQLiteConnection connection,
            int wordId,
            WordModel word)
        {
            foreach (var sense in word.Senses)
            {
                Debug.WriteLine($"sense.Definition:{sense.Definition}");

                var command = new SQLiteCommand(@"
                INSERT INTO Definitions (WordId, Definition,ExplanationLanguageCode, PartOfSpeech)
                VALUES (@WordId, @Definition,@ExplanationLanguageCode, @PartOfSpeech);
                SELECT last_insert_rowid();
                ", connection);

                command.Parameters.AddWithValue("@WordId", wordId);
                command.Parameters.AddWithValue("@Definition", sense.Definition);
                command.Parameters.AddWithValue("@ExplanationLanguageCode", word.TargetLanguageCode);
                command.Parameters.AddWithValue("@PartOfSpeech", sense.Category);
                var result = await command.ExecuteScalarAsync();
                var definitionId = Convert.ToInt32(result);

                await InsertOrUpdateExamplesAsync(connection,wordId,definitionId, sense);

            }
            foreach (var sense in word.OriginalSenses)
            {

                Debug.WriteLine($"sense.OriginalSenses:{sense.Definition}");
                var command = new SQLiteCommand(@"
                INSERT INTO Definitions (WordId, Definition,ExplanationLanguageCode, PartOfSpeech)
                VALUES (@WordId, @Definition,@ExplanationLanguageCode, @PartOfSpeech);
                SELECT last_insert_rowid();
                ", connection);

                command.Parameters.AddWithValue("@WordId", wordId);
                command.Parameters.AddWithValue("@Definition", sense.Definition);
                command.Parameters.AddWithValue("@ExplanationLanguageCode", word.SourceLanguageCode);
                command.Parameters.AddWithValue("@PartOfSpeech", sense.Category);
                var result = await command.ExecuteScalarAsync();
                var definitionId = Convert.ToInt32(result);

                await InsertOrUpdateExamplesAsync(connection, wordId,definitionId, sense);
            }

        }

        private async Task<int> InsertOrUpdateWordAsync(SQLiteConnection connection, WordModel word)
        {
            var command = new SQLiteCommand(@"
            INSERT INTO Words (Word, SourceLanguageCode,TargetLanguageCode)
            VALUES (@Word, @SourceLanguageCode, @TargetLanguageCode);
            SELECT last_insert_rowid();
            ", connection);

            command.Parameters.AddWithValue("@Word", word.Word);
            command.Parameters.AddWithValue("@SourceLanguageCode", word.SourceLanguageCode );
            command.Parameters.AddWithValue("@TargetLanguageCode", word.TargetLanguageCode);

            var result = await command.ExecuteScalarAsync();
            //Debug.WriteLine($"Convert.ToInt32(result):{Convert.ToInt32(result)}");
            return Convert.ToInt32(result); // 返回 WordId
        }

        private async Task InsertOrUpdatePhoneticsAsync(
            SQLiteConnection connection,
            int wordId,
            List<PronunciationModel> pronunciations)
        {
            foreach (var pronunciation in pronunciations)
            {
                var command = new SQLiteCommand(@"
            INSERT INTO Phonetics (WordId, Phonetic, Accent, AudioFilePath)
            VALUES (@WordId, @Phonetic, @Accent, @AudioFilePath )
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
            int wordId,int definitionId,
            SenseModel sense)
        {
                if (sense.Examples == null || !sense.Examples.Any())
                {
                    return; // 跳过没有例句的 sense
                }
                foreach (var example in sense.Examples)
                {
                    var command = new SQLiteCommand(@"
                INSERT INTO Examples (WordId, DefinitionId, ExampleText, Translation)
                VALUES (@WordId, @DefinitionId,@ExampleText, @Translation)
            ", connection);

                    command.Parameters.AddWithValue("@WordId", wordId);
                command.Parameters.AddWithValue("@DefinitionId", definitionId);
                command.Parameters.AddWithValue("@ExampleText", example.Key);
                    command.Parameters.AddWithValue("@Translation", example.Value);

                    await command.ExecuteNonQueryAsync();
                }
            
        }

        private async Task<int> GetOrCreateCollectionIdAsync(SQLiteConnection connection, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("SourceFileName cannot be null or empty.");

            // 查询是否存在相同的 Name
            var selectCommand = new SQLiteCommand(@"
                SELECT Id FROM Collections WHERE SourceFileName = @SourceFileName;
                 ", connection);

            selectCommand.Parameters.AddWithValue("@SourceFileName", collectionName);

            var result = await selectCommand.ExecuteScalarAsync();

            // 如果存在，直接返回 Id
            if (result != null && int.TryParse(result.ToString(), out int collectionId))
            {
                return collectionId;
            }

            // 如果不存在，插入新记录
            var insertCommand = new SQLiteCommand(@"
                INSERT INTO Collections (Name,SourceFileName)
                VALUES (@Name, @SourceFileName);
                ", connection);
            insertCommand.Parameters.AddWithValue("@Name", collectionName);
            insertCommand.Parameters.AddWithValue("@SourceFileName", collectionName);
            await insertCommand.ExecuteNonQueryAsync();

            // 再次查询并返回新插入行的 Id
            var fetchCommand = new SQLiteCommand(@"
                SELECT Id FROM Collections WHERE Name = @SourceFileName;
                ", connection);

            fetchCommand.Parameters.AddWithValue("@SourceFileName", collectionName);
            result = await fetchCommand.ExecuteScalarAsync();

            if (result != null && int.TryParse(result.ToString(), out collectionId))
            {
                return collectionId;
            }

            throw new Exception("Failed to retrieve or create the collection.");
        }

        private async Task InsertOrUpdateWordSentenceCollectionLinkAsync(SQLiteConnection connection, int collectionId, int wordId)
        {
            var command = new SQLiteCommand(@"
                INSERT INTO WordSentenceCollectionLink (WordId,CollectionId)
                VALUES (@WordId,@CollectionId )
                ", connection);

            command.Parameters.AddWithValue("@WordId", wordId);
            command.Parameters.AddWithValue("@CollectionId", collectionId);

            var result = await command.ExecuteScalarAsync();
            //return Convert.ToInt32(result);
        }

        public async Task<WordModel?> GetWordFromLocalAsync(
            string wordString,
            string sourceLang,
            string targetLang)
        {
            if (string.IsNullOrWhiteSpace(wordString) || string.IsNullOrWhiteSpace(sourceLang) || string.IsNullOrWhiteSpace(targetLang))
                throw new ArgumentException("Word, SourceLanguageCode, and TargetLanguageCode cannot be null or empty.");

            using var connection = GetConnection();

            var wordModel = new WordModel
            {
                Word = wordString,
                SourceLanguageCode = sourceLang,
                TargetLanguageCode = targetLang,
                Pronounciations = new List<PronunciationModel>(),
                Definitions = new Dictionary<string, string>()
            };

            try
            {
                // Step 1: Query Words table to get WordId
                var wordCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Words 
                    WHERE Word = @Word AND SourceLanguageCode = @SourceLanguageCode;
                ", connection);

                wordCommand.Parameters.AddWithValue("@Word", wordString);
                wordCommand.Parameters.AddWithValue("@SourceLanguageCode", sourceLang);

                var wordIdResult = await wordCommand.ExecuteScalarAsync();
                

                if (wordIdResult == null)
                {
                    return null; // Word not found
                }

                int wordId = Convert.ToInt32(wordIdResult);

                // Step 2: Query Phonetics table for Pronunciations
                var phoneticCommand = new SQLiteCommand(@"
                    SELECT Phonetic, Accent, AudioFilePath 
                    FROM Phonetics 
                    WHERE WordId = @WordId AND IsDeleted = 0;
                ", connection);

                phoneticCommand.Parameters.AddWithValue("@WordId", wordId);

                using (var reader = await phoneticCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        wordModel.Pronounciations.Add(new PronunciationModel
                        {
                            PhoneticSpelling = reader.GetString(reader.GetOrdinal("Phonetic")),
                            Dialect = reader["Accent"]?.ToString(),
                            AudioFile = reader["AudioFilePath"]?.ToString()
                            
                        });
                    }
                }

                // Step 3: Query Definitions table for Definitions
                var definitionCommand = new SQLiteCommand(@"
                    SELECT Definition, PartOfSpeech 
                    FROM Definitions 
                    WHERE WordId = @WordId AND ExplanationLanguageCode = @TargetLanguageCode AND IsDeleted = 0;
                ", connection);

                definitionCommand.Parameters.AddWithValue("@WordId", wordId);
                definitionCommand.Parameters.AddWithValue("@TargetLanguageCode", targetLang);

                using (var reader = await definitionCommand.ExecuteReaderAsync())
                {
                    var definitionsByCategory = new Dictionary<string, List<string>>();

                    while (await reader.ReadAsync())
                    {
                        var partOfSpeech = reader["PartOfSpeech"]?.ToString();
                        var definition = reader["Definition"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(partOfSpeech) && !string.IsNullOrWhiteSpace(definition))
                        {
                            if (!definitionsByCategory.ContainsKey(partOfSpeech))
                            {
                                definitionsByCategory[partOfSpeech] = new List<string>();
                            }

                            definitionsByCategory[partOfSpeech].Add(definition);
                        }
                    }

                    // Combine definitions for each part of speech into a single string
                    foreach (var kvp in definitionsByCategory)
                    {
                        wordModel.Definitions[kvp.Key] = string.Join(", ", kvp.Value);
                    }
                }

                return wordModel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching WordModel: {ex.Message}");
                throw;
            }
        }

        public async Task<int> WordExistsAsync(SQLiteConnection connection, WordModel word)
        {
            if (word == null || string.IsNullOrWhiteSpace(word.Word) || string.IsNullOrWhiteSpace(word.SourceLanguageCode))
                throw new ArgumentException("Invalid WordModel: Word and SourceLanguageCode cannot be null or empty.");

            var command = new SQLiteCommand(@"
                    SELECT Id
                    FROM Words
                    WHERE Word = @Word AND SourceLanguageCode = @SourceLanguageCode;
                ", connection);

            command.Parameters.AddWithValue("@Word", word.Word);
            command.Parameters.AddWithValue("@SourceLanguageCode", word.SourceLanguageCode);

            var result = await command.ExecuteScalarAsync();

            return result != null ? Convert.ToInt32(result) : 0; // Return Id if exists, 0 if not
        }

        public async Task<int> CheckAndSaveAsync(WordModel word)
        {

            using var connection = GetConnection();

            var wordIdResult = await WordExistsAsync(connection,word);
            
            int wordId ;
            if ( wordIdResult == 0) // Word does not exist, call the storage function
            {
                // Call the storage function (implementation not shown here)
                wordId = await SaveOrUpdateWordAsync(word); // stores the word and returns its Id
            }
            else
            {
                wordId = Convert.ToInt32(wordIdResult);
            }
            //MessageBox.Show($"wordId:{wordId}");

            return wordId;
        }

        public async Task<bool> CheckCollectionLinkExistAsync(WordModel word)
        {
            
            if (word == null || string.IsNullOrWhiteSpace(word.Word) || string.IsNullOrWhiteSpace(word.SourceLanguageCode) )
                throw new ArgumentException("Invalid input: Word, SourceLanguageCode, and CollectionName cannot be null or empty.");

            using var connection = GetConnection();

            try
            {
                // Step 1: Check if the word exists in the Words table
                var selectWordCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Words 
                    WHERE Word = @Word AND SourceLanguageCode = @SourceLanguageCode;
                ", connection);

                selectWordCommand.Parameters.AddWithValue("@Word", word.Word);
                selectWordCommand.Parameters.AddWithValue("@SourceLanguageCode", word.SourceLanguageCode);

                var wordIdResult = await selectWordCommand.ExecuteScalarAsync();
                //MessageBox.Show(Convert.ToString(wordIdResult));
                if (wordIdResult == null)
                {
                    // Word does not exist, so it cannot be linked to the collection
                    return false;
                }

                int wordId = Convert.ToInt32(wordIdResult);

                // Step 3: Check if the link exists in the WordSentenceCollectionLink table
                var selectLinkCommand = new SQLiteCommand(@"
                    SELECT EXISTS(
                        SELECT 1 
                        FROM WordSentenceCollectionLink 
                        WHERE WordId = @WordId
                    );
                ", connection);

                selectLinkCommand.Parameters.AddWithValue("@WordId", wordId);

                var linkExistsResult = await selectLinkCommand.ExecuteScalarAsync();

                //Debug.WriteLine($"Convert.ToInt32(linkExistsResult):{linkExistsResult}");
                return Convert.ToInt32(linkExistsResult) == 1; // Returns true if the link exists
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking collection link: {ex.Message}");
                throw;
            }
        }

        public async Task CollectionLinkAsync(WordModel word, string collectionName)
        {
            if (word == null)
                throw new ArgumentException("Word cannot be null or empty.");

            var connection = GetConnection();

            var wordId = await CheckAndSaveAsync(word);

            using var transaction = connection.BeginTransaction();

            try
            {
                //  Check if the collection exists, otherwise create it
                var collectionId = await GetOrCreateCollectionIdAsync(connection, collectionName);

                //  Check if the Word and Collection link already exists
                var selectLinkCommand = new SQLiteCommand(@"
                SELECT EXISTS(
                    SELECT 1 
                    FROM WordSentenceCollectionLink 
                    WHERE WordId = @WordId AND CollectionId = @CollectionId
                );
            ", connection);
                selectLinkCommand.Parameters.AddWithValue("@WordId", wordId);
                selectLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                var linkExists = Convert.ToInt32(await selectLinkCommand.ExecuteScalarAsync()) == 1;

                if (!linkExists)
                {
                    // Step 4: Insert the link into WordSentenceCollectionLink
                    var insertLinkCommand = new SQLiteCommand(@"
                INSERT INTO WordSentenceCollectionLink (WordId, CollectionId) 
                VALUES (@WordId, @CollectionId);
            ", connection);
                    insertLinkCommand.Parameters.AddWithValue("@WordId", wordId);
                    insertLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                    await insertLinkCommand.ExecuteNonQueryAsync();
                }

                // Commit the transaction
                transaction.Commit();

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Error while saving to collection: " + ex.Message, ex);
            }
            finally
            {
                connection.Dispose();
            }
        }

        public async Task CollectionLinkAsync(WordModel word, long collectionId)
        {
            if (word == null)
                throw new ArgumentException("Word cannot be null or empty.");

            var connection = GetConnection();

            var wordId = await CheckAndSaveAsync(word);

            using var transaction = connection.BeginTransaction();

            try
            {
                //  Check if the Word and Collection link already exists
                var selectLinkCommand = new SQLiteCommand(@"
                SELECT EXISTS(
                    SELECT 1 
                    FROM WordSentenceCollectionLink 
                    WHERE WordId = @WordId AND CollectionId = @CollectionId
                );
            ", connection);
                selectLinkCommand.Parameters.AddWithValue("@WordId", wordId);
                selectLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                var linkExists = Convert.ToInt32(await selectLinkCommand.ExecuteScalarAsync()) == 1;

                if (!linkExists)
                {
                    // Step 4: Insert the link into WordSentenceCollectionLink
                    var insertLinkCommand = new SQLiteCommand(@"
                INSERT INTO WordSentenceCollectionLink (WordId, CollectionId) 
                VALUES (@WordId, @CollectionId);
            ", connection);
                    insertLinkCommand.Parameters.AddWithValue("@WordId", wordId);
                    insertLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                    await insertLinkCommand.ExecuteNonQueryAsync();
                }

                // Commit the transaction
                transaction.Commit();

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Error while saving to collection: " + ex.Message, ex);
            }
            finally
            {
                connection.Dispose();
            }
        }

        public async Task RemoveCollectionLinkAsync(WordModel word, string collectionName)
        {
            if (word == null || string.IsNullOrWhiteSpace(word.Word) || string.IsNullOrWhiteSpace(word.SourceLanguageCode) || string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Invalid input: Word, SourceLanguageCode, and CollectionName cannot be null or empty.");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Check if the word exists in the Words table


                var wordIdResult = await WordExistsAsync(connection,word);
                
                if (wordIdResult == 0)
                {
                    // Word does not exist, so it cannot have any links
                    return;
                }

                int wordId = Convert.ToInt32(wordIdResult);
                //MessageBox.Show($"wordId {wordId}");
                // Step 2: Check if the collection exists in the Collections table
                var selectCollectionCommand = new SQLiteCommand(@"
                        SELECT Id 
                        FROM Collections 
                        WHERE Name = @CollectionName;
                    ", connection);

                selectCollectionCommand.Parameters.AddWithValue("@CollectionName", collectionName);

                var collectionIdResult = await selectCollectionCommand.ExecuteScalarAsync();

                if (collectionIdResult == null)
                {
                    // Collection does not exist, so the link cannot exist
                    return;
                }

                int collectionId = Convert.ToInt32(collectionIdResult);

                // Step 3: Delete the link from the WordSentenceCollectionLink table
                var deleteLinkCommand = new SQLiteCommand(@"
            DELETE FROM WordSentenceCollectionLink 
            WHERE WordId = @WordId AND CollectionId = @CollectionId;
        ", connection);

                deleteLinkCommand.Parameters.AddWithValue("@WordId", wordId);
                deleteLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                int rowsAffected = await deleteLinkCommand.ExecuteNonQueryAsync();

                // Commit the transaction
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error removing word from collection: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveCollectionLinkAsync(WordModel word, long collectionId)
        {
            if (word == null || word.Id == null || string.IsNullOrWhiteSpace(word.SourceLanguageCode) || collectionId == null)
                throw new ArgumentException("Invalid input: Word, SourceLanguageCode, and CollectionName cannot be null or empty.");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                //  Delete the link from the WordSentenceCollectionLink table
                var deleteLinkCommand = new SQLiteCommand(@"
                    DELETE FROM WordSentenceCollectionLink 
                    WHERE WordId = @WordId AND CollectionId = @CollectionId;
                ", connection);

                deleteLinkCommand.Parameters.AddWithValue("@WordId", word.Id);
                deleteLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                int rowsAffected = await deleteLinkCommand.ExecuteNonQueryAsync();

                // Commit the transaction
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error removing word from collection: {ex.Message}");
                throw;
            }
        }

        public async Task<List<CollectionModel>> GetAllCollections()
        {
            using var connection = GetConnection();
            var command = new SQLiteCommand(@"
                SELECT Id, Name
                FROM Collections
                WHERE IsDeleted = 0;
            ", connection);
            using var reader = await command.ExecuteReaderAsync();
            var collections = new List<CollectionModel>();
            while (await reader.ReadAsync())
            {
                var collection = new CollectionModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name"))
                };
                collections.Add(collection);
            }
            return collections;
        }

        public async Task<List<WordBasicModel>> GetAllWordsBasicAsync(long CollectId = 0)
        {
            var words = new List<WordBasicModel>();

            using var connection = GetConnection();

            // SQL 查询，动态根据 CollectionId 是否为 0 设置条件
            var sql = @"
                SELECT 
                    w.Id, 
                    w.Word, 
                    wscl.CollectionId
                FROM Words w
                INNER JOIN WordSentenceCollectionLink wscl 
                    ON wscl.WordId = w.Id
                WHERE w.IsDeleted = 0
                  AND (@CollectionId = 0 OR wscl.CollectionId = @CollectionId)
                ORDER BY wscl.UpdateTime DESC;
    ";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@CollectionId", CollectId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var wordModel = new WordBasicModel
                {
                    Word = reader["Word"]?.ToString(),
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    CollectionId = reader.GetInt64(reader.GetOrdinal("CollectionId"))
                };
                words.Add(wordModel);
            }

            return words;
        }

        public async Task<WordModel> GetWordDetailsAsync(long wordId)
        {
            var wordModel = new WordModel
            {
                Pronounciations = new List<PronunciationModel>(),
                // 如果还需保持原先的 Definitions 字典，可留着；否则可移除
                Definitions = new Dictionary<string, string>(),
                Senses = new List<SenseModel>()
            };

            using var connection = GetConnection();

            // Step 1: 从 Words 表中获取单词基本信息
            var wordCommand = new SQLiteCommand(@"
                SELECT Word, SourceLanguageCode, TargetLanguageCode, IsDeleted
                FROM Words
                WHERE Id = @WordId;
            ", connection);

            wordCommand.Parameters.AddWithValue("@WordId", wordId);

            using var wordReader = await wordCommand.ExecuteReaderAsync();
            if (await wordReader.ReadAsync())
            {
                wordModel.Word = wordReader["Word"]?.ToString();
                wordModel.SourceLanguageCode = wordReader["SourceLanguageCode"]?.ToString();
                wordModel.TargetLanguageCode = wordReader["TargetLanguageCode"]?.ToString();

                var isDeleted = wordReader.GetBoolean(wordReader.GetOrdinal("IsDeleted"));
                if (isDeleted)
                {
                    // 已标记删除，视需求处理或直接返回 null
                }
            }
            else
            {
                // 没有找到该单词
                return null;
            }

            // Step 2: 从 Phonetics 表中获取发音信息
            var phoneticCommand = new SQLiteCommand(@"
        SELECT Phonetic, Accent, AudioFilePath
        FROM Phonetics
        WHERE WordId = @WordId AND IsDeleted = 0;
    ", connection);
            phoneticCommand.Parameters.AddWithValue("@WordId", wordId);

            using var phoneticReader = await phoneticCommand.ExecuteReaderAsync();
            while (await phoneticReader.ReadAsync())
            {
                var pronModel = new PronunciationModel
                {
                    PhoneticSpelling = phoneticReader["Phonetic"]?.ToString() ?? "",
                    Dialect = phoneticReader["Accent"]?.ToString() ?? "",
                    AudioFile = phoneticReader["AudioFilePath"]?.ToString() ?? ""
                };
                wordModel.Pronounciations.Add(pronModel);
            }

            // Step 3: 从 Definitions 表中获取释义，构造 SenseModel
            //         key: definitionId, value: SenseModel
            // 将同语言的放一起
            var definitionDict = new Dictionary<int, SenseModel>();

            var definitionCommand = new SQLiteCommand(@"
        SELECT Id, PartOfSpeech, Definition, ExplanationLanguageCode
        FROM Definitions
        WHERE WordId = @WordId AND IsDeleted = 0
            ORDER BY 
        CASE WHEN ExplanationLanguageCode = @SrcLang THEN 0 ELSE 1 END,
        PartOfSpeech;
    ", connection);
            definitionCommand.Parameters.AddWithValue("@WordId", wordId);
            definitionCommand.Parameters.AddWithValue("@SrcLang", wordModel.TargetLanguageCode);

            using var definitionReader = await definitionCommand.ExecuteReaderAsync();
            while (await definitionReader.ReadAsync())
            {
                var defId = definitionReader.GetInt32(definitionReader.GetOrdinal("Id"));
                var category = definitionReader["PartOfSpeech"]?.ToString() ?? "Unknown";
                var def = definitionReader["Definition"]?.ToString() ?? "";
                var expLang = definitionReader["ExplanationLanguageCode"]?.ToString() ?? "";

                //var tense = definitionReader["Tense"]?.ToString() ?? "";

                var sense = new SenseModel
                {
                    ExplanationLanguageCode = expLang,
                    Category = category,
                    Definition = def,
                    //Description = tense,
                    Examples = new Dictionary<string, string>()
                };

                definitionDict[defId] = sense;
                wordModel.Senses.Add(sense);
            }

            // Step 4: 从 Examples 表中获取例句，按 DefinitionId 加到对应的 SenseModel.Examples
            var exampleCommand = new SQLiteCommand(@"
                SELECT DefinitionId, ExampleText, Translation
                FROM Examples
                WHERE WordId = @WordId
                  AND DefinitionId IS NOT NULL
                ORDER BY Id;
            ", connection);
            exampleCommand.Parameters.AddWithValue("@WordId", wordId);

            using var exampleReader = await exampleCommand.ExecuteReaderAsync();
            while (await exampleReader.ReadAsync())
            {
                var defId = exampleReader.GetInt32(exampleReader.GetOrdinal("DefinitionId"));
                var text = exampleReader["ExampleText"]?.ToString() ?? "";
                var translation = exampleReader["Translation"]?.ToString() ?? "";

                if (definitionDict.TryGetValue(defId, out var sense))
                {
                    // 添加到该Sense的 Examples 字典
                    if (sense.Examples == null)
                        sense.Examples = new Dictionary<string, string>();

                    // Key: 例句文本, Value: 翻译
                    sense.Examples[text] = translation;
                }
            }
            //string json = System.Text.Json.JsonSerializer.Serialize(wordModel, new JsonSerializerOptions
            //{
            //    WriteIndented = true   // 缩进美化输出
            //});
            //Debug.WriteLine(json);
            return wordModel;
        }

        public async Task DeleteCollectionAsync(long CollectionId)
        {
            using var connection = GetConnection();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Step 1: 删除 WordSentenceCollectionLink 表中对应的记录
                var deleteLinkCommand = new SQLiteCommand(@"
            DELETE FROM WordSentenceCollectionLink
            WHERE CollectionId = @CollectionId;
        ", connection);
                deleteLinkCommand.Parameters.AddWithValue("@CollectionId", CollectionId);
                await deleteLinkCommand.ExecuteNonQueryAsync();

                // Step 2: 删除 Collections 表中对应的记录
                var deleteCollectionCommand = new SQLiteCommand(@"
            DELETE FROM Collections
            WHERE Id = @CollectionId;
        ", connection);
                deleteCollectionCommand.Parameters.AddWithValue("@CollectionId", CollectionId);
                await deleteCollectionCommand.ExecuteNonQueryAsync();

                // 提交事务
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();
                Console.WriteLine($"Error deleting collection: {ex.Message}");
                throw;
            }
        }


        public async Task<SentenceModel?> GetOrSaveSentenceAsync(SentenceModel sentenceModel)
        {
            if (sentenceModel == null || string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子模型不能为空");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 先检查句子是否存在
                var checkCommand = new SQLiteCommand(@"
            SELECT Id, Sentence, Translation, TargetLanguageCode 
            FROM Sentences 
            WHERE Sentence = @Sentence 
            AND SourceLanguageCode = @SourceLanguageCode 
            AND TargetLanguageCode = @TargetLanguageCode;
        ", connection);

                checkCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                checkCommand.Parameters.AddWithValue("@SourceLanguageCode", sentenceModel.SourceLanguageCode);
                checkCommand.Parameters.AddWithValue("@TargetLanguageCode", sentenceModel.TargetLanguageCode);

                using var reader = await checkCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // 如果存在，直接返回现有数据
                    return new SentenceModel
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        Sentence = reader.GetString(reader.GetOrdinal("Sentence")),
                        Translation = reader.GetString(reader.GetOrdinal("Translation")),
                        TargetLanguageCode = reader.GetString(reader.GetOrdinal("TargetLanguageCode"))
                    };
                }

                // 如果不存在，插入新句子
                var insertCommand = new SQLiteCommand(@"
                    INSERT INTO Sentences (
                        Sentence, 
                        Translation, 
                        SourceLanguageCode,
                        TargetLanguageCode
                    ) VALUES (
                        @Sentence,
                        @Translation,
                        @SourceLanguageCode,
                        @TargetLanguageCode
                    );
                    SELECT last_insert_rowid();
                ", connection);

                insertCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                insertCommand.Parameters.AddWithValue("@Translation", sentenceModel.Translation);
                insertCommand.Parameters.AddWithValue("@SourceLanguageCode", sentenceModel.SourceLanguageCode);
                insertCommand.Parameters.AddWithValue("@TargetLanguageCode", sentenceModel.TargetLanguageCode);

                var newId = Convert.ToInt64(await insertCommand.ExecuteScalarAsync());

                Debug.WriteLine($"newId {newId}");

                transaction.Commit();

                // 返回新插入的数据，包含新的ID
                sentenceModel.Id = newId;
                return sentenceModel;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"保存句子时发生错误: {ex.Message}", ex);
            }
        }

        public async Task RemoveSentenceAsync(SentenceModel sentenceModel)
        {
            if (string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 先获取句子ID
                var selectCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Sentences 
                    WHERE Sentence = @Sentence 
                    AND SourceLanguageCode = @SourceLanguage
                    AND TargetLanguageCode = @TargetLanguage;
                ", connection);

                selectCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                selectCommand.Parameters.AddWithValue("@SourceLanguage", sentenceModel.SourceLanguageCode);
                selectCommand.Parameters.AddWithValue("@TargetLanguage", sentenceModel.TargetLanguageCode);

                var sentenceId = await selectCommand.ExecuteScalarAsync();

                if (sentenceId == null)
                {
                    throw new Exception("未找到要删除的句子");
                }

                // 删除 WordSentenceCollectionLink 中的关联记录
                var deleteLinkCommand = new SQLiteCommand(@"
                    DELETE FROM WordSentenceCollectionLink 
                    WHERE SentenceId = @SentenceId;
                ", connection);

                deleteLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);
                await deleteLinkCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"删除句子关联记录时发生错误: {ex.Message}", ex);
            }
        }

        public async Task<SentenceModel> GetSentenceAsync(string sentence,string SourceLanguageCode,string TargetLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();

            var command = new SQLiteCommand(@"
                SELECT 
                    Id,
                    Sentence,
                    Translation,
                    SourceLanguageCode,
                    TargetLanguageCode
                FROM Sentences 
                WHERE Sentence = @Sentence 
                AND SourceLanguageCode = @SourceLanguageCode
                AND TargetLanguageCode = @TargetLanguageCode;
            ", connection);

            command.Parameters.AddWithValue("@Sentence", sentence);
            command.Parameters.AddWithValue("@SourceLanguageCode", SourceLanguageCode);
            command.Parameters.AddWithValue("@TargetLanguageCode", TargetLanguageCode);

            try
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new SentenceModel
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("Id")),
                        Sentence = reader.GetString(reader.GetOrdinal("Sentence")),
                        Translation = reader["Translation"]?.ToString(),
                        //AudioFilePath = reader["AudioFilePath"]?.ToString(),
                        SourceLanguageCode = reader["SourceLanguageCode"]?.ToString(),
                        TargetLanguageCode = reader["TargetLanguageCode"]?.ToString(),
                        //Analysis = reader["Analysis"]?.ToString()
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"查询句子时发生错误: {ex.Message}", ex);
            }
        }


        public async Task CollectSentenceAsync(SentenceModel sentenceModel, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 获取句子ID
                var selectSentenceCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Sentences 
                    WHERE Sentence = @Sentence 
                    AND SourceLanguageCode = @SourceLanguage 
                    AND TargetLanguageCode = @TargetLanguage ;
                     ", connection);

                selectSentenceCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                selectSentenceCommand.Parameters.AddWithValue("@SourceLanguage", sentenceModel.SourceLanguageCode);
                selectSentenceCommand.Parameters.AddWithValue("@TargetLanguage", sentenceModel.TargetLanguageCode);

                var sentenceId = await selectSentenceCommand.ExecuteScalarAsync();

                if (sentenceId == null)
                {
                    throw new Exception("未找到要收藏的句子");
                }

                // 检查是否已经存在收藏记录
                var checkLinkCommand = new SQLiteCommand(@"
                    SELECT COUNT(1) 
                    FROM WordSentenceCollectionLink 
                    WHERE SentenceId = @SentenceId;
                ", connection);

                checkLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);

                var existingCount = Convert.ToInt32(await checkLinkCommand.ExecuteScalarAsync());

                if (existingCount > 0)
                {
                    return; // 已经收藏过，直接返回
                }

                var collectionId = await GetOrCreateCollectionIdAsync(connection, collectionName);

                // 创建新的收藏记录
                var insertLinkCommand = new SQLiteCommand(@"
                    INSERT INTO WordSentenceCollectionLink (
                        SentenceId,
                        CollectionId
                    ) VALUES (
                        @SentenceId,
                        @CollectionId
                    );
                ", connection);

                insertLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);
                insertLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId); // 默认收藏集ID为1

                await insertLinkCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"收藏句子时发生错误: {ex.Message}", ex);
            }
        }

        public async Task RemoveSentenceFromCollectionAsync(SentenceModel sentenceModel, long collectionId)
        {
            if (sentenceModel == null || string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 根据句子文本和语言信息查找对应句子的ID
                var selectSentenceCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Sentences 
                    WHERE Sentence = @Sentence 
                      AND SourceLanguageCode = @SourceLanguage 
                      AND TargetLanguageCode = @TargetLanguage;
                ", connection);

                selectSentenceCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                selectSentenceCommand.Parameters.AddWithValue("@SourceLanguage", sentenceModel.SourceLanguageCode);
                selectSentenceCommand.Parameters.AddWithValue("@TargetLanguage", sentenceModel.TargetLanguageCode);

                var sentenceId = await selectSentenceCommand.ExecuteScalarAsync();
                if (sentenceId == null)
                {
                    throw new Exception("未找到对应的句子，无法删除收藏记录");
                }

                // 删除收藏记录：根据句子ID和集合ID删除关联记录
                var deleteLinkCommand = new SQLiteCommand(@"
                    DELETE FROM WordSentenceCollectionLink 
                    WHERE SentenceId = @SentenceId AND CollectionId = @CollectionId;
                ", connection);

                deleteLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);
                deleteLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                await deleteLinkCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"删除句子收藏记录时发生错误: {ex.Message}", ex);
            }
        }

        public async Task CollectSentenceAsync(SentenceModel sentenceModel, long collectionId)
        {
            if (sentenceModel == null || string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 获取句子ID
                var selectSentenceCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Sentences 
                    WHERE Sentence = @Sentence 
                      AND SourceLanguageCode = @SourceLanguage 
                      AND TargetLanguageCode = @TargetLanguage;
                ", connection);

                selectSentenceCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                selectSentenceCommand.Parameters.AddWithValue("@SourceLanguage", sentenceModel.SourceLanguageCode);
                selectSentenceCommand.Parameters.AddWithValue("@TargetLanguage", sentenceModel.TargetLanguageCode);

                var sentenceId = await selectSentenceCommand.ExecuteScalarAsync();

                if (sentenceId == null)
                {
                    throw new Exception("未找到要收藏的句子");
                }

                // 检查当前集合中是否已存在该句子的收藏记录
                var checkLinkCommand = new SQLiteCommand(@"
                    SELECT COUNT(1) 
                    FROM WordSentenceCollectionLink 
                    WHERE SentenceId = @SentenceId AND CollectionId = @CollectionId;
                ", connection);

                checkLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);
                checkLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                var existingCount = Convert.ToInt32(await checkLinkCommand.ExecuteScalarAsync());

                if (existingCount > 0)
                {
                    // 已经收藏过该句子在该集合中，直接返回
                    return;
                }

                // 插入新的收藏记录
                var insertLinkCommand = new SQLiteCommand(@"
                    INSERT INTO WordSentenceCollectionLink (
                        SentenceId,
                        CollectionId
                    ) VALUES (
                        @SentenceId,
                        @CollectionId
                    );
                ", connection);

                insertLinkCommand.Parameters.AddWithValue("@SentenceId", sentenceId);
                insertLinkCommand.Parameters.AddWithValue("@CollectionId", collectionId);

                await insertLinkCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"收藏句子时发生错误: {ex.Message}", ex);
            }
        }

        public async Task<long?> CheckSentenceCollectedAsync(SentenceModel sentenceModel)
        {
            if (sentenceModel == null || string.IsNullOrWhiteSpace(sentenceModel.Sentence))
                throw new ArgumentException("句子不能为空");

            using var connection = GetConnection();

            try
            {
                // 首先获取句子的 ID
                var selectSentenceCommand = new SQLiteCommand(@"
                    SELECT Id 
                    FROM Sentences 
                    WHERE Sentence = @Sentence 
                      AND SourceLanguageCode = @SourceLanguage 
                      AND TargetLanguageCode = @TargetLanguage
                      AND IsDeleted = 0;
                ", connection);

                selectSentenceCommand.Parameters.AddWithValue("@Sentence", sentenceModel.Sentence);
                selectSentenceCommand.Parameters.AddWithValue("@SourceLanguage", sentenceModel.SourceLanguageCode);
                selectSentenceCommand.Parameters.AddWithValue("@TargetLanguage", sentenceModel.TargetLanguageCode);

                var sentenceId = await selectSentenceCommand.ExecuteScalarAsync();
                if (sentenceId == null)
                {
                    // 句子不存在
                    return null;
                }

                // 查询收藏记录，返回第一个匹配的 CollectionId
                var selectCollectionCommand = new SQLiteCommand(@"
                    SELECT CollectionId 
                    FROM WordSentenceCollectionLink 
                    WHERE SentenceId = @SentenceId
                    LIMIT 1;
                ", connection);
                selectCollectionCommand.Parameters.AddWithValue("@SentenceId", sentenceId);

                var collectionIdObject = await selectCollectionCommand.ExecuteScalarAsync();
                if (collectionIdObject != null)
                {
                    return Convert.ToInt64(collectionIdObject);
                }

                // 如果没有收藏记录，则返回 null
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"检查句子收藏状态时发生错误: {ex.Message}", ex);
            }
        }

        public async Task<List<SentenceModel>> GetAllSentenceAsync(int CollectionId)
        {
            var sentences = new List<SentenceModel>();
            using var connection = GetConnection();
            string sql;
            if (CollectionId == 0)
            {
                // 如果 CollectionId 为 0，则查询所有在WordSentenceCollectionLink中关联的句子，
                // 前提是该表中的SentenceId不为0（即存在句子关联记录）
                sql = @"
                    SELECT s.Id, s.Sentence, s.Translation, s.SourceLanguageCode, s.TargetLanguageCode, s.AudioFilePath
                    FROM Sentences s
                    INNER JOIN WordSentenceCollectionLink wscl ON wscl.SentenceId = s.Id
                    WHERE wscl.SentenceId IS NOT NULL AND wscl.SentenceId <> 0
                    ORDER BY wscl.UpdateTime DESC;";
            }
            else
            {
                // 根据传入的CollectionId查询关联的句子记录
                sql = @"
                    SELECT s.Id, s.Sentence, s.Translation, s.SourceLanguageCode, s.TargetLanguageCode, s.AudioFilePath
                    FROM Sentences s
                    INNER JOIN WordSentenceCollectionLink wscl ON wscl.SentenceId = s.Id
                    WHERE wscl.CollectionId = @CollectionId
                    ORDER BY wscl.UpdateTime DESC;";
            }

            using var command = new SQLiteCommand(sql, connection);
            if (CollectionId != 0)
            {
                command.Parameters.AddWithValue("@CollectionId", CollectionId);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var sentenceModel = new SentenceModel
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    Sentence = reader["Sentence"]?.ToString(),
                    Translation = reader["Translation"]?.ToString(),
                    SourceLanguageCode = reader["SourceLanguageCode"]?.ToString(),
                    TargetLanguageCode = reader["TargetLanguageCode"]?.ToString(),
                    //AudioFilePath = reader["AudioFilePath"]?.ToString()
                };
                sentences.Add(sentenceModel);
            }

            return sentences;
        }
        public void Dispose()
        {
            _connection?.Dispose();
        }


    
    }
}