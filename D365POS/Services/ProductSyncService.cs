using D365POS.Models;
using System.Diagnostics;

namespace D365POS.Services
{
    public class ProductSyncService
    {
        private readonly GetActiveProductsService _apiService = new GetActiveProductsService();
        private readonly DatabaseService _dbService = new DatabaseService();

        public async Task SyncProductsAsync(string company, string storeId)
        {
            try
            {
                // 1. Fetch products from API
                var apiProducts = await _apiService.GetActiveProductsAsync(company, storeId);

                if (apiProducts == null || apiProducts.Count == 0)
                {
                    Debug.WriteLine("No products received from API.");
                    return;
                }

                // 2. Clear existing products from local DB
                await _dbService.DeleteAllProducts();

                // 3. Map API response to StoreProducts
                var storeProducts = apiProducts.Select(p => new StoreProducts
                {
                    ItemId = p.ItemId,
                    ItemBarCode = p.ItemBarcode,
                    Description = p.Description,
                    DescriptionAr = p.DescriptionAr,
                    UnitId = p.UnitId,
                    PLUCode = p.PLUCode,
                    SalesTaxGroup = p.SalesTaxGroup,
                    ItemSalesTaxGroup = p.ItemSalesTaxGroup,
                    TaxFactor = p.TaxFactor,
                }).ToList();

                // 4. Save to local SQLite
                await _dbService.InsertProducts(storeProducts);

            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error during product sync", ex);
            }
        }
    }
}
