using System.ComponentModel.DataAnnotations;
namespace APBD_7.DTOs;

public record AddProductDTO(
    [Required] int IdProduct,
    [Required] int IdWarehouse,
    [Required] int Amount,
    [Required] DateTime CreatedAt
);