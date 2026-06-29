namespace Telemetry.Consumer.Repository;

using Telemetry.Shared.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;

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
        using var cmd = new SqlCommand("dbo.sp_InsertAnalysisResults", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@Id", SqlDbType.VarChar, 50).Value = result.Id;
        cmd.Parameters.Add("@SensorReadingId", SqlDbType.VarChar, 50).Value = result.SensorReadingId;
        cmd.Parameters.Add("@AnalysisType", SqlDbType.VarChar, 50).Value = result.AnalysisType;
        cmd.Parameters.Add("@Result", SqlDbType.Float).Value = result.Result;
        cmd.Parameters.Add("@ProcessedAt", SqlDbType.DateTime2).Value = result.ProcessedAt;

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}