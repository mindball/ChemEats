namespace Domain.Models.Orders;

public sealed record EmployeeOutstandingSummary(
    string UserId,
    string EmployeeName,
    int UnpaidCount,
    decimal GrossAmount,      // Sum of snapshot prices
    decimal PortionAmount,    // Company portion (already applied + expected at payment)
    decimal NetAmount,        // GrossAmount - PortionAmount (min 0 per order)
    int EarlierUnpaidCount,   // Unpaid orders before the period start
    decimal EarlierNetAmount  // Net amount of those earlier orders
)
{
    public decimal TotalDue => NetAmount + EarlierNetAmount;
}
