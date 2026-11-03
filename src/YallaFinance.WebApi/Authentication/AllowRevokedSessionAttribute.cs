namespace YallaFinance.WebApi.Authentication;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowRevokedSessionAttribute : Attribute;
