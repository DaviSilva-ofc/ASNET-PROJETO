using AspnetCoreStarter.Models;
using System.Collections.Generic;

namespace AspnetCoreStarter.Models
{
    public class ClientViewModel
    {
        public Agrupamento Agrupamento { get; set; }
        public string Abbreviation { get; set; }
        public string DirectorName { get; set; }
        public int DirectorUserId { get; set; }
        public int TicketCount { get; set; }
        public int ContractCount { get; set; }
        public List<SchoolViewModel> Schools { get; set; } = new();
    }

    public class SchoolViewModel
    {
        public AspnetCoreStarter.Models.School School { get; set; }
        public string CoordinatorName { get; set; }
        public int? CoordinatorUserId { get; set; }
        public List<Bloco> Blocos { get; set; } = new();
    }
}
