using System;

namespace AutofacModularity
{
    
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterServicePerDependencyAttribute : Attribute {}

}