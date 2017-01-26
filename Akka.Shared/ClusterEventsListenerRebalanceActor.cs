using Akka.Actor;
using Akka.Cluster;

namespace Akka.Shared
{
    public class ClusterEventsListenerRebalanceActor : ReceiveActor
    {
        private readonly IActorRef _listener;

        private readonly Cluster.Cluster _cluster = Cluster.Cluster.Get(Context.System);

        public ClusterEventsListenerRebalanceActor(IActorRef listener)
        {
            _listener = listener;
            Receive<ClusterEvent.MemberUp>(up =>
            {
                if (up.Member.HasRole("riskenginewidget"))
                {
                    _listener.Tell(new WidgetManagerActor.Reinit());
                }
            });
        }

        /// <summary>
        /// Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            // subscribe to IMemberEvent and UnreachableMember events
            _cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, typeof(ClusterEvent.MemberUp));
        }

        /// <summary>
        /// Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
        }
    }
}
