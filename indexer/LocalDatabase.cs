using System.Collections.Generic;
using Shared.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Indexer
{
    public class LocalDatabase : IDatabase
    {
        private SqliteConnection _connection;
        private IConfiguration _configuration;

        public LocalDatabase(IConfiguration configuration)
        {
            _configuration = configuration;

            var connectionStringBuilder = new SqliteConnectionStringBuilder();

            connectionStringBuilder.Mode = SqliteOpenMode.ReadWriteCreate;

            connectionStringBuilder.DataSource = _configuration["Database:Path"];

            if (_configuration["SKIP_PROCESSING"] == "false")
            {
                _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);

                _connection.Open();

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

        private void Execute(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public void InsertAllWords(Dictionary<string, int> res)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                var command = _connection.CreateCommand();
                command.CommandText =
                @"INSERT INTO word(id, name) VALUES(@id,@name)";

                var paramName = command.CreateParameter();
                paramName.ParameterName = "name";
                command.Parameters.Add(paramName);

                var paramId = command.CreateParameter();
                paramId.ParameterName = "id";
                command.Parameters.Add(paramId);

                // Insert all entries in the res

                foreach (var p in res)
                {
                    paramName.Value = p.Key;
                    paramId.Value = p.Value;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void InsertAllOcc(int docId, Dictionary<int, int> wordIds){
            using (var transaction = _connection.BeginTransaction())
            {
                var command = _connection.CreateCommand();
                command.CommandText =
                @"INSERT INTO Occ(wordId, docId, ocurrences) VALUES(@wordId,@docId,@ocurrences)";

                var paramwordId = command.CreateParameter();
                paramwordId.ParameterName = "wordId";

                command.Parameters.Add(paramwordId);

                var paramDocId = command.CreateParameter();
                paramDocId.ParameterName = "docId";
                paramDocId.Value = docId;

                command.Parameters.Add(paramDocId);

                var paramocurrences = command.CreateParameter();
                paramocurrences.ParameterName = "ocurrences";

                command.Parameters.Add(paramocurrences);

                foreach (var p in wordIds)
                {
                    paramwordId.Value = p.Key;
                    paramocurrences.Value = p.Value;

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void InsertWord(int id, string value){
            var insertCmd = new SqliteCommand("INSERT INTO word(id, name) VALUES(@id,@name)");
            insertCmd.Connection = _connection;

            var pName = new SqliteParameter("name", value);
            insertCmd.Parameters.Add(pName);

            var pCount = new SqliteParameter("id", id);
            insertCmd.Parameters.Add(pCount);

            insertCmd.ExecuteNonQuery();
        }

        public void InsertDocument(BEDocument doc){
            var insertCmd = new SqliteCommand("INSERT INTO document(id, url, idxTime, creationTime) VALUES(@id,@url, @idxTime, @creationTime)");
            insertCmd.Connection = _connection;

            var pId = new SqliteParameter("id", doc.mId);
            insertCmd.Parameters.Add(pId);

            var pUrl = new SqliteParameter("url", doc.mUrl);
            insertCmd.Parameters.Add(pUrl);

            var pIdxTime = new SqliteParameter("idxTime", doc.mIdxTime);
            insertCmd.Parameters.Add(pIdxTime);

            var pCreationTime = new SqliteParameter("creationTime", doc.mCreationTime);
            insertCmd.Parameters.Add(pCreationTime);

            insertCmd.ExecuteNonQuery();

        }
        
        public BEDocument GetDocumentById(int docId)
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT id, url, idxTime, creationTime FROM document WHERE id = @id";
    
            var paramDocId = selectCmd.CreateParameter();
            paramDocId.ParameterName = "id";
            paramDocId.Value = docId;
            selectCmd.Parameters.Add(paramDocId);

            using (var reader = selectCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new BEDocument
                    {
                        mId = reader.GetInt32(0),
                        mUrl = reader.GetString(1),
                        mIdxTime = reader.GetString(2),
                        mCreationTime = reader.GetString(3)
                    };
                }
            }
            return null;
        }


        public Dictionary<string, int> GetAllWords()
        {
            Dictionary<string, int> res = new Dictionary<string, int>();

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM word";

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var w = reader.GetString(1);

                    res.Add(w, id);
                }
            }
            return res;
        }
        
        public List<Term> GetAllWordCounts()
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "select Occ.wordId as Id, count(occ.wordId) as countWord, Word.name as Name "+
                                    "from Occ JOIN Word on Occ.wordId = Word.id " + 
                                    "group by Occ.wordId order by countWord DESC";

            List<Term> result = new();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var cc = reader.GetInt32(1);
                    var name = reader.GetString(2);

                    result.Add(new Term { Id = id, Value = name, Count = cc});
                }
            }
            return result;
        }
        
        

        public int GetDocumentCounts() {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT count(*) FROM document";

            using (var reader = selectCmd.ExecuteReader()) {
                if (reader.Read()) {
                    var count = reader.GetInt32(0);
                    return count;
                }
            }
            return -1;
        }
    }
}
