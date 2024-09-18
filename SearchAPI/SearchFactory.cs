namespace ConsoleSearch;

public class SearchFactory
{
    public static ISearchLogic GetSearchLogic(IDatabase database)
    {
        return new SearchLogic(database);
    }
}
