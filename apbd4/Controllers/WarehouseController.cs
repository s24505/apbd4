using apbd4.Service;
using Microsoft.AspNetCore.Mvc;

namespace apbd4.Controllers
{
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct(Warehouse warehouse)
        {
            if (warehouse.Amount <= 0)
            {
                return BadRequest("Amount should be higher than 0");
            }

            string result = await _warehouseService.AddProduct(warehouse);
            return Ok(result);
        }

        [HttpPost("AddProductByProcedure")]
        public async Task<IActionResult> AddProductByProcedure(Warehouse warehouse)
        {
            string result = await _warehouseService.AddProductProcedure(warehouse);
            return Ok(result);
        }
    }
}