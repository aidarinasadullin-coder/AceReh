namespace SnowMeltingCalculator.Services.Logging;

/// <summary>
/// Уровни локального журнала (волна 4 hardening-роадмапа, D2).
/// </summary>
public enum AppLogLevel
{
    Info,
    Warn,
    Error
}

/// <summary>
/// Локальный журнал приложения: файл в %LOCALAPPDATA%, без телеметрии
/// (решение владельца D2). Единственная цель — воспроизводимость
/// инцидентов на машине пользователя.
/// </summary>
public interface IAppLog
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}
