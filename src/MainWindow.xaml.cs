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

using SnowMeltingCalculator.Services.Logging;
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
        private readonly Services.Updates.IUpdateCheckService _updateCheckService;
        private bool _isClosingAfterSave;
        private bool _isCheckingUpdates;

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
            SummaryViewModel summary,
            Services.Updates.IUpdateCheckService updateCheckService,
            Services.Project.IProjectAutosaveService autosaveService)
        {
            _viewModel = viewModel;
            _projectStateService = projectStateService;
            _dialogService = dialogService;
            _updateCheckService = updateCheckService;
            _autosave = autosaveService;
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

            // «Отменить / Вернуть» (ADR-014, политика хоткеев): глобальный
            // undo/redo перекрывает посимвольный undo текстбокса; tunneling —
            // bubbling KeyDown перехватывается TextBoxBase, а почти все цели
            // отката вводятся из текстбоксов.
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            // Загружаем проект, переданный через командную строку, после отображения окна
            Loaded += MainWindow_Loaded;

            // Автосохранение (план 3.1 роадмапа post-1.8): тик раз в ~2 мин,
            // гвард состояния — внутри SaveAutosnapshotAsync; штатное закрытие
            // гасит снапшот в любом пути (сохранил / отказался / чистый).
            _autosaveTimer.Tick += async (_, _) => await AutosaveTickAsync();
            _autosaveTimer.Interval = TimeSpan.FromMinutes(2);
            _autosaveTimer.Start();
            Closed += (_, _) =>
            {
                _autosaveTimer.Stop();
                _autosave.DeleteSnapshot();
            };
        }

        private readonly Services.Project.IProjectAutosaveService _autosave;
        private readonly System.Windows.Threading.DispatcherTimer _autosaveTimer = new();
        private bool _isAutosaveTickInProgress;

        /// <summary>
        /// Тик автосохранения: политика файла — сервис, гвард состояния — VM
        /// (<see cref="ViewModels.Results.ResultsViewModel.SaveAutosnapshotAsync"/>).
        /// Ошибки — только журнал: сбой автосейва не мешает работе.
        /// </summary>
        private async Task AutosaveTickAsync()
        {
            if (_isAutosaveTickInProgress)
            {
                return;
            }

            _isAutosaveTickInProgress = true;
            try
            {
                await _viewModel.ResultsViewModel.SaveAutosnapshotAsync(_autosave.SnapshotPath);
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.AutosaveTick");
            }
            finally
            {
                _isAutosaveTickInProgress = false;
            }
        }

        /// <summary>
        /// Предложение восстановить проект после аварийного завершения
        /// (план 3.1): показывается только при старте без файла проекта
        /// (признак приходит из App.OnStartup — InitialProjectPath к этому
        /// моменту уже обнулён загрузчиком) и при живом снапшоте.
        /// «Нет» гасит копию — отвергнутое не всплывает при следующих
        /// стартах. Сбой не роняет старт.
        /// </summary>
        public async Task ShowAutosaveRestorePromptAsync(bool startedWithoutFile)
        {
            try
            {
                // Остатки .tmp (крах между записью и move) — чистить до проверки
                _autosave.CleanupStale();

                if (!startedWithoutFile || !_autosave.HasSnapshot())
                {
                    return;
                }

                var timestamp = _autosave.SnapshotTimestamp();
                var stamp = timestamp?.ToString("dd.MM.yyyy HH:mm", Core.AppCulture.Culture) ?? "недавно";
                var answer = _dialogService.Show(
                    $"Обнаружена автосохранённая копия проекта (изменена {stamp}).\nВосстановить её? При отказе копия будет удалена.",
                    "Восстановление проекта",
                    DialogButtons.YesNo,
                    DialogIcon.Question);

                if (answer == SnowMeltingCalculator.Services.Navigation.DialogResult.Yes)
                {
                    var restored = await _viewModel.ResultsViewModel.RestoreFromAutosnapshotAsync(_autosave.SnapshotPath);
                    if (!restored)
                    {
                        // Битый снапшот — не маячить при каждом старте
                        _autosave.DeleteSnapshot();
                    }
                }
                else
                {
                    _autosave.DeleteSnapshot();
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.ShowAutosaveRestorePrompt");
            }
        }

        /// <summary>
        /// Хоткеи «Отменить» (Ctrl+Z) и «Вернуть» (Ctrl+Y) — ADR-014.
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && _viewModel.CanUndo)
            {
                _viewModel.UndoCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control && _viewModel.CanRedo)
            {
                _viewModel.RedoCommand.Execute(null);
                e.Handled = true;
            }
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
        /// «Файл → Инструкция»: открывает инструкцию пользователя
        /// (docs\manual\README.html, деплоится рядом с exe)
        /// в браузере по умолчанию. Отсутствие файла и ошибки запуска
        /// не роняют приложение — показываются через диалог.
        /// </summary>
        private void InstructionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "docs", "manual", "README.html");

            if (!System.IO.File.Exists(path))
            {
                _dialogService.ShowError(
                    $"Файл инструкции не найден:\n{path}",
                    "Инструкция");
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) {
                AppLog.Warn(ex, "MainWindow.InstructionMenuItem_Click");
                _dialogService.ShowError(
                    $"Не удалось открыть инструкцию:\n{ex.Message}",
                    "Инструкция");
            }
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
            catch (Exception ex) {
                AppLog.Warn(ex, "MainWindow.LoadInitialProjectAsync");
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

        // ====================================================================
        // Проверка обновлений и «Что нового» (план 1.3 роадмапа post-1.8).
        // Сеть — только по команде «Проверить обновления»; при старте
        // решение показа принимается локально (WhatsNewTracker).
        // ====================================================================

        /// <summary>
        /// «Файл → Проверить обновления»: сверка версии с манифестом канала.
        /// Доступная версия → диалог с изменениями и кнопкой папки выдачи;
        /// та же версия → «последняя»; недоступный канал → вежливое
        /// сообщение (U9-деградация). Повторный клик во время проверки
        /// игнорируется.
        /// </summary>
        private async void CheckUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_isCheckingUpdates)
            {
                return;
            }

            _isCheckingUpdates = true;
            try
            {
                var outcome = await _updateCheckService.CheckAsync();
                switch (outcome.Kind)
                {
                    case Services.Updates.UpdateCheckKind.UpdateAvailable:
                        var manifest = outcome.Manifest!;
                        ShowWhatsNewDialog(
                            $"Доступна новая версия: {manifest.Version}",
                            $"У вас {Services.Updates.WhatsNewTracker.Normalize(Services.Updates.WhatsNewTracker.CurrentAssemblyVersion()!)}"
                            + (string.IsNullOrEmpty(manifest.PublishedAt) ? string.Empty : $" · выпущена {manifest.PublishedAt}"),
                            manifest.WhatsNew,
                            manifest.FolderUrl);
                        break;
                    case Services.Updates.UpdateCheckKind.UpToDate:
                        _dialogService.Show(
                            $"У вас последняя версия ({Services.Updates.WhatsNewTracker.Normalize(Services.Updates.WhatsNewTracker.CurrentAssemblyVersion()!)}).",
                            "Проверка обновлений",
                            DialogButtons.OK,
                            DialogIcon.Information);
                        break;
                    default:
                        _dialogService.Show(
                            $"Не удалось проверить обновления: {outcome.Reason}",
                            "Проверка обновлений",
                            DialogButtons.OK,
                            DialogIcon.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.CheckUpdatesMenuItem_Click");
            }
            finally
            {
                _isCheckingUpdates = false;
            }
        }

        /// <summary>
        /// Показ «Что нового» при старте, если текущая версия ещё не
        /// показывалась. Вызывается из App.OnStartup ПОСЛЕ закрытия сплэша
        /// (модальный диалог из MainWindow_Loaded повис бы под Topmost-сплэшем
        /// — находка №1 чека R-2026-09-21-02). Только локальное сравнение,
        /// без сети (U2).
        /// </summary>
        public void ShowWhatsNewIfPending()
        {
            try
            {
                var current = Services.Updates.WhatsNewTracker.CurrentAssemblyVersion();
                if (current is null
                    || !Services.Updates.WhatsNewTracker.ShouldShow(AppSettings.Instance.WhatsNewShownVersion, current))
                {
                    return;
                }

                var items = Services.Updates.WhatsNewCatalog.Find(
                    Services.Updates.WhatsNewTracker.Normalize(current));
                if (items is null)
                {
                    return;
                }

                ShowWhatsNewDialog(
                    $"Что нового в версии {Services.Updates.WhatsNewTracker.Normalize(current)}",
                    null,
                    items,
                    folderUrl: null);

                Services.Updates.WhatsNewTracker.MarkShown(AppSettings.Instance, current);
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.ShowWhatsNewIfPending");
            }
        }

        private void ShowWhatsNewDialog(
            string title,
            string? subtitle,
            System.Collections.Generic.IReadOnlyList<string> items,
            string? folderUrl)
        {
            new WhatsNewWindow(title, subtitle, items, folderUrl) { Owner = this }.ShowDialog();
        }

        // ====================================================================
        // Недавние проекты (план 1.2 роадмапа post-1.8) и drag-drop .smc.
        // Оба входа ведут в ResultsViewModel.LoadProjectFromPathAsync —
        // единую точку загрузки по пути (подтверждение dirty внутри).
        // ====================================================================

        /// <summary>
        /// Фасад над AppSettings (состояние одно на приложение — settings.json);
        /// экземпляр шелла независим от экземпляра ResultsViewModel.
        /// </summary>
        private readonly Services.RecentProjects.RecentProjectsService _recentProjects = new();

        /// <summary>
        /// Наполнение подменю «Недавние проекты» при каждом открытии:
        /// отсутствующие на диске файлы скрываются, пустой список —
        /// disabled-заглушка, внизу — «Очистить список».
        /// </summary>
        private void RecentProjectsMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            // System.Windows.Controls MenuItem — полное имя: в
            // SnowMeltingCalculator.Models.Navigation есть свой MenuItem
            // (пункты степпера), он резолвится по using'ам этого файла.
            RecentProjectsMenuItem.Items.Clear();

            var paths = _recentProjects.GetRecent();
            if (paths.Count == 0)
            {
                RecentProjectsMenuItem.Items.Add(new System.Windows.Controls.MenuItem
                {
                    Header = "Нет недавних проектов",
                    IsEnabled = false
                });
                return;
            }

            foreach (var path in paths)
            {
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = System.IO.Path.GetFileName(path),
                    ToolTip = path
                };
                item.Click += async (_, _) => await OpenRecentProjectAsync(path);
                RecentProjectsMenuItem.Items.Add(item);
            }

            RecentProjectsMenuItem.Items.Add(new System.Windows.Controls.Separator());
            var clear = new System.Windows.Controls.MenuItem
            {
                Header = "Очистить список"
            };
            System.Windows.Automation.AutomationProperties.SetAutomationId(clear, "ClearRecentProjectsMenuItem");
            clear.Click += (_, _) => _recentProjects.Clear();
            RecentProjectsMenuItem.Items.Add(clear);
        }

        /// <summary>
        /// Открытие проекта из подменю недавних. Файл мог исчезнуть после
        /// построения меню — LoadProjectFromPathAsync покажет штатный диалог
        /// ошибки (зафиксировано планом 1.2, §4).
        /// </summary>
        private async Task OpenRecentProjectAsync(string path)
        {
            try
            {
                await _viewModel.ResultsViewModel.LoadProjectFromPathAsync(path);
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.OpenRecentProjectAsync");
                _dialogService.ShowError($"Не удалось открыть проект:\n{ex.Message}", "Ошибка");
            }
        }

        /// <summary>
        /// Валиден ли drag-вход: ровно один файл .smc (план 1.2, D3/D9).
        /// </summary>
        private static string? GetDroppedProjectFile(IDataObject data)
        {
            if (!data.GetDataPresent(DataFormats.FileDrop))
            {
                return null;
            }

            if (data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return null;
            }

            return files.FirstOrDefault(Services.RecentProjects.RecentProjectsService.IsProjectFile);
        }

        /// <summary>
        /// Показ оверлея только для валидного входа; иначе — штатный отказ
        /// (Effects.None).
        /// </summary>
        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            var projectFile = GetDroppedProjectFile(e.Data);
            e.Effects = projectFile is not null ? DragDropEffects.Copy : DragDropEffects.None;
            DragDropOverlay.Visibility = projectFile is not null ? Visibility.Visible : Visibility.Hidden;
            e.Handled = true;
        }

        /// <summary>
        /// Гашение оверлея с гардом: DragLeave стреляет и при проходе курсора
        /// над дочерними элементами окна — скрываем только когда курсор
        /// реально покинул окно, иначе фликер (план 1.2, D4).
        /// </summary>
        private void MainWindow_DragLeave(object sender, DragEventArgs e)
        {
            var point = e.GetPosition(this);
            var insideWindow = point.X >= 0 && point.X <= ActualWidth
                && point.Y >= 0 && point.Y <= ActualHeight;
            if (!insideWindow)
            {
                DragDropOverlay.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Сброс файла на окно: первый .smc из списка открывается, остальное
        /// игнорируется (план 1.2, D9).
        /// </summary>
        private async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            DragDropOverlay.Visibility = Visibility.Hidden;

            var projectFile = GetDroppedProjectFile(e.Data);
            if (projectFile is null)
            {
                return;
            }

            try
            {
                await _viewModel.ResultsViewModel.LoadProjectFromPathAsync(projectFile);
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "MainWindow.MainWindow_Drop");
                _dialogService.ShowError($"Не удалось открыть проект:\n{ex.Message}", "Ошибка");
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
            catch (Exception ex) {
                AppLog.Warn(ex, "MainWindow.ResolveView");
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
