using apbd4;

namespace apbd4.Repository;

public interface IWarehouseRepository
{
    Task<bool> CheckIfOrdersExist(Warehouse warehouse);
    Task<bool> CheckOrder(Warehouse warehouse);
    Task<bool> CheckIfProductExists(Warehouse warehouse);
    Task UpdateFull(DateTime createdAt, decimal orderId);
    Task<bool> CheckIfWareHouseExists(Warehouse warehouse);
    Task<int> InsertOrder(Warehouse warehouse);
    Task<int> InsertProduct(Warehouse warehouse, int orderId);
    Task<string> AddProductByProcedure(Warehouse warehouse);
    Task<string> ExecProcedure(Warehouse warehouse);
}
