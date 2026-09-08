using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Канонический срез климатического состояния проекта, единственный writable owner
    /// проектных климатических значений внутри <see cref="IProjectSession"/>.
    /// </summary>
    public interface IProjectSessionClimateState
    {
        /// <summary>
        /// Текущий непротиворечивый срез состояния.
        /// </summary>
        ClimateStateSnapshot Snapshot { get; }

        /// <summary>
        /// Событие возникает только при фактическом изменении среза.
        /// </summary>
        event EventHandler<ClimateStateChangedEventArgs>? Changed;

        /// <summary>
        /// Применить выбор города.
        /// </summary>
        /// <param name="city">Выбранный город или <c>null</c> для сброса.</param>
        /// <param name="isHighRequirements">Повышенные требования.</param>
        /// <param name="origin">Источник мутации.</param>
        /// <returns>Результат мутации с origin и срезами до/после.</returns>
        ClimateMutationResult ApplyCitySelection(CityInfo? city, bool isHighRequirements, ClimateMutationOrigin origin);

        /// <summary>
        /// Применить одну индивидуальную правку скалярного значения.
        /// </summary>
        /// <param name="edit">Правка.</param>
        /// <param name="origin">Источник мутации.</param>
        /// <returns>Результат мутации; невалидный ввод не изменяет состояние.</returns>
        ClimateMutationResult ApplyIndividualEdit(ClimateEdit edit, ClimateMutationOrigin origin);

        /// <summary>
        /// Применить срез климатических данных из сохранённого проекта.
        /// </summary>
        /// <param name="data">Данные проекта.</param>
        /// <param name="city">Город, если известен, для восстановления холодной пятидневки.</param>
        /// <param name="origin">Источник мутации.</param>
        /// <returns>Результат мутации.</returns>
        ClimateMutationResult ApplyProjectSnapshot(ClimateProjectData data, CityInfo? city, ClimateMutationOrigin origin);

        /// <summary>
        /// Сбросить климатические значения к дефолтам.
        /// </summary>
        /// <param name="origin">Источник мутации.</param>
        /// <returns>Результат мутации.</returns>
        ClimateMutationResult ResetToDefaults(ClimateMutationOrigin origin);

        /// <summary>
        /// Сбросить скалярные климатические значения к данным выбранного города.
        /// </summary>
        /// <param name="city">Город, к данным которого сбрасываться. Если <c>null</c>,
        /// используется текущий <see cref="ClimateStateSnapshot.SelectedCity"/> и сохранённая температура.</param>
        /// <param name="origin">Источник мутации.</param>
        /// <returns>Результат мутации.</returns>
        ClimateMutationResult ResetToCityData(CityInfo? city, ClimateMutationOrigin origin);

        /// <summary>
        /// Каноническое применение полного снимка при отмене/возврате действия
        /// (ADR-014): прямое присваивание всех 12 полей снимка, включая
        /// <c>HasUserModifications</c> и <c>Period0Days</c>, без нормализации и без
        /// origin-пересчётов. Предназначено только для origins
        /// <see cref="ClimateMutationOrigin.Undo"/>/<see cref="ClimateMutationOrigin.Redo"/>.
        /// </summary>
        /// <param name="snapshot">Снимок «до»/«после» из дневника отмены.</param>
        /// <param name="origin">Источник мутации (<see cref="ClimateMutationOrigin.Undo"/>
        /// или <see cref="ClimateMutationOrigin.Redo"/>).</param>
        /// <returns>Результат мутации.</returns>
        ClimateMutationResult ApplySnapshot(ClimateStateSnapshot snapshot, ClimateMutationOrigin origin);
    }
}
