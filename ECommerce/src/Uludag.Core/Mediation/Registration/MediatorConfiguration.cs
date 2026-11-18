using System.Reflection;

namespace Uludag.Core.Mediation.Registration;

/// <summary>
/// Mediator kayıt ayarları: taranacak assembly'ler, pipeline behavior'ları ve yaşam süreleri.
/// </summary>
public sealed class MediatorConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<Type> _behaviorTypes = [];

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;
    internal IReadOnlyList<Type> BehaviorTypes => _behaviorTypes;

    /// <summary>Handler'ların yaşam süresi. Varsayılan: Transient.</summary>
    public MediatorServiceLifetime HandlerLifetime { get; set; } = MediatorServiceLifetime.Transient;

    /// <summary>Pipeline behavior'larının yaşam süresi. Varsayılan: Transient.</summary>
    public MediatorServiceLifetime BehaviorLifetime { get; set; } = MediatorServiceLifetime.Transient;

    /// <summary>IMediator / ISender / IPublisher yaşam süresi. Varsayılan: Transient.</summary>
    public MediatorServiceLifetime MediatorLifetime { get; set; } = MediatorServiceLifetime.Transient;

    /// <summary>Varsayılan <see cref="Mediator"/> yerine kullanılacak özel implementasyon (ör. paralel publish).</summary>
    public Type MediatorImplementationType { get; private set; } = typeof(Mediator);

    public MediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            RegisterServicesFromAssembly(assembly);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Pipeline behavior ekler. Açık generic (ör. <c>typeof(LoggingBehavior&lt;,&gt;)</c>) ya da
    /// kapalı tip (ör. <c>typeof(CreateProductValidationBehavior)</c>) verilebilir.
    /// Eklenme sırası çalışma sırasıdır: ilk eklenen en dışta çalışır.
    /// </summary>
    public MediatorConfiguration AddBehavior(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsAbstract || behaviorType.IsInterface)
            throw new ArgumentException($"'{behaviorType.FullName}' somut bir sınıf olmalıdır.", nameof(behaviorType));

        if (!MediatorRegistrar.GetGenericInterfaces(behaviorType, typeof(IPipelineBehavior<,>)).Any())
            throw new ArgumentException($"'{behaviorType.FullName}' IPipelineBehavior<,> uygulamıyor.", nameof(behaviorType));

        if (!_behaviorTypes.Contains(behaviorType))
            _behaviorTypes.Add(behaviorType);
        return this;
    }

    public MediatorConfiguration AddBehavior<TBehavior>() => AddBehavior(typeof(TBehavior));

    public MediatorConfiguration UseMediator<TMediator>() where TMediator : IMediator
    {
        MediatorImplementationType = typeof(TMediator);
        return this;
    }
}
