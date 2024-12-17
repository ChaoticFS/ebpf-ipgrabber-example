using Shared.Model;
using System.Text.Json;
using SearchAPI.Services;
using System.Text;

namespace SearchAPI.Database;
public class RqliteDatabase : IDatabase
{
    private IConfiguration _configuration;
    private HttpClient _httpClient;
    private ICacheService _cacheService;

    public RqliteDatabase(IConfiguration configuration, ICacheService cacheService)
    {
        _configuration = configuration;

        _httpClient = new HttpClient { BaseAddress = new Uri(_configuration["Database:ConnectionString"]) };

        _cacheService = cacheService;
    }

    private string AsString(List<int> x) => $"({string.Join(',', x)})";

    // key is the id of the document, the value is number of search words in the document
    public Dictionary<int, int> GetDocuments(List<int> wordIds)
    {
        var sql = "SELECT docId, ocurrences AS count " +
                  "FROM Occ " +
                 $"WHERE wordId in {AsString(wordIds)} " + 
                  "GROUP BY docId " + 
                  "ORDER BY count DESC;";

        var pairs = Query(sql, row =>
        {
            var id = row[0].GetInt32();
            var count = row[1].GetInt32();
            return new KeyValuePair<int, int>(id, count);
        }).Result;

        var result = new Dictionary<int, int>(pairs);

        return result;
    }

    private Dictionary<string, int> GetAllWords()
    {
        var cachedResult = _cacheService.GetAsync<Dictionary<string, int>>("words").Result;

        if (cachedResult != null)
        {
            Console.WriteLine("Cache hit for getting all words");
            return cachedResult;
        }

        string sql = "SELECT * FROM word";

        var pairs = Query(sql, row =>
        {
            var id = row[0].GetInt32();
            var word = row[1].GetString();
            return new KeyValuePair<string, int>(word, id);
        }).Result;

        var result = new Dictionary<string, int>(pairs);


        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync("words", result, expiration);
        Console.WriteLine("Cache miss for getting all words");

        return result;
    }

    public List<ResultDocument> GetDocDetails(Dictionary<int, int> docIdOcc)
    {
        string sql = "SELECT * " +
                     "FROM document " +
                     "WHERE id IN " + AsString(docIdOcc.Keys.ToList());

        List<ResultDocument> result = Query(sql, row =>
        {
            var id = row[0].GetInt32();

            return new ResultDocument
            {
                Id = id,
                Url = row[1].ToString(),
                IdxTime = row[2].ToString(),
                CreationTime = row[3].ToString(),
                Count = docIdOcc[id]
            };
        }).Result;

        return result;
    }

    /* Return a list of id's for words; all them among wordIds, but not present in the document
     */
    public List<int> getMissing(int docId, List<int> wordIds)
    {
        var sql = "SELECT wordId FROM Occ " +
                 $"WHERE wordId IN {AsString(wordIds)} " +  
                 $"AND docId = {docId} " +
                  "ORDER BY wordId;";

        List<int> present = Query(sql, row =>
        {
            return row[0].GetInt32();
        }).Result;

        var result = new List<int>(wordIds);
        foreach (var w in present)
        {
            result.Remove(w);
        }

        return result;
    }

    public List<string> WordsFromIds(List<int> wordIds)
    {
        var sql = "SELECT name " +
                  "FROM word " +
                  "WHERE id IN " + AsString(wordIds);

        List<string> result = Query(sql, row =>
        {
            return row[0].ToString();
        }).Result;

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

        string sql = "SELECT s.id, s.name AS Synonym " +
                     "FROM Word_Synonym ws " +
                     "JOIN word w ON ws.wordId = w.id " +
                     "JOIN Synonym s ON ws.synonymId = s.id " +
                    $"WHERE w.name = '{EscapeString(word)}';";

        List<Synonym> result = Query(sql, row =>
        {
            return new Synonym
            {
                Id = row[0].GetInt32(),
                Name = row[1].GetString(),
            };
        }).Result;


        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync($"synonyms:{word}", result, expiration);
        Console.WriteLine($"Cache miss for synonyms of word: {word}");

        return result;
    }

    
    public int AddSynonym(string synonym)
    {
        string sqlInsert = "INSERT INTO Synonym(name) " +
                          $"VALUES('{EscapeString(synonym)}')";

        int id = ExecuteAndGetId(sqlInsert).Result;

        _cacheService.ClearAsync().Wait();

        return id;
    }

    public void UpdateSynonym(Synonym synonym)
    {
        string sql = "UPDATE Synonym " +
                    $"SET name = '{EscapeString(synonym.Name)}' " +
                    $"WHERE id = {synonym.Id}";

        Execute(sql);
        
        _cacheService.ClearAsync().Wait();
    }

    public void DeleteSynonym(int synonymId) 
    {
        string sql = "DELETE FROM Synonym " +
                    $"WHERE id = {synonymId}";

        Execute(sql);

        _cacheService.ClearAsync().Wait();
    }

    public void AddSynonymWord(string synonym, string word)
    {
        string sql = "INSERT INTO Word_Synonym (wordId, synonymId) " +
                     "VALUES (" +
                       $"(SELECT id FROM word WHERE name = '{EscapeString(word)}'), " +
                       $"(SELECT id FROM Synonym WHERE name = '{EscapeString(synonym)}')" +
                     ");";

        Execute(sql);

        _cacheService.ClearAsync().Wait();
    }

    public void DeleteSynonymWord(int synonymId, int wordId)
    {
        string sql = "DELETE FROM Word_Synonym " +
                    $"WHERE wordId = {wordId} " +
                    $"AND synonymId = {synonymId}";

        Execute(sql);

        _cacheService.ClearAsync().Wait();
    }
    
    public List<Synonym> GetAllSynonyms()
    {
        var cachedResult = _cacheService.GetAsync<List<Synonym>>("synonyms").Result;

        if (cachedResult != null)
        {
            Console.WriteLine($"Cache hit for all synonyms");
            return cachedResult;
        }

        string sql = @"SELECT id, name FROM Synonym";

        List<Synonym> result = Query(sql, row =>
        {
            return new Synonym
            {
                Id = row[0].GetInt32(),
                Name = row[1].GetString(),
            };
        }).Result;

        TimeSpan expiration = TimeSpan.FromMinutes(30);
        _cacheService.SetAsync($"synonyms", result, expiration);
        Console.WriteLine("Cache miss for all synonyms");

        return result;
    }

    private void Execute(string sql)
    {
        var payload = JsonSerializer.Serialize(new[] { sql });

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = _httpClient.PostAsync("/db/execute", content).Result;
        response.EnsureSuccessStatusCode();
    }

    private string EscapeString(string value)
    {
        // Escape single quotes for SQL strings
        return value.Replace("'", "''"); 
    }

    private async Task<List<T>> Query<T>(string sql, Func<JsonElement, T> mapFunc)
    {
        var payload = JsonSerializer.Serialize(new[] { sql });

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/db/query", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to execute query on {_httpClient.BaseAddress}/query: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var success = responseJson.RootElement.GetProperty("results")[0].TryGetProperty("values", out var rows);

        var result = new List<T>();
        if (success)
        {
            foreach (var row in rows.EnumerateArray())
            {
                result.Add(mapFunc(row));
            }
        }

        return result;
    }

    private async Task<int> ExecuteAndGetId(string sql)
    {
        var payload = JsonSerializer.Serialize(new[] { sql });

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/db/execute", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to execute query with id return on {_httpClient.BaseAddress}/execute: {response.StatusCode} - {response.ReasonPhrase}");
        }

        var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = responseJson.RootElement.GetProperty("results")[0];

        if (result.TryGetProperty("last_insert_id", out var lastInsertId))
        {
            return lastInsertId.GetInt32();
        }

        throw new Exception("Execute with id return did not return a last_insert_id");
    }
}