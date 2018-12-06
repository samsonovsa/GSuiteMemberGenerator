using Autofac;
using GSuite.Libs.Services.Interfaces;
using GSuite.Libs.Services;
using GSuite.Libs.Interface;


namespace GSuite.Libs.Config
{
    public class ConfigIoCModule: Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // optional: chain ServiceModule with other modules for going deeper down in the architecture: 
            // builder.RegisterModule<DataModule>();

            // builder.RegisterType<SerferService>().As<ISerferService>();
            builder.RegisterType<SerferSeleniumService>().As<ISerferService>();  
            builder.RegisterType<EntityReader>().As<IEntityReader>();
            builder.RegisterType<Configuration>().As<IConfiguration>();
            builder.RegisterType<Worker>().As<IWorker>()
                .WithParameter((pi, ctx) => pi.ParameterType == typeof(ISerferService) && pi.Name == "serfer",  //"SerferService",
                          (pi, ctx) => ctx.Resolve<ISerferService>());

            builder.RegisterType<GSuiteDataGenerator>().As<IGSuiteDataGenerator>();


        }
    }
}
