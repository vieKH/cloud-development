using Microsoft.AspNetCore.Mvc;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Cache;

namespace Inventory.ApiService.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(InventoryCache _cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Product>> Get(int? id)
    {
        var index = id ?? 0;

        if (index < 0)
            return BadRequest("id must be >= 0");

        var data = await _cache.GetAsync(100, seed: 1, CancellationToken.None);

        var product = data.ElementAtOrDefault(index);

        return product is not null? Ok(product): NotFound();
    }
}
