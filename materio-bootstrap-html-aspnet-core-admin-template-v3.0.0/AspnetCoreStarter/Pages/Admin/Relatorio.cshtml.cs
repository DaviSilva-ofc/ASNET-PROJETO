using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using ClosedXML.Excel;

namespace AspnetCoreStarter.Pages.Admin
{
    public class RelatorioModel : PageModel
    {
        private readonly AppDbContext _context;

        public RelatorioModel(AppDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ── Analytics properties ────────────────────────────────────────────────
        public int TotalTickets { get; set; }
        public int TicketsConcluidos { get; set; }
        public int TicketsAbertos { get; set; }
        public double TaxaDeSucesso { get; set; }
        public double CsatGlobal { get; set; }
        
        // Expense = Tickets + Stock Quantity
        public List<(string Nome, int Chamadas, int Stock, int TotalScore)> EscolasComMaisDespesa { get; set; } = new();
        public List<(string Marca, int Total)> MarcasMaisFrageis { get; set; } = new();
        public List<(string Professor, string Email, int Total, int UserId)> ProfessoresMaisChamadores { get; set; } = new();
        public List<(string Tecnico, int Total, int Concluidos, double Csat, int UserId)> PerformanceTecnicos { get; set; } = new();
        public List<Ticket> FeedbacksRecentes { get; set; } = new();
        
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public string TrimestralLabel { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public int Trimestre { get; set; } = 0; // 0 = last 90 days

        [BindProperty(SupportsGet = true)]
        public int Ano { get; set; } = DateTime.Now.Year;

        [BindProperty(SupportsGet = true)]
        public int? FilterClientId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterAgrupamentoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterSchoolId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterTechnicianId { get; set; }

        public List<Empresa> Empresas { get; set; } = new();
        public List<Agrupamento> Agrupamentos { get; set; } = new();
        public List<School> Escolas { get; set; } = new();
        public List<User> Tecnicos { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return RedirectToPage("/Index");

            await LoadFiltersAsync();
            await LoadDataAsync();
            return Page();
        }

        private async Task LoadFiltersAsync()
        {
            Empresas = await _context.Empresas.ToListAsync();
            Agrupamentos = await _context.Agrupamentos.ToListAsync();
            Escolas = await _context.Schools.ToListAsync();
            
            // Fetch users that are in the 'Tecnicos' table
            Tecnicos = await _context.Tecnicos
                .Include(t => t.User)
                .Select(t => t.User)
                .Where(u => u != null && !u.IsDeleted)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetDownloadPdfAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            if (User.FindFirst(ClaimTypes.Role)?.Value != "Admin") return RedirectToPage("/Index");

            await LoadDataAsync();

            var pdf = GeneratePdf();
            var filename = $"Relatorio_{TrimestralLabel.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", filename);
        }

        public async Task<IActionResult> OnGetDownloadExcelAsync()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToPage("/Auth/Login");
            if (User.FindFirst(ClaimTypes.Role)?.Value != "Admin") return RedirectToPage("/Index");

            await LoadDataAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Resumo");
            
            // Header
            ws.Cell(1, 1).Value = "Relatório Analítico - " + TrimestralLabel;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            ws.Cell(3, 1).Value = "Métrica";
            ws.Cell(3, 2).Value = "Valor";
            ws.Range(3, 1, 3, 2).Style.Font.Bold = true;
            ws.Range(3, 1, 3, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

            ws.Cell(4, 1).Value = "Total de Chamadas";
            ws.Cell(4, 2).Value = TotalTickets;
            ws.Cell(5, 1).Value = "Chamadas Concluídas";
            ws.Cell(5, 2).Value = TicketsConcluidos;
            ws.Cell(6, 1).Value = "Chamadas em Aberto";
            ws.Cell(6, 2).Value = TicketsAbertos;
            ws.Cell(7, 1).Value = "Taxa de Sucesso";
            ws.Cell(7, 2).Value = $"{TaxaDeSucesso}%";
            ws.Cell(8, 1).Value = "Satisfação (CSAT)";
            ws.Cell(8, 2).Value = $"{CsatGlobal} / 5.0";

            // Schools Sheet
            var wsSchools = workbook.Worksheets.Add("Escolas e Despesa");
            wsSchools.Cell(1, 1).Value = "Escola";
            wsSchools.Cell(1, 2).Value = "Chamadas (Tickets)";
            wsSchools.Cell(1, 3).Value = "Artigos Stock";
            wsSchools.Cell(1, 4).Value = "Score de Despesa";
            wsSchools.Range(1, 1, 1, 4).Style.Font.Bold = true;
            wsSchools.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 2;
            foreach(var item in EscolasComMaisDespesa)
            {
                wsSchools.Cell(row, 1).Value = item.Nome;
                wsSchools.Cell(row, 2).Value = item.Chamadas;
                wsSchools.Cell(row, 3).Value = item.Stock;
                wsSchools.Cell(row, 4).Value = item.TotalScore;
                row++;
            }
            wsSchools.Columns().AdjustToContents();

            // Brands Sheet
            var wsBrands = workbook.Worksheets.Add("Equipamento Fragil");
            wsBrands.Cell(1, 1).Value = "Marca";
            wsBrands.Cell(1, 2).Value = "Avarias Registadas";
            wsBrands.Range(1, 1, 1, 2).Style.Font.Bold = true;
            row = 2;
            foreach(var item in MarcasMaisFrageis)
            {
                wsBrands.Cell(row, 1).Value = item.Marca;
                wsBrands.Cell(row, 2).Value = item.Total;
                row++;
            }
            wsBrands.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var filename = $"Relatorio_{TrimestralLabel.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        private void CalculatePeriod()
        {
            if (Trimestre == 0)
            {
                PeriodoFim = DateTime.Now;
                PeriodoInicio = PeriodoFim.AddDays(-90);
                TrimestralLabel = "Últimos 90 Dias";
            }
            else
            {
                int startMonth = (Trimestre - 1) * 3 + 1;
                PeriodoInicio = new DateTime(Ano, startMonth, 1);
                PeriodoFim = PeriodoInicio.AddMonths(3).AddTicks(-1);
                TrimestralLabel = $"{Trimestre}º Trimestre {Ano}";
            }
        }

        private async Task LoadDataAsync()
        {
            CalculatePeriod();

            // Fetch Tickets with filters
            var ticketsQuery = _context.Tickets
                .Include(t => t.School)
                .Include(t => t.Equipamento).ThenInclude(e => e.Empresa)
                .Include(t => t.RequestedBy)
                .Include(t => t.Technician)
                .Where(t => t.CreatedAt >= PeriodoInicio && t.CreatedAt <= PeriodoFim
                         && t.Level != "Empréstimo" && t.Level != "Alteração de Estado" && (t.Level == null || !t.Level.Contains("ltera")))
                .AsQueryable();

            if (FilterClientId.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.Equipamento != null && t.Equipamento.EmpresaId == FilterClientId.Value);
            
            if (FilterAgrupamentoId.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.School != null && t.School.AgrupamentoId == FilterAgrupamentoId.Value);

            if (FilterSchoolId.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.SchoolId == FilterSchoolId.Value);

            if (FilterTechnicianId.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.TechnicianId == FilterTechnicianId.Value);

            var tickets = await ticketsQuery.ToListAsync();

            // Fetch Stock Requests with filters
            var stockQuery = _context.PedidosStock
                .Include(p => p.School)
                .Where(p => p.CreatedAt >= PeriodoInicio && p.CreatedAt <= PeriodoFim)
                .AsQueryable();

            if (FilterAgrupamentoId.HasValue)
                stockQuery = stockQuery.Where(p => p.School != null && p.School.AgrupamentoId == FilterAgrupamentoId.Value);

            if (FilterSchoolId.HasValue)
                stockQuery = stockQuery.Where(p => p.SchoolId == FilterSchoolId.Value);

            var stockRequests = await stockQuery.ToListAsync();

            TotalTickets = tickets.Count;
            TicketsConcluidos = tickets.Count(t => t.Status == "Concluído");
            TicketsAbertos = tickets.Count(t => t.Status != "Concluído");
            TaxaDeSucesso = TotalTickets > 0 ? Math.Round((double)TicketsConcluidos / TotalTickets * 100, 1) : 0;

            // CSAT Calculation
            var ratedTickets = tickets.Where(t => t.SatisfacaoRating.HasValue).ToList();
            CsatGlobal = ratedTickets.Any() ? Math.Round(ratedTickets.Average(t => t.SatisfacaoRating!.Value), 1) : 0;

            // Grouping by School for Expense
            var schoolTickets = tickets
                .Where(t => t.School != null)
                .GroupBy(t => t.School!.Name ?? "Desconhecida")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            var schoolStock = stockRequests
                .Where(p => p.School != null)
                .GroupBy(p => p.School!.Name ?? "Desconhecida")
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToList();

            var allSchools = schoolTickets.Select(s => s.Name).Union(schoolStock.Select(s => s.Name)).Distinct();

            EscolasComMaisDespesa = allSchools.Select(s => {
                int tCount = schoolTickets.FirstOrDefault(x => x.Name == s)?.Count ?? 0;
                int sQty = schoolStock.FirstOrDefault(x => x.Name == s)?.Qty ?? 0;
                return (Nome: s, Chamadas: tCount, Stock: sQty, TotalScore: tCount + sQty);
            })
            .OrderByDescending(x => x.TotalScore)
            .Take(5)
            .ToList();

            // Fragile Brands
            MarcasMaisFrageis = tickets
                .Where(t => t.Equipamento?.Brand != null)
                .GroupBy(t => t.Equipamento!.Brand!)
                .Select(g => (Marca: g.Key, Total: g.Count()))
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // Professors
            ProfessoresMaisChamadores = tickets
                .Where(t => t.RequestedBy != null)
                .GroupBy(t => new { t.RequestedBy!.Username, t.RequestedBy.Email, t.RequestedBy.Id })
                .Select(g => (Professor: g.Key.Username ?? "N/A", Email: g.Key.Email ?? "", Total: g.Count(), UserId: g.Key.Id))
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // Performance with CSAT
            PerformanceTecnicos = tickets
                .Where(t => t.Technician != null)
                .GroupBy(t => new { t.Technician!.Username, t.Technician.Id })
                .Select(g => {
                    var total = g.Count();
                    var concluidos = g.Count(t => t.Status == "Concluído");
                    var ratings = g.Where(t => t.SatisfacaoRating.HasValue).Select(t => t.SatisfacaoRating!.Value).ToList();
                    var avgCsat = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0.0;
                    return (Tecnico: g.Key.Username ?? "N/A", Total: total, Concluidos: concluidos, Csat: avgCsat, UserId: g.Key.Id);
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // Individual Feedbacks for UI
            FeedbacksRecentes = tickets
                .Where(t => t.SatisfacaoRating.HasValue)
                .OrderByDescending(t => t.DataAvaliacao ?? t.CreatedAt)
                .Take(10)
                .ToList();
        }

        private byte[] GeneratePdf()
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Gerado em ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" | Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("RELATÓRIO ANALÍTICO").FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                        c.Item().Text(TrimestralLabel).FontSize(14).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(120).Column(c =>
                    {
                        c.Item().AlignRight().Text("ASNET Platform").Bold().FontSize(12);
                    });
                });
                col.Item().PaddingTop(5).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Blue.Lighten2);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(20);

                // Summary
                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    SummaryCard(row.RelativeItem(), "Total de Chamadas", TotalTickets.ToString(), Colors.Blue.Darken2);
                    SummaryCard(row.RelativeItem(), "Taxa de Sucesso", $"{TaxaDeSucesso}%", Colors.Green.Darken2);
                    SummaryCard(row.RelativeItem(), "Satisfação (CSAT)", $"{CsatGlobal} / 5", Colors.Amber.Darken3);
                });

                // Schools Table
                col.Item().Text("Top Escolas (Score de Despesa)").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });
                    table.Header(header => {
                        header.Cell().Element(HeaderStyle).Text("Escola");
                        header.Cell().Element(HeaderStyle).Text("Tickets");
                        header.Cell().Element(HeaderStyle).Text("Stock");
                        header.Cell().Element(HeaderStyle).Text("Total");
                    });
                    foreach (var item in EscolasComMaisDespesa)
                    {
                        table.Cell().Element(CellStyle).Text(item.Nome);
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Chamadas.ToString());
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Stock.ToString());
                        table.Cell().Element(CellStyle).AlignCenter().Text(t => t.Span(item.TotalScore.ToString()).Bold());
                    }
                });

                // Technicians Table
                col.Item().Text("Performance por Técnico").FontSize(12).Bold().FontColor(Colors.Green.Darken3);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });
                    table.Header(header => {
                        header.Cell().Element(HeaderStyle).Text("Técnico");
                        header.Cell().Element(HeaderStyle).Text("Total");
                        header.Cell().Element(HeaderStyle).Text("Conc.");
                        header.Cell().Element(HeaderStyle).Text("CSAT");
                    });
                    foreach (var item in PerformanceTecnicos)
                    {
                        table.Cell().Element(CellStyle).Text(item.Tecnico);
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Total.ToString());
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Concluidos.ToString());
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Csat.ToString("0.0"));
                    }
                });
            });
        }

        private static void SummaryCard(IContainer container, string label, string value, string color)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col => {
                col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium);
                col.Item().Text(value).FontSize(18).Bold().FontColor(color);
            });
        }

        private static IContainer HeaderStyle(IContainer container) => container.Background(Colors.Blue.Darken3).Padding(5);
        private static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);
    }
}
