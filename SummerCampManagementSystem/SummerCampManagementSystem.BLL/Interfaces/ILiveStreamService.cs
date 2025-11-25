using SummerCampManagementSystem.BLL.DTOs.Livestream;
using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ILiveStreamService
    {
        Task <IEnumerable<LivestreamResponseDto>> GetAllLiveStreamsAsync();
        Task<LivestreamResponseDto?> GetLiveStreamByIdAsync(int liveStreamId);
        Task<LivestreamResponseDto> CreateLiveStreamAsync(LivestreamRequestDto dto, int hostId);
        Task<LivestreamResponseDto?> UpdateLiveStreamAsync(int liveStreamId, LivestreamRequestDto dto);
        Task<bool> DeleteLiveStreamAsync(int liveStreamId);
        Task<IEnumerable<LivestreamResponseDto>> GetLiveStreamsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }
}
