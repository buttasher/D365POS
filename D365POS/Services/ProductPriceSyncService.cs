using D365POS.Models;
using System.Diagnostics;

namespace D365POS.Services
{
    class ProductPriceSyncService
    {
        private readonly GetActiveProductPrices _apiService = new GetActiveProductPrices();
        private readonly DatabaseService _dbService = new DatabaseService();

        public async Task SyncProductsPricesAsync(string company, string storeId)
        {
            try
            {
                // 1. Fetch products from API
                var apiProducts = await _apiService.GetActiveProductPricesAsync(company, storeId);

                if (apiProducts == null || apiProducts.Count == 0)
                {
                    Debug.WriteLine("No product prices received from API.");
                    return;
                }

                // 2. Clear existing product prices from local DB
                await _dbService.DeleteAllProductsUnit();

                // 3. Map API response to StoreProductsUnit
                var storeProductPrices = apiProducts.Select(p => new StoreProductsUnit
                {
                    ItemId = p.ItemId,
                    UnitId = p.UnitId,
                    UnitPrice = p.UnitPrice,
                    PriceIncludeTax = p.PriceIncludeTax,
                }).ToList();

                // 4. Save to local SQLite
                await _dbService.InsertProductsUnit(storeProductPrices);

            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error during product price sync", ex);
            }
        }
    }
}
