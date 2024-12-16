using System.Collections.Generic;
using Shared.Model;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Indexer
{
    public class RqliteDatabase : IDatabase
    {
        private HttpClient _httpClient;
        private string _connection;
        private IConfiguration _configuration;

        public RqliteDatabase(IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = new HttpClient { BaseAddress = new Uri(_configuration["Database:ConnectionString"]) };

            if (_configuration["SKIP_PROCESSING"] == "false")
            {
                Execute("DROP TABLE IF EXISTS Occ");
                Execute("DROP TABLE IF EXISTS Word_Synonym");
                Execute("DROP TABLE IF EXISTS Synonym");
                Execute("DROP TABLE IF EXISTS word");
                Execute("DROP TABLE IF EXISTS document");

                Execute("CREATE TABLE document(id INTEGER PRIMARY KEY, url TEXT, idxTime TEXT, creationTime TEXT)");

                Execute("CREATE TABLE word(id INTEGER PRIMARY KEY, name VARCHAR(50))");

                Execute("CREATE TABLE Occ(wordId INTEGER, docId INTEGER, ocurrences INTEGER, "
                      + "FOREIGN KEY (wordId) REFERENCES word(id), "
                      + "FOREIGN KEY (docId) REFERENCES document(id))");
                Execute("CREATE INDEX word_index ON Occ (wordId)");

                Execute("CREATE TABLE Synonym(id INTEGER PRIMARY KEY AUTOINCREMENT, name VARCHAR(50))");

                Execute("CREATE TABLE Word_Synonym(wordId INTEGER, synonymId INTEGER, "
                      + "FOREIGN KEY (wordId) REFERENCES word(id), "
                      + "FOREIGN KEY (synonymId) REFERENCES Synonym(id))");
            }
        }

        public void InsertAllWords(Dictionary<string, int> res)
        {
            if (res == null || res.Count == 0)
                return;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("BEGIN TRANSACTION;");

            foreach (var pair in res)
            {
                sqlBuilder.AppendLine($"INSERT INTO word(id, name) VALUES({pair.Value}, '{EscapeString(pair.Key)}');");
            }

            sqlBuilder.AppendLine("COMMIT;");

            Execute(sqlBuilder.ToString());
        }

        public void InsertAllOcc(int docId, Dictionary<int, int> wordIds)
        {
            if (wordIds == null || wordIds.Count == 0)
                return;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("BEGIN TRANSACTION;");

            foreach (var pair in wordIds)
            {
                sqlBuilder.AppendLine($"INSERT INTO Occ(wordId, docId, ocurrences) VALUES({pair.Key}, {docId}, {pair.Value});");
            }

            sqlBuilder.AppendLine("COMMIT;");

            Execute(sqlBuilder.ToString());
        }

        public void InsertWord(int id, string value){
            string sql = "INSERT INTO word(id, name) " +
                        $"VALUES({id},'{EscapeString(value)}')";

            Execute(sql);
        }

        public void InsertDocument(BEDocument doc)
        {
            string sql = ("INSERT INTO document(id, url, idxTime, creationTime) " +
                         $"VALUES({doc.mId},'{EscapeString(doc.mUrl)}','{EscapeString(doc.mIdxTime)}','{EscapeString(doc.mCreationTime)}')");

            Execute(sql);
        }
        
        public BEDocument GetDocumentById(int docId)
        {
            string sql = "SELECT id, url, idxTime, creationTime " +
                         "FROM document " +
                        $"WHERE id = {docId}";

            var result = Query(sql, row =>
            {
                return new BEDocument
                {
                    mId = row[0].GetInt32(),
                    mUrl = row[1].GetString(),
                    mIdxTime = row[2].GetString(),
                    mCreationTime = row[3].GetString()
                };
            }).Result;

            return result.FirstOrDefault();
        }

        public Dictionary<string, int> GetAllWords()
        {
            string sql = "SELECT * FROM word";

            var result = Query(sql, row =>
            {
                var id = row[0].GetInt32();
                var word = row[1].GetString();
                return new KeyValuePair<string, int>(word, id);
            }).Result;

            return new Dictionary<string, int>(result);
        }
        
        public List<Term> GetAllWordCounts()
        {
            string sql = "SELECT Occ.wordId AS Id, count(occ.wordId) AS countWord, Word.name AS Name "+
                         "FROM Occ " +
                         "JOIN Word ON Occ.wordId = Word.id " + 
                         "GROUP BY Occ.wordId " +
                         "ORDER BY countWord DESC";

            List<Term> result = Query(sql, row =>
            {
                return new Term
                {
                    Id = row[0].GetInt32(),
                    Count = row[1].GetInt32(),
                    Value = row[2].GetString()
                };
            }).Result;
            
            return result;
        }

        public int GetDocumentCounts() 
        {
            string sql = "SELECT count(*) FROM document";

            var count = Query(sql, row =>
            {
                return row[0].GetInt32();
            }).Result;

            return count.FirstOrDefault();
        }

        private void Execute(string sql)
        {
            var payload = JsonSerializer.Serialize(new[] { sql });
            Console.WriteLine(payload); //delete this

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = _httpClient.PostAsync("/db/execute", content).Result;
            response.EnsureSuccessStatusCode();
        }
        private string EscapeString(string value)
        {
            return value.Replace("'", "''"); // Escape single quotes for SQL strings
        }

        private async Task<List<T>> Query<T>(string sql, Func<JsonElement, T> mapFunc)
        {
            var payload = JsonSerializer.Serialize(new[] { sql });
            Console.WriteLine(payload); //delete this

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
    }
}
