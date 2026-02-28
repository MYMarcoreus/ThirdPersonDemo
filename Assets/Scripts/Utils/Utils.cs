using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Google.Protobuf;
using UnityEngine;
using Yy.Protocol.App;


namespace Utils
{
    public static class Struct
    {
        public static (Vector3, Quaternion) ParseNetTransform(Transform_net t)
        {
            var pos = new Vector3(
                t.Position.X,
                t.Position.Y,
                t.Position.Z
            );

            var rot = new Quaternion(
                t.Rotation.X,
                t.Rotation.Y,        
                t.Rotation.Z,        
                t.Rotation.W        
            );
            return (pos, rot); 
        }

        public static (Vector3_net, Quaternion_net) PackNetTransform(Transform t)
        {
            var pos = new Vector3_net
            {
                X = t.position.x,
                Y = t.position.y,
                Z = t.position.z
            };
            var rot = new Quaternion_net
            {
                X = t.rotation.x,
                Y = t.rotation.y,
                Z = t.rotation.z,
                W = t.rotation.w
            };
            return (pos, rot);
        }
        
        //结构体转为字节数组
        public static byte[] StructToByteArray<T>(T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            IntPtr p = Marshal.AllocHGlobal(size);
            try
            {
                byte[] bytes = new byte[size];
                Marshal.StructureToPtr(obj, p, false);
                Marshal.Copy(p, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        //字节数组转为结构体
        public static T ByteArrayToStruct<T>(byte[] bytes) where T : struct
        {

            int size = bytes.Length;
            IntPtr p = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, p, size);
                object obj = Marshal.PtrToStructure<T>(p);
                return (T)obj;
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public static ByteString StructToByteString<T>(this T data) where T : struct
        {
            return ByteString.CopyFrom(StructToByteArray(data));
        }

        public static T ByteStringToStruct<T>(ByteString data) where T : struct
        {
            return data.Length == 0 ? new T() : ByteArrayToStruct<T>(data.ToByteArray());
        }


        #region StringToBytes【函数】将字符串转化为字节组

        /// <summary>
        /// 【函数】将字符串转化为字节组
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] StringToBytes(string data) =>
            data is null ? null : System.Text.Encoding.Default.GetBytes(data);

        #endregion

        #region BytesToString【函数】将字节数组转化为字符串

        /// <summary>
        /// 【函数】将字节数组转化为字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string BytesToString(byte[] data) =>
            data is null ? null : System.Text.Encoding.Default.GetString(data);

        #endregion

    }
    
    public static class Utils
    {

        public static string ComputeMD5(string str)
        {
            MD5 m = MD5.Create();
            byte[] ib = System.Text.Encoding.ASCII.GetBytes(str);
            byte[] hash = m.ComputeHash(ib);
            string sss = "";
            for (int i = 0; i < hash.Length; i++)
                sss += hash[i].ToString("x2");
            return sss;
        }
        
        public static bool IsAlphanumeric(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // 只允许数字和大小写字母
            return Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
        }
        
        public static bool IsValidIPv4(string ip)
        {
            /* 定义IPv4地址的正则表达式
           ^                                             表示从字符串开始位置进行匹配
           ([01]?[0-9]?[0-9] | 2[0-4][0-9] | 25[0-5])    表示0~255，分为三个部分进行判断，这三部分用竖线（|）连接起来
                    ([01]?[0-9]?[0-9])                       表示0~9、00~99、000~199，问号表示前导0是可选的
                    2[0-4][0-9]                              表示200~249
                    25[0-5]                                  表示250~255
           \.                                            表示匹配.
           {3}                                           表示前面的式子要匹配三遍
            */
            string numslice = "([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])";
            string pattern = $@"^({numslice}\.)"+"{3}"+$"{numslice}$";
            return Regex.Match(ip, pattern).Success; // 使用正则表达式进行匹配
        }
    }
}
