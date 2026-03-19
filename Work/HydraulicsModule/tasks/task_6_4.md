# Task 6.4: Обновить MainWindow.xaml

**Этап:** 6 - Интеграция  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 5.1 (CircuitsView.xaml), Task 4.1 (CircuitsViewModel)

---

## 1. Цель задачи

Добавить вкладку "Контура" в главное окно приложения.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| Все | Все юзер-кейсы | Вкладка "Контура" в главном окне |

---

## 3. Изменяемые файлы

### 3.1. MainWindow.xaml

**Путь:** `src/Views/MainWindow.xaml`

**Изменения:**

Добавить TabItem "Контура":

```xml
<TabControl>
    <!-- Существующие вкладки -->
    <TabItem Header="Теплотехнический расчёт">
        <views:ThermalView DataContext="{Binding ThermalViewModel}"/>
    </TabItem>
    
    <TabItem Header="Климат">
        <views:ClimateView DataContext="{Binding ClimateViewModel}"/>
    </TabItem>
    
    <!-- Новая вкладка -->
    <TabItem Header="Контура">
        <views:CircuitsView DataContext="{Binding CircuitsViewModel}"/>
    </TabItem>
</TabControl>
```

---

## 4. Критерии приёмки

- [ ] Вкладка "Контура" добавлена
- [ ] DataContext привязан к CircuitsViewModel
- [ ] Переключение между вкладками работает
- [ ] MainWindow.xaml отображается корректно

---

## 5. Примечания

- Вкладка "Контура" добавляется после вкладки "Климат"
- DataContext привязывается к CircuitsViewModel через DI

---

## 6. Связанные задачи

- Task 4.1: CircuitsViewModel — DataContext для вкладки
- Task 5.1: CircuitsView.xaml — представление для вкладки
- Task 6.1: DI-регистрация — регистрация CircuitsViewModel

---

*Дата создания: 2026-03-17*