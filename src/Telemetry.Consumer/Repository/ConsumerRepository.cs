namespace Telemetry.Consumer.Repository;

using Telemetry.Shared.Models;
using Microsoft.Data.SqlClient;
using System.Data;




public class ConsumerRepository : IConsumerRepository
{
    private readonly string _connectionString;

    public ConsumerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InsertAnalysisResultAsync(AnalysisResult result)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_InsertAnalysisResults", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@SensorReadingId", result.SensorReadingId);
        cmd.Parameters.AddWithValue("@AnalysisType", result.AnalysisType);
        cmd.Parameters.AddWithValue("@Result", result.Result);
        cmd.Parameters.AddWithValue("@ProcessedAt", result.ProcessedAt);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}