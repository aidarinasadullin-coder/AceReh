using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.ViewModels.Construction
{
    /// <summary>
    /// ViewModel редактора материалов.
    /// Позволяет добавлять, редактировать и удалять пользовательские материалы.
    /// Встроенные материалы (IsBuiltIn == true) не подлежат редактированию и удалению.
    /// </summary>
    public partial class MaterialEditorViewModel : ObservableObject
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionTemplateRepository _templateRepository;
        private readonly MaterialCrudValidator _validator;
        private readonly IDialogService _dialogService;
        private readonly ConstructionModel? _construction;
        private bool _isNewMaterial;

        /// <summary>
        /// Событие запроса закрытия окна.
        /// View подписывается на это событие и вызывает Window.Close().
        /// </summary>
        public event Action? RequestClose;

        #region Observable Properties

        /// <summary>
        /// Список всех материалов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Material> _materials = new();

        /// <summary>
        /// Выбранный в списке материал
        /// </summary>
        [ObservableProperty]
        private Material? _selectedMaterial;

        /// <summary>
        /// Редактируемая копия материала (черновик).
        /// Изменения применяются к оригиналу только при Save.
        /// </summary>
        [ObservableProperty]
        private Material? _editingMaterial;

        /// <summary>
        /// Сообщение об ошибке валидации/сохранения
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Признак загрузки данных
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Выбран встроенный материал (только для чтения информации)
        /// </summary>
        public bool IsBuiltInSelected => SelectedMaterial?.IsBuiltIn ?? false;

        /// <summary>
        /// Можно ли редактировать текущий материал
        /// </summary>
        public bool CanEditMaterial => EditingMaterial != null && !EditingMaterial.IsBuiltIn;

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel редактора материалов
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов</param>
        /// <param name="templateRepository">Репозиторий шаблонов конструкций (для проверки ссылок при удалении)</param>
        /// <param name="validator">Валидатор материала</param>
        /// <param name="dialogService">Сервис диалоговых окон</param>
        /// <param name="construction">Текущая конструкция (опционально, для проверки ссылок при удалении)</param>
        public MaterialEditorViewModel(
            IMaterialRepository materialRepository,
            IConstructionTemplateRepository templateRepository,
            MaterialCrudValidator validator,
            IDialogService dialogService,
            ConstructionModel? construction = null)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _construction = construction;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Загрузить материалы из репозитория.
        /// Вызывается из View после установки DataContext.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await _materialRepository.LoadMaterialsAsync();
                Materials = new ObservableCollection<Material>(_materialRepository.GetAllMaterials());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки материалов: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// При выборе материала в списке — подготовить черновик для редактирования.
        /// Встроенные материалы отображаются в режиме только для чтения.
        /// </summary>
        partial void OnSelectedMaterialChanged(Material? value)
        {
            if (value == null)
            {
                EditingMaterial = null;
                ErrorMessage = string.Empty;
                OnPropertyChanged(nameof(IsBuiltInSelected));
                OnPropertyChanged(nameof(CanEditMaterial));
                SaveCommand.NotifyCanExecuteChanged();
                return;
            }

            _isNewMaterial = false;
            EditingMaterial = CloneMaterial(value);
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsBuiltInSelected));
            OnPropertyChanged(nameof(CanEditMaterial));
            SaveCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда добавления нового материала.
        /// Создаёт новый пользовательский материал (IsBuiltIn = false) и выбирает его для редактирования.
        /// </summary>
        [RelayCommand]
        private void Add()
        {
            _isNewMaterial = true;
            SelectedMaterial = null;
            EditingMaterial = new Material
            {
                Id = 0,
                Name = string.Empty,
                Category = MaterialCategory.Concrete,
                LambdaA = 0.1,
                LambdaB = 0.1,
                IsBuiltIn = false
            };
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(CanEditMaterial));
            OnPropertyChanged(nameof(IsBuiltInSelected));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// При изменении редактируемого материала обновляем производные свойства и доступность команд.
        /// </summary>
        partial void OnEditingMaterialChanged(Material? value)
        {
            OnPropertyChanged(nameof(CanEditMaterial));
            OnPropertyChanged(nameof(IsBuiltInSelected));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Команда сохранения материала.
        /// Валидирует черновик, сохраняет в репозиторий и обновляет список.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (EditingMaterial == null) return;

            var result = _validator.Validate(EditingMaterial);
            if (!result.IsValid)
            {
                ErrorMessage = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
                return;
            }

            ErrorMessage = string.Empty;

            try
            {
                if (_isNewMaterial || _materialRepository.GetMaterialById(EditingMaterial.Id) == null)
                {
                    var added = await _materialRepository.AddAsync(EditingMaterial);
                    _isNewMaterial = false;
                    EditingMaterial = CloneMaterial(added);
                }
                else
                {
                    // Копируем значения черновика в оригинал
                    var original = _materialRepository.GetMaterialById(EditingMaterial.Id);
                    if (original != null)
                    {
                        original.Name = EditingMaterial.Name;
                        original.Category = EditingMaterial.Category;
                        original.LambdaA = EditingMaterial.LambdaA;
                        original.LambdaB = EditingMaterial.LambdaB;
                        original.MaxSupplyTemp = EditingMaterial.MaxSupplyTemp;
                        original.MinOutdoorTemp = EditingMaterial.MinOutdoorTemp;
                        original.Notes = EditingMaterial.Notes;
                    }
                    await _materialRepository.UpdateAsync(EditingMaterial);
                }

                await _materialRepository.SaveMaterialsAsync();
                Materials = new ObservableCollection<Material>(_materialRepository.GetAllMaterials());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            }
        }

        private bool CanSave() => CanEditMaterial;

        /// <summary>
        /// Команда удаления материала.
        /// Блокирует удаление встроенных материалов и материалов, на которые ссылаются
        /// шаблоны конструкций или текущая конструкция.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteAsync()
        {
            if (SelectedMaterial == null) return;

            if (SelectedMaterial.IsBuiltIn)
            {
                _dialogService.ShowError(
                    "Невозможно удалить встроенный материал.",
                    "Удаление запрещено");
                return;
            }

            // Проверка ссылок из шаблонов конструкций
            try
            {
                var templates = (await _templateRepository.GetAllAsync()).ToList();
                var referencedTemplates = templates
                    .Where(t => t.LayersAbovePipe.Any(l => l.MaterialId == SelectedMaterial.Id) ||
                                t.LayersBelowPipe.Any(l => l.MaterialId == SelectedMaterial.Id))
                    .ToList();

                if (referencedTemplates.Count > 0)
                {
                    var names = string.Join(", ", referencedTemplates.Select(t => $"'{t.Name}'"));
                    _dialogService.ShowError(
                        $"Материал используется в шаблонах: {names}. Удаление невозможно.",
                        "Удаление запрещено");
                    return;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"Не удалось проверить ссылки на материал: {ex.Message}",
                    "Ошибка");
                return;
            }

            // Проверка ссылок из текущей конструкции
            if (_construction != null)
            {
                bool usedInConstruction = _construction
                    .GetAllLayers()
                    .Any(l => l.Material != null && l.Material.Id == SelectedMaterial.Id);

                if (usedInConstruction)
                {
                    _dialogService.ShowError(
                        "Материал используется в текущей конструкции. Удаление невозможно.",
                        "Удаление запрещено");
                    return;
                }
            }

            try
            {
                await _materialRepository.DeleteAsync(SelectedMaterial.Id);
                await _materialRepository.SaveMaterialsAsync();
                Materials = new ObservableCollection<Material>(_materialRepository.GetAllMaterials());
                SelectedMaterial = null;
                EditingMaterial = null;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"Ошибка удаления: {ex.Message}",
                    "Ошибка");
            }
        }

        private bool CanDelete() => SelectedMaterial != null && !SelectedMaterial.IsBuiltIn;

        /// <summary>
        /// Команда отмены — закрывает окно редактора.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke();
        }

        #endregion

        #region Private Methods

        private static Material CloneMaterial(Material source)
        {
            return new Material
            {
                Id = source.Id,
                Name = source.Name,
                Category = source.Category,
                LambdaA = source.LambdaA,
                LambdaB = source.LambdaB,
                MaxSupplyTemp = source.MaxSupplyTemp,
                MinOutdoorTemp = source.MinOutdoorTemp,
                Notes = source.Notes,
                IsBuiltIn = source.IsBuiltIn
            };
        }

        #endregion
    }
}