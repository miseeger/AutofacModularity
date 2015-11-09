# AutofacModularity

[Autofac](http://autofac.org/) is a well maintained IoC library that covers the 
structural aspects of modularity and it also has extensions for ASP.NET MVC and
WebAPI. This small library is aimed towards modularity and provides functionality
that allows to pack vorious software parts (services) into modules to use them 
in a decoupled manner. It also enhances the module scanning capability and makes 
module configuration easier using an application's config file. 

There is a sample-/boilerplate-[solution template for Visual Studio 2015](https://github.com/miseeger/VisualStudio.Templates/blob/master/Project%20Templates/Autofac%20Multi%20Application%20Solution.zip)
and an older [solution template for SharpDevelop 5](https://github.com/miseeger/SharpDevelop.Templates/blob/master/project/CSharp/CSharp.AutofacModularityProject.xpt). 
Both provide common functionality (services) and a scaffolding plus working examples 
for a modular multi application framework which extensively uses `AutofacModularoity`.

## NuGet

 **Current Version: 3.5.2**
 
 Get it from [nuget.org/packages/AutofacModularity](https://www.nuget.org/packages/AutofacModularity) 
 or via  Package Manager Console.
 
  > *PM> Install-Package AutofacModularoity*

# Modular architecture

A modular archictecture keeps the functionality of certain problem domains or use cases in 
reusable modules. By using [Autofac's modularity](http://autofac.readthedocs.org/en/latest/configuration/modules.html) 
it is possible to put modules together from various libraries providing the services needed. 
If needed, those services can then be configured from settings made in the application's 
config-file.

Here's the "big picture" of the multi-host solution, provided by the solution created
from the VS 2015 template, mentioned above. The following sections describe the functionality 
of the `AutofacModularity` library based on it as an example.

![Modular architecture in a multi-host solution](http://abload.de/img/afmultiarch_ensvl03.png "Modular architecture in a multi-host solution")    

# Usage

The best start is to have an Autofac multi-host application generated as follows:
- get the template for VS 2015
- get the [RT package for Crystal Reports](https://github.com/miseeger/VisualStudio.Templates/blob/master/Packages/CrystalReportsRT_XIII_64.13.0.2.9.nupkg) and put it into a 
- create a new solution based on the template
- save solution.sln in the proper solution folder (it's created by VS in the "root" of your projects folder)
  - In Windows Explorer: delete the .vs folder and the .sln-file from the "root"
- restore the NuGet packages in solution
- build the solution

It may be necessary to reopen the solution in order to get rid of "false positive" errors shown.

## Modules
Using AutofacModularity you can create Autofac modules with a certain kind of magic: Values of
Defined properties are retrieved from the config-file of the app which uses the module.

One module is __one__ assembly. The Name of the module class is to be suffixed with the
wore `Module`. This is important for the assignment process for properties while loading
the module. 

A new module has to be derived from `ConfigurableModule` and overrides the `Load` method.
All registration will be done here.

In this implementation only the service classes decorated with the `RegisterServiceAttribute`
will definitely be registered.
 
```csharp
	public class CommonServicesModule : ConfigurableModule
    {

        public string DbActiveConnection { get; set; }
        public string Option { get; set; }
        public string ReportPath { get; set; }
        public string ReportOutputPath { get; set; }


        // Load common service assemblies and register services in the DI Container.
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // register common services
            builder.RegisterAssemblyTypes(Assembly.LoadFrom(@".\AfMulti.Common.Services.dll"))
                .Where(t => t.GetCustomAttributes(typeof(RegisterServiceAttribute), false).Any())
                .AsSelf()
                .AsImplementedInterfaces()
                .WithParameters(
                    new[]
                    {
                        new NamedParameter("reportOutputPath", ReportOutputPath),
                        new NamedParameter("reportPath", ReportPath),
                        new NamedParameter("option", Option)
                    })
                .SingleInstance();

            // register common data services
            builder.RegisterAssemblyTypes(Assembly.LoadFrom(@".\AfMulti.Common.Data.dll"))
                .Where(t => t.GetCustomAttributes(typeof(RegisterServiceAttribute), false).Any())
                .AsSelf()
                .AsImplementedInterfaces()
                .WithParameters(
                    new[]
                    {
                        new NamedParameter("dbActiveConnection", DbActiveConnection),
                        new NamedParameter("option", Option)
                    })
                .SingleInstance();
        }

    }	
```

The Load method will initially prepare the environment and set up the values of the Properties,
declared for the module. The base functionality was extended accordingly:

```csharp
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
	            
				if (property != null)
	            {
					property.SetValue(this, Convert.ChangeType(value, property.PropertyType), null);    
	            }
				else
				{
					Debug.WriteLine(String.Format("Could not find Property {0} in {1}", 
						propertyName, propertyKey.Split('.')[0] + "Module"));
				}
            }
        }

    }
```

This convention-based configuration of modules follows these rules:
- all property values are registered in the config-file of the Application
- the key for a module property consists of two segments, divided by a dot
  - the Module's name (without the `Module`-suffix) 
  - the name of the Property used in the Module to hold the value. 
- the segments must be identical to the names in the code.

```xml
    <appSettings>
        <!-- Active Database Connections -->    
        <add key="CommonServices.DbActiveConnection" value="DevDbConnection" />

        <!-- Option -->
        <add key="CommonServices.Option" value="An option from app.config" />
    
        <!-- Reporting -->
        <add key="CommonServices.ReportOutputPath" value=".\Reporting\Output" />
    	<add key="CommonServices.reportPath" value=".\Reporting\Templates" />
    </appSettings>        
```

So all services registered in the DI container will get their needed parameteres injected.
   
## Plugins

Plugins are single Modules, also derived from `ConfigurableModule` that contain special 
functionality to be plugged in to an application. This may be a data check task as described 
here. The Plugin class implements the IPlugin interface so that it can be started via 
a `Run` method. Since it is derived from `ConfigurableModule`, it is also configurable like a
"regular" module.

The module functionality is called from the `Plugin` class. The Run starts the plugin's 
functionality and may also be provided with an arguments array to specify the functionality
to be run.

```csharp
    public class CheckDataPlugin : IPlugin
    {

        private String Option { get; set; }

        public CheckDataPlugin(String option)
        {
            Option = option;
        }

        public void Run(string[] args)
        {
            Console.WriteLine("CheckData-Plugin: ready ({0})", Option);
        }

    }
```

The plugin will be packed into a `ConfigurableModule` and registered by its name.
This is important because it's not possible to register it by interface since one is 
created. But to keep it simple the registration is done by name. Contained in a 
module it is now possible to configure the plugin from the app.config as described 
above.

```csharp
    public class CheckDataModule : ConfigurableModule
    {

        public string Test { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder
                .RegisterType<CheckDataPlugin>()
                .Named<IPlugin>("CheckDataPlugin")
                .WithParameter("test", Test);
        }

    }
```

The configuration for this plugin module:

```xml
	<!-- CheckData-Plugin -->
    <add key="CheckData.Option" value="CheckData-Parameter from app.config" />
```    

## WebApi

It is also possible to provide WebAPI controllers and OWIN Middleware belonging to that
API with a configurable module. Just put the controllers in a module assembly, create a module
class derived from `ConfigurableModule`, register the services needed by the controllers, 
add some Middleware and go.

A Controller, which is a plain ASP.NET `ApiController` is created the regular way but has to
be decorated with the `AutofacControllerConfigurationAttribute`. This one also gets the 
registered business service injected.

```csharp
    [AutofacControllerConfiguration]
    [RoutePrefix("api/values")]
    public class ValuesController : ApiController
    {

        private IValueBusinessService _businessService;


        public ValuesController(IValueBusinessService businessService)
        {
            _businessService = businessService;
        }


        [Route("all")]
        [HttpGet]
        public HttpResponseMessage AllValues()
        {
            Console.WriteLine("Querying all values");
            var result = _businessService.GetStringValues();
            return result.Any()
                ? Request.CreateResponse(HttpStatusCode.OK, result)
                : Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    String.Format("No values found."));
        }
        
        // ... further endpoints ...
    }
```

If a special [Middleware](http://docs.autofac.org/en/latest/integration/owin.html)
is needed (e. g. for a special kind of authorization) it is possible to create it 
like so. It is possible to inject services from the regstiered modules:

```csharp
    public class AuthMiddleware : OwinMiddleware
    {

        private ILoggingService _logger;
        private IDbDataService _dbData;


        public AuthMiddleware(OwinMiddleware next, ILoggingService logger,
            IDbDataService dbData) : base(next)
        {
            _logger = logger;
            _dbData = dbData;

            _logger.SetName("AfMulti.Modules.WebApis.Value");
        }


        public override async Task Invoke(IOwinContext context)
        {

            // the most important part that makes the Middleware ONLY acting
            // for the Value API's endpoints
            if (!context.Request.Path.Value.ToLower().StartsWith("/api/value/"))
                await Next.Invoke(context);

            // ... the logic follows here ... omitted for simplicity
            
            // responding on result
            if (authSuccessfull) 
            {
                await Next.Invoke(context);
            }
            else
            {
                var response = context.Response;
                response.OnSendingHeaders(state =>
                {
                    var resp = (OwinResponse)state;
                    resp.StatusCode = 403;
                    resp.ReasonPhrase = "Forbidden";
                }, response);
            }
            
        }

    }
```

All the API functionality is then registered with the module:

```csharp
public class ValueWebApiModule : ConfigurableModule
    {

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // register the needed (business-)services
            builder.RegisterAssemblyModuleFromFile(
                @".\AfMulti.Modules.Common.Services.dll",
                (f) => System.Console.WriteLine("      {0}", f));

            builder.RegisterAssemblyModuleFromFile(
                @".\AfMulti.Modules.Domain.Value.dll",
                (f) => System.Console.WriteLine("      {0}", f));

            // register the controllers
            builder
                .RegisterApiControllers(Assembly.GetExecutingAssembly());

            // register OWIN middleware
            builder
                .RegisterType<AuthMiddleware>().InstancePerRequest();

        }

    }
```

It is also possible to configure this WebAPI module since it is also derived from
`ConfigurableModule`.    

## Bootstrapping

All the modules are pretty nice to use separately, to externally configure logic and create 
your own blend like creating a LEGO thingy. But like LEGO bricks everything has to be 
put together (wired-up) and put to work. All this is done in the bootstrapping process. 
A UI or Console Application use a bootstrapping class to configure DI and a Shell to put 
everything to work. A self-hosted OWIN WebAPI is bootstrapped from its Startup class as usual.

### Windows application

#### Bootstrapper (AbstractBootstrapper) 

The bootstrapping class which is instaciated in and run from the `Main` method of the 
application is derived from the (abstract) class `AbstractBootstrapper`. This class 
determines a certain process. All steps of this process may be specified by overriding the 
appropreate method.

The bootstrap sequence is started by calling the `Run` metod of the bootstrapper object
and is as follows:

1. `RegisterShell()`
2. `ConfigureContainer()`
3. *Autofac container is built*
4. `PostConfigureContainer()`
5. *Resolves and runs the Shell - If no Shell is registered call `RunAsShell()`*

```csharp
    public class ConsoleBootstrapper : AbstractBootstrapper
    {
        public string[] Args { get; set; }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            System.Console.Write("TaskRunner - configuring plugins ...");

            builder.RegisterAssemblyModuleFromFile(
                @".\AfMulti.Modules.Common.Services.dll");

            builder.RegisterAssemblyModulesFromDirectory(
                @".\", "AfMulti.Modules.Plugins", "~");

            System.Console.WriteLine(" done.");
        }


        protected override void PostConfigureContainer(IContainer container)
        {
            if (Args != null && Args.Length > 0)
            {
                var infoService = container.Resolve<IGlobalInfoService>();
                infoService["Args"] = Args;
            }
        }


        protected override void RegisterShell(ContainerBuilder builder)
        {
            builder.RegisterType<Shell>().As<IShell>();
        }

    }
```

Configuring the Autofac container in the bootstrapper adds/registers the needed 
modules with the application.

Sometimes it is needed to post-configure the container when it is completely built also
for this purpose there is a possibility to act.

#### Shell (IShell)

Running the `Shell` runs the main logic of the application. Inside the `Run` method of
the `Shell`, the application is started eventually.

By registering the `Shell` (as `IShell`) with the Autofac container it will be possible
to inject services registered in the container into the shell in order to use their 
logic from here.

```csharp
    public class Shell : IShell
    {

        private IComponentContext _container;
        private IDbDataService _dataService;
        private ILoggingService _logger;


        public Shell(IComponentContext container, IDbDataService dbDataservice,
            ILoggingService logger)
        {
            _container = container;
            _dataService = dbDataservice;
            _logger = logger;
            _logger.SetName("AfMulti.Apps.Console.Modularity");
        }


        public void Run(string[] args)
        {

            System.Console.WriteLine("\nRegistered Components ({0}):",
                _container.ComponentRegistry.Registrations.Count());

            foreach (var entry in _container.ComponentRegistry.Registrations)
            {
                System.Console.WriteLine("   {0}", entry.ToString().Split(',')[0]);
                _logger.Log(LogLevel.Info, String.Format("Registered: {0}", entry.ToString().Split(',')[0]));
            }

            System.Console.WriteLine("\nFrom DataService:");
            _dataService.Hello();

            System.Console.WriteLine("\nRunning CheckDataPlugin:");
            var cr = _container.ResolveNamed<IPlugin>("CheckDataPlugin");
            cr.Run(args);

            try
            {
                System.Console.WriteLine("\nRunning ImportDataPlugin:");
                var ir = _container.ResolveNamed<IPlugin>("ImportDataPlugin");
                ir.Run(args);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
            }

            System.Console.Write("\nPress any key to continue . . . ");
            System.Console.ReadKey(true);

        }

    }
```

### WebApi
 
An OWIN WebAPI is bootstrapped in its `Startup` class.
```csharp
public class Startup
    {

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            var builder = new ContainerBuilder();
            
            // ... some config code ...

            // this configures the Autofac container 
            app.RegisterDependencies(ref config, ref builder);

            // continue OWIN pipeline (Web API)
            app.UseWebApi(ref config);
            app.UseWelcomePage();
        }

    }
```

The `RegisterDependencies` method is an extension method for the `IAppBuilder` which
calls the `DependencyConfig.Register` that finally does all the Autofac configuration

```csharp
    public static void Register(ref IAppBuilder app, ref HttpConfiguration config, ref ContainerBuilder builder)
    {

        // Register modules
        builder.RegisterAssemblyModuleFromFile(@".\AfMulti.Modules.Common.Services.dll",
            (f) => System.Console.WriteLine("   {0}", f));

        Console.WriteLine("\r\nLoading WebApis from Directory ...");
        builder.RegisterAssemblyModulesFromDirectory(@".\", "AfMulti.Modules.WebApis", "~",
            (f) => System.Console.WriteLine("   {0}", f));

        // Register Web API controller in executing assembly.
        builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

        var container = builder.Build();

        // Create and assign a dependency resolver for Web API to use.
        config.DependencyResolver =
            (IDependencyResolver)new AutofacWebApiDependencyResolver(container);

        // This should be the first middleware added to the IAppBuilder.
        app.UseAutofacMiddleware(container);

        // Make sure the Autofac lifetime scope is passed to Web API.
        app.UseAutofacWebApi(config);

    }
```

Autofac will automatically add Middleware classes to IAppBuilder in the order they are 
registered. The Middleware will then execute in the order added to IAppBuilder.
 
# License
Licensed under the MIT license.

