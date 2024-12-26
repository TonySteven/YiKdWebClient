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
    public class AttachmentHelper
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="chunkSize"></param>
        /// <param name="chunkAction"></param>
        public static void ReadFileInChunksByAction(string filePath, Action<FileChunk> chunkAction, long chunkSize = 1024 * 1024)
        {
            // List<byte[]> chunks = new List<byte[]>();
            string fileName = Path.GetFileName(filePath); //文件名。
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                long Chunkindex = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    /*构造文件分块内容*/
                    FileChunk fileChunk = new FileChunk();
                    fileChunk.Filename = fileName;
                    fileChunk.Chunkindex = Chunkindex;
                    if (bytesRead <= chunkSize)
                    {
                        byte[] lastChunk = new byte[bytesRead];
                        Array.Copy(buffer, lastChunk, bytesRead);

                        fileChunk.IsLast = true;
                        fileChunk.Chunkbyte = lastChunk;
                        // chunks.Add();

                    }
                    else
                    {
                        // chunks.Add((byte[])buffer.Clone());
                        fileChunk.Chunkbyte = (byte[])buffer.Clone();
                    }

                    chunkAction(fileChunk);
                    Chunkindex++;
                }
            }

            // return chunks.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="yiK3CloudClient"></param>
        /// <param name="UploadModeltemplate"></param>
        /// <param name="filePath"></param>
        /// <param name="chunkSize"></param>
        public static string AttachmentUpload(string filePath, YiK3CloudClient yiK3CloudClient, UploadModel UploadModeltemplate, long chunkSize = 1024 * 1024)
        {




            string FileId = string.Empty;
            string resjson = string.Empty;

            Action<FileChunk> action = (fileChunk) =>
            {
                if (fileChunk.IsLast) { resjson = "ok"; }
            };
            ReadFileInChunksByAction(filePath, action, chunkSize);
            return resjson;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UploadModelTemplate"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void CheckUploadModelData(UploadModel UploadModelTemplate)
        {
            var data = UploadModelTemplate.data;

            if (string.IsNullOrWhiteSpace(data.FileName))
            {
                throw new ArgumentException("FileName cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(data.FormId))
            {
                throw new ArgumentException("FormId cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(data.InterId))
            {
                throw new ArgumentException("InterId cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(data.BillNO))
            {
                throw new ArgumentException("BillNO cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(data.FileId))
            {
                throw new ArgumentException("FileId cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(data.SendByte))
            {
                throw new ArgumentException("SendByte cannot be null or empty.");
            }

            // Check Entrykey and EntryinterId conditions
            if (string.IsNullOrWhiteSpace(data.Entrykey) != string.IsNullOrWhiteSpace(data.EntryinterId))
            {
                throw new ArgumentException("Both Entrykey and EntryinterId should be either null or non-empty.");
            }

            // Add additional validation rules as needed
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
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// 是否为最后一块
        /// </summary>
        public bool IsLast { get; set; } = false;

        private byte[] _Chunkbyte = new Byte[0];
        /// <summary>
        /// 分块byte流
        /// </summary>
        public byte[] Chunkbyte
        {
            get { return _Chunkbyte; }
            set
            {
                _Chunkbyte = value;
                ChunkBase64 = Convert.ToBase64String(value);
            }
        }
        /// <summary>
        /// 分块base64流
        /// </summary>
        public string ChunkBase64 { get; set; } = string.Empty;
    }

}