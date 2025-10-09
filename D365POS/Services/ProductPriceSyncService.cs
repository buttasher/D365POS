using D365POS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365POS.Services
{
    class ProductPriceSyncService
    {
        private readonly GetActiveProductPrices _apiService = new GetActiveProductPrices();
        private readonly DatabaseService _dbService = new DatabaseService();

        public async Task SyncProductsPricesAsync(string company, string storeId)
        {
            // 1. Fetch products from API
            var apiProducts = await _apiService.GetActiveProductPricesAsync(company, storeId);

            if (apiProducts == null || apiProducts.Count == 0)
                return;

            // 2. Clear existing products from local DB
            await _dbService.DeleteAllProductsUnit();

            // 3. Map API response to StoreProducts
            var storeProductPrices = apiProducts.Select(p => new StoreProductsUnit
            {
                ItemId = p.ItemId,
                UnitId = p.UnitId,
                UnitPrice = p.UnitPrice,
            }).ToList();

            // 4. Save to local SQLite
            await _dbService.InsertProductsUnit(storeProductPrices);
        }
    }
}
