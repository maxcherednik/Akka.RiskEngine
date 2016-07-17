using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using Akka.Util.Internal;

namespace Akka.Shared
{
    public class WidgetManagerActor : ReceiveActor
    {
        private readonly int _minNumberOfWidgetHolders;
        private readonly Dictionary<long, IActorRef> _widgets = new Dictionary<long, IActorRef>();
        private IActorRef _widgetHolderRouter;
        private ILoggingAdapter _logger;

        private readonly HashSet<long> _initRequests = new HashSet<long>();


        protected override void PreStart()
        {
            _logger = Context.GetLogger();
            _widgetHolderRouter = Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "widgetsRouter");

            _widgetHolderRouter.Tell(new GetRoutees());
        }

        public WidgetManagerActor(int minNumberOfWidgetHolders)
        {
            _minNumberOfWidgetHolders = minNumberOfWidgetHolders;
            Initializing();
        }

        private void Initializing()
        {
            Receive<Routees>(routees =>
            {
                var routeesNumber = routees.Members.ToList().Count;
                if (routeesNumber >= _minNumberOfWidgetHolders)
                {
                    _logger.Info("Widget holders are here: {0}. Let's create widgets", routeesNumber);

                    // here we need to create widgets

                    for (var i = 0; i < 20; i++)
                    {
                        _widgetHolderRouter.Tell(new WidgetHolderActor.CreateWidget(i));
                        _initRequests.Add(i);
                    }
                }
                else
                {
                    _logger.Info("Not enough widget holders. {0} out of {1} are ready. Let's wait for 3 seconds", routeesNumber, _minNumberOfWidgetHolders);

                    // let's try in 3 seconds
                    Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(3), _widgetHolderRouter, new GetRoutees(), Self);
                }
            });

            Receive<WidgetHolderActor.WidgetCreated>(created =>
            {
                if (_initRequests.Contains(created.WidgetId))
                {
                    _initRequests.Remove(created.WidgetId);
                    _widgets.Add(created.WidgetId, created.WdgetActorRef);
                    Context.Watch(created.WdgetActorRef);
                }

                if (_initRequests.Count == 0)
                {
                    Become(Ready);

                    SetReceiveTimeout(null);
                }
            });

            Receive<ReceiveTimeout>(timeout =>
            {
                Console.WriteLine("WidgetCreation timeout");

                if (_initRequests.Count > 0)
                {
                    // issue create command again
                    _initRequests.ForEach(i => _widgetHolderRouter.Tell(new WidgetHolderActor.CreateWidget(i)));
                }
            });

            ReceiveAny(o =>
            {
                Sender.Tell("Initializing. Try a bit a later...");
                Console.WriteLine("Busy");
            });
        }

        private void Ready()
        {
            Receive<Subscribe>(subscribe =>
            {
                Console.WriteLine("Someone wants to subscribe " + subscribe.WidgetId);

                IActorRef widget;
                if (_widgets.TryGetValue(subscribe.WidgetId, out widget))
                {
                    widget.Tell(new WidgetActor.Subscribe(), Sender);
                }
                else
                {
                    
                }
            });

            Receive<Terminated>(terminated =>
            {
                Console.WriteLine("Widget died " + terminated);
                var widgetRefToBeRemovedPair = _widgets.FirstOrDefault(@ref => @ref.Value.Equals(terminated.ActorRef));

                _widgets.Remove(widgetRefToBeRemovedPair.Key);

                _widgetHolderRouter.Tell(new WidgetHolderActor.CreateWidget(widgetRefToBeRemovedPair.Key));

            });
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

