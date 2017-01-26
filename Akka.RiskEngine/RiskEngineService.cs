using System;
using Akka.Actor;
using log4net;
using System.Reflection;
using System.Threading;
using Akka.Routing;
using Akka.Shared;

namespace Akka.RiskEngine
{
    class RiskEngineService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType.FullName);

        private ActorSystem _actorSystem;

        private IActorRef _widgetManager;
        private IActorRef _clusterListener;
        private IActorRef _widgetHolder;
        private IActorRef _clusterRebalanceListener;

        private readonly ManualResetEvent _asTerminatedEvent = new ManualResetEvent(false);


        public void Start()
        {
            log.Info("Starting risk engine...");

            _actorSystem = ActorSystem.Create("riskengine");

            _clusterListener = _actorSystem.ActorOf<ClusterEventsListenerActor>("clusterListener");

            _widgetHolder = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "widgetsRouter");

            IWidgetConfigurationProvider widgetConfigurationProvider = new FakeWidgetConfigurationProvider();

            _widgetManager = _actorSystem.ActorOf(Props.Create(() => new WidgetManagerActor(_widgetHolder, widgetConfigurationProvider, TimeSpan.FromSeconds(5))), "widgetmanager");

            _clusterRebalanceListener = _actorSystem.ActorOf(Props.Create(() => new ClusterEventsListenerRebalanceActor(_widgetManager)), "clusterRebalanceListener");

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
