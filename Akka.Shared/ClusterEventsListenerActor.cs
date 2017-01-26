using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace Akka.Shared
{
    public class ClusterEventsListenerActor : UntypedActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly Cluster.Cluster _cluster = Cluster.Cluster.Get(Context.System);

        /// <summary>
        /// Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            // subscribe to IMemberEvent and UnreachableMember events
            _cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember));
        }

        /// <summary>
        /// Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            _cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            var up = message as ClusterEvent.MemberUp;
            if (up != null)
            {
                var mem = up;
                _log.Info("Member is Up: {0}", mem.Member);
            }
            else if (message is ClusterEvent.MemberJoined)
            {
                var memberJoined = (ClusterEvent.MemberJoined)message;
                _log.Info("Member is joining: {0}", memberJoined.Member);
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember)message;
                _log.Info("Member detected as unreachable: {0}", unreachable.Member);
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved)message;
                _log.Info("Member is Removed: {0}", removed.Member);
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}
