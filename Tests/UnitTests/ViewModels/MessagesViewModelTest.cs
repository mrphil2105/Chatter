using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Chatter.Application.Services;
using Chatter.ViewModels;
using Chatter.ViewModels.Abstract;
using FluentAssertions;
using Moq;
using Xunit;

namespace Chatter.UnitTests.ViewModels
{
    public class MessagesViewModelTest
    {
        [Theory]
        [AutoMoqData]
        public async Task SendMessage_DoesNotCallSendMessage_WhenMessageIsEmpty(
            [Frozen] Mock<IMessageService> messageServiceMock, MessagesViewModel viewModel)
        {
            await viewModel.SendMessageCommand.ExecuteAsync();

            messageServiceMock.Verify(ms => ms.SendMessageAsync(string.Empty, It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendMessage_CallsSendMessage_WhenMessageIsNotEmpty(string message,
            [Frozen] Mock<IMessageService> messageServiceMock, MessagesViewModel viewModel)
        {
            viewModel.Message = message;

            await viewModel.SendMessageCommand.ExecuteAsync();

            messageServiceMock.Verify(ms => ms.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendMessage_AddsMessageViewModel_WhenMessageIsNotEmpty(string message,
            MessagesViewModel viewModel)
        {
            viewModel.Message = message;

            await viewModel.SendMessageCommand.ExecuteAsync();

            viewModel.MessageViewModels.Should()
                .ContainSingle(vm => vm.Message == message);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendMessage_CallsShowErrorBox_WhenSendMessageThrowsIOException(string message,
            [Frozen] Mock<IMessageService> messageServiceMock, [Frozen] Mock<IViewManager> viewManagerMock,
            MessagesViewModel viewModel)
        {
            viewModel.Message = message;
            messageServiceMock.Setup(ms => ms.SendMessageAsync(message, It.IsAny<CancellationToken>()))
                .Throws<IOException>();

            await viewModel.SendMessageCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task SendMessage_CallsShowErrorBox_WhenSendMessageIsCanceled(string message,
            [Frozen] Mock<IMessageService> messageServiceMock, [Frozen] Mock<IViewManager> viewManagerMock,
            MessagesViewModel viewModel)
        {
            viewModel.Message = message;
            messageServiceMock.Setup(ms => ms.SendMessageAsync(message, It.IsAny<CancellationToken>()))
                .Throws<OperationCanceledException>();

            await viewModel.SendMessageCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
