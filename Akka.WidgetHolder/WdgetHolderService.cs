using System;
using System.Threading;
using Akka.Actor;
using Akka.Shared;

namespace Akka.WidgetHolder
{
    class WdgetHolderService
    {
        private static ActorSystem ActorSystem { get; set; }

        private IActorRef _widgetHolder;

        public void Start()
        {
            ActorSystem = ActorSystem.Create("riskengine");

            _widgetHolder = ActorSystem.ActorOf(Props.Create(() => new WidgetHolderActor()), "widget");

            Console.WriteLine("Started!");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping...");

            var stopTask =_widgetHolder.GracefulStop(TimeSpan.FromSeconds(5));

            var stopped = stopTask.Result;
            
            var cluster = Cluster.Cluster.Get(ActorSystem);

            cluster.Leave(cluster.SelfAddress);

            Thread.Sleep(30000);

            ActorSystem.Terminate().Wait();

            Console.WriteLine("Stopped!");
        }
    }
}
