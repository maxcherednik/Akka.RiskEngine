using System.Collections.Generic;
using Akka.Actor;
using Akka.Distributed.Actors;
using System;

namespace Akka.Distributed
{
    class MainClass
    {
        private static ActorSystem ClusterSystem { get; set; }

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting risk client...");

            ClusterSystem = ActorSystem.Create("riskengine");

            Console.WriteLine("Started!");

            var client = ClusterSystem.ActorOf(Props.Create(() => new WidgetManagerClientActor()), "widgetmanagerclient");

            var rnd = new Random();
            while (true)
            {
                Console.ReadKey();
                var widgetId = rnd.Next(0, 49);
                client.Tell(new WidgetManagerClientActor.Subscribe(widgetId));
            }
            

            Console.ReadKey();

            ClusterSystem.Terminate().Wait();

            Console.WriteLine("Stopped. Press any key!");

            Console.ReadKey();
        }
    }
}
