namespace apbd4.Service

{
    public interface IWarehouseService
    {
        Task<string> AddProduct(Warehouse warehouse);
        Task<string> AddProductProcedure(Warehouse warehouse);
    }
}