using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace AspnetCoreStarter.Pages.Clients.Directors
{
    public class DirectorEquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public DirectorEquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; } = new();
        public List<StockEmpresa> MyBorrowedItems { get; set; } = new();
        public List<Ticket> MyStockRequests { get; set; } = new();
        public HashSet<int> EquipmentWithActiveTickets { get; set; } = new();

        [BindProperty]
        public Ticket NewTicket { get; set; } = new();

        [BindProperty]
        public Equipamento NewEquipment { get; set; } = new();

        [BindProperty]
        public string LocationType { get; set; } = "escola";

        public string SuccessMessage { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public AspnetCoreStarter.Models.School ActiveFilterEscola { get; set; }
        public Agrupamento ActiveFilterAgrupamento { get; set; }

        public List<Agrupamento> AvailableAgrupamentos { get; set; } = new();
        public List<AspnetCoreStarter.Models.School> AvailableEscolas { get; set; } = new();
        public List<Bloco> AvailableBlocos { get; set; } = new();
        public List<Empresa> AvailableEmpresas { get; set; } = new();
        public List<string> UniqueEquipmentNames { get; set; } = new();
        public List<string> UniqueBrands { get; set; } = new();
        public List<string> UniqueModels { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterBrand { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterModel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEscolaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterBlocoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEmpresaId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? success)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Auth/Login");

            // Find the director's agrupamento
            var director = await _context.Diretores.Include(d => d.User).FirstOrDefaultAsync(d => d.UserId == userId);
            if (director == null || director.AgrupamentoId == null)
            {
                Equipments = new List<Equipamento>();
                return Page();
            }

            int myAgrupamentoId = director.AgrupamentoId.Value;

            int? myEmpresaId = director.User?.EmpresaId;

            var queryFull = _context.Equipamentos
                .Include(e => e.StatusEquipamentos)
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .Include(e => e.Empresa)
                .Where(e => (e.Room != null && e.Room.Block.School.AgrupamentoId == myAgrupamentoId))
                .AsQueryable();

            var query = queryFull;

            // Apply Search Filters (Case-insensitive and robust)
            if (!string.IsNullOrEmpty(FilterName)) 
            {
                query = query.Where(e => e.Name == FilterName);
            }

            if (!string.IsNullOrEmpty(FilterBrand))
            {
                query = query.Where(e => e.Brand == FilterBrand);
            }

            if (!string.IsNullOrEmpty(FilterModel))
            {
                query = query.Where(e => e.Model == FilterModel);
            }

            if (!string.IsNullOrEmpty(FilterType)) 
            {
                var typeMatch = FilterType;
                var typeNoAccent = FilterType.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").ToLower();
                
                // Search in BOTH Type and Name to be highly inclusive
                // Allow matches in Name even if Type is NULL
                query = query.Where(e => (e.Type != null && (e.Type.Contains(typeMatch) || e.Type.ToLower().Contains(typeNoAccent))) || 
                                         (e.Name != null && e.Name.ToLower().Contains(typeNoAccent)));
            }

            if (!string.IsNullOrEmpty(FilterSerialNumber)) 
            {
                var snLower = FilterSerialNumber.ToLower();
                query = query.Where(e => e.SerialNumber != null && e.SerialNumber.ToLower().Contains(snLower));
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (FilterStatus == "Associado")
                {
                    query = query.Where(e => e.TicketId != null);
                }
                else if (FilterStatus == "A funcionar")
                {
                    query = query.Where(e => e.Status == "A funcionar" || e.Status == "Disponível" || e.Status == "Em uso" || string.IsNullOrEmpty(e.Status));
                }
                else if (FilterStatus == "Avariado")
                {
                    query = query.Where(e => e.Status == "Avariado" || e.Status == "Indisponível");
                }
                else if (FilterStatus == "Em reparação")
                {
                    query = query.Where(e => e.Status == "Em reparação");
                }
            }

            // Apply Location Filters (limited to their agrupamento)
            if (FilterRoomId.HasValue)
            {
                query = query.Where(e => e.RoomId == FilterRoomId.Value);
                ActiveFilterRoom = await _context.Salas.FindAsync(FilterRoomId.Value);
            }
            else if (FilterBlocoId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.BlockId == FilterBlocoId.Value);
                ActiveFilterBloco = await _context.Blocos.FindAsync(FilterBlocoId.Value);
            }
            else if (FilterEscolaId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.Block.SchoolId == FilterEscolaId.Value);
                ActiveFilterEscola = await _context.Schools.FindAsync(FilterEscolaId.Value);
            }

            Equipments = await query.ToListAsync();

            // Find equipment IDs that already have active tickets
            var activeTicketEqIds = await _context.Tickets
                .Where(t => t.EquipamentoId != null && t.Status != "Concluído")
                .Select(t => t.EquipamentoId.Value)
                .Distinct()
                .ToListAsync();
            EquipmentWithActiveTickets = new HashSet<int>(activeTicketEqIds);

            // Filter context-sensitive lists
            AvailableAgrupamentos = await _context.Agrupamentos.Where(a => a.Id == myAgrupamentoId).ToListAsync();
            AvailableEscolas = await _context.Schools.Where(s => s.AgrupamentoId == myAgrupamentoId).ToListAsync();
            var schoolIds = AvailableEscolas.Select(s => s.Id).ToList();
            
            AvailableBlocos = await _context.Blocos.Where(b => schoolIds.Contains(b.SchoolId)).ToListAsync();
            var blocoIds = AvailableBlocos.Select(b => b.Id).ToList();
            
            Rooms = await _context.Salas.Where(s => s.BlockId.HasValue && blocoIds.Contains(s.BlockId.Value)).ToListAsync();
            AvailableEmpresas = await _context.Empresas.ToListAsync();
            
            // Get unique values for filter dropdowns based on Agrupamento scope
            var scopedEquipments = await queryFull.ToListAsync();

            UniqueEquipmentNames = scopedEquipments.Select(e => e.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n).ToList();
            UniqueBrands = scopedEquipments.Select(e => e.Brand).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToList();
            UniqueModels = scopedEquipments.Select(e => e.Model).Where(m => !string.IsNullOrEmpty(m)).Distinct().OrderBy(m => m).ToList();
            
            ActiveFilterAgrupamento = AvailableAgrupamentos.FirstOrDefault();

            // Fetch items borrowed by the director
            MyBorrowedItems = await _context.StockEmpresa
                .Where(s => s.DirectorId == userId)
                .ToListAsync();

            // Fetch ALL pending requests from the director's grouping
            int? agrupamentoId = director?.AgrupamentoId;
            var agrupamentoTickets = _context.Tickets
                .Include(t => t.RequestedBy)
                .Include(t => t.School)
                .Where(t => t.Level == "Empréstimo" && (t.Status == "Pedido" || t.Status == "Pendente"));

            if (agrupamentoId.HasValue)
            {
                // Mostra tickets da escola OU tickets de utilizadores que pertencem a escolas do agrupamento
                var agrupamentoSchoolIds = await _context.Schools.Where(s => s.AgrupamentoId == agrupamentoId).Select(s => s.Id).ToListAsync();
                agrupamentoTickets = agrupamentoTickets.Where(t => 
                    (t.SchoolId != null && agrupamentoSchoolIds.Contains(t.SchoolId.Value)) ||
                    (t.RequestedByUserId != null && _context.Users.Any(u => u.Id == t.RequestedByUserId)) // Fallback de segurança
                );
            }
            else
            {
                agrupamentoTickets = agrupamentoTickets.Where(t => t.RequestedByUserId == userId);
            }

            MyStockRequests = await agrupamentoTickets
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (ModelState.IsValid)
            {
                // Ensure mutual exclusivity based on location type
                if (LocationType == "empresa")
                {
                    NewEquipment.RoomId = null;
                }
                else
                {
                    NewEquipment.EmpresaId = null;
                }

                NewEquipment.Name = NormalizeEquipmentName(NewEquipment.Name);
                _context.Equipamentos.Add(NewEquipment);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento registado com sucesso!" });
            }
            return await OnGetAsync(null);
        }

        private static string NormalizeEquipmentName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Desconhecido";
            string normalized = name.Trim();
            var pluralMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Computadores", "Computador" },
                { "Monitores", "Monitor" },
                { "Impressoras", "Impressora" },
                { "Projetores", "Projetor" },
                { "Televisores", "Televisão" },
                { "Portáteis", "Portátil" },
                { "Ratos", "Rato" },
                { "Teclados", "Teclado" },
                { "UPSs", "UPS" },
                { "Switches", "Switch" },
                { "Quadros Interativos", "Quadro Interativo" },
                { "Mesas Interativas", "Mesa Interativa" }
            };

            if (pluralMaps.TryGetValue(normalized, out var singular)) return singular;

            if (normalized.EndsWith("ores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("adores", StringComparison.OrdinalIgnoreCase)) return normalized.Substring(0, normalized.Length - 2);
            if (normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !normalized.EndsWith("ss", StringComparison.OrdinalIgnoreCase) && normalized.Length > 4)
                return normalized.Substring(0, normalized.Length - 1);

            return normalized;
        }

        public async Task<IActionResult> OnPostCreateTicketAsync()
        {
            if (NewTicket.EquipamentoId == null) return RedirectToPage(new { success = "Erro: Equipamento não selecionado." });

            var equipment = await _context.Equipamentos
                .Include(e => e.Room)
                    .ThenInclude(r => r.Block)
                .FirstOrDefaultAsync(e => e.Id == NewTicket.EquipamentoId);

            if (equipment == null) return RedirectToPage(new { success = "Erro: Equipamento não encontrado." });

            NewTicket.SchoolId = equipment.Room.Block.SchoolId;
            NewTicket.Status = "Pendente";
            NewTicket.CreatedAt = DateTime.UtcNow;
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr)) NewTicket.RequestedByUserId = int.Parse(userIdStr);

            NewTicket.Description = $"Pedido de reparação para {equipment.Name} ({equipment.SerialNumber}) em {equipment.Room.Name}";

            _context.Tickets.Add(NewTicket);
            
            // Mark equipment as damaged if it wasn't already
            if (equipment.Status != "Avariado")
            {
                equipment.Status = "Avariado";
            }
            
            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Ticket solicitado com sucesso!" });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
                // If the equipment is 'Em reparação', the Director cannot edit it.
                if (item.Status == "Em reparação")
                {
                    return RedirectToPage();
                }

                if (item.Status == "A funcionar" || item.Status == "Disponível" || string.IsNullOrEmpty(item.Status))
                {
                    item.Status = "Avariado";
                }
                else
                {
                    item.Status = "A funcionar";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
                _context.Equipamentos.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento eliminado com sucesso!" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage(new { success = "Erro ao validar dados do equipamento" });

            var equip = await _context.Equipamentos.FindAsync(NewEquipment.Id);
            if (equip == null) return RedirectToPage(new { success = "Equipamento não encontrado" });

            // Update fields
            equip.Name = NewEquipment.Name;
            equip.Type = NewEquipment.Type;
            equip.Brand = NewEquipment.Brand;
            equip.Model = NewEquipment.Model;
            equip.SerialNumber = NewEquipment.SerialNumber;
            equip.Status = NewEquipment.Status ?? "A funcionar";
            
            if (LocationType == "empresa")
            {
                equip.EmpresaId = NewEquipment.EmpresaId;
                equip.RoomId = null;
            }
            else
            {
                equip.RoomId = NewEquipment.RoomId;
                equip.EmpresaId = null;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { success = "Equipamento atualizado com sucesso!" });
        }

        public async Task<IActionResult> OnPostReturnItemAsync(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            var item = await _context.StockEmpresa.FirstOrDefaultAsync(s => s.Id == id && s.DirectorId == userId);
            if (item != null)
            {
                item.DirectorId = null;
                item.Status = "Disponível";
                item.IsAvailable = true;
                
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento devolvido com sucesso." });
            }
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateStockRequestAsync(string? itemName, string? itemType, int quantity, string? notes)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Auth/Login");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                return RedirectToPage(new { success = "Por favor selecione um artigo." });
            }

            var director = await _context.Diretores.Include(d => d.Agrupamento).FirstOrDefaultAsync(d => d.UserId == userId);
            
            var dataObj = new {
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
                AgrupamentoId = director?.AgrupamentoId ?? 0,
                RequestorId = userId,
                RequestorRole = "Diretor"
            };
            var dataJson = System.Text.Json.JsonSerializer.Serialize(dataObj);

            var ticket = new Ticket
            {
                Description = $"PEDIDO DE STOCK (DIRETOR):\nArtigo: {itemName}\nTipo: {itemType ?? "N/A"}\nQuantidade: {quantity}\nMotivo: {notes}\nAgrupamento: {director?.Agrupamento?.Name}\n\n[DATA:{dataJson}]",
                Level = "Empréstimo",
                Status = "Pendente",
                CreatedAt = DateTime.UtcNow,
                RequestedByUserId = userId
            };

            // If we have a school context, assign it
            var firstSchool = await _context.Schools.FirstOrDefaultAsync(s => s.AgrupamentoId == director.AgrupamentoId);
            if (firstSchool != null) ticket.SchoolId = firstSchool.Id;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { success = $"Pedido de {itemName} enviado com sucesso para a administração." });
        }
    }
}
