using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Единственный writable owner состояния жизненного цикла проекта:
    /// идентификация, путь, dirty-флаг и guard восстановления.
    /// </summary>
    public class ProjectSession : IProjectSession, IProjectStateService, IMarkDirtyService
    {
        private string _projectNumber = string.Empty;
        private string _projectObject = string.Empty;
        private string? _currentFilePath;
        private bool _isDirty;
        private bool _isLoadProjectInProgress;
        private int _restoreDepth;
        private readonly ProjectSessionClimateState _climateState;

        /// <inheritdoc />
        public IProjectSessionClimateState ClimateState => _climateState;

        public ProjectSession(IClimateData? climateData = null, CalculationContext? calculationContext = null)
        {
            _climateState = new ProjectSessionClimateState(this, climateData, calculationContext);
        }

        /// <inheritdoc />
        public string ProjectNumber
        {
            get => _projectNumber;
            set
            {
                ThrowIfNull(value, nameof(ProjectNumber));
                SetProperty(ref _projectNumber, value, nameof(ProjectNumber));
            }
        }

        /// <inheritdoc />
        public string ProjectObject
        {
            get => _projectObject;
            set
            {
                ThrowIfNull(value, nameof(ProjectObject));
                SetProperty(ref _projectObject, value, nameof(ProjectObject));
            }
        }

        /// <inheritdoc />
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value, nameof(CurrentFilePath));
        }

        /// <inheritdoc />
        public bool IsDirty => _isDirty;

        /// <inheritdoc />
        public bool IsLoadProjectInProgress => _isLoadProjectInProgress;

        /// <inheritdoc />
        public void MarkDirty()
        {
            if (_isDirty)
            {
                return;
            }

            _isDirty = true;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <inheritdoc />
        public void MarkClean()
        {
            if (!_isDirty)
            {
                return;
            }

            _isDirty = false;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <inheritdoc />
        public IDisposable BeginProjectRestore()
        {
            if (_restoreDepth == int.MaxValue)
            {
                throw new InvalidOperationException("Restore nesting depth exceeded.");
            }

            if (_restoreDepth == 0)
            {
                _restoreDepth = 1;
                _isLoadProjectInProgress = true;

                try
                {
                    OnPropertyChanged(nameof(IsLoadProjectInProgress));
                }
                catch
                {
                    _restoreDepth = 0;
                    _isLoadProjectInProgress = false;
                    throw;
                }
            }
            else
            {
                _restoreDepth++;
            }

            return new ProjectRestoreLease(this);
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Вызвать событие изменения свойства.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void ThrowIfNull(string? value, string paramName)
        {
            if (value is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void EndRestore()
        {
            if (_restoreDepth <= 0)
            {
                return;
            }

            _restoreDepth--;

            if (_restoreDepth == 0)
            {
                _isLoadProjectInProgress = false;
                OnPropertyChanged(nameof(IsLoadProjectInProgress));
            }
        }

        private sealed class ProjectRestoreLease : IDisposable
        {
            private readonly ProjectSession _session;
            private int _disposed;

            public ProjectRestoreLease(ProjectSession session)
            {
                _session = session;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _session.EndRestore();
                }
            }
        }
    }
}
