using Akka.Actor;
using System;
using Akka.Shared;
using log4net;
using System.Reflection;
using System.Threading;

namespace Akka.RiskEngine
{
    class RiskEngineService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName);

        private ActorSystem _actorSystem;

        private IActorRef _widgetManager;

        private readonly ManualResetEvent _asTerminatedEvent = new ManualResetEvent(false);

        public void Start()
        {
            log.Info("Starting risk engine...");

            _actorSystem = ActorSystem.Create("riskengine");

            //_widgetManager = _actorSystem.ActorOf(Props.Create(() => new WidgetManagerActor(2)), "widgetmanager");

            log.Info("Started!");
        }

        public void Stop()
        {
            log.Info("Stopping...");

            var cluster = Cluster.Cluster.Get(_actorSystem);
            cluster.RegisterOnMemberRemoved(() => MemberRemoved(_actorSystem));
            cluster.Leave(cluster.SelfAddress);

            _asTerminatedEvent.WaitOne();

            log.Info("Actor system terminated, exiting");
        }

        private async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();
            _asTerminatedEvent.Set();
        }
    }
}
