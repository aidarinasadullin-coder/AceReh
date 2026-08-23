using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Чистый маппер persistence DTO ↔ канонический тепловой срез (DEC-T08).
    /// Restore-половина (DTO → канонический кандидат) создана в Todo 9;
    /// save-половина (Snapshot → DTO) добавлена в Todo 10: save читает только
    /// <c>IProjectSession.ThermalState.Snapshot</c>. Класс не знает
    /// о состоянии, событиях и dirty — только преобразование значений.
    ///
    /// Контракт save (DEC-T08, точный wire-набор):
    /// - inputs: <c>SelectedMode, SupplyTemperature, GroundTemperature,
    ///   SelectedPipe.{Name,OuterDiameter,InnerDiameter,WallThickness},
    ///   PipeSpacing</c>;
    /// - result: ровно восемь полей <c>PowerUp, PowerDown, PowerTotal,
    ///   SupplyTemperature, ReturnTemperature, MeanTemperature, DeltaT,
    ///   IsValid</c>; null-результат даёт null в DTO;
    /// - никогда не персистятся статус, сообщения, origins, Article,
    ///   ThermalConductivity и канонические метаданные.
    ///
    /// Контракт restore (DEC-T08):
    /// - труба: совпадение со стандартной → каноническая труба берётся из
    ///   стандартного определения; неизвестная → первая стандартная (frozen
    ///   fallback); null → null;
    /// - отсутствующий legacy шаг укладки → DTO-инициализатор 200;
    /// - невалидный сохранённый результат НЕ становится каноническим: финализация
    ///   выполнит ровно один fallback-расчёт;
    /// - runtime-only поля результата (<c>Alpha</c>, <c>MeltingHeat</c>, ...) не
    ///   содержатся в wire-DTO и восстанавливаются CLR-дефолтами; каноническими
    ///   остаются ровно семь числовых полей плюс <c>IsValid</c>.
    /// Никогда не персистятся статус, сообщения, origins, Article,
    /// ThermalConductivity и канонические метаданные.
    /// </summary>
    public static class ThermalPersistenceMapper
    {
        /// <summary>
        /// Построить wire-DTO теплового раздела проекта из канонического снимка
        /// состояния (save-половина, DEC-T08). Переносятся ровно wire-поля:
        /// снимок трубы даёт четыре поля <see cref="PipeTypeProjectData"/>,
        /// снимок результата — восемь полей <see cref="ThermalResultProjectData"/>
        /// (включая <c>IsValid</c>); null-результат остаётся null. Статус,
        /// сообщения, origins, Article и ThermalConductivity не персистятся.
        /// </summary>
        public static ThermalProjectData BuildThermalProjectData(ThermalStateSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var inputs = snapshot.Inputs;
            return new ThermalProjectData
            {
                SelectedMode = inputs.Mode,
                SupplyTemperature = inputs.SupplyTemperature,
                GroundTemperature = inputs.GroundTemperature,
                PipeSpacing = inputs.PipeSpacing,
                SelectedPipe = inputs.Pipe is null ? null : new PipeTypeProjectData
                {
                    Name = inputs.Pipe.Name,
                    OuterDiameter = inputs.Pipe.OuterDiameter,
                    InnerDiameter = inputs.Pipe.InnerDiameter,
                    WallThickness = inputs.Pipe.WallThickness
                },
                Result = BuildResultProjectData(snapshot.Result)
            };
        }

        /// <summary>
        /// Построить wire-DTO сохранённого результата: ровно восемь полей
        /// контракта; runtime-only поля снимка (Alpha, MeltingHeat, ...) и
        /// ValidationErrors в DTO не попадают.
        /// </summary>
        private static ThermalResultProjectData? BuildResultProjectData(
            ThermalResultSnapshot? result)
        {
            if (result is null)
            {
                return null;
            }

            return new ThermalResultProjectData
            {
                PowerUp = result.PowerUp,
                PowerDown = result.PowerDown,
                PowerTotal = result.PowerTotal,
                SupplyTemperature = result.SupplyTemperature,
                ReturnTemperature = result.ReturnTemperature,
                MeanTemperature = result.MeanTemperature,
                DeltaT = result.DeltaT,
                IsValid = result.IsValid
            };
        }

        /// <summary>
        /// Построить канонического кандидата входных данных из thermal-DTO проекта.
        /// </summary>
        public static ThermalInputsSnapshot BuildInputsCandidate(
            ThermalProjectData? data,
            IReadOnlyList<PipeType> availablePipes)
        {
            if (data is null)
            {
                return ThermalInputsSnapshot.Default;
            }

            var pipe = ResolveStandardPipe(data.SelectedPipe, availablePipes);
            return new ThermalInputsSnapshot(
                data.SelectedMode,
                data.SupplyTemperature,
                data.GroundTemperature,
                ThermalPipeSnapshot.FromPipeType(pipe),
                data.PipeSpacing);
        }

        /// <summary>
        /// Разрешить сохранённую трубу в стандартное определение:
        /// структурное совпадение → соответствующая стандартная труба;
        /// несовпадение → первая стандартная (frozen fallback); null → null.
        /// </summary>
        public static PipeType? ResolveStandardPipe(
            PipeTypeProjectData? persisted,
            IReadOnlyList<PipeType> availablePipes)
        {
            if (persisted is null)
            {
                return null;
            }

            return ResolveStandardPipeCore(
                new PipeType
                {
                    Name = persisted.Name,
                    OuterDiameter = persisted.OuterDiameter,
                    InnerDiameter = persisted.InnerDiameter,
                    WallThickness = persisted.WallThickness
                },
                availablePipes);
        }

        /// <summary>
        /// Разрешить канонический снимок трубы в стандартное определение
        /// (для обновления адаптера ViewModel теми же правилами).
        /// </summary>
        public static PipeType? ResolveStandardPipe(
            ThermalPipeSnapshot? persisted,
            IReadOnlyList<PipeType> availablePipes)
        {
            if (persisted is null)
            {
                return null;
            }

            return ResolveStandardPipeCore(persisted.ToPipeType(), availablePipes);
        }

        private static PipeType? ResolveStandardPipeCore(
            PipeType persisted,
            IReadOnlyList<PipeType> availablePipes)
        {
            if (availablePipes is null || availablePipes.Count == 0)
            {
                return null;
            }

            // PipeType.Equals сравнивает Name/Outer/Inner/WallThickness — ровно
            // те wire-поля, которые сохраняет .smc (Article/λ не участвуют).
            return availablePipes.FirstOrDefault(p => p == persisted)
                ?? availablePipes[0];
        }

        /// <summary>
        /// Построить канонический снимок сохранённого результата. Null и
        /// невалидный результат дают null: невалидное сохранённое значение не
        /// становится финальным каноническим результатом (DEC-T08).
        /// </summary>
        public static ThermalResultSnapshot? BuildSavedResult(ThermalResultProjectData? result)
        {
            if (result is null || !result.IsValid)
            {
                return null;
            }

            return new ThermalResultSnapshot(
                alpha: 0.0,
                powerUp: result.PowerUp,
                powerDown: result.PowerDown,
                powerTotal: result.PowerTotal,
                meltingHeat: 0.0,
                radiationHeat: 0.0,
                convectionHeat: 0.0,
                excessTemperature: 0.0,
                meanTemperature: result.MeanTemperature,
                supplyTemperature: result.SupplyTemperature,
                returnTemperature: result.ReturnTemperature,
                deltaT: result.DeltaT,
                rFb: 0.0,
                rD: 0.0,
                parameterM: 0.0,
                efficiencyEtaR: 0.0,
                massFlowRate: 0.0,
                volumeFlowRate: 0.0,
                isValid: true,
                validationErrors: null);
        }

        /// <summary>
        /// Собрать доменный результат из канонического снимка для публикации
        /// через адаптер (LoadResult). Runtime-only поля остаются CLR-дефолтами.
        /// </summary>
        public static ThermalCalculationResult ToDomainResult(ThermalResultSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new ThermalCalculationResult
            {
                PowerUp = snapshot.PowerUp,
                PowerDown = snapshot.PowerDown,
                PowerTotal = snapshot.PowerTotal,
                SupplyTemperature = snapshot.SupplyTemperature,
                ReturnTemperature = snapshot.ReturnTemperature,
                MeanTemperature = snapshot.MeanTemperature,
                DeltaT = snapshot.DeltaT,
                IsValid = snapshot.IsValid
            };
        }
    }
}
