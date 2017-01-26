using Akka.Actor;
using System.Collections.Generic;
using Akka.Event;

namespace Akka.Shared
{
    public class WidgetActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();

        public WidgetActor(string s)
        {
            _logger.Info("Created {0}", s);

            Receive<Subscribe>(subscribe =>
            {
                _logger.Info("New subscriber for widget {0}", s);

                if (!_subscribers.Contains(Sender))
                {
                    _subscribers.Add(Sender);

                    Context.Watch(Sender);
                }
                else
                {
                    _logger.Info("Already subscribed {0}", s);
                }
            });

            Receive<Terminated>(terminated =>
            {
                _logger.Info("Subscriber died");

                DeleteSubscriber(terminated.ActorRef);
            });

            Receive<Unsubscribe>(unsubscribe =>
            {
                _logger.Info("Subscriber wants to unsubscribe");
                DeleteSubscriber(Sender);
            });
        }

        private void DeleteSubscriber(IActorRef subscriber)
        {
            Context.Unwatch(subscriber);
            _subscribers.Remove(subscriber);
        }

        public class Subscribe
        {

        }

        public class Unsubscribe
        {

        }
    }
}
