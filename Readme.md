# 23.09.2025
## ����� ���� �� stronglytype Id-����
## ������� ���������� ���� ����� WebApp, ���� ���� ��� �������� ������
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

# 06.10.2025 ������ �������� menu �� �� ����� �� ������� � ��� � ��� ������� ��������� �� ������ �� ��� �������
# Pagination not working when click in numbers