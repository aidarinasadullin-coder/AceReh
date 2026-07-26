using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
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
        private readonly IProjectStateService _projectStateService;
        private readonly IDialogService _dialogService;
        private bool _isClosingAfterSave;

        private readonly Dictionary<NavigationTarget, object> _moduleViewCache = new();

        /// <summary>
        /// Текущий материализованный модульный View, управляемый оболочкой.
        /// </summary>
        public object? CurrentModuleView { get; private set; }

        /// <summary>
        /// Путь к файлу проекта, который нужно открыть при запуске приложения
        /// (например, при двойном клике по файлу .smc в проводнике).
        /// </summary>
        public string? InitialProjectPath { get; set; }

        public MainWindow(
            MainViewModel viewModel,
            IProjectStateService projectStateService,
            IDialogService dialogService)
        {
            _viewModel = viewModel;
            _projectStateService = projectStateService;
            _dialogService = dialogService;

            InitializeComponent();
            DataContext = viewModel;

            WireViewModel();

            // Регистрируем обработчик клавиатурных сокращений
            KeyDown += MainWindow_KeyDown;

            // Загружаем проект, переданный через командную строку, после отображения окна
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Обработчик отображения окна: открывает проект, переданный через командную строку.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadInitialProjectAsync();
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
            if (_moduleViewCache.TryGetValue(target, out var cached))
                return cached;

            try
            {
                if (target == NavigationTarget.Results)
                {
                    _viewModel.ResultsViewModel.LoadHydraulicsDataOnNavigate();
                }

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
                animation.From = 220;
                animation.To = 65;
            }
            else
            {
                animation.From = 65;
                animation.To = 220;
            }

            sidebarGrid.BeginAnimation(System.Windows.Controls.Grid.WidthProperty, animation);
        }

        #region Обработчики кнопок управления окном

        /// <summary>
        /// Обработчик перетаскивания окна за хедер
        /// </summary>
        private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
        /// Обработчик кнопки "Развернуть/Восстановить"
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                UpdateMaximizeRestoreButton(sender as System.Windows.Controls.Button, false);
            }
            else
            {
                WindowState = WindowState.Maximized;
                UpdateMaximizeRestoreButton(sender as System.Windows.Controls.Button, true);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Закрыть"
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Обновление иконки кнопки развернуть/восстановить
        /// </summary>
        private void UpdateMaximizeRestoreButton(System.Windows.Controls.Button? button, bool isMaximized)
        {
            if (button == null) return;

            var path = FindVisualChild<Path>(button);
            if (path != null)
            {
                // Иконка "Развернуть": квадратная рамка
                // Иконка "Восстановить": передний квадрат + задний квадрат (только верх и право)
                path.Data = isMaximized
                    ? Geometry.Parse("M3,7 L3,17 L13,17 L13,7 Z M7,3 L17,3 L17,13") // Восстановить: передний полный + задний (верх+право)
                    : Geometry.Parse("M3,3 L15,3 L15,15 L3,15 Z"); // Развернуть: квадратная рамка
            }

            button.ToolTip = isMaximized ? "Восстановить" : "Развернуть";
        }

        /// <summary>
        /// Поиск визуального дочернего элемента
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        #endregion
    }
}
