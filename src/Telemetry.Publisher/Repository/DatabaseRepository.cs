namespace Telemetry.Publisher.Repository;

using Microsoft.Data.SqlClient;
using System.Data;
using Telemetry.Shared.Models;

public class DatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;

    public DatabaseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task SavePendingReadingAsync(SensorReading reading)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("dbo.sp_SavePendingReading", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@Id", SqlDbType.VarChar, 50).Value = reading.Id;
        cmd.Parameters.Add("@Timestamp", SqlDbType.DateTime2).Value = reading.Timestamp;
        cmd.Parameters.Add("@Value", SqlDbType.Float).Value = reading.Value;
        cmd.Parameters.Add("@SensorType", SqlDbType.VarChar, 50).Value = reading.SensorType;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<SensorReading?> GetOldestPendingAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("dbo.sp_GetOldestPending", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SensorReading
            {
                Id = reader.GetString(0),
                Timestamp = reader.GetDateTime(1),
                Value = reader.GetDouble(2),
                SensorType = reader.GetString(3)
            };
        }
        return null;
    }
}