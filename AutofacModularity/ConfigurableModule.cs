using System;
using System.Configuration;
using System.Linq;
using Autofac;

namespace AutofacModularity
{
    public class ConfigurableModule : Module
    {
        
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            var settings = ConfigurationManager.AppSettings;
            var propertyKeys = settings.AllKeys;

            foreach (var propertyKey in propertyKeys.Where(pk => pk.Split('.')[0] + "Module" == GetType().Name))
            {
                var parts = propertyKey.Split('.');
                var propertyName = parts[1];
                var value = settings[propertyKey];

                var property = GetType().GetProperty(propertyName);
                property.SetValue(this, Convert.ChangeType(value, property.PropertyType), null);
            }
        }

    }

}