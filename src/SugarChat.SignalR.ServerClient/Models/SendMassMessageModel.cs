﻿using SugarChat.SignalR.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SugarChat.SignalR.Server.Models
{
    public class SendMassMessageModel
    {
        public SendWay SendWay { get; set; }

        public string[] Messages { get; set; }

        public IReadOnlyList<string> SendTos { get; set; }
    }
}
