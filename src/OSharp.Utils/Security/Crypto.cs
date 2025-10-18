// -----------------------------------------------------------------------
//  <copyright file="Crypto.cs" company="LiuliuSoft">
//      Copyright (c) 2025 66SOFT. All rights reserved.
//  </copyright>
//  <site>https://ifs.66soft.net</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2025-10-18 14:10</last-date>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using OSharp.Data;

// ReSharper disable AssignNullToNotNullAttribute
namespace OSharp.Security
{
    /// <summary>
    /// 加密解密工具类
    /// </summary>
    public static class Crypto
    {
        #region AES加密解密

        /// <summary>
        /// 生成AES密钥
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateAesKey()
        {
            using (var aes = CreateAes())
            {
                aes.GenerateKey();
                return aes.Key;
            }
        }

        /// <summary>
        /// AES加密 byte[] 数据
        /// </summary>
        /// <param name="data">要加密的byte[]数据</param>
        /// <param name="key">AES密钥，必须是32位，如果为null，则使用AES随机密钥</param>
        /// <returns>加密后的数据和密钥</returns>
        public static (AesEncryptData EncryptData, byte[] Key) AesEncrypt(byte[] data, byte[] key = null)
        {
            Check.NotNull(data, nameof(data));
            Check.Required<ArgumentException>(key == null || key.Length == 32, "传入AES密钥必须是32位");

            using (var aes = CreateAes())
            {
                if (key != null && key.Length == 32)
                {
                    aes.Key = key;
                }

                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                            return (new AesEncryptData(ms.ToArray(), aes.IV), aes.Key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// AES解密 AesEncryptData 数据
        /// </summary>
        /// <param name="encryptData">要解密的AesEncryptData数据</param>
        /// <param name="key">AES密钥</param>
        /// <returns>解密后的数据</returns>
        public static byte[] AesDecrypt(AesEncryptData encryptData, byte[] key)
        {
            Check.NotNull(encryptData, nameof(encryptData));
            Check.Required<ArgumentException>(encryptData.Iv != null && encryptData.Iv.Length == 16, "无效的IV值");
            Check.Required<ArgumentException>(encryptData.CipherData != null && encryptData.CipherData.Length > 0,
                "无效的加密数据");
            Check.Required<ArgumentException>(key != null && key.Length == 32, "无效的AES密钥");

            using (var aes = CreateAes())
            {
                using (var decryptor = aes.CreateDecryptor(key, encryptData.Iv))
                {
                    using (var ms = new MemoryStream(encryptData.CipherData))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var decMs = new MemoryStream())
                            {
                                cs.CopyTo(decMs);
                                return decMs.ToArray();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// AES加密 string 数据
        /// </summary>
        /// <param name="data">要加密的string数据</param>
        /// <param name="base64Key">AES密钥的Base64字符串，如果为null，则生成一个随机密钥</param>
        /// <returns>加密后的数据和密钥</returns>
        public static (AesEncryptData EncryptData, byte[] Key) AesEncrypt(string data, string base64Key = null)
        {
            Check.NotNull(data, nameof(data));
            var dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] keyBytes = null;
            if (base64Key != null)
            {
                keyBytes = SafeFromBase64String(base64Key, nameof(base64Key), "无效的AES密钥Base64格式");
            }

            return AesEncrypt(dataBytes, keyBytes);
        }

        /// <summary>
        /// AES解密 AesEncryptData 数据
        /// </summary>
        /// <param name="encryptData">要解密的AesEncryptData数据</param>
        /// <param name="base64Key">AES密钥的Base64字符串</param>
        /// <returns>解密后的数据</returns>
        public static string AesDecrypt(AesEncryptData encryptData, string base64Key)
        {
            Check.NotNull(encryptData, nameof(encryptData));
            Check.NotNullOrEmpty(base64Key, nameof(base64Key));

            var keyBytes = SafeFromBase64String(base64Key, nameof(base64Key), "无效的AES密钥Base64格式");
            var dataBytes = AesDecrypt(encryptData, keyBytes);
            return Encoding.UTF8.GetString(dataBytes);
        }

        /// <summary>
        /// AES加密文件
        /// </summary>
        /// <param name="sourceFile">要加密的源文件</param>
        /// <param name="targetFile">写入加密数据的目标文件</param>
        /// <param name="base64Key">AES密钥的Base64字符串，如果为null，则生成一个随机密钥</param>
        /// <returns>加密后的数据和密钥</returns>
        public static (AesEncryptData EncryptData, byte[] Key) AesEncryptFile(string sourceFile, string targetFile,
            string base64Key = null)
        {
            Check.NotNullOrEmpty(sourceFile, nameof(sourceFile));
            Check.Required<FileNotFoundException>(File.Exists(sourceFile), $"AES加密文件时，源文件 {sourceFile} 不存在。");
            Check.NotNullOrEmpty(targetFile, nameof(targetFile));

            var dataBytes = File.ReadAllBytes(sourceFile);
            byte[] keyBytes = null;
            if (base64Key != null)
            {
                keyBytes = SafeFromBase64String(base64Key, nameof(base64Key), "无效的AES密钥Base64格式");
            }

            var result = AesEncrypt(dataBytes, keyBytes);
            File.WriteAllText(targetFile, result.EncryptData.ToJson());
            return result;
        }

        /// <summary>
        /// AES解密文件
        /// </summary>
        /// <param name="encryptFile">要解密的加密文件</param>
        /// <param name="decryptFile">写入解密数据的目标文件</param>
        /// <param name="base64Key">AES密钥的Base64字符串</param>
        /// <returns>解密后的数据</returns>
        public static void AesDecryptFile(string encryptFile, string decryptFile, string base64Key)
        {
            Check.NotNullOrEmpty(encryptFile, nameof(encryptFile));
            Check.Required<FileNotFoundException>(File.Exists(encryptFile), $"AES解密文件时，加密文件 {encryptFile} 不存在");
            Check.NotNullOrEmpty(decryptFile, nameof(decryptFile));
            Check.NotNullOrEmpty(base64Key, nameof(base64Key));

            var json = File.ReadAllText(encryptFile);
            var encryptData = AesEncryptData.FromJson(json);
            var keyBytes = SafeFromBase64String(base64Key, nameof(base64Key), "无效的AES密钥Base64格式");
            var dataBytes = AesDecrypt(encryptData, keyBytes);
            File.WriteAllBytes(decryptFile, dataBytes);
        }

        private static Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.BlockSize = 128;
            return aes;
        }

        #endregion

        #region RSA加密解密

        /// <summary>
        /// 生成RSA密钥
        /// </summary>
        /// <returns>RSA密钥</returns>
        public static (string PublicKey, string PrivateKey) GenerateRsaKey()
        {
            using (var rsa = RSA.Create())
            {
                return (PublicKey: rsa.ToXmlString(false), PrivateKey: rsa.ToXmlString(true));
            }
        }

        /// <summary>
        /// RSA加密 byte[] 数据
        /// </summary>
        /// <param name="data">要加密的byte[]数据</param>
        /// <param name="publicKey">RSA公钥</param>
        /// <returns>加密后的数据</returns>
        public static byte[] RsaEncrypt(byte[] data, string publicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNullOrEmpty(publicKey, nameof(publicKey));

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(publicKey);

                // 检查数据长度是否超过RSA密钥的限制
                // 对于OAEP-SHA256填充：最大数据长度 = 密钥长度(字节) - 2 * 哈希长度(字节) - 2
                // 对于2048位密钥：256 - 2 * 32 - 2 = 190字节
                var maxDataLength = rsa.KeySize / 8 - 2 * 32 - 2; // 32是SHA256的字节长度
                if (data.Length > maxDataLength)
                {
                    throw new ArgumentException(
                        $@"数据长度({data.Length}字节)超过RSA密钥({rsa.KeySize}位)的最大限制({maxDataLength}字节)。请使用混合加密或分块加密。",
                        nameof(data));
                }

                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
        }

        /// <summary>
        /// RSA解密 byte[] 数据
        /// </summary>
        /// <param name="data">要解密的byte[]数据</param>
        /// <param name="privateKey">RSA私钥</param>
        /// <returns>解密后的数据</returns>
        public static byte[] RsaDecrypt(byte[] data, string privateKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNullOrEmpty(privateKey, nameof(privateKey));

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
        }


        /// <summary>
        /// 使用指定私钥对明文进行签名，返回明文签名的字节数组
        /// </summary>
        /// <param name="data">要签名的明文字节数组</param>
        /// <param name="privateKey">RSA私钥</param>
        /// <returns>明文数据的签名字节数组</returns>
        public static byte[] RsaSignData(byte[] data, string privateKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNullOrEmpty(privateKey, nameof(privateKey));

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKey);
                return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        /// 使用指定公钥验证解密得到的明文是否符合签名
        /// </summary>
        /// <param name="data">解密的明文字节数组</param>
        /// <param name="signature">明文签名字节数组</param>
        /// <param name="publicKey">RSA公钥</param>
        /// <returns>验证是否通过</returns>
        public static bool RsaVerifyData(byte[] data, byte[] signature, string publicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNull(signature, nameof(signature));
            Check.NotNullOrEmpty(publicKey, nameof(publicKey));

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(publicKey);
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        /// RSA加密 string 数据
        /// </summary>
        /// <param name="data">要加密的string数据</param>
        /// <param name="publicKey">RSA公钥</param>
        /// <returns>加密后的数据</returns>
        public static string RsaEncrypt(string data, string publicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNull(publicKey, nameof(publicKey));

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = RsaEncrypt(dataBytes, publicKey);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// RSA解密 string 数据
        /// </summary>
        /// <param name="data">要解密的string数据</param>
        /// <param name="privateKey">RSA私钥</param>
        /// <returns>解密后的数据</returns>     
        public static string RsaDecrypt(string data, string privateKey)
        {
            Check.NotNullOrEmpty(data, nameof(data));
            Check.NotNull(privateKey, nameof(privateKey));

            var dataBytes = SafeFromBase64String(data, nameof(data), "无效的RSA加密数据Base64格式");
            var decryptedBytes = RsaDecrypt(dataBytes, privateKey);
            return Encoding.UTF8.GetString(decryptedBytes);
        }


        /// <summary>
        /// 使用指定私钥对明文进行签名，返回明文签名的Base64字符串
        /// </summary>
        /// <param name="data">要签名的明文字符串</param>
        /// <param name="privateKey">RSA私钥</param>
        /// <returns>明文签名的Base64字符串</returns>
        public static string RsaSignData(string data, string privateKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNull(privateKey, nameof(privateKey));

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = RsaSignData(dataBytes, privateKey);
            return Convert.ToBase64String(signature);
        }

        /// <summary>
        /// 使用指定公钥验证解密得到的明文是否符合签名
        /// </summary>
        /// <param name="data">解密的明文字符串</param>
        /// <param name="signature">明文签名的Base64字符串</param>
        /// <param name="publicKey">RSA公钥</param>
        /// <returns>验证是否通过</returns>
        public static bool RsaVerifyData(string data, string signature, string publicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNullOrEmpty(signature, nameof(signature));
            Check.NotNull(publicKey, nameof(publicKey));

            var signatureBytes = SafeFromBase64String(signature, nameof(signature), "无效的RSA签名Base64格式");
            var dataBytes = Encoding.UTF8.GetBytes(data);
            return RsaVerifyData(dataBytes, signatureBytes, publicKey);
        }

        #endregion

        #region AES+RSA组合加密

        /// <summary>
        /// AES+RSA组合加密，使用自己的RSA私钥对要加密的数据进行签名，使用AES随机生成的密钥加密数据，使用对方的RSA公钥加密AES密钥
        /// </summary>
        /// <param name="data">要加密的明文字节数组</param>
        /// <param name="ownRsaPrivateKey">自己的RSA私钥</param>
        /// <param name="faceRsaPublicKey">对方的RSA公钥</param>
        /// <returns>组合加密后的数据</returns>
        public static HybridEncryptData HybridEncrypt(byte[] data, string ownRsaPrivateKey, string faceRsaPublicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNullOrEmpty(ownRsaPrivateKey, nameof(ownRsaPrivateKey));
            Check.NotNullOrEmpty(faceRsaPublicKey, nameof(faceRsaPublicKey));

            //使用自己的RSA私钥对要加密的数据进行签名
            var signature = RsaSignData(data, ownRsaPrivateKey);

            //使用AES随机生成的密钥加密数据
            var aesEncryptResult = AesEncrypt(data);

            //使用对方的RSA公钥加密AES密钥
            var rsaEncryptedAesKey = RsaEncrypt(aesEncryptResult.Key, faceRsaPublicKey);

            var hybridEncryptData = new HybridEncryptData
            {
                AesEncryptData = aesEncryptResult.EncryptData,
                Signature = signature,
                RsaEncryptedAesKey = rsaEncryptedAesKey
            };
            return hybridEncryptData;
        }

        /// <summary>
        /// AES+RSA组合解密，使用自己的RSA私钥解密AES密钥，使用AES密钥解密数据，使用对方的RSA公钥验证解密数据的签名
        /// </summary>
        /// <param name="hybridEncryptData">组合加密后的数据</param>
        /// <param name="ownRsaPrivateKey">自己的RSA私钥</param>
        /// <param name="faceRsaPublicKey">对方的RSA公钥</param>
        /// <returns>解密后的数据</returns>
        /// <exception cref="CryptographicException">解密后的数据签名验证失败</exception>
        public static byte[] HybridDecrypt(HybridEncryptData hybridEncryptData, string ownRsaPrivateKey,
            string faceRsaPublicKey)
        {
            Check.NotNull(hybridEncryptData, nameof(hybridEncryptData));
            Check.NotNullOrEmpty(ownRsaPrivateKey, nameof(ownRsaPrivateKey));
            Check.NotNullOrEmpty(faceRsaPublicKey, nameof(faceRsaPublicKey));

            // 使用自己的私钥解密AES密钥
            var aesKey = RsaDecrypt(hybridEncryptData.RsaEncryptedAesKey, ownRsaPrivateKey);

            // 使用AES密钥解密数据
            var decryptedData = AesDecrypt(hybridEncryptData.AesEncryptData, aesKey);

            // 使用对方的RSA公钥验证解密数据的签名
            var isValid = RsaVerifyData(decryptedData, hybridEncryptData.Signature, faceRsaPublicKey);
            if (!isValid)
            {
                throw new CryptographicException("解密后的数据签名验证失败");
            }

            return decryptedData;
        }

        /// <summary>
        /// AES+RSA组合加密，使用自己的RSA私钥对要加密的数据进行签名，使用AES随机生成的密钥加密数据，使用对方的RSA公钥加密AES密钥
        /// </summary>
        /// <param name="data">要加密的明文字符串</param>
        /// <param name="ownRsaPrivateKey">自己的RSA私钥</param>
        /// <param name="faceRsaPublicKey">对方的RSA公钥</param>
        /// <returns>组合加密后的数据</returns>
        public static string HybridEncrypt(string data, string ownRsaPrivateKey, string faceRsaPublicKey)
        {
            Check.NotNull(data, nameof(data));
            Check.NotNull(ownRsaPrivateKey, nameof(ownRsaPrivateKey));
            Check.NotNull(faceRsaPublicKey, nameof(faceRsaPublicKey));

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hybridEncryptData = HybridEncrypt(dataBytes, ownRsaPrivateKey, faceRsaPublicKey);
            return hybridEncryptData.ToJson();
        }

        /// <summary>
        /// AES+RSA组合解密，使用自己的RSA私钥解密AES密钥，使用AES密钥解密数据，使用对方的RSA公钥验证解密数据的签名
        /// </summary>
        /// <param name="json">组合加密后的数据</param>
        /// <param name="ownRsaPrivateKey">自己的RSA私钥</param>
        /// <param name="faceRsaPublicKey">对方的RSA公钥</param>
        /// <returns>解密后的数据</returns>
        /// <exception cref="CryptographicException">解密后的数据签名验证失败</exception>
        public static string HybridDecrypt(string json, string ownRsaPrivateKey, string faceRsaPublicKey)
        {
            Check.NotNullOrEmpty(json, nameof(json));
            Check.NotNull(ownRsaPrivateKey, nameof(ownRsaPrivateKey));
            Check.NotNull(faceRsaPublicKey, nameof(faceRsaPublicKey));

            var hybridEncryptData = HybridEncryptData.FromJson(json);
            var decryptedData = HybridDecrypt(hybridEncryptData, ownRsaPrivateKey, faceRsaPublicKey);
            return Encoding.UTF8.GetString(decryptedData);
        }

        /// <summary>
        /// 安全地将Base64字符串转换为字节数组
        /// </summary>
        /// <param name="base64String">Base64字符串</param>
        /// <param name="paramName">参数名称，用于异常消息</param>
        /// <param name="customMessage">自定义错误消息，如果为null则使用默认消息</param>
        /// <returns>转换后的字节数组</returns>
        /// <exception cref="ArgumentException">当Base64格式无效时</exception>
        private static byte[] SafeFromBase64String(string base64String, string paramName, string customMessage = null)
        {
            try
            {
                return Convert.FromBase64String(base64String);
            }
            catch (FormatException)
            {
                var message = customMessage ?? $"无效的Base64格式: {paramName}";
                throw new ArgumentException(message, paramName);
            }
        }

        #endregion
    }

    /// <summary>
    /// AES加密后的数据，包含加密后的数据和IV
    /// </summary>
    public class AesEncryptData
    {
        // 无参构造函数，支持JSON序列化
        public AesEncryptData()
        { }

        public AesEncryptData(byte[] cipherData, byte[] iv)
        {
            CipherData = cipherData ?? throw new ArgumentNullException(nameof(cipherData));
            Iv = iv ?? throw new ArgumentNullException(nameof(iv));

            if (Iv.Length != 16)
                throw new ArgumentException(@"IV长度必须为16字节", nameof(iv));
        }

        public byte[] Iv { get; set; }
        public byte[] CipherData { get; set; }

        public string GetIvString()
        {
            return Convert.ToBase64String(Iv);
        }

        public string GetCipherDataString()
        {
            return Convert.ToBase64String(CipherData);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static AesEncryptData FromJson(string json)
        {
            return JsonSerializer.Deserialize<AesEncryptData>(json);
        }
    }

    /// <summary>
    /// 组合加密后的数据，AES加密数据、AES向量IV、RSA加密的AES密钥、数据签名
    /// </summary>
    public class HybridEncryptData
    {
        public AesEncryptData AesEncryptData { get; set; }
        public byte[] Signature { get; set; }
        public byte[] RsaEncryptedAesKey { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static HybridEncryptData FromJson(string json)
        {
            return JsonSerializer.Deserialize<HybridEncryptData>(json);
        }
    }
}
