using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Фаза жизненного цикла теплового расчёта (DEC-T01).
    /// </summary>
    public enum ThermalCalculationPhase
    {
        /// <summary>Данные актуальны.</summary>
        Actual,

        /// <summary>Требуется пересчёт.</summary>
        NeedsRecalculation,

        /// <summary>Выполняется расчёт.</summary>
        Calculating
    }

    /// <summary>
    /// Неизменяемый структурный снимок типа трубы. Equality — строгое
    /// поэлементное сравнение всех шести полей; ссылочное равенство не является
    /// идентичностью: две структурно равные трубы равны.
    /// </summary>
    public sealed class ThermalPipeSnapshot : IEquatable<ThermalPipeSnapshot>
    {
        public string Name { get; }
        public string Article { get; }
        public double OuterDiameter { get; }
        public double InnerDiameter { get; }
        public double WallThickness { get; }
        public double ThermalConductivity { get; }

        public ThermalPipeSnapshot(
            string name,
            string article,
            double outerDiameter,
            double innerDiameter,
            double wallThickness,
            double thermalConductivity)
        {
            Name = name ?? string.Empty;
            Article = article ?? string.Empty;
            OuterDiameter = outerDiameter;
            InnerDiameter = innerDiameter;
            WallThickness = wallThickness;
            ThermalConductivity = thermalConductivity;
        }

        /// <summary>
        /// Защитная копия изменяемого доменного <see cref="PipeType"/> в неизменяемый снимок.
        /// </summary>
        public static ThermalPipeSnapshot? FromPipeType(PipeType? pipe)
        {
            if (pipe is null)
            {
                return null;
            }

            return new ThermalPipeSnapshot(
                pipe.Name,
                pipe.Article,
                pipe.OuterDiameter,
                pipe.InnerDiameter,
                pipe.WallThickness,
                pipe.ThermalConductivity);
        }

        /// <summary>
        /// Новая изменяемая копия для сборки входов калькулятора; владелец снимка
        /// никогда не разделяет мутируемый экземпляр.
        /// </summary>
        public PipeType ToPipeType()
        {
            return new PipeType
            {
                Name = Name,
                Article = Article,
                OuterDiameter = OuterDiameter,
                InnerDiameter = InnerDiameter,
                WallThickness = WallThickness,
                ThermalConductivity = ThermalConductivity
            };
        }

        public bool Equals(ThermalPipeSnapshot? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && string.Equals(Article, other.Article, StringComparison.Ordinal)
                && OuterDiameter.Equals(other.OuterDiameter)
                && InnerDiameter.Equals(other.InnerDiameter)
                && WallThickness.Equals(other.WallThickness)
                && ThermalConductivity.Equals(other.ThermalConductivity);
        }

        public override bool Equals(object? obj) => obj is ThermalPipeSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Name, StringComparer.Ordinal);
            hash.Add(Article, StringComparer.Ordinal);
            hash.Add(OuterDiameter);
            hash.Add(InnerDiameter);
            hash.Add(WallThickness);
            hash.Add(ThermalConductivity);
            return hash.ToHashCode();
        }

        public static bool operator ==(ThermalPipeSnapshot? left, ThermalPipeSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ThermalPipeSnapshot? left, ThermalPipeSnapshot? right)
            => !(left == right);
    }

    /// <summary>
    /// Неизменяемый структурный снимок последнего производного результата теплового
    /// расчёта. Полная поверхность значений <see cref="ThermalCalculationResult"/>
    /// (DEC-T01); <see cref="ValidationErrors"/> — упорядоченный защищённый список.
    /// </summary>
    public sealed class ThermalResultSnapshot : IEquatable<ThermalResultSnapshot>
    {
        private readonly ReadOnlyCollection<string> _validationErrors;

        // === Коэффициенты ===
        public double Alpha { get; }

        // === Мощности ===
        public double PowerUp { get; }
        public double PowerDown { get; }
        public double PowerTotal { get; }

        // === Составляющие мощности ===
        public double MeltingHeat { get; }
        public double RadiationHeat { get; }
        public double ConvectionHeat { get; }

        // === Температуры ===
        public double ExcessTemperature { get; }
        public double MeanTemperature { get; }
        public double SupplyTemperature { get; }
        public double ReturnTemperature { get; }
        public double DeltaT { get; }

        // === Сопротивления ===
        public double RFb { get; }
        public double RD { get; }

        // === Теория стержня ===
        public double ParameterM { get; }
        public double EfficiencyEtaR { get; }

        // === Расходы ===
        public double MassFlowRate { get; }
        public double VolumeFlowRate { get; }

        // === Валидация ===
        public bool IsValid { get; }

        /// <summary>
        /// Упорядоченный список ошибок валидации. Защищённая копия: изменение
        /// исходного массива после создания снимка не влияет на состояние.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors => _validationErrors;

        public ThermalResultSnapshot(
            double alpha,
            double powerUp,
            double powerDown,
            double powerTotal,
            double meltingHeat,
            double radiationHeat,
            double convectionHeat,
            double excessTemperature,
            double meanTemperature,
            double supplyTemperature,
            double returnTemperature,
            double deltaT,
            double rFb,
            double rD,
            double parameterM,
            double efficiencyEtaR,
            double massFlowRate,
            double volumeFlowRate,
            bool isValid,
            IEnumerable<string>? validationErrors)
        {
            Alpha = alpha;
            PowerUp = powerUp;
            PowerDown = powerDown;
            PowerTotal = powerTotal;
            MeltingHeat = meltingHeat;
            RadiationHeat = radiationHeat;
            ConvectionHeat = convectionHeat;
            ExcessTemperature = excessTemperature;
            MeanTemperature = meanTemperature;
            SupplyTemperature = supplyTemperature;
            ReturnTemperature = returnTemperature;
            DeltaT = deltaT;
            RFb = rFb;
            RD = rD;
            ParameterM = parameterM;
            EfficiencyEtaR = efficiencyEtaR;
            MassFlowRate = massFlowRate;
            VolumeFlowRate = volumeFlowRate;
            IsValid = isValid;
            _validationErrors = Array.AsReadOnly(
                validationErrors?.ToArray() ?? Array.Empty<string>());
        }

        /// <summary>
        /// Защитная копия изменяемого доменного результата в неизменяемый снимок.
        /// </summary>
        public static ThermalResultSnapshot? FromResult(ThermalCalculationResult? result)
        {
            if (result is null)
            {
                return null;
            }

            return new ThermalResultSnapshot(
                result.Alpha,
                result.PowerUp,
                result.PowerDown,
                result.PowerTotal,
                result.MeltingHeat,
                result.RadiationHeat,
                result.ConvectionHeat,
                result.ExcessTemperature,
                result.MeanTemperature,
                result.SupplyTemperature,
                result.ReturnTemperature,
                result.DeltaT,
                result.RFb,
                result.RD,
                result.ParameterM,
                result.EfficiencyEtaR,
                result.MassFlowRate,
                result.VolumeFlowRate,
                result.IsValid,
                result.ValidationErrors);
        }

        public bool Equals(ThermalResultSnapshot? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Alpha.Equals(other.Alpha)
                && PowerUp.Equals(other.PowerUp)
                && PowerDown.Equals(other.PowerDown)
                && PowerTotal.Equals(other.PowerTotal)
                && MeltingHeat.Equals(other.MeltingHeat)
                && RadiationHeat.Equals(other.RadiationHeat)
                && ConvectionHeat.Equals(other.ConvectionHeat)
                && ExcessTemperature.Equals(other.ExcessTemperature)
                && MeanTemperature.Equals(other.MeanTemperature)
                && SupplyTemperature.Equals(other.SupplyTemperature)
                && ReturnTemperature.Equals(other.ReturnTemperature)
                && DeltaT.Equals(other.DeltaT)
                && RFb.Equals(other.RFb)
                && RD.Equals(other.RD)
                && ParameterM.Equals(other.ParameterM)
                && EfficiencyEtaR.Equals(other.EfficiencyEtaR)
                && MassFlowRate.Equals(other.MassFlowRate)
                && VolumeFlowRate.Equals(other.VolumeFlowRate)
                && IsValid == other.IsValid
                && ValidationErrors.SequenceEqual(other.ValidationErrors, StringComparer.Ordinal);
        }

        public override bool Equals(object? obj) => obj is ThermalResultSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Alpha);
            hash.Add(PowerUp);
            hash.Add(PowerDown);
            hash.Add(PowerTotal);
            hash.Add(MeltingHeat);
            hash.Add(RadiationHeat);
            hash.Add(ConvectionHeat);
            hash.Add(ExcessTemperature);
            hash.Add(MeanTemperature);
            hash.Add(SupplyTemperature);
            hash.Add(ReturnTemperature);
            hash.Add(DeltaT);
            hash.Add(RFb);
            hash.Add(RD);
            hash.Add(ParameterM);
            hash.Add(EfficiencyEtaR);
            hash.Add(MassFlowRate);
            hash.Add(VolumeFlowRate);
            hash.Add(IsValid);
            foreach (var error in ValidationErrors)
            {
                hash.Add(error, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }

        public static bool operator ==(ThermalResultSnapshot? left, ThermalResultSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ThermalResultSnapshot? left, ThermalResultSnapshot? right)
            => !(left == right);
    }

    /// <summary>
    /// Неизменяемый структурный снимок пользовательских входных данных теплового
    /// модуля. Дефолты точны по DEC-T01: Melting / 50.0 / 10.0 / null / 200.
    /// </summary>
    public sealed class ThermalInputsSnapshot : IEquatable<ThermalInputsSnapshot>
    {
        /// <summary>Точные дефолты сброса (<c>ThermalViewModel.Reset</c>, DEC-T01).</summary>
        public static ThermalInputsSnapshot Default { get; } =
            new(OperatingMode.Melting, 50.0, 10.0, null, 200);

        public OperatingMode Mode { get; }
        public double SupplyTemperature { get; }
        public double GroundTemperature { get; }
        public ThermalPipeSnapshot? Pipe { get; }
        public int PipeSpacing { get; }

        public ThermalInputsSnapshot(
            OperatingMode mode,
            double supplyTemperature,
            double groundTemperature,
            ThermalPipeSnapshot? pipe,
            int pipeSpacing)
        {
            Mode = mode;
            SupplyTemperature = supplyTemperature;
            GroundTemperature = groundTemperature;
            Pipe = pipe;
            PipeSpacing = pipeSpacing;
        }

        public bool Equals(ThermalInputsSnapshot? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Mode.Equals(other.Mode)
                && SupplyTemperature.Equals(other.SupplyTemperature)
                && GroundTemperature.Equals(other.GroundTemperature)
                && (Pipe is null ? other.Pipe is null : Pipe.Equals(other.Pipe))
                && PipeSpacing.Equals(other.PipeSpacing);
        }

        public override bool Equals(object? obj) => obj is ThermalInputsSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Mode);
            hash.Add(SupplyTemperature);
            hash.Add(GroundTemperature);
            if (Pipe is not null)
            {
                hash.Add(Pipe);
            }

            hash.Add(PipeSpacing);
            return hash.ToHashCode();
        }

        public static bool operator ==(ThermalInputsSnapshot? left, ThermalInputsSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ThermalInputsSnapshot? left, ThermalInputsSnapshot? right)
            => !(left == right);
    }

    /// <summary>
    /// Неизменяемый структурный снимок статуса теплового модуля.
    /// </summary>
    public sealed class ThermalStatusSnapshot : IEquatable<ThermalStatusSnapshot>
    {
        /// <summary>Дефолтный статус: фаза Actual, оба сообщения пустые (DEC-T01).</summary>
        public static ThermalStatusSnapshot Default { get; } =
            new(ThermalCalculationPhase.Actual, string.Empty, string.Empty);

        public ThermalCalculationPhase Phase { get; }
        public string RecalculationMessage { get; }
        public string ValidationMessage { get; }

        public ThermalStatusSnapshot(
            ThermalCalculationPhase phase,
            string? recalculationMessage,
            string? validationMessage)
        {
            Phase = phase;
            RecalculationMessage = recalculationMessage ?? string.Empty;
            ValidationMessage = validationMessage ?? string.Empty;
        }

        public bool Equals(ThermalStatusSnapshot? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Phase.Equals(other.Phase)
                && string.Equals(RecalculationMessage, other.RecalculationMessage, StringComparison.Ordinal)
                && string.Equals(ValidationMessage, other.ValidationMessage, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => obj is ThermalStatusSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Phase);
            hash.Add(RecalculationMessage, StringComparer.Ordinal);
            hash.Add(ValidationMessage, StringComparer.Ordinal);
            return hash.ToHashCode();
        }

        public static bool operator ==(ThermalStatusSnapshot? left, ThermalStatusSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ThermalStatusSnapshot? left, ThermalStatusSnapshot? right)
            => !(left == right);
    }

    /// <summary>
    /// Полный непротиворечивый срез канонического теплового состояния проекта.
    /// Equality — композиция поэлементного равенства компонентов.
    /// </summary>
    public sealed class ThermalStateSnapshot : IEquatable<ThermalStateSnapshot>
    {
        /// <summary>Точный дефолтный срез состояния (DEC-T01).</summary>
        public static ThermalStateSnapshot Default { get; } =
            new(ThermalInputsSnapshot.Default, null, ThermalStatusSnapshot.Default);

        public ThermalInputsSnapshot Inputs { get; }
        public ThermalResultSnapshot? Result { get; }
        public ThermalStatusSnapshot Status { get; }

        public ThermalStateSnapshot(
            ThermalInputsSnapshot inputs,
            ThermalResultSnapshot? result,
            ThermalStatusSnapshot status)
        {
            Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            Result = result;
            Status = status ?? throw new ArgumentNullException(nameof(status));
        }

        public bool Equals(ThermalStateSnapshot? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Inputs.Equals(other.Inputs)
                && (Result is null ? other.Result is null : Result.Equals(other.Result))
                && Status.Equals(other.Status);
        }

        public override bool Equals(object? obj) => obj is ThermalStateSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Inputs);
            if (Result is not null)
            {
                hash.Add(Result);
            }

            hash.Add(Status);
            return hash.ToHashCode();
        }

        public static bool operator ==(ThermalStateSnapshot? left, ThermalStateSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ThermalStateSnapshot? left, ThermalStateSnapshot? right)
            => !(left == right);
    }
}
