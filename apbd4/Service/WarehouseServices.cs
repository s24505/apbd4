using apbd4.Repository;
using apbd4.Service;

namespace apbd4.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _repository;

        public WarehouseService(IWarehouseRepository warehouseRepository)
        {
            _repository = warehouseRepository;
        }

        public async Task<string> AddProduct(Warehouse warehouse)
        {
            if (!await CheckIfExist(warehouse))
            {
                return "Warehouse or product doesn't exist";
            }

            bool isOrderExisting = await CheckOrder(warehouse);
            bool isCompletedOrdersExist = await _repository.CheckIfOrdersExist(warehouse);

            if (!isOrderExisting && isCompletedOrdersExist)
            {
                int result = await _repository.InsertOrder(warehouse);
                return "Key value for Product_Warehouse: " + result.ToString();
            }
            else
            {
                return "Product order exists";
            }
        }

        private async Task<bool> CheckIfExist(Warehouse warehouse)
        {
            bool productExists = await _repository.CheckIfProductExists(warehouse);
            bool warehouseExists = await _repository.CheckIfWareHouseExists(warehouse);
    
            return productExists && warehouseExists;
        }

        private Task<bool> CheckOrder(Warehouse warehouse)
        {
            return _repository.CheckOrder(warehouse);
        }

        public async Task<string> AddProductProcedure(Warehouse warehouse)
        {
            return await _repository.ExecProcedure(warehouse);
        }
    }
}