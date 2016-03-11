using System;
using System.Globalization;
using Akka.Actor;

namespace Akka.Shared
{
    public class PositionKeeperActor : ReceiveActor
    {
        private long _latestItemId = 100;

        private ICancelable _cancel;
        private IActorRef _subscriber;

        public PositionKeeperActor()
        {
            _cancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Self, "generate", Self);

            Receive<Subscribe>(subscribe =>
            {
                for (var i = subscribe.SinceId; i < _latestItemId; i++)
                {
                    Sender.Tell(i);
                }
                _subscriber = Sender;
            });

            Receive<string>(s => s == "generate", s =>
            {
                _latestItemId++;
                _subscriber.Tell(_latestItemId.ToString(CultureInfo.InvariantCulture));
            });
        }

        public class Subscribe
        {
            public long SinceId { get; private set; }

            public Subscribe(long sinceId)
            {
                SinceId = sinceId;
            }
        }

        public class Unsubscribe
        {

        }
    }
}
