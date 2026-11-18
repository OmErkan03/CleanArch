namespace Uludag.Core.Mediation.Registration;

/// <summary>
/// Değerler Microsoft.Extensions.DependencyInjection.ServiceLifetime ile birebir aynıdır,
/// bu yüzden doğrudan cast edilebilir: <c>(ServiceLifetime)descriptor.Lifetime</c>.
/// </summary>
public enum MediatorServiceLifetime
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}

/// <summary>
/// DI container'a eklenmesi gereken tek bir kaydı tanımlar. Container'dan bağımsızdır;
/// kullanan proje bu kayıtları kendi container'ına (ör. IServiceCollection) aktarır.
/// </summary>
public sealed record MediatorServiceDescriptor(Type ServiceType, Type ImplementationType, MediatorServiceLifetime Lifetime);
