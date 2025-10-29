using System.Diagnostics;
using D365POS.Models;

namespace D365POS.Services
{
    public class MaskSyncService
    {
        private readonly GetMasksService _apiService = new GetMasksService();
        private readonly DatabaseService _dbService = new DatabaseService();

        public async Task SyncMasksAsync(string company)
        {
            try
            {
                // 1️⃣ Fetch data from API
                var apiResponse = await _apiService.GetMasksServiceAsync(company);

                if (apiResponse == null || apiResponse.Count == 0)
                {
                    Debug.WriteLine("[MaskSync] No data returned from API.");
                    return;
                }

                // Clear old mask data
                await _dbService.DeleteAllMasksAsync();
                await _dbService.DeleteAllMaskSegmentsAsync();

                // Prepare and insert new data
                var masksToInsert = new List<BarcodeMasks>();
                var maskSegmentsToInsert = new List<BarcodeMasksSegment>();

                foreach (var mask in apiResponse)
                {
                    var maskEntity = new BarcodeMasks
                    {
                        MaskId = int.TryParse(mask.MaskId, out var idVal) ? idVal : 0,
                        Mask = mask.Mask,
                        Prefix = int.TryParse(mask.Prefix, out var prefixVal) ? prefixVal : 0,
                        Length = mask.Length
                    };

                    masksToInsert.Add(maskEntity);
                }

                // Insert all masks
                await _dbService.InsertMasks(masksToInsert);

                // Now fetch inserted masks to assign FK IDs
                var savedMasks = await _dbService.GetAllMasksAsync();

                foreach (var mask in apiResponse)
                {
                    var parentMask = savedMasks.FirstOrDefault(x => x.Mask == mask.Mask);
                    if (parentMask == null || mask.barcodeSegments == null)
                        continue;

                    foreach (var seg in mask.barcodeSegments)
                    {
                        var segmentEntity = new BarcodeMasksSegment
                        {
                            BarcodeMasksId = parentMask.BarcodeMasksId,
                            SegmentNumber = seg.SegmentNum,
                            Type = seg.Type,
                            Length = seg.Length,
                            Char = seg.Char,
                            Decimals = seg.Decimals
                        };

                        maskSegmentsToInsert.Add(segmentEntity);
                    }
                }

                // Insert all mask segments
                await _dbService.InsertMasksSegment(maskSegmentsToInsert);

            }
            catch (Exception ex)
            {
                throw new Exception("Mask sync failed", ex);
            }
        }
    }
}
