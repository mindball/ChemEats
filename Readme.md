# 23.09.2025
## Сложи типа на stronglytype Id-тата
## Направи миграциите през Новия WebApp, също така съм забърсал базата
## added other atrributes on entities such as required
# 27.09:

public class SupplierRequest
{
    [Required(ErrorMessage = "Supplier name is required")]
    [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
}

# remove automapper
# why after migration string fields has inserted into db like memo ?
#SaveMenusAsync 

# 06.10.2025 когато добавяме menu не ни спира да добавим и още и още някаквъ механицъм за защита да има помисли
# Pagination not working when click in numbers