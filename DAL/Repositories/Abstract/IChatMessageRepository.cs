using DAL.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Abstract
{
    public interface IChatMessageRepository
    {
        Task<Responce> CreateAsync(ChatMessage chatMessage);
        Task<Responce> UpdateAsync(ChatMessage chatMessage);
        Task<Responce> DeleteAsync(Guid id);
        Task<ChatMessage> FindByIdAsync(Guid id);
        Task<List<ChatMessage>> GetAllChatMessagesAsListAsync();
        IQueryable<ChatMessage> GetAllChatMessages();
    }
}
