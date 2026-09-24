using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;

namespace WeiKit
{
    /// <summary>
    /// 文件加密存储类
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns></returns>
        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns></returns>
        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将对象序列化并加密后保存到指定路径。
        /// </summary>
        /// <typeparam name="T">要保存的对象类型。</typeparam>
        /// <param name="filePath">文件的完整路径。</param>
        /// <param name="obj">要保存的对象实例。</param>
        /// <param name="password">用于加密的密码。</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void SaveEncrypted<T>(string filePath, T obj, string password) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空或空白。", nameof(filePath));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("密码不能为空或空白。", nameof(password));

            // 将对象序列化为JSON字符串
            string jsonString = JsonConvert.SerializeObject(obj);
            // 生成AES加密所需的Key和IV
            using (Aes aesAlg = Aes.Create())
            {
                byte[] salt = GenerateRandomSalt(); // 随机生成盐值
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256); // 使用更强的迭代次数和哈希算法
                aesAlg.Key = pdb.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = pdb.GetBytes(aesAlg.BlockSize / 8);

                // 加密数据
                byte[] encryptedData = EncryptStringToBytes_Aes(jsonString, aesAlg.Key, aesAlg.IV);

                // 将盐值附加到加密数据前面
                byte[] dataWithSalt = new byte[salt.Length + encryptedData.Length];
                Buffer.BlockCopy(salt, 0, dataWithSalt, 0, salt.Length);
                Buffer.BlockCopy(encryptedData, 0, dataWithSalt, salt.Length, encryptedData.Length);

                // 保存到文件
                File.WriteAllBytes(filePath, dataWithSalt);
            }
        }

        /// <summary>
        /// 从指定路径读取加密后的数据，解密并反序列化为对象。
        /// </summary>
        /// <typeparam name="T">要载入的对象类型。</typeparam>
        /// <param name="filePath">文件的完整路径。</param>
        /// <param name="password">用于解密的密码。</param>
        /// <returns>返回解密并反序列化后的对象实例。</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static T LoadDecrypted<T>(string filePath, string password) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("文件路径不能为空或空白。", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("指定的文件不存在。", filePath);
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("密码不能为空或空白。", nameof(password));
            // 从文件读取加密后的数据
            byte[] dataWithSalt = File.ReadAllBytes(filePath);
            // 提取盐值
            int saltLength = 32; // 盐值长度
            if (dataWithSalt.Length < saltLength)
            {
                throw new InvalidOperationException("文件数据损坏，无法提取盐值。");
            }
            byte[] salt = new byte[saltLength];
            Buffer.BlockCopy(dataWithSalt, 0, salt, 0, salt.Length);
            // 提取加密数据
            byte[] encryptedData = new byte[dataWithSalt.Length - saltLength];
            Buffer.BlockCopy(dataWithSalt, saltLength, encryptedData, 0, encryptedData.Length);
            // 生成AES加密所需的Key和IV
            using (Aes aesAlg = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256); // 使用更强的迭代次数和哈希算法
                aesAlg.Key = pdb.GetBytes(aesAlg.KeySize / 8);
                aesAlg.IV = pdb.GetBytes(aesAlg.BlockSize / 8);
                // 解密数据
                string decryptedJson = DecryptStringFromBytes_Aes(encryptedData, aesAlg.Key, aesAlg.IV);
                // 反序列化为对象
                return JsonConvert.DeserializeObject<T>(decryptedJson);
            }
        }

        /// <summary>
        /// 生成随机盐值。
        /// </summary>
        /// <returns>随机生成的盐值。</returns>
        private static byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[32]; // 盐值长度
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}

