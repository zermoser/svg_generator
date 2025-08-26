using TimelineChartAPI.Models;
using System.Threading.Tasks;

namespace TimelineChartAPI.Services
{
    public interface ITimelineChartService
    {
        string GenerateSvg(TimelineChartRequest request);
        Task<byte[]> GeneratePngAsync(TimelineChartRequest request);
    }
}
