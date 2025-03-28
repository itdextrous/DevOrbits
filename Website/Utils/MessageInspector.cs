using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.IO;
using System.Xml;

namespace MyVoltage.Utils
{
    public class MessageInspector : IClientMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var buffer = reply.CreateBufferedCopy(Int32.MaxValue);
            reply = buffer.CreateMessage();

            Task.Run(() =>
            {
                this.LogMessage(buffer, false);
            });
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var buffer = request.CreateBufferedCopy(Int32.MaxValue);
            request = buffer.CreateMessage();

            Task.Run(() =>
            {
                this.LogMessage(buffer, false);
            });

            return null;
        }

        private void LogMessage(MessageBuffer buffer, bool isRequest)
        {
            var originalMessage = buffer.CreateMessage();
            string messageContent;

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
                {
                    originalMessage.WriteMessage(xmlTextWriter);
                    xmlTextWriter.Flush();
                    xmlTextWriter.Close();
                }
                messageContent = stringWriter.ToString();
            }

            // log messageContent to the database
        }

    }

}

