using Autofac;
using AutofacModularity.Interfaces;

namespace AutofacModularity
{

    public abstract class AbstractBootstrapper : IRunnable
    {

        protected abstract void ConfigureContainer(ContainerBuilder builder);
        protected abstract void RegisterShell(ContainerBuilder builder);

        public void Run()
        {
            var builder = new ContainerBuilder();
            
            ConfigureContainer(builder);
            RegisterShell(builder);
            
            var container = builder.Build();
            
            if (container.IsRegistered<IShell>())
            {
            	var Shell = container.Resolve<IShell>();
            	if (Shell != null) 
            	{
            		Shell.Run();
           		 }
            	
            }
            else
            {
            	DiRepository.Instance.Container = container;
            }
        }

    }

}