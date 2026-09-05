using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.ViewModels.Construction
{
    /// <summary>
    /// ViewModel редактора шаблонов конструкций.
    /// Позволяет добавлять, редактировать и удалять пользовательские шаблоны.
    /// Встроенные шаблоны (IsBuiltIn == true) не подлежат редактированию и удалению.
    /// </summary>
    public partial class TemplateEditorViewModel : ObservableObject
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionTemplateRepository _templateRepository;
        private readonly ConstructionTemplateValidator _validator;
        private readonly IDialogService _dialogService;
        private bool _isNewTemplate;

        /// <summary>
        /// Событие запроса закрытия окна.
        /// View подписывается на это событие и вызывает Window.Close().
        /// </summary>
        public event Action? RequestClose;

        #region Observable Properties

        /// <summary>
        /// Список всех шаблонов конструкций
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ConstructionTemplate> _templates = new();

        /// <summary>
        /// Выбранный в списке шаблон
        /// </summary>
        [ObservableProperty]
        private ConstructionTemplate? _selectedTemplate;

        /// <summary>
        /// Редактируемая копия шаблона (черновик).
        /// Изменения применяются к оригиналу только при Save.
        /// </summary>
        [ObservableProperty]
        private ConstructionTemplate? _editingTemplate;

        /// <summary>
        /// Доступные материалы для выбора в слоях
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Material> _availableMaterials = new();

        /// <summary>
        /// Редактируемые слои над трубой
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<EditableLayer> _editingLayersAbovePipe = new();

        /// <summary>
        /// Редактируемые слои под трубой
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<EditableLayer> _editingLayersBelowPipe = new();

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
        /// Выбран встроенный шаблон (только для чтения информации)
        /// </summary>
        public bool IsBuiltInSelected => SelectedTemplate?.IsBuiltIn ?? false;

        /// <summary>
        /// Можно ли редактировать текущий шаблон
        /// </summary>
        public bool CanEditTemplate => EditingTemplate != null && !EditingTemplate.IsBuiltIn;

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel редактора шаблонов
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов</param>
        /// <param name="templateRepository">Репозиторий шаблонов конструкций</param>
        /// <param name="validator">Валидатор шаблона</param>
        /// <param name="dialogService">Сервис диалоговых окон</param>
        public TemplateEditorViewModel(
            IMaterialRepository materialRepository,
            IConstructionTemplateRepository templateRepository,
            ConstructionTemplateValidator validator,
            IDialogService dialogService)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Загрузить шаблоны и материалы из репозиториев.
        /// Вызывается из View после установки DataContext.
        /// </summary>
        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                if (!_materialRepository.IsLoaded)
                {
                    await _materialRepository.LoadMaterialsAsync();
                }

                AvailableMaterials = new ObservableCollection<Material>(_materialRepository.GetAllMaterials());

                var templates = await _templateRepository.GetAllAsync();
                Templates = new ObservableCollection<ConstructionTemplate>(templates);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// При выборе шаблона в списке — подготовить черновик для редактирования.
        /// Встроенные шаблоны отображаются в режиме только для чтения.
        /// </summary>
        partial void OnSelectedTemplateChanged(ConstructionTemplate? value)
        {
            if (value == null)
            {
                EditingTemplate = null;
                EditingLayersAbovePipe = new ObservableCollection<EditableLayer>();
                EditingLayersBelowPipe = new ObservableCollection<EditableLayer>();
                ErrorMessage = string.Empty;
                OnPropertyChanged(nameof(IsBuiltInSelected));
                OnPropertyChanged(nameof(CanEditTemplate));
                SaveCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                return;
            }

            _isNewTemplate = false;
            EditingTemplate = CloneTemplate(value);
            LoadEditingLayers(EditingTemplate);
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsBuiltInSelected));
            OnPropertyChanged(nameof(CanEditTemplate));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// При смене редактируемого шаблона обновить доступность команд.
        /// </summary>
        partial void OnEditingTemplateChanged(ConstructionTemplate? value)
        {
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            AddLayerAbovePipeCommand.NotifyCanExecuteChanged();
            AddLayerBelowPipeCommand.NotifyCanExecuteChanged();
            RemoveLayerAbovePipeCommand.NotifyCanExecuteChanged();
            RemoveLayerBelowPipeCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда добавления нового шаблона.
        /// Создаёт новый пользовательский шаблон (IsBuiltIn = false) с одним слоем по умолчанию.
        /// </summary>
        [RelayCommand]
        private void Add()
        {
            _isNewTemplate = true;
            SelectedTemplate = null;
            EditingTemplate = new ConstructionTemplate
            {
                Id = 0,
                Name = string.Empty,
                Description = string.Empty,
                DefaultGroundwaterLevel = 2.0,
                IsBuiltIn = false,
                LayersAbovePipe = new(),
                LayersBelowPipe = new()
            };

            EditingLayersAbovePipe = new ObservableCollection<EditableLayer>
            {
                new EditableLayer
                {
                    Material = AvailableMaterials.FirstOrDefault(),
                    Thickness = 50,
                    Order = 0
                }
            };

            EditingLayersBelowPipe = new ObservableCollection<EditableLayer>();

            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(IsBuiltInSelected));
            OnPropertyChanged(nameof(CanEditTemplate));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Команда сохранения шаблона.
        /// Валидирует черновик, сохраняет в репозиторий и обновляет список.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (EditingTemplate == null) return;

            RebuildTemplateLayers();

            var result = _validator.Validate(EditingTemplate);
            if (!result.IsValid)
            {
                ErrorMessage = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
                return;
            }

            ErrorMessage = string.Empty;

            try
            {
                var existing = await _templateRepository.GetByIdAsync(EditingTemplate.Id);
                if (_isNewTemplate || existing == null)
                {
                    var added = await _templateRepository.AddAsync(EditingTemplate);
                    _isNewTemplate = false;
                    EditingTemplate = CloneTemplate(added);
                }
                else
                {
                    var updated = await _templateRepository.UpdateAsync(EditingTemplate);
                    EditingTemplate = CloneTemplate(updated);
                }

                LoadEditingLayers(EditingTemplate);
                await _templateRepository.SaveAsync();
                Templates = new ObservableCollection<ConstructionTemplate>(await _templateRepository.GetAllAsync());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
            }
        }

        private bool CanSave() => CanEditTemplate;

        /// <summary>
        /// Команда удаления шаблона.
        /// Блокирует удаление встроенных шаблонов.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteAsync()
        {
            if (SelectedTemplate == null) return;

            if (SelectedTemplate.IsBuiltIn)
            {
                _dialogService.ShowError(
                    "Невозможно удалить встроенный шаблон.",
                    "Удаление запрещено");
                return;
            }

            try
            {
                await _templateRepository.DeleteAsync(SelectedTemplate.Id);
                await _templateRepository.SaveAsync();
                Templates = new ObservableCollection<ConstructionTemplate>(await _templateRepository.GetAllAsync());
                SelectedTemplate = null;
                EditingTemplate = null;
                EditingLayersAbovePipe = new ObservableCollection<EditableLayer>();
                EditingLayersBelowPipe = new ObservableCollection<EditableLayer>();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"Ошибка удаления: {ex.Message}",
                    "Ошибка");
            }
        }

        private bool CanDelete() => SelectedTemplate != null && !SelectedTemplate.IsBuiltIn;

        /// <summary>
        /// Команда отмены — закрывает окно редактора.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke();
        }

        /// <summary>
        /// Команда добавления нового слоя над трубой.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditLayers))]
        private void AddLayerAbovePipe()
        {
            if (EditingTemplate == null) return;

            EditingLayersAbovePipe.Add(new EditableLayer
            {
                Material = AvailableMaterials.FirstOrDefault(),
                Thickness = 50,
                Order = EditingLayersAbovePipe.Count
            });
        }

        /// <summary>
        /// Команда добавления нового слоя под трубой.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditLayers))]
        private void AddLayerBelowPipe()
        {
            if (EditingTemplate == null) return;

            EditingLayersBelowPipe.Add(new EditableLayer
            {
                Material = AvailableMaterials.FirstOrDefault(),
                Thickness = 100,
                Order = EditingLayersBelowPipe.Count
            });
        }

        private bool CanEditLayers() => EditingTemplate != null && !EditingTemplate.IsBuiltIn;

        /// <summary>
        /// Команда удаления слоя из секции "над трубой".
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditLayers))]
        private void RemoveLayerAbovePipe(EditableLayer? layer)
        {
            if (layer == null || EditingTemplate == null) return;

            EditingLayersAbovePipe.Remove(layer);
            ReindexLayers(EditingLayersAbovePipe);
        }

        /// <summary>
        /// Команда удаления слоя из секции "под трубой".
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditLayers))]
        private void RemoveLayerBelowPipe(EditableLayer? layer)
        {
            if (layer == null || EditingTemplate == null) return;

            EditingLayersBelowPipe.Remove(layer);
            ReindexLayers(EditingLayersBelowPipe);
        }

        #endregion

        #region Private Methods

        private void LoadEditingLayers(ConstructionTemplate template)
        {
            var above = new ObservableCollection<EditableLayer>();
            var below = new ObservableCollection<EditableLayer>();

            foreach (var layer in template.LayersAbovePipe.OrderBy(l => l.Order))
            {
                above.Add(CreateEditableLayer(layer, LayerPosition.AbovePipe));
            }

            foreach (var layer in template.LayersBelowPipe.OrderBy(l => l.Order))
            {
                below.Add(CreateEditableLayer(layer, LayerPosition.BelowPipe));
            }

            EditingLayersAbovePipe = above;
            EditingLayersBelowPipe = below;
        }

        private EditableLayer CreateEditableLayer(LayerTemplate layer, LayerPosition position)
        {
            return new EditableLayer
            {
                Material = _materialRepository.GetMaterialById(layer.MaterialId),
                Thickness = layer.Thickness,
                Order = layer.Order
            };
        }

        private void RebuildTemplateLayers()
        {
            if (EditingTemplate == null) return;

            EditingTemplate.LayersAbovePipe = EditingLayersAbovePipe
                .Select((l, index) => new LayerTemplate
                {
                    MaterialId = l.Material?.Id ?? 0,
                    Thickness = l.Thickness,
                    Position = LayerPosition.AbovePipe,
                    Order = index
                })
                .ToList();

            EditingTemplate.LayersBelowPipe = EditingLayersBelowPipe
                .Select((l, index) => new LayerTemplate
                {
                    MaterialId = l.Material?.Id ?? 0,
                    Thickness = l.Thickness,
                    Position = LayerPosition.BelowPipe,
                    Order = index
                })
                .ToList();
        }

        private void ReindexLayers(ObservableCollection<EditableLayer> layers)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].Order = i;
            }
        }

        private static ConstructionTemplate CloneTemplate(ConstructionTemplate source)
        {
            return new ConstructionTemplate
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                DefaultGroundwaterLevel = source.DefaultGroundwaterLevel,
                IsBuiltIn = source.IsBuiltIn,
                LayersAbovePipe = source.LayersAbovePipe
                    .Select(l => new LayerTemplate
                    {
                        MaterialId = l.MaterialId,
                        Thickness = l.Thickness,
                        Position = l.Position,
                        Order = l.Order
                    })
                    .ToList(),
                LayersBelowPipe = source.LayersBelowPipe
                    .Select(l => new LayerTemplate
                    {
                        MaterialId = l.MaterialId,
                        Thickness = l.Thickness,
                        Position = l.Position,
                        Order = l.Order
                    })
                    .ToList(),
                MaterialSnapshots = source.MaterialSnapshots
                    .Select(s => new MaterialSnapshot
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Category = s.Category,
                        LambdaA = s.LambdaA,
                        LambdaB = s.LambdaB,
                        MaxSupplyTemp = s.MaxSupplyTemp,
                        MinOutdoorTemp = s.MinOutdoorTemp,
                        Notes = s.Notes,
                        IsBuiltIn = s.IsBuiltIn
                    })
                    .ToList()
            };
        }

        #endregion
    }

    /// <summary>
    /// Редактируемое представление слоя шаблона для привязки к UI.
    /// </summary>
    public partial class EditableLayer : ObservableObject
    {
        /// <summary>
        /// Выбранный материал слоя
        /// </summary>
        [ObservableProperty]
        private Material? _material;

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        [ObservableProperty]
        private double _thickness;

        /// <summary>
        /// Порядковый номер слоя
        /// </summary>
        [ObservableProperty]
        private int _order;
    }
}
