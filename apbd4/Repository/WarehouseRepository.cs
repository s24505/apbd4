using System.Data;
using System.Data.SqlClient;

namespace apbd4.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly IConfiguration _configuration;

        public WarehouseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<SqlConnection> OpenConnectionAsync()
        {
            var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
            await connection.OpenAsync();
            return connection;
        }

        private async Task<int> ExecuteScalarAsync(SqlCommand command)
        {
            using var connection = await OpenConnectionAsync();
            command.Connection = connection;
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task ExecuteNonQueryAsync(SqlCommand command)
        {
            using var connection = await OpenConnectionAsync();
            command.Connection = connection;
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> CheckIfOrdersExist(Warehouse warehouse)
        {
            using var command = new SqlCommand(
                "IF EXISTS (SELECT * FROM [master].[dbo].[Product_Warehouse] WHERE IdOrder = (SELECT IdOrder FROM [master].[dbo].[Order] WHERE IdProduct = @IdProduct AND Amount = @Amount)) " +
                "BEGIN SELECT 1 END ELSE BEGIN SELECT 2 END", await OpenConnectionAsync());
            command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            return await ExecuteScalarAsync(command) == 2;
        }

        public async Task<bool> CheckOrder(Warehouse warehouse)
        {
            using var command = new SqlCommand(
                @"SELECT 
                    CASE 
                    WHEN EXISTS (
                        SELECT * FROM [master].[dbo].[Order] WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < GETDATE()
                    ) THEN 1 
                    ELSE 2 
                    END", await OpenConnectionAsync());
            command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
            command.Parameters.AddWithValue("@Amount", warehouse.Amount);
            return await ExecuteScalarAsync(command) == 1;
        }

        public async Task<bool> CheckIfProductExists(Warehouse warehouse)
        {
            using var command = new SqlCommand(
                "IF EXISTS (SELECT * FROM [master].[dbo].[Product] WHERE IdProduct = @IdProduct) " +
                "BEGIN SELECT 1 END ELSE BEGIN SELECT 2 END", await OpenConnectionAsync());
            command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
            return await ExecuteScalarAsync(command) == 1;
        }

        public async Task UpdateFull(DateTime createdAt, decimal orderId)
        {
            using var command = new SqlCommand(
                "UPDATE [master].[dbo].[Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @OrderId", await OpenConnectionAsync());
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            command.Parameters.AddWithValue("@OrderId", orderId);
            await ExecuteNonQueryAsync(command);
            Console.WriteLine("Update executed");
        }

        public async Task<bool> CheckIfWareHouseExists(Warehouse warehouse)
        {
            using var command = new SqlCommand(
                @"SELECT 
                    CASE 
                    WHEN EXISTS (
                        SELECT * FROM [master].[dbo].[Warehouse] 
                        WHERE IdWarehouse = @IdWarehouse
                    )THEN 1 
                    ELSE 2 
                    END", await OpenConnectionAsync());
            command.Parameters.AddWithValue("@IdWarehouse", warehouse.WarehouseId);
            return await ExecuteScalarAsync(command) == 1;
        }

        public async Task<int> InsertOrder(Warehouse warehouse)
        {
            int orderId;
            using (var connection = await OpenConnectionAsync())
            using (var command = new SqlCommand(
                    "INSERT INTO [master].[dbo].[Order] ([IdProduct], [Amount], [CreatedAt], [FulfilledAt]) " +
                    "VALUES (@IdProduct, @Amount, @CreatedAt, null); SELECT SCOPE_IDENTITY()", connection))
            {
                command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
                command.Parameters.AddWithValue("@Amount", warehouse.Amount);
                command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedDateTime);
                var orderIdentity = await command.ExecuteScalarAsync();
                orderId = Convert.ToInt32(orderIdentity);
                await UpdateFull(warehouse.CreatedDateTime, (decimal)orderIdentity);
            }
            return orderId;
        }

        public async Task<int> InsertProduct(Warehouse warehouse, int orderId)
        {
            int productWarehouseId;
            using (var connection = await OpenConnectionAsync())
            using (var command = new SqlCommand(@"
                DECLARE @ProductId INT;
                SELECT @ProductId = [Price] FROM [master].[dbo].[Product] WHERE [IdProduct] = @IdProduct;
                INSERT INTO [master].[dbo].[Product_Warehouse] ([IdWarehouse], [IdProduct], [IdOrder], [Amount], [Price], [CreatedAt]) 
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @ProductId, @CreatedAt);
                SELECT SCOPE_IDENTITY()", connection))
            {
                command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
                command.Parameters.AddWithValue("@IdWarehouse", warehouse.WarehouseId);
                command.Parameters.AddWithValue("@IdOrder", orderId);
                command.Parameters.AddWithValue("@Amount", warehouse.Amount);
                command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedDateTime);
                var productPrice = await command.ExecuteScalarAsync();
                command.Parameters.AddWithValue("@Price", warehouse.Amount * (int)productPrice);
                var idProductWarehouse = await command.ExecuteScalarAsync();
                productWarehouseId = Convert.ToInt32(idProductWarehouse);
            }
            return productWarehouseId;
        }

        public async Task<string> AddProductByProcedure(Warehouse warehouse)
        {
            try
            {
                string result = await ExecProcedure(warehouse);
                return result;
            }
            catch (SqlException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> ExecProcedure(Warehouse warehouse)
        {
            try
            {
                using var connection = await OpenConnectionAsync();
                using var command = new SqlCommand("AddProductToWarehouse", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (ProductExists(warehouse.ProductId))
                {
                    command.Parameters.AddWithValue("@IdProduct", warehouse.ProductId);
                    command.Parameters.AddWithValue("@IdWarehouse", warehouse.WarehouseId);
                    command.Parameters.AddWithValue("@Amount", warehouse.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", warehouse.CreatedDateTime);
                    var result = await command.ExecuteScalarAsync();
                    return result.ToString();
                }
                else
                {
                    return "Product doesn't exists";
                }
            }
            catch (SqlException ex)
            {
                return ex.Message;
            }
        }

        private bool ProductExists(int productId)
        {
            try
            {
                using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
                using var command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Id = @IdProduct", connection);
                command.Parameters.AddWithValue("@IdProduct", productId);
                connection.Open();
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }
}
