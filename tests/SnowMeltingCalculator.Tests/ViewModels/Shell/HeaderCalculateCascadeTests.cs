using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.ViewModels.Shell;

/// <summary>
/// Шапочная «Рассчитать» (решение владельца 2026-09-06): публикация валидного
/// теплового результата будит реактивный каскад гидравлики
/// (HydraulicsStateCoordinator.OnContextChanged → CalculateAll), а шелл ведёт
/// на Тепловой шаг при отсутствии свежего валидного результата — иначе
/// нажатие с другого шага выглядело молчаливым no-op (статус-бар показывает
/// сообщения только текущего шага).
/// </summary>
[TestFixture]
public class HeaderCalculateCascadeTests
{
    private ServiceProvider _provider = null!;
    private MainViewModel _main = null!;
    private ThermalViewModel _thermal = null!;
    private CircuitsViewModel _circuits = null!;
    private List<string> _circuitsPropertyChanged = null!;

    [SetUp]
    public void Setup()
    {
        // Без создания WPF Application: VM и сервисы не зависят от
        // DispatcherObject, а Application, созданный на потоке этой фикстуры,
        // ломает thread-affinity тестов EditorDialogService (они получают
        // MainWindow с другого потока — «владельцем является другой поток»).

        var services = new ServiceCollection();
        services.AddApplicationServices();
        _provider = services.BuildServiceProvider();

        _main = _provider.GetRequiredService<MainViewModel>();
        _thermal = _provider.GetRequiredService<ThermalViewModel>();
        _circuits = _provider.GetRequiredService<CircuitsViewModel>();

        // Свежий старт: активен шаг «Климат» (MainViewModel._currentNavigationTarget)
        _circuitsPropertyChanged = new List<string>();
        _circuits.PropertyChanged += (_, e) => _circuitsPropertyChanged.Add(e.PropertyName!);
    }

    [TearDown]
    public void TearDown()
    {
        _provider?.Dispose();
    }

    [Test]
    public async Task HeaderCalculate_WithInvalidThermal_NavigatesToThermalAndSkipsCascade()
    {
        // Свежий старт: тип трубы не выбран → тепловая валидация падает.
        await _main.HeaderCalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(_main.CurrentNavigationTarget, Is.EqualTo(NavigationTarget.Thermal),
                "Шелл должен перевести пользователя на Тепловой шаг, где ошибка видна в статус-баре.");
            Assert.That(_main.CurrentValidationText, Does.Contain("Тип трубы не задан"),
                "Статус-бар должен показать валидацию теплового модуля.");
            Assert.That(_circuitsPropertyChanged, Does.Not.Contain("Summary"),
                "При невалидном тепловом входе публикации результата нет — реактивный каскад не срабатывает.");
        });
    }

    [Test]
    public async Task HeaderCalculate_WithValidThermal_RunsHydraulicsCascadeWithoutNavigation()
    {
        // Валидный сценарий: реальный город из базы (климатические данные) +
        // канонические дефолты конструкции + стандартная труба → тепловой
        // результат валиден → каскад дожимает гидравлику, шелл остаётся
        // на текущем шаге.
        var climateState = _provider.GetRequiredService<IProjectSession>().ClimateState;
        var climateService = _provider.GetRequiredService<IClimateDataService>();
        await climateService.LoadClimateDataAsync(); // база городов грузится лениво
        var city = climateService.GetCityByName("Москва")
            ?? throw new AssertionException("Город «Москва» должен находиться в базе городов.");
        climateState.ApplyCitySelection(city, isHighRequirements: false, ClimateMutationOrigin.User);

        var materials = _provider.GetRequiredService<IMaterialRepository>();
        await materials.LoadMaterialsAsync(); // справочник материалов грузится лениво
        _provider.GetRequiredService<ConstructionDefaultStateInitializer>()
            .Apply(ConstructionMutationOrigin.Initialization);

        _thermal.SelectedPipe = _thermal.AvailablePipes.First();
        // Разумный режим для Москвы: низкая подача — обратка остаётся в допуске
        _thermal.SupplyTemperature = 25.0;

        await _main.HeaderCalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(_thermal.ValidationMessage, Is.Empty.Or.Null,
                "Тепловой расчёт по городским данным со стандартной трубой должен пройти без ошибок.");
            Assert.That(_circuitsPropertyChanged, Does.Contain("Summary"),
                "После теплового пересчёта каскад гидравлики должен выполниться реактивным хуком координатора.");
            Assert.That(_main.CurrentNavigationTarget, Is.EqualTo(NavigationTarget.Climate),
                "При валидном расчёте шелл не должен перескакивать между шагами.");
        });
    }
}
