namespace Uludag.Core.Mediation.Internal;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Yalnızca BCL'deki <see cref="IServiceProvider"/> kullanarak bir servisin tüm kayıtlarını çözer.
    /// IEnumerable&lt;T&gt; çözümlemesini desteklemeyen container'larda boş dizi döner.
    /// </summary>
    public static T[] GetServices<T>(IServiceProvider serviceProvider)
    {
        var services = serviceProvider.GetService(typeof(IEnumerable<T>)) as IEnumerable<T>;
        return services is null ? [] : services as T[] ?? services.ToArray();
    }
}
