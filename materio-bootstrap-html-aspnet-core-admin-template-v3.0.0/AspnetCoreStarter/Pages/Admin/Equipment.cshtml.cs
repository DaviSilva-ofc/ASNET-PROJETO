using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspnetCoreStarter.Pages.Admin
{
    public class EquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public EquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; }

        [BindProperty]
        public Equipamento NewEquipment { get; set; }

        public string? SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; }

        public bool IsMaintenanceScheduledThisYear { get; set; }
        public DateTime? ScheduledMaintenanceDate { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public School ActiveFilterEscola { get; set; }
        public Agrupamento ActiveFilterAgrupamento { get; set; }
        public Empresa ActiveFilterEmpresa { get; set; }

        public List<Agrupamento> AvailableAgrupamentos { get; set; }
        public List<School> AvailableEscolas { get; set; }
        public List<Bloco> AvailableBlocos { get; set; }
        public List<Empresa> AvailableEmpresas { get; set; }
        public List<User> Technicians { get; set; } = new();
        public List<string> UniqueEquipmentNames { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterSerialNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamentoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEscolaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterBlocoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterRoomId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterEmpresaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterBrand { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterModel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty]
        public string LocationType { get; set; } // "escola" or "empresa"


        public async Task<IActionResult> OnGetAsync(string? success)
        {
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;

            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && userRole != "Tecnico") return RedirectToPage("/Index");

            // Ensure data exists for selects
            Rooms = await _context.Salas.ToListAsync();
            AvailableAgrupamentos = await _context.Agrupamentos.ToListAsync();
            AvailableEscolas = await _context.Schools.ToListAsync();
            AvailableBlocos = await _context.Blocos.ToListAsync();
            AvailableEmpresas = await _context.Empresas.ToListAsync();
            
            // Fetch Technicians
            var techIds = await _context.Tecnicos.Select(t => t.UserId).ToListAsync();
            Technicians = await _context.Users.Where(u => techIds.Contains(u.Id)).ToListAsync();

            // Preventives Check for current year
            int currentYear = DateTime.Now.Year;
            var maintenanceSettings = await AspnetCoreStarter.Helpers.MaintenanceSettingsHelper.GetSettingsAsync(currentYear);
            IsMaintenanceScheduledThisYear = maintenanceSettings.ScheduledDate.HasValue;
            ScheduledMaintenanceDate = maintenanceSettings.ScheduledDate;

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .Include(e => e.Empresa)
                .AsQueryable();

            // Apply Filters (Case-insensitive and robust)
            if (!string.IsNullOrEmpty(FilterName)) 
            {
                var nameLower = FilterName.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(nameLower)); 
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
            else if (FilterAgrupamentoId.HasValue)
            {
                query = query.Where(e => e.Room != null && e.Room.Block.School.AgrupamentoId == FilterAgrupamentoId.Value);
                ActiveFilterAgrupamento = await _context.Agrupamentos.FindAsync(FilterAgrupamentoId.Value);
            }

            if (FilterEmpresaId.HasValue)
            {
                query = query.Where(e => e.EmpresaId == FilterEmpresaId.Value);
                ActiveFilterEmpresa = await _context.Empresas.FindAsync(FilterEmpresaId.Value);
            }

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                if (FilterStatus == "Associado")
                {
                    query = query.Where(e => e.TicketId != null);
                }
                else
                {
                    query = query.Where(e => e.Status == FilterStatus || e.StatusEquipamentos.Any(s => s.Estado == FilterStatus));
                }
            }

            Equipments = await query.ToListAsync();
            
            UniqueEquipmentNames = await _context.Equipamentos
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .Select(e => e.Name)
                .Distinct()
                .OrderBy(n => n)
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

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
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
            return RedirectToPage(new { Tab = Tab });
        }

        public async Task<IActionResult> OnPostScheduleMaintenanceAsync(DateTime scheduledDate, int? technicianId, List<int> schoolIds)
        {
            if (scheduledDate == default) return RedirectToPage(new { Tab = "preventive" });

            int currentYear = DateTime.Now.Year;
            await AspnetCoreStarter.Helpers.MaintenanceSettingsHelper.SaveSettingsAsync(currentYear, scheduledDate);

            // Create Tickets for each selected school
            if (schoolIds != null && schoolIds.Count > 0)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdStr, out int adminId);

                foreach (var schoolId in schoolIds)
                {
                    var school = await _context.Schools.FindAsync(schoolId);
                    var ticket = new Ticket
                    {
                        Type = "Manutenção Preventiva",
                        Level = "Média",
                        Status = technicianId.HasValue ? "Aceite" : "Pendente",
                        Description = $"Manutenção Preventiva Anual - {currentYear}",
                        SchoolId = schoolId,
                        AdminId = adminId,
                        TechnicianId = technicianId,
                        CreatedAt = DateTime.UtcNow,
                        ScheduledDate = scheduledDate,
                        AcceptedAt = technicianId.HasValue ? DateTime.UtcNow : null
                    };
                    _context.Tickets.Add(ticket);

                    if (technicianId.HasValue)
                    {
                        var tech = await _context.Users.FindAsync(technicianId);
                        _context.TicketHistorico.Add(new TicketHistorico
                        {
                            TicketId = ticket.Id,
                            Acao = $"Atribuído para Manutenção Preventiva por Admin. Técnico: {tech?.Username}",
                            TipoAcao = TipoAcaoHistorico.Status,
                            Autor = User.Identity?.Name ?? "Admin",
                            Data = DateTime.UtcNow
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { Tab = "preventive", success = $"Manutenção agendada e tickets criados para {schoolIds?.Count ?? 0} escolas!" });
        }

        public async Task<IActionResult> OnPostSubmitPreventiveChecklistAsync(int? roomId, int? empresaId, string artigo, string? notes)
        {
            if (string.IsNullOrEmpty(artigo)) return RedirectToPage(new { Tab = "preventive" });

            var query = _context.Equipamentos.AsQueryable();
            if (roomId.HasValue) query = query.Where(e => e.RoomId == roomId.Value);
            else if (empresaId.HasValue) query = query.Where(e => e.EmpresaId == empresaId.Value);
            else return RedirectToPage(new { Tab = "preventive" });

            var equipments = await query.Where(e => e.Name == artigo).ToListAsync();
            var now = DateTime.Now;

            foreach (var equip in equipments)
            {
                equip.LastMaintenanceDate = now;
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(userIdStr, out int uid) ? uid : null;

                int? schoolId = null;
                string locationName = "";
                if (roomId.HasValue)
                {
                    var room = await _context.Salas.Include(r => r.Block).ThenInclude(b => b.School).FirstOrDefaultAsync(r => r.Id == roomId.Value);
                    if (room != null)
                    {
                        schoolId = room.Block?.SchoolId;
                        locationName = $"{room.Name} ({room.Block?.Name})";
                    }
                }
                else if (empresaId.HasValue)
                {
                    var emp = await _context.Empresas.FindAsync(empresaId.Value);
                    if (emp != null) locationName = emp.Name;
                }

                var ticket = new Ticket
                {
                    Type = "Manutenção Preventiva",
                    Level = "Média",
                    Status = "Pendente",
                    Description = $"[Checklist: {artigo} em {locationName}] Observações: {notes}",
                    SchoolId = schoolId,
                    RequestedByUserId = currentUserId,
                    CreatedAt = now
                };
                _context.Tickets.Add(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { Tab = "preventive", success = $"Checklist de {artigo} concluída com sucesso!" });
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
    }
}
