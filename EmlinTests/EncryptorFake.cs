﻿using Emlin.Encryption;
using System;
using System.Security.Cryptography;

namespace EmlinTests
{
    class EncryptorFake : IEncryptor
    {
        public AesCryptoServiceProvider Endec => new AesCryptoServiceProvider();

        public string Decrypted(string encrypted, string filepath)
        {
            return encrypted;
        }

        public string Encrypt(string decrypted)
        {
            return decrypted;
        }
    }
}
