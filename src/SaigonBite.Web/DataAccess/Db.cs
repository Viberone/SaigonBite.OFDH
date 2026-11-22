using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SaigonBite.Web.DataAccess;

/// <summary>Opens connections to the SQLite database and creates it from schema.sql and seed.sql on first run.</summary>
public class Db(string databasePath)
{
    public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    public string DatabasePath { get; } = databasePath;

    public SqliteConnection Open()
    {
        var c = new SqliteConnection($"Data Source={DatabasePath};Foreign Keys=True");
        c.Open();
        return c;
    }

    /// <summary>Creates the database if it does not exist (or always when reset is true).</summary>
    public static Db Create(string databasePath, string sqlFolder, bool reset = false)
    {
        if (reset && File.Exists(databasePath))
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
        var db = new Db(databasePath);
        if (!File.Exists(databasePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
            using var c = db.Open();
            foreach (var file in new[] { "schema.sql", "seed.sql" })
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = File.ReadAllText(Path.Combine(sqlFolder, file));
                cmd.ExecuteNonQuery();
            }
        }
        return db;
    }

    // ---------- small helpers shared by the DAOs ----------
    public static string Ts(DateTime t) => t.ToString(TimeFormat, CultureInfo.InvariantCulture);
    public static object Ts(DateTime? t) => t is DateTime v ? Ts(v) : DBNull.Value;
    public static DateTime ParseTs(string s) => DateTime.ParseExact(s, TimeFormat, CultureInfo.InvariantCulture);
    public static DateTime? ParseTsOrNull(SqliteDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? null : ParseTs(r.GetString(r.GetOrdinal(col)));
    public static string? StrOrNull(SqliteDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));
    public static int? IntOrNull(SqliteDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetInt32(r.GetOrdinal(col));

    public static SqliteCommand Cmd(SqliteConnection c, string sql, SqliteTransaction? tx = null, params (string, object?)[] args)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }
}
