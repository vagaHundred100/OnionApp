using BLL.DTO;
using DAL.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services.Abstract
{
    public interface IChatService
    {
        Task<Responce> DeleteMesageFromUserAsync(string messageStringId, string userStringId);
        Task<Responce> DeleteMessageFromChatAsync(string messageId, string userStringId);
        Task<Responce> ExportToPDF_AllRecivedMessagesAsync(string reciverId);
        Task<Responce> ExportToPDF_AllSentMessages(string senderId);
        Task<ResponceWithData<List<GroupedMessagesDTO>>> ReadAllRecivedMessagesAsync(string reciverID);
        Task<ResponceWithData<List<GroupedMessagesDTO>>> ReadAllSentMessagesAsync(string senderId);
        Task<Responce> UpdateMessage(string messageId);
        Task<Responce> WriteMessageAsync(MessageWriteDTO messageDTO);
    }
}
