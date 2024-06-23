using Microsoft.Data.SqlClient;

namespace APBD_6.Warehouse;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouse(ProductWarehouse productWarehouse)
    {
        if (productWarehouse.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        
        command.CommandText = "SELECT COUNT(*)" +
                              " FROM Product" +
                              " WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", productWarehouse.IdProduct);

        if ((int)(await command.ExecuteScalarAsync())! == 0)
            throw new ArgumentException("Product does not exist");
        
        command.CommandText = "SELECT COUNT(*)" +
                              " FROM Warehouse" +
                              " WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", productWarehouse.IdWarehouse);
        if ((int)(await command.ExecuteScalarAsync())! == 0)
            throw new ArgumentException("Warehouse does not exist");
        
        command.CommandText =
            "SELECT COUNT(*)" +
            " FROM [Order]" +
            " WHERE IdProduct = @IdProduct" +
            " AND Amount = @Amount" +
            " AND CreatedAt < @CreatedAt";
        command.Parameters.AddWithValue("@Amount", productWarehouse.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productWarehouse.CreatedAt);
        if ((int)(await command.ExecuteScalarAsync())! == 0)
            throw new ArgumentException("Order does not exist for this product");
        
        command.CommandText =
            "SELECT IdOrder" +
            " FROM [Order]" +
            " WHERE IdProduct = @IdProduct" +
            " AND Amount = @Amount" +
            " AND CreatedAt < @CreatedAt";
        var orderId = (int)(await command.ExecuteScalarAsync())!;
        
        command.CommandText = "SELECT Price" +
                              " FROM Product" +
                              " WHERE IdProduct = @IdProduct";
        var price = (decimal)(await command.ExecuteScalarAsync())!;
        
        command.CommandText = "SELECT COUNT(*)" +
                              " FROM Product_Warehouse" +
                              " WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", orderId);
        if ((int)(await command.ExecuteScalarAsync())! != 0)
            throw new ArgumentException("Order has already been fulfilled");
        
        command.CommandText = "UPDATE [Order]" +
                              " SET FulfilledAt = @FulfilledAt" +
                              " WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync();
        
        var total = productWarehouse.Amount * price;
        command.CommandText =
            "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)" +
            " VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
        command.Parameters.AddWithValue("@Price", total);
        command.Parameters["@CreatedAt"].Value = DateTime.UtcNow;
        var createdId = await command.ExecuteNonQueryAsync();

        return createdId;
    }
}