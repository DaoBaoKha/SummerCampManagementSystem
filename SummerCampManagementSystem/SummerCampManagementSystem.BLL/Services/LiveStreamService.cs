using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.DTOs.Livestream;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class LiveStreamService : ILiveStreamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public LiveStreamService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LivestreamResponseDto> CreateLiveStreamAsync(LivestreamRequestDto dto, int hostId)
        {
            var liveStream = _mapper.Map<Livestream>(dto);
            liveStream.hostId = hostId;
            await _unitOfWork.LiveStreams.CreateAsync(liveStream);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<LivestreamResponseDto>(liveStream);

        }

        public async Task<bool> DeleteLiveStreamAsync(int liveStreamId)
        {
            var liveStream =  await _unitOfWork.LiveStreams.GetByIdAsync(liveStreamId);
            if (liveStream == null) return false;
            await _unitOfWork.LiveStreams.RemoveAsync(liveStream);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<LivestreamResponseDto>> GetAllLiveStreamsAsync()
        {
            var liveStreams =  await _unitOfWork.LiveStreams.GetAllAsync();
            return _mapper.Map<IEnumerable<LivestreamResponseDto>>(liveStreams);
        }

        public async Task<LivestreamResponseDto?> GetLiveStreamByIdAsync(int liveStreamId)
        {
            var liveStream =  await _unitOfWork.LiveStreams.GetByIdAsync(liveStreamId);
            return liveStream == null ? null : _mapper.Map<LivestreamResponseDto>(liveStream);
        }

        public async Task<LivestreamResponseDto?> UpdateLiveStreamAsync(int liveStreamId, LivestreamRequestDto dto)
        {
            var liveStream =  await _unitOfWork.LiveStreams.GetByIdAsync(liveStreamId);

            if (liveStream == null) return null;

            var oldHostId = liveStream.hostId;
            _mapper.Map(dto, liveStream);
            liveStream.hostId = oldHostId;

            await _unitOfWork.LiveStreams.UpdateAsync(liveStream);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<LivestreamResponseDto>(liveStream);
        }

        public async Task<IEnumerable<LivestreamResponseDto>> GetLiveStreamsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var fromUtc = fromDate.ToUtcForStorage();
            var toUtc = toDate.ToUtcForStorage();

            var livestreams = await _unitOfWork.LiveStreams.GetLiveStreamsByDateRange(fromUtc, toUtc);
            return _mapper.Map<IEnumerable<LivestreamResponseDto>>(livestreams);
        }
    }
}
