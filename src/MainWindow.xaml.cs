using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.Views.Climate;
using SnowMeltingCalculator.Views.Construction;
using SnowMeltingCalculator.Views.Thermal;
using SnowMeltingCalculator.Views.Hydraulics;
using SnowMeltingCalculator.Views.Results;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.ViewModels.Shell;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private readonly MainViewModel _viewModel;
        private readonly IProjectSession _projectStateService;
        private readonly IDialogService _dialogService;
        private bool _isClosingAfterSave;

        private readonly Dictionary<NavigationTarget, object> _moduleViewCache = new();

        /// <summary>
        /// Текущий материализованный модульный View, управляемый оболочкой.
        /// </summary>
        public object? CurrentModuleView { get; private set; }

        /// <summary>
        /// Read-only адаптер правой панели «Сводка» (Фаза 1 редизайна).
        /// </summary>
        public SummaryViewModel Summary { get; }

        private bool _isSummaryVisible;
        /// <summary>
        /// Панель «Сводка» видна на широких окнах (≥1680), на узких скрыта
        /// (план Ф1.5). Управляется из SizeChanged, чтобы не зависеть от
        /// тонкостей биндинга ActualWidth.
        /// </summary>
        public bool IsSummaryVisible
        {
            get => _isSummaryVisible;
            private set
            {
                if (_isSummaryVisible == value) return;
                _isSummaryVisible = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSummaryVisible)));
            }
        }

        /// <summary>
        /// Путь к файлу проекта, который нужно открыть при запуске приложения
        /// (например, при двойном клике по файлу .smc в проводнике).
        /// </summary>
        public string? InitialProjectPath { get; set; }

        public MainWindow(
            MainViewModel viewModel,
            IProjectSession projectStateService,
            IDialogService dialogService,
            SummaryViewModel summary)
        {
            _viewModel = viewModel;
            _projectStateService = projectStateService;
            _dialogService = dialogService;
            Summary = summary;

            InitializeComponent();
            DataContext = viewModel;

            // Сплит-кнопка «Отчёт PDF»: ContextMenu живёт вне визуального
            // дерева и правым кликом открывается без DataContext — команды
            // пунктов привязываются к MainViewModel сразу (ревью Ф6, P2-1)
            ReportExportButton.ContextMenu.DataContext = viewModel;

            // Адаптивность свода: ≥1680 видна, уже — скрыта (план Ф1.5)
            SizeChanged += (_, _) => IsSummaryVisible = ActualWidth >= 1680;
            IsSummaryVisible = ActualWidth >= 1680;

            WireViewModel();

            // Регистрируем обработчик клавиатурных сокращений
            KeyDown += MainWindow_KeyDown;

            // Загружаем проект, переданный через командную строку, после отображения окна
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Обработчик отображения окна: скрывает welcome при старте с файлом
        /// (.smc из проводника — проект сразу открыт) и открывает проект,
        /// переданный через командную строку.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InitialProjectPath))
            {
                _viewModel.DismissWelcome();
            }

            await LoadInitialProjectAsync();
        }

        /// <summary>
        /// Диалог «О программе» (Ф7.2, рендер 06b) — модальный, поверх
        /// главного окна.
        /// </summary>
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        /// <summary>
        /// Загружает проект по пути из <see cref="InitialProjectPath"/>.
        /// </summary>
        private async Task LoadInitialProjectAsync()
        {
            if (string.IsNullOrEmpty(InitialProjectPath))
                return;

            try
            {
                await _viewModel.ResultsViewModel.LoadProjectFromPathAsync(InitialProjectPath);
            }
            catch (Exception ex)
            {
                // Стартовая загрузка проекта не должна ронять приложение из async void-обработчика
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки проекта при запуске: {ex.Message}");
                _dialogService.ShowError(
                    $"Не удалось открыть проект:\n{ex.Message}",
                    "Ошибка загрузки проекта");
            }
            finally
            {
                // Предотвращаем повторную загрузку при последующих событиях Loaded
                InitialProjectPath = null;
            }
        }

        private void WireViewModel()
        {
            // Подписываемся на изменение состояния боковой панели для анимации
            // и на изменение текущей навигационной цели для материализации View.
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Материализуем начальный Climate view лениво при первом обращении к навигации.
            UpdateModuleView(_viewModel.CurrentNavigationTarget);
        }

        /// <summary>
        /// Обработчик клавиатурных сокращений
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+B для переключения боковой панели
            if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _viewModel.ToggleSidebarCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+S для сохранения
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _viewModel.ResultsViewModel.SaveProjectCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+S для сохранения как
            if (e.Key == Key.S && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                _viewModel.ResultsViewModel.SaveProjectAsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+O для открытия
            if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _viewModel.ResultsViewModel.OpenProjectCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+N для создания нового расчёта
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _viewModel.NewCalculationCommand.ExecuteAsync(null);
                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Обработчик закрытия окна с проверкой несохранённых изменений
        /// </summary>
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await MainWindow_ClosingAsync(e);
        }

        private async Task MainWindow_ClosingAsync(System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingAfterSave)
            {
                _isClosingAfterSave = false;
                return;
            }

            if (!_projectStateService.IsDirty)
            {
                return;
            }

            var result = _dialogService.Show(
                "Текущий проект имеет несохранённые изменения. Сохранить перед закрытием?",
                "Закрытие приложения",
                DialogButtons.YesNoCancel,
                DialogIcon.Question);

            switch (result)
            {
                case SnowMeltingCalculator.Services.Navigation.DialogResult.Cancel:
                    e.Cancel = true;
                    break;

                case SnowMeltingCalculator.Services.Navigation.DialogResult.No:
                    break;

                case SnowMeltingCalculator.Services.Navigation.DialogResult.Yes:
                    e.Cancel = true;
                    await _viewModel.ResultsViewModel.SaveProjectCommand.ExecuteAsync(null);
                    if (!_projectStateService.IsDirty)
                    {
                        _isClosingAfterSave = true;
                        Close();
                    }
                    break;
            }
        }

        /// <summary>
        /// Обработчик изменения свойств ViewModel для анимации и навигации
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSidebarCollapsed))
            {
                AnimateSidebar(_viewModel.IsSidebarCollapsed);
            }

            if (e.PropertyName == nameof(MainViewModel.CurrentNavigationTarget))
            {
                UpdateModuleView(_viewModel.CurrentNavigationTarget);
            }
        }

        /// <summary>
        /// Материализует и кэширует View для указанной навигационной цели,
        /// обновляя <see cref="CurrentModuleView"/>.
        /// </summary>
        private void UpdateModuleView(NavigationTarget target)
        {
            CurrentModuleView = ResolveView(target);
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(CurrentModuleView)));
        }

        /// <summary>
        /// Возвращает закэшированный View для цели навигации или создаёт его.
        /// Для Results предварительно гидратирует данные гидравлики.
        /// При ошибке конструирования показывает диалог и возвращает ClimateView.
        /// </summary>
        private object ResolveView(NavigationTarget target)
        {
            var hasCachedView = _moduleViewCache.TryGetValue(target, out var cached);

            try
            {
                if (target == NavigationTarget.Results)
                {
                    _viewModel.ResultsViewModel.LoadHydraulicsDataOnNavigate();
                }

                if (hasCachedView)
                    return _moduleViewCache[target];

                object view = target switch
                {
                    NavigationTarget.Climate => new ClimateView { DataContext = _viewModel.ClimateViewModel },
                    NavigationTarget.Construction => new ConstructionView { DataContext = _viewModel.ConstructionViewModel },
                    NavigationTarget.Thermal => new ThermalView { DataContext = _viewModel.ThermalViewModel },
                    NavigationTarget.Hydraulics => new CircuitsView { DataContext = _viewModel.CircuitsViewModel },
                    NavigationTarget.Results => new ResultsView { DataContext = _viewModel.ResultsViewModel },
                    _ => new ClimateView { DataContext = _viewModel.ClimateViewModel }
                };

                _moduleViewCache[target] = view;
                return view;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"Ошибка при открытии раздела:\n{ex.Message}",
                    "Ошибка навигации");

                return _moduleViewCache.TryGetValue(NavigationTarget.Climate, out var fallback)
                    ? fallback
                    : new ClimateView { DataContext = _viewModel.ClimateViewModel };
            }
        }

        /// <summary>
        /// Анимация сворачивания/разворачивания боковой панели
        /// </summary>
        private void AnimateSidebar(bool isCollapsed)
        {
            var sidebarGrid = FindName("SidebarGrid") as System.Windows.Controls.Grid;
            if (sidebarGrid == null) return;

            var animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            if (isCollapsed)
            {
                animation.From = 230;
                animation.To = 70;
            }
            else
            {
                animation.From = 70;
                animation.To = 230;
            }

            sidebarGrid.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, animation);
        }

        #region Обработчики кнопок управления окном

        /// <summary>
        /// Перетаскивание окна за хедер; двойной клик — развернуть/восстановить
        /// (Фаза 3Б)
        /// </summary>
        private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Свернуть"
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Обработчик кнопки "Развернуть/Восстановить"; глиф и тултип
        /// переключаются декларативно по WindowState (Shell.WindowMaximizeGlyph)
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        /// <summary>
        /// Обработчик кнопки "Закрыть" (поведение не меняется — Фаза 3Б)
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Сплит-кнопка «Отчёт PDF ▾» (Фаза 6): открывает меню экспорта левым
        /// кликом. DataContext меню привязан в конструкторе (правый клик
        /// открывает ContextMenuService без code-behind — ревью Ф6, P2-1);
        /// здесь задаётся только геометрия (вниз от кнопки). Гейт готовности
        /// данных — IsEnabled кнопки (ResultsViewModel.IsDataReady).
        /// </summary>
        private void ReportExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.ContextMenu is { } menu)
            {
                menu.PlacementTarget = button;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menu.IsOpen = true;
            }
        }

        private void ToggleWindowState() =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        #endregion
    }
}
