using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mimic.Common.Networking;

namespace Mimic.RealmServer
{
    public class AuthWriter : IDisposable
    {
        private readonly AsyncBinaryWriter _writer;

        public AuthWriter(Stream input)
        {
            _writer = new AsyncBinaryWriter(input);
        }

        public Task PassChallengeAsync(
            Span<byte> publicKey,
            Span<byte> generator,
            Span<byte> safePrime,
            Span<byte> salt)
        {
            _writer.Write((byte)AuthCommand.LogonChallenge);
            _writer.Write((byte)0);

            _writer.Write((byte)AuthStatus.Success);
            _writer.Write(publicKey.Slice(0, 32));
            _writer.Write((byte)generator.Length);
            _writer.Write(generator);
            _writer.Write((byte)safePrime.Length);
            _writer.Write(safePrime);
            _writer.Write(salt.Slice(0, 32));

            // 16 bytes of nothingness
            // TODO: figure out the purpose of this
            for (int i = 0; i < 16; i++)
                _writer.Write((byte)0);

            // security flags
            // TODO: provide these from a param
            _writer.Write((byte)0);

            return _writer.FlushAsync();
        }

        public Task FailChallengeAsync(AuthStatus code)
        {
            _writer.Write((byte)AuthCommand.LogonChallenge);
            //_writer.Write((byte)0);
            _writer.Write((byte)code);

            return _writer.FlushAsync();
        }

        public Task PassProofAsync(
            Span<byte> proof,
            uint accountFlags,
            uint surveyId)
        {
            _writer.Write((byte)AuthCommand.LogonProof);
            _writer.Write((byte)0); // success?
            _writer.Write(proof);
            _writer.Write(accountFlags);
            _writer.Write(surveyId);
            _writer.Write((ushort)0);

            return _writer.FlushAsync();
        }

        public Task FailProofAsync(AuthStatus code)
        {
            _writer.Write((byte)AuthCommand.LogonProof);
            _writer.Write((byte)code);

            return _writer.FlushAsync();
        }

        public Task ServerListAsync()
        {
            // TODO: clean this up
            ushort realmCount = 1000;

            List<byte> realms = new List<byte>();
            realms.AddRange(BitConverter.GetBytes(0)); // unused/unknown
            realms.AddRange(BitConverter.GetBytes(realmCount)); // number of realms
            for (int i = 0; i < realmCount; i++)
            {
                realms.Add(0x02); // realm type
                realms.Add(0x00); // lock (0x00 == unlocked)
                realms.Add(0x40); // realm flags (0x40 == recommended)
                realms.AddRange(Encoding.UTF8.GetBytes($"Realm {i}")); // name
                realms.Add(0); // null-terminator
                realms.AddRange(Encoding.UTF8.GetBytes("127.0.0.1:1234")); // address
                realms.Add(0); // null-terminator
                realms.AddRange(BitConverter.GetBytes(0.5f)); // population level
                realms.Add((byte)(i % 16)); // number of characters
                realms.Add(0x01); // timezone

                realms.Add(0x2C); // unknown
            }

            realms.AddRange(BitConverter.GetBytes((ushort)0x0010)); // unused/unknown

            _writer.Write((byte)AuthCommand.RealmList);
            _writer.Write((ushort)realms.Count);
            _writer.Write(realms.ToArray());

            return _writer.FlushAsync();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
