using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccommodationController : ControllerBase
    {
        private readonly IAccommodationService _accommodationService;
        private readonly IUserContextService _userContextService;   
        public AccommodationController(IAccommodationService accommodationService, IUserContextService userContextService)
        {
            _accommodationService = accommodationService;
            _userContextService = userContextService;
        }

       
    }
}
