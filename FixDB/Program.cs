using System;
using MySqlConnector;

namespace FixDB
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string connectionString = "Server=localhost;Port=3306;Database=ASNET_DB;Uid=root;Pwd=A12345-a;";
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new MySqlCommand("SELECT id_pedido, nome_artigo, status FROM pedidos_stock ORDER BY id_pedido DESC LIMIT 20;", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("id | nome_artigo | status");
                        Console.WriteLine("---+-------------+--------");
                        while (reader.Read())
                        {
                            Console.WriteLine($"{reader["id_pedido"]} | {reader["nome_artigo"]} | [{reader["status"]}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
