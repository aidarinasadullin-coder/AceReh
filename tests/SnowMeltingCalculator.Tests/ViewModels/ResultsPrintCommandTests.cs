using System.ComponentModel;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Printing;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Команда печати ResultsViewModel на новом IPrintService (волна 2
    /// hardening-роадмапа, решения владельца D1/D8): гейт готовности,
    /// подтверждение печати, фолбэк «Открыть PDF» при отказе печати.
    /// Статусы ловятся подпиской на PropertyChanged: команда в конце
    /// цикла очищает StatusMessage автосбросом, поэтому ассерт по
    /// значению после await команды был бы всегда пустым. Ветку
    /// удачного shell-старта печатает PrintServiceTests — здесь
    /// склейка сообщений и порядка вызовов на fake-сервисе.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ResultsPrintCommandTests
    {
        /// <summary>Fake IPrintService с заранее заданными результатами.</summary>
        private sealed class FakePrintService : IPrintService
        {
            private readonly PrintResult[] _printResults;
            private readonly PrintResult[] _openResults;
            private int _printIndex;
            private int _openIndex;

            public FakePrintService(PrintResult[] printResults, PrintResult[] openResults)
            {
                _printResults = printResults;
                _openResults = openResults;
            }

            public int PrintCalls { get; private set; }

            public int OpenCalls { get; private set; }

            public PrintResult PrintPdf(string pdfPath)
            {
                PrintCalls++;
                return _printResults[_printIndex++ % _printResults.Length];
            }

            public PrintResult OpenPdf(string pdfPath)
            {
                OpenCalls++;
                return _openResults[_openIndex++ % _openResults.Length];
            }
        }

        private static async Task<List<string>> CaptureStatusMessagesWhileAsync(
            ResultsViewModel viewModel, Func<Task> action)
        {
            var statuses = new List<string>();
            void Handler(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ResultsViewModel.StatusMessage)
                    && !string.IsNullOrEmpty(viewModel.StatusMessage))
                {
                    statuses.Add(viewModel.StatusMessage);
                }
            }

            viewModel.PropertyChanged += Handler;
            try
            {
                await action();
            }
            finally
            {
                viewModel.PropertyChanged -= Handler;
            }

            return statuses;
        }

        private static async Task<ResultsViewModel> CreateReadyViewModelAsync(
            FakePrintService printService,
            bool printDialogAccepted)
        {
            var projectStateService = new ProjectStateService();
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2));
            var dialogServiceMock = new Mock<IDialogService>();
            dialogServiceMock.Setup(d => d.ShowPrintDialog()).Returns(printDialogAccepted);
            var pdfExportMock = new Mock<IPdfExportService>();
            pdfExportMock
                .Setup(s => s.ExportResultsToPdfAsync(
                    It.IsAny<string>(), It.IsAny<ResultsPdfData>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(true);
            var projectFileServiceMock = new Mock<IProjectFileService>();
            projectFileServiceMock.Setup(f => f.GetPreviewPdfPath()).Returns(@"C:\temp\preview.pdf");

            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(
                projectStateService,
                circuitsVm,
                out _,
                out _,
                out _,
                printService: printService,
                dialogService: dialogServiceMock.Object,
                pdfExportService: pdfExportMock.Object,
                projectFileServiceOverride: projectFileServiceMock.Object);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);

            projectStateService.Session.HydraulicsState.ReplaceCollectors(new[]
            {
                new HydraulicCollectorSnapshot(
                    1, "HKV-D (2-12 контуров)", ValveType.HKV_D,
                    new[] { new HydraulicCircuitSnapshot(1, 50, 10, 5, 10, 20) },
                    new HydraulicCollectorSummarySnapshot(1, 60, 5000, 720, 24000, 22000, 2.2, "HKV-D"))
            }, HydraulicsMutationOrigin.Calculation);
            return viewModel;
        }

        [Test]
        public async Task PrintPdf_WhenModulesNotReady_ShowsGateMessageWithoutPrinting()
        {
            var printService = new FakePrintService(
                new[] { PrintResult.Ok() }, new[] { PrintResult.Ok() });
            var projectStateService = new ProjectStateService();
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2));
            var dialogServiceMock = new Mock<IDialogService>();
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(
                projectStateService,
                circuitsVm,
                out _,
                out _,
                out _,
                printService: printService,
                dialogService: dialogServiceMock.Object);

            var statuses = await CaptureStatusMessagesWhileAsync(
                viewModel, () => viewModel.PrintPdfCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsDataReady, Is.False, "guard: свежий проект не готов");
                Assert.That(printService.PrintCalls, Is.Zero);
                Assert.That(printService.OpenCalls, Is.Zero);
                dialogServiceMock.Verify(d => d.ShowPrintDialog(), Times.Never);
                Assert.That(statuses, Does.Contain("Невозможно напечатать: не все данные готовы"));
            });
        }

        [Test]
        public async Task PrintPdf_WhenPrintSucceeds_SendsOnceWithoutFallback()
        {
            var printService = new FakePrintService(
                new[] { PrintResult.Ok() }, new[] { PrintResult.Ok() });
            var viewModel = await CreateReadyViewModelAsync(printService, printDialogAccepted: true);
            Assert.That(viewModel.IsDataReady, Is.True, "guard: модули доведены до готовности");

            var statuses = await CaptureStatusMessagesWhileAsync(
                viewModel, () => viewModel.PrintPdfCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(printService.PrintCalls, Is.EqualTo(1));
                Assert.That(printService.OpenCalls, Is.Zero,
                    "Успешная печать не должна открывать PDF-фолбэк.");
                Assert.That(statuses, Does.Contain("Документ отправлен на печать"));
                Assert.That(statuses, Does.Not.Contain("Печать отменена"));
            });
        }

        [Test]
        public async Task PrintPdf_WhenPrintFails_OpensPdfAsFallback()
        {
            var printService = new FakePrintService(
                new[] { PrintResult.Fail("принтер не найден") },
                new[] { PrintResult.Ok() });
            var viewModel = await CreateReadyViewModelAsync(printService, printDialogAccepted: true);

            var statuses = await CaptureStatusMessagesWhileAsync(
                viewModel, () => viewModel.PrintPdfCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(printService.PrintCalls, Is.EqualTo(1));
                Assert.That(printService.OpenCalls, Is.EqualTo(1),
                    "D8: отказ печати открывает готовый PDF в системном просмотрщике.");
                Assert.That(statuses, Does.Contain("Печать не удалась: принтер не найден — PDF открыт в просмотрщике"));
            });
        }

        [Test]
        public async Task PrintPdf_WhenPrintAndOpenBothFail_ShowsPrintError()
        {
            var printService = new FakePrintService(
                new[] { PrintResult.Fail("принтер не найден") },
                new[] { PrintResult.Fail("нет ассоциированного приложения") });
            var viewModel = await CreateReadyViewModelAsync(printService, printDialogAccepted: true);

            var statuses = await CaptureStatusMessagesWhileAsync(
                viewModel, () => viewModel.PrintPdfCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(printService.PrintCalls, Is.EqualTo(1));
                Assert.That(printService.OpenCalls, Is.EqualTo(1));
                Assert.That(statuses, Does.Contain("Ошибка печати: принтер не найден"));
            });
        }

        [Test]
        public async Task PrintPdf_WhenPrintDialogDeclined_DoesNotPrint()
        {
            var printService = new FakePrintService(
                new[] { PrintResult.Ok() }, new[] { PrintResult.Ok() });
            var viewModel = await CreateReadyViewModelAsync(printService, printDialogAccepted: false);

            var statuses = await CaptureStatusMessagesWhileAsync(
                viewModel, () => viewModel.PrintPdfCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(printService.PrintCalls, Is.Zero);
                Assert.That(printService.OpenCalls, Is.Zero);
                Assert.That(statuses, Does.Contain("Печать отменена"));
            });
        }
    }
}
