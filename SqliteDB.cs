using System.Data.SQLite;
using System.Text;
using Dapper;

namespace IDificator;
public class DB
{
    public bool IsDbInitialized => connection != null;
    readonly string dbName;
    readonly string initDbScript;
    protected readonly ILogger<DB> log;
    SQLiteConnection connection;
    SemaphoreSlim mutex;

    public DB(ILogger<DB> log, string dbName)
    {
        this.log = log;
        this.dbName = dbName + ".db";

        mutex = new SemaphoreSlim(1);

        initDbScript = """ 
CREATE TABLE "Shitposts" (
	"ThreadId"	INTEGER NOT NULL,
	"PostId"	INTEGER NOT NULL,
    "BoardId"   TEXT NOT NULL,
    "UserHash"  TEXT NOT NULL,
	UNIQUE("ThreadId", "PostId", "BoardID")
);

CREATE TABLE "ValidationQueue" (
    "BoardId"   TEXT NOT NULL,
    "ThreadId"  INTEGER NOT NULL,
    "PostId"    INTEGER NOT NULL,
    "ServerTS"  INTEGER NOT NULL,
    "Attempt"   INTEGER NOT NULL,
	UNIQUE("ThreadId", "PostId", "BoardID")
);
""";

        ensureDbInitialized();
    }

    public int Execute(string sql)
    {
        log.LogDebug(sql);
        return connection.Execute(sql);
    }

    public T QuerySingle<T>(string sql)
    {
        log.LogDebug(sql);
        return connection.QuerySingle<T>(sql);
    }

    public IEnumerable<T> Query<T>(string sql)
    {
        log.LogDebug(sql);
        return connection.Query<T>(sql);
    }

    public async Task<int> ExecuteAsync(string sql)
    {
        try
        {
            await mutex.WaitAsync();
            log.LogDebug(sql);
            return await connection.ExecuteAsync(sql);
        }
        finally
        {
            mutex.Release();
        }
    }

    public async Task<int> ExecuteAsync(string sql, object param)
    {
        try
        {
            await mutex.WaitAsync();
            //TODO: create message that will convert param = @param to param = 'value'
            log.LogDebug(sql, param);
            return await connection.ExecuteAsync(sql, param);
        }
        finally
        {
            mutex.Release();
        }
    }

    public async Task<T> QuerySingleAsync<T>(string sql)
    {
        try
        {
            await mutex.WaitAsync();
            log.LogDebug(sql);
            return await connection.QuerySingleOrDefaultAsync<T>(sql);
        }
        finally
        {
            mutex.Release();
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
    {
        try
        {
            await mutex.WaitAsync();
            log.LogDebug(sql);
            return await connection.QueryAsync<T>(sql, param);
        }
        finally
        {
            mutex.Release();
        }
    }

    string buildSqlCommand(string sql, string predicate = null, string orderBy = null, int? from = null, int? amount = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(sql);

        if (!string.IsNullOrEmpty(predicate))
            sb.Append(" where ").Append(predicate);
        if (!string.IsNullOrEmpty(orderBy))
            sb.Append(" order by ").Append(orderBy);
        if (from.HasValue)
            sb.Append(" limit ").Append(from.Value);
        if (amount.HasValue)
            sb.Append(", ").Append(amount.Value);

        string ret = sb.ToString();
        sb.Clear();
        return ret;
    }

    void ensureDbFileExists()
    {
        if (!File.Exists(dbName))
        {
            log.LogDebug("{dbName} file not found, creating...", dbName);
            File.Create(dbName).Close();
        }
        else
            log.LogDebug("{dbName} file found", dbName);
    }

    void ensureDbInitialized()
    {
        log.LogDebug("Looking for {dbName} file", dbName);
        ensureDbFileExists();

        var csBuilder = new SQLiteConnectionStringBuilder();
        csBuilder.DataSource = dbName;
        csBuilder.Version = 3; // its 3 by default, remove?
        csBuilder["Compress"] = "True"; // not sure about that, how does that affect performance?

        connection = new SQLiteConnection(csBuilder.ToString());

        string sqlCommand = @"SELECT COUNT(*) FROM SQLITE_MASTER WHERE TYPE='table'";
        SQLiteCommand cmd = new SQLiteCommand(sqlCommand, connection);

        try
        {
            log.LogDebug("Trying to open connection to {dbName} database", dbName);
            connection.Open();

            var resp = cmd.ExecuteScalar();
            if (resp == null || (long)resp == 0)
            {
                log.LogDebug("DB not initialized");
                initializeTables();
            }
        }
        catch { initializeTables(); }
        finally { connection.Close(); }
    }

    void initializeTables()
    {
        var cmd = new SQLiteCommand(initDbScript, connection);

        try
        {
            log.LogDebug("Trying to initialize DB {dbName} with script: {initDbScript}", dbName, initDbScript);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Failed to initialize DB: {exception}", ex.Message);
            throw new Exception("Failed to initialize DB: " + ex.Message);
        }
    }
}

public class ValidationQueue
{
    public string BoardId { get; set; }
    public int ThreadId { get; set; }
    public int PostId { get; set; }
    public int ServerTS { get; set; }
    public int Attempt { get; set; }

    public DateTime PostDateTime(int postTS) => DateTime.UnixEpoch.AddSeconds(postTS);
    public DateTime ServerDateTime => DateTime.UnixEpoch.AddSeconds(ServerTS);
    public string FullPostId => $"{BoardId}.{ThreadId}.{PostId}";
}
