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
        public const string ParseFile = "parse-file";

        // Route templates (used by WebApi endpoint mapping)
        public const string ByIdRoute = "{menuId:guid}";
        public const string BySupplierRoute = BySupplier + "/{supplierId:guid}";
        public const string UpdateDateRoute = "{menuId:guid}/" + Date;
        public const string UpdateActiveUntilRoute = "{menuId:guid}/" + ActiveUntil;

        // URL builders (used by WebApp HTTP client)
        public static string ById(Guid menuId) => $"{Base}/{menuId}";
        public static string BySupplierId(Guid supplierId) => $"{Base}/{BySupplier}/{supplierId}";
        public static string UpdateDate(Guid menuId) => $"{Base}/{menuId}/{Date}";
        public static string UpdateActiveUntil(Guid menuId) => $"{Base}/{menuId}/{ActiveUntil}";
        public static string Delete(Guid menuId) => $"{Base}/{menuId}";
    }

    public static class AdminMenus
    {
        public const string Base = "api/admin/menus";

        // Route templates (used by WebApi endpoint mapping)
        public const string FinalizeRoute = "{menuId:guid}/finalize";

        // URL builders (used by WebApp HTTP client)
        public static string Finalize(Guid menuId) => $"{Base}/{menuId}/finalize";
    }

    public static class Orders
    {
        public const string Base = "api/mealorders";
        public const string Me = "me";
        public const string MeItems = "me/items";
        public const string MePayments = "me/payments";
        public const string MePaymentsSummary = "me/payments/summary";

        // Route templates (used by WebApi endpoint mapping)
        public const string ByIdRoute = "{orderId:guid}";
        public const string MarkAsPaidRoute = "{orderId:guid}/pay";
        public const string MeByMenuRoute = "me/menu/{menuId:guid}";

        // URL builders (used by WebApp HTTP client)
        public static string ById(Guid orderId) => $"{Base}/{orderId}";
        public static string Delete(Guid orderId) => $"{Base}/{orderId}";
        public static string MarkAsPaid(Guid orderId) => $"{Base}/{orderId}/pay";
        public static string MeByMenu(Guid menuId) => $"{Base}/{Me}/menu/{menuId}";
    }

    public static class AdminOrders
    {
        public const string Base = "api/admin/mealorders";
        public const string OrderPay = "order-pay";

        // Route templates (used by WebApi endpoint mapping)
        public const string UnpaidRoute = "unpaid/{userId}";
        public const string PeriodRoute = "period/{userId}";

        // URL builders (used by WebApp HTTP client)
        public static string Unpaid(string userId) => $"{Base}/unpaid/{Uri.EscapeDataString(userId)}";
        public static string Period(string userId) => $"{Base}/period/{Uri.EscapeDataString(userId)}";
    }

    public static class Suppliers
    {
        public const string Base = "api/suppliers";

        // Route templates (used by WebApi endpoint mapping)
        public const string ByIdRoute = "{id:guid}";

        // URL builders (used by WebApp HTTP client)
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
        public const string MyPassword = "me/password";

        // Route templates (used by WebApi endpoint mapping)
        public const string RolesRoute = "{userId}/roles/{roleName}";
        public const string ResetPasswordRoute = "{userId}/password/reset";

        // URL builders (used by WebApp HTTP client)
        public static string RoleAction(string userId, string roleName) =>
            $"{Base}/{Uri.EscapeDataString(userId)}/roles/{Uri.EscapeDataString(roleName)}";

        public static string ChangeMyPassword() =>
            $"{Base}/{MyPassword}";

        public static string ResetPassword(string userId) =>
            $"{Base}/{Uri.EscapeDataString(userId)}/password/reset";
    }

    public static class Reports
    {
        public const string Base = "api/reports";

        // Route templates (used by WebApi endpoint mapping)
        public const string MenuReportRoute = "menu/{menuId:guid}";

        // URL builders (used by WebApp HTTP client)
        public static string MenuReport(Guid menuId) => $"{Base}/menu/{menuId}";
    }
}
