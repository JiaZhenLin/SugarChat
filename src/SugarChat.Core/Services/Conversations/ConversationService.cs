﻿using AutoMapper;
using SugarChat.Core.Services.Groups;
using SugarChat.Core.Services.GroupUsers;
using SugarChat.Core.Services.Messages;
using SugarChat.Core.Services.Users;
using SugarChat.Message.Commands.Conversations;
using SugarChat.Message.Events.Conversations;
using SugarChat.Message.Requests.Conversations;
using SugarChat.Message.Responses.Conversations;
using SugarChat.Message.Dtos;
using SugarChat.Message.Dtos.Conversations;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SugarChat.Core.Domain;
using SugarChat.Message.Paging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using SugarChat.Core.Services.GroupCustomProperties;

namespace SugarChat.Core.Services.Conversations
{
    public class ConversationService : IConversationService
    {
        private readonly IMapper _mapper;
        private readonly IUserDataProvider _userDataProvider;
        private readonly IGroupUserDataProvider _groupUserDataProvider;
        private readonly IConversationDataProvider _conversationDataProvider;
        private readonly IGroupDataProvider _groupDataProvider;
        private readonly IMessageDataProvider _messageDataProvider;
        private readonly IGroupCustomPropertyDataProvider _groupCustomPropertyDataProvider;

        public ConversationService(
            IMapper mapper,
            IUserDataProvider userDataProvider,
            IGroupUserDataProvider groupUserDataProvider,
            IConversationDataProvider conversationDataProvider,
            IGroupDataProvider groupDataProvider,
            IMessageDataProvider messageDataProvider,
            IGroupCustomPropertyDataProvider groupCustomPropertyDataProvider)
        {
            _mapper = mapper;
            _userDataProvider = userDataProvider;
            _conversationDataProvider = conversationDataProvider;
            _groupDataProvider = groupDataProvider;
            _groupUserDataProvider = groupUserDataProvider;
            _messageDataProvider = messageDataProvider;
            _groupCustomPropertyDataProvider = groupCustomPropertyDataProvider;
        }

        public async Task<PagedResult<ConversationDto>> GetConversationListByUserIdAsync(GetConversationListRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userDataProvider.GetByIdAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            var groupIds = (await _groupUserDataProvider.GetByUserIdAsync(request.UserId, cancellationToken, request.GroupType)).Select(x => x.GroupId).ToList();
            if (request.GroupIds.Any())
            {
                groupIds = groupIds.Intersect(request.GroupIds).ToList();
            }

            var conversations = new List<ConversationDto>();
            if (groupIds.Count == 0)
                return new PagedResult<ConversationDto> { Result = conversations, Total = groupIds.Count };

            var messageCountGroupByGroupIds = await _messageDataProvider.GetMessageUnreadCountGroupByGroupIdsAsync(user.Id,
                    groupIds,
                    request.PageSettings,
                    request.FilterUnreadCountByGroupCustomProperties,
                    request.FilterUnreadCountByGroupUserCustomProperties,
                    request.FilterUnreadCountByMessageCustomProperties,
                    cancellationToken);
            var groupIdResults = messageCountGroupByGroupIds.Select(x => x.GroupId);
            var groups = (await _groupDataProvider.GetByIdsAsync(groupIdResults, null, cancellationToken)).Result;
            var lastMessageForGroups = await _messageDataProvider.GetLastMessageForGroupsAsync(messageCountGroupByGroupIds.Select(x => x.GroupId), cancellationToken).ConfigureAwait(false);
            var groupCustomProperties = await _groupCustomPropertyDataProvider.GetPropertiesByGroupIds(groupIdResults, cancellationToken).ConfigureAwait(false);

            foreach (var messageCountGroupByGroupId in messageCountGroupByGroupIds)
            {
                var _groupCustomProperties = groupCustomProperties.Where(x => x.GroupId == messageCountGroupByGroupId.GroupId).ToList();
                var lastMessage = lastMessageForGroups.FirstOrDefault(x => x.GroupId == messageCountGroupByGroupId.GroupId);
                var group = groups.SingleOrDefault(x => x.Id == messageCountGroupByGroupId.GroupId);
                var groupDto = _mapper.Map<GroupDto>(group);
                groupDto.CustomPropertyList = _mapper.Map<IEnumerable<GroupCustomPropertyDto>>(_groupCustomProperties);
                groupDto.CustomProperties = _groupCustomProperties.Select(x => new { x.Key, x.Value }).Distinct().ToDictionary(x => x.Key, x => x.Value);
                var conversationDto = new ConversationDto
                {
                    ConversationID = messageCountGroupByGroupId.GroupId,
                    GroupProfile = groupDto,
                    UnreadCount = messageCountGroupByGroupId.UnreadCount,
                    LastMessageSentTime = messageCountGroupByGroupId.LastSentTime
                };
                if (lastMessage is not null)
                {
                    conversationDto.LastMessage = _mapper.Map<MessageDto>(lastMessage);
                }
                conversations.Add(conversationDto);
            }

            return new PagedResult<ConversationDto> { Result = conversations, Total = groupIds.Count };
        }

        public async Task<GetConversationProfileResponse> GetConversationProfileByIdAsync(
            GetConversationProfileRequest request, CancellationToken cancellationToken = default)
        {
            var groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(request.UserId, request.ConversationId,
                    cancellationToken);
            groupUser.CheckExist(request.UserId, request.ConversationId);

            var (_, count) = await _messageDataProvider.GetUnreadCountByGroupIdsAsync(request.UserId,
                    new List<string> { request.ConversationId },
                    request.FilterUnreadCountByGroupCustomProperties,
                    request.FilterUnreadCountByGroupUserCustomProperties,
                    request.FilterUnreadCountByMessageCustomProperties,
                    cancellationToken).ConfigureAwait(false);

            var conversationDto = await GetConversationDto(groupUser, cancellationToken);
            conversationDto.UnreadCount = count;

            return new GetConversationProfileResponse
            {
                Result = conversationDto
            };
        }

        public async Task<GetMessageListResponse> GetPagedMessagesByConversationIdAsync(GetMessageListRequest request,
            CancellationToken cancellationToken = default)
        {
            var groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(request.UserId, request.ConversationId,
                    cancellationToken);
            groupUser.CheckExist(request.UserId, request.ConversationId);

            var messages = await _conversationDataProvider
                .GetPagedMessagesByConversationIdAsync(request.ConversationId, request.NextReqMessageId, request.Index, request.Count,
                    cancellationToken).ConfigureAwait(false);

            return new GetMessageListResponse
            {
                Messages = messages.Select(x => _mapper.Map<MessageDto>(x)).ToList(),
                NextReqMessageID = messages.LastOrDefault()?.Id
            };
        }

        public async Task<ConversationRemovedEvent> RemoveConversationByConversationIdAsync(
            RemoveConversationCommand command, CancellationToken cancellationToken = default)
        {
            var groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(command.UserId, command.ConversationId,
                    cancellationToken);
            groupUser.CheckExist(command.UserId, command.ConversationId);

            await _groupUserDataProvider.RemoveAsync(groupUser, cancellationToken);

            return _mapper.Map<ConversationRemovedEvent>(command);
        }

        private async Task<ConversationDto> GetConversationDto(GroupUser groupUser,
            CancellationToken cancellationToken = default)
        {
            var conversationDto = new ConversationDto();
            conversationDto.ConversationID = groupUser.GroupId;
            conversationDto.UnreadCount =
                (await _messageDataProvider.GetUnreadMessagesFromGroupAsync(
                    groupUser.UserId, groupUser.GroupId, cancellationToken: cancellationToken)).Count();
            conversationDto.LastMessage = _mapper.Map<MessageDto>(
                await _messageDataProvider.GetLatestMessageOfGroupAsync(groupUser.GroupId, cancellationToken));
            var groupDto =
                _mapper.Map<GroupDto>(await _groupDataProvider.GetByIdAsync(groupUser.GroupId, cancellationToken));
            groupDto.MemberCount =
                await _groupUserDataProvider.GetGroupMemberCountByGroupIdAsync(groupUser.GroupId, cancellationToken);
            conversationDto.GroupProfile = groupDto;
            return conversationDto;
        }

        public async Task<PagedResult<ConversationDto>> GetConversationByKeyword(GetConversationByKeywordRequest request, CancellationToken cancellationToken = default)
        {
            if (request.SearchParms != null && request.SearchParms != new Dictionary<string, string>()
                && (request.MessageSearchParms == null || request.MessageSearchParms == new Dictionary<string, string>()))
                request.MessageSearchParms = request.SearchParms;

            var groupIds = (await _groupUserDataProvider.GetByUserIdAsync(request.UserId, cancellationToken, request.GroupType)).Select(x => x.GroupId).ToList();
            var filterGroupIds = new List<string>();
            if (request.GroupIds.Any())
            {
                groupIds = groupIds.Intersect(request.GroupIds).ToList();
            }

            var conversations = new List<ConversationDto>();
            if ((request.GroupSearchParms == null || !request.GroupSearchParms.Any()) && (request.MessageSearchParms == null || !request.MessageSearchParms.Any())) { }
            else
            {
                if (request.GroupSearchParms != null && request.GroupSearchParms.Any())
                {
                    var _groupIds = (await _groupDataProvider.GetByCustomPropertiesAsync(groupIds, request.GroupSearchParms, null, cancellationToken))
                            .Where(x => x.Type == request.GroupType || (request.GroupType == 0 && x.Type == null))
                            .Select(x => x.Id);
                    filterGroupIds.AddRange(_groupIds);
                }

                if (request.MessageSearchParms != null && request.MessageSearchParms.Any())
                {
                    var _groupIds = await _groupDataProvider.GetGroupIdsByMessageKeywordAsync(groupIds, request.MessageSearchParms, request.IsExactSearch, cancellationToken, request.GroupType);
                    filterGroupIds.AddRange(_groupIds);
                }

                if (!filterGroupIds.Any())
                    return new PagedResult<ConversationDto> { Result = conversations, Total = 0 };
                else
                    groupIds = groupIds.Where(x => filterGroupIds.Contains(x)).ToList();
            }
            var messageCountGroupByGroupIds = await _messageDataProvider.GetMessageUnreadCountGroupByGroupIdsAsync(request.UserId,
                    groupIds,
                    request.PageSettings,
                    request.FilterUnreadCountByGroupCustomProperties,
                    request.FilterUnreadCountByGroupUserCustomProperties,
                    request.FilterUnreadCountByMessageCustomProperties,
                    cancellationToken);
            var groupIdResults = messageCountGroupByGroupIds.Select(x => x.GroupId);
            var groups = (await _groupDataProvider.GetByIdsAsync(groupIdResults, null, cancellationToken)).Result;
            var lastMessageForGroups = await _messageDataProvider.GetLastMessageForGroupsAsync(messageCountGroupByGroupIds.Select(x => x.GroupId), cancellationToken).ConfigureAwait(false);
            var groupCustomProperties = await _groupCustomPropertyDataProvider.GetPropertiesByGroupIds(groupIdResults, cancellationToken).ConfigureAwait(false);
            foreach (var messageCountGroupByGroupId in messageCountGroupByGroupIds)
            {
                var _groupCustomProperties = groupCustomProperties.Where(x => x.GroupId == messageCountGroupByGroupId.GroupId).ToList();
                var lastMessage = lastMessageForGroups.FirstOrDefault(x => x.GroupId == messageCountGroupByGroupId.GroupId);
                var group = groups.SingleOrDefault(x => x.Id == messageCountGroupByGroupId.GroupId);
                var groupDto = _mapper.Map<GroupDto>(group);
                groupDto.CustomPropertyList = _mapper.Map<IEnumerable<GroupCustomPropertyDto>>(_groupCustomProperties);
                groupDto.CustomProperties = _groupCustomProperties.Select(x => new { x.Key, x.Value }).Distinct().ToDictionary(x => x.Key, x => x.Value);
                var conversationDto = new ConversationDto
                {
                    ConversationID = messageCountGroupByGroupId.GroupId,
                    GroupProfile = groupDto,
                    UnreadCount = messageCountGroupByGroupId.UnreadCount,
                    LastMessageSentTime = messageCountGroupByGroupId.LastSentTime
                };
                if (lastMessage is not null)
                {
                    conversationDto.LastMessage = _mapper.Map<MessageDto>(lastMessage);
                }
                conversations.Add(conversationDto);
            }

            return new PagedResult<ConversationDto> { Result = conversations, Total = groupIds.Count };
        }
    }
}