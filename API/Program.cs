using System;
using System.Data.SQLite;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace API
{
    class Program
    {
        private static readonly string connectionString = "Data Source=nationality.db;Version=3;";

        static async Task Main(string[] args)
        {
            InitializeDatabase();

            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Make a new API request");
            Console.WriteLine("2. View saved results from the database");
            Console.Write("Your choice: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await MakeApiRequest();
                    break;
                case "2":
                    Console.WriteLine("Saved results from the database:");
                    ShowSavedResults();
                    break;
                default:
                    Console.WriteLine("Invalid choice, please select 1 or 2.");
                    break;
            }
        }

        private static async Task MakeApiRequest()
        {
            Console.Write("Enter a name: ");
            string name = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Please enter a valid name.");
                return;
            }

            await GetNationalityAsync(name);

            Console.WriteLine("\nSaved results from database:");
            ShowSavedResults();
        }
        private static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string createTableQuery = @"CREATE TABLE IF NOT EXISTS NationalityResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CountryCode TEXT NOT NULL,
                    Probability REAL NOT NULL)";
                SQLiteCommand command = new SQLiteCommand(createTableQuery, connection);
                command.ExecuteNonQuery();
            }
        }
        private static async Task GetNationalityAsync(string name)
        {
            string apiUrl = $"https://api.nationalize.io?name={name}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    var nationalityData = JsonSerializer.Deserialize<NationalityResponse>(jsonResponse);

                    if (nationalityData != null && nationalityData.country.Length > 0)
                    {
                        Console.WriteLine($"Results for name '{name}':");
                        foreach (var country in nationalityData.country)
                        {
                            Console.WriteLine($"Country: {country.country_id}, Probability: {country.probability}");
                            SaveResultToDatabase(name, country.country_id, country.probability);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No data available for this name.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        private static void SaveResultToDatabase(string name, string countryCode, double probability)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string insertQuery = @"INSERT INTO NationalityResults (Name, CountryCode, Probability) 
                                       VALUES (@Name, @CountryCode, @Probability)";
                SQLiteCommand command = new SQLiteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@CountryCode", countryCode);
                command.Parameters.AddWithValue("@Probability", probability);
                command.ExecuteNonQuery();
            }
        }
        private static void ShowSavedResults()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = @"SELECT Name, CountryCode, Probability FROM NationalityResults";
                SQLiteCommand command = new SQLiteCommand(selectQuery, connection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string result = $"Name: {reader["Name"]}, Country: {reader["CountryCode"]}, Probability: {reader["Probability"]}";
                        Console.WriteLine(result);
                    }
                }
            }
        }
    }

    public class NationalityResponse
    {
        public string name { get; set; }
        public Country[] country { get; set; }
    }

    public class Country
    {
        public string country_id { get; set; }
        public double probability { get; set; }
    }
}
