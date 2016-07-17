using System;
using System.Threading;
using Akka.Actor;
using Akka.Shared;
using log4net;
using System.Reflection;

namespace Akka.WidgetHolder
{
    class WdgetHolderService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName);

        private ActorSystem _actorSystem;

        private IActorRef _widgetHolder;

        private readonly ManualResetEvent _asTerminatedEvent = new ManualResetEvent(false);

        public void Start()
        {
            _actorSystem = ActorSystem.Create("riskengine");

            _widgetHolder = _actorSystem.ActorOf(Props.Create(() => new WidgetHolderActor()), "widget");

            log.Info("Started");
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
