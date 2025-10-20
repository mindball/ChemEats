using Domain.Infrastructure.Identity;
using StronglyTypedIds;

namespace Domain.Entities;
//
// public class Employee : Entity<EmployeeId>
// {
//     private readonly List<MealOrder> _orders = new();
//
//     private Employee() { }
//
//     public Employee(EmployeeId id, string fullName, string abbreviation)
//     {
//         Id = id;
//         FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
//         Abbreviation = abbreviation ?? throw new ArgumentNullException(nameof(abbreviation));
//     }
//
//     public string FullName { get; private set; }
//     public string Abbreviation { get; private set; }
//     public IReadOnlyCollection<MealOrder> Orders => _orders.AsReadOnly();
//
//     public Guid? UserId { get; private set; }
//     public ApplicationUser? User { get; private set; }
//
//     public void AddOrder(MealOrder order)
//     {
//         if (order is null) throw new ArgumentNullException(nameof(order));
//         _orders.Add(order);
//     }
//
//     public void Update(string fullName, string abbreviation)
//     {
//         FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
//         Abbreviation = abbreviation ?? throw new ArgumentNullException(nameof(abbreviation));
//     }
// }