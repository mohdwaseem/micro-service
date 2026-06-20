namespace ProductService.DTOs;

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category);
