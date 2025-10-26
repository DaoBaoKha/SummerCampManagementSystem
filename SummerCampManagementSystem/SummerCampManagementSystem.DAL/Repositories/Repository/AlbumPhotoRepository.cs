using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class AlbumPhotoRepository : GenericRepository<AlbumPhoto>, IAlbumPhotoRepository
    {
        public AlbumPhotoRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
