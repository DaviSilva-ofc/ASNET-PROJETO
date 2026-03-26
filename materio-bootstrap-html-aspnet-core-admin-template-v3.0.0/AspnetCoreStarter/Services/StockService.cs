using System.Collections.Generic;

namespace AspnetCoreStarter.Services
{
    public interface IStockService
    {
        int GetThreshold(string? equipmentName, string? type);
        bool IsLowStock(string? equipmentName, string? type, int currentCount);
    }

    public class StockService : IStockService
    {
        // Dictionary to store thresholds by equipment name or type.
        // We prioritize equipment name, then type, then a default value.
        private readonly Dictionary<string, int> _thresholds = new(System.StringComparer.OrdinalIgnoreCase)
        {
            // By Type (matching the dropdown in Stocks.cshtml)
            { "Rato", 5 },
            { "Teclado", 5 },
            { "UPS", 2 },
            { "Desktop", 2 },
            { "Portátil", 2 },
            { "Monitor", 2 },
            { "Impressora", 1 },
            { "Router", 1 },
            { "Switch", 1 },
            { "Access Point", 1 },
            { "Motherboard", 2 },
            { "Disco HDD", 3 },
            { "Disco SSD", 3 },
            { "Memória RAM", 4 },
            { "Fonte Alimentação", 2 }
        };

        private const int DefaultThreshold = 3;

        public int GetThreshold(string? equipmentName, string? type)
        {
            // 1. Try by equipment name (more specific)
            if (!string.IsNullOrEmpty(equipmentName) && _thresholds.TryGetValue(equipmentName, out var nameThreshold))
            {
                return nameThreshold;
            }

            // 2. Try by type
            if (!string.IsNullOrEmpty(type) && _thresholds.TryGetValue(type, out var typeThreshold))
            {
                return typeThreshold;
            }

            return DefaultThreshold;
        }

        public bool IsLowStock(string? equipmentName, string? type, int currentCount)
        {
            return currentCount < GetThreshold(equipmentName, type);
        }
    }
}
