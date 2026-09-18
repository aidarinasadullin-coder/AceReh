using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Services.History;
using SnowMeltingCalculator.Tests.ViewModels;

namespace SnowMeltingCalculator.Tests.Configuration
{
    /// <summary>
    /// Пин eager-захвата UI-диспетчера в композиции (P19, волна 2
    /// hardening-роадмапа): диспетчер undo-дневника фиксируется в точке
    /// построения провайдера, а не при первом Resolve — иначе resolve
    /// с фонового потока привязал бы DispatcherTimer дневника к чужому
    /// диспетчеру.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class CompositionDispatcherTests
    {
        [Test]
        public void UndoRedoService_FactoryResolvedFromBackgroundThread_UsesDispatcherCapturedAtComposition()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            using var provider = services.BuildServiceProvider();

            // Тот же поток, на котором выполнялась композиция, — диспетчер,
            // захваченный AddResultsModule eagerly (Dispatcher.CurrentDispatcher
            // в тестовом потоке вернёт уже созданный им же инстанс).
            var expected = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            IUndoRedoService? resolved = null;
            var worker = new Thread(() =>
            {
                // Резолвим не весь граф (ViewModel'ы — UI-объекты), а фабрику
                // регистрации undo-дневника — ровно тот код, что раньше
                // вызывал Dispatcher.CurrentDispatcher из лямбды.
                var descriptor = services.Single(
                    d => d.ServiceType == typeof(IUndoRedoService));
                resolved = (IUndoRedoService)descriptor.ImplementationFactory!(provider);
            });
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();
            worker.Join();

            var dispatcher = (System.Windows.Threading.Dispatcher?)resolved!
                .GetType()
                .GetField("_dispatcher", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(resolved);

            Assert.That(dispatcher, Is.EqualTo(expected),
                "Диспетчер undo-дневника должен захватываться в точке композиции: " +
                "первый Resolve с фонового потока при ленивом захвате привязал бы " +
                "таймер тишины к фоновому диспетчеру (P19).");
        }
    }
}
