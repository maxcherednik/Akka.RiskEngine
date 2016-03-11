using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using System;
using System.Linq;
using Akka.Shared;

namespace Akka.Distributed.Actors
{
    public class WidgetManagerClientActor : ReceiveActor
    {
        private ILoggingAdapter _logger;
        private IActorRef _widgetsManagerRouter;

        protected override void PreStart()
        {
            _logger = Context.GetLogger();
            _widgetsManagerRouter = Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "widgetsManagerRouter");
        }

        public WidgetManagerClientActor()
        {
            Receive<Subscribe>(subscribe =>
            {
                var membersCount = _widgetsManagerRouter.Ask<Routees>(new GetRoutees()).Result.Members.ToList().Count;
                Console.WriteLine("member count " + membersCount);
                _widgetsManagerRouter.Tell(new WidgetManagerActor.Subscribe(subscribe.WidgetId));
            });

            Receive<string>(s => _logger.Info(s));
        }


        public class Subscribe
        {
            public long WidgetId { get; private set; }

            public Subscribe(long widgetId)
            {
                WidgetId = widgetId;
            }
        }
    }

}

