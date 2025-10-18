using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSharp.Security;

namespace OSharp.Tests.Security
{
    /// <summary>
    /// Crypto类的单元测试
    /// </summary>
    [TestClass]
    public class CryptoTests
    {
        #region AES加密解密测试

        [TestMethod]
        public void GenerateAesKey_ShouldReturn32Bytes()
        {
            // Act
            var key = Crypto.GenerateAesKey();

            // Assert
            Assert.IsNotNull(key);
            Assert.AreEqual(32, key.Length);
        }

        [TestMethod]
        public void AesEncrypt_WithByteArray_ShouldEncryptSuccessfully()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var key = Crypto.GenerateAesKey();

            // Act
            var (encryptData, returnedKey) = Crypto.AesEncrypt(data, key);

            // Assert
            Assert.IsNotNull(encryptData);
            Assert.IsNotNull(encryptData.CipherData);
            Assert.IsNotNull(encryptData.Iv);
            Assert.AreEqual(16, encryptData.Iv.Length);
            Assert.AreEqual(key, returnedKey);
            Assert.AreNotEqual(data, encryptData.CipherData);
        }

        [TestMethod]
        public void AesEncrypt_WithNullKey_ShouldGenerateRandomKey()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hello, World!");

            // Act
            var (encryptData, key) = Crypto.AesEncrypt(data, null);

            // Assert
            Assert.IsNotNull(encryptData);
            Assert.IsNotNull(key);
            Assert.AreEqual(32, key.Length);
        }

        [TestMethod]
        public void AesDecrypt_ShouldDecryptSuccessfully()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Hello, World!");
            var (encryptData, key) = Crypto.AesEncrypt(originalData, null);

            // Act
            var decryptedData = Crypto.AesDecrypt(encryptData, key);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        public void AesEncrypt_WithString_ShouldEncryptSuccessfully()
        {
            // Arrange
            var data = "Hello, World!";
            var key = Convert.ToBase64String(Crypto.GenerateAesKey());

            // Act
            var (encryptData, returnedKey) = Crypto.AesEncrypt(data, key);

            // Assert
            Assert.IsNotNull(encryptData);
            Assert.IsNotNull(returnedKey);
        }

        [TestMethod]
        public void AesDecrypt_WithString_ShouldDecryptSuccessfully()
        {
            // Arrange
            var originalData = "Hello, World!";
            var (encryptData, key) = Crypto.AesEncrypt(originalData, null);
            var base64Key = Convert.ToBase64String(key);

            // Act
            var decryptedData = Crypto.AesDecrypt(encryptData, base64Key);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AesEncrypt_WithInvalidKeyLength_ShouldThrowException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var invalidKey = new byte[16]; // 应该是32字节

            // Act
            Crypto.AesEncrypt(data, invalidKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AesDecrypt_WithInvalidKeyLength_ShouldThrowException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var (encryptData, _) = Crypto.AesEncrypt(data, null);
            var invalidKey = new byte[16]; // 应该是32字节

            // Act
            Crypto.AesDecrypt(encryptData, invalidKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AesDecrypt_WithInvalidBase64Key_ShouldThrowException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var (encryptData, _) = Crypto.AesEncrypt(data, null);
            var invalidBase64Key = "invalid-base64-string";

            // Act
            Crypto.AesDecrypt(encryptData, invalidBase64Key);
        }

        #endregion

        #region RSA加密解密测试

        [TestMethod]
        public void GenerateRsaKey_ShouldReturnValidKeys()
        {
            // Act
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();

            // Assert
            Assert.IsNotNull(publicKey);
            Assert.IsNotNull(privateKey);
            Assert.IsTrue(publicKey.Contains("<RSAKeyValue>"));
            Assert.IsTrue(privateKey.Contains("<RSAKeyValue>"));
            Assert.IsTrue(privateKey.Length > publicKey.Length);
        }

        [TestMethod]
        public void RsaEncrypt_WithValidData_ShouldEncryptSuccessfully()
        {
            // Arrange
            var (publicKey, _) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");

            // Act
            var encryptedData = Crypto.RsaEncrypt(data, publicKey);

            // Assert
            Assert.IsNotNull(encryptedData);
            Assert.AreNotEqual(data, encryptedData);
        }

        [TestMethod]
        public void RsaDecrypt_ShouldDecryptSuccessfully()
        {
            // Arrange
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();
            var originalData = Encoding.UTF8.GetBytes("Hello, World!");
            var encryptedData = Crypto.RsaEncrypt(originalData, publicKey);

            // Act
            var decryptedData = Crypto.RsaDecrypt(encryptedData, privateKey);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        public void RsaEncrypt_WithString_ShouldEncryptSuccessfully()
        {
            // Arrange
            var (publicKey, _) = Crypto.GenerateRsaKey();
            var data = "Hello, World!";

            // Act
            var encryptedData = Crypto.RsaEncrypt(data, publicKey);

            // Assert
            Assert.IsNotNull(encryptedData);
            Assert.IsTrue(encryptedData.Length > 0);
        }

        [TestMethod]
        public void RsaDecrypt_WithString_ShouldDecryptSuccessfully()
        {
            // Arrange
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();
            var originalData = "Hello, World!";
            var encryptedData = Crypto.RsaEncrypt(originalData, publicKey);

            // Act
            var decryptedData = Crypto.RsaDecrypt(encryptedData, privateKey);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RsaEncrypt_WithDataTooLong_ShouldThrowException()
        {
            // Arrange
            var (publicKey, _) = Crypto.GenerateRsaKey();
            var data = new byte[200]; // 超过2048位RSA密钥的190字节限制
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            // Act
            Crypto.RsaEncrypt(data, publicKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RsaDecrypt_WithInvalidBase64Data_ShouldThrowException()
        {
            // Arrange
            var (_, privateKey) = Crypto.GenerateRsaKey();
            var invalidBase64Data = "invalid-base64-string";

            // Act
            Crypto.RsaDecrypt(invalidBase64Data, privateKey);
        }

        #endregion

        #region RSA签名验证测试

        [TestMethod]
        public void RsaSignData_WithByteArray_ShouldSignSuccessfully()
        {
            // Arrange
            var (_, privateKey) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");

            // Act
            var signature = Crypto.RsaSignData(data, privateKey);

            // Assert
            Assert.IsNotNull(signature);
            Assert.IsTrue(signature.Length > 0);
        }

        [TestMethod]
        public void RsaVerifyData_WithValidSignature_ShouldReturnTrue()
        {
            // Arrange
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var signature = Crypto.RsaSignData(data, privateKey);

            // Act
            var isValid = Crypto.RsaVerifyData(data, signature, publicKey);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void RsaVerifyData_WithInvalidSignature_ShouldReturnFalse()
        {
            // Arrange
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var wrongData = Encoding.UTF8.GetBytes("Wrong Data");
            var signature = Crypto.RsaSignData(wrongData, privateKey);

            // Act
            var isValid = Crypto.RsaVerifyData(data, signature, publicKey);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void RsaSignData_WithString_ShouldSignSuccessfully()
        {
            // Arrange
            var (_, privateKey) = Crypto.GenerateRsaKey();
            var data = "Hello, World!";

            // Act
            var signature = Crypto.RsaSignData(data, privateKey);

            // Assert
            Assert.IsNotNull(signature);
            Assert.IsTrue(signature.Length > 0);
        }

        [TestMethod]
        public void RsaVerifyData_WithString_ShouldVerifySuccessfully()
        {
            // Arrange
            var (publicKey, privateKey) = Crypto.GenerateRsaKey();
            var data = "Hello, World!";
            var signature = Crypto.RsaSignData(data, privateKey);

            // Act
            var isValid = Crypto.RsaVerifyData(data, signature, publicKey);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RsaVerifyData_WithInvalidBase64Signature_ShouldThrowException()
        {
            // Arrange
            var (publicKey, _) = Crypto.GenerateRsaKey();
            var data = "Hello, World!";
            var invalidSignature = "invalid-base64-signature";

            // Act
            Crypto.RsaVerifyData(data, invalidSignature, publicKey);
        }

        #endregion

        #region 混合加密测试

        [TestMethod]
        public void HybridEncrypt_WithByteArray_ShouldEncryptSuccessfully()
        {
            // Arrange
            var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, _) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");

            // Act
            var hybridData = Crypto.HybridEncrypt(data, ownPrivateKey, facePublicKey);

            // Assert
            Assert.IsNotNull(hybridData);
            Assert.IsNotNull(hybridData.AesEncryptData);
            Assert.IsNotNull(hybridData.Signature);
            Assert.IsNotNull(hybridData.RsaEncryptedAesKey);
        }

        [TestMethod]
        public void HybridDecrypt_ShouldDecryptSuccessfully()
        {
            // Arrange
            var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, facePrivateKey) = Crypto.GenerateRsaKey();
            var originalData = Encoding.UTF8.GetBytes("Hello, World!");
            var hybridData = Crypto.HybridEncrypt(originalData, ownPrivateKey, facePublicKey);

            // Act
            var decryptedData = Crypto.HybridDecrypt(hybridData, facePrivateKey, ownPublicKey);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        public void HybridEncrypt_WithString_ShouldEncryptSuccessfully()
        {
            // Arrange
            var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, _) = Crypto.GenerateRsaKey();
            var data = "Hello, World!";

            // Act
            var hybridJson = Crypto.HybridEncrypt(data, ownPrivateKey, facePublicKey);

            // Assert
            Assert.IsNotNull(hybridJson);
            Assert.IsTrue(hybridJson.Length > 0);
        }

        [TestMethod]
        public void HybridDecrypt_WithString_ShouldDecryptSuccessfully()
        {
            // Arrange
            var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, facePrivateKey) = Crypto.GenerateRsaKey();
            var originalData = "Hello, World!";
            var hybridJson = Crypto.HybridEncrypt(originalData, ownPrivateKey, facePublicKey);

            // Act
            var decryptedData = Crypto.HybridDecrypt(hybridJson, facePrivateKey, ownPublicKey);

            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void HybridDecrypt_WithInvalidSignature_ShouldThrowException()
        {
            // Arrange
            var (ownPublicKey, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, facePrivateKey) = Crypto.GenerateRsaKey();
            var data = Encoding.UTF8.GetBytes("Hello, World!");
            var hybridData = Crypto.HybridEncrypt(data, ownPrivateKey, facePublicKey);

            // 修改签名使其无效
            hybridData.Signature[0] = (byte)(hybridData.Signature[0] ^ 0xFF);

            // Act
            Crypto.HybridDecrypt(hybridData, facePrivateKey, ownPublicKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void HybridDecrypt_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var (ownPublicKey, _) = Crypto.GenerateRsaKey();
            var (_, facePrivateKey) = Crypto.GenerateRsaKey();
            var invalidJson = "invalid-json-string";

            // Act
            Crypto.HybridDecrypt(invalidJson, facePrivateKey, ownPublicKey);
        }

        #endregion

        #region AesEncryptData类测试

        [TestMethod]
        public void AesEncryptData_Constructor_ShouldCreateValidInstance()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var iv = new byte[16];

            // Act
            var encryptData = new AesEncryptData(cipherData, iv);

            // Assert
            Assert.AreEqual(cipherData, encryptData.CipherData);
            Assert.AreEqual(iv, encryptData.Iv);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AesEncryptData_Constructor_WithNullCipherData_ShouldThrowException()
        {
            // Arrange
            var iv = new byte[16];

            // Act
            new AesEncryptData(null, iv);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AesEncryptData_Constructor_WithNullIv_ShouldThrowException()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };

            // Act
            new AesEncryptData(cipherData, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AesEncryptData_Constructor_WithInvalidIvLength_ShouldThrowException()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var invalidIv = new byte[8]; // 应该是16字节

            // Act
            new AesEncryptData(cipherData, invalidIv);
        }

        [TestMethod]
        public void AesEncryptData_GetIvString_ShouldReturnBase64String()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var iv = new byte[16];
            var encryptData = new AesEncryptData(cipherData, iv);

            // Act
            var ivString = encryptData.GetIvString();

            // Assert
            Assert.IsNotNull(ivString);
            Assert.AreEqual(Convert.ToBase64String(iv), ivString);
        }

        [TestMethod]
        public void AesEncryptData_GetCipherDataString_ShouldReturnBase64String()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var iv = new byte[16];
            var encryptData = new AesEncryptData(cipherData, iv);

            // Act
            var cipherString = encryptData.GetCipherDataString();

            // Assert
            Assert.IsNotNull(cipherString);
            Assert.AreEqual(Convert.ToBase64String(cipherData), cipherString);
        }

        [TestMethod]
        public void AesEncryptData_ToJson_ShouldReturnValidJson()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var iv = new byte[16];
            var encryptData = new AesEncryptData(cipherData, iv);

            // Act
            var json = encryptData.ToJson();

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("CipherData"));
            Assert.IsTrue(json.Contains("Iv"));
        }

        [TestMethod]
        public void AesEncryptData_FromJson_ShouldDeserializeSuccessfully()
        {
            // Arrange
            var cipherData = new byte[] { 1, 2, 3, 4 };
            var iv = new byte[16];
            var originalData = new AesEncryptData(cipherData, iv);
            var json = originalData.ToJson();

            // Act
            var deserializedData = AesEncryptData.FromJson(json);

            // Assert
            Assert.IsNotNull(deserializedData);
            Assert.AreEqual(originalData.CipherData, deserializedData.CipherData);
            Assert.AreEqual(originalData.Iv, deserializedData.Iv);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AesEncryptData_FromJson_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var invalidJson = "invalid-json-string";

            // Act
            AesEncryptData.FromJson(invalidJson);
        }

        #endregion

        #region HybridEncryptData类测试

        [TestMethod]
        public void HybridEncryptData_ToJson_ShouldReturnValidJson()
        {
            // Arrange
            var hybridData = new HybridEncryptData
            {
                AesEncryptData = new AesEncryptData(new byte[] { 1, 2, 3, 4 }, new byte[16]),
                Signature = new byte[] { 5, 6, 7, 8 },
                RsaEncryptedAesKey = new byte[] { 9, 10, 11, 12 }
            };

            // Act
            var json = hybridData.ToJson();

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("AesEncryptData"));
            Assert.IsTrue(json.Contains("Signature"));
            Assert.IsTrue(json.Contains("RsaEncryptedAesKey"));
        }

        [TestMethod]
        public void HybridEncryptData_FromJson_ShouldDeserializeSuccessfully()
        {
            // Arrange
            var originalData = new HybridEncryptData
            {
                AesEncryptData = new AesEncryptData(new byte[] { 1, 2, 3, 4 }, new byte[16]),
                Signature = new byte[] { 5, 6, 7, 8 },
                RsaEncryptedAesKey = new byte[] { 9, 10, 11, 12 }
            };
            var json = originalData.ToJson();

            // Act
            var deserializedData = HybridEncryptData.FromJson(json);

            // Assert
            Assert.IsNotNull(deserializedData);
            Assert.IsNotNull(deserializedData.AesEncryptData);
            Assert.IsNotNull(deserializedData.Signature);
            Assert.IsNotNull(deserializedData.RsaEncryptedAesKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void HybridEncryptData_FromJson_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var invalidJson = "invalid-json-string";

            // Act
            HybridEncryptData.FromJson(invalidJson);
        }

        #endregion

        #region 文件操作测试

        [TestMethod]
        public void AesEncryptFile_ShouldEncryptFileSuccessfully()
        {
            // Arrange
            var sourceFile = Path.GetTempFileName();
            var targetFile = Path.GetTempFileName();
            var key = Convert.ToBase64String(Crypto.GenerateAesKey());
            var testData = "Hello, World!";
            File.WriteAllText(sourceFile, testData);

            try
            {
                // Act
                var (encryptData, returnedKey) = Crypto.AesEncryptFile(sourceFile, targetFile, key);

                // Assert
                Assert.IsNotNull(encryptData);
                Assert.IsNotNull(returnedKey);
                Assert.IsTrue(File.Exists(targetFile));
                var encryptedContent = File.ReadAllText(targetFile);
                Assert.IsNotNull(encryptedContent);
                Assert.AreNotEqual(testData, encryptedContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
        }

        [TestMethod]
        public void AesDecryptFile_ShouldDecryptFileSuccessfully()
        {
            // Arrange
            var sourceFile = Path.GetTempFileName();
            var targetFile = Path.GetTempFileName();
            var decryptFile = Path.GetTempFileName();
            var key = Convert.ToBase64String(Crypto.GenerateAesKey());
            var testData = "Hello, World!";
            File.WriteAllText(sourceFile, testData);

            try
            {
                // 先加密文件
                var (encryptData, _) = Crypto.AesEncryptFile(sourceFile, targetFile, key);

                // Act - 解密文件
                Crypto.AesDecryptFile(targetFile, decryptFile, key);

                // Assert
                Assert.IsTrue(File.Exists(decryptFile));
                var decryptedContent = File.ReadAllText(decryptFile);
                Assert.AreEqual(testData, decryptedContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(targetFile)) File.Delete(targetFile);
                if (File.Exists(decryptFile)) File.Delete(decryptFile);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void AesEncryptFile_WithNonExistentSource_ShouldThrowException()
        {
            // Arrange
            var nonExistentFile = "non-existent-file.txt";
            var targetFile = Path.GetTempFileName();
            var key = Convert.ToBase64String(Crypto.GenerateAesKey());

            try
            {
                // Act
                Crypto.AesEncryptFile(nonExistentFile, targetFile, key);
            }
            finally
            {
                // Cleanup
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void AesDecryptFile_WithNonExistentSource_ShouldThrowException()
        {
            // Arrange
            var nonExistentFile = "non-existent-file.txt";
            var targetFile = Path.GetTempFileName();
            var key = Convert.ToBase64String(Crypto.GenerateAesKey());

            try
            {
                // Act
                Crypto.AesDecryptFile(nonExistentFile, targetFile, key);
            }
            finally
            {
                // Cleanup
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
        }

        #endregion

        #region 参数验证测试

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AesEncrypt_WithNullData_ShouldThrowException()
        {
            // Act
            Crypto.AesEncrypt((byte[])null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AesDecrypt_WithNullEncryptData_ShouldThrowException()
        {
            // Act
            Crypto.AesDecrypt(null, new byte[32]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RsaEncrypt_WithNullData_ShouldThrowException()
        {
            // Arrange
            var (publicKey, _) = Crypto.GenerateRsaKey();

            // Act
            Crypto.RsaEncrypt((string)null, publicKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RsaDecrypt_WithNullData_ShouldThrowException()
        {
            // Arrange
            var (_, privateKey) = Crypto.GenerateRsaKey();

            // Act
            Crypto.RsaDecrypt((string)null, privateKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HybridEncrypt_WithNullData_ShouldThrowException()
        {
            // Arrange
            var (_, ownPrivateKey) = Crypto.GenerateRsaKey();
            var (facePublicKey, _) = Crypto.GenerateRsaKey();

            // Act
            Crypto.HybridEncrypt((string)null, ownPrivateKey, facePublicKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HybridDecrypt_WithNullHybridData_ShouldThrowException()
        {
            // Arrange
            var (ownPublicKey, _) = Crypto.GenerateRsaKey();
            var (_, facePrivateKey) = Crypto.GenerateRsaKey();

            // Act
            Crypto.HybridDecrypt((string)null, facePrivateKey, ownPublicKey);
        }

        #endregion
    }
}
