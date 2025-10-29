using D365POS.Models;
using SQLite;
using System.Linq.Expressions;

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
            _connection.CreateTableAsync<StoreProducts>().Wait();
            _connection.CreateTableAsync<StoreProductsUnit>().Wait();
            _connection.CreateTableAsync<POSRetailTransactionTable>().Wait();
            _connection.CreateTableAsync<POSRetailTransactionSalesTrans>().Wait();
            _connection.CreateTableAsync<POSRetailTransactionPaymentTrans>().Wait();
            _connection.CreateTableAsync<POSRetailTransactionTaxTrans>().Wait();
            _connection.CreateTableAsync<BarcodeMasks>().Wait();
            _connection.CreateTableAsync<BarcodeMasksSegment>().Wait();

        }

        // ==========================
        // TransactionTable Methods
        // ==========================
        public async Task CreateTransactionTable(POSRetailTransactionTable transactionTable)
        {
            await _connection.InsertAsync(transactionTable);
        }
        public async Task<List<POSRetailTransactionTable>> GetAllTransactions()
        {
            return await _connection.Table<POSRetailTransactionTable>().ToListAsync();
        }
        public async Task<T?> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return await _connection.Table<T>().Where(predicate).FirstOrDefaultAsync();
        }
        // ==========================
        // SalesTrans Methods
        // ==========================
        public async Task CreateTransactionSalesTrans(POSRetailTransactionSalesTrans SalesTrans)
        {
            await _connection.InsertAsync(SalesTrans);
        }
        public async Task<List<T>> GetAllSalesTransAsync<T>() where T : new()
        {
            return await _connection.Table<T>().ToListAsync();
        }

        // Get filtered rows based on condition
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return await _connection.Table<T>().Where(predicate).ToListAsync();
        }
        // ==========================
        // PaymentTrans Methods
        // ==========================
        public async Task CreateTransactionPaymentTrans(POSRetailTransactionPaymentTrans PaymentTrans)
        {
            await _connection.InsertAsync(PaymentTrans);
        }
        // ==========================
        // TaxTrans Methods
        // ==========================
        public async Task CreateTransactionTaxTrans(POSRetailTransactionTaxTrans TaxTrans)
        {
            await _connection.InsertAsync(TaxTrans);
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
        // Masks Methods
        // ==========================
        // Insert multiple products
        public async Task InsertMasks(List<BarcodeMasks> masks)
        {
            await _connection.InsertAllAsync(masks);
        }
        public async Task<List<BarcodeMasks>> GetAllMasksAsync()
        {
            return await _connection.Table<BarcodeMasks>().ToListAsync();
        }
        public async Task DeleteAllMasksAsync()
        {
            await _connection.DeleteAllAsync<BarcodeMasks>();
        }
        // Masks Segment Methods
        // ==========================
        // Insert multiple products
        public async Task InsertMasksSegment(List<BarcodeMasksSegment> masksSegment)
        {
            await _connection.InsertAllAsync(masksSegment);
        }
        public async Task DeleteAllMaskSegmentsAsync()
        {
            await _connection.DeleteAllAsync<BarcodeMasksSegment>();
        }

    }
}
