using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Immutable persistence record of one custom (non built-in) material.
    /// Mirrors the <see cref="MaterialSnapshot"/> wire fields used by
    /// ProjectData.CustomMaterials without exposing the mutable domain class.
    /// </summary>
    public sealed class ProjectCustomMaterialRecord
    {
        public int Id { get; }
        public string Name { get; }
        public MaterialCategory Category { get; }
        public double LambdaA { get; }
        public double LambdaB { get; }
        public double? MaxSupplyTemp { get; }
        public double? MinOutdoorTemp { get; }
        public string? Notes { get; }
        public bool IsBuiltIn { get; }

        public ProjectCustomMaterialRecord(
            int id,
            string? name,
            MaterialCategory category,
            double lambdaA,
            double lambdaB,
            double? maxSupplyTemp,
            double? minOutdoorTemp,
            string? notes,
            bool isBuiltIn)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
            LambdaA = lambdaA;
            LambdaB = lambdaB;
            MaxSupplyTemp = maxSupplyTemp;
            MinOutdoorTemp = minOutdoorTemp;
            Notes = notes;
            IsBuiltIn = isBuiltIn;
        }
    }

    /// <summary>
    /// Immutable persistence record of one template layer.
    /// Mirrors the mutable <see cref="LayerTemplate"/> wire fields.
    /// </summary>
    public sealed class ProjectTemplateLayerRecord
    {
        public int MaterialId { get; }
        public double Thickness { get; }
        public LayerPosition Position { get; }
        public int Order { get; }

        public ProjectTemplateLayerRecord(int materialId, double thickness, LayerPosition position, int order)
        {
            MaterialId = materialId;
            Thickness = thickness;
            Position = position;
            Order = order;
        }
    }

    /// <summary>
    /// Immutable persistence record of one custom construction template.
    /// Mirrors the ProjectData.CustomTemplates wire shape (identity, name,
    /// description, loads, groundwater level, built-in flag, layer lists and
    /// material portability snapshots) without exposing mutable domain classes.
    /// </summary>
    public sealed class ProjectTemplateRecord
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<ProjectTemplateLayerRecord> LayersAbovePipe { get; }
        public IReadOnlyList<ProjectTemplateLayerRecord> LayersBelowPipe { get; }
        public bool HasLoads { get; }
        public double DefaultGroundwaterLevel { get; }
        public bool IsBuiltIn { get; }
        public IReadOnlyList<ProjectCustomMaterialRecord> MaterialSnapshots { get; }

        public ProjectTemplateRecord(
            int id,
            string? name,
            string? description,
            IEnumerable<ProjectTemplateLayerRecord>? layersAbovePipe,
            IEnumerable<ProjectTemplateLayerRecord>? layersBelowPipe,
            bool hasLoads,
            double defaultGroundwaterLevel,
            bool isBuiltIn,
            IEnumerable<ProjectCustomMaterialRecord>? materialSnapshots)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            LayersAbovePipe = CopyValidated(layersAbovePipe, nameof(layersAbovePipe));
            LayersBelowPipe = CopyValidated(layersBelowPipe, nameof(layersBelowPipe));
            HasLoads = hasLoads;
            DefaultGroundwaterLevel = defaultGroundwaterLevel;
            IsBuiltIn = isBuiltIn;
            MaterialSnapshots = CopyValidated(materialSnapshots, nameof(materialSnapshots));
        }

        private static IReadOnlyList<T> CopyValidated<T>(IEnumerable<T>? source, string paramName)
            where T : class
        {
            if (source is null)
            {
                throw new ArgumentNullException(paramName);
            }

            var items = source.ToArray();
            foreach (var item in items)
            {
                if (item is null)
                {
                    throw new ArgumentException("Collection must not contain null elements.", paramName);
                }
            }

            return Array.AsReadOnly(items);
        }
    }

    /// <summary>
    /// Immutable, self-consistent snapshot of everything a project save needs:
    /// project identity/mode plus the four canonical module state snapshots and
    /// the custom materials/templates carried by the .smc wire format.
    /// Deliberately excludes paths, dirty flags, restore guards, dates and any
    /// transient UI/service state; later save tasks receive dates explicitly.
    /// </summary>
    public sealed class ProjectSnapshot
    {
        public string ProjectNumber { get; }
        public string ProjectObject { get; }
        public bool IsOperatingMode { get; }
        public ClimateStateSnapshot ClimateStateSnapshot { get; }
        public ConstructionStateSnapshot ConstructionStateSnapshot { get; }
        public ThermalStateSnapshot ThermalStateSnapshot { get; }
        public HydraulicsStateSnapshot HydraulicsStateSnapshot { get; }
        public IReadOnlyList<ProjectCustomMaterialRecord> CustomMaterials { get; }
        public IReadOnlyList<ProjectTemplateRecord> CustomTemplates { get; }

        public ProjectSnapshot(
            string? projectNumber,
            string? projectObject,
            bool isOperatingMode,
            ClimateStateSnapshot? climateStateSnapshot,
            ConstructionStateSnapshot? constructionStateSnapshot,
            ThermalStateSnapshot? thermalStateSnapshot,
            HydraulicsStateSnapshot? hydraulicsStateSnapshot,
            IEnumerable<ProjectCustomMaterialRecord>? customMaterials,
            IEnumerable<ProjectTemplateRecord>? customTemplates)
        {
            ProjectNumber = projectNumber ?? throw new ArgumentNullException(nameof(projectNumber));
            ProjectObject = projectObject ?? throw new ArgumentNullException(nameof(projectObject));
            IsOperatingMode = isOperatingMode;
            ClimateStateSnapshot = climateStateSnapshot ?? throw new ArgumentNullException(nameof(climateStateSnapshot));
            ConstructionStateSnapshot = constructionStateSnapshot ?? throw new ArgumentNullException(nameof(constructionStateSnapshot));
            ThermalStateSnapshot = thermalStateSnapshot ?? throw new ArgumentNullException(nameof(thermalStateSnapshot));
            HydraulicsStateSnapshot = hydraulicsStateSnapshot ?? throw new ArgumentNullException(nameof(hydraulicsStateSnapshot));

            var materials = customMaterials switch
            {
                null => throw new ArgumentNullException(nameof(customMaterials)),
                _ => customMaterials.ToArray()
            };
            foreach (var material in materials)
            {
                if (material is null)
                {
                    throw new ArgumentException("Collection must not contain null elements.", nameof(customMaterials));
                }
            }

            CustomMaterials = Array.AsReadOnly(materials);

            var templates = customTemplates switch
            {
                null => throw new ArgumentNullException(nameof(customTemplates)),
                _ => customTemplates.ToArray()
            };
            foreach (var template in templates)
            {
                if (template is null)
                {
                    throw new ArgumentException("Collection must not contain null elements.", nameof(customTemplates));
                }
            }

            CustomTemplates = Array.AsReadOnly(templates);
        }
    }
}
