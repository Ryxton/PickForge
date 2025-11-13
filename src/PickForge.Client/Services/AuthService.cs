using Microsoft.JSInterop;

namespace PickForge.Client.Services;

public class AuthService
{
    private const string AuthKey = "pickforge_authenticated";
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;

    public AuthService(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AuthKey);
            return value == "true";
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync(string password)
    {
        var correctPassword = _configuration["AppPassword"] ?? "Montoya";
        
        if (password == correctPassword)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthKey, "true");
            return true;
        }
        
        return false;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthKey);
    }
}