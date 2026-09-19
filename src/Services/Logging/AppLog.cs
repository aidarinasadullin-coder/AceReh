using System.IO;

namespace SnowMeltingCalculator.Services.Logging
{
    /// <summary>
    /// Статический фасад над <see cref="IAppLog"/>: позволяет логировать из
    /// любых мест (статические классы, конвертеры, catch-блоки) без
    /// конструкторной инъекции в десятки классов. Экземпляр регистрируется
    /// один раз в composition root; до регистрации фасад молчит (лог не
    /// может быть причиной сбоя приложения).
    /// </summary>
    public static class AppLog
    {
        private static IAppLog? _instance;

        public static void Register(IAppLog instance) => _instance = instance;

        public static void Info(string message) => _instance?.Info(message);

        public static void Warn(string message) => _instance?.Warn(message);

        /// <summary>Предупреждение с контекстом исключения: «контекст: Тип: сообщение».</summary>
        public static void Warn(Exception exception, string context) =>
            _instance?.Warn($"{context}: {Format(exception)}");

        public static void Error(string message, Exception? exception = null) =>
            _instance?.Error(message, exception);

        private static string Format(Exception exception) =>
            $"{exception.GetType().Name}: {exception.Message}";
    }
}
