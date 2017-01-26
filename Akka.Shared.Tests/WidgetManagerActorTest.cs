using System;
using System.Collections.Generic;
using System.Threading;
using Akka.Actor;
using Akka.TestKit.TestActors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Akka.Shared.Tests
{
    [TestClass]
    public class WidgetManagerActorTest : TestKit.VsTest.TestKit
    {
        [TestMethod]
        public void InitWidgetManagerActorWithZeroWidgets()
        {
            // setup
            var retryTimeout = TimeSpan.FromSeconds(1);

            var widgetHolderMock = CreateTestProbe();

            var widgetRepo = new Mock<IWidgetConfigurationProvider>();

            widgetRepo.Setup(provider => provider.GetAllAsync()).ReturnsAsync(new List<long>());


            // call
            var widgetManagerActor = Sys.ActorOf(Props.Create(() => new WidgetManagerActor(widgetHolderMock, widgetRepo.Object, retryTimeout)));


            // assert
            widgetHolderMock.ExpectNoMsg();
            ExpectNoMsg(0);

            widgetRepo.Verify(provider => provider.GetAllAsync(), Times.Once);

            Sys.Stop(widgetManagerActor);
        }

        [TestMethod]
        public void InitWidgetManagerActorWithSeveralWidgets()
        {
            // setup
            var retryTimeout = TimeSpan.FromSeconds(1);

            var widgetHolderMock = CreateTestProbe();

            var widgetRepo = new Mock<IWidgetConfigurationProvider>();

            widgetRepo.Setup(provider => provider.GetAllAsync()).ReturnsAsync(new List<long> { 0, 1, 2, 100 });


            // call
            var widgetManagerActor = Sys.ActorOf(Props.Create(() => new WidgetManagerActor(widgetHolderMock, widgetRepo.Object, retryTimeout)));


            //assert
            widgetHolderMock.ExpectMsg<WidgetHolderActor.CreateWidget>(widget => widget.WidgetId == 0);
            widgetHolderMock.Reply(new WidgetHolderActor.WidgetCreated(0, Sys.ActorOf<BlackHoleActor>()));

            widgetHolderMock.ExpectMsg<WidgetHolderActor.CreateWidget>(widget => widget.WidgetId == 1);
            widgetHolderMock.Reply(new WidgetHolderActor.WidgetCreated(1, Sys.ActorOf<BlackHoleActor>()));

            widgetHolderMock.ExpectMsg<WidgetHolderActor.CreateWidget>(widget => widget.WidgetId == 2);
            widgetHolderMock.Reply(new WidgetHolderActor.WidgetCreated(2, Sys.ActorOf<BlackHoleActor>()));

            widgetHolderMock.ExpectMsg<WidgetHolderActor.CreateWidget>(widget => widget.WidgetId == 100);
            widgetHolderMock.Reply(new WidgetHolderActor.WidgetCreated(100, Sys.ActorOf<BlackHoleActor>()));

            widgetHolderMock.ExpectNoMsg(0);
            ExpectNoMsg(0);

            widgetRepo.Verify(provider => provider.GetAllAsync(), Times.Once);

            Sys.Stop(widgetManagerActor);
        }

        [TestMethod]
        public void WhenWidgetConfigurationProviderFailsShouldRetry()
        {
            // setup
            var retryTimeout = TimeSpan.FromSeconds(1);

            var widgetHolderMock = CreateTestProbe();

            var widgetRepo = new Mock<IWidgetConfigurationProvider>();

            var autoResetEvent = new AutoResetEvent(false);

            widgetRepo.Setup(provider => provider.GetAllAsync()).ThrowsAsync(new Exception("Storage is not available")).Callback(() => autoResetEvent.Set());


            // call
            var widgetManagerActor = Sys.ActorOf(Props.Create(() => new WidgetManagerActor(widgetHolderMock, widgetRepo.Object, retryTimeout)));


            // assert
            var waitTime = TimeSpan.FromSeconds(10);
            const int numberOfRetries = 3;

            for (var i = 0; i < numberOfRetries; i++)
            {
                if (!autoResetEvent.WaitOne(waitTime))
                {
                    Assert.Fail("Retry didn't work within: " + waitTime);
                }
            }

            widgetHolderMock.ExpectNoMsg(0);
            ExpectNoMsg(0);

            widgetRepo.Verify(provider => provider.GetAllAsync(), Times.AtLeast(numberOfRetries));

            Sys.Stop(widgetManagerActor);
        }

        [TestMethod]
        public void IfNotInitialisedShouldRespondWithInitinilizing()
        {
            // setup
            var retryTimeout = TimeSpan.FromSeconds(1);

            var widgetHolderMock = CreateTestProbe();

            var widgetRepo = new Mock<IWidgetConfigurationProvider>();

            var autoResetEvent = new AutoResetEvent(false);

            widgetRepo.Setup(provider => provider.GetAllAsync()).Callback(() => autoResetEvent.WaitOne());


            // call
            var widgetManagerActor = Sys.ActorOf(Props.Create(() => new WidgetManagerActor(widgetHolderMock, widgetRepo.Object, retryTimeout)));


            // assert

            widgetManagerActor.Tell(new WidgetManagerActor.Subscribe(123));

            ExpectMsg<WidgetManagerActor.Initializing>();

            autoResetEvent.Set();

            Sys.Stop(widgetManagerActor);
        }
    }
}
