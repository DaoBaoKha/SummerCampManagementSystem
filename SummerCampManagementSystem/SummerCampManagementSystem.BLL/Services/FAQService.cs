using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.FAQ;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class FAQService : IFAQService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FAQService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FAQResponseDto> CreateFAQAsync(FAQRequestDto dto)
        {
            var faq = _mapper.Map<FAQ>(dto);
            
            await _unitOfWork.FAQs.CreateAsync(faq);
            await _unitOfWork.CommitAsync();
            
            return _mapper.Map<FAQResponseDto>(faq);
        }

        public async Task<bool> DeleteFAQAsync(int faqId)
        {
            var faq = await _unitOfWork.FAQs.GetByIdAsync(faqId);
            
            if (faq == null) return false;
            
            await _unitOfWork.FAQs.RemoveAsync(faq);
            await _unitOfWork.CommitAsync();
            
            return true;
        }

        public async Task<IEnumerable<FAQResponseDto>> GetAllFAQsAsync()
        {
            var faqs = await _unitOfWork.FAQs.GetAllAsync();
            return _mapper.Map<IEnumerable<FAQResponseDto>>(faqs);
        }

        public async Task<FAQResponseDto?> GetFAQByIdAsync(int faqId)
        {
            var faq = await _unitOfWork.FAQs.GetByIdAsync(faqId);
            return faq == null ? null : _mapper.Map<FAQResponseDto>(faq);
        }

        public async Task<FAQResponseDto?> UpdateFAQAsync(int faqId, FAQRequestDto dto)
        {
            var faq = await _unitOfWork.FAQs.GetByIdAsync(faqId);
            
            if (faq == null) return null;
            
            _mapper.Map(dto, faq);
            
            await _unitOfWork.FAQs.UpdateAsync(faq);
            await _unitOfWork.CommitAsync();
            
            return _mapper.Map<FAQResponseDto>(faq);
        }
    }
}
