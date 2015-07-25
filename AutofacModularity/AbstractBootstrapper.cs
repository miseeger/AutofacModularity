using Autofac;
using AutofacModularity.Interfaces;

namespace AutofacModularity
{

    public abstract class AbstractBootstrapper : IRunnable
    {

        protected virtual void ConfigureContainer(ContainerBuilder builder)
        {
        }
        
        protected virtual void RegisterShell(ContainerBuilder builder)
        {
        }
        
        protected virtual void PostConfigureContainer(IContainer container)
        {
        }
        
        protected virtual void RunAsShell(IContainer container)
        {
        }

		public void Run(string[] args)
        {
            var builder = new ContainerBuilder();
            
            RegisterShell(builder);
            ConfigureContainer(builder);
            
            var container = builder.Build();
            
            PostConfigureContainer(container);
            
            if (container.IsRegistered<IShell>())
            {
            	var shell = container.Resolve<IShell>();
            	if (shell != null) 
            	{
            		shell.Run(args);
           		 }
            }
            else
            {
            	DiRepository.Instance.Container = container;
            	RunAsShell(container);
            }
        }

    }

}