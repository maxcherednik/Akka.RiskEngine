using Akka.Actor;
using System;
using Akka.Shared;

namespace Akka.RiskEngine
{
    class RiskEngineService
    {
        private static ActorSystem ActorSystem { get; set; }

        private IActorRef _widgetManager;

        public void Start()
        {
            Console.WriteLine("Starting risk engine...");

            ActorSystem = ActorSystem.Create("riskengine");

            _widgetManager =ActorSystem.ActorOf(Props.Create(() => new WidgetManagerActor(4)), "widgetmanager");

            Console.WriteLine("Started!");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping...");

            var stopTask = _widgetManager.GracefulStop(TimeSpan.FromSeconds(5));

            if (stopTask.Result)
            {
                
            }

            ActorSystem.Terminate().Wait();

            Console.WriteLine("Stopped!");
        }
    }
}
