using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// PDF-рендерер детального расчётного отчёта (мини-фаза PDF-PZ, ADR-010):
    /// та же <see cref="CalculationReportData"/>, что у Markdown-версии,
    /// разделы 1:1, A4-портрет, нейтральный инженерный стиль (спека §7.1).
    /// </summary>
    /// <remarks>
    /// Рендер не вычисляет и не выбирает значения (AC-5) — только форматирует
    /// готовую модель. Решение владельца 2026-09-07 (спека §7.2): в PDF не
    /// выводятся ссылки на кодовую базу — <c>SourceDetail</c>, источник
    /// констант-таблицы и <c>SourcePath</c> приложения формул подавляются,
    /// трассировка — инженерными категориями источника
    /// (<see cref="ReportValueSource"/> → «введено пользователем», «база
    /// программы», «рассчитано программой» и т.п.). Категории подстановок
    /// шагов берутся из <see cref="CalculationStep.Result"/> и табличных
    /// <see cref="ReportValue{T}"/> — метаданные <c>step.Inputs</c> не
    /// используются (в билдере они не отражают природу величины).
    /// Числа — каноническая культура приложения (В6, <see cref="ReportNumber"/>).
    /// </remarks>
    public sealed class CalculationReportPdfRenderer : ICalculationReportPdfRenderer
    {
        /// <summary>
        /// Бренд-шрифт Inter (спека §7.2), при недоступности резолвера —
        /// резервный Arial (§3.2 гайдлайна). Инициализация резолвера — до
        /// первого шрифтового рендера (урок №9 Ф8).
        /// </summary>
        private static readonly string FontName = InitFontName();

        private const string MissingValue = CalculationReportMarkdownRendererConstants.MissingValue;

        /// <summary>
        /// Статус «заполнитель» билдеров: величина не хранится в проекте
        /// и не вычислена. В статусах формул приложения остаётся видимым
        /// (санкционированный маркер); нулевые заполнители в значениях
        /// показывает как «нет данных» общий гейт <c>!ZeroIsValid</c>
        /// (В2/В14, <see cref="CalculationReportMarkdownRenderHelper.Value"/>).
        /// </summary>
        private const string UnconfirmedStatusMarker = "требуется привязка к существующей формуле";

        private const string TextColorHex = "#212121";
        private const string SecondaryTextColorHex = "#757575";
        private const string BorderColorHex = "#BDBDBD";
        private const string HeaderBackgroundHex = "#F5F5F5";

        /// <summary>Активный Красный — только линия шапки (спека §7.2).</summary>
        private const string BrandRedHex = "#E50040";
        private const string SemanticErrorHex = "#D32F2F";
        private const string SemanticWarningHex = "#FF9800";

        /// <summary>
        /// Маркеры ссылок на кодовую базу в свободных текстах модели
        /// (решение владельца 2026-09-07, спека §7.2): тексты, их содержащие,
        /// в PDF не выводятся. Охватывает пути классов/свойств, внутренние
        /// артефакты документации (ADR/DEC/wire) и служебные статусы.
        /// </summary>
        private static readonly string[] CodeBaseMarkers =
        {
            "ProjectData",
            "ThermalCalculator",
            "FlowRegimeCalculator",
            "ThermalConstants",
            "Core.Constants",
            "ViewModel",
            "SourceDetail",
            ".cs",
            "DEC-T",
            "ADR-",
            "wire",
            "GlycoProperties",
            "материал БД",
        };

        /// <summary>Служебные статусы билдеров — в PDF не выводятся (§7.2:
        /// трассировка только инженерными категориями).</summary>
        private static readonly string[] StatusMarkers =
        {
            "кодовое значение",
            "не сохраняется",
            "недоступно",
            "условно:",
            "справочно,",
            "требуется привязка",
        };

        /// <summary>
        /// Конструкции псевдокода в формулах — в PDF не выводятся.
        /// </summary>
        private static readonly string[] PseudoCodeMarkers =
        {
            "(int)",
            "(double)",
            "sum(",
            "max(",
            "min(",
            "Count",
        };

        /// <summary>
        /// Подстановки обозначений: идентификаторы модели → обозначения,
        /// используемые самим отчётом в шагах расчёта (t_H, Q_таяния, qTotal…).
        /// Только нотация — выражения не меняются. Порядок: длинные ключи
        /// раньше коротких.
        /// </summary>
        private static readonly (string From, string To)[] SymbolSubstitutions =
        {
            ("interpolation 64/2300 .. Colebrook-White at Re=4000", "интерполяция 64/2300 … Колбрук-Уайт при Re = 4000"),
            ("Colebrook-White iterative", "Колбрук-Уайт, итерационно"),
            ("JHmu_low", "JHmü_низ"),
            ("AlphaBottom", "α_низ"),
            ("DpVerteiler", "Δp_распределителя"),
            ("MeltingHeat", "Q_таяния"),
            ("ConvectionHeat", "Q_конв"),
            ("RadiationHeat", "Q_изл"),
            ("ExcessTemperature", "JHmü"),
            ("MeanTemperature", "T_средняя"),
            ("SupplyTemperature", "T_подачи"),
            ("ReturnTemperature", "T_обратки"),
            ("SurfaceTemperature", "t_П"),
            ("AirTemperature", "t_H"),
            ("GroundTemperature", "t_G"),
            ("CircuitLength", "L_HK"),
            ("SupplyLength", "L_Zul"),
            ("TotalLength", "L_total"),
            ("TotalFlowRate", "V_Σ"),
            ("TotalPower", "P"),
            ("PressureLoss", "Δp"),
            ("SystemVolume", "V_сист"),
            ("PowerTotal", "qTotal"),
            ("MassFlowRate", "ṁ"),
            ("VolumeFlowRate", "V̇_м2"),
            ("RzsCount", "N_РЗС"),
            ("lambdaR", "λ_R"),
            ("lambdaB", "λБ"),
            ("lambdaA", "λА"),
            ("lambdaE", "λE"),
            ("LambdaE", "λE"),
            ("DpRohr", "Δp_трубы"),
            ("DpVent", "Δp_клапана"),
            ("maxDp", "Δp_max"),
            ("epsilon", "ε"),
            ("sigma", "σ"),
            ("alpha", "α"),
            ("lambda_i", "λ_i"),
            ("lambda_I", "λ_I"),
            ("lambda", "λ"),
            ("nu", "ν"),
            ("DeltaT", "ΔT"),
            ("d_inner", "d_вн"),
            ("d_ext", "d_нар"),
            ("totalLength", "L"),
            ("FlowRate", "V"),
            ("q_down", "q↓"),
            ("q_up", "q↑"),
            ("V_dot", "V̇"),
            ("spacing", "s"),
            ("rho", "ρ"),
            ("PI", "π"),
            ("pi", "π"),
            ("->", "→"),
            ("^4", "⁴"),
            ("^3", "³"),
            ("^2", "²"),
        };

        /// <summary>A4-портрет: ширина контентной области, pt (595 − поля 2×50).</summary>
        private const double ContentWidthPoints = 495;

        /// <summary>
        /// Сформировать документ отчёта. Разделы и их порядок — 1:1 с
        /// Markdown-рендером (<see cref="CalculationReportMarkdownRenderer"/>).
        /// </summary>
        public Document Render(CalculationReportData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var document = new Document();

            var section = document.AddSection();
            ConfigurePageSetup(document, section);
            BuildHeader(section.Headers.Primary, data);
            BuildFooter(section.Footers.Primary, data);

            RenderTitle(section, data);
            RenderMethodology(section, data);
            RenderSummary(section, data);
            RenderProjectSection(section, data.ProjectSection);
            RenderClimateSection(section, data.ClimateSection);
            RenderConstructionSection(section, data.ConstructionSection);
            RenderThermalSection(section, data.ThermalSection, data.ClimateSection, data.Mode);
            RenderHydraulicsSection(section, data.HydraulicsSection);
            RenderEquipmentSection(section, data.EquipmentSection);
            RenderWarnings(section, data.Warnings, data.ThermalSection.DetailValidationErrors);
            RenderSourcesAppendix(section, data.SourcesAppendix);
            RenderFormulasAppendix(section, data.FormulasAppendix);

            return document;
        }

        /// <summary>Инициализирует резолвер шрифтов и возвращает имя бренд-шрифта.</summary>
        private static string InitFontName()
        {
            CalculationReportPdfFontBootstrapper.EnsureInitialized();
            return CalculationReportPdfFontBootstrapper.InterAvailable
                ? CalculationReportInterFontResolver.FamilyName
                : "Arial";
        }

        private static void ConfigurePageSetup(Document document, Section section)
        {
            var pageSetup = document.DefaultPageSetup.Clone();
            // A4-портрет задаётся явными размерами: связка PageFormat +
            // Orientation после Clone() в PDFsharp 6.2 ненадёжна (урок №8 Ф8).
            pageSetup.PageWidth = Unit.FromPoint(595);
            pageSetup.PageHeight = Unit.FromPoint(842);
            pageSetup.LeftMargin = Unit.FromPoint(50);
            pageSetup.RightMargin = Unit.FromPoint(50);
            pageSetup.TopMargin = Unit.FromPoint(64);
            pageSetup.HeaderDistance = Unit.FromPoint(28);
            pageSetup.BottomMargin = Unit.FromPoint(56);
            pageSetup.FooterDistance = Unit.FromPoint(28);
            section.PageSetup = pageSetup;
        }

        #region Титул, методика, сводка

        private static void RenderTitle(Section section, CalculationReportData data)
        {
            AddSectionHeading(section, "Детальный расчётный отчёт", headingSize: 16, spaceAfter: 10);

            var modeLabel = FormatModeLabel(data.Mode);
            AddKeyValueTable(section, new (string, string)[]
            {
                ("Тип документа", "детальный расчётный отчёт"),
                ("Режим отчёта", modeLabel),
                ("Номер проекта", data.ProjectSection.ProjectNumber),
                ("Объект", data.ProjectSection.ProjectObject),
                ("Дата формирования", data.ReportDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                ("Версия формата отчёта", "v1"),
            });
            AddSpacer(section, 6);
        }

        private static void RenderMethodology(Section section, CalculationReportData data)
        {
            AddSectionHeading(section, "Методика");
            AddBodyParagraph(section,
                "Расчётные данные приведены по внутренней методике REHAU, реализованной в приложении SnowMeltingCalculator. " +
                "Отчёт не заявляет соответствие ГОСТ/СП, если конкретный источник данных явно не указывает такой источник.",
                italic: false);
            AddSpacer(section, 4);
        }

        private static void RenderSummary(Section section, CalculationReportData data)
        {
            AddSectionHeading(section, "Краткая сводка");
            AddScalarTable(section, new[]
            {
                ("Суммарная тепловая мощность", data.EquipmentSection.TotalThermalPower),
                ("Объём системы", data.EquipmentSection.SystemVolume),
                ("Расход насоса", data.EquipmentSection.PumpFlowRate),
                ("Напор насоса", data.EquipmentSection.PumpHead),
                ("Объём расширительного бака", data.EquipmentSection.ExpansionTankVolume),
                ("Общая длина трубы", data.EquipmentSection.TotalPipeLength),
                ("Количество РЗС / коллекторов", data.EquipmentSection.RzsCount),
            });
            AddKeyValueTable(section, new (string, string)[]
            {
                ("Выбранный режим отчёта", FormatModeLabel(data.Mode)),
            });
            AddSpacer(section, 6);
        }

        private static string FormatModeLabel(CalculationReportMode mode)
        {
            return mode == CalculationReportMode.Operating
                ? "Рабочий режим"
                : "Расчётный/холодный режим";
        }

        #endregion

        #region Исходные данные

        private static void RenderProjectSection(Section section, ProjectSection data)
        {
            AddSectionHeading(section, "Исходные данные проекта");
            AddTable(
                section,
                new[] { 285.0, 120.0, 90.0 },
                new[] { "Параметр", "Значение", "Источник" },
                new[]
                {
                    new[] { "Номер проекта", EmptyAsDash(data.ProjectNumber), SourceLabel(ReportValueSource.Project) },
                    new[] { "Объект", EmptyAsDash(data.ProjectObject), SourceLabel(ReportValueSource.Project) },
                });
            AddSpacer(section, 6);
        }

        private static void RenderClimateSection(Section section, ClimateSection data)
        {
            AddSectionHeading(section, "Климатические данные");
            AddScalarTable(section, new (string, ReportValue<string>)[]
            {
                ("Город", data.City),
                ("Регион", data.Region),
                ("Климатическая зона", data.ClimateZone),
            });
            AddScalarTable(section, new (string, ReportValue<double>)[]
            {
                ("Расчётная температура наружного воздуха", data.AirTemperature),
                ("Скорость ветра", data.WindSpeed),
                ("Относительная влажность", data.Humidity),
                ("Интенсивность снегопада", data.SnowfallIntensity),
                ("Количество дней холодного периода", data.ColdPeriodDays),
                ("Температура поверхности", data.SurfaceTemperature),
                ("Температура грунта", data.GroundTemperature),
                ("Температура подачи", data.SupplyTemperature),
                ("Температура обратки", data.ReturnTemperature),
                ("Средняя температура теплоносителя", data.MeanTemperature),
                ("Температурный перепад", data.DeltaT),
            });
            AddSpacer(section, 6);
        }

        private static void RenderConstructionSection(Section section, ConstructionSection data)
        {
            AddSectionHeading(section, "Конструкция");
            AddScalarTable(section, new[]
            {
                ("Уровень грунтовых вод", data.GroundwaterLevel),
                ("Сопротивление вверх R1", data.R1),
                ("Сопротивление вниз R2", data.R2),
                ("Эквивалентная теплопроводность λE", data.LambdaE),
            });

            if (!string.IsNullOrWhiteSpace(data.LambdaRuleNote))
            {
                AddNoteParagraph(section, data.LambdaRuleNote);
            }

            RenderSteps(section, data.Steps);

            if (data.Layers.Count > 0)
            {
                AddSubHeading(section, "Слои конструкции");
                AddTable(
                    section,
                    new[] { 75.0, 170.0, 80.0, 85.0, 85.0 },
                    new[] { "Позиция", "Материал", "Толщина", "Теплопроводность", "Термическое сопротивление" },
                    data.Layers.Select(layer => new[]
                    {
                        FormatLayerPosition(layer.Position),
                        FormatValue(layer.MaterialName),
                        FormatValueWithUnit(layer.Thickness),
                        FormatValueWithUnit(layer.Lambda),
                        FormatValueWithUnit(layer.ThermalResistance),
                    }).ToList());
            }

            AddSpacer(section, 6);
        }

        #endregion

        #region Теплотехнический расчёт

        private static void RenderThermalSection(
            Section section,
            ThermalSection data,
            ClimateSection climate,
            CalculationReportMode mode)
        {
            AddSectionHeading(section, "Теплотехнический расчёт");
            AddBodyParagraph(section, $"Источник детальных величин: {data.DetailSourceDescription}.");

            if (!data.IsDetailAvailable)
            {
                AddNoteParagraph(
                    section,
                    $"{MissingValue}: детальные тепловые величины недоступны. " +
                    "Выполните тепловой расчёт и повторите экспорт. Ниже сохранённые итоги проекта.");
            }

            if (!string.IsNullOrWhiteSpace(data.DetailNote))
            {
                AddNoteParagraph(section, $"Примечание: {data.DetailNote}");
            }

            if (mode == CalculationReportMode.DesignCold)
            {
                // В3: холодный отчёт — краткая тепловая справка (средняя
                // температура, ΔT — контекст вязкости и ламинарного режима),
                // полный ход расчёта — в рабочем отчёте.
                AddSubHeading(section, "Краткая тепловая справка");
                AddScalarTable(section, new[]
                {
                    ("Полезная мощность вверх", data.PowerUp),
                    ("Мощность вниз", data.PowerDown),
                    ("Суммарная удельная мощность", data.TotalPowerDensity),
                    ("Средняя температура теплоносителя", climate?.MeanTemperature ?? new ReportValue<double>()),
                    ("Температурный перепад", climate?.DeltaT ?? new ReportValue<double>()),
                });
                AddNoteParagraph(
                    section,
                    "Средняя температура теплоносителя и перепад ΔT задают свойства теплоносителя " +
                    "и ламинарный режим холодного пуска. Полный пошаговый тепловой расчёт — в отчёте рабочего режима.");
            }
            else
            {
                // Величины детального набора (ADR-010): при недоступных
                // деталях — маркер «нет данных» в ячейках (В2).
                AddScalarTable(section, new[]
                {
                    ("Коэффициент теплоотдачи", data.Alpha),
                    ("Мощность на плавление снега", data.MeltingHeat),
                    ("Лучистый тепловой поток (справочно)", data.RadiationHeat),
                    ("Конвективный тепловой поток", data.ConvectionHeat),
                    ("Полное сопротивление вверх RFb", data.RFb),
                    ("Полное сопротивление вниз RD", data.RD),
                    ("Параметр затухания M", data.ParameterM),
                    ("КПД ребра EtaR", data.EfficiencyEtaR),
                    ("Избыточная температура", data.ExcessTemperature),
                    ("Массовый расход на м²", data.MassFlowRate),
                    ("Объёмный расход на м²", data.VolumeFlowRate),
                }, value => data.IsDetailAvailable ? FormatValue(value) : MissingValue);

                // Сводный маркер «нет данных» для detail-величин (В2).
                if (!data.IsDetailAvailable)
                {
                    AddBodyParagraph(
                        section,
                        $"α, Q_таяния, Q_конв, Q_изл, RFb, RD, m, ηR, JHmü, расходы — {MissingValue}.");
                }

                AddSubHeading(section, "Пошаговый расчёт");
                RenderSteps(section, data.Steps);

                if (data.Constants.Count > 0)
                {
                    AddSubHeading(section, "Константы расчёта (из кода программы)");
                    AddTable(
                        section,
                        new[] { 175.0, 75.0, 105.0, 140.0 },
                        new[] { "Константа", "Обозначение", "Значение", "Единица" },
                        data.Constants.Select(constant => new[]
                        {
                            constant.Name,
                            constant.Symbol,
                            ReportNumber.Format(constant.Value, constant.Decimals),
                            constant.Unit,
                        }).ToList());
                }
            }

            AddSpacer(section, 6);
        }

        #endregion

        #region Гидравлический расчёт

        private static void RenderHydraulicsSection(Section section, HydraulicsSection data)
        {
            AddSectionHeading(section, "Гидравлический расчёт");
            AddScalarTable(section, new (string, ReportValue<double>)[]
            {
                ("Концентрация гликоля", data.GlycolConcentration),
                ("Плотность теплоносителя", data.Density),
                ("Удельная теплоёмкость", data.SpecificHeat),
                ("Кинематическая вязкость", data.KinematicViscosity),
            });
            AddScalarTable(section, new (string, ReportValue<string>)[]
            {
                ("Тип гликоля", data.GlycolType),
            });

            RenderReferenceCircuit(section, data.ReferenceCircuit);
            RenderModeComparison(section, data.ModeComparison);

            foreach (var collector in data.Collectors.OrderBy(c => c.Number))
            {
                AddSubHeading(section, $"Коллектор {collector.Number}");
                AddBodyParagraph(section, $"Тип: {collector.Type}");
                AddSubHeading(section, "Сводка по коллектору", headingSize: 9.5);
                AddTable(
                    section,
                    new[] { 215.0, 110.0, 80.0, 90.0 },
                    new[] { "Параметр", "Значение", "Единица", "Источник" },
                    new[]
                    {
                        new[] { "Тип коллектора", FormatValue(collector.Summary.CollectorType), "-", SourceLabel(collector.Summary.CollectorType.Source) },
                        new[] { "Количество контуров", FormatValue(collector.Summary.CircuitCount), collector.Summary.CircuitCount.Unit, SourceLabel(collector.Summary.CircuitCount.Source) },
                        new[] { "Общая длина труб", FormatValue(collector.Summary.TotalPipeLength), collector.Summary.TotalPipeLength.Unit, SourceLabel(collector.Summary.TotalPipeLength.Source) },
                        new[] { "Общая мощность", FormatValue(collector.Summary.TotalPower), collector.Summary.TotalPower.Unit, SourceLabel(collector.Summary.TotalPower.Source) },
                        new[] { "Общий расход", FormatValue(collector.Summary.TotalFlowRate), collector.Summary.TotalFlowRate.Unit, SourceLabel(collector.Summary.TotalFlowRate.Source) },
                        new[] { "Потери давления", FormatValue(collector.Summary.PressureLoss), collector.Summary.PressureLoss.Unit, SourceLabel(collector.Summary.PressureLoss.Source) },
                        new[] { "Kv", FormatValue(collector.Summary.Kv), collector.Summary.Kv.Unit, SourceLabel(collector.Summary.Kv.Source) },
                    });

                AddSubHeading(section, "Контуры", headingSize: 9.5);
                RenderCircuits(section, collector.Circuits);
            }

            AddSpacer(section, 6);
        }

        /// <summary>
        /// Таблица контуров; 16 колонок Markdown разбиты на две таблицы
        /// (контент 1:1, компоновка — под A4-портрет).
        /// </summary>
        private static void RenderCircuits(Section section, IReadOnlyList<ReportCircuit> circuits)
        {
            if (circuits.Count == 0)
            {
                return;
            }

            var ordered = circuits.OrderBy(c => c.CircuitNumber).ToList();

            AddTable(
                section,
                new[] { 45.0, 60.0, 60.0, 70.0, 65.0, 60.0, 75.0, 60.0 },
                new[] { "Контур", "Длина", "Площадь", "Мощность", "Расход", "Скорость", "Число Рейнольдса", "Режим течения" },
                ordered.Select(circuit => new[]
                {
                    circuit.CircuitNumber.ToString(CultureInfo.InvariantCulture),
                    FormatValue(circuit.CircuitLength),
                    FormatValue(circuit.CircuitArea),
                    FormatValueWithUnit(circuit.Power),
                    FormatValueWithUnit(circuit.FlowRate),
                    FormatValue(circuit.Velocity),
                    FormatValue(circuit.ReynoldsNumber),
                    FormatValue(circuit.FlowRegime),
                }).ToList());

            AddTable(
                section,
                new[] { 42.0, 45.0, 56.0, 50.0, 72.0, 50.0, 56.0, 80.0, 44.0 },
                new[] { "Контур", "Коэфф. трения", "Удельные потери", "Потери в трубе", "Потери в распределителе", "Потери в вентиле", "Суммарные потери", "Дросселирование", "Обороты клапана" },
                ordered.Select(circuit => new[]
                {
                    circuit.CircuitNumber.ToString(CultureInfo.InvariantCulture),
                    FormatValue(circuit.FrictionFactor),
                    FormatValueWithUnit(circuit.PressureLossPerMeter),
                    FormatValueWithUnit(circuit.DpRohr),
                    FormatValueWithUnit(circuit.DpVerteiler),
                    FormatValueWithUnit(circuit.DpVent),
                    FormatValueWithUnit(circuit.DpGesamt),
                    FormatValueWithUnit(circuit.Throttling),
                    FormatValue(circuit.ValveTurns),
                }).ToList());
        }

        /// <summary>Референсный контур: цепочка шагов + пример балансировки (В4).</summary>
        private static void RenderReferenceCircuit(Section section, ReferenceCircuitSection? reference)
        {
            if (reference is null)
            {
                return;
            }

            AddSubHeading(
                section,
                $"Референсный контур (коллектор {reference.CollectorNumber}, контур {reference.CircuitNumber}, {reference.CollectorType})");
            AddBodyParagraph(
                section,
                $"Контур с максимальными потерями; полная длина {FormatValue(reference.TotalLength)} {reference.TotalLength.Unit}.");
            RenderSteps(section, reference.Steps);

            AddSubHeading(section, "Пример балансировки", headingSize: 9.5);
            if (!string.IsNullOrWhiteSpace(reference.BalancingNote))
            {
                AddNoteParagraph(section, reference.BalancingNote);
            }

            RenderSteps(section, reference.BalancingSteps);

            if (!string.IsNullOrWhiteSpace(reference.DpVentNote))
            {
                AddNoteParagraph(section, reference.DpVentNote);
                AddSpacer(section, 2);
            }
        }

        /// <summary>Сравнение «рабочий vs холодный пуск» (В3, режим DesignCold).</summary>
        private static void RenderModeComparison(Section section, IReadOnlyList<ModeComparisonRow> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            AddSubHeading(section, "Сравнение режимов: рабочий vs холодный пуск");
            AddTable(
                section,
                new[] { 54.0, 49.0, 40.0, 40.0, 45.0, 45.0, 40.0, 40.0, 47.0, 47.0, 48.0 },
                new[] { "Коллектор", "Тип", "ν рабочий, мм²/с", "ν пуск, мм²/с", "Re рабочий", "Re пуск", "λ рабочий", "λ пуск", "Δp рабочий, Па", "Δp пуск, Па", "Кратность" },
                rows.Select(row => new[]
                {
                    row.CollectorNumber.ToString(CultureInfo.InvariantCulture),
                    row.CollectorType,
                    ReportNumber.Format(row.WorkingViscosity),
                    ReportNumber.Format(row.ColdViscosity),
                    ReportNumber.Format(row.WorkingReynolds, "N0"),
                    ReportNumber.Format(row.ColdReynolds, "N0"),
                    ReportNumber.Format(row.WorkingFriction, "N3"),
                    ReportNumber.Format(row.ColdFriction, "N3"),
                    ReportNumber.Format(row.WorkingPressureLossPa, "N0"),
                    ReportNumber.Format(row.ColdPressureLossPa, "N0"),
                    row.GrowthRatio > 0
                        ? "×" + ReportNumber.Format(row.GrowthRatio)
                        : "-",
                }).ToList());
            AddNoteParagraph(
                section,
                "ν, Re и λ — значения худшего контура коллектора; Δp и кратность — по сводке коллектора. " +
                "Холодный пуск: вязкость теплоносителя при расчётной температуре многократно растёт, " +
                "Re падает до ламинарного режима, потери давления увеличиваются в разы — подбор насоса выполняется по наихудшему режиму.");
            AddSpacer(section, 4);
        }

        #endregion

        #region Оборудование, проверки, приложения

        private static void RenderEquipmentSection(Section section, EquipmentSection data)
        {
            AddSectionHeading(section, "Оборудование и KPI");
            AddScalarTable(section, new[]
            {
                ("Суммарная тепловая мощность", data.TotalThermalPower),
                ("Объём системы", data.SystemVolume),
                ("Расход насоса", data.PumpFlowRate),
                ("Напор насоса", data.PumpHead),
                ("Объём расширительного бака", data.ExpansionTankVolume),
                ("Общая длина труб", data.TotalPipeLength),
                ("Количество РЗС / коллекторов", data.RzsCount),
            });

            if (data.CollectorSpecifications.Count > 0)
            {
                AddSubHeading(section, "Спецификации коллекторов");
                AddTable(
                    section,
                    new[] { 50.0, 135.0, 50.0, 70.0, 70.0, 70.0, 50.0 },
                    new[] { "Коллектор", "Тип", "Контуров", "Мощность", "Расход", "Потери давления", "Kv" },
                    data.CollectorSpecifications.OrderBy(s => s.Number).Select(spec => new[]
                    {
                        spec.Number.ToString(CultureInfo.InvariantCulture),
                        spec.Type,
                        spec.CircuitCount.ToString(CultureInfo.InvariantCulture),
                        FormatValueWithUnit(spec.TotalPower),
                        FormatValueWithUnit(spec.TotalFlowRate),
                        FormatValueWithUnit(spec.PressureLoss),
                        FormatValueWithUnit(spec.Kv),
                    }).ToList());
            }

            AddSpacer(section, 6);
        }

        private static void RenderWarnings(
            Section section,
            IReadOnlyList<CalculationReportWarning> warnings,
            IReadOnlyList<string> validationNotes)
        {
            AddSectionHeading(section, "Предупреждения и ограничения");

            if (validationNotes.Count > 0)
            {
                // В7: примечания валидации результата расчёта/пересчёта —
                // в разделе проверок, вместе с v1-лимитами.
                foreach (var note in validationNotes)
                {
                    AddBodyParagraph(section, "• " + note);
                }

                AddSpacer(section, 2);
            }

            if (warnings.Count == 0)
            {
                AddBodyParagraph(section, CalculationReportMarkdownRendererConstants.NoWarningSentinel);
                AddSpacer(section, 6);
                return;
            }

            AddTable(
                section,
                new[] { 172.0, 55.0, 168.0, 100.0 },
                new[] { "Код", "Уровень", "Сообщение", "Связанные значения" },
                warnings.Select(warning => new[]
                {
                    // ZWSP после подчёркиваний: длинные коды переносятся,
                    // а не вылетают за границу ячейки.
                    warning.Code.Replace("_", "_\u200B"),
                    warning.Severity,
                    NormalizeWarningMessage(warning.Message),
                    warning.RelatedValues.Count > 0
                        ? string.Join(", ", warning.RelatedValues)
                        : "-",
                }).ToList(),
                // Семантические цвета — дополнение к текстовому статусу
                // (спека §7.2), статус остаётся читаемым ч/б печатью.
                cellColor: (rowIndex, columnIndex) => columnIndex == 1
                    ? warnings[rowIndex].Severity?.Trim().ToLowerInvariant() switch
                    {
                        "error" => SemanticErrorHex,
                        "warning" => SemanticWarningHex,
                        _ => null,
                    }
                    : null);
            AddSpacer(section, 6);
        }

        private static void RenderSourcesAppendix(Section section, SourcesAppendix appendix)
        {
            AddSectionHeading(section, "Приложение: источники значений");
            if (appendix.Entries.Count == 0)
            {
                AddBodyParagraph(section, "Нет записей.");
                AddSpacer(section, 6);
                return;
            }

            // Решение владельца 2026-09-07 (спека §7.2): колонки с путями кода
            // (SourceDetail, FormulaSource, WhereCalculated, WhereUsed) в PDF
            // не выводятся — трассировка инженерными категориями; формулы —
            // в инженерной нотации. Повторы по ключу «название+обозначение+
            // единица» скрывается — билдеры отдают одну и ту же величину
            // из нескольких секций.
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            var sourceRows = new List<string[]>();
            foreach (var entry in appendix.Entries)
            {
                var row = new[]
                {
                    entry.Name,
                    entry.Symbol,
                    entry.PhysicalMeaning,
                    entry.Unit,
                    SourceLabel(entry.Source),
                    FormatFormulaForPdf(entry.Formula),
                };
                var key = entry.Name + "\u0001" + entry.Symbol + "\u0001" + entry.Unit;
                if (!seenKeys.Add(key))
                {
                    continue;
                }

                sourceRows.Add(row);
            }

            AddTable(
                section,
                new[] { 95.0, 80.0, 120.0, 45.0, 70.0, 85.0 },
                new[] { "Название", "Обозначение", "Физический смысл", "Ед.", "Источник", "Формула" },
                sourceRows);
            AddSpacer(section, 6);
        }

        private static void RenderFormulasAppendix(Section section, FormulasAppendix appendix)
        {
            AddSectionHeading(section, "Приложение: формулы и обозначения");
            if (appendix.Formulas.Count == 0)
            {
                AddBodyParagraph(section, "Нет записей.");
                AddSpacer(section, 6);
                return;
            }

            // SourcePath подавляется (решение владельца, спека §7.2) —
            // колонки «Источник» в PDF нет; выражения — в инженерной нотации.
            var grouped = appendix.Formulas
                .OrderBy(f => f.Section)
                .ThenBy(f => f.Symbol)
                .GroupBy(f => f.Section)
                .ToList();
            var groupList = grouped.ToList();
            for (var i = 0; i < groupList.Count; i++)
            {
                AddSubHeading(section, groupList[i].Key);
                // После последней группы спейсеры не ставятся — иначе в конце
                // документа возможна пустая страница (находка визуального
                // ревью).
                AddFormulaTable(section, groupList[i].ToList(), trailingSpacer: i < groupList.Count - 1);
            }
        }

        #endregion

        #region Шаги расчёта

        /// <summary>Блок шага: формула → подстановка → результат → примечание.
        /// Формула верстается LaTeX-математикой (запрос владельца 2026-09-07);
        /// при невозможности вёрстки — текстовая строка.</summary>
        private static void RenderStep(Section section, CalculationStep step)
        {
            var title = section.AddParagraph();
            title.AddText(step.Title);
            title.Format.Font.Name = FontName;
            title.Format.Font.Size = 9;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = GetColor(TextColorHex);
            title.Format.SpaceBefore = Unit.FromPoint(5);
            title.Format.SpaceAfter = Unit.FromPoint(1);
            title.Format.KeepWithNext = true;

            var formulaImage = CalculationReportLaTeXFormulaRenderer.TryRenderPng(step.FormulaText);
            if (formulaImage != null)
            {
                var formulaParagraph = section.AddParagraph();
                formulaParagraph.Format.LeftIndent = Unit.FromPoint(12);
                formulaParagraph.Format.SpaceAfter = Unit.FromPoint(2);
                AddFormulaImage(formulaParagraph, formulaImage);
            }
            else
            {
                AddStepLine(section, "Формула: ", step.FormulaText);
            }

            if (!string.IsNullOrWhiteSpace(step.SubstitutionText))
            {
                AddStepLine(section, "Подстановка: ", step.SubstitutionText);
            }

            var resultUnit = string.IsNullOrEmpty(step.Result.Unit)
                ? string.Empty
                : " " + step.Result.Unit;
            AddStepLine(
                section,
                "Результат: ",
                $"{FormatValue(step.Result)}{resultUnit}",
                boldValue: true);
            if (!string.IsNullOrWhiteSpace(step.Note))
            {
                // Заметки с внутренними артефактами не выводятся (§7.2).
                var note = SuppressCodeReferences(step.Note);
                if (note != "-")
                {
                    AddStepLine(section, "Примечание: ", note);
                }
            }
        }

        private static void AddStepLine(Section section, string label, string text, bool boldValue = false)
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = Unit.FromPoint(12);
            paragraph.Format.SpaceAfter = Unit.FromPoint(1);
            var labelRun = paragraph.AddFormattedText(label, TextFormat.NotBold);
            labelRun.Font.Name = FontName;
            labelRun.Font.Size = 9;
            labelRun.Font.Color = GetColor(SecondaryTextColorHex);
            var valueRun = paragraph.AddFormattedText(SuppressCodeReferences(text));
            valueRun.Font.Name = FontName;
            valueRun.Font.Size = 9;
            valueRun.Font.Bold = boldValue;
            valueRun.Font.Color = GetColor(TextColorHex);
        }

        /// <summary>Встроить PNG формулы: 0,5 pt/px (рендер двукратный),
        /// ширина ограничена доступной областью.</summary>
        private static void AddFormulaImage(Paragraph paragraph, CalculationReportLaTeXFormulaRenderer.FormulaImage image, double maxWidthPt = 480)
        {
            var widthPt = Math.Min(image.WidthPx * CalculationReportLaTeXFormulaRenderer.PointPerPixel, maxWidthPt);
            var img = paragraph.AddImage("base64:" + Convert.ToBase64String(image.Bytes));
            img.LockAspectRatio = true;
            img.Width = Unit.FromPoint(widthPt);
        }

        /// <summary>Список шагов; пустой список не рендерится.</summary>
        private static void RenderSteps(Section section, IReadOnlyList<CalculationStep> steps)
        {
            foreach (var step in steps)
            {
                RenderStep(section, step);
            }
        }

        #endregion

        #region Колонтитулы

        private static void BuildHeader(HeaderFooter header, CalculationReportData data)
        {
            // Шапка: подпись слева, логотип справа на светлом фоне, под ними —
            // линия Активного Красного (единственный бренд-акцент документа,
            // спека §7.2). Свободное пространство вокруг логотипа ≥ 20% высоты.
            var table = header.AddTable();
            table.Borders.Visible = false;
            table.AddColumn(Unit.FromPoint(ContentWidthPoints - 140));
            table.AddColumn(Unit.FromPoint(140));
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;

            var captionCell = row.Cells[0];
            var caption = captionCell.AddParagraph();
            caption.Format.Font.Name = FontName;
            caption.Format.Font.Size = 8;
            caption.Format.Font.Color = GetColor(SecondaryTextColorHex);
            caption.AddText("РЕХАУ — Калькулятор снеготаяния");
            caption.AddLineBreak();
            caption.AddText($"Проект {EmptyAsDash(data.ProjectSection.ProjectNumber)} · {data.ReportDate:dd.MM.yyyy}");

            var logoCell = row.Cells[1];
            logoCell.Borders.DistanceFromTop = Unit.FromPoint(4);
            logoCell.Borders.DistanceFromBottom = Unit.FromPoint(4);
            logoCell.Borders.DistanceFromLeft = Unit.FromPoint(4);
            logoCell.Borders.DistanceFromRight = Unit.FromPoint(4);
            var logoParagraph = logoCell.AddParagraph();
            logoParagraph.Format.Alignment = ParagraphAlignment.Right;
            var logoBytes = TryLoadLogoBytes();
            if (logoBytes != null)
            {
                // Протокол Ф8: в официальном PDFsharp 6.x байты вставляются
                // fileless base64-протоколом (ImageSource.FromBinary не существует).
                var image = logoParagraph.AddImage("base64:" + Convert.ToBase64String(logoBytes));
                image.LockAspectRatio = true;
                image.Height = Unit.FromPoint(18);
            }
            else
            {
                var fallback = logoParagraph.AddFormattedText("РЕХАУ");
                fallback.Font.Name = FontName;
                fallback.Font.Size = 12;
                fallback.Font.Bold = true;
                fallback.Font.Color = GetColor(BrandRedHex);
            }

            var brandLine = header.AddParagraph();
            brandLine.Format.Font.Size = 1;
            brandLine.Format.SpaceBefore = Unit.FromPoint(2);
            brandLine.Format.SpaceAfter = Unit.FromPoint(0);
            brandLine.Format.Borders.Bottom = new Border
            {
                Width = Unit.FromPoint(1.5),
                Color = GetColor(BrandRedHex),
            };
        }

        /// <summary>
        /// Логотип РЕХАУ — встроенный WPF-ресурс (deploy-независимо, fileless).
        /// </summary>
        private static byte[]? TryLoadLogoBytes()
        {
            try
            {
                // WPF кладёт ресурсы в <Assembly>.g.resources; ResourceManager
                // сам добавляет суффикс «.resources», поэтому корень — «.g».
                var assembly = typeof(CalculationReportPdfRenderer).Assembly;
                var manager = new System.Resources.ResourceManager(assembly.GetName().Name + ".g", assembly);
                using var stream = manager.GetStream("resources/images/rehau_logo.png") as Stream;
                if (stream == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.Length > 0 ? ms.ToArray() : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Логотип отчёта не загружен: {ex.Message}");
                return null;
            }
        }

        private static void BuildFooter(HeaderFooter footer, CalculationReportData data)
        {
            var table = footer.AddTable();
            table.AddColumn(Unit.FromPoint(ContentWidthPoints * 2 / 3));
            table.AddColumn(Unit.FromPoint(ContentWidthPoints / 3));
            var row = table.AddRow();

            var left = row.Cells[0].AddParagraph();
            left.Format.Font.Name = FontName;
            left.Format.Font.Size = 7.5;
            left.Format.Font.Color = GetColor(SecondaryTextColorHex);
            left.AddText($"© {data.ReportDate.Year} РЕХАУ | Расчёт выполнен в РЕХАУ Калькуляторе снеготаяния");

            var right = row.Cells[1].AddParagraph();
            right.Format.Alignment = ParagraphAlignment.Right;
            right.Format.Font.Name = FontName;
            right.Format.Font.Size = 7.5;
            right.Format.Font.Color = GetColor(SecondaryTextColorHex);
            right.AddText("Стр. ");
            right.AddPageField();
            right.AddText(" из ");
            right.AddNumPagesField();
        }

        #endregion

        #region Хелперы форматирования

        /// <summary>Значение double: точность по <see cref="ReportValue{T}.Decimals"/>
        /// величины (В9, спека §7.3), формат таблицы — запасной; «нет данных»
        /// для null и для нуля при <c>!ZeroIsValid</c> (В2/В14); обороты
        /// клапана — дробью. Семантика единая с
        /// <see cref="CalculationReportMarkdownRenderHelper"/> — делегирование,
        /// без дублирования правила.</summary>
        private static string FormatValue(ReportValue<double> value)
        {
            return CalculationReportMarkdownRenderHelper.Value(value);
        }

        private static string FormatValue(ReportValue<string> value)
        {
            return string.IsNullOrWhiteSpace(value.Value) ? MissingValue : value.Value;
        }

        private static string FormatValueWithUnit(ReportValue<double> value)
        {
            var displayValue = FormatValue(value);
            return string.IsNullOrEmpty(value.Unit)
                ? displayValue
                : $"{displayValue} {value.Unit}";
        }

        /// <summary>
        /// Инженерная категория источника (решение владельца 2026-09-07,
        /// спека §7.2) — вместо путей к коду.
        /// </summary>
        private static string SourceLabel(ReportValueSource source)
        {
            return source switch
            {
                ReportValueSource.UserInput => "введено пользователем",
                ReportValueSource.Project => "данные проекта",
                ReportValueSource.ProgramDatabase => "база программы",
                ReportValueSource.Calculated => "рассчитано программой",
                ReportValueSource.Derived => "производная величина",
                _ => source.ToString(),
            };
        }

        private static string EmptyAsDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        /// <summary>
        /// Идентификаторы режимов в текстах предупреждений билдера →
        /// названия режимов по глоссарию (в PDF нет идентификаторов кода).
        /// </summary>
        private static string NormalizeWarningMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "-";
            }

            return message
                .Replace("в режиме DesignCold", "в режиме холодного пуска", StringComparison.Ordinal)
                .Replace("в режиме Operating", "в рабочем режиме", StringComparison.Ordinal);
        }

        /// <summary>
        /// Позиция слоя конструкции: имя enum → подпись по глоссарию.
        /// </summary>
        private static string FormatLayerPosition(string? position)
        {
            return position switch
            {
                "AbovePipe" => "над трубой",
                "BelowPipe" => "под трубой",
                _ => string.IsNullOrWhiteSpace(position) ? "-" : position,
            };
        }

        /// <summary>
        /// Подавить текст, содержащий ссылки на кодовую базу (решение
        /// владельца, спека §7.2): в ячейке остаётся прочерк.
        /// </summary>
        private static string SuppressCodeReferences(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "-";
            }

            foreach (var marker in CodeBaseMarkers)
            {
                if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    return "-";
                }
            }

            return text;
        }

        /// <summary>
        /// Статус формулы для приложения: санкционированные маркеры
        /// («требуется привязка…») остаются, служебная лексика — прочерк.
        /// </summary>
        private static string FormatFormulaStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "-";
            }

            if (status == CalculationReportMarkdownRendererConstants.FormulaNotInMvp
                || status == UnconfirmedStatusMarker)
            {
                return status;
            }

            return FormatFormulaForPdf(status);
        }

        /// <summary>
        /// Приложить инженерную нотацию к формуле из модели (подстановки
        /// обозначений) и подавить служебные статусы и псевдокод — для
        /// колонок «Формула»/«Выражение» приложений (спека §7.2).
        /// </summary>
        private static string FormatFormulaForPdf(string? formula)
        {
            var text = SuppressCodeReferences(formula);
            if (text == "-")
            {
                return text;
            }

            foreach (var (from, to) in SymbolSubstitutions)
            {
                // Ключи из одних букв заменяются целыми словами (lambda не
                // заденет lambdaR); прочие — прямой заменой (символы, ^2).
                if (char.IsAsciiLetter(from[0]) && from.All(char.IsAsciiLetter))
                {
                    text = System.Text.RegularExpressions.Regex.Replace(
                        text, "\\b" + from + "\\b", to);
                }
                else
                {
                    text = text.Replace(from, to, StringComparison.Ordinal);
                }
            }

            foreach (var marker in PseudoCodeMarkers.Concat(StatusMarkers))
            {
                if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    return "-";
                }
            }

            return text;
        }

        private static Color GetColor(string hex)
        {
            return Color.Parse(hex);
        }

        #endregion

        #region Табличные и текстовые примитивы

        /// <summary>
        /// Таблица «Параметр | Значение | Единица | Источник» — PDF-аналог
        /// скалярных таблиц Markdown без колонки «Обозначение» (пути кода).
        /// <paramref name="formatValue"/> — внешнее форматирование значения
        /// (В2: подмена «нет данных»).
        /// </summary>
        private static void AddScalarTable(
            Section section,
            IEnumerable<(string Name, ReportValue<double> Value)> rows,
            Func<ReportValue<double>, string>? formatValue = null)
        {
            var format = formatValue ?? FormatValue;
            AddTable(
                section,
                new[] { 225.0, 115.0, 75.0, 80.0 },
                new[] { "Параметр", "Значение", "Единица", "Источник" },
                rows.Select(row => new[]
                {
                    row.Name,
                    format(row.Value),
                    row.Value.Unit,
                    SourceLabel(row.Value.Source),
                }).ToList());
        }

        private static void AddScalarTable(Section section, IEnumerable<(string Name, ReportValue<string> Value)> rows)
        {
            AddTable(
                section,
                new[] { 225.0, 115.0, 75.0, 80.0 },
                new[] { "Параметр", "Значение", "Единица", "Источник" },
                rows.Select(row => new[]
                {
                    row.Name,
                    FormatValue(row.Value),
                    row.Value.Unit,
                    SourceLabel(row.Value.Source),
                }).ToList());
        }

        private static void AddKeyValueTable(Section section, IReadOnlyList<(string Label, string Value)> rows)
        {
            AddTable(
                section,
                new[] { 160.0, 335.0 },
                null,
                rows.Select(row => new[] { row.Label, row.Value }).ToList());
        }

        /// <summary>
        /// Таблица данных: светло-серая шапка, строки без заливки
        /// (спека §7.2), серые границы. <paramref name="headers"/> = null —
        /// таблица без шапки.
        /// </summary>
        private static void AddTable(
            Section section,
            double[] widths,
            string[]? headers,
            IReadOnlyList<string[]> rows,
            Func<int, int, string?>? cellColor = null)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var table = section.AddTable();
            table.Borders.Width = Unit.FromPoint(0.5);
            table.Borders.Color = GetColor(BorderColorHex);
            table.Rows.LeftIndent = Unit.FromPoint(0);
            foreach (var width in widths)
            {
                table.AddColumn(Unit.FromPoint(width));
            }

            if (headers != null)
            {
                var headerRow = table.AddRow();
                headerRow.HeadingFormat = true;

                headerRow.Shading.Color = GetColor(HeaderBackgroundHex);
                SetCellPadding(headerRow, 3);
                for (var i = 0; i < headers.Length; i++)
                {
                    FillCell(headerRow.Cells[i], headers[i], bold: true, size: 8);
                }
            }

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var cells = rows[rowIndex];
                var row = table.AddRow();
                SetCellPadding(row, 3);
                for (var i = 0; i < cells.Length; i++)
                {
                    FillCell(row.Cells[i], cells[i], bold: false, size: 8, colorHex: cellColor?.Invoke(rowIndex, i));
                }
            }

            AddSpacer(section, 4);
        }

        /// <summary>Внутренние отступы ячеек — иначе текст соседних колонок
        /// сливается на границе (находка визуального ревью).</summary>
        private static void SetCellPadding(Row row, double points)
        {
            row.Borders.DistanceFromTop = Unit.FromPoint(1.5);
            row.Borders.DistanceFromBottom = Unit.FromPoint(1.5);
            row.Borders.DistanceFromLeft = Unit.FromPoint(points);
            row.Borders.DistanceFromRight = Unit.FromPoint(points);
        }

        private static void FillCell(Cell cell, string? text, bool bold, double size, string? colorHex = null)
        {
            // Подавление ссылок на кодовую базу — общая точка прохода
            // табличного текста билдеров (спека §7.2).
            var paragraph = cell.AddParagraph(SuppressCodeReferences(text));
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = size;
            paragraph.Format.Font.Color = GetColor(colorHex ?? TextColorHex);
            paragraph.Format.Font.Bold = bold;
        }

        /// <summary>
        /// Таблица приложения формул: «Выражение» верстается LaTeX-математикой
        /// (запрос владельца 2026-09-07), при невозможности — текст.
        /// </summary>
        private static void AddFormulaTable(Section section, IReadOnlyList<ReportFormula> formulas, bool trailingSpacer = true)
        {
            var table = section.AddTable();
            table.Borders.Width = Unit.FromPoint(0.5);
            table.Borders.Color = GetColor(BorderColorHex);
            foreach (var width in new[] { 80.0, 300.0, 115.0 })
            {
                table.AddColumn(Unit.FromPoint(width));
            }

            var headerRow = table.AddRow();
            headerRow.HeadingFormat = true;
            headerRow.Shading.Color = GetColor(HeaderBackgroundHex);
            SetCellPadding(headerRow, 3);
            FillCell(headerRow.Cells[0], "Символ", bold: true, size: 8);
            FillCell(headerRow.Cells[1], "Выражение", bold: true, size: 8);
            FillCell(headerRow.Cells[2], "Статус", bold: true, size: 8);

            foreach (var formula in formulas)
            {
                var row = table.AddRow();
                SetCellPadding(row, 3);
                FillCell(row.Cells[0], formula.Symbol, bold: false, size: 8);

                var expression = string.IsNullOrWhiteSpace(formula.Expression)
                    ? null
                    : FormatFormulaForPdf(formula.Expression);
                var image = expression == null
                    ? null
                    : CalculationReportLaTeXFormulaRenderer.TryRenderPng(expression);
                if (image != null)
                {
                    var imageParagraph = row.Cells[1].AddParagraph();
                    AddFormulaImage(imageParagraph, image, maxWidthPt: 290);
                }
                else
                {
                    FillCell(
                        row.Cells[1],
                        expression ?? CalculationReportMarkdownRendererConstants.FormulaNotInMvp,
                        bold: false,
                        size: 8);
                }

                var status = FormatFormulaStatus(formula.FormulaStatus);
                if (string.IsNullOrWhiteSpace(formula.Expression) && !string.IsNullOrWhiteSpace(formula.FormulaStatus))
                {
                    status = CalculationReportMarkdownRendererConstants.FormulaNotInMvp;
                }

                FillCell(row.Cells[2], status, bold: false, size: 8);
            }

            if (trailingSpacer)
            {
                AddSpacer(section, 4);
            }
        }

        private static void AddSectionHeading(Section section, string text, double headingSize = 12, double spaceAfter = 4)
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = headingSize;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Color = GetColor(TextColorHex);
            paragraph.Format.SpaceBefore = Unit.FromPoint(headingSize >= 16 ? 0 : 10);
            paragraph.Format.SpaceAfter = Unit.FromPoint(spaceAfter);
            paragraph.Format.KeepWithNext = true;
        }

        private static void AddSubHeading(Section section, string text, double headingSize = 10.5)
        {
            var paragraph = section.AddParagraph(text);
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = headingSize;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Color = GetColor(TextColorHex);
            paragraph.Format.SpaceBefore = Unit.FromPoint(8);
            paragraph.Format.SpaceAfter = Unit.FromPoint(3);
            paragraph.Format.KeepWithNext = true;
        }

        private static void AddBodyParagraph(Section section, string text, bool italic = false)
        {
            var paragraph = section.AddParagraph(SuppressCodeReferences(text));
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = GetColor(TextColorHex);
            paragraph.Format.SpaceAfter = Unit.FromPoint(2);
        }

        private static void AddNoteParagraph(Section section, string text)
        {
            var paragraph = section.AddParagraph(SuppressCodeReferences(text));
            paragraph.Format.LeftIndent = Unit.FromPoint(12);
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = 8.5;
            paragraph.Format.Font.Color = GetColor(SecondaryTextColorHex);
            paragraph.Format.SpaceBefore = Unit.FromPoint(2);
            paragraph.Format.SpaceAfter = Unit.FromPoint(2);
        }

        private static void AddSpacer(Section section, double points)
        {
            var paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = Unit.FromPoint(points);
            paragraph.Format.SpaceAfter = Unit.FromPoint(0);
            paragraph.Format.Font.Size = 1;
        }

        #endregion
    }
}
