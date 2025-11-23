using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationCamperController : ControllerBase
    {
        private readonly IRegistrationCamperService _registrationCamperService;
        public RegistrationCamperController(IRegistrationCamperService registrationCamperService)
        {
            _registrationCamperService = registrationCamperService;
        }

        /// <summary>
        /// get list or search registration campers
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RegistrationCamperResponseDto>>> Get([FromQuery] RegistrationCamperSearchDto searchDto)
        {
            if(searchDto.CamperId.HasValue || searchDto.CampId.HasValue || !string.IsNullOrEmpty(searchDto.Status.ToString()))
            {
                var result = await _registrationCamperService.SearchRegistrationCampersAsync(searchDto);
                return Ok(result);
            }
            else 
            {
                var result = await _registrationCamperService.GetAllRegistrationCampersAsync();
                return Ok(result);
            }
        }
    }
}
