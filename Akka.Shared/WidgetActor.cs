using Akka.Actor;
using System;
using System.Collections.Generic;

namespace Akka.Shared
{
    public class WidgetActor : ReceiveActor
    {
        private readonly Dictionary<IActorRef, ICancelable> _subscribers = new Dictionary<IActorRef, ICancelable>();
        private IActorRef _positionKeper;

        private List<string> _list = new List<string>();

        protected override void PreStart()
        {
            _positionKeper = Context.ActorOf<PositionKeeperActor>();
            _positionKeper.Tell(new PositionKeeperActor.Subscribe(0));
        }

        public WidgetActor(string s)
        {
            
            Console.WriteLine("Created " + s);

            Receive<Subscribe>(subscribe =>
            {
                Console.WriteLine("new subscriber here for " + s);

                if (!_subscribers.ContainsKey(Sender))
                {
                    var cancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Sender, "ping from " + s, Self);
                    _subscribers.Add(Sender, cancel);

                    Context.Watch(Sender);
                }
                else
                {
                    Console.WriteLine("Already subscribed " + s);
                }
            });

            Receive<Terminated>(terminated =>
            {
                Console.WriteLine("Subscriber died");

                DeleteSubscriber(terminated.ActorRef);
            });

            Receive<Unsubscribe>(unsubscribe =>
            {
                Console.WriteLine("Subscriber wants to stop");
                DeleteSubscriber(Sender);
            });

            Receive<string>(s1 =>
            {
                Console.WriteLine("Update from position keeper " + s1);
                _list.Add(s1);
            });
        }

        private void DeleteSubscriber(IActorRef subscriber)
        {
            ICancelable cancelable;
            if (_subscribers.TryGetValue(subscriber, out cancelable))
            {
                cancelable.Cancel();
                Context.Unwatch(subscriber);
                _subscribers.Remove(subscriber);
            }
        }

        public class Subscribe
        {

        }

        public class Unsubscribe
        {

        }
    }
}
