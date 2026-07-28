# Инвентаризация расчётных параметров для детального отчёта

Этот документ является реестром расчётных величин для нового детального расчётного отчёта. Существующий краткий PDF-экспорт `Результат` не меняется и не использует этот реестр как основной контракт.

Правило источников: в отчёт можно включать только значения, формулы и предупреждения, подтверждённые текущим кодом или существующей документацией. Если значение есть в программе, но формула пока не привязана к конкретному источнику, оно помечается как `требуется привязка к существующей формуле`. Новые формулы и новые расчёты ради отчёта не добавляются.

## Источники

| Источник | Роль |
| --- | --- |
| `docs/Formulas_Snegotayanie.md` | Основной справочник формул. |
| `docs/Hydraulics_Analysis.md` | Гидравлические формулы и сверка с Excel. |
| `docs/Расхождения_с_Formulas_Snegotayanie.md` | Известные расхождения между формулами и кодом. |
| `docs/инструкция/README v.2.1.md` | Пример полноты, терминология и демонстрационный расчёт. |
| `src/Services/Thermal/ThermalCalculator.cs` | Реализация теплотехнического расчёта. |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Реализация гидравлического расчёта. |
| `src/Models/Thermal/ThermalCalculationResult.cs` | Поля результата теплотехнического расчёта. |
| `src/Models/Hydraulics/CircuitRow.cs` | Поля контуров, `OperatingResult`, `DesignResult`. |
| `src/Models/Project/ProjectData.cs` | Данные проекта и сохраняемые расчётные результаты. |
| `src/Services/Results/ResultsPdfData.cs` | Текущая поверхность данных краткого PDF. |
| `src/Services/Results/ResultsPdfDataBuilder.cs` | Текущая сборка данных краткого PDF. |
| `src/ViewModels/Results/ResultsViewModel.cs` | Итоговые KPI и сценарии экспорта. |

## Общие правила включения

| Правило | Решение |
| --- | --- |
| Значение есть в `ResultsPdfData` | Включать в детальный отчёт, если оно инженерно значимо. |
| Значение сохраняется в `ProjectData` | Включать, если нужно для воспроизводимости расчёта. |
| Значение есть в `OperatingResult` | Включать в рабочий отчёт. |
| Значение есть в `DesignResult` | Включать в расчётный/холодный отчёт. |
| Значение отображается только как UI-подсказка | Включать условно, если оно есть в состоянии программы. |
| Формула есть в docs, но код отличается | Источник истины — текущий код; расхождение раскрывать примечанием. |
| Формула не найдена | Значение можно показать как `Calculated`, но формулу пометить `требуется привязка к существующей формуле`. |
| Значение не считается программой | Не включать как расчётный результат MVP. |

## Конструкция

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Материал слоя | - | Выбор пользователя или материал из базы | - | UserInput / ProgramDatabase | `ProjectData`, `ResultsPdfData`, `materials_db.json` | Таблица конструкции | Да |
| Толщина слоя | `d_i` | Ввод пользователя | мм | UserInput | `ProjectData`, `ResultsPdfData` | `R_i`, конструкция | Да |
| Теплопроводность слоя | `lambda_i` | Из материала `lambdaA`/`lambdaB` или ручное переопределение | Вт/(м·К) | UserInput / ProgramDatabase | `ProjectData`, `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | `R_i`, `R1`, `R2` | Да |
| Выбор сухой/влажной lambda | `lambdaA` / `lambdaB` | `УГВ < 1 м -> lambdaB`, иначе `lambdaA` | Вт/(м·К) | ProgramDatabase / Project | `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | Теплопроводность нижних слоёв | Да |
| Термическое сопротивление слоя | `R_i` | `R_i = (d_i / 1000) / lambda_i` | м²·К/Вт | Calculated | `ProjectData`, `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | `R1`, `R2` | Да |
| Сопротивление над трубой | `R1` | `sum(R_i)` для слоёв над трубой | м²·К/Вт | Calculated | `ProjectData`, `ResultsPdfData`, `ThermalCalculator.cs` | `RFb`, теплотехника | Да |
| Сопротивление под трубой | `R2` | `sum(R_i)` для слоёв под трубой | м²·К/Вт | Calculated | `ProjectData`, `ResultsPdfData`, `ThermalCalculator.cs` | `RD`, теплотехника | Да |
| Эквивалентная теплопроводность у трубы | `lambdaE` | Теплопроводность слоя замоноличивания трубы; точная привязка к construction-модели | Вт/(м·К) | Calculated / ProgramDatabase | `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | Параметр `m` теории стержня | Да |
| Уровень грунтовых вод | `УГВ` | Ввод пользователя, влияет на `lambdaA`/`lambdaB` | м | UserInput | `ProjectData`, `README v.2.1.md` | Выбор теплопроводности нижних слоёв | Да |
| Схема конструкции | - | Рендер текущей конструкции | PNG | Derived | `ResultsPdfDataBuilder.cs` | Визуальное приложение | Условно |

## Климат и входные тепловые параметры

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Город | - | Выбор пользователя | - | UserInput | `ProjectData`, `ResultsPdfData` | Климатические параметры | Да |
| Регион | - | Из климатической базы, если доступен | - | ProgramDatabase | `ProjectData`, `README v.2.1.md` | Исходные данные | Да, если доступен |
| Расчётная температура воздуха | `t_H` | Из климатической логики/ручного ввода | °C | ProgramDatabase / UserInput | `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | `alpha`, `Q_таяние`, `JHmu` | Да |
| Скорость ветра | `v_H` | Из климатической базы или ручного ввода | м/с | ProgramDatabase / UserInput | `ProjectData`, `ResultsPdfData` | `alpha` | Да |
| Влажность | `phi` | Из климатической базы; в docs указано как информационное значение | % | ProgramDatabase / UserInput | `README v.2.1.md`, `ProjectData` | Исходные данные | Условно, с пометкой о неиспользовании, если не участвует в коде |
| Интенсивность снегопада | `h` | Ввод пользователя | мм/ч | UserInput | `ProjectData`, `ResultsPdfData` | `Q_таяние` | Да |
| Климатическая зона | - | Из климатической логики | - | ProgramDatabase / Calculated | `ProjectData`, `ResultsPdfData` | Исходные данные | Да |
| Холодный период | - | Из климатической базы, если доступен | дн. | ProgramDatabase | `ResultsPdfData` | Исходные данные | Да, если доступен |
| Температура поверхности | `t_P` | Выбранный режим: антиобледенение/таяние/интенсивный режим | °C | UserInput / Derived | `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | `alpha`, `Q_таяние` | Да |
| Температура грунта | `t_G` | Ввод пользователя, default описан как `+10 °C` | °C | UserInput | `ProjectData`, `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | `C`, `PowerDown` | Да |
| Температура подачи | `T_supply` | Ввод пользователя | °C | UserInput | `ProjectData`, `ResultsPdfData` | `T_return`, `DeltaT`, гидравлика | Да |
| Температура обратки | `T_return` | `T_return = 2 * T_mean - T_supply` | °C | Calculated | `ThermalCalculationResult`, `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | `DeltaT`, гидравлика | Да |
| Средняя/рабочая температура | `T_mean` | `T_mean = JHmu + t_H` | °C | Calculated | `ThermalCalculationResult`, `ResultsPdfData`, `README v.2.1.md` | Свойства теплоносителя, гидравлика | Да |
| Температурный перепад | `DeltaT` | `DeltaT = T_supply - T_return` | K | Calculated | `ThermalCalculationResult`, `ProjectData`, `README v.2.1.md` | Расход | Да |

## Теплотехнический расчёт

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Коэффициент теплоотдачи | `alpha` | `alpha = 2.26 * (t_P - t_H)^0.33 + 2.6 * v_H` | Вт/(м²·К) | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `RFb`, `Q_конв` | Да |
| Мощность на плавление снега | `Q_таяние` | `(h / 3600000) * rho_snow * (c_ice * (0 - t_H) + L_melt + c_water * (t_P - 0))` | Вт/м² | Calculated | `ThermalCalculator.cs`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | `PowerUp` | Да |
| Плотность снега | `rho_snow` | Константа, в примере `900` | кг/м³ | Calculated / constant | `ThermalCalculator.cs`, `README v.2.1.md` | `Q_таяние` | Да, в константах |
| Теплоёмкость льда | `c_ice` | Константа кода; в примере `2050` | Дж/(кг·К) | Calculated / constant | `ThermalCalculator.cs`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | `Q_таяние` | Да, с примечанием о расхождениях |
| Теплота плавления льда | `L_melt` | Константа кода; в примере `334000` | Дж/кг | Calculated / constant | `ThermalCalculator.cs`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | `Q_таяние` | Да |
| Теплоёмкость воды | `c_water` | Константа кода; в примере `4180` | Дж/(кг·К) | Calculated / constant | `ThermalCalculator.cs`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | `Q_таяние` | Да |
| Конвективный тепловой поток | `Q_конв` | `Q_конв = alpha * (t_P - t_H)` | Вт/м² | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `PowerUp` | Да |
| Лучистый поток | `Q_изл` | Формула есть в docs; по расхождениям не входит в `PowerUp` | Вт/м² | Calculated / reference | `docs/Formulas_Snegotayanie.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | Справочно | Условно, если значение доступно |
| Полезная мощность вверх | `PowerUp`, `q_FB` | `PowerUp = Q_таяние + Q_конв` | Вт/м² | Calculated | `ThermalCalculationResult`, `ProjectData`, `ResultsPdfData`, `ThermalCalculator.cs` | `JHmu`, итоговая мощность | Да |
| Сопротивление вверх | `RFb` | `RFb = R1 + 1 / alpha` | м²·К/Вт | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `m`, `JHmu`, `PowerDown` | Да |
| Сопротивление вниз | `RD` | `RD = R2 + 1 / alpha_низ`; в docs нижняя граница фактически адиабатическая | м²·К/Вт | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md` | `m`, `JHmu`, `PowerDown` | Да |
| Коэффициент формы | `fm` | Константа `0.6` | - | Calculated / constant | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md` | `m` | Да, в константах |
| Параметр затухания | `m` | `m = 0.6 * sqrt((1/RFb + 1/RD) / (lambdaE * d_ext))` | 1/м | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `etaR` | Да, если доступен; иначе в формульном приложении |
| Аргумент КПД ребра | `x` | `x = m * lR / 2` | - | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md` | `etaR` | Условно |
| КПД ребра | `etaR` | `etaR = tanh(x) / x` | - | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `A`, `JHmu` | Да, если доступен; иначе в формульном приложении |
| Коэффициент A | `A` | `A = 1 / etaR` | - | Calculated | `ThermalCalculator.cs`, `README v.2.1.md` | `JHmu`, `PowerDown` | Условно |
| Коэффициент B | `B` | `B = 1/RFb + 1/RD` | 1/(м²·К/Вт) | Calculated | `ThermalCalculator.cs`, `README v.2.1.md` | `JHmu`, `PowerDown` | Условно |
| Коэффициент C | `C` | `C = abs(t_H - t_G)` | K | Calculated | `ThermalCalculator.cs`, `README v.2.1.md` | `JHmu`, `PowerDown` | Условно |
| Коэффициент D | `D` | `D = lR / (pi * lambdaR)` | м·К/Вт | Calculated | `ThermalCalculator.cs`, `README v.2.1.md` | `JHmu`, `PowerDown` | Условно |
| Коэффициент E | `E` | `E = s / (d - s)` | - | Calculated | `ThermalCalculator.cs`, `README v.2.1.md` | `JHmu`, `PowerDown` | Условно |
| Избыточная температура теплоносителя | `JHmu` | `[A + (B - C/(q_FB * RFb * RD)) * D * E] * q_FB * RFb` | °C | Calculated | `ThermalCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `README v.2.1.md` | `T_mean` | Да |
| Мощность вниз | `PowerDown`, `q_D` | `(JHmu_low * RFb + C * D * E) / (RFb * RD * (A + B * D * E))`, где `JHmu_low = T_mean - t_G` | Вт/м² | Calculated | `ThermalCalculationResult`, `ProjectData`, `ResultsPdfData`, `ThermalCalculator.cs`, `README v.2.1.md` | `TotalPowerDensity` | Да |
| Суммарная удельная мощность | `TotalPowerDensity`, `q_total` | `PowerUp + PowerDown` | Вт/м² | Calculated | `ProjectData`, `ResultsPdfData`, `ThermalCalculationResult` | Мощность контуров, KPI | Да |
| Массовый расход на м² | `m_dot` | `q_total / (c_p / 3.6) / DeltaT` | кг/(ч·м²) | Calculated | `README v.2.1.md`, `docs/Formulas_Snegotayanie.md` | Расход | Условно, если доступен в состоянии программы |
| Объёмный расход на м² | `V_dot_m2` | `m_dot / rho * 1000` | л/(ч·м²) | Calculated | `README v.2.1.md`, `docs/Formulas_Snegotayanie.md` | Расход | Условно, если доступен |
| Рекомендуемая температура подачи | - | `T_mean + 15 / 2`, округление по логике программы | °C | Calculated / Derived | `README v.2.1.md`, текущий UI-код требует привязки | Подсказка пользователю | Условно |

## Теплоноситель

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Тип гликоля | - | Выбор пользователя | - | UserInput | `ProjectData`, `ResultsPdfData` | Свойства теплоносителя | Да |
| Концентрация | - | Выбор пользователя, диапазон валидации описан как 10-90% | % | UserInput | `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | Свойства теплоносителя | Да |
| Плотность | `rho` | Интерполяция из `glycol_data.json` по типу, концентрации, температуре | кг/м³ или г/см³ | ProgramDatabase / Calculated | `README v.2.1.md`, `ProjectData`, `CircuitRow.cs` | Расход, Re, потери давления | Да |
| Удельная теплоёмкость | `c_p` | Интерполяция из `glycol_data.json` | кДж/(кг·К) | ProgramDatabase / Calculated | `README v.2.1.md`, `docs/Formulas_Snegotayanie.md` | Расход | Да |
| Кинематическая вязкость | `nu` | Интерполяция из `glycol_data.json` | мм²/с | ProgramDatabase / Calculated | `README v.2.1.md`, `ProjectData`, `CircuitRow.cs` | Re, коэффициент трения | Да |
| Теплопроводность теплоносителя | `lambda_fluid` | Интерполяция из `glycol_data.json` | Вт/(м·К) | ProgramDatabase / Calculated | `README v.2.1.md` | Свойства теплоносителя | Условно, если доступна |
| Число Прандтля | `Pr` | Из/по данным `glycol_data.json` | - | ProgramDatabase / Calculated | `README v.2.1.md` | Свойства теплоносителя | Условно, если доступно |
| Температура замерзания | - | Из `glycol_data.json`; проверка замерзания по docs не должна заявляться как реализованная без привязки | °C | ProgramDatabase | `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | Предупреждения/справка | Условно |

## Гидравлический расчёт

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Номер контура | - | Номер строки/контура | - | Project | `ProjectData`, `ResultsPdfData` | Таблица контуров | Да |
| Длина греющего участка | `L_HK` | Ввод пользователя или из площади | м | UserInput / Project | `ProjectData`, `README v.2.1.md` | Площадь, мощность, длина | Да |
| Площадь контура | `S` | `S = L_HK * VAHK / 100` или обратная связь `L_HK = S * 100 / VAHK` | м² | Calculated / UserInput | `README v.2.1.md`, `CircuitRow.cs` | Мощность, сводка | Да |
| Длина подводки | `L_Zul` | Ввод пользователя | м | UserInput | `ProjectData`, `README v.2.1.md` | Мощность подводки, потери | Да |
| Общая длина контура | `L_total` | `L_HK + L_Zul` | м | Calculated | `CircuitRow.cs`, `ResultsPdfDataBuilder.cs` | `DpRohr`, объём системы | Да |
| Шаг укладки | `VAHK` | Ввод пользователя | см или мм | UserInput | `ProjectData`, `ResultsPdfData` | Площадь, мощность | Да |
| Шаг подводки | `VAZul` | Ввод пользователя/default | см | UserInput / Project | `ProjectData`, `README v.2.1.md` | Мощность подводки | Да |
| Доля тепла подводки | `qZul` | Ввод/default | % | UserInput / Project | `ProjectData`, `README v.2.1.md` | Мощность контура | Да |
| Мощность контура | `Q_HK` | `[L_HK/(100/VAHK) + L_Zul/(100/VAZul) * (qZul/100)] * (PowerUp + PowerDown)` | Вт | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `ProjectData`, `ResultsPdfData` | Расход, таблица контуров | Да |
| Объёмный расход контура | `V_dot` | `Q_HK * 3.6 / (rho * c_p * DeltaT)`, точные единицы сверять с кодом | л/ч | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `ProjectData`, `ResultsPdfData` | Скорость, потери, сумма расхода | Да |
| Скорость потока | `v` | `V_dot * 4 / (3600 * pi * d_inner^2) * 10^6` | м/с | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `ProjectData`, `ResultsPdfData` | Re, потери, предупреждения | Да |
| Внутренний диаметр трубы | `d_inner` | `d_ext - 2 * s` | мм | ProgramDatabase / Calculated | `docs/Formulas_Snegotayanie.md`, `rehau_products.json` | Скорость, Re, потери | Да |
| Наружный диаметр трубы | `d_ext` | Из базы продукта | мм | ProgramDatabase | `ProjectData`, `rehau_products.json` | `d_inner`, `m` | Да |
| Толщина стенки трубы | `s` | Из базы продукта | мм | ProgramDatabase | `ProjectData`, `rehau_products.json` | `d_inner`, коэффициент `E` | Да |
| Теплопроводность трубы | `lambdaR` | Из базы продукта/формул, пример `0.35` | Вт/(м·К) | ProgramDatabase | `docs/Formulas_Snegotayanie.md`, `rehau_products.json` | Коэффициент `D` | Да |
| Число Рейнольдса | `Re` | `1000 * v * d_inner / nu` | - | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `ProjectData`, `CircuitRow.cs` | Режим течения, коэффициент трения | Да |
| Режим течения | - | По `Re`: ламинарный, переходный, турбулентный; пороги по коду | - | Calculated | `FlowRegimeCalculator.cs`, `ProjectData`, `ResultsPdfData` | Таблица контуров | Да |
| Коэффициент трения, ламинарный | `lambda` | `64 / Re` | - | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `CircuitsCalculator.cs` | Потери давления | Да |
| Коэффициент трения, переходный | `lambda` | Линейная интерполяция от `64/2300` к Colebrook-White при `Re=4000` | - | Calculated | `CircuitsCalculator.cs`, `docs/Hydraulics_Analysis.md` | Потери давления | Да |
| Коэффициент трения, турбулентный | `lambda` | Colebrook-White; точный вид брать из `CircuitsCalculator.cs` | - | Calculated | `CircuitsCalculator.cs`, `docs/Hydraulics_Analysis.md` | Потери давления | Да |
| Шероховатость трубы | `epsilon` | Константа PE-Xa, в docs `0.007 мм` | мм | ProgramDatabase / constant | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md` | Colebrook-White | Да, в константах |
| Удельные потери давления | `R` | `10000 * (v^2 * rho * lambda) / (2 * d_inner) * 100`; единицы привязать к коду | Па/м | Calculated | `docs/Formulas_Snegotayanie.md`, `docs/Hydraulics_Analysis.md`, `ProjectData`, `ResultsPdfData` | `DpRohr`, предупреждения | Да |
| Потери в трубе | `DpRohr` | `(L_HK + L_Zul) * R` | Па / кПа | Calculated | `ProjectData`, `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | `DpGesamt` | Да |
| Потери в распределителе | `DpVerteiler` | Формула зависит от типа коллектора; точный вид брать из кода | Па / кПа | Calculated | `CircuitsCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `ProjectData`, `ResultsPdfData` | `DpGesamt` | Да |
| Потери в вентиле | `DpVent` | Формула зависит от типа коллектора/клапана; точный вид брать из кода | Па / кПа | Calculated | `CircuitsCalculator.cs`, `docs/Formulas_Snegotayanie.md`, `ProjectData`, `ResultsPdfData` | `DpGesamt` | Да |
| Суммарные потери контура | `DpGesamt` | `DpRohr + DpVerteiler + DpVent` | Па / кПа / мбар | Calculated | `ProjectData`, `ResultsPdfData`, `docs/Formulas_Snegotayanie.md` | Определяющий контур, насос, предупреждения | Да |
| Максимальные потери коллектора | `Dp_max` | `max(DpGesamt)` по контурам коллектора | Па / кПа / мбар | Calculated | `CollectorSummary`, `ResultsPdfData`, `HydraulicSummaryBuilder.cs` | Насос, балансировка, предупреждения | Да |
| Дросселирование/увязка | `zu_drosseln` | Формула в docs и примере различается; точную привязку брать из текущего кода | Па / кПа | Calculated | `CircuitsCalculator.cs`, `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | Балансировка | Да, с привязкой к коду |
| Kv балансировочного клапана | `Kv` | Из модели/расчёта; пример IV: `Kv = flow_m3h / sqrt(dp / rho)` | м³/ч | Calculated / ProgramDatabase | `ProjectData`, `ResultsPdfData`, `README v.2.1.md` | Обороты клапана, спецификация | Да |
| Обороты клапана | - | Формула зависит от типа коллектора; IV пример `5.1818 * Kv - 0.23`; HKV-D требует привязки к коду | об. | Calculated | `CircuitsCalculator.cs`, `ProjectData`, `ResultsPdfData`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | Балансировка | Да |
| Референсный контур | - | Контур с максимальными потерями; точную логику брать из кода | - | Calculated | `CircuitRow.cs`, `CircuitsCalculator.cs`, `README v.2.1.md` | Балансировка | Условно |

## Режимозависимые гидравлические результаты

Детальный отчёт должен строиться отдельно по выбранному режиму.

| Режим отчёта | Источник | Включаемые поля |
| --- | --- | --- |
| Рабочий | `OperatingResult` | `Temperature`, `Density`, `KinematicViscosity`, `ReynoldsNumber`, `FlowRegime`, `FrictionFactor`, `PressureLossPerMeter`, `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln`, а также поля строки контура: `Power`, `FlowRate`, `Velocity`, `ValveTurns`. |
| Расчётный/холодный | `DesignResult` | Те же поля, но из `DesignResult`; если результата нет, выводится missing-data предупреждение. |

## Оборудование и KPI

| Параметр | Обозначение | Формула / привязка | Ед. | Источник значения | Где подтверждено | Где используется | В отчёт |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Суммарная тепловая мощность | `Q_total_project` | Агрегация мощности контуров/коллекторов; точную формулу брать из текущего results-кода | кВт | Calculated | `ResultsViewModel.cs`, `ResultsPdfData` | KPI, сводка | Да |
| Объём системы | `V_system` | Объём труб по внутреннему диаметру и длине; точную формулу брать из текущего results-кода | л | Calculated | `ResultsViewModel.cs`, `ResultsPdfData` | KPI, расширительный бак | Да |
| Расход насоса | `Q_pump` | Суммарный расход системы, обычно `sum(flow) / 1000` | м³/ч | Calculated | `ResultsViewModel.cs`, `HydraulicSummaryBuilder.cs`, `ResultsPdfData` | KPI, оборудование | Да |
| Напор насоса | `H_pump` | По максимальным потерям давления; точную привязку брать из текущего results-кода | кПа | Calculated | `ResultsViewModel.cs`, `HydraulicSummaryBuilder.cs`, `ResultsPdfData` | KPI, оборудование | Да |
| Объём расширительного бака | `V_tank` | По объёму системы и коэффициентам, точную формулу брать из текущего results-кода | л | Calculated | `ResultsViewModel.cs`, `ResultsPdfData` | KPI, оборудование | Да |
| Количество коллекторов / РЗС | - | Количество групп коллекторов | шт. | Project / Calculated | `ResultsPdfData`, `ResultsViewModel.cs` | Сводка, оборудование | Да |
| Тип коллектора | - | Подбор по суммарному расходу; docs: `<1.5 -> 1"`, `1.5..2.5 -> 1 1/4"`, `>2.5 -> 1 1/2"`; точную логику брать из `CollectorTypeSelector` | - | Calculated / ProgramDatabase | `docs/Formulas_Snegotayanie.md`, `README v.2.1.md`, `CollectorTypeSelector.cs` | Спецификация | Да |
| Количество контуров коллектора | - | Count active circuits | шт. | Project | `ResultsPdfData`, `ProjectData` | Спецификация | Да |
| Суммарная мощность коллектора | - | Sum circuit power | кВт | Calculated | `ResultsPdfData`, `HydraulicSummaryBuilder.cs` | Спецификация | Да |
| Суммарный расход коллектора | - | Sum circuit flow | м³/ч | Calculated | `ResultsPdfData`, `HydraulicSummaryBuilder.cs` | Спецификация | Да |
| Потери давления коллектора | - | Max/summary pressure loss | мбар | Calculated | `ResultsPdfData`, `HydraulicSummaryBuilder.cs` | Спецификация, предупреждения | Да |
| Kv коллектора/клапана | `Kv` | Из модели/подбора/расчёта | м³/ч | Calculated / ProgramDatabase | `ResultsPdfData`, `ProjectData`, `rehau_products.json` | Спецификация, балансировка | Да |
| Общая длина труб | `L_total_pipe` | Сумма длин контуров и подводок | м | Calculated | `ResultsPdfData`, `ResultsViewModel.cs` | Сводка, объём | Да |

## Предупреждения и лимиты

Предупреждения в отчёте допустимы только при наличии источника в текущих данных, флагах, базах или валидации программы.

| Предупреждение / лимит | Формула / условие | Ед. | Источник | Статус включения |
| --- | --- | --- | --- | --- |
| Толщина слоя вне диапазона | Диапазон из validation/constants или construction-валидации | мм | `README v.2.1.md`, validation-код | Включать, если есть флаг/ошибка |
| Минимальная толщина над трубой | `>= 40 мм` без нагрузок, `>= 50 мм` с нагрузками | мм | `README v.2.1.md`, construction-код требует привязки | Включать при привязке к существующей проверке |
| Превышение `max_supply_temp` материала | `T_supply > max_supply_temp` | °C | `materials_db.json`, `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | Включать как warning, если есть существующий флаг; не заявлять блокировку без кода |
| Асфальт при низкой температуре | Условие по `min_outdoor_temp`, точную границу брать из базы/кода | °C | `materials_db.json`, `README v.2.1.md` | Включать при существующей проверке |
| Валидация наружной температуры | `-50..+10` | °C | `README v.2.1.md`, validation constants | Включать при ошибке проекта/UI |
| Валидация скорости ветра | `0.1..30` | м/с | `README v.2.1.md`, validation constants | Включать при ошибке проекта/UI |
| Валидация влажности | `20..100` | % | `README v.2.1.md`, validation constants | Включать при ошибке проекта/UI |
| Валидация снегопада | `0..20` | мм/ч | `README v.2.1.md`, validation constants | Включать при ошибке проекта/UI |
| Минимальная скорость потока | Текущий код/инструкция: `v >= 0.1`; formula-docs могут отличаться | м/с | `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md`, validation-код | Да, по коду |
| Максимальная скорость потока | Текущий код/инструкция: `v <= 2.0`; formula-docs могут отличаться | м/с | `README v.2.1.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md`, validation-код | Да, по коду |
| Удельные потери давления | `R <= 300 Па/м` | Па/м | `CircuitRow.cs`, `README v.2.1.md` | Да |
| Потери коллектора | `Dp <= 320 мбар`, если такой лимит есть в текущей модели/базе | мбар | `README v.2.1.md`, product DB/validation-код | Да, если лимит привязан |
| Холодный старт | Сравнение `DesignResult.DpGesamt` с существующим лимитом | мбар | `DesignResult`, summary, `README v.2.1.md` | Да, если лимит привязан |
| `T_return < 0` | В docs указано как риск, но автопроверка может быть не реализована | °C | `README v.2.1.md` | Не включать как автоматический warning без флага; можно как примечание о риске |
| `T_return < T_freeze` | В docs есть как риск, но discrepancy-docs указывают, что freezing-check может быть не реализован | °C | `docs/Расхождения_с_Formulas_Snegotayanie.md` | Не включать как warning без флага |
| `DeltaT > 30 K` | Указано в инструкции; код требует привязки | K | `README v.2.1.md`, thermal validation-код | Условно |
| Длина контура > 120 м | В formula-docs есть, в discrepancy-docs отмечено как не реализовано | м | `docs/Formulas_Snegotayanie.md`, `docs/Расхождения_с_Formulas_Snegotayanie.md` | Не включать как автоматический warning без реализации |
| Максимум оборотов клапана | Например `8` для IV в примере; точную логику брать из кода | об. | `README v.2.1.md`, `CircuitsCalculator.cs` | Включать при существующем warning-поле |

## Известные расхождения, которые нельзя скрывать

| Тема | Как отражать в отчёте |
| --- | --- |
| Код против формульной документации | Источник истины для чисел — текущий код. Документация используется для раскрытия формул, но расхождения проверяются по `docs/Расхождения_с_Formulas_Snegotayanie.md`. |
| `Q_изл` | Показывать только как справочный параметр, если он доступен; не включать в `PowerUp`, если код его не включает. |
| Константы плавления снега | Использовать значения текущего кода; если docs отличаются, добавить примечание. |
| `DeltaT = 15 K` | Не считать жёстким результатом, если фактический `DeltaT` зависит от `T_supply` и `T_return`; показывать как рекомендацию/целевой перепад только при привязке к коду. |
| Бетон `50 °C` | Не заявлять блокировку расчёта, если в коде это только warning или не реализовано. |
| Проверка замерзания гликоля | Не заявлять автоматическую проверку, если она не реализована. |
| Длина контура `120 м` | Не заявлять автоматическое предупреждение, если оно не реализовано. |
| Скорость потока | Использовать лимиты текущего кода, а не рекомендательные значения из старых docs, если они отличаются. |
| Формулы HKV-D / IV | Формулы оборотов и потерь привязывать к текущему `CircuitsCalculator.cs`; если формула не подтверждена, помечать `требуется привязка к существующей формуле`. |

## Требования к следующему этапу

Перед реализацией детального отчёта разработчик должен:

1. Проверить каждое поле со статусом `требуется привязка к существующей формуле` по текущему коду.
2. Не добавлять новые расчёты для закрытия пробелов реестра.
3. Не переносить в отчёт предупреждения, которые существуют только в старой документации и не представлены в текущих данных/валидации.
4. Сначала написать тесты на сохранение краткого PDF `Результат`.
5. Затем писать тесты на режимы `Operating`/`DesignCold`, трассировку источников и полноту раскрытия формул.
