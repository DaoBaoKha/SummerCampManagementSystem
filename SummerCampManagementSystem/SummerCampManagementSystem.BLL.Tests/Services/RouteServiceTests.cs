using AutoMapper;
using Moq;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace SummerCampManagementSystem.BLL.Tests.Services
{
    public class RouteServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly RouteService _routeService;
        private readonly IConfigurationProvider _realMapperConfiguration;

        public RouteServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            // --- setup automapper config ---
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Route, RouteResponseDto>()
                   .ForMember(dest => dest.CampName, opt => opt.MapFrom(src => src.camp.name)); 
                cfg.CreateMap<RouteRequestDto, Route>();
            });
            _realMapperConfiguration = config;
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(_realMapperConfiguration);

            _routeService = new RouteService(
                _mockUnitOfWork.Object,
                _mockMapper.Object
            );
        }

        // UT06 - TEST CASE 1: Camp not found
        [Fact]
        public async Task CreateRoute_CampNotFound_ThrowsKeyNotFound()
        {
            // arrange
            var request = new RouteRequestDto { campId = 99, routeName = "Route 1", routeType = "Bus" };

            // mock camp not found
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(99))
                .ReturnsAsync((Camp?)null);

            // act & assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _routeService.CreateRouteAsync(request));
            Assert.Contains("Camp with ID 99 not found", ex.Message);
        }

        // UT06 - TEST CASE 2: Successful creation
        [Fact]
        public async Task CreateRoute_ValidData_ReturnsSuccess()
        {
            // arrange
            var request = new RouteRequestDto { campId = 1, routeName = "Route A", routeType = "Bus" };

            // mock camp exists
            _mockUnitOfWork.Setup(u => u.Camps.GetByIdAsync(1))
                .ReturnsAsync(new Camp { campId = 1, name = "Summer Camp" });

            // setup mapper
            _mockMapper.Setup(m => m.Map<Route>(request)).Returns(new Route { campId = 1, routeName = "Route A" });

            _mockMapper.Setup(m => m.Map<RouteResponseDto>(It.IsAny<Route>()))
                .Returns(new RouteResponseDto { routeId = 10, routeName = "Route A", status = "Active" });

            // mock create behavior
            _mockUnitOfWork.Setup(u => u.Routes.CreateAsync(It.IsAny<Route>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _routeService.CreateRouteAsync(request);

            // assert
            Assert.NotNull(result);
            Assert.Equal("Route A", result.routeName);

            _mockUnitOfWork.Verify(u => u.Routes.CreateAsync(It.IsAny<Route>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}