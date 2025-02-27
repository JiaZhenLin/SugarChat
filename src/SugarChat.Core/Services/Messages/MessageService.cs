using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using SugarChat.Core.Domain;
using SugarChat.Message.Exceptions;
using SugarChat.Core.Services.Friends;
using SugarChat.Core.Services.Groups;
using SugarChat.Core.Services.GroupUsers;
using SugarChat.Core.Services.Users;
using SugarChat.Message.Commands.Messages;
using SugarChat.Message.Events.Messages;
using SugarChat.Message.Requests;
using SugarChat.Message.Responses;
using SugarChat.Message.Dtos;
using SugarChat.Message.Requests.Messages;
using SugarChat.Message.Responses.Messages;
using System.Linq;
using SugarChat.Core.Services.Configurations;
using SugarChat.Message.Paging;
using SugarChat.Core.Services.MessageCustomProperties;
using SugarChat.Core.IRepositories;
using Serilog;
using SugarChat.Core.Services.GroupUserCustomProperties;

namespace SugarChat.Core.Services.Messages
{
    public class MessageService : IMessageService
    {
        private readonly IMapper _mapper;
        private readonly IUserDataProvider _userDataProvider;
        private readonly IMessageDataProvider _messageDataProvider;
        private readonly IFriendDataProvider _friendDataProvider;
        private readonly IGroupDataProvider _groupDataProvider;
        private readonly IGroupUserDataProvider _groupUserDataProvider;
        private readonly IConfigurationDataProvider _configurationDataProvider;
        private readonly IMessageCustomPropertyDataProvider _messageCustomPropertyDataProvider;
        private readonly ITransactionManager _transactionManagement;
        private readonly IGroupUserCustomPropertyDataProvider _groupUserCustomPropertyDataProvider;

        public MessageService(IMapper mapper, IUserDataProvider userDataProvider,
            IMessageDataProvider messageDataProvider,
            IFriendDataProvider friendDataProvider, IGroupDataProvider groupDataProvider,
            IGroupUserDataProvider groupUserDataProvider, IConfigurationDataProvider configurationDataProvider,
            IMessageCustomPropertyDataProvider messageCustomPropertyDataProvider,
            ITransactionManager transactionManagement,
            IGroupUserCustomPropertyDataProvider groupUserCustomPropertyDataProvider)
        {
            _mapper = mapper;
            _userDataProvider = userDataProvider;
            _messageDataProvider = messageDataProvider;
            _friendDataProvider = friendDataProvider;
            _groupDataProvider = groupDataProvider;
            _groupUserDataProvider = groupUserDataProvider;
            _configurationDataProvider = configurationDataProvider;
            _messageCustomPropertyDataProvider = messageCustomPropertyDataProvider;
            _transactionManagement = transactionManagement;
            _groupUserCustomPropertyDataProvider = groupUserCustomPropertyDataProvider;
        }


        private Task<User> GetUserAsync(string id, CancellationToken cancellationToken = default)
        {
            return _userDataProvider.GetByIdAsync(id, cancellationToken);
        }

        public async Task<GetAllUnreadToUserResponse> GetAllUnreadToUserAsync(GetAllUnreadToUserRequest request,
            CancellationToken cancellationToken = default)
        {
            string userId = request.UserId;
            User user = await GetUserAsync(userId, cancellationToken);
            user.CheckExist(userId);

            var messages = await _messageDataProvider.GetAllUnreadToUserAsync(userId, request.GroupType, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetAllUnreadToUserResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetUnreadToUserFromFriendResponse> GetUnreadToUserFromFriendAsync(
            GetUnreadToUserFromFriendRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            user = await GetUserAsync(request.FriendId, cancellationToken);
            user.CheckExist(request.FriendId);

            Friend friend =
                await _friendDataProvider.GetByBothIdsAsync(request.UserId, request.FriendId, cancellationToken);
            friend.CheckExist(request.UserId, request.FriendId);

            var messages = await _messageDataProvider.GetUnreadToUserWithFriendAsync(request.UserId, request.FriendId, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetUnreadToUserFromFriendResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetAllHistoryToUserFromFriendResponse> GetAllHistoryToUserFromFriendAsync(
            GetAllHistoryToUserFromFriendRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            user = await GetUserAsync(request.FriendId, cancellationToken);
            user.CheckExist(request.FriendId);

            Friend friend =
                await _friendDataProvider.GetByBothIdsAsync(request.UserId, request.FriendId, cancellationToken);
            friend.CheckExist(request.UserId, request.FriendId);

            var messages = await _messageDataProvider.GetAllHistoryToUserWithFriendAsync(request.UserId, request.FriendId, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetAllHistoryToUserFromFriendResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetAllHistoryToUserResponse> GetAllHistoryToUserAsync(GetAllHistoryToUserRequest request,
            CancellationToken cancellationToken = default)
        {
            string userId = request.UserId;
            User user = await GetUserAsync(userId, cancellationToken);
            user.CheckExist(userId);

            var messages = await _messageDataProvider.GetAllHistoryToUserAsync(userId, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetAllHistoryToUserResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetUnreadMessagesFromGroupResponse> GetUnreadMessagesFromGroupAsync(
            GetUnreadMessagesFromGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            Group group = await _groupDataProvider.GetByIdAsync(request.GroupId, cancellationToken);
            group.CheckExist(request.GroupId);

            GroupUser groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(request.UserId, request.GroupId,
                    cancellationToken);
            groupUser.CheckExist(request.UserId, request.GroupId);

            var messages = await _messageDataProvider.GetUnreadMessagesFromGroupAsync(request.UserId, request.GroupId, request.MessageId, request.Count, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetUnreadMessagesFromGroupResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetAllMessagesFromGroupResponse> GetAllMessagesFromGroupAsync(
            GetAllMessagesFromGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            Group group = await _groupDataProvider.GetByIdAsync(request.GroupId, cancellationToken);
            group.CheckExist(request.GroupId);

            GroupUser groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(request.UserId, request.GroupId,
                    cancellationToken);
            groupUser.CheckExist(request.UserId, request.GroupId);

            var messages = await _messageDataProvider.GetAllMessagesFromGroupAsync(request.GroupId, request.Index, request.MessageId, request.Count, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new GetAllMessagesFromGroupResponse
            {
                Messages = messageDtos
            };
        }

        public async Task<GetMessagesOfGroupResponse> GetMessagesOfGroupAsync(GetMessagesOfGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            Group group = await _groupDataProvider.GetByIdAsync(request.GroupId, cancellationToken);
            group.CheckExist(request.GroupId);

            var messages = await _messageDataProvider.GetMessagesOfGroupAsync(request.GroupId, request.PageSettings, request.FromDate, cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages.Result);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new()
            {
                Messages = new PagedResult<MessageDto>
                {
                    Result = messageDtos,
                    Total = messages.Total
                }
            };
        }

        public async Task<GetMessagesOfGroupBeforeResponse> GetMessagesOfGroupBeforeAsync(
            GetMessagesOfGroupBeforeRequest request,
            CancellationToken cancellationToken = default)
        {
            Domain.Message message = await _messageDataProvider.GetByIdAsync(request.MessageId, cancellationToken);
            message.CheckExist(request.MessageId);

            var messages =
                await _messageDataProvider.GetMessagesOfGroupBeforeAsync(request.MessageId, request.Count,
                    cancellationToken);
            var messageDtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return new()
            {
                Messages = messageDtos
            };
        }

        public async Task<MessageReadSetByUserBasedOnMessageIdEvent> SetMessageReadByUserBasedOnMessageIdAsync(
            SetMessageReadByUserBasedOnMessageIdCommand command,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(command.UserId, cancellationToken);
            user.CheckExist(command.UserId);
            Domain.Message message = await _messageDataProvider.GetByIdAsync(command.MessageId, cancellationToken);
            message.CheckExist(command.MessageId);
            GroupUser groupUser =
                await _groupUserDataProvider.GetByUserAndGroupIdAsync(command.UserId, message.GroupId,
                    cancellationToken);
            groupUser.CheckExist(command.UserId, message.GroupId);

            groupUser.UnreadCount = 0;
            groupUser.LastReadTime = DateTime.Now;
            await _groupUserDataProvider.UpdateAsync(groupUser, cancellationToken).ConfigureAwait(false);

            return _mapper.Map<MessageReadSetByUserBasedOnMessageIdEvent>(command);
        }

        public async Task<MessageReadSetByUserBasedOnGroupIdEvent> SetMessageReadByUserBasedOnGroupIdAsync(
            SetMessageReadByUserBasedOnGroupIdCommand command,
            CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(command.UserId, cancellationToken);
            user.CheckExist(command.UserId);

            Group group = await _groupDataProvider.GetByIdAsync(command.GroupId, cancellationToken);
            group.CheckExist(command.GroupId);

            GroupUser groupUser = await _groupUserDataProvider.GetByUserAndGroupIdAsync(command.UserId, command.GroupId, cancellationToken);
            groupUser.CheckExist(command.UserId, command.GroupId);

            groupUser.UnreadCount = 0;
            groupUser.LastReadTime = DateTime.Now;
            await _groupUserDataProvider.UpdateAsync(groupUser, cancellationToken).ConfigureAwait(false);
            return _mapper.Map<MessageReadSetByUserBasedOnGroupIdEvent>(command);
        }

        public async Task<MessageReadSetByUserIdsBasedOnGroupIdEvent> SetMessageReadByUserIdsBasedOnGroupIdAsync(
            SetMessageReadByUserIdsBasedOnGroupIdCommand command,
            CancellationToken cancellationToken = default)
        {
            Group group = await _groupDataProvider.GetByIdAsync(command.GroupId, cancellationToken).ConfigureAwait(false);
            group.CheckExist(command.GroupId);

            var groupUsers = await _groupUserDataProvider.GetByGroupIdAndUsersIdAsync(command.GroupId, command.UserIds, cancellationToken).ConfigureAwait(false);
            foreach (var groupUser in groupUsers)
            {
                groupUser.UnreadCount = 0;
                groupUser.LastReadTime = DateTime.Now;
            }
            await _groupUserDataProvider.UpdateRangeAsync(groupUsers, cancellationToken).ConfigureAwait(false);

            return _mapper.Map<MessageReadSetByUserIdsBasedOnGroupIdEvent>(command);
        }

        public async Task<MessageRevokedEvent> RevokeMessageAsync(RevokeMessageCommand command,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            var message = await _messageDataProvider.GetByIdAsync(command.MessageId, cancellationToken).ConfigureAwait(false);
            message.CheckExist(command.MessageId);
            if (message.SentBy != command.UserId)
            {
                throw new BusinessWarningException(Prompt.RevokeOthersMessage.WithParams(command.UserId, command.MessageId));
            }

            var revokeTimeLimitInMinutes =
                (await _configurationDataProvider.GetServerConfigurationsAsync(cancellationToken)).RevokeTimeLimitInMinutes;
            if (message.SentTime.AddMinutes(revokeTimeLimitInMinutes) < now)
            {
                throw new BusinessWarningException(Prompt.TooLateToRevoke.WithParams(command.UserId, command.MessageId));
            }

            message.IsRevoked = true;
            await _messageDataProvider.UpdateAsync(message, cancellationToken);

            return _mapper.Map<MessageRevokedEvent>(command);
        }

        public async Task<MessageSavedEvent> SaveMessageAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
        {
            Group group = await _groupDataProvider.GetByIdAsync(command.GroupId, cancellationToken).ConfigureAwait(false);
            group.CheckExist(command.GroupId);

            GroupUser sendBy = await _groupUserDataProvider.GetByUserAndGroupIdAsync(command.SentBy, command.GroupId, cancellationToken);
            sendBy.CheckExist(command.SentBy, command.GroupId);

            Domain.Message message = _mapper.Map<Domain.Message>(command);
            message.SentTime = DateTime.Now;

            var messageCustomProperties = new List<MessageCustomProperty>();
            if (command.CustomProperties != null && command.CustomProperties.Any())
            {
                foreach (var customProperty in command.CustomProperties)
                {
                    messageCustomProperties.Add(new MessageCustomProperty
                    {
                        MessageId = command.Id,
                        Key = customProperty.Key,
                        Value = customProperty.Value
                    });
                }
            }
            message.CustomProperties = null;

            int time = 1;
            do
            {
                using (var transaction = await _transactionManagement.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        var groupUsers = await _groupUserDataProvider.GetByGroupIdAsync(command.GroupId, cancellationToken).ConfigureAwait(false);
                        groupUsers = groupUsers.Where(x => x.UserId != command.SentBy).ToList();
                        if (command.IgnoreUnreadCountByGroupUserCustomProperties != null && command.IgnoreUnreadCountByGroupUserCustomProperties.Any())
                        {
                            var _groupUserIds = groupUsers.Select(x => x.Id).ToList();
                            var filterGroupUserIds = await _groupUserCustomPropertyDataProvider.FilterGroupUserByCustomProperties(groupUsers.Select(x => x.Id),
                                    command.IgnoreUnreadCountByGroupUserCustomProperties, cancellationToken).ConfigureAwait(false);
                            groupUsers = groupUsers.Where(x => !filterGroupUserIds.Contains(x.Id)).ToList();
                        }
                        foreach (var groupUser in groupUsers)
                        {
                            groupUser.UnreadCount++;
                        }
                        await _groupUserDataProvider.UpdateRangeAsync(groupUsers, cancellationToken).ConfigureAwait(false);
                        await _messageCustomPropertyDataProvider.AddRangeAsync(messageCustomProperties, cancellationToken).ConfigureAwait(false);
                        await _messageDataProvider.AddAsync(message, cancellationToken).ConfigureAwait(false);
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(200);
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        if (time > 3)
                        {
                            throw;
                        }
                    }
                    time++;
                }
            } while (time < 4);
            return _mapper.Map<MessageSavedEvent>(command);
        }

        public async Task<GetUnreadMessageCountResponse> GetUnreadMessageCountAsync(GetUnreadMessageCountRequest request, CancellationToken cancellationToken = default)
        {
            string userId = request.UserId;
            User user = await GetUserAsync(userId, cancellationToken);
            user.CheckExist(userId);

            var groupIds = (await _groupUserDataProvider.GetByUserIdAsync(request.UserId, request.GroupIds, request.GroupType, cancellationToken)).Select(x => x.GroupId).ToList();

            var includeGroupIdsByCustomProperties = await _groupDataProvider.GetGroupIdByIncludeCustomPropertiesAsync(groupIds, request.IncludeGroupByGroupCustomProperties, cancellationToken).ConfigureAwait(false);
            groupIds = groupIds.Where(x => includeGroupIdsByCustomProperties.Contains(x)).ToList();

            var excludeGroupIdsByCustomProperties = await _groupDataProvider.GetGroupIdByExcludeCustomPropertiesAsync(groupIds, request.ExcludeGroupByGroupCustomProperties, cancellationToken).ConfigureAwait(false);
            groupIds = groupIds.Where(x => !excludeGroupIdsByCustomProperties.Contains(x)).ToList();

            if (!groupIds.Any())
                return new GetUnreadMessageCountResponse
                {
                    Count = 0
                };

            var (_, count) = await _messageDataProvider.GetUnreadCountByGroupIdsAsync(request.UserId, groupIds, cancellationToken).ConfigureAwait(false);
            return new GetUnreadMessageCountResponse
            {
                Count = count
            };
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesByGroupIdsAsync(GetMessagesByGroupIdsRequest request, CancellationToken cancellationToken = default)
        {
            User user = await GetUserAsync(request.UserId, cancellationToken);
            user.CheckExist(request.UserId);

            var messages = await _messageDataProvider.GetMessagesByGroupIdsAsync(request.GroupIds, cancellationToken);
            var messageDtos = messages.Select(x => _mapper.Map<MessageDto>(x)).ToList();
            await GetPropertiesForMessages(messageDtos, cancellationToken).ConfigureAwait(false);
            return messageDtos;
        }

        public async Task UpdateMessageDataAsync(UpdateMessageDataCommand command, CancellationToken cancellationToken = default)
        {
            var user = await _userDataProvider.GetByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
            user.CheckExist(command.UserId);

            var ids = command.Messages.Select(x => x.Id).ToArray();
            var messages = await _messageDataProvider.GetListByIdsAsync(ids, cancellationToken).ConfigureAwait(false);
            var groups = (await _groupDataProvider.GetByIdsAsync(messages.Select(x => x.GroupId), null, cancellationToken).ConfigureAwait(false)).Result;
            foreach (var message in messages)
            {
                var group = groups.SingleOrDefault(x => x.Id == message.GroupId);
                group.CheckExist(message.GroupId);
            }
            var userIds = messages.Select(x => x.SentBy);
            var users = await _userDataProvider.GetListAsync(x => userIds.Contains(x.Id));
            foreach (var message in messages)
            {
                var _user = users.SingleOrDefault(x => x.Id == message.SentBy);
                _user.CheckExist(message.SentBy);
            }

            var messageCustomProperties = await _messageCustomPropertyDataProvider.GetPropertiesByMessageIds(command.Messages.Select(x => x.Id), cancellationToken).ConfigureAwait(false);
            var oldMessageCustomProperties = new List<MessageCustomProperty>();
            var newMessageCustomProperties = new List<MessageCustomProperty>();
            foreach (var messageDto in command.Messages)
            {
                var message = messages.FirstOrDefault(x => x.Id == messageDto.Id);
                if (message != null)
                {
                    _mapper.Map(messageDto, message);
                    if (messageDto.CustomProperties != null && messageDto.CustomProperties.Any())
                    {
                        oldMessageCustomProperties.AddRange(messageCustomProperties.Where(x => x.MessageId == message.Id).ToList());
                        foreach (var customProperty in messageDto.CustomProperties)
                        {
                            newMessageCustomProperties.Add(new MessageCustomProperty
                            {
                                MessageId = messageDto.Id,
                                Key = customProperty.Key,
                                Value = customProperty.Value
                            });
                        }
                    }
                }
            }

            using (var transaction = await _transactionManagement.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await _messageCustomPropertyDataProvider.RemoveRangeAsync(oldMessageCustomProperties, cancellationToken).ConfigureAwait(false);
                    await _messageCustomPropertyDataProvider.AddRangeAsync(newMessageCustomProperties, cancellationToken).ConfigureAwait(false);
                    await _messageDataProvider.UpdateRangeAsync(messages, cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }

        private async Task GetPropertiesForMessages(IEnumerable<MessageDto> messages, CancellationToken cancellationToken)
        {
            var messageIds = messages.Select(x => x.Id);
            var messageCustomProperties = await _messageCustomPropertyDataProvider.GetPropertiesByMessageIds(messageIds, cancellationToken).ConfigureAwait(false);
            foreach (var message in messages)
            {
                var _messageCustomProperties = messageCustomProperties.Where(x => x.MessageId == message.Id).ToList();
                message.CustomProperties = _messageCustomProperties.Select(x => new { x.Key, x.Value }).Distinct().ToDictionary(x => x.Key, x => x.Value);
            }
        }

        /// <summary>
        /// 迁移数据使用，一次性代码
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task MigrateCustomPropertyAsync(CancellationToken cancellation = default)
        {
            var total = await _messageDataProvider.GetCountAsync(x => x.CustomProperties != null && x.CustomProperties != new Dictionary<string, string>(), cancellation).ConfigureAwait(false);
            var pageSize = 5000;
            var pageIndex = total / pageSize + 1;
            for (int i = 1; i <= pageIndex; i++)
            {
                using (var transaction = await _transactionManagement.BeginTransactionAsync(cancellation).ConfigureAwait(false))
                {
                    try
                    {
                        var messages = await _messageDataProvider.GetListAsync(new PageSettings { PageNum = 1, PageSize = pageSize }, x => x.CustomProperties != null && x.CustomProperties != new Dictionary<string, string>(), cancellation).ConfigureAwait(false);
                        var messageCustomProperties = new List<MessageCustomProperty>();
                        foreach (var message in messages)
                        {
                            foreach (var customProperty in message.CustomProperties)
                            {
                                messageCustomProperties.Add(new MessageCustomProperty { MessageId = message.Id, Key = customProperty.Key, Value = customProperty.Value });
                            }
                            message.CustomProperties = new Dictionary<string, string>();
                        }
                        await _messageDataProvider.UpdateRangeAsync(messages, cancellation).ConfigureAwait(false);
                        await _messageCustomPropertyDataProvider.AddRangeAsync(messageCustomProperties, cancellation).ConfigureAwait(false);
                        await transaction.CommitAsync(cancellation).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Migrate Message CustomProperty Error");
                        await transaction.RollbackAsync(cancellation).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}