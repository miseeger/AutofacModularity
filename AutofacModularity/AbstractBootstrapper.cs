using Autofac;
using AutofacModularity.Interfaces;

namespace AutofacModularity
{

    public abstract class AbstractBootstrapper : IRunnable
    {

        protected abstract void ConfigureContainer(ContainerBuilder builder);

        public void Run()
        {
            var builder = new ContainerBuilder();
            ConfigureContainer(builder);
			DiRepository.Instance.Container = builder.Build();
        }

    }

}