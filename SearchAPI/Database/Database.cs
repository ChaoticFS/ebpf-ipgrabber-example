using Shared.Model;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using SearchAPI.Services;

namespace SearchAPI.Database;
public class Database : IDatabase
{
    private IConfiguration _configuration;
    private SqliteConnection _connection;
    private ICacheService _cacheService;

    public Database(IConfiguration configuration, ICacheService cacheService)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();

        _configuration = configuration;

        var relativePath = _configuration["Database:Path"];

        var absolutePath = Path.GetFullPath(relativePath, AppContext.BaseDirectory);

        connectionStringBuilder.DataSource = absolutePath;

        _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);

        _connection.Open();

        _cacheService = cacheService;
    }

    private string AsString(List<int> x) => $"({string.Join(',', x)})";

    // key is the id of the document, the value is number of search words in the document
    public Dictionary<int, int> GetDocuments(List<int> wordIds)
    {
        var res = new Dictionary<int, int>();

        /* Example sql statement looking for doc id's that
           contain words with id 2 and 3
        
           SELECT docId, COUNT(wordId) as count
             FROM Occ
            WHERE wordId in (2,3)
         GROUP BY docId
         ORDER BY COUNT(wordId) DESC 
         */

        var sql = "SELECT docId, ocurrences as count FROM Occ where ";
        sql += "wordId in " + AsString(wordIds) + " GROUP BY docId ";
        sql += "ORDER BY count DESC;";


        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = sql;

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var docId = reader.GetInt32(0);
                var count = reader.GetInt32(1);

                res.Add(docId, count);
            }
        }

        return res;
    }

    private Dictionary<string, int> GetAllWords()
    {
        var cachedResult = _cacheService.GetAsync<Dictionary<string, int>>("words").Result;

        if (cachedResult != null)
        {
            Console.WriteLine("Cache hit for getting all words");
            return cachedResult;
        }

        Dictionary<string, int> res = new Dictionary<string, int>();

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM word";

        using (var reader = selectCmd.ExecuteReader())
        {
            int duplicates = 0;

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var w = reader.GetString(1);

                if (!res.TryAdd(w, id))
                {
                    duplicates++;
                }
            }

            if (duplicates > 0)
            {
                Console.WriteLine(duplicates + " duplicate words skipped");
                Console.WriteLine("This only shows up if you ran the indexer more than once, double check things");
            }
        }

        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync("words", res, expiration);
        Console.WriteLine("Cache miss for getting all words");

        return res;
    }

    public List<ResultDocument> GetDocDetails(Dictionary<int, int> docIdOcc)
    {
        List<ResultDocument> res = new List<ResultDocument>();

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM document where id in " + AsString(docIdOcc.Keys.ToList());

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var url = reader.GetString(1);
                var idxTime = reader.GetString(2);
                var creationTime = reader.GetString(3);

                res.Add(new ResultDocument { Id = id, Url = url, IdxTime = idxTime, CreationTime = creationTime, Count = docIdOcc[id] });
            }
        }
        return res;
    }

    /* Return a list of id's for words; all them among wordIds, but not present in the document
     */
    public List<int> getMissing(int docId, List<int> wordIds)
    {
        var sql = "SELECT wordId FROM Occ where ";
        sql += "wordId in " + AsString(wordIds) + " AND docId = " + docId;
        sql += " ORDER BY wordId;";

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = sql;

        List<int> present = new List<int>();

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var wordId = reader.GetInt32(0);
                present.Add(wordId);
            }
        }
        var result = new List<int>(wordIds);
        foreach (var w in present)
            result.Remove(w);


        return result;
    }

    public List<string> WordsFromIds(List<int> wordIds)
    {
        var sql = "SELECT name FROM Word where ";
        sql += "id in " + AsString(wordIds);

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = sql;

        List<string> result = new List<string>();

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var wordId = reader.GetString(0);
                result.Add(wordId);
            }
        }
        return result;
    }


    public List<int> GetWordIds(string[] query, out List<string> outIgnored)
    {
        Dictionary<string, int> mWords = GetAllWords();

        var res = new List<int>();
        var ignored = new List<string>();

        var queryWordsWithSynonyms = new List<string>();

        foreach (var word in query)
        {
            var synonyms = GetSynonyms(word);

            foreach (var synonym in synonyms)
            {
                queryWordsWithSynonyms.Add(synonym.Name);
            }

            queryWordsWithSynonyms.Add(word);
        }

        foreach (var aWord in queryWordsWithSynonyms)
        {
            if (mWords.ContainsKey(aWord))
            {
                res.Add(mWords[aWord]);
            }
            else
            {
                ignored.Add(aWord);
            }
        }

        outIgnored = ignored;
        return res;
    }

    public List<int> GetWordIds(string[] query, bool caseSensitive, out List<string> outIgnored)
    {
        Dictionary<string, int> mWords = GetAllWords();

        var res = new List<int>();
        var ignored = new List<string>();

        var queryWordsWithSynonyms = new List<string>();

        foreach (var word in query)
        {
            var synonyms = GetSynonyms(word);

            foreach (var synonym in synonyms)
            {
                queryWordsWithSynonyms.Add(synonym.Name);
            }

            queryWordsWithSynonyms.Add(word);
        }

        if (caseSensitive)
        {
            foreach (var aWord in queryWordsWithSynonyms)
            {
                if (mWords.ContainsKey(aWord))
                    res.Add(mWords[aWord]);
                else
                    ignored.Add(aWord);
            }
            outIgnored = ignored;
            return res;
        }
        else
        {
            foreach (string aWord in queryWordsWithSynonyms)
            {
                bool found = false;
                foreach (var word in mWords)
                {
                    if (aWord.Equals(word.Key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!res.Contains(word.Value))
                        {
                            res.Add(word.Value);
                            found = true;
                        }
                    }
                }
                if (!found)
                {
                    ignored.Add(aWord);
                }
            }
            outIgnored = ignored;
            return res;
        }
    }

    public List<Synonym> GetSynonyms(string word)
    {
        var cachedResult = _cacheService.GetAsync<List<Synonym>>($"synonyms:{word}").Result;

        if (cachedResult != null)
        {
            Console.WriteLine($"Cache hit for synonyms of word: {word}");
            return cachedResult;
        }

        var synonyms = new List<Synonym>();

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = @"SELECT s.id, s.name AS Synonym
                                  FROM Word_Synonym ws
                                  JOIN word w ON ws.wordId = w.id
                                  JOIN Synonym s ON ws.synonymId = s.id
                                  WHERE w.name = @wordName;";
        selectCmd.Parameters.AddWithValue("@wordName", word);

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                synonyms.Add(new Synonym
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
        }

        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync($"synonyms:{word}", synonyms, expiration);
        Console.WriteLine($"Cache miss for synonyms of word: {word}");

        return synonyms;
    }

    
    public int AddSynonym(string synonym)
    {
        using (var transaction = _connection.BeginTransaction())
        {
            var command = _connection.CreateCommand();
            command.CommandText =
                @"INSERT INTO Synonym(name) 
          VALUES(@name);
          SELECT last_insert_rowid();";
                
        
            var paramName = command.CreateParameter();
            paramName.ParameterName = "name";
            paramName.Value = synonym;
            command.Parameters.Add(paramName);
            
            var id = Convert.ToInt32(command.ExecuteScalar());
                
            transaction.Commit();

            _cacheService.ClearAsync();

            return id;
        }
    }
    
    

    public void UpdateSynonym(Synonym synonym)
    {
        using (var transaction = _connection.BeginTransaction())
        {
            var command = _connection.CreateCommand();
            command.CommandText =
            @"UPDATE Synonym 
              SET name = @name
              WHERE id = @id";

            var paramName = command.CreateParameter();
            paramName.ParameterName = "name";
            paramName.Value = synonym.Name;
            command.Parameters.Add(paramName);

            var paramId = command.CreateParameter();
            paramId.ParameterName = "id";
            paramId.Value = synonym.Id;
            command.Parameters.Add(paramId);

            command.ExecuteNonQuery();

            _cacheService.ClearAsync();

            transaction.Commit();
        }
    }

    public void DeleteSynonym(int synonymId) 
    {
        using (var transaction = _connection.BeginTransaction())
        {
            var command = _connection.CreateCommand();
            command.CommandText =
            @"DELETE FROM Synonym 
              WHERE id = @id";

            var paramId = command.CreateParameter();
            paramId.ParameterName = "id";
            paramId.Value = synonymId;
            command.Parameters.Add(paramId);

            command.ExecuteNonQuery();

            _cacheService.ClearAsync();

            transaction.Commit();
        }
    }

    public void AddSynonymWord(int synonymId, int wordId)
    {
        using (var transaction = _connection.BeginTransaction())
        {
            var command = _connection.CreateCommand();
            command.CommandText =
            @"INSERT INTO Word_Synonym(wordId, synonymId) 
              VALUES(@wordId, @synonymId)";

            var paramWord = command.CreateParameter();
            paramWord.ParameterName = "wordId";
            paramWord.Value = wordId;
            command.Parameters.Add(paramWord);

            var paramSynonym = command.CreateParameter();
            paramSynonym.ParameterName = "synonymId";
            paramSynonym.Value = synonymId;
            command.Parameters.Add(paramSynonym);

            command.ExecuteNonQuery();

            _cacheService.ClearAsync();

            transaction.Commit();
        }
    }

    public void DeleteSynonymWord(int synonymId, int wordId)
    {
        using (var transaction = _connection.BeginTransaction())
        {
            var command = _connection.CreateCommand();
            command.CommandText =
            @"DELETE FROM Word_Synonym 
              WHERE wordId = @wordId 
              AND synonymId = @synonymId";

            var paramWord = command.CreateParameter();
            paramWord.ParameterName = "wordId";
            paramWord.Value = wordId;
            command.Parameters.Add(paramWord);

            var paramSynonym = command.CreateParameter();
            paramSynonym.ParameterName = "synonymId";
            paramSynonym.Value = synonymId;
            command.Parameters.Add(paramSynonym);

            command.ExecuteNonQuery();

            _cacheService.ClearAsync();

            transaction.Commit();
        }
    }
    
    public List<Synonym> GetAllSynonyms()
    {
        var cachedResult = _cacheService.GetAsync<List<Synonym>>("synonyms").Result;

        if (cachedResult != null)
        {
            Console.WriteLine($"Cache hit for all synonyms");
            return cachedResult;
        }

        var synonyms = new List<Synonym>();

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = @"SELECT id, name FROM Synonym";

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                synonyms.Add(new Synonym
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
        }

        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync($"synonyms", synonyms, expiration);
        Console.WriteLine("Cache miss for all synonyms");

        return synonyms;
    }
}