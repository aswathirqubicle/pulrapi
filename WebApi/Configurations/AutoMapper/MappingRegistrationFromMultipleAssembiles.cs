using System.Reflection;
using ApplicationConfigureServices = Core.Application.ConfigureServices;
using DashboardApplicationConfigureServices = Dashboard.Application.ConfigureServices;

namespace WebApi.Configurations.AutoMapper
{
    public static class MappingRegistrationFromMultipleAssembiles
    {
        public static Assembly[] GetAssemblies() {

            Assembly appAssembly = typeof(ApplicationConfigureServices).Assembly;
            Assembly dashboardAppAssembly = typeof(DashboardApplicationConfigureServices).Assembly;

            return [appAssembly, dashboardAppAssembly];
        }
    }
}
