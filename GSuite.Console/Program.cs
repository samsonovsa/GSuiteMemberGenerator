using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSuite.Libs.Config;
using GSuite.Libs.Interface;
using Autofac;

namespace GSuite.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            
 
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterModule<ConfigIoCModule>();
            IContainer container = builder.Build();

            var generator = container.Resolve<IGSuiteDataGenerator>();
          //  string path = AppDomain.CurrentDomain.BaseDirectory;

            generator.SetGropsFileName("");
            generator.SetUsersFileName("");
            try
            {
                generator.GenerationPercentComplete += Generator_GenerationPercentComplete;
                generator.GenerateAsync().Wait();
            }
            catch (Exception e)
            {

                System.Console.WriteLine(e.InnerException.Message) ;
            }



            //IConfiguration _config = new Configuration();
            //string result = _config.GetURL();


            System.Console.WriteLine("The programm complited");
            System.Console.Read();


            

        }

        private static void Generator_GenerationPercentComplete(object sender, int e)
        {
            System.Console.Write("Event: {0}",e);
        }
    }
}
