using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Core.Results;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Application save boundary: assembles one immutable snapshot from the
    /// aggregate root, maps it once to the wire DTO and delegates file
    /// persistence exactly once. Dates are explicit save-operation inputs;
    /// the service owns no lifecycle, dirty, path or UI state.
    /// </summary>
    public interface IProjectSaveService
    {
        /// <summary>
        /// Save the project: assemble one snapshot from canonical state, map
        /// it to <c>ProjectData</c> and persist it through the file service.
        /// </summary>
        /// <param name="projectSession">Aggregate project root.</param>
        /// <param name="filePath">Target file path.</param>
        /// <param name="dates">Explicit save-operation dates.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Detailed result of the save operation.</returns>
        Task<OperationResult<object?>> SaveAsync(
            IProjectSession projectSession,
            string filePath,
            ProjectSaveDates dates,
            CancellationToken cancellationToken = default);
    }
}
