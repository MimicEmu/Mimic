using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mimic.Common.Networking;

namespace Mimic.RealmServer
{
    internal class AuthHandler : ISocketHandler, IDisposable
    {
        private const uint GameName = 0x00_57_6f_57; // 'WoW'
        private const string TestPassword = "PASSWORD"; // Password is uppercase

        private readonly ILogger _logger;
        private readonly SrpHandler _authentication;

        private bool _run = true;
        private TcpClient _client;
        private AsyncBinaryReader _reader;
        private AuthWriter _writer;

        private ushort _buildNumber;
        private AuthCommand _currentCommand;

        public TcpClient Client
            => _client;

        public AuthHandler(ILogger<AuthHandler> logger)
        {
            _logger = logger;
            _authentication = new SrpHandler();
        }

        public async Task RunAsync()
        {
            while (_run)
            {
                var cmd = (AuthCommand)await _reader.ReadUInt8Async();
                _currentCommand = cmd;
                _logger.LogTrace("Handling opcode {Opcode} from client", cmd);
                switch (cmd)
                {
                    case AuthCommand.LogonChallenge:
                        await HandleLogonChallengeAsync();
                        break;
                    case AuthCommand.LogonProof:
                        await HandleLogonProofAsync();
                        break;
                    case AuthCommand.RealmList:
                        await HandleRealmListAsync();
                        break;
                    default:
                        _logger.LogDebug("Unhandled opcode {Opcode}", cmd);
                        _run = false;
                        break;
                }
            }
        }

        public void SetClient(TcpClient client)
        {
            _client = client;
            _client.ReceiveTimeout = 1000;

            var stream = _client.GetStream();
            _reader = new AsyncBinaryReader(stream);
            _writer = new AuthWriter(stream);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _writer?.Dispose();
        }

        private async Task HandleLogonChallengeAsync()
        {
            var error = await _reader.ReadUInt8Async(); // always 3
            var size = await _reader.ReadUInt16Async();

            if (_reader.Available < size)
            {
                await _writer.FailChallengeAsync(AuthStatus.ProtocolError);
                return;
            }

            var gameName = await _reader.ReadUInt32Async();

            if (gameName != GameName)
            {
                await _writer.FailChallengeAsync(AuthStatus.ProtocolError);
                return;
            }

            var versionMajor = await _reader.ReadUInt8Async();
            var versionMinor = await _reader.ReadUInt8Async();
            var versionPatch = await _reader.ReadUInt8Async();

            _buildNumber = await _reader.ReadUInt16Async();

            var platform = (Architecture)await _reader.ReadUInt32Async();
            var os = (OperatingSystem)await _reader.ReadUInt32Async();
            var locale = (Locale)await _reader.ReadUInt32Async();

            var timezoneBias = await _reader.ReadUInt32Async();

            var ipAddress = new IPAddress(await _reader.ReadUInt32Async());
            var realAddress = (_client.Client.RemoteEndPoint as IPEndPoint).Address;

            var accountName = await _reader
                .ReadStringAsync(StringEncoding.LengthPrefixedUInt8);
            accountName = accountName.ToUpperInvariant();

            using (var sha = SHA1.Create())
            {
                var pw = Encoding.UTF8.GetBytes(
                    $"{accountName}:{TestPassword}");
                var hash = sha.ComputeHash(pw);

                _authentication.ComputePrivateFields(accountName, hash);
            }

            await _writer.PassChallengeAsync(
                _authentication.PublicKey,
                _authentication.Generator,
                _authentication.SafePrime,
                _authentication.Salt);
        }

        public async Task HandleLogonProofAsync()
        {
            var clientPublicKey = await _reader.ReadBytesAsync(32);
            var clientProof = await _reader.ReadBytesAsync(20);
            var crc = await _reader.ReadBytesAsync(20);
            var keyCount = await _reader.ReadUInt8Async();
            var securityFlags = await _reader.ReadUInt8Async();

            var authStatus = _authentication.Authenticate(
                clientPublicKey.Span, clientProof.Span);

            if (!authStatus)
            {
                await _writer.FailProofAsync(AuthStatus.IncorrectPassword);
                return;
            }

            // TODO: check build number and send back appropriate packet
            // (assuming WotLK right now, 3.3.5a, build 12340)
            await _writer.PassProofAsync(
                _authentication.ComputeProof(),
                0, 0);
        }

        public async Task HandleRealmListAsync()
        {
            // 4 empty bytes?
            uint unknown = await _reader.ReadUInt32Async();

            // TODO: retrieve realm list from external source and provide here
            await _writer.ServerListAsync();
        }
    }
}
