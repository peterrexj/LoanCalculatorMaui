namespace LoanCalculator.Core.Services;

public static class ServiceLocator
{
    public static IServiceProvider ServiceProvider { get; set; }

    public static T GetService<T>() => ServiceProvider.GetRequiredService<T>();
}
