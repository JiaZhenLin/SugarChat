﻿using Mediator.Net.Context;
using Mediator.Net.Contracts;
using SugarChat.Core.Services.Conversations;
using SugarChat.Message.Commands.Conversations;
using System.Threading;
using System.Threading.Tasks;

namespace SugarChat.Core.Mediator.CommandHandlers.Conversations
{
    public class SetMessageAsReadCommandHandler : ICommandHandler<SetMessageAsReadCommand>
    {
        public IConversationService _conversationService;
        public SetMessageAsReadCommandHandler(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        public async Task Handle(IReceiveContext<SetMessageAsReadCommand> context, CancellationToken cancellationToken)
        {
            var messageReadedEvent = await _conversationService.SetMessageAsReadByConversationIdAsync(context.Message, cancellationToken).ConfigureAwait(false);
            await context.PublishAsync(messageReadedEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
