using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;

namespace SanShine.SaaS.Core.Infrastucture.Entities.BaseEntities
{
    public class BaseEntity
    {
        [DbManager.sColumn("created_at")]
        public DateTime CreatedAt { get; set; }

        [DbManager.sColumn("created_by")]
        public int CreatedBy { get; set; }

        [DbManager.sColumn("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [DbManager.sColumn("updated_by")]
        public int UpdatedBy { get; set; }
    }
}
