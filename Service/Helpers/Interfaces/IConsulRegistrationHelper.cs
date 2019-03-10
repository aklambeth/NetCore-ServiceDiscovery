using System;

namespace TestService.Helpers
{
    public interface IConsulRegistrationHelper
    {
        void AddService(Uri address);
        void Register();
    }
}