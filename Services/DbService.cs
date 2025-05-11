using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NazwaProcedury";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
    }
    
    public async Task<int> AddProductToWarehouseAsync(WarehouseRequest request)
{
    await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
    await using SqlCommand command = new SqlCommand();
    command.Connection = connection;
    await connection.OpenAsync();

    DbTransaction transaction = await connection.BeginTransactionAsync();
    command.Transaction = transaction as SqlTransaction;

    try
    {
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        var result = await command.ExecuteScalarAsync();
        if (result == null) throw new ArgumentException("Product not found");
        
        command.Parameters.Clear();
        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        result = await command.ExecuteScalarAsync();
        if (result == null) throw new ArgumentException("Warehouse not found");

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");
        
        command.Parameters.Clear();
        command.CommandText = @"
            SELECT IdOrder, CreatedAt FROM [Order]
            WHERE IdProduct = @IdProduct AND Amount = @Amount";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@Amount", request.Amount);

        int idOrder = -1;
        DateTime createdAtOrder = DateTime.MinValue;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) throw new InvalidOperationException("Matching order not found");
            idOrder = reader.GetInt32(0);
            createdAtOrder = reader.GetDateTime(1);
        }

        if (createdAtOrder >= request.CreatedAt)
            throw new InvalidOperationException("Order must be created before delivery");
        
        command.Parameters.Clear();
        command.CommandText = @"
            SELECT 1 FROM Product_Warehouse
            WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        result = await command.ExecuteScalarAsync();
        if (result != null) throw new InvalidOperationException("Order already fulfilled");
        
        command.Parameters.Clear();
        command.CommandText = @"
            UPDATE [Order] SET FulfilledAt = @Now
            WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@Now", DateTime.Now);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        await command.ExecuteNonQueryAsync();
        
        command.Parameters.Clear();
        command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        var price = Convert.ToDecimal(await command.ExecuteScalarAsync());

        decimal totalPrice = price * request.Amount;

        command.Parameters.Clear();
        command.CommandText = @"
            INSERT INTO Product_Warehouse
            (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@Price", totalPrice);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        int newId = (int)(await command.ExecuteScalarAsync());

        await transaction.CommitAsync();
        return newId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
    public async Task<int> AddProductToWarehouseViaProcedureAsync(WarehouseRequest request)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await connection.OpenAsync();

        try
        {
            var result = await command.ExecuteScalarAsync();

            if (result == null || !int.TryParse(result.ToString(), out int newId))
            {
                throw new InvalidOperationException("Procedure did not return a valid ID.");
            }

            return newId;
        }
        catch (SqlException ex) when (ex.Class == 18)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }


}