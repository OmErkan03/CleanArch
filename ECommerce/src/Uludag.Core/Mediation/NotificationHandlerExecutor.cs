namespace Uludag.Core.Mediation;

/// <summary>
/// Yayınlanacak bir bildirim için tek bir handler çağrısını temsil eder.
/// </summary>
/// <param name="HandlerInstance">Çağrılacak handler nesnesi (loglama vb. için).</param>
/// <param name="HandlerCallback">Handler'ı çalıştıran temsilci.</param>
public sealed record NotificationHandlerExecutor(object HandlerInstance, Func<CancellationToken, Task> HandlerCallback);
