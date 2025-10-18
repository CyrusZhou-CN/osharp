# 创建 Encryptor 加密解密辅助类

## 目标

在 `Encryptor.cs` 中实现完整的加密解密功能，使用结构化数据封装，避免字节拼接：

- **AES 独立加密**：支持对称加密，自动生成IV，使用结构化返回
- **RSA 独立加密**：支持非对称加密和签名验证，使用结构化返回
- **混合加密**：RSA+AES混合加密，使用结构化返回
- 采用静态方法设计
- 统一使用 `OSharp.Security` 命名空间

## 实现方案

### 1. 数据结构设计（嵌套类）

```csharp
public static class Encryptor
{
    /// <summary>
    /// AES加密结果
    /// </summary>
    public class AesEncryptResult
    {
        public byte[] IV { get; set; }          // 16字节IV
        public byte[] CipherData { get; set; }  // 加密数据
        public string Key { get; set; }         // 使用的密钥（自动生成时填充）
        
        // 便捷方法
        public string ToJson()                  // 序列化为JSON（所有byte[]转Base64）
        public static AesEncryptResult FromJson(string json)  // 从JSON反序列化
        public string GetIVBase64()             // 获取IV的Base64字符串
        public string GetCipherDataBase64()     // 获取加密数据的Base64字符串
    }
    
    /// <summary>
    /// RSA加密结果
    /// </summary>
    public class RsaEncryptResult
    {
        public byte[] CipherData { get; set; }  // 加密数据
        
        // 便捷方法
        public string ToBase64()                // 转换为Base64字符串
        public static RsaEncryptResult FromBase64(string base64)  // 从Base64创建
    }
    
    /// <summary>
    /// RSA签名结果
    /// </summary>
    public class RsaSignResult
    {
        public byte[] Data { get; set; }        // 原始数据
        public byte[] Signature { get; set; }   // 签名
        
        // 便捷方法
        public string ToJson()                  // 序列化为JSON
        public static RsaSignResult FromJson(string json)  // 从JSON反序列化
        public string GetSignatureBase64()      // 获取签名的Base64字符串
    }
    
    /// <summary>
    /// 混合加密结果
    /// </summary>
    public class HybridEncryptResult
    {
        public byte[] EncryptedAesKey { get; set; }  // RSA加密的AES密钥
        public AesEncryptResult AesResult { get; set; } // AES加密结果
        
        // 便捷方法
        public string ToJson()                  // 序列化为JSON
        public static HybridEncryptResult FromJson(string json)  // 从JSON反序列化
    }
}
```

**Result类设计原则：**

- 内部使用 `byte[]` 存储，保持性能和灵活性
- 提供 `ToJson()`/`FromJson()` 方便持久化和传输
- 提供 `ToBase64()`/`GetXxxBase64()` 方便字符串化
- 使用 Newtonsoft.Json 进行序列化（项目已引用）

### 2. AES加密功能

#### 字节数组操作

- `AesEncryptResult AesEncrypt(byte[] data, string key = null)` 
  - 自动生成IV
  - key为null时生成随机密钥并填入Result.Key

- `byte[] AesDecrypt(AesEncryptResult result, string key = null)` 
  - 使用result的IV解密
  - key为null时使用result.Key

#### 字符串操作

- `AesEncryptResult AesEncryptString(string data, string key = null)`
  - 内部转UTF8字节后加密

- `string AesDecryptString(AesEncryptResult result, string key = null)`
  - 解密后转UTF8字符串

#### 文件操作

- `AesEncryptResult AesEncryptFile(string sourceFile, string targetFile, string key = null)`
  - 读取源文件加密后写入目标文件
  - 返回加密信息供解密使用

- `void AesDecryptFile(string encryptedFile, string targetFile, AesEncryptResult result, string key = null)`
  - 使用result信息解密文件

#### 辅助方法

- `string GenerateAesKey()` - 生成32字节密钥的Base64

### 3. RSA加密功能

#### 加密解密

- `RsaEncryptResult RsaEncrypt(byte[] data, string publicKey)`
- `byte[] RsaDecrypt(RsaEncryptResult result, string privateKey)`
- `RsaEncryptResult RsaEncryptString(string data, string publicKey)`
- `string RsaDecryptString(RsaEncryptResult result, string privateKey)`

#### 签名验证

- `RsaSignResult RsaSign(byte[] data, string privateKey)`
- `bool RsaVerify(RsaSignResult result, string publicKey)`
- `RsaSignResult RsaSignString(string data, string privateKey)`
- `bool RsaVerifyString(RsaSignResult result, string publicKey)`

#### 密钥生成

- `(string PublicKey, string PrivateKey) GenerateRsaKeyPair(int keySize = 2048)`

### 4. 混合加密功能

- `HybridEncryptResult HybridEncrypt(byte[] data, string rsaPublicKey)`
  - 生成随机AES密钥
  - 用AES加密数据（含IV）
  - 用RSA加密AES密钥
  - 返回结构化结果

- `byte[] HybridDecrypt(HybridEncryptResult result, string rsaPrivateKey)`
  - 用RSA解密AES密钥
  - 用AES密钥和IV解密数据

- `HybridEncryptResult HybridEncryptString(string data, string rsaPublicKey)`
- `string HybridDecryptString(HybridEncryptResult result, string rsaPrivateKey)`
- `HybridEncryptResult HybridEncryptFile(string sourceFile, string targetFile, string rsaPublicKey)`
- `void HybridDecryptFile(string encryptedFile, string targetFile, HybridEncryptResult result, string rsaPrivateKey)`

### 5. 技术规格

**AES**

- 算法：AES-256-CBC
- 密钥：32字节（Base64编码）
- IV：16字节（每次随机生成）
- 填充：PKCS7

**RSA**

- 默认密钥长度：2048位
- 填充：RSAEncryptionPadding.Pkcs1
- 签名：SHA256withRSA
- 密钥格式：XML

**混合加密**

- AES加密数据，RSA加密AES密钥
- 适合大数据量加密
