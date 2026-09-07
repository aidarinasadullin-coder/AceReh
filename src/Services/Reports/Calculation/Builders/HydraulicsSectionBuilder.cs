using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела гидравлического расчёта.
    /// </summary>
    public sealed class HydraulicsSectionBuilder : IReportSectionBuilder<HydraulicsSection>
    {
        public SectionBuildResult<HydraulicsSection> Build(ProjectData project, CalculationReportMode mode, ThermalReportDetail? thermalDetail = null)
        {
            var hydraulics = project.HydraulicsData ?? new HydraulicsProjectData();
            var collectors = (hydraulics.Collectors ?? new List<CollectorProjectData>())
                .Select(collector => BuildReportCollector(collector, mode))
                .ToList();

            var section = new HydraulicsSection
            {
                ReferenceCircuit = BuildReferenceCircuit(hydraulics, mode),
                ModeComparison = mode == CalculationReportMode.DesignCold
                    ? BuildModeComparison(hydraulics)
                    : new List<ModeComparisonRow>(),
                GlycolType = ReportValueFactory.Create(hydraulics.GlycolType.ToString(), "-", ReportValueSource.UserInput, "ProjectData.HydraulicsData.GlycolType"),
                GlycolConcentration = ReportValueFactory.Create(hydraulics.GlycolConcentration, "%", ReportValueSource.UserInput, "ProjectData.HydraulicsData.GlycolConcentration", decimals: ReportDecimals.For("%")),
                Density = ReportValueFactory.Create(0.0, "г/см³", ReportValueSource.Calculated, "CircuitResultProjectData.Density", decimals: ReportDecimals.For("г/см³"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                SpecificHeat = ReportValueFactory.Create(0.0, "кДж/(кг·К)", ReportValueSource.Calculated, "GlycolProperties.SpecificHeat", decimals: ReportDecimals.For("кДж/(кг·К)"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                KinematicViscosity = ReportValueFactory.Create(0.0, "мм²/с", ReportValueSource.Calculated, "CircuitResultProjectData.KinematicViscosity", decimals: ReportDecimals.For("мм²/с"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Collectors = collectors
            };

            return new SectionBuildResult<HydraulicsSection>
            {
                Section = section,
                ParameterMetadata = HydraulicsReportMetadataBuilder.BuildMetadata(section),
                Formulas = HydraulicsReportMetadataBuilder.BuildFormulas()
            };
        }

        /// <summary>
        /// Сравнение «рабочий vs холодный пуск» (В3): по каждому коллектору —
        /// вязкость и параметры худшего контура каждого режима из сохранённых
        /// результатов. Кратность роста — Derived-отношение для отображения.
        /// </summary>
        private static List<ModeComparisonRow> BuildModeComparison(HydraulicsProjectData hydraulics)
        {
            var rows = new List<ModeComparisonRow>();
            foreach (var collector in hydraulics.Collectors ?? new List<CollectorProjectData>())
            {
                var summary = collector.Summary;
                if (summary is null)
                {
                    continue;
                }

                var working = PickWorst(collector, CalculationReportMode.Operating);
                var cold = PickWorst(collector, CalculationReportMode.DesignCold);

                rows.Add(new ModeComparisonRow
                {
                    CollectorNumber = collector.CollectorNumber,
                    CollectorType = collector.CollectorType ?? string.Empty,
                    WorkingViscosity = working?.KinematicViscosity ?? 0.0,
                    ColdViscosity = cold?.KinematicViscosity ?? 0.0,
                    WorkingReynolds = working?.ReynoldsNumber ?? 0.0,
                    ColdReynolds = cold?.ReynoldsNumber ?? 0.0,
                    WorkingFriction = working?.FrictionFactor ?? 0.0,
                    ColdFriction = cold?.FrictionFactor ?? 0.0,
                    WorkingPressureLossPa = summary.PressureLoss_Operating_Pa,
                    ColdPressureLossPa = summary.PressureLoss_Cold_Pa,
                    GrowthRatio = summary.PressureLoss_Operating_Pa > 0.0
                        ? summary.PressureLoss_Cold_Pa / summary.PressureLoss_Operating_Pa
                        : 0.0
                });
            }

            return rows;
        }

        /// <summary>Худший контур коллектора в режиме (max DpGesamt; при ничьей — минимальный номер).</summary>
        private static CircuitResultProjectData? PickWorst(CollectorProjectData collector, CalculationReportMode mode)
        {
            CircuitProjectData? worstCircuit = null;
            CircuitResultProjectData? worstResult = null;
            double worstDp = -1.0;
            foreach (var circuit in collector.Circuits ?? new List<CircuitProjectData>())
            {
                var result = mode == CalculationReportMode.Operating ? circuit.OperatingResult : circuit.DesignResult;
                if (result is null)
                {
                    continue;
                }

                if (result.DpGesamt > worstDp
                    || (result.DpGesamt == worstDp
                        && worstCircuit is not null
                        && circuit.CircuitNumber < worstCircuit.CircuitNumber))
                {
                    worstDp = result.DpGesamt;
                    worstCircuit = circuit;
                    worstResult = result;
                }
            }

            return worstResult;
        }

        /// <summary>
        /// Референсный контур (В4): контур с максимальными потерями худшего
        /// коллектора выбранного режима; при ничьей — минимальный номер контура
        /// (совместимо с доменным <c>IsReferenceCircuit</c>). Все величины шагов —
        /// сохранённые результаты контура; новых вычислений нет (AC-5). Если
        /// результатов режима нет — null (missing-data, T2-13).
        /// </summary>
        private static ReferenceCircuitSection? BuildReferenceCircuit(HydraulicsProjectData hydraulics, CalculationReportMode mode)
        {
            CollectorProjectData? worstCollector = null;
            double worstPressure = -1.0;

            foreach (var collector in hydraulics.Collectors ?? new List<CollectorProjectData>())
            {
                var summary = collector.Summary;
                if (summary is null)
                {
                    continue;
                }

                var pressure = mode == CalculationReportMode.Operating
                    ? summary.PressureLoss_Operating_Pa
                    : summary.PressureLoss_Cold_Pa;
                if (pressure > worstPressure
                    || (pressure == worstPressure
                        && worstCollector is not null
                        && collector.CollectorNumber < worstCollector.CollectorNumber))
                {
                    worstPressure = pressure;
                    worstCollector = collector;
                }
            }

            if (worstCollector is null)
            {
                return null;
            }

            CircuitProjectData? worstCircuit = null;
            CircuitResultProjectData? worstResult = null;
            double worstDp = -1.0;

            foreach (var circuit in worstCollector.Circuits ?? new List<CircuitProjectData>())
            {
                var result = mode == CalculationReportMode.Operating ? circuit.OperatingResult : circuit.DesignResult;
                if (result is null)
                {
                    continue;
                }

                if (result.DpGesamt > worstDp
                    || (result.DpGesamt == worstDp
                        && worstCircuit is not null
                        && circuit.CircuitNumber < worstCircuit.CircuitNumber))
                {
                    worstDp = result.DpGesamt;
                    worstCircuit = circuit;
                    worstResult = result;
                }
            }

            if (worstCircuit is null || worstResult is null)
            {
                return null;
            }

            var totalLength = worstCircuit.CircuitLength + worstCircuit.SupplyLength;
            var isHkv = (worstCollector.CollectorType ?? string.Empty).Contains("HKV", StringComparison.OrdinalIgnoreCase);
            var resultValues = worstResult;

            ReportValue<double> V(string key, double value, string unit, int? decimals = null) =>
                ReportValueFactory.Create(value, unit, ReportValueSource.Calculated, key, decimals: decimals ?? ReportDecimals.For(unit));

            var steps = new List<CalculationStep>
            {
                new CalculationStep
                {
                    Key = "hyd.ref.power",
                    Title = "Шаг 1. Мощность контура Q_HK",
                    FormulaText = "Q_HK = [L_HK/(100/VAHK) + L_Zul/(100/VAZul)·(qZul/100)]·q_total",
                    SubstitutionText = $"Q_HK (L_HK = {ReportNumber.Format(worstCircuit.CircuitLength, 0)} м; VAHK = {ReportNumber.Format(worstCircuit.PipeSpacingCm, 0)} см; L_Zul = {ReportNumber.Format(worstCircuit.SupplyLength, 1)} м; VAZul = {ReportNumber.Format(worstCircuit.SupplySpacingCm, 0)} см; qZul = {ReportNumber.Format(worstCircuit.SupplyHeatPercent, 0)} %) = {ReportNumber.Format(resultValues.Power, 0)} Вт",
                    Result = V("Q_HK", resultValues.Power, "Вт"),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("L_HK", worstCircuit.CircuitLength, "м"),
                        V("VAHK", worstCircuit.PipeSpacingCm, "см"),
                        V("L_Zul", worstCircuit.SupplyLength, "м")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.flow",
                    Title = "Шаг 2. Объёмный расход V̇",
                    FormulaText = "V̇ = Q_HK·3,6/(ρ·c_p·ΔT)",
                    SubstitutionText = $"V̇ (Q_HK = {ReportNumber.Format(resultValues.Power, 0)} Вт; ρ = {ReportNumber.Format(resultValues.Density, 3)} г/см³) = {ReportNumber.Format(resultValues.FlowRate, 1)} л/ч",
                    Result = V("V_dot", resultValues.FlowRate, "л/ч"),
                    Note = "c_p и ΔT — из теплового раздела; свойства теплоносителя — интерполяция glycol-базы при рабочей температуре.",
                    Inputs = new List<ReportValue<double>>
                    {
                        V("Q_HK", resultValues.Power, "Вт"),
                        V("ρ", resultValues.Density, "г/см³")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.velocity",
                    Title = "Шаг 3. Скорость потока v",
                    FormulaText = "v = V̇·4000/(3600·π·d_вн²)",
                    SubstitutionText = $"v (V̇ = {ReportNumber.Format(resultValues.FlowRate, 1)} л/ч) = {ReportNumber.Format(resultValues.Velocity, 3)} м/с",
                    Result = V("v", resultValues.Velocity, "м/с"),
                    Note = "d_вн — по трубе проекта; геометрия в контуре не сохраняется, поэтому подстановка приведена сокращённо.",
                    Inputs = new List<ReportValue<double>> { V("V_dot", resultValues.FlowRate, "л/ч") }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.re",
                    Title = "Шаг 4. Число Рейнольдса Re",
                    FormulaText = "Re = 1000·v·d_вн/ν",
                    SubstitutionText = $"Re (v = {ReportNumber.Format(resultValues.Velocity, 3)} м/с; ν = {ReportNumber.Format(resultValues.KinematicViscosity, 3)} мм²/с) = {ReportNumber.Format(resultValues.ReynoldsNumber, 0)}",
                    Result = V("Re", resultValues.ReynoldsNumber, "-", decimals: 0),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("v", resultValues.Velocity, "м/с"),
                        V("ν", resultValues.KinematicViscosity, "мм²/с")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.lambda",
                    Title = "Шаг 5. Коэффициент гидравлического трения λ",
                    FormulaText = "Re < 2300: λ = 64/Re; Re > 4000: Колбрук–Уайт (итерации, старт по Блазиусу); между — линейная интерполяция",
                    SubstitutionText = $"λ (Re = {ReportNumber.Format(resultValues.ReynoldsNumber, 0)}) = {ReportNumber.Format(resultValues.FrictionFactor, 4)}",
                    Result = V("lambda", resultValues.FrictionFactor, "-"),
                    Note = $"Режим течения контура: {worstResult.FlowRegime ?? worstResult.FlowRegimeString ?? "не сохранён"}. Шероховатость PE-Xa 0,007 мм.",
                    Inputs = new List<ReportValue<double>> { V("Re", resultValues.ReynoldsNumber, "-") }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.r",
                    Title = "Шаг 6. Удельные потери давления R",
                    FormulaText = "R = 10000·v²·ρ·λ/(2·d_вн)·100 (ρ в г/см³, d в мм)",
                    SubstitutionText = $"R (v = {ReportNumber.Format(resultValues.Velocity, 3)} м/с; λ = {ReportNumber.Format(resultValues.FrictionFactor, 4)}) = {ReportNumber.Format(resultValues.PressureLossPerMeter, 1)} Па/м",
                    Result = V("R", resultValues.PressureLossPerMeter, "Па/м"),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("v", resultValues.Velocity, "м/с"),
                        V("λ", resultValues.FrictionFactor, "-")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.dprohr",
                    Title = "Шаг 7. Потери в трубе DpRohr",
                    FormulaText = "DpRohr = (L_HK + L_Zul)·R",
                    SubstitutionText = $"DpRohr = ({ReportNumber.Format(worstCircuit.CircuitLength, 0)} + {ReportNumber.Format(worstCircuit.SupplyLength, 1)})·{ReportNumber.Format(resultValues.PressureLossPerMeter, 1)} = {ReportNumber.Format(resultValues.DpRohr, 0)} Па",
                    Result = V("DpRohr", resultValues.DpRohr, "Па"),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("L_total", totalLength, "м"),
                        V("R", resultValues.PressureLossPerMeter, "Па/м")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.dpvert",
                    Title = "Шаг 8. Потери в распределителе DpVerteiler",
                    FormulaText = isHkv
                        ? "DpVerteiler = (V_м³ч/Kv_распределителя)²·10⁵·ρ"
                        : "DpVerteiler = 15000·(ρ/2)·v²",
                    SubstitutionText = $"DpVerteiler (V̇ = {ReportNumber.Format(resultValues.FlowRate, 1)} л/ч; v = {ReportNumber.Format(resultValues.Velocity, 3)} м/с) = {ReportNumber.Format(resultValues.DpVerteiler, 0)} Па",
                    Result = V("DpVerteiler", resultValues.DpVerteiler, "Па"),
                    Note = "Формула зависит от типа коллектора: HKV-D — через Kv распределителя, IV — через скорость.",
                    Inputs = new List<ReportValue<double>>
                    {
                        V("V_dot", resultValues.FlowRate, "л/ч"),
                        V("v", resultValues.Velocity, "м/с")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.dpvent",
                    Title = "Шаг 9. Потери в вентиле DpVent",
                    FormulaText = isHkv
                        ? "DpVent = 15000·(ρ/2)·v²"
                        : "DpVent = (V_м³ч/Kv_вентиля)²·10⁵·ρ",
                    SubstitutionText = $"DpVent (номинальный Kv, клапан полностью открыт) = {ReportNumber.Format(resultValues.DpVent, 0)} Па",
                    Result = V("DpVent", resultValues.DpVent, "Па"),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("V_dot", resultValues.FlowRate, "л/ч"),
                        V("v", resultValues.Velocity, "м/с")
                    }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.dpgesamt",
                    Title = "Шаг 10. Суммарные потери DpGesamt",
                    FormulaText = "DpGesamt = DpRohr + DpVerteiler + DpVent",
                    SubstitutionText = $"DpGesamt = {ReportNumber.Format(resultValues.DpRohr, 0)} + {ReportNumber.Format(resultValues.DpVerteiler, 0)} + {ReportNumber.Format(resultValues.DpVent, 0)} = {ReportNumber.Format(resultValues.DpGesamt, 0)} Па",
                    Result = V("DpGesamt", resultValues.DpGesamt, "Па"),
                    Inputs = new List<ReportValue<double>>
                    {
                        V("DpRohr", resultValues.DpRohr, "Па"),
                        V("DpVerteiler", resultValues.DpVerteiler, "Па"),
                        V("DpVent", resultValues.DpVent, "Па")
                    }
                }
            };

            var balancingSteps = new List<CalculationStep>
            {
                new CalculationStep
                {
                    Key = "hyd.ref.throttle",
                    Title = "Балансировка. Избыточное давление для увязки zu_dr.",
                    FormulaText = isHkv
                        ? "zu_dr. = maxDp − (DpRohr + DpVent) — распределитель HKV не дросселируется"
                        : "zu_dr. = maxDp − (DpRohr + DpVerteiler) — вентиль IV не дросселируется",
                    SubstitutionText = $"zu_dr. (DpGesamt контура = {ReportNumber.Format(resultValues.DpGesamt, 0)} Па) = {ReportNumber.Format(resultValues.Throttling, 0)} Па",
                    Result = V("zu_dr", resultValues.Throttling, "Па"),
                    Note = "Референсный контур имеет максимальные потери: zu_dr. = 0, клапан полностью открыт.",
                    Inputs = new List<ReportValue<double>> { V("DpGesamt", resultValues.DpGesamt, "Па") }
                },
                new CalculationStep
                {
                    Key = "hyd.ref.kv",
                    Title = "Балансировка. Требуемый Kv клапана",
                    FormulaText = "Kv = (V̇/1000)/√(zu_dr/10⁵/ρ)",
                    SubstitutionText = "Kv — не сохраняется в проекте: подстановка недоступна (формула приведена для проверки настройки).",
                    Result = V("Kv", 0.0, "м³/ч"),
                    Note = "Величина Kv контура не сохраняется в wire-наборе проекта (DEC-T08/ADR-010) — новых вычислений отчёт не выполняет."
                },
                new CalculationStep
                {
                    Key = "hyd.ref.turns",
                    Title = "Балансировка. Настроечные обороты клапана",
                    FormulaText = isHkv
                        ? "Кубическая характеристика HKV: Kv → обороты по полиному; округление до ¼ оборота"
                        : "Линейная характеристика IV: обороты = a·Kv − b; округление до ¼ оборота",
                    SubstitutionText = $"Обороты контура = {ReportNumber.Format(resultValues.ValveTurns, 2)} об",
                    Result = V("обороты", resultValues.ValveTurns, "об")
                }
            };

            return new ReferenceCircuitSection
            {
                CollectorNumber = worstCollector.CollectorNumber,
                CircuitNumber = worstCircuit.CircuitNumber,
                CollectorType = worstCollector.CollectorType ?? string.Empty,
                TotalLength = ReportValueFactory.Create(totalLength, "м", ReportValueSource.Calculated, "CircuitProjectData.CircuitLength + SupplyLength", decimals: ReportDecimals.For("м"), formula: "L_HK + L_Zul"),
                Steps = steps,
                BalancingSteps = balancingSteps,
                BalancingNote = isHkv
                    ? "База вычитания HKV-D: из максимальных потерь исключаются DpRohr и DpVent (распределитель не дросселируется); максимум оборотов 2½."
                    : "База вычитания IV: из максимальных потерь исключаются DpRohr и DpVerteiler (вентиль не дросселируется); максимум оборотов 8.",
                DpVentNote = "Колонка DpVent показывает потери при полностью открытом клапане (номинальный Kv) и после балансировки не пересчитывается; реальное выравнивание обеспечивает настройка клапана на рассчитанные обороты."
            };
        }

        private static ReportCollector BuildReportCollector(CollectorProjectData collector, CalculationReportMode mode)
        {
            var circuits = (collector.Circuits ?? new List<CircuitProjectData>())
                .Select(circuit => BuildReportCircuit(collector, circuit, mode))
                .ToList();

            var summary = collector.Summary ?? new CollectorSummaryProjectData();

            return new ReportCollector
            {
                Number = collector.CollectorNumber,
                Type = collector.CollectorType ?? string.Empty,
                Circuits = circuits,
                Summary = new ReportCollectorSummary
                {
                    CollectorType = ReportValueFactory.Create(summary.CollectorType ?? string.Empty, "-", ReportValueSource.Calculated, "CollectorSummaryProjectData.CollectorType", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                    CircuitCount = ReportValueFactory.Create((double)summary.CircuitCount, "шт", ReportValueSource.Calculated, "CollectorSummaryProjectData.CircuitCount", decimals: ReportDecimals.For("шт")),
                    TotalPipeLength = ReportValueFactory.Create(summary.TotalPipeLength, "м", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPipeLength", decimals: ReportDecimals.For("м")),
                    TotalPower = ReportValueFactory.Create(summary.TotalPower, "Вт", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPower", decimals: ReportDecimals.For("Вт")),
                    TotalFlowRate = ReportValueFactory.Create(summary.TotalFlowRate, "л/ч", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalFlowRate", decimals: ReportDecimals.For("л/ч")),
                    PressureLoss = ReportValueFactory.Create(
                        mode == CalculationReportMode.Operating ? summary.PressureLoss_Operating_Pa : summary.PressureLoss_Cold_Pa,
                        "Па",
                        ReportValueSource.Calculated,
                        mode == CalculationReportMode.Operating ? "CollectorSummaryProjectData.PressureLoss_Operating_Pa" : "CollectorSummaryProjectData.PressureLoss_Cold_Pa",
                        decimals: ReportDecimals.For("Па"),
                        formula: "max(DpGesamt)"),
                    Kv = ReportValueFactory.Create(summary.Kv, "-", ReportValueSource.Calculated, "CollectorSummaryProjectData.Kv", decimals: 2, formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed)
                }
            };
        }

        private static ReportCircuit BuildReportCircuit(CollectorProjectData collector, CircuitProjectData circuit, CalculationReportMode mode)
        {
            var resultValues = (mode == CalculationReportMode.Operating ? circuit.OperatingResult : circuit.DesignResult)
                ?? new CircuitResultProjectData();

            return new ReportCircuit
            {
                CircuitNumber = circuit.CircuitNumber,
                CircuitLength = ReportValueFactory.Create(circuit.CircuitLength, "м", ReportValueSource.UserInput, "CircuitProjectData.CircuitLength", decimals: ReportDecimals.For("м")),
                CircuitArea = ReportValueFactory.Create(circuit.CircuitLength * circuit.PipeSpacingCm / 100.0, "м²", ReportValueSource.Calculated, "CircuitRow.CircuitArea", decimals: ReportDecimals.For("м²"), formula: "L_HK * VAHK / 100"),
                SupplyLength = ReportValueFactory.Create(circuit.SupplyLength, "м", ReportValueSource.UserInput, "CircuitProjectData.SupplyLength", decimals: ReportDecimals.For("м")),
                TotalLength = ReportValueFactory.Create(circuit.CircuitLength + circuit.SupplyLength, "м", ReportValueSource.Calculated, "CircuitRow.TotalLength", decimals: ReportDecimals.For("м"), formula: "CircuitLength + SupplyLength"),
                PipeSpacing = ReportValueFactory.Create(circuit.PipeSpacingCm, "см", ReportValueSource.UserInput, "CircuitProjectData.PipeSpacingCm", decimals: ReportDecimals.For("см")),
                SupplySpacing = ReportValueFactory.Create(circuit.SupplySpacingCm, "см", ReportValueSource.UserInput, "CircuitProjectData.SupplySpacingCm", decimals: ReportDecimals.For("см")),
                SupplyHeatPercent = ReportValueFactory.Create(circuit.SupplyHeatPercent, "%", ReportValueSource.UserInput, "CircuitProjectData.SupplyHeatPercent", decimals: ReportDecimals.For("%")),
                Power = ReportValueFactory.Create(resultValues.Power, "Вт", ReportValueSource.Calculated, "CircuitResultProjectData.Power", decimals: ReportDecimals.For("Вт"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FlowRate = ReportValueFactory.Create(resultValues.FlowRate, "л/ч", ReportValueSource.Calculated, "CircuitResultProjectData.FlowRate", decimals: ReportDecimals.For("л/ч"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Velocity = ReportValueFactory.Create(resultValues.Velocity, "м/с", ReportValueSource.Calculated, "CircuitResultProjectData.Velocity", decimals: ReportDecimals.For("м/с"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Density = ReportValueFactory.Create(resultValues.Density, "г/см³", ReportValueSource.Calculated, "CircuitResultProjectData.Density", decimals: ReportDecimals.For("г/см³"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                KinematicViscosity = ReportValueFactory.Create(resultValues.KinematicViscosity, "мм²/с", ReportValueSource.Calculated, "CircuitResultProjectData.KinematicViscosity", decimals: ReportDecimals.For("мм²/с"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ReynoldsNumber = ReportValueFactory.Create(resultValues.ReynoldsNumber, "-", ReportValueSource.Calculated, "CircuitResultProjectData.ReynoldsNumber", decimals: 0, formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FrictionFactor = ReportValueFactory.Create(resultValues.FrictionFactor, "-", ReportValueSource.Calculated, "CircuitResultProjectData.FrictionFactor", decimals: 3, formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                PressureLossPerMeter = ReportValueFactory.Create(resultValues.PressureLossPerMeter, "Па/м", ReportValueSource.Calculated, "CircuitResultProjectData.PressureLossPerMeter", decimals: ReportDecimals.For("Па/м"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpRohr = ReportValueFactory.Create(resultValues.DpRohr, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpRohr", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpVerteiler = ReportValueFactory.Create(resultValues.DpVerteiler, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpVerteiler", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpVent = ReportValueFactory.Create(resultValues.DpVent, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpVent", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpGesamt = ReportValueFactory.Create(resultValues.DpGesamt, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpGesamt", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Throttling = ReportValueFactory.Create(circuit.Throttling, "Па", ReportValueSource.Calculated, "CircuitProjectData.Throttling", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ZuDrosseln = ReportValueFactory.Create(resultValues.Throttling, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.Throttling", decimals: ReportDecimals.For("Па"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ValveTurns = ReportValueFactory.Create(resultValues.ValveTurns, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", decimals: ReportDecimals.For("об"), formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FlowRegime = ReportValueFactory.Create(resultValues.FlowRegime ?? resultValues.FlowRegimeString ?? string.Empty, "-", ReportValueSource.Calculated, "CircuitResultProjectData.FlowRegime", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed)
            };
        }
    }
}
