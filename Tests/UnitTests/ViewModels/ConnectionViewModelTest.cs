using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
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
    public class ConnectionViewModelTest
    {
        //
        // Server
        //

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsListen_WhenEndPointIsValid(IPEndPoint endPoint,
            [Frozen] Mock<IServerService> serverServiceMock, ConnectionViewModel viewModel)
        {
            viewModel.IsServer = true;
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            serverServiceMock.Verify(
                ss => ss.ListenAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CatchesOperationCanceledException_WhenListenIsCanceled(IPEndPoint endPoint,
            [Frozen] Mock<IServerService> serverServiceMock, ConnectionViewModel viewModel)
        {
            viewModel.IsServer = true;
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;
            serverServiceMock
                .Setup(ss => ss.ListenAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()))
                .Throws<OperationCanceledException>();

            Func<Task> act = () => viewModel.ConnectOrListenCommand.ExecuteAsync();

            await act.Should()
                .NotThrowAsync<OperationCanceledException>();
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsShowErrorBox_WhenListenThrowsSocketException(IPEndPoint endPoint,
            [Frozen] Mock<IServerService> serverServiceMock, [Frozen] Mock<IViewManager> viewManagerMock,
            ConnectionViewModel viewModel)
        {
            viewModel.IsServer = true;
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;
            serverServiceMock
                .Setup(ss => ss.ListenAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()))
                .Throws<SocketException>();

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), "Unable To Listen"), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public void CancelOrDisconnect_CallsDropClient_WhenIsConnected([Frozen] Mock<IServerService> serverServiceMock,
            ConnectionViewModel viewModel)
        {
            viewModel.IsServer = true;
            viewModel.IsConnected = true;

            viewModel.CancelOrDisconnectCommand.Execute();

            serverServiceMock.Verify(ss => ss.DropClient(false), Times.Once);
        }

        //
        // Client
        //

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsConnect_WhenEndPointIsValid(IPEndPoint endPoint,
            [Frozen] Mock<IClientService> clientServiceMock, ConnectionViewModel viewModel)
        {
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            clientServiceMock.Verify(
                cs => cs.ConnectAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CatchesOperationCanceledException_WhenConnectIsCanceled(IPEndPoint endPoint,
            [Frozen] Mock<IClientService> clientServiceMock, ConnectionViewModel viewModel)
        {
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;
            clientServiceMock.Setup(cs =>
                    cs.ConnectAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()))
                .Throws<OperationCanceledException>();

            Func<Task> act = () => viewModel.ConnectOrListenCommand.ExecuteAsync();

            await act.Should()
                .NotThrowAsync<OperationCanceledException>();
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsShowErrorBox_WhenConnectThrowsSocketException(IPEndPoint endPoint,
            [Frozen] Mock<IClientService> clientServiceMock, [Frozen] Mock<IViewManager> viewManagerMock,
            ConnectionViewModel viewModel)
        {
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;
            clientServiceMock.Setup(cs =>
                    cs.ConnectAsync(endPoint.Address, endPoint.Port, It.IsAny<CancellationToken>()))
                .Throws<SocketException>();

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), "Unable To Connect"), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public void CancelOrDisconnect_CallsDisconnect_WhenIsConnected([Frozen] Mock<IClientService> clientServiceMock,
            ConnectionViewModel viewModel)
        {
            viewModel.IsConnected = true;

            viewModel.CancelOrDisconnectCommand.Execute();

            clientServiceMock.Verify(cs => cs.Disconnect(false), Times.Once);
        }

        //
        // Common
        //

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_RaisesPropertyChanged(IPEndPoint endPoint, ConnectionViewModel viewModel)
        {
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = endPoint.Port;
            using var monitor = viewModel.Monitor();

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            monitor.Should()
                .RaisePropertyChangeFor(vm => vm.IsConnectingOrListening);
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsShowErrorBox_WhenAddressIsInvalid(IPEndPoint endPoint,
            string invalidAddress, [Frozen] Mock<IViewManager> viewManagerMock, ConnectionViewModel viewModel)
        {
            viewModel.Address = invalidAddress;
            viewModel.Port = endPoint.Port;

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), "Invalid Address"), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ConnectOrListen_CallsShowErrorBox_WhenPortIsInvalid(IPEndPoint endPoint,
            [Range(ushort.MaxValue + 1, int.MaxValue)] int invalidPort, [Frozen] Mock<IViewManager> viewManagerMock,
            ConnectionViewModel viewModel)
        {
            viewModel.Address = endPoint.Address.ToString();
            viewModel.Port = invalidPort;

            await viewModel.ConnectOrListenCommand.ExecuteAsync();

            viewManagerMock.Verify(vm => vm.ShowErrorBoxAsync(It.IsAny<string>(), "Invalid Port"), Times.Once);
        }
    }
}
