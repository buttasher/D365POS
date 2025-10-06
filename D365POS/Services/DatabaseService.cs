using SQLite;
using D365POS.Models;

namespace D365POS.Services
{
    public class DatabaseService
    {
        private const string DB_Name = "D365POS.db3";
        private readonly SQLiteAsyncConnection _connection;

        public DatabaseService()
        {
           _connection = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, DB_Name));
           _connection.CreateTableAsync<SalesTable>();

        }

        public async Task<SalesTable> GetById(int id)
        {
            return await _connection.Table<SalesTable>().Where(i => i.SalesId == id).FirstOrDefaultAsync();
        }
        public async Task Create(SalesTable salesTable)
        {
            await _connection.InsertAsync(salesTable);
        }
        public async Task Update(SalesTable salesTable)
        {
            await _connection.UpdateAsync(salesTable);
        }
        public async Task Delete(SalesTable salesTable)
        {
            await _connection.DeleteAsync(salesTable);
        }
        //Added method to get all products from StoreProducts table 
        public async Task<List<StoreProducts>> GetAllProducts()
        {
            return await _connection.Table<StoreProducts>().ToListAsync();
        }

    }
}
