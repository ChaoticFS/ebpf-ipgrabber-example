using System;
using System.Collections.Generic;
using System.IO;
using Shared.Model;

namespace Indexer
{
    public class Crawler
    {
        private readonly char[] separators = " \\\n\t\"$'!,?;.:-_**+=)([]{}<>/@&%€#".ToCharArray();
        /* Will be used to spilt text into words. So a word is a maximal sequence of
         * chars that does not contain any char from separators */

        private Dictionary<string, int> words = new Dictionary<string, int>();
        /* Will contain all words from files during indexing - thet key is the 
         * value of the word and the value is its id in the database */

        private int documentCounter = 0;
        /* Will count the number of documents indexed during indexing */

        IDatabase _db;

        public Crawler(IDatabase db)
        { 
            _db = db; 
        }

        //Return a dictionary containing all words (as the key)in the file
        // [f] and the value is the number of ocurrences of the key in file.
        private Dictionary<string, int> ExtractWordsInFile(FileInfo f)
        {
            Dictionary<string, int> res = new Dictionary<string, int>();
            var content = File.ReadAllLines(f.FullName);
            foreach (var line in content)
            {
                foreach (var aWord in line.Split(separators, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!res.ContainsKey(aWord))
                        res[aWord] = 0;

                    res[aWord]++;
                }
            }
            return res;
        }

        private Dictionary<int, int> GetWordIdFromWords(Dictionary<string, int> src) 
        {
            Dictionary<int, int> res = new Dictionary<int, int>();

            foreach ( var p in src)
            {
                res.Add(words[p.Key], p.Value);
            }
            return res;
        }

        // Return a dictionary of all the words (the key) in the files contained
        // in the directory [dir]. Only files with an extension in
        // [extensions] is read. The value part of the return value is
        // the number of ocurrences of the key.
        public void IndexFilesIn(DirectoryInfo dir, List<string> extensions) {
            
            Console.WriteLine($"Crawling {dir.FullName}");

            foreach (var file in dir.EnumerateFiles())
                if (extensions.Contains(file.Extension))
                {
                    documentCounter++;
                    BEDocument newDoc = new BEDocument{
                        mId = documentCounter,
                        mUrl = file.FullName,
                        mIdxTime = DateTime.Now.ToString(),
                        mCreationTime = file.CreationTime.ToString()
                    };
                    
                    _db.InsertDocument(newDoc);
                    Dictionary<string, int> newWords = new Dictionary<string, int>();
                    Dictionary<string, int> wordsInFile = ExtractWordsInFile(file);
                    foreach (var aWord in wordsInFile.Keys) {
                        if (!words.ContainsKey(aWord)) {
                            words.Add(aWord, words.Count + 1);
                            newWords.Add(aWord, words[aWord]);
                        }
                    }
                    _db.InsertAllWords(newWords);

                    _db.InsertAllOcc(newDoc.mId, GetWordIdFromWords(wordsInFile));


                }
            foreach (var d in dir.EnumerateDirectories())
                IndexFilesIn(d, extensions);
        }

        
    }
}
