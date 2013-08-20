using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;

namespace AutofacModularity
{
    public static class BuilderExtensions
    {

        public static void RegisterAssemblyModulesFromFile(this ContainerBuilder builder, String loadFromFile)
        {
            builder.RegisterAssemblyModules<IModule>(Assembly.LoadFrom(loadFromFile));
        }


        public static void RegisterAssemblyModulesFromFile(this ContainerBuilder builder, String loadFromFile, 
            Action<String> regCallbackAction)
        {
            builder.RegisterAssemblyModules<IModule>(Assembly.LoadFrom(loadFromFile));

            if (regCallbackAction != null)
            {
                regCallbackAction.Invoke(loadFromFile);
            }
        }


        public static void RegisterAssemblyModulesFromDirectory(this ContainerBuilder builder, String directory, 
            String includingModulePrefix, String excludingModulePrefix)
        {
            var assemblyFileNames =
                Directory.GetFiles(directory, "*.dll")
                    .Where(
                        f =>
                            f.Contains(includingModulePrefix)
                            && !f.Contains(excludingModulePrefix)
                            && f.EndsWith(".dll")).ToList();

            foreach (var assemblyFileName in assemblyFileNames)
            {
                builder.RegisterAssemblyModulesFromFile(assemblyFileName);
            }

        }


        public static void RegisterAssemblyModulesFromDirectory(this ContainerBuilder builder, String directory,
            String includingModulePrefix, String excludingModulePrefix, Action<String> regCallbackAction)
        {
            var assemblyFileNames =
                Directory.GetFiles(directory, "*.dll")
                    .Where(
                        f =>
                            f.Contains(includingModulePrefix)
                            && !f.Contains(excludingModulePrefix)
                            && f.EndsWith(".dll")).ToList();

            foreach (var assemblyFileName in assemblyFileNames)
            {
                builder.RegisterAssemblyModulesFromFile(assemblyFileName);
                if (regCallbackAction != null)
                {
                    regCallbackAction.Invoke(assemblyFileName);
                }
            }

        }

    }

}