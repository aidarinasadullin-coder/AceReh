// ================================================================================
// Фаза 1Б редизайна — база smoke-фикстур: один запуск приложения на фикстуру.
// ================================================================================

using System;
using System.Threading;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

/// <summary>
/// База UiSmoke-фикстур: запуск реального приложения один раз на фикстуру
/// (старт SelfContained-exe долог), грейсфул-закрытие в teardown.
/// Все тесты наследника попадают в категорию UiSmoke.
/// </summary>
[Category("UiSmoke")]
[Apartment(ApartmentState.STA)]
public abstract class UiSmokeFixtureBase
{
    protected UiSmokeApplication App { get; private set; } = null!;
    /// <summary>Аргументы командной строки запуска exe (пусто — старт без проекта).</summary>
    protected virtual string[] LaunchArguments => Array.Empty<string>();

    [OneTimeSetUp]
    public void LaunchApplication()
    {
        App = UiSmokeApplication.Launch(LaunchArguments);
    }

    [OneTimeTearDown]
    public void CloseApplication()
    {
        // OneTimeTearDown выполняется и при провале OneTimeSetUp (App ещё null)
        App?.Dispose();
    }
}
