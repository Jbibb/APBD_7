namespace APBD_7.DTOs;

public record OrderAndRequestCreatedAtDTO(
    DateTime OrderCreatedAt,
    DateTime RequestCreatedAt
);