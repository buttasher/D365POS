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

            // Create all tables
            _connection.CreateTableAsync<SalesTable>().Wait();
            _connection.CreateTableAsync<StoreProducts>().Wait();
            _connection.CreateTableAsync<StoreProductsUnit>().Wait();
        }

        // ==========================
        // SalesTable Methods
        // ==========================
        public async Task<SalesTable> GetById(int id)
        {
            return await _connection.Table<SalesTable>()
                                    .Where(i => i.SalesId == id)
                                    .FirstOrDefaultAsync();
        }

        public async Task Create(SalesTable salesTable)
        {
            await _connection.InsertAsync(salesTable);
        }

        // ==========================
        // StoreProducts Methods
        // ==========================
        // Get all products
        public async Task<List<StoreProducts>> GetAllProducts()
        {
            return await _connection.Table<StoreProducts>().ToListAsync();
        }

        // Insert single product
        public async Task InsertProduct(StoreProducts product)
        {
            await _connection.InsertAsync(product);
        }

        // Insert multiple products
        public async Task InsertProducts(List<StoreProducts> products)
        {
            await _connection.InsertAllAsync(products);
        }

        // Delete all products (useful before syncing)
        public async Task DeleteAllProducts()
        {
            await _connection.DeleteAllAsync<StoreProducts>();
        }
        // ==========================
        // StoreProductsPrices Methods
        // ==========================
        // Get all products
        public async Task<List<StoreProductsUnit>> GetAllProductsUnit()
        {
            return await _connection.Table<StoreProductsUnit>().ToListAsync();
        }
        // Insert single product
        public async Task InsertProductUnit(StoreProductsUnit productUnit)
        {
            await _connection.InsertAsync(productUnit);
        }

        // Insert multiple products
        public async Task InsertProductsUnit(List<StoreProductsUnit> productsUnit)
        {
            await _connection.InsertAllAsync(productsUnit);
        }

        // Delete all products (useful before syncing)
        public async Task DeleteAllProductsUnit()
        {
            await _connection.DeleteAllAsync<StoreProductsUnit>();
        }
    }
}
