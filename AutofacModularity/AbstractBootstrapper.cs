using Autofac;
using AutofacModularity.Interfaces;

namespace AutofacModularity
{

    public abstract class AbstractBootstrapper : IRunnable
    {

        protected abstract void ConfigureContainer();

        public void Run()
        {
            var builder = new ContainerBuilder();
            ConfigureContainer();
			DiRepository.Instance.Container = builder.Build();
        }

    }

}