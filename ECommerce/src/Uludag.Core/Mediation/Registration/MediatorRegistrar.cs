using System.Reflection;

namespace Uludag.Core.Mediation.Registration;

/// <summary>
/// Verilen assembly'leri tarayıp mediator için gerekli tüm DI kayıtlarını üretir.
/// Hiçbir DI kütüphanesine bağımlı değildir; çıktı, kullanan projede istenen container'a aktarılır:
/// <code>
/// foreach (var d in MediatorRegistrar.CreateDescriptors(cfg => cfg.RegisterServicesFromAssemblyContaining&lt;X&gt;()))
///     services.Add(new ServiceDescriptor(d.ServiceType, d.ImplementationType, (ServiceLifetime)d.Lifetime));
/// </code>
/// </summary>
public static class MediatorRegistrar
{
    public static IReadOnlyList<MediatorServiceDescriptor> CreateDescriptors(Action<MediatorConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MediatorConfiguration();
        configure(configuration);
        return CreateDescriptors(configuration);
    }

    public static IReadOnlyList<MediatorServiceDescriptor> CreateDescriptors(MediatorConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var descriptors = new List<MediatorServiceDescriptor>();

        // 1) Mediator'ın kendisi
        var mediatorType = configuration.MediatorImplementationType;
        descriptors.Add(new(typeof(IMediator), mediatorType, configuration.MediatorLifetime));
        descriptors.Add(new(typeof(ISender), mediatorType, configuration.MediatorLifetime));
        descriptors.Add(new(typeof(IPublisher), mediatorType, configuration.MediatorLifetime));

        // 2) Handler'lar
        var requestHandlerOwners = new Dictionary<Type, Type>();

        foreach (var type in configuration.Assemblies.SelectMany(GetLoadableTypes).Where(IsConcreteClass))
        {
            foreach (var serviceType in GetGenericInterfaces(type, typeof(IRequestHandler<,>)))
            {
                // Bir istek için birden fazla handler olması mediator pattern'ine aykırıdır.
                if (requestHandlerOwners.TryGetValue(serviceType, out var existing))
                    throw new InvalidOperationException(
                        $"'{serviceType.GenericTypeArguments[0].FullName}' isteği için birden fazla handler bulundu: " +
                        $"'{existing.FullName}' ve '{type.FullName}'.");

                requestHandlerOwners[serviceType] = type;
                descriptors.Add(new(serviceType, type, configuration.HandlerLifetime));
            }

            foreach (var serviceType in GetGenericInterfaces(type, typeof(INotificationHandler<>)))
                descriptors.Add(new(serviceType, type, configuration.HandlerLifetime));
        }

        // 3) Pipeline behavior'ları (eklenme sırası korunur)
        foreach (var behaviorType in configuration.BehaviorTypes)
        {
            if (behaviorType.IsGenericTypeDefinition)
            {
                descriptors.Add(new(typeof(IPipelineBehavior<,>), behaviorType, configuration.BehaviorLifetime));
                continue;
            }

            foreach (var serviceType in GetGenericInterfaces(behaviorType, typeof(IPipelineBehavior<,>)))
                descriptors.Add(new(serviceType, behaviorType, configuration.BehaviorLifetime));
        }

        return descriptors;
    }

    internal static IEnumerable<Type> GetGenericInterfaces(Type type, Type openGenericInterface)
        => type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

    // Açık generic handler'lar (ör. GenericHandler<T>) otomatik taramaya dahil edilmez.
    private static bool IsConcreteClass(Type type)
        => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false };

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
