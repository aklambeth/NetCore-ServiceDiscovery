using System;

namespace TestWebApp.Helpers
{
    public interface IConsulRegistrationHelper
    {
        void AddService(Uri address);
        void Register();
    }
}