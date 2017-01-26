using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;

namespace Akka.Shared
{
    public class WidgetManagerActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        private readonly Dictionary<long, IActorRef> _widgets = new Dictionary<long, IActorRef>();

        private readonly IActorRef _widgetHolder;
        private readonly IWidgetConfigurationProvider _widgetConfigurationProvider;
        private readonly TimeSpan _storageNotAvailalableRetryPeriod;

        private readonly HashSet<long> _initRequests = new HashSet<long>();

        private ICancelable _creationCheckCancel;

        protected override void PreStart()
        {
            _logger.Info("Widget initialization");
            Self.Tell(new Init());
        }

        protected override void PostStop()
        {
            _creationCheckCancel?.Cancel();
            base.PostStop();
        }

        public WidgetManagerActor(IActorRef widgetHolder, IWidgetConfigurationProvider widgetConfigurationProvider, TimeSpan storageNotAvailalableRetryPeriod)
        {
            _widgetHolder = widgetHolder;
            _widgetConfigurationProvider = widgetConfigurationProvider;
            _storageNotAvailalableRetryPeriod = storageNotAvailalableRetryPeriod;

            ReadingWidgetConfigurations();
        }

        private void ReadingWidgetConfigurations()
        {
            Receive<Init>(init =>
            {
                _logger.Info("Lets request widget configurations");

                _widgetConfigurationProvider.GetAllAsync().PipeTo(Self);
            });

            Receive<List<long>>(list =>
            {
                _logger.Info("Widget configurations are here. Widget count: {0}", list.Count);

                list.ForEach(wid =>
                {
                    _initRequests.Add(wid);
                });

                _creationCheckCancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.Zero, TimeSpan.FromSeconds(3), Self, new CheckInit(), Self);

                Become(WidgetCreation);
            });

            Receive<Status.Failure>(failure =>
            {
                _logger.Warning("Widget configuration issue. We will retry in: {0}", _storageNotAvailalableRetryPeriod);

                Context.System.Scheduler.ScheduleTellOnce(_storageNotAvailalableRetryPeriod, Self, new Init(), Self);
            });

            Receive<Subscribe>(subscribe =>
            {
                _logger.Info("Someone wants to subscribe for widget: {0} I am initialising here. Hold on...", subscribe.WidgetId);
                Sender.Tell(new Initializing());
            });
        }

        private void WidgetCreation()
        {
            _logger.Info("Lets wait for widget initialization");

            Receive<WidgetHolderActor.WidgetCreated>(created =>
            {
                _logger.Info("Widget {0} created", created.WidgetId);

                if (_initRequests.Contains(created.WidgetId))
                {
                    _initRequests.Remove(created.WidgetId);
                    _widgets[created.WidgetId] = created.WidgetActorRef;
                    Context.Watch(created.WidgetActorRef);
                }
            });

            Receive<CheckInit>(timeout =>
            {
                _logger.Info("Widget initialization check");

                if (_initRequests.Count > 0)
                {
                    // issue create command again
                    _initRequests.ForEach(i => _widgetHolder.Tell(new WidgetHolderActor.CreateWidget(i)));

                    _logger.Info("Widget initialization check. Widgets left: {0} ", _initRequests.Count);
                }
                else
                {
                    if (_creationCheckCancel != null)
                    {
                        _creationCheckCancel.Cancel();
                        _creationCheckCancel = null;
                    }

                    _logger.Info("All widgets initilized");
                    Become(Ready);
                }
            });

            Receive<Terminated>(terminated =>
            {
                _logger.Warning("Widget died {0} during initialization. Not cool... Let's hadnle it once initialized", terminated);

                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5), Self, terminated, Self);
            });

            Receive<Subscribe>(subscribe =>
            {
                _logger.Info("Someone wants to subscribe for widget: {0} I am initialising here. Hold on...", subscribe.WidgetId);
                Sender.Tell(new Initializing());
            });
        }

        private void Ready()
        {
            Receive<Subscribe>(subscribe =>
            {
                _logger.Info("Someone wants to subscribe {0}", subscribe.WidgetId);

                IActorRef widget;
                if (_widgets.TryGetValue(subscribe.WidgetId, out widget))
                {
                    widget.Tell(new WidgetActor.Subscribe(), Sender);
                }
                else
                {
                    Sender.Tell(new WidgetNotFound());
                }
            });

            Receive<Terminated>(terminated =>
            {
                _logger.Warning("Widget died {0}", terminated);
                var widgetRefToBeRemovedPair = _widgets.FirstOrDefault(@ref => @ref.Value.Equals(terminated.ActorRef));

                _widgets.Remove(widgetRefToBeRemovedPair.Key);

                _initRequests.Add(widgetRefToBeRemovedPair.Key);
                _widgetHolder.Tell(new WidgetHolderActor.CreateWidget(widgetRefToBeRemovedPair.Key));

                if (_creationCheckCancel == null)
                {
                    _creationCheckCancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), Self, new CheckInit(), Self);
                }
            });

            Receive<CheckInit>(timeout =>
            {
                _logger.Info("Widget recovery check");

                if (_initRequests.Count > 0)
                {
                    // issue create command again
                    _initRequests.ForEach(i => _widgetHolder.Tell(new WidgetHolderActor.CreateWidget(i)));

                    _logger.Info("Widget recovery. Widgets left: {0} ", _initRequests.Count);
                }
                else
                {
                    if (_creationCheckCancel != null)
                    {
                        _creationCheckCancel.Cancel();
                        _creationCheckCancel = null;
                    }

                    _logger.Info("All widgets recovered");
                }
            });

            Receive<WidgetHolderActor.WidgetCreated>(created =>
            {
                _logger.Info("Widget {0} recovered", created.WidgetId);

                if (_initRequests.Contains(created.WidgetId))
                {
                    _initRequests.Remove(created.WidgetId);
                    _widgets[created.WidgetId] = created.WidgetActorRef;
                    Context.Watch(created.WidgetActorRef);
                }
            });

            Receive<Reinit>(reinit =>
            {
                _logger.Info("Seems like someone wants us to reinit all the widgets");
                _widgets.ForEach(pair => Context.Stop(pair.Value));
            });
        }

        public class Subscribe
        {
            public long WidgetId { get; }

            public Subscribe(long widgetId)
            {
                WidgetId = widgetId;
            }
        }

        public class WidgetNotFound
        {

        }


        public class Initializing
        {

        }

        public class Reinit
        {

        }

        private class Init
        {

        }

        private class CheckInit
        {

        }
    }
}

