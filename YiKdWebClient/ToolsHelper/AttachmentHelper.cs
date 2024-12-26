using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.ToolsHelper
{
    /// <summary>
    /// 附件工具类
    /// </summary>
    public  class AttachmentHelper
    {

      /// <summary>
      /// 
      /// </summary>
      /// <param name="filePath"></param>
      /// <param name="chunkSize"></param>
      /// <param name="chunkAction"></param>
        public static void ReadFileInChunksByAction(string filePath, Action<byte[]> chunkAction,long chunkSize=1024*1024)
        {
           // List<byte[]> chunks = new List<byte[]>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (bytesRead < chunkSize)
                    {
                        byte[] lastChunk = new byte[bytesRead];
                        Array.Copy(buffer, lastChunk, bytesRead);
                        // chunks.Add();
                        chunkAction(lastChunk);
                    }
                    else
                    {
                        // chunks.Add((byte[])buffer.Clone());
                        chunkAction((byte[])buffer.Clone());
                    }

                   
                }
            }

           // return chunks.ToArray();
        }


    }
    /// <summary>
    /// 文件块
    /// </summary>
    public class FileChunk 
    {
        /// <summary>
        /// 文件块索引
        /// </summary>
        public long Chunkindex { get; set; } = 0;

        /// <summary>
        /// 文件名
        /// </summary>
        public string Filename { get; set; }=string.Empty;

        /// <summary>
        /// 是否为最后一块
        /// </summary>
        public bool IsLast { get; set; } = false;
        /// <summary>
        /// 分块byte流
        /// </summary>
        public byte[] Chunkbyte { get; set; }=new byte[0];
        /// <summary>
        /// 分块base64流
        /// </summary>
        public string ChunkBase64 { get; set; } = string.Empty;
    }
}
