
using Akka.Actor;
using System.Collections.Generic;

namespace Akka.Shared
{
    public class WidgetHolderActor : ReceiveActor
    {
        private readonly Dictionary<long, IActorRef> _widgets = new Dictionary<long, IActorRef>();

        public WidgetHolderActor()
        {
            Receive<CreateWidget>(widget =>
            {
                IActorRef widgetActorRef;
                if (!_widgets.TryGetValue(widget.WidgetId, out widgetActorRef))
                {
                    widgetActorRef = Context.ActorOf(Props.Create(() => new WidgetActor("w" + widget.WidgetId)), "w" + widget.WidgetId);
                }

                Sender.Tell(new WidgetCreated(widget.WidgetId, widgetActorRef));
            });
        }

        public class CreateWidget
        {
            public long WidgetId { get; private set; }

            public CreateWidget(long widgetId)
            {
                WidgetId = widgetId;
            }
        }

        public class WidgetCreated
        {
            public long WidgetId { get; private set; }
            public IActorRef WdgetActorRef { get; private set; }

            public WidgetCreated(long widgetId, IActorRef wdgetActorRef)
            {
                WidgetId = widgetId;
                WdgetActorRef = wdgetActorRef;
            }
        }
    }
}
