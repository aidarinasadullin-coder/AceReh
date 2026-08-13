using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectSessionTests
    {
        private IProjectSession _session = null!;

        [SetUp]
        public void Setup()
        {
            _session = new ProjectSession();
        }

        #region Initial state

        [Test]
        public void InitialState_HasExpectedDefaults()
        {
            Assert.That(_session.ProjectNumber, Is.EqualTo(string.Empty));
            Assert.That(_session.ProjectObject, Is.EqualTo(string.Empty));
            Assert.That(_session.CurrentFilePath, Is.Null);
            Assert.That(_session.IsDirty, Is.False);
            Assert.That(_session.IsLoadProjectInProgress, Is.False);
        }

        [Test]
        public void ProjectSession_ClimateState_ReturnsStableCanonicalOwner()
        {
            var first = _session.ClimateState;
            var second = _session.ClimateState;

            Assert.That(first, Is.SameAs(second));

            first.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.AirTemperature, -12.5),
                ClimateMutationOrigin.User);

            Assert.That(second.Snapshot.AirTemperature, Is.EqualTo(-12.5));
        }

        #endregion

        #region ProjectNumber

        [Test]
        public void ProjectNumber_Setter_RaisesPropertyChanged_Once()
        {
            var changed = CapturePropertyChanges();

            _session.ProjectNumber = "PN-001";

            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.ProjectNumber) }));
        }

        [Test]
        public void ProjectNumber_Setter_DoesNotRaisePropertyChanged_WhenValueUnchanged()
        {
            _session.ProjectNumber = "PN-001";
            var changed = CapturePropertyChanges();

            _session.ProjectNumber = "PN-001";

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void ProjectNumber_Setter_ThrowsArgumentNullException_ForNull()
        {
            Assert.That(() => _session.ProjectNumber = null!, Throws.ArgumentNullException);
            Assert.That(_session.ProjectNumber, Is.EqualTo(string.Empty));
        }

        #endregion

        #region ProjectObject

        [Test]
        public void ProjectObject_Setter_RaisesPropertyChanged_Once()
        {
            var changed = CapturePropertyChanges();

            _session.ProjectObject = "Object A";

            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.ProjectObject) }));
        }

        [Test]
        public void ProjectObject_Setter_DoesNotRaisePropertyChanged_WhenValueUnchanged()
        {
            _session.ProjectObject = "Object A";
            var changed = CapturePropertyChanges();

            _session.ProjectObject = "Object A";

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void ProjectObject_Setter_ThrowsArgumentNullException_ForNull()
        {
            Assert.That(() => _session.ProjectObject = null!, Throws.ArgumentNullException);
            Assert.That(_session.ProjectObject, Is.EqualTo(string.Empty));
        }

        #endregion

        #region CurrentFilePath

        [Test]
        public void CurrentFilePath_Setter_RaisesPropertyChanged_Once()
        {
            var changed = CapturePropertyChanges();

            _session.CurrentFilePath = "C:\\project.smc";

            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.CurrentFilePath) }));
        }

        [Test]
        public void CurrentFilePath_Setter_DoesNotRaisePropertyChanged_WhenValueUnchanged()
        {
            _session.CurrentFilePath = "C:\\project.smc";
            var changed = CapturePropertyChanges();

            _session.CurrentFilePath = "C:\\project.smc";

            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void CurrentFilePath_Setter_AcceptsNull()
        {
            _session.CurrentFilePath = "C:\\project.smc";
            var changed = CapturePropertyChanges();

            _session.CurrentFilePath = null;

            Assert.That(_session.CurrentFilePath, Is.Null);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.CurrentFilePath) }));
        }

        #endregion

        #region Dirty state

        [Test]
        public void MarkDirty_SetsIsDirtyTrue_And_RaisesPropertyChanged_Once()
        {
            var changed = CapturePropertyChanges();

            _session.MarkDirty();

            Assert.That(_session.IsDirty, Is.True);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsDirty) }));
        }

        [Test]
        public void MarkDirty_WhenAlreadyDirty_IsIdempotent()
        {
            _session.MarkDirty();
            var changed = CapturePropertyChanges();

            _session.MarkDirty();

            Assert.That(_session.IsDirty, Is.True);
            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void MarkClean_SetsIsDirtyFalse_And_RaisesPropertyChanged_Once()
        {
            _session.MarkDirty();
            var changed = CapturePropertyChanges();

            _session.MarkClean();

            Assert.That(_session.IsDirty, Is.False);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsDirty) }));
        }

        [Test]
        public void MarkClean_WhenAlreadyClean_IsIdempotent()
        {
            var changed = CapturePropertyChanges();

            _session.MarkClean();

            Assert.That(_session.IsDirty, Is.False);
            Assert.That(changed, Is.Empty);
        }

        #endregion

        #region Restore guard

        [Test]
        public void BeginProjectRestore_SetsIsLoadProjectInProgressTrue_And_RaisesPropertyChanged_Once()
        {
            var changed = CapturePropertyChanges();

            using var scope = _session.BeginProjectRestore();

            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsLoadProjectInProgress) }));
        }

        [Test]
        public void BeginProjectRestore_Nested_DoesNotRaiseEventForSecondEntry()
        {
            using var outer = _session.BeginProjectRestore();
            var changed = CapturePropertyChanges();

            using var inner = _session.BeginProjectRestore();

            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(changed, Is.Empty);
        }

        [Test]
        public void BeginProjectRestore_NestedLeases_DisposeInnerThenOuter_PreservesGuardUntilFinalExit()
        {
            var changed = CapturePropertyChanges();
            var outer = _session.BeginProjectRestore();
            var inner = _session.BeginProjectRestore();

            Assert.That(inner, Is.Not.SameAs(outer));
            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsLoadProjectInProgress) }));

            inner.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsLoadProjectInProgress) }));

            outer.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(changed, Is.EqualTo(new[]
            {
                nameof(IProjectSession.IsLoadProjectInProgress),
                nameof(IProjectSession.IsLoadProjectInProgress)
            }));

            inner.Dispose();
            outer.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(changed, Has.Count.EqualTo(2));
        }

        [Test]
        public void BeginProjectRestore_NestedLeases_DisposeOuterThenInner_PreservesGuardUntilFinalExit()
        {
            var changed = CapturePropertyChanges();
            var outer = _session.BeginProjectRestore();
            var inner = _session.BeginProjectRestore();

            outer.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(changed, Has.Count.EqualTo(1));

            inner.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(changed, Has.Count.EqualTo(2));

            outer.Dispose();
            inner.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(changed, Has.Count.EqualTo(2));
        }

        [Test]
        public void BeginProjectRestore_NestedLeases_FinalExitSubscriberThrows_ClearsGuardAndKeepsLeasesIdempotent()
        {
            var outer = _session.BeginProjectRestore();
            var inner = _session.BeginProjectRestore();
            var exitEvents = 0;
            _session.PropertyChanged += (_, eventArgs) =>
            {
                if (eventArgs.PropertyName == nameof(IProjectSession.IsLoadProjectInProgress) && !_session.IsLoadProjectInProgress)
                {
                    exitEvents++;
                    throw new InvalidOperationException("Exit subscriber failure.");
                }
            };

            inner.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.True);
            Assert.That(exitEvents, Is.EqualTo(0));
            Assert.That(() => outer.Dispose(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(exitEvents, Is.EqualTo(1));

            outer.Dispose();
            inner.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(exitEvents, Is.EqualTo(1));
        }

        [Test]
        public void BeginProjectRestore_DisposeOuter_ClearsGuard_And_RaisesPropertyChanged_Once()
        {
            var scope = _session.BeginProjectRestore();
            var changed = CapturePropertyChanges();

            scope.Dispose();

            Assert.That(_session.IsLoadProjectInProgress, Is.False);
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsLoadProjectInProgress) }));

            scope.Dispose();
            Assert.That(changed, Is.EqualTo(new[] { nameof(IProjectSession.IsLoadProjectInProgress) }));
        }

        #endregion

        #region DI / compatibility

        [Test]
        public void DependencyInjection_ResolvesSameCanonicalInstance_ForAllLifecycleInterfaces()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            var provider = services.BuildServiceProvider();
            var session = provider.GetRequiredService<IProjectSession>();
            var info = provider.GetRequiredService<IProjectInfoService>();
            var state = provider.GetRequiredService<IProjectStateService>();
            var markDirty = provider.GetRequiredService<IMarkDirtyService>();

            Assert.That(info, Is.SameAs(session));
            Assert.That(state, Is.SameAs(session));
            Assert.That(markDirty, Is.SameAs(session));
        }

        [Test]
        public void LegacyInterfaces_ObserveCanonicalStateChanges()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            var provider = services.BuildServiceProvider();
            var session = provider.GetRequiredService<IProjectSession>();
            var state = provider.GetRequiredService<IProjectStateService>();
            var info = provider.GetRequiredService<IProjectInfoService>();

            state.CurrentFilePath = "C:\\via-state.smc";
            session.MarkDirty();
            info.ProjectNumber = "PN-LEGACY";

            Assert.That(session.CurrentFilePath, Is.EqualTo("C:\\via-state.smc"));
            Assert.That(session.IsDirty, Is.True);
            Assert.That(session.ProjectNumber, Is.EqualTo("PN-LEGACY"));
        }

        [Test]
        public void DependencyInjection_LifecycleConsumersShareCanonicalSession()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            var provider = services.BuildServiceProvider();
            var session = provider.GetRequiredService<IProjectSession>();
            var resultsVm = provider.GetRequiredService<ResultsViewModel>();
            var calcState = provider.GetRequiredService<ICalculationStateService>();

            var resultsVmSession = GetField<IProjectSession>(resultsVm, "_projectSession");
            var calcStateSession = GetField<IProjectSession>(calcState, "_projectSession");

            Assert.That(resultsVmSession, Is.SameAs(session));
            Assert.That(calcStateSession, Is.SameAs(session));
        }

        private static T GetField<T>(object instance, string fieldName) where T : class
        {
            var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)(field?.GetValue(instance) ?? throw new InvalidOperationException($"Field {fieldName} not found."));
        }

        #endregion

        private List<string> CapturePropertyChanges()
        {
            var changed = new List<string>();
            _session.PropertyChanged += (sender, e) => changed.Add(e.PropertyName!);
            return changed;
        }
    }
}
