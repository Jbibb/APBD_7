using System.Data;
using APBD_7.DTOs;
using System.Data.SqlClient;
using APBD_7.Validators;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IValidator<AddProductDTO>, AddProductRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("warehouse", async (AddProductDTO request, IConfiguration configuration, IValidator<AddProductDTO> validator) =>
{
    var validate = await validator.ValidateAsync(request);
    if (!validate.IsValid)
    {
        return Results.ValidationProblem(validate.ToDictionary());
    }
    
    var connection = new SqlConnection(configuration.GetConnectionString("Default"));
    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using (connection)
    {
        var transaction = await connection.BeginTransactionAsync();
        try
        {

            var c1 = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @1", connection,
                (SqlTransaction)transaction);
            c1.Parameters.AddWithValue("@1", request.IdProduct);
            var reader = await c1.ExecuteReaderAsync();

            if (!reader.HasRows)
                return Results.NotFound("Product not found");
            await reader.CloseAsync();

            var c2 = new SqlCommand("SELECT * FROM \"Order\" WHERE IdProduct = @1 AND Amount = @2", connection,
                (SqlTransaction)transaction);
            c2.Parameters.AddWithValue("@1", request.IdProduct);
            c2.Parameters.AddWithValue("@2", request.Amount);

            reader = await c2.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return Results.NotFound("Order matching idProduct and amount not found");
            }

            await reader.ReadAsync();
            if (reader.GetDateTime("CreatedAt").CompareTo(request.CreatedAt) >= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "CreatedAt", new[] { "CreatedAt date in request is earlier than CreatedAt date in order" } }
                });
            }

            var idOrder = reader.GetInt32("IdOrder");
            await reader.CloseAsync();

            var c3 = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @1", connection,
                (SqlTransaction)transaction);
            c3.Parameters.AddWithValue("@1", idOrder);

            reader = await c3.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                return Results.BadRequest("Order already realised");
            }

            await reader.CloseAsync();

            var c4 = new SqlCommand("UPDATE \"Order\" SET FulfilledAt = @1 WHERE IdOrder = @2", connection,
                (SqlTransaction)transaction);
            c4.Parameters.AddWithValue("@1", DateTime.Now);
            c4.Parameters.AddWithValue("@2", idOrder);
            await c4.ExecuteNonQueryAsync();

            var c5 = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @1", connection,
                (SqlTransaction)transaction);
            c5.Parameters.AddWithValue("@1", request.IdProduct);
            reader = await c5.ExecuteReaderAsync();

            await reader.ReadAsync();
            var productPrice = reader.GetDecimal("Price");
            await reader.CloseAsync();


            var c6 = new SqlCommand(
                "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) OUTPUT inserted.IdProductWarehouse VALUES (@1, @2, @3, @4, @5, @6)",
                connection, (SqlTransaction)transaction);
            c6.Parameters.AddWithValue("@1", request.IdWarehouse);
            c6.Parameters.AddWithValue("@2", request.IdProduct);
            c6.Parameters.AddWithValue("@3", request.IdProduct);
            c6.Parameters.AddWithValue("@4", request.Amount);
            c6.Parameters.AddWithValue("@5", request.Amount * productPrice);
            c6.Parameters.AddWithValue("@6", DateTime.Now);

            var newRecordId = (int)(await c6.ExecuteScalarAsync())!;
            await transaction.CommitAsync();
            return Results.Ok(newRecordId);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
        }
        
        

    }
});
app.Run();
