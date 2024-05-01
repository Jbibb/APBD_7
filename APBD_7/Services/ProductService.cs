using System.Data;
using System.Data.SqlClient;
using APBD_7.DTOs;
using APBD_7.Models;

namespace APBD_7.Services;

public interface IProductService
{
    public Task<bool> ProductIdExists(int idProduct);
    public Task<bool> WarehouseIdExists(int idWarehouse);
    public Task<Order?> GetMatchingOrder(int idProduct, int amount);
    public Task<bool> IsOrderRealised(int idOrder);

    public Task BeginTransaction();
    public Task CommitTransaction();
    public Task RollbackTransaction();
    
    public Task UpdateOrderFulfilledAt(int idOrder);
    public Task<int> InsertToWarehouse(AddProductRequestDTO request);
    
}

public class ProductService(IConfiguration configuration) : IProductService
{
    private SqlTransaction? _transaction;
    private SqlConnection? _transactionConnection;
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<bool> ProductIdExists(int idProduct)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @1", connection);
        command.Parameters.AddWithValue("@1", idProduct);
        var value = await command.ExecuteScalarAsync();
        if (value is null)
            return false;
        return true;
    }

    public async Task<bool> WarehouseIdExists(int idWarehouse)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @1", connection);
        command.Parameters.AddWithValue("@1", idWarehouse);
        var value = await command.ExecuteScalarAsync();
        if (value is null)
            return false;
        return true;
    }

    public async Task<Order?> GetMatchingOrder(int idProduct, int amount)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand("SELECT * FROM \"Order\" WHERE IdProduct = @1 AND Amount = @2", connection);
        command.Parameters.AddWithValue("@1", idProduct);
        command.Parameters.AddWithValue("@2", amount);
        var reader = await command.ExecuteReaderAsync();
        await using (reader)
        {
            await reader.ReadAsync();
            if (!reader.HasRows)
            {
                return null;
            }

            var order = new Order
            {
                IdOrder = reader.GetInt32("IdOrder"),
                IdProduct = reader.GetInt32("IdProduct"),
                Amount = reader.GetInt32("Amount"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                FulfilledAt = reader.GetDateTime("FulfilledAt")
            };
            return order;
        }
    }

    public async Task<bool> IsOrderRealised(int idOrder)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @1", connection);
        command.Parameters.AddWithValue("@1", idOrder);
        var value = await command.ExecuteScalarAsync();
        if (value is null)
            return false;
        return true;
    }

    public async Task BeginTransaction()
    {
        if (_transaction is null)
        {
            _transactionConnection = await GetConnection();
            _transaction = (SqlTransaction) await _transactionConnection.BeginTransactionAsync();
        }
    }

    public async Task CommitTransaction()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
        }
    }
    
    public async Task RollbackTransaction()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
        }
    }

    
    public async Task UpdateOrderFulfilledAt(int idOrder)
    {
        if (_transaction is not null)
        {
            var command = new SqlCommand("UPDATE \"Order\" SET FulfilledAt = @1 WHERE IdOrder = @2", _transactionConnection,
                _transaction);
            command.Parameters.AddWithValue("@1", DateTime.Now);
            command.Parameters.AddWithValue("@2", idOrder);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<int> InsertToWarehouse(AddProductRequestDTO request)
    {
        var price = await GetProductPrice(request.IdProduct);
        if (price is null)
            throw new Exception("Unable to get product price");
        
        var command = new SqlCommand("INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) OUTPUT inserted.IdProductWarehouse VALUES (@1, @2, @3, @4, @5, @6)", _transactionConnection, _transaction);
        command.Parameters.AddWithValue("@1", request.IdWarehouse);
        command.Parameters.AddWithValue("@2", request.IdProduct);
        command.Parameters.AddWithValue("@3", request.IdProduct);
        command.Parameters.AddWithValue("@4", request.Amount);
        command.Parameters.AddWithValue("@5", request.Amount * price);
        command.Parameters.AddWithValue("@6", DateTime.Now);

        var newRecordId = (int)(await command.ExecuteScalarAsync())!;
        return newRecordId;
    }

    private async Task<Decimal?> GetProductPrice(int idProduct)
    {
        if (_transaction is not null)
        {
            var command = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @1", _transactionConnection, _transaction);
            command.Parameters.AddWithValue("@1", idProduct);
            var value = await command.ExecuteScalarAsync();
            return (Decimal)value;
        }
        return null;
    }
}