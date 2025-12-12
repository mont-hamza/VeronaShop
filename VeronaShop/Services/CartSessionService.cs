using Microsoft.JSInterop;
namespace VeronaShop.Services
{
    public class CartSessionService
    {
        private readonly IJSRuntime _js;
        private const string Key = "verona_cart_id";

        public CartSessionService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> GetOrCreateSessionIdAsync()
        {
            try
            {
                var id = await _js.InvokeAsync<string>("localStorage.getItem", Key);
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString();
                    await _js.InvokeVoidAsync("localStorage.setItem", Key, id);
                }
                return id;
            }
            catch
            {
                // Fallback server-side: generate id (won't persist across browser sessions)
                return Guid.NewGuid().ToString();
            }
        }
    }
}
