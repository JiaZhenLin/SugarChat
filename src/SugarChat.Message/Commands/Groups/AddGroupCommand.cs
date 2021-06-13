using System;
using Mediator.Net.Contracts;

namespace SugarChat.Message.Commands.Groups
{
    public class AddGroupCommand : IdRequiredCommand
    {
        public string Name { get; set; }
        
        public string AvatarUrl { get; set; }
        
        public string Description { get; set; }
    }
}