using FR.DataAccess.Context;
using DndMasterCover.DataAccess.Context;

namespace DndMasterCover.DataAccess.Repositories
{
    public class BaseRepository
    {
        protected IDatabaseContextFactory<DatabaseContext> ContextFactory { get; set; }

        protected BaseRepository(IDatabaseContextFactory<DatabaseContext> contextFactory)
        {
            ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }
    }
}