using System;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Testing;
using NSubstitute;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class BaseQueuePollingTest : BehaviourTest<JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected readonly AmazonSQS Sqs = Substitute.For<AmazonSQS>();
        protected readonly IMessageSerialiser<CustomerOrderRejectionSms> Serialiser = Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>();
        protected CustomerOrderRejectionSms DeserialisedMessage;
        protected const string MessageBody = "object";
        protected readonly IHandler<CustomerOrderRejectionSms> Handler = Substitute.For<IHandler<CustomerOrderRejectionSms>>();
        protected readonly IMessageSerialisationRegister SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        protected readonly IMessageFootprintStore MessageFootprintStore = Substitute.For<IMessageFootprintStore>();
        private readonly string _messageTypeString = typeof(CustomerOrderRejectionSms).ToString();
        protected int TestWaitTime = 20;

        protected override JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), SerialisationRegister, MessageFootprintStore);
        }

        protected override void Given()
        {
            var response = GenerateResponseMessage(_messageTypeString, Guid.NewGuid());

            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => response, x => new ReceiveMessageResponse());

            SerialisationRegister.GetSerialiser(_messageTypeString).Returns(Serialiser);
            DeserialisedMessage = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived);
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(Handler);
            SystemUnderTest.Listen();

            Thread.Sleep(TestWaitTime);

            SystemUnderTest.StopListening();
        }

        protected ReceiveMessageResponse GenerateResponseMessage(string messageType, Guid messageId)
        {
            return new ReceiveMessageResponse
            {
                ReceiveMessageResult = new ReceiveMessageResult
                {
                    Message = new[] {       
                            new Message
                                {   
                                    MessageId = messageId.ToString(),
                                    Body = "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}"
                                },
                            new Message
                                {
                                    MessageId = messageId.ToString(),
                                    Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," + "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                                }}.ToList(),
                }
            };
        }
    }
}