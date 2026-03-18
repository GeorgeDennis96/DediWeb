
namespace DbAccessLibrary
{
    public interface IDbDataAccess
    {
        Task<List<T>> LoadData<T, U>(string sql, U parameters);
        Task<int> SaveData<T>(string sql, T parameters);
    }
}