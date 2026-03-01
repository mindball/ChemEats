using Domain.Common.Enums;

namespace Domain.Entities;

public static class PaymentTermsExtensions
{
    public static DateTime CalculateDueDate(this PaymentTerms terms, DateTime orderDate)
    {
        return orderDate.Date.AddDays((int)terms);
    }
}
