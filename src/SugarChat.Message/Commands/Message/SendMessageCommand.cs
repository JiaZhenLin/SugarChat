﻿using System;
using System.Collections.Generic;
using System.Text;
using Mediator.Net.Contracts;

namespace SugarChat.Message.Command
{
    public class SendMessageCommand : ICommand
    {
        public string GroupId { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        public string SentBy { get; set; }
        public object Payload { get; set; }
    }
}
