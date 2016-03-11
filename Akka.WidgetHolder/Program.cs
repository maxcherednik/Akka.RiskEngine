using System;
using Topshelf;

namespace Akka.WidgetHolder
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<WdgetHolderService>(s =>
                {
                    s.ConstructUsing(n => new WdgetHolderService());
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                    //continue and restart directives are also available
                });

                x.RunAsLocalSystem();
                x.UseAssemblyInfoForServiceInfo();
            });
        }
    }
}
