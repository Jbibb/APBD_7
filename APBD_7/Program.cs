using System.Data;
using APBD_7.DTOs;
using System.Data.SqlClient;
using APBD_7.Services;
using APBD_7.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IValidator<AddProductRequestDTO>, AddProductRequestValidator>();
builder.Services.AddTransient<IValidator<OrderAndRequestCreatedAtDTO>, OrderDateValidator>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("warehouse", async (AddProductRequestDTO request, IProductService productService, IValidator<AddProductRequestDTO> requestValidator, IValidator<OrderAndRequestCreatedAtDTO> orderDateValidator) =>
{
    var validate = await requestValidator.ValidateAsync(request);
    if (!validate.IsValid)
    {
        return Results.ValidationProblem(validate.ToDictionary());
    }

    if (!await productService.ProductIdExists(request.IdProduct))
    {
        return Results.NotFound("Product with id = " + request.IdProduct + " not found");
    }
    
    if (!await productService.WarehouseIdExists(request.IdWarehouse))
    {
        return Results.NotFound("Warehouse with id = " + request.IdWarehouse + " not found");
    }

    var orderData = await productService.GetMatchingOrder(request.IdProduct, request.Amount);
    if (orderData is null)
    {
        return Results.NotFound("Order matching idProduct = " + request.IdProduct + " and amount = " + request.Amount + " not found");
    }

    validate = await orderDateValidator.ValidateAsync(new OrderAndRequestCreatedAtDTO(orderData.CreatedAt, request.CreatedAt));
    if (!validate.IsValid)
    {
        return Results.ValidationProblem(validate.ToDictionary());
    }

    if (await productService.IsOrderRealised(orderData.IdOrder))
    {
        return Results.BadRequest("Order already realised");
    }

    try
    {
        await productService.BeginTransaction();

        await productService.UpdateOrderFulfilledAt(orderData.IdOrder);
        int? newRecordId = await productService.InsertToWarehouse(request);

        await productService.CommitTransaction();

        return Results.Created("nie ma", newRecordId);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        await productService.RollbackTransaction();
    }
    return Results.BadRequest();
});
app.Run();
