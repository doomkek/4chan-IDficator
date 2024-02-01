
using Serilog;

namespace IDificator;
public static class WebRequest
{
    public static async Task<T> GetRequestAsync<T>(string url)
    {
        using HttpClient client = new HttpClient();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, $"WebRequest exception: {ex.Message}");
            return default(T);
        }
    }
}