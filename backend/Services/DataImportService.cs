using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using backend.Data;
using backend.Models;

namespace backend.Services
{
    public class DataImportService
    {
        private readonly AppDbContext _dbContext;
        private readonly PasswordService _passwordService;
        private readonly ILogger<DataImportService> _logger;

        public DataImportService(
            AppDbContext dbContext,
            PasswordService passwordService,
            ILogger<DataImportService> logger)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task ImportUsersFromCsvAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("CSV file not found at path: {FilePath}", filePath);
                return;
            }

            try
            {
                // Check if we already have users in the database
                if (_dbContext.Users.Any())
                {
                    _logger.LogInformation("Users already exist in the database. Skipping CSV import.");
                    return;
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                };

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);
                
                // Register mappings
                csv.Context.RegisterClassMap<UserCsvMap>();
                
                var records = csv.GetRecords<UserCsvRecord>().ToList();
                _logger.LogInformation("Found {Count} records in CSV file", records.Count);

                var users = new List<User>();
                foreach (var record in records)
                {
                    var user = new User
                    {
                        Matricule = record.Matricule,
                        FirstName = record.FirstName,
                        LastName = record.LastName,
                        PasswordHash = _passwordService.HashPassword(record.Password)
                    };
                    users.Add(user);
                }

                await _dbContext.Users.AddRangeAsync(users);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Successfully imported {Count} users from CSV", users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users from CSV: {Message}", ex.Message);
                throw;
            }
        }
    }

    public class UserCsvRecord
    {
        public string Matricule { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class UserCsvMap : ClassMap<UserCsvRecord>
    {
        public UserCsvMap()
        {
            Map(m => m.Matricule).Name("Matrecul");
            Map(m => m.Password).Name("Password");
            Map(m => m.FirstName).Name("First Name");
            Map(m => m.LastName).Name("Last Name");
        }
    }
} 