using Akka.Actor;
using System.Collections.Generic;
using Akka.Event;

namespace Akka.Shared
{
    public class WidgetHolderActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        private readonly Dictionary<long, IActorRef> _widgets = new Dictionary<long, IActorRef>();

        public WidgetHolderActor()
        {
            Receive<CreateWidget>(widget =>
            {
                IActorRef widgetActorRef;
                if (!_widgets.TryGetValue(widget.WidgetId, out widgetActorRef))
                {
                    _logger.Info("Creating widget: {0}", widget.WidgetId);

                    widgetActorRef = Context.ActorOf(Props.Create(() => new WidgetActor("w" + widget.WidgetId)), "w" + widget.WidgetId);
                    _widgets.Add(widget.WidgetId, widgetActorRef);
                    Context.Watch(widgetActorRef);
                }
                else
                {
                    _logger.Info("Widget: {0} already created", widget.WidgetId);
                }

                Sender.Tell(new WidgetCreated(widget.WidgetId, widgetActorRef));
            });

            Receive<Terminated>(terminated =>
            {
                _logger.Info("Widget died {0}", terminated);

                var widgetIdToDelete = -1L;
                foreach (var widgetPair in _widgets)
                {
                    if (widgetPair.Value.Equals(terminated.ActorRef))
                    {
                        widgetIdToDelete = widgetPair.Key;
                        break;
                    }
                }
                if (widgetIdToDelete >= 0)
                {
                    if (_widgets.Remove(widgetIdToDelete))
                    {
                        _logger.Info("Widget: {0} removed from collection", widgetIdToDelete);
                    }
                }
            });
        }

        public class CreateWidget
        {
            public long WidgetId { get; }

            public CreateWidget(long widgetId)
            {
                WidgetId = widgetId;
            }
        }

        public class WidgetCreated
        {
            public long WidgetId { get; }
            public IActorRef WidgetActorRef { get; }

            public WidgetCreated(long widgetId, IActorRef widgetActorRef)
            {
                WidgetId = widgetId;
                WidgetActorRef = widgetActorRef;
            }
        }
    }
}
