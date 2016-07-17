using System.Collections.Generic;
using Akka.Actor;
using Akka.Distributed.Actors;
using System;
using System.Threading;

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



            Console.ReadKey();

            client.Tell(new WidgetManagerClientActor.Subscribe(10));




            Console.ReadKey();

            var cluster = Cluster.Cluster.Get(ClusterSystem);
            
            cluster.Leave(cluster.SelfAddress);

            Thread.Sleep(30000);

            ClusterSystem.Terminate().Wait();

            Console.WriteLine("Stopped. Press any key!");

            Console.ReadKey();
        }
    }
}
