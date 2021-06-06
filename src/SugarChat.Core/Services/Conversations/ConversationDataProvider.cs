﻿using SugarChat.Core.IRepositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SugarChat.Core.Services.Conversations
{
    public class ConversationDataProvider : IConversationDataProvider
    {
        private readonly IRepository _repository;

        public ConversationDataProvider(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<(List<Domain.Message> Messages, string NextReqMessageId)>
            GetPagingMessagesByConversationIdAsync(string conversationId, string nextReqMessageId = "", int count = 15,
                CancellationToken cancellationToken = default)
        {
            var messages = new List<Domain.Message>();

            if (string.IsNullOrEmpty(nextReqMessageId))
            {
                messages = _repository.Query<Domain.Message>().Where(x => x.GroupId == conversationId)
                    .OrderByDescending(x => x.SentTime)
                    .Take(count)
                    .ToList();
            }
            else
            {
                var nextReqMessage =
                    await _repository.SingleOrDefaultAsync<Domain.Message>(x => x.Id == nextReqMessageId);
                messages = _repository.Query<Domain.Message>().Where(x =>
                        x.GroupId == conversationId && x.CreatedDate < nextReqMessage.CreatedDate)
                    .OrderByDescending(x => x.SentTime)
                    .Take(count)
                    .ToList();
            }

            return (messages, messages?.Last()?.Id);
        }
    }
}