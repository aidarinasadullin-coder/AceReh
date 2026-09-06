using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Рендеринг разделов детального расчётного отчёта в Markdown.
    /// </summary>
    public static class CalculationReportMarkdownSectionRenderer
    {
        public static void RenderTitle(StringBuilder sb, CalculationReportData data)
        {
            var modeLabel = data.Mode == CalculationReportMode.Operating
                ? "Рабочий режим"
                : "Расчётный/холодный режим";

            sb.AppendLine("# Детальный расчётный отчёт");
            sb.AppendLine("**Тип документа:** детальный расчётный отчёт");
            sb.AppendLine($"**Режим отчёта:** {modeLabel}");
            sb.AppendLine($"**Номер проекта:** {CalculationReportMarkdownRenderHelper.EscapeCell(data.ProjectSection.ProjectNumber)}");
            sb.AppendLine($"**Объект:** {CalculationReportMarkdownRenderHelper.EscapeCell(data.ProjectSection.ProjectObject)}");
            sb.AppendLine($"**Дата формирования:** {data.ReportDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            sb.AppendLine("**Версия формата отчёта:** v1");
            sb.AppendLine();
        }

        public static void RenderMethodology(StringBuilder sb, CalculationReportData data)
        {
            sb.AppendLine("## Методика");
            sb.AppendLine("> Расчётные данные приведены по внутренней методике REHAU, реализованной в приложении SnowMeltingCalculator. " +
                "Отчёт не заявляет соответствие ГОСТ/СП, если конкретный источник данных явно не указывает такой источник.");
            sb.AppendLine();
        }

        public static void RenderSummary(StringBuilder sb, CalculationReportData data)
        {
            var modeLabel = data.Mode == CalculationReportMode.Operating
                ? "Рабочий режим"
                : "Расчётный/холодный режим";

            sb.AppendLine("## Краткая сводка");
            sb.AppendLine("| Параметр | Значение | Единица | Источник |");
            sb.AppendLine("| --- | --- | --- | --- |");
            sb.AppendLine($"| Суммарная тепловая мощность | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.TotalThermalPower)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.TotalThermalPower.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.TotalThermalPower)} |");
            sb.AppendLine($"| Объём системы | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.SystemVolume)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.SystemVolume.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.SystemVolume)} |");
            sb.AppendLine($"| Расход насоса | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.PumpFlowRate)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.PumpFlowRate.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.PumpFlowRate)} |");
            sb.AppendLine($"| Напор насоса | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.PumpHead)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.PumpHead.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.PumpHead)} |");
            sb.AppendLine($"| Объём расширительного бака | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.ExpansionTankVolume)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.ExpansionTankVolume.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.ExpansionTankVolume)} |");
            sb.AppendLine($"| Общая длина трубы | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.TotalPipeLength)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.TotalPipeLength.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.TotalPipeLength)} |");
            sb.AppendLine($"| Количество РЗС / коллекторов | {CalculationReportMarkdownRenderHelper.Value(data.EquipmentSection.RzsCount)} | {CalculationReportMarkdownRenderHelper.EscapeCell(data.EquipmentSection.RzsCount.Unit)} | {CalculationReportMarkdownRenderHelper.Source(data.EquipmentSection.RzsCount)} |");
            sb.AppendLine($"| Выбранный режим отчёта | {modeLabel} | - | Derived |");
            sb.AppendLine();
        }

        public static void RenderProjectSection(StringBuilder sb, ProjectSection section)
        {
            sb.AppendLine("## Исходные данные проекта");
            sb.AppendLine("| Параметр | Значение | Источник |");
            sb.AppendLine("| --- | --- | --- |");
            sb.AppendLine($"| Номер проекта | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ProjectNumber)} | Project |");
            sb.AppendLine($"| Объект | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ProjectObject)} | Project |");
            sb.AppendLine();
        }

        public static void RenderClimateSection(StringBuilder sb, ClimateSection section)
        {
            sb.AppendLine("## Климатические данные");
            sb.AppendLine("| Параметр | Обозначение | Значение | Единица | Источник |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            sb.AppendLine($"| Город | {CalculationReportMarkdownRenderHelper.EscapeCell(section.City.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.City)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.City.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.City)} |");
            sb.AppendLine($"| Регион | {CalculationReportMarkdownRenderHelper.EscapeCell(section.Region.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.Region)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.Region.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.Region)} |");
            sb.AppendLine($"| Расчётная температура наружного воздуха | {CalculationReportMarkdownRenderHelper.EscapeCell(section.AirTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.AirTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.AirTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.AirTemperature)} |");
            sb.AppendLine($"| Скорость ветра | {CalculationReportMarkdownRenderHelper.EscapeCell(section.WindSpeed.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.WindSpeed)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.WindSpeed.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.WindSpeed)} |");
            sb.AppendLine($"| Относительная влажность | {CalculationReportMarkdownRenderHelper.EscapeCell(section.Humidity.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.Humidity)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.Humidity.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.Humidity)} |");
            sb.AppendLine($"| Интенсивность снегопада | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SnowfallIntensity.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.SnowfallIntensity)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SnowfallIntensity.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.SnowfallIntensity)} |");
            sb.AppendLine($"| Климатическая зона | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ClimateZone.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.ClimateZone)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ClimateZone.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.ClimateZone)} |");
            sb.AppendLine($"| Количество дней холодного периода | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ColdPeriodDays.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.ColdPeriodDays)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ColdPeriodDays.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.ColdPeriodDays)} |");
            sb.AppendLine($"| Температура поверхности | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SurfaceTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.SurfaceTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SurfaceTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.SurfaceTemperature)} |");
            sb.AppendLine($"| Температура грунта | {CalculationReportMarkdownRenderHelper.EscapeCell(section.GroundTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.GroundTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.GroundTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.GroundTemperature)} |");
            sb.AppendLine($"| Температура подачи | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SupplyTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.SupplyTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.SupplyTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.SupplyTemperature)} |");
            sb.AppendLine($"| Температура обратки | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ReturnTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.ReturnTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.ReturnTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.ReturnTemperature)} |");
            sb.AppendLine($"| Средняя температура теплоносителя | {CalculationReportMarkdownRenderHelper.EscapeCell(section.MeanTemperature.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.MeanTemperature)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.MeanTemperature.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.MeanTemperature)} |");
            sb.AppendLine($"| Температурный перепад | {CalculationReportMarkdownRenderHelper.EscapeCell(section.DeltaT.SourceDetail)} | {CalculationReportMarkdownRenderHelper.Value(section.DeltaT)} | {CalculationReportMarkdownRenderHelper.EscapeCell(section.DeltaT.Unit)} | {CalculationReportMarkdownRenderHelper.Source(section.DeltaT)} |");
            sb.AppendLine();
        }

        public static void RenderConstructionSection(StringBuilder sb, ConstructionSection section)
        {
            sb.AppendLine("## Конструкция");
            CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new[]
            {
                ("Уровень грунтовых вод", section.GroundwaterLevel),
                ("Сопротивление вверх R1", section.R1),
                ("Сопротивление вниз R2", section.R2),
                ("Эквивалентная теплопроводность LambdaE", section.LambdaE),
            });

            if (!string.IsNullOrWhiteSpace(section.LambdaRuleNote))
            {
                sb.AppendLine();
                sb.AppendLine($"> {section.LambdaRuleNote}");
            }

            RenderSteps(sb, section.Steps);

            if (section.Layers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Слои конструкции");
                sb.AppendLine("| Позиция | Материал | Толщина | Ед. | Теплопроводность | Ед. | Термическое сопротивление | Ед. |");
                sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (var layer in section.Layers)
                {
                    sb.AppendLine($"| {CalculationReportMarkdownRenderHelper.EscapeCell(layer.Position)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(layer.MaterialName)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(layer.Thickness)} | {CalculationReportMarkdownRenderHelper.EscapeCell(layer.Thickness.Unit)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(layer.Lambda)} | {CalculationReportMarkdownRenderHelper.EscapeCell(layer.Lambda.Unit)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(layer.ThermalResistance)} | {CalculationReportMarkdownRenderHelper.EscapeCell(layer.ThermalResistance.Unit)} |");
                }
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Блок шага расчёта: формула → подстановка → результат → примечание (ADR-010).
        /// </summary>
        private static void RenderStep(StringBuilder sb, CalculationStep step)
        {
            sb.AppendLine($"**{CalculationReportMarkdownRenderHelper.EscapeCell(step.Title)}**");
            sb.AppendLine($"- Формула: `{CalculationReportMarkdownRenderHelper.EscapeCell(step.FormulaText)}`");
            if (!string.IsNullOrWhiteSpace(step.SubstitutionText))
            {
                sb.AppendLine($"- Подстановка: {CalculationReportMarkdownRenderHelper.EscapeCell(step.SubstitutionText)}");
            }

            sb.AppendLine($"- Результат: **{CalculationReportMarkdownRenderHelper.Value(step.Result)} {CalculationReportMarkdownRenderHelper.EscapeCell(step.Result.Unit)}**");
            if (!string.IsNullOrWhiteSpace(step.Note))
            {
                sb.AppendLine($"- Примечание: {CalculationReportMarkdownRenderHelper.EscapeCell(step.Note)}");
            }

            sb.AppendLine();
        }

        /// <summary>Список шагов; пустой список не рендерится.</summary>
        private static void RenderSteps(StringBuilder sb, IReadOnlyList<CalculationStep> steps)
        {
            foreach (var step in steps)
            {
                RenderStep(sb, step);
            }
        }

        public static void RenderThermalSection(StringBuilder sb, ThermalSection section, CalculationReportMode mode)
        {
            sb.AppendLine("## Теплотехнический расчёт");
            sb.AppendLine($"*Источник детальных величин: {CalculationReportMarkdownRenderHelper.EscapeCell(section.DetailSourceDescription)}.*");

            if (!section.IsDetailAvailable)
            {
                sb.AppendLine();
                sb.AppendLine($"> {CalculationReportMarkdownRendererConstants.MissingValue}: детальные тепловые величины недоступны. " +
                    "Выполните тепловой расчёт и повторите экспорт. Ниже сохранённые итоги (wire-набор проекта).");
            }

            if (!string.IsNullOrWhiteSpace(section.DetailNote))
            {
                sb.AppendLine();
                sb.AppendLine($"> Примечание: {CalculationReportMarkdownRenderHelper.EscapeCell(section.DetailNote)}");
            }

            // Величины, приходящие из детального набора (ADR-010): при их
            // отсутствии — маркер «нет данных» (В2).
            string Detail(ReportValue<double> value) =>
                section.IsDetailAvailable
                    ? CalculationReportMarkdownRenderHelper.Value(value)
                    : CalculationReportMarkdownRendererConstants.MissingValue;

            if (mode == CalculationReportMode.DesignCold)
            {
                // В3: холодный отчёт — краткая тепловая справка (контекст вязкости
                // и ламинарного режима), полный ход расчёта — в рабочем отчёте.
                sb.AppendLine();
                sb.AppendLine("### Краткая тепловая справка");
                CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new[]
                {
                    ("Полезная мощность вверх", section.PowerUp),
                    ("Мощность вниз", section.PowerDown),
                    ("Суммарная удельная мощность", section.TotalPowerDensity),
                });
                sb.AppendLine();
                sb.AppendLine("> Средняя температура теплоносителя и перепад ΔT приведены в разделе «Климатические данные» выше: " +
                    "они определяют свойства теплоносителя и ламинарный режим холодного пуска. " +
                    "Полный пошаговый тепловой расчёт — в отчёте рабочего режима.");
            }
            else
            {
                sb.AppendLine();
                CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new[]
                {
                    ("Коэффициент теплоотдачи", section.Alpha),
                    ("Мощность на плавление снега", section.MeltingHeat),
                    ("Лучистый тепловой поток (справочно)", section.RadiationHeat),
                    ("Конвективный тепловой поток", section.ConvectionHeat),
                    ("Полное сопротивление вверх RFb", section.RFb),
                    ("Полное сопротивление вниз RD", section.RD),
                    ("Параметр затухания M", section.ParameterM),
                    ("КПД ребра EtaR", section.EfficiencyEtaR),
                    ("Избыточная температура", section.ExcessTemperature),
                    ("Массовый расход на м²", section.MassFlowRate),
                    ("Объёмный расход на м²", section.VolumeFlowRate),
                });

                // Маркеры «нет данных» для detail-величин поверх таблицы (В2).
                if (!section.IsDetailAvailable)
                {
                    sb.AppendLine();
                    sb.AppendLine($"*α, Q_таяния, Q_конв, RFb, RD, m, ηR, JHmü, расходы — {CalculationReportMarkdownRendererConstants.MissingValue}.*");
                }

                sb.AppendLine();
                sb.AppendLine("### Пошаговый расчёт");
                sb.AppendLine();
                RenderSteps(sb, section.Steps);

                if (section.Constants.Count > 0)
                {
                    sb.AppendLine("### Константы расчёта (из кода программы)");
                    sb.AppendLine("| Константа | Обозначение | Значение | Единица | Источник |");
                    sb.AppendLine("| --- | --- | --- | --- | --- |");
                    foreach (var constant in section.Constants)
                    {
                        sb.AppendLine($"| {CalculationReportMarkdownRenderHelper.EscapeCell(constant.Name)} | {CalculationReportMarkdownRenderHelper.EscapeCell(constant.Symbol)} | " +
                            $"{ReportNumber.Format(constant.Value, constant.Decimals)} | {CalculationReportMarkdownRenderHelper.EscapeCell(constant.Unit)} | " +
                            $"{CalculationReportMarkdownRenderHelper.EscapeCell(constant.SourceDetail)} |");
                    }

                    sb.AppendLine();
                }
            }

            if (section.DetailValidationErrors.Count > 0)
            {
                // В7: примечания валидации результата расчёта/пересчёта.
                sb.AppendLine("### Примечания валидации теплового расчёта");
                foreach (var error in section.DetailValidationErrors)
                {
                    sb.AppendLine($"- {CalculationReportMarkdownRenderHelper.EscapeCell(error)}");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Перегрузка совместимости: прежние вызовы (тесты v1) — полный режим Operating.
        /// </summary>
        public static void RenderThermalSection(StringBuilder sb, ThermalSection section)
        {
            RenderThermalSection(sb, section, CalculationReportMode.Operating);
        }

        public static void RenderHydraulicsSection(StringBuilder sb, HydraulicsSection section)
        {
            sb.AppendLine("## Гидравлический расчёт");
            CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new (string, ReportValue<string>)[]
            {
                ("Тип гликоля", section.GlycolType),
            });
            CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new (string, ReportValue<double>)[]
            {
                ("Концентрация гликоля", section.GlycolConcentration),
                ("Плотность теплоносителя", section.Density),
                ("Удельная теплоёмкость", section.SpecificHeat),
                ("Кинематическая вязкость", section.KinematicViscosity),
            });

            RenderReferenceCircuit(sb, section.ReferenceCircuit);
            RenderModeComparison(sb, section.ModeComparison);

            foreach (var collector in section.Collectors.OrderBy(c => c.Number))
            {
                sb.AppendLine($"### Коллектор {collector.Number}");
                sb.AppendLine($"- Тип: {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Type)}");
                sb.AppendLine("#### Сводка по коллектору");
                sb.AppendLine("| Параметр | Значение | Единица | Источник |");
                sb.AppendLine("| --- | --- | --- | --- |");
                sb.AppendLine($"| Тип коллектора | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.CollectorType)} | - | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.CollectorType)} |");
                sb.AppendLine($"| Количество контуров | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.CircuitCount)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.CircuitCount.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.CircuitCount)} |");
                sb.AppendLine($"| Общая длина труб | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.TotalPipeLength)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.TotalPipeLength.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.TotalPipeLength)} |");
                sb.AppendLine($"| Общая мощность | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.TotalPower)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.TotalPower.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.TotalPower)} |");
                sb.AppendLine($"| Общий расход | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.TotalFlowRate)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.TotalFlowRate.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.TotalFlowRate)} |");
                sb.AppendLine($"| Потери давления | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.PressureLoss)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.PressureLoss.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.PressureLoss)} |");
                sb.AppendLine($"| Kv | {CalculationReportMarkdownRenderHelper.Value(collector.Summary.Kv)} | {CalculationReportMarkdownRenderHelper.EscapeCell(collector.Summary.Kv.Unit)} | {CalculationReportMarkdownRenderHelper.Source(collector.Summary.Kv)} |");

                sb.AppendLine("#### Контуры");
                sb.AppendLine("| Контур | Длина | Площадь | Мощность | Расход | Скорость | Число Рейнольдса | Коэффициент трения | Режим течения | Удельные потери | Потери в трубе | Потери в распределителе | Потери в вентиле | Суммарные потери | Дросселирование | Обороты клапана |");
                sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

                foreach (var circuit in collector.Circuits.OrderBy(c => c.CircuitNumber))
                {
                    sb.AppendLine($"| {circuit.CircuitNumber} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.CircuitLength)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.CircuitArea)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.Power)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.FlowRate)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.Velocity)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.ReynoldsNumber)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.FrictionFactor)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.FlowRegime)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.PressureLossPerMeter)} | " +
                        $"{CalculationReportMarkdownRenderHelper.ValueWithUnit(circuit.DpRohr)} | " +
                        $"{CalculationReportMarkdownRenderHelper.ValueWithUnit(circuit.DpVerteiler)} | " +
                        $"{CalculationReportMarkdownRenderHelper.ValueWithUnit(circuit.DpVent)} | " +
                        $"{CalculationReportMarkdownRenderHelper.ValueWithUnit(circuit.DpGesamt)} | " +
                        $"{CalculationReportMarkdownRenderHelper.ValueWithUnit(circuit.Throttling)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(circuit.ValveTurns)} |");
                }
            }

            sb.AppendLine();
        }

        /// <summary>Референсный контур: цепочка шагов + пример балансировки (В4).</summary>
        private static void RenderReferenceCircuit(StringBuilder sb, ReferenceCircuitSection? reference)
        {
            if (reference is null)
            {
                return;
            }

            sb.AppendLine($"### Референсный контур (коллектор {reference.CollectorNumber}, контур {reference.CircuitNumber}, {CalculationReportMarkdownRenderHelper.EscapeCell(reference.CollectorType)})");
            sb.AppendLine($"*Контур с максимальными потерями; полная длина {CalculationReportMarkdownRenderHelper.Value(reference.TotalLength)} {CalculationReportMarkdownRenderHelper.EscapeCell(reference.TotalLength.Unit)}.*");
            sb.AppendLine();
            RenderSteps(sb, reference.Steps);

            sb.AppendLine("#### Пример балансировки");
            if (!string.IsNullOrWhiteSpace(reference.BalancingNote))
            {
                sb.AppendLine($"> {CalculationReportMarkdownRenderHelper.EscapeCell(reference.BalancingNote)}");
            }

            sb.AppendLine();
            RenderSteps(sb, reference.BalancingSteps);

            if (!string.IsNullOrWhiteSpace(reference.DpVentNote))
            {
                sb.AppendLine($"> {CalculationReportMarkdownRenderHelper.EscapeCell(reference.DpVentNote)}");
                sb.AppendLine();
            }
        }

        /// <summary>Сравнение «рабочий vs холодный пуск» (В3, режим DesignCold).</summary>
        private static void RenderModeComparison(StringBuilder sb, IReadOnlyList<ModeComparisonRow> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            sb.AppendLine("### Сравнение режимов: рабочий vs холодный пуск");
            sb.AppendLine();
            sb.AppendLine("| Коллектор | Тип | ν рабочий, мм²/с | ν пуск, мм²/с | Re рабочий | Re пуск | λ рабочий | λ пуск | Δp рабочий, Па | Δp пуск, Па | Кратность |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var row in rows)
            {
                sb.AppendLine($"| {row.CollectorNumber} | {CalculationReportMarkdownRenderHelper.EscapeCell(row.CollectorType)} | " +
                    $"{ReportNumber.Format(row.WorkingViscosity)} | {ReportNumber.Format(row.ColdViscosity)} | " +
                    $"{ReportNumber.Format(row.WorkingReynolds, "N0")} | {ReportNumber.Format(row.ColdReynolds, "N0")} | " +
                    $"{ReportNumber.Format(row.WorkingFriction)} | {ReportNumber.Format(row.ColdFriction)} | " +
                    $"{ReportNumber.Format(row.WorkingPressureLossPa, "N0")} | {ReportNumber.Format(row.ColdPressureLossPa, "N0")} | " +
                    $"×{ReportNumber.Format(row.GrowthRatio)} |");
            }

            sb.AppendLine();
            sb.AppendLine("> Холодный пуск: вязкость теплоносителя при расчётной температуре многократно растёт, " +
                "Re падает до ламинарного режима, потери давления увеличиваются в разы — подбор насоса выполняется по наихудшему режиму.");
            sb.AppendLine();
        }

        public static void RenderEquipmentSection(StringBuilder sb, EquipmentSection section)
        {
            sb.AppendLine("## Оборудование и KPI");
            CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new[]
            {
                ("Суммарная тепловая мощность", section.TotalThermalPower),
                ("Объём системы", section.SystemVolume),
                ("Расход насоса", section.PumpFlowRate),
                ("Напор насоса", section.PumpHead),
                ("Объём расширительного бака", section.ExpansionTankVolume),
                ("Общая длина труб", section.TotalPipeLength),
                ("Количество РЗС / коллекторов", section.RzsCount),
            });

            if (section.CollectorSpecifications.Count > 0)
            {
                sb.AppendLine("### Спецификации коллекторов");
                sb.AppendLine("| Коллектор | Тип | Контуров | Мощность | Расход | Потери давления | Kv |");
                sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
                foreach (var spec in section.CollectorSpecifications.OrderBy(s => s.Number))
                {
                    sb.AppendLine($"| {spec.Number} | {CalculationReportMarkdownRenderHelper.EscapeCell(spec.Type)} | {spec.CircuitCount} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(spec.TotalPower)} {CalculationReportMarkdownRenderHelper.EscapeCell(spec.TotalPower.Unit)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(spec.TotalFlowRate)} {CalculationReportMarkdownRenderHelper.EscapeCell(spec.TotalFlowRate.Unit)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(spec.PressureLoss)} {CalculationReportMarkdownRenderHelper.EscapeCell(spec.PressureLoss.Unit)} | " +
                        $"{CalculationReportMarkdownRenderHelper.Value(spec.Kv)} {CalculationReportMarkdownRenderHelper.EscapeCell(spec.Kv.Unit)} |");
                }
            }

            sb.AppendLine();
        }
    }
}
