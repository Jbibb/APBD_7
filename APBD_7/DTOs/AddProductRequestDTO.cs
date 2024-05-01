namespace APBD_7.DTOs;

public record AddProductRequestDTO(
    int IdProduct,
    int IdWarehouse,
    int Amount,
    DateTime CreatedAt
);