using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreStarter.Pages.Clients.Technicians
{
    public class TechnicianEquipmentModel : PageModel
    {
        private readonly AppDbContext _context;

        public TechnicianEquipmentModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Equipamento> Equipments { get; set; }
        public List<Sala> Rooms { get; set; }

        [BindProperty]
        public Equipamento NewEquipment { get; set; }

        public string? SuccessMessage { get; set; }

        public Sala ActiveFilterRoom { get; set; }
        public Bloco ActiveFilterBloco { get; set; }
        public School ActiveFilterEscola { get; set; }
        public Agrupamento ActiveFilterAgrupamento { get; set; }
        public Empresa ActiveFilterEmpresa { get; set; }

        public List<Agrupamento> AvailableAgrupamentos { get; set; }
        public List<School> AvailableEscolas { get; set; }
        public List<Bloco> AvailableBlocos { get; set; }
        public List<Empresa> AvailableEmpresas { get; set; }
        public List<string> UniqueEquipmentNames { get; set; } = new();
        public List<string> UniqueBrands { get; set; } = new();
        public List<string> UniqueModels { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? FilterName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterArticle { get; set; }

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
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterBrand { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterModel { get; set; }

        [BindProperty]
        public string LocationType { get; set; } // "escola" or "empresa"

        public async Task<IActionResult> OnGetAsync(string? success)
        {
            if (!string.IsNullOrEmpty(success)) SuccessMessage = success;

            if (User?.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && userRole != "Tecnico") return RedirectToPage("/Index");

            var query = _context.Equipamentos
                .Include(e => e.Room).ThenInclude(r => r.Block).ThenInclude(b => b.School).ThenInclude(s => s.Agrupamento)
                .Include(e => e.Empresa)
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterName))
            {
                var nameLower = FilterName.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(nameLower) || 
                                         (e.Brand != null && e.Brand.ToLower().Contains(nameLower)) || 
                                         (e.Model != null && e.Model.ToLower().Contains(nameLower)));
            }

            if (!string.IsNullOrEmpty(FilterType))
            {
                query = query.Where(e => e.Type != null && e.Type == FilterType);
            }

            if (!string.IsNullOrEmpty(FilterArticle))
            {
                var normalizedArticle = NormalizeEquipmentName(FilterArticle);
                query = query.Where(e => e.Name != null && (e.Name == FilterArticle || e.Name == normalizedArticle));
            }

            if (!string.IsNullOrEmpty(FilterSerialNumber))
            {
                query = query.Where(e => e.SerialNumber.Contains(FilterSerialNumber));
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
                query = query.Where(e => e.Status == FilterStatus);
            }

            if (!string.IsNullOrEmpty(FilterBrand))
            {
                query = query.Where(e => e.Brand != null && e.Brand == FilterBrand);
            }

            if (!string.IsNullOrEmpty(FilterModel))
            {
                query = query.Where(e => e.Model != null && e.Model == FilterModel);
            }

            Equipments = await query.ToListAsync();
            Rooms = await _context.Salas.ToListAsync();
            AvailableAgrupamentos = await _context.Agrupamentos.ToListAsync();
            AvailableEscolas = await _context.Schools.ToListAsync();
            AvailableBlocos = await _context.Blocos.ToListAsync();
            AvailableEmpresas = await _context.Empresas.ToListAsync();
            var baseCategories = new List<string> { "Computadores", "Monitores", "Impressoras", "Networking", "Servidores/Infra", "Componentes", "Periféricos", "Projetores", "Quadros Interativos", "Outros" };
            
            var existingNames = await _context.Equipamentos
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .Select(e => e.Name)
                .Distinct()
                .ToListAsync();

            // Sincronizar nomes do DB com o formato de exibição (Plural se categoria base)
            UniqueEquipmentNames = baseCategories
                .Union(existingNames)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            UniqueBrands = await _context.Equipamentos
                .Where(e => !string.IsNullOrEmpty(e.Brand))
                .Select(e => e.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            UniqueModels = await _context.Equipamentos
                .Where(e => !string.IsNullOrEmpty(e.Model))
                .Select(e => e.Model)
                .Distinct()
                .OrderBy(m => m)
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

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage(new { success = "Erro ao validar dados do equipamento" });

            var equip = await _context.Equipamentos.FindAsync(NewEquipment.Id);
            if (equip == null) return RedirectToPage(new { success = "Equipamento não encontrado" });

            // Update fields
            equip.Name = NormalizeEquipmentName(NewEquipment.Name);
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

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _context.Equipamentos.FindAsync(id);
            if (item != null)
            {
                _context.Equipamentos.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToPage(new { success = "Equipamento excluído com sucesso!" });
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string status)
        {
            var equip = await _context.Equipamentos.FindAsync(id);
            if (equip != null)
            {
                equip.Status = NormalizeEquipmentStatus(status);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private static string NormalizeEquipmentStatus(string? rawStatus)
        {
            var s = (rawStatus ?? "").Trim().ToLowerInvariant();

            if (s.Contains("repara")) return "Em Reparação";
            if (s.Contains("avari")) return "Avariado";
            if (s.Contains("funcion") || s.Contains("funcionando")) return "A funcionar";

            return string.IsNullOrWhiteSpace(rawStatus) ? "A funcionar" : rawStatus.Trim();
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
    }
}
