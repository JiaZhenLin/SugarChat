﻿using Mediator.Net;
using Shouldly;
using SugarChat.Core.Basic;
using SugarChat.Core.Domain;
using SugarChat.Core.Exceptions;
using SugarChat.Core.IRepositories;
using SugarChat.Core.Services;
using SugarChat.Message.Commands.GroupUsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SugarChat.IntegrationTest.Services
{
    public class GroupUserFixture : TestBase
    {
        private string groupId = Guid.NewGuid().ToString();
        private string groupOwnerId = Guid.NewGuid().ToString();
        private string userId = Guid.NewGuid().ToString();

        private async Task AddGroup(IRepository repository)
        {
            await repository.AddAsync(new Group
            {
                Id = groupId,
                Name = "testGroup",
                AvatarUrl = "testAvatarUrl",
                Description = "testDescription"
            });
        }

        private async Task AddGroupOwner(IRepository repository)
        {
            await repository.AddAsync(new User
            {
                Id = groupOwnerId
            });
            await repository.AddAsync(new GroupUser
            {
                Id = Guid.NewGuid().ToString(),
                UserId = groupOwnerId,
                GroupId = groupId,
                IsMaster = true,
                IsAdmin = true
            });
        }

        private async Task AddUser(IRepository repository)
        {
            await repository.AddAsync(new User
            {
                Id = userId
            });
        }

        private async Task AddGroupUser(IRepository repository)
        {
            await AddUser(repository);
            await repository.AddAsync(new GroupUser
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                GroupId = groupId
            });
        }

        [Fact]
        public async Task ShouldJoinGroup()
        {
            await Run<IMediator, IRepository>(async (mediator, repository) =>
            {
                await AddGroup(repository);
                await AddUser(repository);
                JoinGroupCommand command = new JoinGroupCommand
                {
                    GroupId = Guid.NewGuid().ToString(),
                    UserId = Guid.NewGuid().ToString(),
                };
                {
                    var response = await mediator.SendAsync<JoinGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.GroupNoExists, command.GroupId));
                }
                {
                    command.GroupId = groupId;
                    var response = await mediator.SendAsync<JoinGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.UserNoExists, command.UserId));
                }

                command.UserId = userId;
                await mediator.SendAsync(command);
                (await repository.AnyAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == command.UserId)).ShouldBe(true);

                {
                    var response = await mediator.SendAsync<JoinGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.InGroup, command.UserId, command.GroupId));
                }
            });
        }

        [Fact]
        public async Task ShouldQuitGroup()
        {
            await Run<IMediator, IRepository>(async (mediator, repository) =>
            {
                await AddGroup(repository);
                await AddGroupOwner(repository);
                await AddGroupUser(repository);
                QuitGroupCommand command = new QuitGroupCommand
                {
                    GroupId = Guid.NewGuid().ToString(),
                    UserId = Guid.NewGuid().ToString()
                };
                {
                    var response = await mediator.SendAsync<QuitGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.UserId, command.GroupId));
                }
                {
                    command.GroupId = groupId;
                    var response = await mediator.SendAsync<QuitGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.UserId, command.GroupId));
                }
                {
                    command.UserId = groupOwnerId;
                    var response = await mediator.SendAsync<QuitGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.IsOwner, command.UserId, command.GroupId));
                }

                command.UserId = userId;
                await mediator.SendAsync(command);
                (await repository.ToListAsync<GroupUser>()).Count.ShouldBe(1);
                (await repository.ToListAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == groupOwnerId)).Count.ShouldBe(1);

                {
                    var response = await mediator.SendAsync<QuitGroupCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.UserId, command.GroupId));
                }
            });
        }

        [Fact]
        public async Task ShouldChangeGroupOwner()
        {
            await Run<IMediator, IRepository>(async (mediator, repository) =>
            {
                await AddGroup(repository);
                await AddGroupOwner(repository);
                await AddGroupUser(repository);
                ChangeGroupOwnerCommand command = new ChangeGroupOwnerCommand
                {
                    FromUserId = Guid.NewGuid().ToString(),
                    ToUserId = Guid.NewGuid().ToString(),
                    GroupId = Guid.NewGuid().ToString()
                };
                {
                    var response = await mediator.SendAsync<ChangeGroupOwnerCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.FromUserId, command.GroupId));
                }
                {
                    command.GroupId = groupId;
                    var response = await mediator.SendAsync<ChangeGroupOwnerCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.FromUserId, command.GroupId));
                }
                {
                    command.FromUserId = userId;
                    var response = await mediator.SendAsync<ChangeGroupOwnerCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.IsNotOwner, command.FromUserId, command.GroupId));
                }
                {
                    command.FromUserId = groupOwnerId;
                    var response = await mediator.SendAsync<ChangeGroupOwnerCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.ToUserId, command.GroupId));
                }

                command.ToUserId = userId;
                await mediator.SendAsync(command);

                (await repository.FirstOrDefaultAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == command.FromUserId)).IsMaster.ShouldBeFalse();

                var groupUser = await repository.FirstOrDefaultAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == command.ToUserId);
                groupUser.IsMaster.ShouldBeTrue();
                groupUser.IsAdmin.ShouldBeTrue();
            });
        }

        [Fact]
        public async Task ShouldAddGroupMember()
        {
            await Run<IMediator, IRepository>(async (mediator, repository) =>
            {
                await AddGroup(repository);
                await AddGroupOwner(repository);
                AddGroupMemberCommand command = new AddGroupMemberCommand
                {
                    GroupId = Guid.NewGuid().ToString(),
                    GroupAdminId = Guid.NewGuid().ToString(),
                    MemberId = Guid.NewGuid().ToString()
                };
                {
                    var response = await mediator.SendAsync<AddGroupMemberCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.GroupAdminId, command.GroupId));
                }
                {
                    command.GroupId = groupId;
                    var response = await mediator.SendAsync<AddGroupMemberCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.NotInGroup, command.GroupAdminId, command.GroupId));
                }

                command.GroupAdminId = groupOwnerId;
                await mediator.SendAsync(command);
                (await repository.AnyAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == command.MemberId)).ShouldBeTrue();

                command.MemberId = userId;
                await mediator.SendAsync(command);
                (await repository.AnyAsync<GroupUser>(x => x.GroupId == command.GroupId && x.UserId == command.MemberId)).ShouldBeTrue();

                {
                    command.GroupAdminId = groupOwnerId;
                    var response = await mediator.SendAsync<AddGroupMemberCommand, SugarChatResponse<object>>(command);
                    response.Message.ShouldBe(string.Format(ServiceCheckExtensions.InGroup, command.MemberId, command.GroupId));
                }
            });
        }
    }
}