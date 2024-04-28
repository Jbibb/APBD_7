using System.ComponentModel.DataAnnotations;
using System.Data;
using APBD_7.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using APBD_7.Validators;
using FluentValidation;
using ValidationException = FluentValidation.ValidationException;

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

    using (connection)
    {
        var transaction = await connection.BeginTransactionAsync();

        var c1 = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @1", connection, (SqlTransaction) transaction);
        c1.Parameters.AddWithValue("@1", request.IdProduct);
        var reader = await c1.ExecuteReaderAsync();

        if (!reader.HasRows)
            return Results.NotFound(); 
        return Results.Ok(reader.ReadAsync());

    }
});
app.Run();
