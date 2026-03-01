namespace Shared;

public static class ApiRoutes
{
    public static class Menus
    {
        public const string Base = "api/menus";
        public const string Active = "active";
        public const string BySupplier = "supplier";
        public const string Date = "date";
        public const string ActiveUntil = "active-until";

        public static string ById(Guid menuId) => $"{Base}/{menuId}";
        public static string BySupplierId(Guid supplierId) => $"{Base}/{BySupplier}/{supplierId}";
        public static string UpdateDate(Guid menuId) => $"{Base}/{menuId}/{Date}";
        public static string UpdateActiveUntil(Guid menuId) => $"{Base}/{menuId}/{ActiveUntil}";
        public static string Delete(Guid menuId) => $"{Base}/{menuId}";
    }

    public static class AdminMenus
    {
        public const string Base = "api/admin/menus";

        public static string Finalize(Guid menuId) => $"{Base}/{menuId}/finalize";
    }

    public static class Orders
    {
        public const string Base = "api/mealorders";
        public const string Me = "me";
        public const string MeItems = "me/items";
        public const string MePayments = "me/payments";
        public const string MePaymentsSummary = "me/payments/summary";

        public static string ById(Guid orderId) => $"{Base}/{orderId}";
        public static string Delete(Guid orderId) => $"{Base}/{orderId}";
        public static string MarkAsPaid(Guid orderId) => $"{Base}/{orderId}/pay";
        public static string MeByMenu(Guid menuId) => $"{Base}/{Me}/menu/{menuId}";
    }

    public static class AdminOrders
    {
        public const string Base = "api/admin/mealorders";
        public const string OrderPay = "order-pay";

        public static string Unpaid(string userId) => $"{Base}/unpaid/{Uri.EscapeDataString(userId)}";
        public static string Period(string userId) => $"{Base}/period/{Uri.EscapeDataString(userId)}";
    }

    public static class Suppliers
    {
        public const string Base = "api/suppliers";

        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Settings
    {
        public const string Base = "api/settings";
        public const string Portion = "portion";
    }

    public static class Employees
    {
        public const string SyncEmployees = "api/sync-employees";
        public const string Base = "api/employees";
        public const string Login = "api/login";
    }

    public static class Reports
    {
        public const string Base = "api/reports";

        public static string MenuReport(Guid menuId) => $"{Base}/menu/{menuId}";
    }
}
