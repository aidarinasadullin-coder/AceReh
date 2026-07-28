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

            if (section.Layers.Count > 0)
            {
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

        public static void RenderThermalSection(StringBuilder sb, ThermalSection section)
        {
            sb.AppendLine("## Теплотехнический расчёт");
            CalculationReportMarkdownRenderHelper.RenderScalarTable(sb, new (string, ReportValue<double>)[]
            {
                ("Коэффициент теплоотдачи", section.Alpha),
                ("Мощность на плавление снега", section.MeltingHeat),
                ("Лучистый тепловой поток (справочно)", section.RadiationHeat),
                ("Конвективный тепловой поток", section.ConvectionHeat),
                ("Мощность вверх", section.PowerUp),
                ("Мощность вниз", section.PowerDown),
                ("Суммарная удельная мощность", section.TotalPowerDensity),
                ("Полное сопротивление вверх RFb", section.RFb),
                ("Полное сопротивление вниз RD", section.RD),
                ("Параметр затухания M", section.ParameterM),
                ("КПД ребра EtaR", section.EfficiencyEtaR),
                ("Избыточная температура", section.ExcessTemperature),
                ("Массовый расход на м²", section.MassFlowRate),
                ("Объёмный расход на м²", section.VolumeFlowRate),
                ("Плотность снега", section.SnowDensity),
                ("Теплоёмкость льда", section.IceHeatCapacity),
                ("Теплота плавления льда", section.IceMeltingHeat),
                ("Теплоёмкость воды", section.WaterHeatCapacity),
            });
            sb.AppendLine();
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
