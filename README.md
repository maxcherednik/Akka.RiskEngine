
Few things covered in this example:

1. Absolute minimum to build an Akka cluster

  Here you can play with the cluster itself 
  - add node(start a new Akka.WidgetHolder copy)
  - crash node(close the Akka.WidgetHolder)
  - node recovery(restart the Akka.WidgetHolder node which you crashed) 
  - graceful stop(close the Akka.WidgetHolder with Ctrl+C)
  
2. Widget failover and work distribution logic:
  - When Akka.RiskEngine starts it creates N widgets which are automatically created on the available Akka.WidgetHolder node in a round-robin fasion.
  - When a new Akka.WidgetHolder node added the Akka.RiskEngine does widget rebalancing by destroying and recreating widgets
  - When a known Akka.WidgetHolder node gracefully goes down(Ctrl+C) the Akka.RiskEngine failover recreate widgets on the available Akka.WidgetHolder nodes
  - If there are no available Akka.WidgetHolder nodes, the Akka.RiskEngine keeps trying to recover the widgets
  
How to run:
 1. Akka.RiskEngine node serves as a seed node configured to run on localhost:4053
 2. Akka.WidgetHolder node serves as a widget container configured to run on localhost:4054(every new node sould use it's own static port)

  
