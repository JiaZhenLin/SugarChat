﻿using Mediator.Net.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarChat.Core.Basic
{
    public interface ISugarChatResponse<T> : IResponse
    {
        public int Code { get; set;  }

        public string Message { get; set; }

        public T Data { get; set; }
    }
    public interface ISugarChatResponse : ISugarChatResponse<object>
    {
    }
}
