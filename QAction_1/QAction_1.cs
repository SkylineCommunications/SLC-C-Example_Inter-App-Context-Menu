namespace Skyline.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Scripting;
    using Skyline.Protocol.InterApp.API;

    public static class Generic
    {
        public static string GetSocialSecurityNumber()
        {
            return string.Join("_", Guid.NewGuid().ToString().Split('-').Take(2).ToList());
        }
    }

    public class ContextMenuHelper
    {
        private static readonly Dictionary<string, Action<SLProtocolExt, ContextMenuData>> ContextMenuExecutorMap = new Dictionary<string, Action<SLProtocolExt, ContextMenuData>>
        {
            { "1001", (protocol, data) => AddPerson(protocol, data) },
            { "1002", (protocol, data) => EditPerson(protocol, data) },
            { "1003", (protocol, data) => DeletePerson(protocol, data) },
        };

        public static void Execute(SLProtocolExt protocol, string[] data)
        {
            var contextMenuData = (ContextMenuData)data;

            protocol.Log($"ContextMenuData: {JsonConvert.SerializeObject(contextMenuData)}");

            if (!ContextMenuExecutorMap.ContainsKey(contextMenuData.Action))
            {
                throw new NotImplementedException($"ContextMenu Action");
            }

            ContextMenuExecutorMap[contextMenuData.Action].Invoke(protocol, contextMenuData);
        }

        private static void AddPerson(SLProtocolExt protocol, ContextMenuData data)
        {
            var addPersonMessage = new PersonsExposer.AddPersonMessage
            {
                FirstName = data.DataFields[0],
                LastName = data.DataFields[1],
                Gender = data.DataFields[2],
                Age = Convert.ToInt16(data.DataFields[3]),
            };

            InterAppHelper.TryExecuteMessage(protocol, addPersonMessage);
        }

        private static void EditPerson(SLProtocolExt protocol, ContextMenuData data)
        {
            var editPersonMessage = new PersonsExposer.EditPersonMessage
            {
                FirstName = data.DataFields[0],
                LastName = data.DataFields[1],
                Gender = data.DataFields[2],
                Age = Convert.ToInt16(data.DataFields[3]),
                Id = data.DataFields[4],
            };

            InterAppHelper.TryExecuteMessage(protocol, editPersonMessage);
        }

        private static void DeletePerson(SLProtocolExt protocol, ContextMenuData data)
        {
            var deletePersonMessage = new PersonsExposer.DeletePersonMessage
            {
                Id = data.DataFields[0],
            };

            InterAppHelper.TryExecuteMessage(protocol, deletePersonMessage);
        }

        public class ContextMenuData
        {
            public string Guid { get; set; }

            public string Action { get; set; }

            public int TableId { get; set; }

            public string[] DataFields { get; set; }

            public static explicit operator ContextMenuData(string[] data)
            {
                return new ContextMenuData
                {
                    Guid = data[0],
                    Action = data[1],
                    TableId = GetTableId(data[1]),
                    DataFields = data.Skip(2).ToArray(),
                };
            }

            public static int GetTableId(string action)
            {
                return (Convert.ToInt32(action) / 1000) * 1000;
            }
        }
    }
}

namespace Skyline.Protocol.InterApp.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Core.InterAppCalls.Common.MessageExecution;
    using Skyline.DataMiner.Scripting;

    public static class InterAppHelper
    {
        private static readonly List<Type> KnownTypes = new List<Type>
        {
            typeof(PersonsExposer),
            typeof(PersonsExposer.AddPersonMessage),
            typeof(PersonsExposer.EditPersonMessage),
            typeof(PersonsExposer.DeletePersonMessage),
            typeof(PersonsExposer.PersonReturnMessage),
        };

        private static readonly Dictionary<Type, Type> MessageToExecutorMapping = new Dictionary<Type, Type>
        {
            { typeof(PersonsExposer.AddPersonMessage), typeof(PersonsExposer.AddPersonExecutor) },
            { typeof(PersonsExposer.EditPersonMessage), typeof(PersonsExposer.EditPersonExecutor) },
            { typeof(PersonsExposer.DeletePersonMessage), typeof(PersonsExposer.DeletePersonExecutor) },
        };

        public static SLProtocolExt GetSLProtocol(object slProtocol)
        {
            try
            {
                var protocol = (SLProtocolExt)slProtocol;

                if (protocol == null)
                {
                    throw new ArgumentException("Expected argument of type SLProtocolExt");
                }

                return protocol;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Expected argument of type SLProtocolExt: {e}");
            }
        }

        public static void ParseInterAppCall(SLProtocolExt protocol)
        {
            string raw = Convert.ToString(protocol.GetParameter(protocol.GetTriggerParameter()));

            IInterAppCall receivedCall = InterAppCallFactory.CreateFromRaw(raw, KnownTypes);

            if (receivedCall == null)
            {
                protocol.Log("QA" + protocol.QActionID + "|Run|ERR: Value in Parameter was empty.", LogType.Error, LogLevel.NoLogging);

                return;
            }

            foreach (var message in receivedCall.Messages)
            {
                TryExecuteMessage(protocol, message);
            }
        }

        public static void TryExecuteMessage(SLProtocolExt protocol, Message message)
        {
            message.TryExecute(protocol, protocol, MessageToExecutorMapping, out Message returnMessage);

            protocol.Log($"Message: {JsonConvert.SerializeObject(message)}");

            protocol.Log($"ReturnMessage: {JsonConvert.SerializeObject(returnMessage)}");

            if (returnMessage.ReturnAddress == null)
                return;

            returnMessage?.Send(protocol.SLNet.RawConnection, message.ReturnAddress.AgentId, message.ReturnAddress.ElementId, message.ReturnAddress.ParameterId, new List<Type> { typeof(Message) });
        }
    }

    public class PersonsExposer
    {
        public class AddPersonMessage : Message
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Gender { get; set; }

            public int Age { get; set; }
        }

        public class EditPersonMessage : Message
        {
            public string Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Gender { get; set; }

            public int Age { get; set; }
        }

        public class DeletePersonMessage : Message
        {
            public string Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        public class AddPersonExecutor : MessageExecutor<AddPersonMessage>
        {
            private SLProtocolExt protocol;

            public AddPersonExecutor(AddPersonMessage message) : base(message)
            {
            }

            public override void DataGets(object dataSource)
            {
                protocol = InterAppHelper.GetSLProtocol(dataSource);
            }

            public override void Parse()
            {
            }

            public override void Modify()
            {
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DataSets(object dataDestination)
            {
                protocol = InterAppHelper.GetSLProtocol(dataDestination);

                var row = new PersonsQActionRow
                {
                    Personsindex_1001 = Generic.GetSocialSecurityNumber(),
                    Personsfirstname_1002 = Message.FirstName,
                    Personslastname_1003 = Message.LastName,
                    Personsgender_1004 = Message.Gender.ToString(),
                    Personsage_1005 = Message.Age,
                };

                protocol.persons.AddRow(row);
            }

            public override Message CreateReturnMessage()
            {
                return new PersonReturnMessage
                {
                    Guid = Message.Guid,
                    ReturnAddress = Message.ReturnAddress,
                    Source = Message.Source,
                    Message = "Succesfully Added New Person",
                };
            }
        }

        public class EditPersonExecutor : MessageExecutor<EditPersonMessage>
        {
            private SLProtocolExt protocol;

            public EditPersonExecutor(EditPersonMessage message) : base(message)
            {
            }

            public override void DataGets(object dataSource)
            {
                protocol = InterAppHelper.GetSLProtocol(dataSource);
            }

            public override void Parse()
            {
                if (Message.Id == null)
                {
                    var displayKeys = protocol.persons.DisplayKeys;

                    var displayKey = displayKeys.First(d => d.Contains(Message.FirstName) && d.Contains(Message.LastName));

                    Message.Id = displayKey.Split('/').First();
                }
            }

            public override void Modify()
            {
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DataSets(object dataDestination)
            {
                protocol = InterAppHelper.GetSLProtocol(dataDestination);

                var row = new PersonsQActionRow
                {
                    Personsindex_1001 = Message.Id,
                    Personsfirstname_1002 = Message.FirstName,
                    Personslastname_1003 = Message.LastName,
                    Personsgender_1004 = Message.Gender.ToString(),
                    Personsage_1005 = Message.Age,
                };

                protocol.persons.SetRow(row);
            }

            public override Message CreateReturnMessage()
            {
                return new PersonReturnMessage
                {
                    Guid = Message.Guid,
                    ReturnAddress = Message.ReturnAddress,
                    Source = Message.Source,
                    Message = "Succesfully Edited Person",
                };
            }
        }

        public class DeletePersonExecutor : MessageExecutor<DeletePersonMessage>
        {
            private SLProtocolExt protocol;

            public DeletePersonExecutor(DeletePersonMessage message) : base(message)
            {
            }

            public override void DataGets(object dataSource)
            {
                protocol = InterAppHelper.GetSLProtocol(dataSource);
            }

            public override void Parse()
            {
                if (Message.Id == null)
                {
                    var displayKeys = protocol.persons.DisplayKeys;

                    var displayKey = displayKeys.First(d => d.Contains(Message.FirstName) && d.Contains(Message.LastName));

                    Message.Id = displayKey.Split('/').First();
                }
            }

            public override void Modify()
            {
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DataSets(object dataDestination)
            {
                protocol = InterAppHelper.GetSLProtocol(dataDestination);

                protocol.persons.DeleteRow(Message.Id);
            }

            public override Message CreateReturnMessage()
            {
                return new PersonReturnMessage
                {
                    Guid = Message.Guid,
                    ReturnAddress = Message.ReturnAddress,
                    Source = Message.Source,
                    Message = "Succesfully Removed Person",
                };
            }
        }

        public class PersonReturnMessage : Message
        {
            public string Message { get; set; }
        }
    }
}