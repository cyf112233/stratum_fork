using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Persistence
{
    public class CustomIconRepository : AsyncRepository<CustomIcon, string>, ICustomIconRepository
    {
        public CustomIconRepository(Database database) : base(database)
        {
        }
    }
}
