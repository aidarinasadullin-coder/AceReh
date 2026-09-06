using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.ViewModels.Construction;
using System.Linq;
using System.Windows;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Провайдер сервисов (для доступа из других мест)
        /// </summary>
        public static IServiceProvider? Services { get; private set; }

        /// <summary>
        /// При запуске приложения
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Завершение — по закрытию MainWindow (P2-3 ревью Ф7): сплэш
            // закрывается раньше/позже главного окна, и при дефолтном
            // OnLastWindowClose его закрытие в catch-ветке убило бы
            // приложение до показа диалога ошибки.
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Русская локаль чисел во всех биндингах (запятая, пробел-тысячи) —
            // решение владельца; до любых окон и биндингов.
            Core.AppCulture.PinBindingCulture();

            // Сплэш (Ф7.2, рендер 06): показ до загрузки климата и
            // материалов — пользователь видит бренд, а не «зависший» стол.
            var splashStart = System.Diagnostics.Stopwatch.StartNew();
            var splash = new SplashWindow();
            splash.Show();

            // Настройка DI
            var services = new ServiceCollection();

            // Регистрация всех сервисов приложения
            services.AddApplicationServices();

            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;

            try
            {
                // Загрузка климатических данных
                var climateService = _serviceProvider.GetRequiredService<IClimateDataService>();
                await climateService.LoadClimateDataAsync();

                // Загрузка материалов для конструктора конструкции
                var materialRepository = _serviceProvider.GetRequiredService<IMaterialRepository>();
                await materialRepository.LoadMaterialsAsync();

                // Инициализация ConstructionViewModel
                var constructionViewModel = _serviceProvider.GetRequiredService<ConstructionViewModel>();
                await constructionViewModel.InitializeCommand.ExecuteAsync(null);

                // Определяем путь к файлу проекта, переданный через командную строку
                // (например, при двойном клике по файлу .smc в проводнике Windows)
                string? startupProjectPath = SelectStartupProjectPath(e.Args);

                // Создание главного окна
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                if (!string.IsNullOrEmpty(startupProjectPath))
                {
                    mainWindow.InitialProjectPath = startupProjectPath;
                }
                mainWindow.Show();

                // MainWindow — явно: WPF назначает Application.MainWindow на
                // первое показанное окно (сплэш), и с OnMainWindowClose его
                // закрытие завершило бы приложение (вскрыто UiSmoke Ф7).
                MainWindow = mainWindow;

                // Сплэш живёт суммарно ≥ MinSplashDuration и закрывается
                // fade-out'ом уже поверх показанного главного окна.
                var remaining = SplashWindow.MinSplashDuration - splashStart.Elapsed;
                await splash.CloseAfterDelayAsync(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске приложения: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"InnerException StackTrace: {ex.InnerException.StackTrace}");
                }

                // Диалог до закрытия сплэша: с OnMainWindowClose приложение
                // не завершится между окнами; сплэш (Topmost) гасим после,
                // чтобы он не перекрыл MessageBox.
                MessageBox.Show(
                    $"Ошибка при запуске приложения:\n{ex.Message}\n\n{(ex.InnerException != null ? ex.InnerException.Message : "")}\n\n{ex.StackTrace}",
                    "Ошибка запуска",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                splash.Close();
                Shutdown();
            }
        }

        internal static string? SelectStartupProjectPath(IEnumerable<string> arguments)
        {
            return arguments.FirstOrDefault(argument =>
                !string.IsNullOrWhiteSpace(argument) &&
                argument.EndsWith(".smc", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// При завершении приложения освобождаем корневой ServiceProvider
        /// (все singleton-сервисы, реализующие IDisposable).
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Services = null;
            _serviceProvider = null;

            base.OnExit(e);
        }
    }
}
