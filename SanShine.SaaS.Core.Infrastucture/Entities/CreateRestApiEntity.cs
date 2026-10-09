using SanShine.SaaS.Core.Infrastucture.DbManagerRepository;
using SanShine.SaaS.Core.Infrastucture.Entities.BaseEntities;

namespace SanShine.SaaS.Core.Infrastucture.Entities
{
    public class CreateRestApiEntity : BaseEntity
    {
        [DbManager.sColumn("id")]
        public string Id { get; set; }

        [DbManager.sColumn("username")]
        public string Username { get; set; }

        [DbManager.sColumn("status")]
        public string Status { get; set; }

        [DbManager.sColumn("company")]
        public string Company { get; set; }

        [DbManager.sColumn("branch")]
        public string Branch { get; set; }
    }
}
