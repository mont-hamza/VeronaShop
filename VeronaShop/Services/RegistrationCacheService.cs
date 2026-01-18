using System;
using Microsoft.Extensions.Caching.Memory;
using VeronaShop.Models;

namespace VeronaShop.Services
{
    public class RegistrationCacheService
    {
        private readonly IMemoryCache _cache;

        public RegistrationCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string StoreRegistration(RegisterModel model)
        {
            // Generate a 6-digit code
            var code = new Random().Next(100000, 999999).ToString();
            // Key by email
            _cache.Set($"Reg_{model.Email}", (model, code), TimeSpan.FromMinutes(15));
            return code;
        }

        public (RegisterModel Model, string Code)? GetRegistration(string email)
        {
            if (_cache.TryGetValue($"Reg_{email}", out (RegisterModel, string) entry))
            {
                return entry;
            }
            return null;
        }

        public void RemoveRegistration(string email)
        {
            _cache.Remove($"Reg_{email}");
        }
    }
}
